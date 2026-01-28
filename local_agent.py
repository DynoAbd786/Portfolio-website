
import sys
import json
import requests
import sseclient
import threading
import time
import os
import argparse
from termcolor import colored
from datetime import datetime

# CONFIGURATION
# Default configuration, can be overridden by arguments
DEFAULT_SERVER_URL = "https://mkkai.dev/api/mcp/sse"
DEFAULT_OLLAMA_URL = "http://localhost:11434"
STATE_FILE = os.path.expanduser("~/.mcp_bridge_state.json")

class SessionManager:
    def __init__(self):
        self.state = {
            "session_id": None,
            "endpoint": None,
            "history": [],
            "current_model": "llama3.2:1b"
        }
        self.load_state()

    def load_state(self):
        if os.path.exists(STATE_FILE):
            try:
                with open(STATE_FILE, 'r') as f:
                    self.state.update(json.load(f))
            except Exception as e:
                print(colored(f"Error loading state: {e}", "red"))

    def save_state(self):
        try:
            with open(STATE_FILE, 'w') as f:
                json.dump(self.state, f, indent=2)
        except Exception as e:
            print(colored(f"Error saving state: {e}", "red"))

    def get_session_id(self):
        return self.state["session_id"]
    
    def set_session_info(self, endpoint):
        # Extract session_id from endpoint if possible
        # Assumes endpoint format: ...?sessionId=XYZ
        try:
            from urllib.parse import urlparse, parse_qs
            parsed = urlparse(endpoint)
            sid = parse_qs(parsed.query).get('sessionId', [None])[0]
            if sid:
                self.state["session_id"] = sid
        except:
            pass
        self.state["endpoint"] = endpoint
        self.save_state()

    def add_history(self, role, content):
        self.state["history"].append({"role": role, "content": content})
        # Keep history manageable
        if len(self.state["history"]) > 20:
             self.state["history"] = self.state["history"][-20:]
        self.save_state()

    def get_history(self):
        return self.state["history"]

    def get_model(self):
        return self.state["current_model"]

    def set_model(self, model):
        self.state["current_model"] = model
        self.save_state()

class OllamaClient:
    def __init__(self, base_url):
        self.base_url = base_url

    def list_models(self):
        try:
            resp = requests.get(f"{self.base_url}/api/tags")
            if resp.status_code == 200:
                models = [m['name'] for m in resp.json().get('models', [])]
                return models
            return []
        except:
            return []

    def chat(self, model, messages, tools=None):
        payload = {
            "model": model,
            "messages": messages,
            "stream": False
        }
        
        # Add tools if the model supports them and tools are provided
        # Note: Generic Ollama tool support varies by model, but we'll try standard format
        if tools:
            # Simple prompt injection for models that don't support native tools yet OR standard tool definitions
            # For now, let's inject tool definitions into the system prompt for reliability
            # system_msg = self._create_tool_system_prompt(tools)
            # messages = [{"role": "system", "content": system_msg}] + messages
            pass 

        # Ideally use native tool support if available in newer Ollama versions
        # payload["tools"] = tools 

        try:
            resp = requests.post(f"{self.base_url}/api/chat", json=payload)
            if resp.status_code == 200:
                return resp.json().get('message', {})
            return {"error": f"Ollama Error: {resp.text}"}
        except Exception as e:
            return {"error": str(e)}

    def _create_tool_system_prompt(self, tools):
        tool_desc = json.dumps(tools, indent=2)
        return (
            "You have access to the following tools via the Model Context Protocol:\n"
            f"{tool_desc}\n\n"
            "To use a tool, reply with a JSON object: {\"tool\": \"tool_name\", \"arguments\": {...}}\n"
            "If no tool is needed, reply normally."
        )

class MCPAgent:
    def __init__(self, server_url, ollama_url, service_mode=False):
        self.server_url = server_url
        self.ollama = OllamaClient(ollama_url)
        self.session_mgr = SessionManager()
        self.service_mode = service_mode
        self.post_endpoint = self.session_mgr.state["endpoint"]
        self.available_tools = [] # To be populated from MCP server

    def start(self):
        # 0. Check Model Availability
        try:
            available = self.ollama.list_models()
            current = self.session_mgr.get_model()
            if available and current not in available:
                print(colored(f"[Config] Model '{current}' not found locally.", "yellow"))
                new_model = available[0]
                self.session_mgr.set_model(new_model)
                print(colored(f"[Config] Auto-switching to available model: '{new_model}'", "green"))
        except Exception as e:
            print(colored(f"[Warning] Could not verify models: {e}", "yellow"))

        # Start SSE Listener
        threading.Thread(target=self._sse_listener, daemon=True).start()
        
        if self.service_mode:
            print(colored("[Service Mode] Running in background...", "blue"))
            while True:
                time.sleep(1)
        else:
            self._interactive_mode()

    def _sse_listener(self):
        headers = {'Accept': 'text/event-stream'}
        while True:
            try:
                print(colored(f"[SSE] Connecting to {self.server_url}...", "cyan"))
                response = requests.get(self.server_url, stream=True, headers=headers)
                client = sseclient.SSEClient(response)
                
                for event in client.events():
                    if event.event == 'endpoint':
                        # ... Handshake ...
                        self.post_endpoint = event.data
                        self.session_mgr.set_session_info(event.data)
                        print(colored(f"[SSE] Handshake complete. Endpoint: {self.post_endpoint}", "green"))
                        
                        # 1. Advertise Local Models to Server
                        models = self.ollama.list_models()
                        self._send_mcp_request("notifications/models_list", {"models": models})
                        
                        # 2. Discover Remote Tools
                        self._send_mcp_request("tools/list", {})

                    elif event.event == 'message':
                        # Standard MCP JSON-RPC
                        data = json.loads(event.data)
                        self._handle_mcp_message(data)

                    elif event.event == 'chat_request':
                        # Special event for "Scenario A" (Site uses Local LLM)
                        data = json.loads(event.data)
                        self._handle_remote_chat(data)

            except Exception as e:
                print(colored(f"[SSE] Connection error: {e}", "red"))
                time.sleep(5)

    def _handle_remote_chat(self, data):
        # data = { "model": "..", "messages": [...], "callback_id": "..." }
        print(colored(f"[Remote] Chat Request for model: {data.get('model')}", "magenta"))
        
        model = data.get('model') or self.session_mgr.get_model()
        messages = data.get('messages', [])
        
        response = self.ollama.chat(model, messages)
        
        # Send result back (Assuming a specific tool or endpoint for this)
        # For now, we use a generic notification or a specific method if defined
        result_payload = {
            "callback_id": data.get("callback_id"),
            "response": response.get("content", ""),
            "model": model
        }
        self._send_mcp_request("notifications/chat_response", result_payload)


    def _send_mcp_request(self, method, params):
        if not self.post_endpoint:
            return
        
        payload = {
            "jsonrpc": "2.0",
            "method": method,
            "params": params,
            "id": int(time.time() * 1000)
        }
        try:
            requests.post(self.post_endpoint, json=payload)
        except Exception as e:
            print(colored(f"[MCP] Send Error: {e}", "red"))

    def _handle_mcp_message(self, data):
        # Handle responses from MCP server
        if "result" in data:
            result = data["result"]
            # If it's a tool list
            if "tools" in result:
                self.available_tools = result["tools"]
                print(colored(f"[MCP] Discovered {len(self.available_tools)} tools.", "yellow"))
            
            # If it's a tool call result (from a request we initiated via Ollama)
            # In interactive mode, we'd display this.
            pass

    def _interactive_mode(self):
        print(colored("=== Local MCP Agent ===", "green"))
        print(f"Server: {self.server_url}")
        print(f"Model: {self.session_mgr.get_model()}")
        print("Commands: /switch <model>, /tools, /quit")
        
        while True:
            try:
                user_input = input(colored("\nYou: ", "green"))
                if not user_input.strip(): continue

                if user_input.startswith("/switch"):
                    model = user_input.split(" ")[1]
                    self.session_mgr.set_model(model)
                    print(colored(f"Switched to {model}", "yellow"))
                    continue
                
                if user_input.startswith("/tools"):
                    print(json.dumps(self.available_tools, indent=2))
                    continue

                if user_input == "/quit":
                    break

                self.session_mgr.add_history("user", user_input)
                self._process_chat(user_input)

            except KeyboardInterrupt:
                break

    def _process_chat(self, user_input):
        print(colored("Thinking...", "grey"))
        
        # 1. Ask Ollama (with tool definitions injected)
        # Note: A real implementation would convert self.available_tools to Ollama tool format
        # For this bridge, we'll do a simple pass for now or assume model can handle text description
        
        messages = self.session_mgr.get_history()
        
        response = self.ollama.chat(self.session_mgr.get_model(), messages)
        content = response.get("content", "")
        
        if content:
            print(colored(f"Ollama: {content}", "blue"))
            self.session_mgr.add_history("assistant", content)
            
            # Here we would detect tool calls in 'content' if the model outputted JSON
            # and then call _send_mcp_request("tools/call", ...)

        elif "error" in response:
            print(colored(response["error"], "red"))

if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--service", action="store_true", help="Run in background service mode")
    parser.add_argument("--server", default=DEFAULT_SERVER_URL, help="MCP Server URL")
    args = parser.parse_args()

    agent = MCPAgent(args.server, DEFAULT_OLLAMA_URL, args.service)
    agent.start()

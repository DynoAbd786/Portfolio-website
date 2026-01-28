
import sys
import os
import json
import requests
import sseclient # pip install sseclient-py
import threading
import time

# CONFIGURATION
SERVER_URL = "https://mkkai.dev/api/mcp/sse"
OLLAMA_URL = "http://localhost:11434/api/chat"
SESSION_ID = None
POST_ENDPOINT = None

def get_bridge_key():
    return os.environ.get("BRIDGE_API_KEY")

def send_response_to_server(response_data):
    if not POST_ENDPOINT:
        sys.stderr.write("Error: No POST endpoint to send response\n")
        return

    headers = {'Content-Type': 'application/json'}
    api_key = get_bridge_key()
    if api_key:
        headers['X-Bridge-Key'] = api_key

    try:
        sys.stderr.write(f"Sending response to {POST_ENDPOINT}\n")
        requests.post(POST_ENDPOINT, json=response_data, headers=headers)
    except Exception as e:
        sys.stderr.write(f"Failed to post response: {e}\n")

def call_ollama(model, messages, callback_id):
    sys.stderr.write(f"Calling Ollama with model: {model}\n")
    
    # payload for ollama
    payload = {
        "model": model,
        "messages": messages,
        "stream": False # Use non-streaming for simplicity first
    }

    try:
        # If model is not specified or empty, default to phi4
        target_model = model if model else "phi4"
        payload["model"] = target_model

        response = requests.post(OLLAMA_URL, json=payload)
        
        if response.status_code == 200:
            result = response.json()
            content = result.get("message", {}).get("content", "")
            
            # Send back to server
            # The server expects a JSON-RPC like notification or just the payload
            # Based on McpController logic: method="notifications/chat_response", params={callback_id, response, model}
            
            bridge_response = {
                "jsonrpc": "2.0",
                "method": "notifications/chat_response",
                "params": {
                    "callback_id": callback_id,
                    "response": content,
                    "model": target_model
                }
            }
            send_response_to_server(bridge_response)
        else:
             sys.stderr.write(f"Ollama error: {response.text}\n")
             
    except Exception as e:
        sys.stderr.write(f"Failed to call Ollama: {e}\n")

def fetch_models(callback_id):
    sys.stderr.write("Fetching local Ollama models...\n")
    try:
        response = requests.get("http://localhost:11434/api/tags")
        if response.status_code == 200:
            result = response.json()
            models = [m.get("name") for m in result.get("models", [])]
            
            bridge_response = {
                "jsonrpc": "2.0",
                "method": "notifications/models_response",
                "params": {
                    "callback_id": callback_id,
                    "models": models
                }
            }
            send_response_to_server(bridge_response)
        else:
            sys.stderr.write(f"Ollama tags error: {response.status_code}\n")
    except Exception as e:
        sys.stderr.write(f"Failed to fetch models: {e}\n")

def handle_server_event(event):
    global POST_ENDPOINT
    
    if event.event == 'endpoint':
        POST_ENDPOINT = event.data
        sys.stderr.write(f"Connected! Post endpoint: {POST_ENDPOINT}\n")
        
    elif event.event == 'chat_request':
        # Parse the custom chat request format from McpService
        # payload = { model, callback_id, messages }
        try:
            data = json.loads(event.data)
            sys.stderr.write(f"Received chat request: {data.get('callback_id')}\n")
            
            # Spawn a thread to handle the chat generation so we don't block the SSE loop
            t = threading.Thread(target=call_ollama, args=(
                data.get('model'), 
                data.get('messages'), 
                data.get('callback_id')
            ))
            t.start()
            
        except Exception as e:
            sys.stderr.write(f"Error parsing chat request: {e}\n")

    elif event.event == 'models_request':
        try:
            data = json.loads(event.data)
            callback_id = data.get('callback_id')
            
            # Spawn a thread to fetch models
            t = threading.Thread(target=fetch_models, args=(callback_id,))
            t.start()
        except Exception as e:
            sys.stderr.write(f"Error parsing models request: {e}\n")

    elif event.event == 'ping':
        pass # heartbeats

def listen_to_server():
    api_key = get_bridge_key()
    headers = {'Accept': 'text/event-stream'}
    if api_key:
        headers['X-Bridge-Key'] = api_key
        sys.stderr.write(f"Using API Key: {api_key[:4]}***\n")
    else:
        sys.stderr.write("Warning: BRIDGE_API_KEY not set.\n")
    
    while True:
        try:
            sys.stderr.write("Connecting to server...\n")
            response = requests.get(SERVER_URL, stream=True, headers=headers)
            
            if response.status_code != 200:
                sys.stderr.write(f"Server error: {response.status_code}\n")
                time.sleep(5)
                continue
                
            client = sseclient.SSEClient(response)
            for event in client.events():
                handle_server_event(event)
                
        except Exception as e:
            sys.stderr.write(f"Connection lost: {e}\n")
            time.sleep(5) # Reconnect delay

if __name__ == "__main__":
    # Just run the listener in the main thread now
    listen_to_server()

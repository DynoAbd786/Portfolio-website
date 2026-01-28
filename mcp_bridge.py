
import sys
import os
import json
import requests
import sseclient # pip install sseclient-py
import threading
import time

# CONFIGURATION
SERVER_URL = "https://mkkai.dev/api/mcp/sse"
SESSION_ID = None
POST_ENDPOINT = None

def listen_to_server():
    global SESSION_ID, POST_ENDPOINT
    
    api_key = os.environ.get("BRIDGE_API_KEY")
    headers = {'Accept': 'text/event-stream'}
    if api_key:
        headers['X-Bridge-Key'] = api_key
        sys.stderr.write(f"Using API Key: {api_key[:4]}***\n")
    else:
        sys.stderr.write("Warning: BRIDGE_API_KEY not set. Connection may be rejected.\n")
    
    try:
        response = requests.get(SERVER_URL, stream=True, headers=headers)
        client = sseclient.SSEClient(response)
        
        for event in client.events():
            if event.event == 'endpoint':
                # The server tells us where to send POST messages
                POST_ENDPOINT = event.data
                # Extract session ID from the endpoint URL if needed, or just use the endpoint
                sys.stderr.write(f"Connected! endpoint: {POST_ENDPOINT}\n")
                
            elif event.event == 'message':
                # Forward server messages to LM Studio (Stdout)
                print(event.data)
                sys.stdout.flush()
                
    except Exception as e:
        sys.stderr.write(f"Connection error: {e}\n")
        sys.exit(1)

def listen_to_client():
    # Listen to LM Studio (Stdin)
    for line in sys.stdin:
        if not line.strip():
            continue
            
        if POST_ENDPOINT:
            try:
                # Forward to server
                headers = {'Content-Type': 'application/json'}
                if os.environ.get("BRIDGE_API_KEY"):
                    headers['X-Bridge-Key'] = os.environ.get("BRIDGE_API_KEY")
                    
                requests.post(POST_ENDPOINT, data=line, headers=headers)
            except Exception as e:
                sys.stderr.write(f"Post error: {e}\n")
        else:
            sys.stderr.write("Waiting for server handshake...\n")

if __name__ == "__main__":
    # Start the SSE listener in a background thread
    t = threading.Thread(target=listen_to_server, daemon=True)
    t.start()
    
    # Listen to Stdin in the main thread
    listen_to_client()

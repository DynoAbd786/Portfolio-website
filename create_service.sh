#!/bin/bash

SERVICE_FILE="$HOME/.config/systemd/user/mcp-bridge.service"
SCRIPT_PATH="$HOME/code/personal/website/local_agent.py"
PYTHON_PATH=$(which python3)

# Ensure directory exists
mkdir -p "$HOME/.config/systemd/user"

echo "Creating service file at $SERVICE_FILE..."

cat <<EOF > "$SERVICE_FILE"
[Unit]
Description=MCP Local Agent Bridge (Ollama <-> mkkai.dev)
After=network-online.target ollama.service

[Service]
ExecStart=$PYTHON_PATH $SCRIPT_PATH --service
Restart=always
RestartSec=10
StandardOutput=journal
StandardError=journal

[Install]
WantedBy=default.target
EOF

echo "Reloading systemd daemon..."
systemctl --user daemon-reload

echo "Enabling service (auto-start on login)..."
systemctl --user enable mcp-bridge.service

echo "Service created! To start it now, run:"
echo "  systemctl --user start mcp-bridge.service"
echo "To check status:"
echo "  systemctl --user status mcp-bridge.service"
echo "To follow logs:"
echo "  journalctl --user -u mcp-bridge -f"

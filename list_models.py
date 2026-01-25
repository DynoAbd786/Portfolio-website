import requests
import os
import json

# Read key from .env manually or just assume it is in the file I wrote
key = "YOUR_API_KEY_HERE"
with open("website.api/.env", "r") as f:
    for line in f:
        if line.startswith("Gemini__ApiKey="):
            key = line.strip().split("=")[1]
            break

url = f"https://generativelanguage.googleapis.com/v1beta/models?key={key}"

response = requests.get(url)
print(response.status_code)
if response.status_code == 200:
    models = response.json().get("models", [])
    for m in models:
        if "generateContent" in m.get("supportedGenerationMethods", []):
            print(m["name"])
else:
    print(response.text)

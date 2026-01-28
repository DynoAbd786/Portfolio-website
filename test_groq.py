import os
import requests
import json
from dotenv import load_dotenv

load_dotenv("website.api/.env")
api_key = os.getenv("Groq__ApiKey")

headers = {
    "Authorization": f"Bearer {api_key}",
    "Content-Type": "application/json"
}

url = "https://api.groq.com/openai/v1/models"
print(f"Fetching models from {url}...")
try:
    response = requests.get(url, headers=headers)
    print(f"Status Code: {response.status_code}")
    print(f"Models: {json.dumps(response.json(), indent=2)}")
except Exception as e:
    print(f"Error: {e}")

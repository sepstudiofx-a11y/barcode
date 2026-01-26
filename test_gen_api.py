import requests
import json

url = 'http://localhost:9010/api/barcode/generate'
data = {
    "Chem": "UREA IIGEN",
    "BottleType": "40ml",
    "RgtType": "R1",
    "LotNumber": "013",
    "SerialNumber": "0477",
    "ExpDate": "11/30/2025"
}

try:
    print(f"Sending request to {url}...")
    print(f"Data: {json.dumps(data, indent=2)}")
    
    response = requests.post(url, json=data)
    
    print(f"\nStatus Code: {response.status_code}")
    print("Response Body:")
    try:
        print(json.dumps(response.json(), indent=2))
    except:
        print(response.text)

except Exception as e:
    print(f"Error: {e}")

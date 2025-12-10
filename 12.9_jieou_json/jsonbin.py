<<<<<<< HEAD
import requests
from datetime import datetime

JSONBIN_BIN_ID = "6938ea6743b1c97be9e37eb8"
JSONBIN_ACCESS_KEY = "$2a$10$yis0TuHfcYwNtWmWdqA4ZemJWabM7AONm7zNRu8cINNkqIs6YDC0y"

JSONBIN_URL = f"https://api.jsonbin.io/v3/b/{JSONBIN_BIN_ID}"

def save_latest_reply(text):
    data = {
        "text": text,
        "timestamp": datetime.now().isoformat(),
        "read": False
    }
    
    try:
        response = requests.put(
            JSONBIN_URL,
            json=data,
            headers={
                "X-Access-Key": JSONBIN_ACCESS_KEY,
                "Content-Type": "application/json"
            }
        )
        if response.status_code != 200:
            print(f"JSONBin Save Error: {response.status_code} - {response.text}")
        return response.status_code == 200
    except Exception as e:
        print(f"JSONBin Save Exception: {e}")
        return False

def get_latest_reply():
    try:
        response = requests.get(
            JSONBIN_URL + "/latest",
            headers={"X-Access-Key": JSONBIN_ACCESS_KEY}
        )
        if response.status_code == 200:
            data = response.json().get("record", {})
            if not data.get("read", False):
                data["read"] = True
                requests.put(
                    JSONBIN_URL,
                    json=data,
                    headers={"X-Access-Key": JSONBIN_ACCESS_KEY}
                )
                return {"has_new": True, "text": data.get("text")}
        return {"has_new": False, "text": None}
    except:
=======
import requests
from datetime import datetime

JSONBIN_BIN_ID = "693805b7d0EA881f401cbf8f"
JSONBIN_ACCESS_KEY = "$2a$10$yis0TuHfcYwNtWmWdqA4ZemJWabM7AONm7zNRu8cINNkqIs6YDC0y"

JSONBIN_URL = f"https://api.jsonbin.io/v3/b/{JSONBIN_BIN_ID}"

def save_latest_reply(text):
    data = {
        "text": text,
        "timestamp": datetime.now().isoformat(),
        "read": False
    }
    
    try:
        response = requests.put(
            JSONBIN_URL,
            json=data,
            headers={
                "X-Access-Key": JSONBIN_ACCESS_KEY,
                "Content-Type": "application/json"
            }
        )
        if response.status_code != 200:
            print(f"JSONBin Save Error: {response.status_code} - {response.text}")
        return response.status_code == 200
    except Exception as e:
        print(f"JSONBin Save Exception: {e}")
        return False

def get_latest_reply():
    try:
        response = requests.get(
            JSONBIN_URL + "/latest",
            headers={"X-Access-Key": JSONBIN_ACCESS_KEY}
        )
        if response.status_code == 200:
            data = response.json().get("record", {})
            if not data.get("read", False):
                data["read"] = True
                requests.put(
                    JSONBIN_URL,
                    json=data,
                    headers={"X-Access-Key": JSONBIN_ACCESS_KEY}
                )
                return {"has_new": True, "text": data.get("text")}
        return {"has_new": False, "text": None}
    except:
>>>>>>> 69b975d53f968ed81969eadd0d07264386cea57b
        return {"has_new": False, "text": None}
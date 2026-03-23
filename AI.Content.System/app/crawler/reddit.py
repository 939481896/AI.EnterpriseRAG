import requests
from app.config import REDDIT_URL

def fetch_reddit():
    try:
        headers = {"User-Agent": "Mozilla/5.0"}
        res = requests.get(REDDIT_URL, headers=headers, timeout=10).json()
        posts = []
        for item in res.get("data", {}).get("children", []):
            data = item["data"]
            posts.append({
                "source": "reddit",
                "title": data.get("title", ""),
                "score": data.get("score", 0)
            })
        return posts
    except Exception as e:
        print("Reddit 抓取失败:", e)
        return []
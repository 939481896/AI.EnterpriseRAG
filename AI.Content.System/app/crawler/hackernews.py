import requests
from app.config import HN_URL

def fetch_hn():
    try:
        ids = requests.get(HN_URL, timeout=10).json()[:10]
        posts = []
        for i in ids:
            item = requests.get(
                f"https://hacker-news.firebaseio.com/v0/item/{i}.json",
                timeout=10
            ).json()
            if not item:
                continue
            posts.append({
                "source": "hn",
                "title": item.get("title", ""),
                "score": item.get("score", 0)
            })
        return posts
    except Exception as e:
        print("HN 抓取失败:", e)
        return []
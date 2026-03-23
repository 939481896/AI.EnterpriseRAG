import snscrape.modules.twitter as sntwitter

def fetch_twitter():
    posts = []
    try:
        for i, tweet in enumerate(sntwitter.TwitterSearchScraper("AI langchain 最新进展 lang:en").get_items()):
            if i >= 5:
                break
            posts.append({
                "source": "twitter",
                "title": tweet.content,
                "score": tweet.likeCount
            })
    except Exception as e:
        print("Twitter 抓取失败（可能接口限制）:", e)
        # 保底返回
        return [
            {"source": "twitter", "title": "Latest AI breakthrough trending", "score": 100}
        ]
    return posts
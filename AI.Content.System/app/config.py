import os
from dotenv import load_dotenv

load_dotenv()

OPENAI_API_KEY = os.getenv("OPENAI_API_KEY")

REDDIT_URL = "https://www.reddit.com/r/artificial/hot.json?limit=10"
HN_URL = "https://hacker-news.firebaseio.com/v0/topstories.json"

DATABASE_URL = "sqlite:///./content.db"
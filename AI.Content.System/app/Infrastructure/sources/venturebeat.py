# -*- coding: utf-8 -*-
from .base import BaseSource
from app.core.logger import get_logger
from bs4 import BeautifulSoup

logger = get_logger(__name__)

class VentureBeatSource(BaseSource):
    source_name = "vb"

    def fetch_raw(self) -> list[dict]:
        url = "https://venturebeat.com/category/ai/"
        raw = []

        try:
            resp = self.session.get(url, timeout=15)
            resp.raise_for_status()
            soup = BeautifulSoup(resp.text, "html.parser")
            articles = soup.select("article.article-wrapper") or soup.select(".post-block")
            for article in articles[:self.limit]:
                title = article.get_text(strip=True)
                href = article["href"]
                raw.append({
                    "title": title,
                    "url": href,
                    "description": "",
                    "score": 0
                })
        except Exception as e:
            logger.warning(f"VentureBeat 抓取失败: {e}")

        return raw
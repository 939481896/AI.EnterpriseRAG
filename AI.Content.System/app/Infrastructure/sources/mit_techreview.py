# -*- coding: utf-8 -*-
from .base import BaseSource
from app.core.logger import get_logger
from bs4 import BeautifulSoup

logger = get_logger(__name__)

class MITTechReviewSource(BaseSource):
    source_name = "mit"

    def fetch_raw(self) -> list[dict]:
        url = "https://www.technologyreview.com/artificial-intelligence/"
        raw = []

        try:
            resp = self.session.get(url, timeout=15)
            resp.raise_for_status()
            soup = BeautifulSoup(resp.text, "html.parser")

            for article in soup.select(".teaserItem__inner")[:self.limit]:
                title_el = article.select_one("h3 a")
                desc_el = article.select_one(".teaserItem__dek")

                if title_el:
                    raw.append({
                        "title": title_el.get_text(strip=True),
                        "description": desc_el.get_text(strip=True) if desc_el else "",
                        "url": "https://www.technologyreview.com" + title_el["href"],
                        "score": 0,
                    })
        except Exception as e:
            logger.warning(f"MIT 抓取失败: {e}")

        return raw
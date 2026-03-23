import random
import time
from typing import List, Dict
from app.infrastructure.sources.base import BaseSource
from app.core.config import settings
from app.core.logger import get_logger
from app.domain.entities import ContentItem

logger = get_logger(__name__)

class RedditSource(BaseSource):
    """
    Reddit 爬虫实现（生产级）
    特性：随机延迟、自动重试、代理支持、多字段抓取、异常容错
    """
    source_name = "reddit"
    
    def __init__(self):
        super().__init__()
        # 从配置读取 Reddit 抓取 URL（替代硬编码）
        self.reddit_url = getattr(settings, "REDDIT_URL", "https://www.reddit.com/r/artificial/hot.json?limit=10")
        # 随机 User-Agent 列表
        self.user_agents = [
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36",
            "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Firefox/123.0",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) Firefox/122.0"
        ]

    def _random_delay(self):
        """随机延迟（1-3秒），防反爬"""
        delay = random.uniform(1, 3)
        logger.debug(f"{self.source_name} 随机延迟 {delay:.2f} 秒")
        time.sleep(delay)

    def fetch_raw(self) -> List[Dict]:
        """
        抓取 Reddit 原始数据（生产级实现）
        返回格式：[{"title": "", "score": 0, "comments": 0, "created": 0}, ...]
        """
        # 设置随机 User-Agent
        self.session.headers.update({
            "User-Agent": random.choice(self.user_agents),
            "Accept": "application/json",
            "Referer": "https://www.reddit.com/"
        })

        try:
            # 随机延迟
            self._random_delay()
            
            # 发送请求（支持代理、超时）
            response = self.session.get(
                self.reddit_url,
                timeout=10,
                params={"limit": self.limit}  # 从配置读取抓取数量
            )
            response.raise_for_status()  # 触发 HTTP 错误（4xx/5xx）
            data = response.json()
            
            logger.info(f"{self.source_name} 抓取成功，响应状态码: {response.status_code}")
        except Exception as e:
            logger.error(f"{self.source_name} 抓取失败: {str(e)}", exc_info=True)
            return []

        # 解析原始数据（容错处理）
        raw_posts = []
        try:
            children = data.get("data", {}).get("children", [])
            for item in children:
                post_data = item.get("data", {})
                # 过滤空标题
                if not post_data.get("title"):
                    continue
                raw_posts.append({
                    "title": post_data.get("title", "").strip(),
                    "score": post_data.get("score", 0),
                    "comments": post_data.get("num_comments", 0),
                    "created": post_data.get("created_utc", 0)
                })
        except Exception as e:
            logger.error(f"{self.source_name} 数据解析失败: {str(e)}", exc_info=True)
            return []

        logger.info(f"{self.source_name} 解析出 {len(raw_posts)} 条有效数据")
        return raw_posts

    def parse_to_entity(self, raw_data: Dict) -> ContentItem:
        """
        扩展解析逻辑：保留原始字段，同时适配领域实体
        注：额外字段（comments/created）可存储到实体扩展属性
        """
        item = ContentItem(
            source=self.source_name,
            title=raw_data.get("title", ""),
            score=raw_data.get("score", 0)
        )
        # 扩展属性（非核心字段，用于后续扩展）
        item.comments = raw_data.get("comments", 0)
        item.created = raw_data.get("created", 0)
        return item
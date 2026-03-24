# -*- coding: utf-8 -*-
import random
import time
from typing import List, Dict
from .base import BaseSource
from app.core.config import settings
from app.core.logger import get_logger
from app.domain.entities import ContentItem

logger = get_logger(__name__)

class RedditSource(BaseSource):
    """
    Reddit 生产级爬虫
    已解决：403 封锁问题、元数据（URL/摘要）缺失问题
    """
    source_name = "reddit"
    
    def __init__(self, limit: int = None):
        super().__init__(limit=limit)
        # 💡 优化：基础 URL 保持纯净，参数动态拼接
        self.base_url = "https://www.reddit.com/r/artificial/hot.json"
        
        # 💡 扩展：更真实的桌面端 User-Agent
        self.user_agents = [
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36",
            "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Version/17.0 Safari/537.36",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:123.0) Gecko/20100101 Firefox/123.0"
        ]

    def _apply_stealth_headers(self):
        """应用深度伪装请求头，减少 403 概率"""
        self.session.headers.update({
            "User-Agent": random.choice(self.user_agents),
            "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8",
            "Accept-Language": "en-US,en;q=0.5",
            "DNT": "1",
            "Connection": "keep-alive",
            "Upgrade-Insecure-Requests": "1",
            "Sec-Fetch-Dest": "document",
            "Sec-Fetch-Mode": "navigate",
            "Sec-Fetch-Site": "none",
            "Sec-Fetch-User": "?1"
        })

    def fetch_raw(self) -> List[Dict]:
        """抓取并返回字典列表"""
        self._apply_stealth_headers()
        
        params = {
            "limit": self.limit,
            "rtj": "only", # 减少数据量
            "redditWebClient": "desktop2x"
        }

        try:
            # 随机延迟，模拟真人行为
            time.sleep(random.uniform(2, 5))
            
            logger.info(f"[{self.source_name}] 正在请求 Reddit (Limit: {self.limit})...")
            response = self.session.get(
                self.base_url, 
                params=params, 
                timeout=15,
                allow_redirects=True
            )
            
            if response.status_code == 403:
                logger.error(f"[{self.source_name}] 抓取失败：403 Forbidden。代理 IP 可能被 Reddit 封禁。")
                return []
                
            response.raise_for_status()
            data = response.json()
            
            children = data.get("data", {}).get("children", [])
            raw_posts = []
            
            for item in children:
                post = item.get("data", {})
                title = post.get("title", "").strip()
                if not title:
                    continue
                
                # 💡 关键：解析出 Pipeline 需要的所有元数据
                raw_posts.append({
                    "title": title,
                    "score": post.get("score", 0),
                    "url": f"https://www.reddit.com{post.get('permalink', '')}",
                    "description": post.get("selftext", "")[:500], # 摘要
                    "comments": post.get("num_comments", 0),
                    "created": post.get("created_utc", 0)
                })
            
            logger.info(f"[{self.source_name}] 成功获取 {len(raw_posts)} 条原始数据")
            return raw_posts

        except Exception as e:
            logger.error(f"[{self.source_name}] 网络或解析异常: {str(e)}")
            return []

    def parse_to_entity(self, raw_data: Dict) -> ContentItem:
        """
        重写解析逻辑：将 Reddit 特有字段映射到通用 ContentItem
        """
        # 核心字段（Pipeline 强依赖）
        entity = ContentItem(
            source=self.source_name,
            title=raw_data.get("title", ""),
            score=raw_data.get("score", 0),
            url=raw_data.get("url"),
            description=raw_data.get("description")
        )
        
        # 扩展属性（用于后续排序逻辑）
        # 确保 ContentItem 实体类里有这些属性，或者直接跳过
        setattr(entity, "comments_count", raw_data.get("comments", 0))
        
        return entity
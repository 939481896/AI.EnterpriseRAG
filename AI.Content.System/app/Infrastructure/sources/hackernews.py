# -*- coding: utf-8 -*-
from concurrent.futures import ThreadPoolExecutor, as_completed
from .base import BaseSource
from app.core.logger import get_logger

logger = get_logger(__name__)

class HackerNewsSource(BaseSource):
    source_name = "hn"
    
    def fetch_item_detail(self, story_id: int) -> dict:
        """获取单条 HN 项目的详情（用于线程池调用）"""
        url = f"https://hacker-news.firebaseio.com/v0/item/{story_id}.json"
        try:
            # 这里的 self.session 是线程安全的（requests.Session 在多线程下表现良好）
            response = self.session.get(url, timeout=5)
            response.raise_for_status()
            data = response.json()
            
            # 只抓取类型为 'story' 且有标题的内容
            if data and data.get("type") == "story" and data.get("title"):
                return {
                    "title": data.get("title"),
                    "score": data.get("score", 0),
                    "url": data.get("url", ""), # 增加 URL 方便 AI 溯源
                    "id": story_id
                }
        except Exception as e:
            logger.warning(f"获取 HN 故事 {story_id} 详情失败: {e}")
        return None

    def fetch_raw(self) -> list[dict]:
        """抓取 HackerNews 热门内容（并发优化版）"""
        # 1. 获取 Top Stories ID 列表
        top_stories_url = "https://hacker-news.firebaseio.com/v0/topstories.json"
        try:
            response = self.session.get(top_stories_url, timeout=10)
            response.raise_for_status()
            # 根据 limit 获取前 N 个 ID
            story_ids = response.json()[:self.limit]
        except Exception as e:
            logger.error(f"无法连接 HN API: {e}")
            return []

        raw_posts = []
        logger.info(f"正在并发抓取 {len(story_ids)} 条 HN 内容详情...")

        # 2. 使用线程池并发抓取详情
        # 线程数根据 limit 动态调整，最大设为 10
        max_workers = min(len(story_ids), 10)
        
        with ThreadPoolExecutor(max_workers=max_workers) as executor:
            # 提交所有任务
            future_to_id = {executor.submit(self.fetch_item_detail, sid): sid for sid in story_ids}
            
            for future in as_completed(future_to_id):
                result = future.result()
                if result:
                    raw_posts.append(result)

        # 3. 按分数降序排列（可选）
        raw_posts.sort(key=lambda x: x["score"], reverse=True)
        
        logger.info(f"HN 抓取完成，成功获取 {len(raw_posts)} 条有效内容")
        return raw_posts
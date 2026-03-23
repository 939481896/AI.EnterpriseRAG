from .base import BaseSource
from app.core.logger import get_logger

logger = get_logger(__name__)

class HackerNewsSource(BaseSource):
    source_name = "hn"
    
    def fetch_raw(self) -> list[dict]:
        """抓取 HackerNews 热门内容"""
        # 获取 top stories ID
        top_stories_url = "https://hacker-news.firebaseio.com/v0/topstories.json"
        response = self.session.get(top_stories_url, timeout=10)
        response.raise_for_status()
        story_ids = response.json()[:self.limit]
        
        # 获取每条内容详情
        raw_posts = []
        for story_id in story_ids:
            story_url = f"https://hacker-news.firebaseio.com/v0/item/{story_id}.json"
            try:
                story_response = self.session.get(story_url, timeout=5)
                story_response.raise_for_status()
                story_data = story_response.json()
                if story_data.get("type") == "story":  # 只保留文章类型
                    raw_posts.append({
                        "title": story_data.get("title"),
                        "score": story_data.get("score", 0)
                    })
            except Exception as e:
                logger.warning(f"获取 HN 故事 {story_id} 失败: {e}")
                continue
        
        return raw_posts
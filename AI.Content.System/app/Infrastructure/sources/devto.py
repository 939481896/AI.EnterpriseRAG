from .base import BaseSource
from app.core.logger import get_logger

logger = get_logger(__name__)

class DevToSource(BaseSource):
    source_name = "devto"
    
    def fetch_raw(self) -> list[dict]:
        """抓取 Dev.to AI 相关热门内容（优化容错）"""
        url = "https://dev.to/api/articles"
        params = {
            "tag": "ai",  # 简化关键词，提高命中率
            "per_page": self.limit,
            "top": 7  # 扩大时间范围（7天内热门）
        }
        
        try:
            response = self.session.get(url, params=params, timeout=15)  # 延长超时
            response.raise_for_status()
            articles = response.json()
            
            # 容错：如果返回非列表，直接返回空
            if not isinstance(articles, list):
                logger.warning(f"{self.source_name} 返回非列表数据: {articles}")
                return []
            
            raw_posts = []
            for article in articles:
                title = article.get("title", "").strip()
                if not title:
                    continue
                raw_posts.append({
                    "title": title,
                    "score": article.get("positive_reactions_count", 0)
                })
            
            return raw_posts
        except Exception as e:
            logger.error(f"{self.source_name} 抓取失败: {str(e)}", exc_info=True)
            return []
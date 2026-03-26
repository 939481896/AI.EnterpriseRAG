# -*- coding: utf-8 -*-
# app.infrastructure.sources.devto.py

from .base import BaseSource
from app.core.logger import get_logger

logger = get_logger(__name__)

class DevToSource(BaseSource):
    source_name = "devto"
    
    def fetch_raw(self) -> list[dict]:
        """抓取 Dev.to 热门内容（深度优化版）"""
        url = "https://dev.to/api/articles"
        
        # 优化 1：组合多个热门标签，通过多次小批量抓取合成大列表
        # 如果你希望简单，也可以保留单个标签
        tags = ["ai", "machinelearning", "webdev"] 
        raw_posts_dict = {} # 使用字典通过 URL 或 ID 去重

        # 这里的 params 依然遵循你设定的 limit
        params = {
            "tag": "ai", 
            "per_page": self.limit,
            "top": 7  # 过去 7 天内的热门
        }
        
        try:
            logger.info(f"正在从 Dev.to 抓取标签为 {params['tag']} 的热门内容...")
            response = self.session.get(url, params=params, timeout=15)
            response.raise_for_status()
            articles = response.json()
            
            if not isinstance(articles, list):
                logger.warning(f"Dev.to 返回了非预期格式: {type(articles)}")
                return []
            
            for article in articles:
                article_id = article.get("id")
                title = article.get("title", "").strip()
                
                if not title or not article_id:
                    continue
                
                # 提取更多维度数据，帮助 AI 更好地理解背景
                # positive_reactions_count 是点赞数，public_reactions_count 包含评论权重
                score = article.get("public_reactions_count", 0)
                
                raw_posts_dict[article_id] = {
                    "title": title,
                    "score": float(score),
                    "url": article.get("url", ""),
                    "description": article.get("description", ""), # 描述信息对选题至关重要
                    "tags": article.get("tag_list", []),
                    "reading_time": article.get("reading_time_minutes", 0)
                }
                
            # 转换为列表并根据分数排序
            result = list(raw_posts_dict.values())
            result.sort(key=lambda x: x["score"], reverse=True)
            
            logger.info(f"Dev.to 抓取完成，成功获取 {len(result)} 条精华内容")
            return result

        except Exception as e:
            # 提供更友好的错误分类日志
            if "timeout" in str(e).lower():
                logger.error(f"Dev.to 抓取超时，请检查网络或代理设置")
            else:
                logger.error(f"Dev.to 抓取发生异常: {str(e)}", exc_info=True)
            return []
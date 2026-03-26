# -*- coding: utf-8 -*-
from typing import List, Optional
from unicodedata import category
from sqlalchemy.orm import Session
from app.domain.entities import GeneratedContent
from app.infrastructure.persistence.models import ContentModel
from app.core.logger import get_logger

logger = get_logger(__name__)

class ContentRepository:
    """内容仓库：负责领域实体与数据库模型之间的转换与存储"""
    
    def __init__(self, db: Session):
        self.db = db

    def _exists(self, title: str) -> bool:
        """内部方法：检查标题是否已存在"""
        return self.db.query(ContentModel).filter(ContentModel.original_title == title).first() is not None

    def save(self, content: GeneratedContent) -> Optional[ContentModel]:
        """保存单条内容（带去重检查）"""
        try:
            if self._exists(content.original_title):
                logger.debug(f"跳过重复内容: {content.original_title[:20]}...")
                return None

            db_model = ContentModel(
                source=content.source,
                original_title=content.original_title,
                url=getattr(content, 'url', None),  # 兼容处理
                topic=content.topic,
                script=content.script,
                score=content.score
            )
            self.db.add(db_model)
            self.db.commit()
            self.db.refresh(db_model)
            return db_model
        except Exception as e:
            self.db.rollback()
            logger.error(f"保存内容失败: {e}")
            return None

    def bulk_save(self, contents: List[GeneratedContent]) -> int:
        """批量保存新内容"""
        if not contents:
            return 0
            
        new_models = []
        try:
            for c in contents:
                if not self._exists(c.original_title):
                    new_models.append(ContentModel(
                        source=c.source,
                        original_title=c.original_title,
                        url=getattr(c, 'url', None),
                        topic=c.topic,
                        category=c.category,
                        description=c.description,
                        script=c.script,
                        score=c.score
                    ))
            
            if not new_models:
                logger.info("没有新内容需要入库")
                return 0
                
            self.db.add_all(new_models)
            self.db.commit()
            count = len(new_models)
            logger.info(f"成功入库 {count} 条新内容 (跳过 {len(contents) - count} 条重复)")
            return count
        except Exception as e:
            self.db.rollback()
            logger.error(f"批量保存失败: {e}")
            return 0

    def get_by_source(self, source: str, limit: int = 10) -> List[ContentModel]:
        """按来源获取最新的高分内容"""
        return self.db.query(ContentModel).filter(
            1==1
            # ContentModel.source == source
        ).order_by(ContentModel.created_at.desc()).limit(limit).all()

    def get_by_id(self, content_id: int) -> Optional[ContentModel]:
        """根据 ID 获取单条内容的完整信息"""
        try:
            return self.db.query(ContentModel).filter(ContentModel.id == content_id).first()
        except Exception as e:
            logger.error(f"查询 ID 为 {content_id} 的内容失败: {e}")
            return None
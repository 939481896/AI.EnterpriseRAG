# app/infrastructure/persistence/repository.py
from sqlalchemy.orm import Session
from app.domain.entities import GeneratedContent
# 导入 models 中的 ContentModel（路径正确）
from app.infrastructure.persistence.models import ContentModel
from app.core.logger import get_logger

logger = get_logger(__name__)

class ContentRepository:
    """内容仓库（封装数据库操作）"""
    def __init__(self, db: Session):
        self.db = db

    def save(self, content: GeneratedContent) -> ContentModel:
        """保存单条生成内容到数据库"""
        db_model = ContentModel(
            source=content.source,
            original_title=content.original_title,
            topic=content.topic,
            script=content.script,
            score=content.score
        )
        self.db.add(db_model)
        self.db.commit()
        self.db.refresh(db_model)
        logger.debug(f"保存内容 ID: {db_model.id}, 选题: {content.topic[:20]}...")
        return db_model

    def bulk_save(self, contents: list[GeneratedContent]) -> int:
        """批量保存（提升性能）"""
        db_models = [
            ContentModel(
                source=c.source,
                original_title=c.original_title,
                topic=c.topic,
                script=c.script,
                score=c.score
            ) for c in contents
        ]
        self.db.add_all(db_models)
        self.db.commit()
        count = len(db_models)
        logger.info(f"批量保存 {count} 条生成内容")
        return count

    def get_by_source(self, source: str, limit: int = 10) -> list[ContentModel]:
        """按数据源查询生成内容"""
        return self.db.query(ContentModel).filter(
            ContentModel.source == source
        ).order_by(ContentModel.score.desc()).limit(limit).all()
from abc import ABC, abstractmethod
from typing import List
from app.domain.entities import ContentItem
from app.core.config import settings
from app.core.logger import get_logger
import requests
from requests.adapters import HTTPAdapter
from urllib3.util.retry import Retry

logger = get_logger(__name__)

class BaseSource(ABC):
    """爬虫基类（定义统一接口）"""
    source_name: str = "base"  # 子类需覆盖
    
    def __init__(self):
        self.session = self._create_session()
        self.limit = settings.CRAWL_LIMIT

    def _create_session(self) -> requests.Session:
        """创建带重试和代理的请求会话（仅用于爬虫，不影响 OpenAI）"""
        # 重试策略
        retry_strategy = Retry(
            total=3,
            backoff_factor=1,
            status_forcelist=[429, 500, 502, 503, 504],
            allowed_methods=["GET"]
        )
        adapter = HTTPAdapter(max_retries=retry_strategy)
        
        session = requests.Session()
        session.mount("https://", adapter)
        session.mount("http://", adapter)
        
        # 代理配置（仅爬虫使用）
        if settings.PROXY_ENABLE and settings.PROXY_URL:
            session.proxies = {
                "http": settings.PROXY_URL,
                "https": settings.PROXY_URL
            }
            logger.info(f"{self.source_name} 启用代理: {settings.PROXY_URL}")
        
        # 默认请求头
        session.headers.update({
            "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/122.0.0.0 Safari/537.36",
            "Accept": "application/json, text/plain, */*",
            "Accept-Language": "en-US,en;q=0.9"
        })
        
        return session

    @abstractmethod
    def fetch_raw(self) -> list[dict]:
        """抓取原始数据（子类实现）"""
        pass

    def parse_to_entity(self, raw_data: dict) -> ContentItem:
        """解析原始数据为领域实体（子类可覆盖）"""
        return ContentItem(
            source=self.source_name,
            title=raw_data.get("title", "").strip(),
            score=raw_data.get("score", 0)
        )

    def fetch(self) -> List[ContentItem]:
        """对外统一接口：抓取并转换为领域实体"""
        try:
            logger.info(f"开始抓取 {self.source_name} 数据（限制 {self.limit} 条）")
            raw_data_list = self.fetch_raw()
            entities = [self.parse_to_entity(data) for data in raw_data_list if data.get("title")]
            
            # 计算排名分
            from app.domain.services import calculate_rank_score
            for entity in entities:
                entity.rank_score = calculate_rank_score(entity)
                
            logger.info(f"{self.source_name} 抓取完成，共 {len(entities)} 条有效数据")
            return entities
        
        except Exception as e:
            logger.error(f"{self.source_name} 抓取失败: {str(e)}", exc_info=True)
            return []
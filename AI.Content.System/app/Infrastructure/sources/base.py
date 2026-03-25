# -*- coding: utf-8 -*-
from abc import ABC, abstractmethod
from typing import List, Optional
from app.domain.entities import ContentItem
from app.core.config import settings
from app.core.logger import get_logger
import requests
from requests.adapters import HTTPAdapter
from urllib3.util.retry import Retry

logger = get_logger(__name__)

class BaseSource(ABC):
    """爬虫基类（定义统一接口）"""
    source_name: str = "base"
    
    # 增加 limit 参数接收，并设置默认值
    def __init__(self, limit: Optional[int] = None):
        self.session = self._create_session()
        # 优先使用传入的 limit，否则使用配置文件里的默认值
        self.limit = limit or settings.CRAWL_LIMIT

    def _create_session(self) -> requests.Session:
        """创建带重试和代理的请求会话"""
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
        
        if settings.PROXY_ENABLE and settings.PROXY_URL:
            session.proxies = {
                "http": settings.PROXY_URL,
                "https": settings.PROXY_URL
            }
            # 注意：此处 logger 可能会在子类未完全初始化时调用，使用 self.source_name 是安全的
            logger.info(f"[{self.source_name}] 爬虫已配置代理: {settings.PROXY_URL}")
        
        session.headers.update({
            "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/122.0.0.0 Safari/537.36",
            "Accept": "application/json",
        })
        return session

    @abstractmethod
    def fetch_raw(self) -> list[dict]:
        """抓取原始数据（子类实现）"""
        pass

    # 支持将抓取到的 url 和 description 转换到实体中
    def parse_to_entity(self, raw_data: dict) -> ContentItem:
        """解析原始数据为领域实体"""
        return ContentItem(
            source=self.source_name,
            title=raw_data.get("title", "").strip(),
            score=raw_data.get("score", 0),
            url=raw_data.get("url"),             # 传递 URL
            description=raw_data.get("description") # 传递描述
        )

    def fetch(self) -> List[ContentItem]:
        """对外统一接口：抓取并转换为领域实体"""
        try:
            logger.info(f"正在从 {self.source_name} 采集数据 (Limit: {self.limit})...")
            raw_data_list = self.fetch_raw()
            
            # 过滤掉没有标题的垃圾数据
            entities = [
                self.parse_to_entity(data) 
                for data in raw_data_list 
                if data and data.get("title")
            ]
            
            # 导入并计算排名分
            from app.domain.services import calculate_rank_score
            for entity in entities:
                entity.rank_score = calculate_rank_score(entity)
                
            logger.info(f"[{self.source_name}] 采集完成，获取到 {len(entities)} 条合格实体")
            return entities
        
        except Exception as e:
            logger.error(f"[{self.source_name}] 运行链路异常: {str(e)}", exc_info=True)
            return []
# app.infrastructure.web_extractor.py
import httpx
from app.core.logger import get_logger

logger = get_logger(__name__)

class WebExtractor:
    def __init__(self, timeout: int = 20):
        self.timeout = timeout
        # 使用 Jina Reader 免费接口 (也可以部署私有版)
        self.base_url = "https://r.jina.ai/"

    def get_main_content(self, url: str) -> str:
        """将 URL 转换为纯净的 Markdown 文本"""
        if not url or not url.startswith("http"):
            return ""
        
        try:
            target_url = f"{self.base_url}{url}"
            # Jina Reader 专门为 LLM 优化，过滤了导航栏和广告
            with httpx.Client(timeout=self.timeout) as client:
                response = client.get(target_url)
                response.raise_for_status()
                # 限制长度，防止 Token 溢出 (约 4000 字符足够总结)
                content = response.text[:6000]
                logger.info(f"成功提取正文，长度: {len(content)}")
                return content
        except Exception as e:
            logger.error(f"提取网页正文失败 [{url}]: {e}")
            return ""
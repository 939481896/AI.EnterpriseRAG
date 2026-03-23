from openai import OpenAI, APIError, RateLimitError
import httpx
from app.core.config import settings
from app.core.logger import get_logger

logger = get_logger(__name__)

class LLMClient:
    """LLM 客户端（统一 AI 调用接口，修复代理参数错误）"""
    def __init__(self):
        # 1. 配置 httpx 客户端（支持代理）
        httpx_client = None
        if settings.PROXY_ENABLE and settings.PROXY_URL:
            try:
                httpx_client = httpx.Client(
                    proxy=settings.PROXY_URL
                )
                logger.info(f"OpenAI 客户端启用代理: {settings.PROXY_URL}")
            except Exception as e:
                logger.warning(f"OpenAI 代理配置失败，使用默认客户端: {e}")
        
        # 2. 初始化 OpenAI 客户端（修复 proxies 参数错误）
        try:
            self.client = OpenAI(
                api_key=settings.OPENAI_API_KEY,
                http_client=httpx_client  # 使用手动配置的 httpx 客户端
            )
            self.model = settings.OPENAI_MODEL
        except Exception as e:
            logger.error(f"OpenAI 客户端初始化失败: {e}", exc_info=True)
            raise

    def generate_topic(self, original_title: str) -> list[str]:
        """基于原始标题生成 5 个短视频选题"""
        prompt = f"""
基于以下技术内容，生成5个短视频爆款选题（中文）：
{original_title}

要求：
1. 标题吸引人，有钩子
2. 符合短视频传播规律
3. 聚焦 AI/技术 领域
4. 每个选题单独一行
"""
        try:
            response = self.client.chat.completions.create(
                model=self.model,
                messages=[{"role": "user", "content": prompt}],
                temperature=0.8,
                timeout=30
            )
            # 解析结果，过滤空行
            topics = [t.strip() for t in response.choices[0].message.content.split("\n") if t.strip()]
            logger.info(f"为[{original_title}]生成 {len(topics)} 个选题")
            return topics[:5]  # 确保最多 5 个
        except RateLimitError:
            logger.error("OpenAI 速率限制超限")
            return []
        except APIError as e:
            logger.error(f"OpenAI API 错误: {e}")
            return []
        except Exception as e:
            logger.error(f"选题生成失败: {e}", exc_info=True)
            return []

    def generate_script(self, topic: str) -> str:
        """基于选题生成短视频脚本"""
        prompt = f"""
写一个短视频脚本（中文）：
主题：{topic}

要求：
1. 时长 30-60 秒
2. 口语化，有节奏，有钩子
3. 开头吸引注意力，中间讲核心，结尾有互动/总结
4. 符合短视频平台传播规律
"""
        try:
            response = self.client.chat.completions.create(
                model=self.model,
                messages=[{"role": "user", "content": prompt}],
                temperature=0.9,
                timeout=30
            )
            script = response.choices[0].message.content.strip()
            logger.info(f"为选题[{topic}]生成脚本（长度：{len(script)}）")
            return script
        except Exception as e:
            logger.error(f"脚本生成失败: {e}", exc_info=True)
            return f"脚本生成失败：{str(e)}"
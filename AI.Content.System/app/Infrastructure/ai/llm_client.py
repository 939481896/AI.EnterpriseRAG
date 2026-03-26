# -*- coding: utf-8 -*-
import os
import time
import httpx
import google.generativeai as genai
from openai import OpenAI
from app.core.config import settings
from app.core.logger import get_logger

logger = get_logger(__name__)

class LLMClient:
    """
    统一 LLM 客户端：支持火山方舟 (DeepSeek/Doubao) 接入
    特性：元数据感知、自动指数级退避、多模型接入点切换
    """
    def __init__(self):
        self.provider = settings.LLM_PROVIDER.lower()
        self.httpx_client = self._setup_httpx_client()
        
        # 对应火山方舟的不同接入点 ID (需在 .env 中配置)
        self.topic_model = getattr(settings, "VOLC_TOPIC_ENDPOINT", settings.VOLC_ENDPOINT_ID)
        self.script_model = getattr(settings, "VOLC_SCRIPT_ENDPOINT", settings.VOLC_ENDPOINT_ID)
        
        if self.provider == "gemini":
            self._init_gemini()
        elif self.provider == "volcengine":
            self._init_volcengine()
        elif self.provider == "deepseek":
            self._init_deepseek()
        else:
            self._init_openai()

    def _setup_httpx_client(self) -> httpx.Client:
        """配置支持代理的 httpx 客户端"""
        proxies = settings.PROXY_URL if settings.PROXY_ENABLE else None
        return httpx.Client(proxy=proxies, timeout=60.0)
    def _setup_no_proxy_client(self) -> httpx.Client:
        """创建一个纯净的、不带代理的 httpx 客户端（用于国内 API）"""
        return httpx.Client(timeout=60.0) # 不传入 proxy 参数

    def _init_volcengine(self):
        """初始化火山方舟 (兼容 OpenAI 格式)"""
        # 火山方舟国内访问通常不需要代理，但在 httpx_client 中已处理逻辑
        direct_client = self._setup_no_proxy_client()
        self.client = OpenAI(
            api_key=settings.VOLC_API_KEY,
            base_url="https://ark.cn-beijing.volces.com/api/v3",
            http_client=direct_client
        )
        logger.info(f"火山方舟客户端就绪，选题模型: {self.topic_model}，脚本模型: {self.script_model}")

    def _init_openai(self):
        self.client = OpenAI(api_key=settings.OPENAI_API_KEY, http_client=self.httpx_client)
        self.model = settings.OPENAI_MODEL or "gpt-4o-mini"
        logger.info(f"OpenAI 客户端就绪: {self.model}")

    def _init_deepseek(self):
        self.client = OpenAI(
            api_key=settings.DEEPSEEK_API_KEY,
            base_url="https://api.deepseek.com",
            http_client=self.httpx_client
        )
        self.model = settings.DEEPSEEK_MODEL or "deepseek-chat"
        logger.info(f"DeepSeek 官方客户端就绪: {self.model}")

    def _init_gemini(self):
        if settings.PROXY_ENABLE and settings.PROXY_URL:
            for env_key in ["HTTP_PROXY", "HTTPS_PROXY", "http_proxy", "https_proxy"]:
                os.environ[env_key] = settings.PROXY_URL
        genai.configure(api_key=settings.GEMINI_API_KEY)
        self.model_name = settings.GEMINI_MODEL or "gemini-2.0-flash"
        self.client = genai.GenerativeModel(self.model_name)
        logger.info(f"Gemini 客户端就绪: {self.model_name}")

    def _ask_llm_with_retry(self, prompt: str, temperature: float, model_override: str = None, max_retries: int = 2) -> str:
        """核心执行逻辑：支持火山方舟多模型切换与 429 退避"""
        # 如果是火山方舟，使用传入的接入点 ID，否则使用默认 model
        current_model = model_override if model_override else getattr(self, "model", None)

        for attempt in range(max_retries + 1):
            try:
                if self.provider == "gemini":
                    response = self.client.generate_content(
                        prompt,
                        generation_config={"temperature": temperature},
                        request_options={"timeout": 40}
                    )
                    return response.text
                else:
                    response = self.client.chat.completions.create(
                        model=current_model,
                        messages=[{"role": "user", "content": prompt}],
                        temperature=temperature
                    )
                    return response.choices[0].message.content
            except Exception as e:
                # 针对频率限制进行自动指数级退避 (火山方舟也会有频率限制)
                if "429" in str(e) and attempt < max_retries:
                    wait_time = (attempt + 1) * 15 
                    logger.warning(f"检测到频率限制 (429)，将在 {wait_time} 秒后重试... ({attempt + 1}/{max_retries})")
                    time.sleep(wait_time)
                    continue
                
                logger.error(f"LLM [{self.provider}] 调用异常: {e}")
                raise

    def generate_topic(self, title: str, description: str = "") -> str:
        """
        【通用爆款标题生成】
        适配流程：先生标题 → 后分类 → 后生脚本
        全赛道通用 | 合规安全 | 高点击率
        """
        logger.info(f"正在生成选题: {title[:30]}...")
    
        context = f"标题：{title}\n内容摘要：{description}" if description else f"标题：{title}"
    
        # 全赛道通用 · 合规 · 爆款标题提示词
        prompt = (
            f"你是千万粉丝短视频爆款导师，请根据以下内容，提炼1个优质短视频标题。\n\n"
            f"{context}\n\n"
            f"【严格规则】\n"
            f"1. 20字以内，只返回1个标题，无任何解释\n"
            f"2. 使用爆款风格：认知反差、痛点钩子、利益吸引、情绪共鸣\n"
            f"3. 严禁违禁词：最、第一、顶级、唯一、完美、100%、暴利、赚钱、稳赚、暴富、躺赚、零风险、必赚、保过、速成\n"
            f"4. 风格通俗、扎心、接地气、不说教、不装逼、不夸大\n"
            f"5. 适配全赛道（职场/财经/生活/教育/AI/商业），中性合规\n"
        )
    
        try:
            content = self._ask_llm_with_retry(
                prompt, 
                temperature=0.75,
                model_override=self.topic_model
            )
            # 彻底清洗干扰字符
            return content.strip().replace('"', '').replace("'", "").replace('选题：', '').replace('标题：', '').strip()
        except Exception:
            return ""

    def generate_script(self, topic: str,category:str) -> str:
        """为选题生成脚本 - 优化时长、排版、避坑与标签"""
        logger.info(f"正在生成脚本: {topic[:20]}...")
        
        # 获取提示词
        prompt = self._get_script_prompt(topic,category)
        
        try:
            # 建议使用 豆包 Pro 接入点，它的口语化程度非常高
            return self._ask_llm_with_retry(
                prompt, 
                temperature=0.85, 
                model_override=self.script_model
            )
        except Exception as e:
            return f"脚本生成失败: {e}"

    def classify_topic(self, title: str, description: str = "") -> str:
        """
        自动识别内容赛道
        返回：ai_tech / workplace / finance / life / education / business
        """
        content = f"标题：{title}\n描述：{description}"

        prompt = """
            请对以下内容进行分类，只返回分类名称，不要返回任何其他文字。

            可选分类：
            ai_tech（AI、科技、编程、开发、工具）
            workplace（职场、效率、办公、沟通、管理）
            finance（理财、财经、经济、投资）
            life（生活、好物、家居、技巧）
            education（学习、知识、教育、思维）
            business（创业、商业、商业模式、营销）

            内容：
            {}

            返回（只返回分类词）：
            """.strip().format(content)

        try:
            category = self._ask_llm_with_retry(prompt, temperature=0.1).strip().lower()
            valid_categories = ["ai_tech", "workplace", "finance", "life", "education", "business"]
            return category if category in valid_categories else "ai_tech"
        except Exception as e:
            logger.warning(f"赛道识别失败: {e}")
            return "ai_tech"

    def _get_script_prompt(self, topic: str,description:str, category: str = "ai_tech") -> str:
        """
        完整版：根据不同赛道，返回高质量、结构完整、可直接拍摄的短视频脚本提示词
        包含：镜头、台词、语气、7段式结构、完整格式
        """

        # ====================== AI / 科技 赛道（你的主赛道，最完整）======================
        if category == "ai_tech":
            return (
                f"你是顶级抖音/快手科技短视频编剧，专门写AI、科技、编程、数码类爆款脚本。\n"
                f"请结合选题『{topic}』和素材『{description}』创作一条 90-120 秒、内容丰满、高完播、高互动的口播脚本。\n\n"
                "--- 硬性规则 ---\n"
                "• 时长：90-120秒（500-700字）\n"
                "• 风格：极度口语化、像朋友聊天、不装逼、不讲晦涩术语，小白秒懂\n"
                "• 违禁词：最、第一、顶级、唯一、暴利、赚钱、国家级、100%、取代人类、万能、强制、无敌、颠覆\n"
                "• 必须基于提供的素材创作，不凭空编造、不夸大技术效果\n"
                "• 内容必须有场景、有应用、有画面感，避免抽象讲解\n\n"
                "--- 输出格式（必须严格遵守）---\n\n"
                "【视频基础信息】\n"
                "• 时长：90-120秒\n"
                "• 建议BGM：科技轻快 / 深邃卡点 / 热门纯音乐\n"
                "• 热门标签：#AI #科技资讯 #人工智能 #干货分享\n\n"
                "【分镜脚本（逐句可拍）】\n"
                "（格式：镜头画面 | 台词 | 语气/节奏）\n\n"
                "1. 【黄金3秒】\n"
                "镜头：博主凑近镜头，表情惊讶/严肃\n"
                "台词：\n"
                "语气：快、抓耳\n\n"
                "2. 【认知反差/打破误区】\n"
                "镜头：切素材/PPT/演示画面\n"
                "台词：\n"
                "语气：解惑、点醒\n\n"
                "3. 【核心干货 1】\n"
                "镜头：正面讲解\n"
                "台词：\n"
                "语气：清晰、肯定\n\n"
                "4. 【核心干货 2】\n"
                "镜头：演示/特写\n"
                "台词：\n"
                "语气：强调重点\n\n"
                "5. 【核心干货 3】\n"
                "镜头：手势强调\n"
                "台词：\n"
                "语气：真诚、实用\n\n"
                "6. 【总结强化】\n"
                "镜头：快速混剪\n"
                "台词：\n"
                "语气：轻松、有说服力\n\n"
                "7. 【高互动结尾】\n"
                "镜头：微笑看镜头\n"
                "台词：用选择题互动，不要求赞\n"
                "语气：亲切、引导评论\n\n"
                "【拍摄建议】\n"
                "• 3条最关键画面建议\n\n"
                "--- 强制要求 ---\n"
                "1. 每段必须有镜头画面，画面要具体、可拍摄、不抽象。\n"
                "2. 台词必须口语化，带停顿、情绪、自然节奏。\n"
                "3. 内容必须丰满、有细节、有逻辑、有记忆点。\n"
                "4. 必须满足 1-2 分钟时长，不能太短、不能干瘪。\n"
                "5. 不要任何多余解释，直接输出标准格式内容。\n"
                "6. 必须结合素材内容创作，不脱离主题、不凭空捏造。"
            )
        # ====================== 职场 / 效率 赛道 ======================
        elif category == "workplace":
            return (
                f"你是职场爆款短视频编剧，写高效、实用、不鸡汤的职场干货脚本。\n"
                f"结合选题『{topic}』和素材『{description}』创作90-120秒口播脚本。\n\n"
                "--- 硬性规则 ---\n"
                "• 时长：90-120秒（500-700字）\n"
                "• 风格：极度口语化、像朋友聊天、干练接地气、不啰嗦\n"
                "• 违禁词：最、第一、顶级、暴利、赚钱、100%、必赚、国家级\n"
                "• 内容具体可落地，不空谈，场景化、有画面感\n\n"
                "--- 输出格式（严格遵守）---\n\n"
                "【视频基础信息】\n"
                "• 时长：90-120秒\n"
                "• 建议BGM：轻快职场、节奏清晰纯音乐\n"
                "• 热门标签：#职场干货 #效率提升 #自我提升 #职场思维\n\n"
                "【分镜脚本（逐句可拍）】\n"
                "（格式：镜头画面 | 台词 | 语气/节奏）\n\n"
                "1. 【黄金3秒】\n"
                "镜头：博主凑近镜头，表情严肃/无奈\n"
                "台词：\n"
                "语气：快、抓耳、扎心\n\n"
                "2. 【痛点共鸣/认知反差】\n"
                "镜头：办公场景/手势强调\n"
                "台词：\n"
                "语气：吐槽、点醒、共鸣\n\n"
                "3. 【干货1】\n"
                "镜头：正面讲解\n"
                "台词：\n"
                "语气：清晰、肯定、实用\n\n"
                "4. 【干货2】\n"
                "镜头：步骤演示/文字特写\n"
                "台词：\n"
                "语气：放慢、强调重点\n\n"
                "5. 【干货3】\n"
                "镜头：手势强调\n"
                "台词：\n"
                "语气：干脆、好记\n\n"
                "6. 【总结强化】\n"
                "镜头：快速混剪\n"
                "台词：\n"
                "语气：有力、有说服力\n\n"
                "7. 【高互动结尾】\n"
                "镜头：微笑看镜头\n"
                "台词：用选择题互动，不硬求赞\n"
                "语气：亲切、引导评论\n\n"
                "【拍摄建议】3条关键画面\n\n"
                "--- 强制要求 ---\n"
                "1. 画面具体可拍摄，不抽象。\n"
                "2. 台词口语化，带停顿与情绪。\n"
                "3. 内容丰满有细节，不空洞。\n"
                "4. 严格控制时长，不短不水。\n"
                "5. 只输出格式内容，无多余解释。"
            )

        # ====================== 财经 / 理财 赛道 ======================
        elif category == "finance":
            return (
                f"你是合规财经科普编剧，理性通俗、不焦虑、不荐股。\n"
                f"结合选题『{topic}』和素材『{description}』创作90-120秒口播脚本。\n\n"
                "--- 硬性规则 ---\n"
                "• 时长：90-120秒（500-700字）\n"
                "• 风格：口语化、通俗、冷静理性，不制造焦虑\n"
                "• 严禁：荐股、稳赚、翻倍、躺赚、零风险、保本、暴富、收益承诺\n"
                "• 违禁词：最、第一、顶级、必涨、必赚、100%、抄底、上车、闭眼入\n"
                "• 结尾必须加：投资有风险，入市需谨慎\n\n"
                "--- 输出格式（严格遵守）---\n\n"
                "【视频基础信息】\n"
                "• 时长：90-120秒\n"
                "• 建议BGM：沉稳财经、舒缓商务纯音乐\n"
                "• 热门标签：#理财科普 #经济常识 #财商思维 #财经知识\n\n"
                "【分镜脚本（逐句可拍）】\n"
                "（格式：镜头画面 | 台词 | 语气/节奏）\n\n"
                "1. 【黄金3秒】\n"
                "镜头：博主正视镜头，沉稳开场\n"
                "台词：\n"
                "语气：快、抓重点、不拖沓\n\n"
                "2. 【现象/误区解读】\n"
                "镜头：数据图表/手势分析\n"
                "台词：\n"
                "语气：解惑、客观、点透\n\n"
                "3. 【知识点1】\n"
                "镜头：正面讲解\n"
                "台词：\n"
                "语气：清晰、通俗\n\n"
                "4. 【知识点2】\n"
                "镜头：类比画面/示意图\n"
                "台词：\n"
                "语气：放慢、强调\n\n"
                "5. 【知识点3】\n"
                "镜头：总结手势\n"
                "台词：\n"
                "语气：务实、易懂\n\n"
                "6. 【总结+风险提示】\n"
                "镜头：庄重正面\n"
                "台词：必须包含风险提示\n"
                "语气：稳重、提醒\n\n"
                "7. 【高互动结尾】\n"
                "镜头：自然看镜头\n"
                "台词：用选择题互动\n"
                "语气：平和、引导评论\n\n"
                "【拍摄建议】3条关键画面\n\n"
                "--- 强制要求 ---\n"
                "1. 画面具体可拍摄。\n"
                "2. 台词口语化、无专业黑话。\n"
                "3. 绝不提供具体投资建议。\n"
                "4. 时长饱满，不水内容。\n"
                "5. 只输出格式内容。"
            )

        # ====================== 生活 / 好物 赛道 ======================
        elif category == "life":
            return (
                f"你是生活好物分享编剧，亲切真实、像闺蜜分享，不夸大。\n"
                f"结合选题『{topic}』和素材『{description}』创作90-120秒口播脚本。\n\n"
                "--- 硬性规则 ---\n"
                "• 时长：90-120秒（500-700字）\n"
                "• 风格：轻松自然、真实接地气、不广告感\n"
                "• 违禁词：最、神器、顶级、万能、专治、100%、绝了、必备\n"
                "• 只做体验分享，不夸大功效、不涉医疗宣传\n\n"
                "--- 输出格式（严格遵守）---\n\n"
                "【视频基础信息】\n"
                "• 时长：90-120秒\n"
                "• 建议BGM：轻松治愈、轻快生活向\n"
                "• 热门标签：#生活好物 #居家技巧 #提升幸福感 #生活小妙招\n\n"
                "【分镜脚本（逐句可拍）】\n"
                "（格式：镜头画面 | 台词 | 语气/节奏）\n\n"
                "1. 【黄金3秒】\n"
                "镜头：博主惊讶表情/展示场景痛点\n"
                "台词：\n"
                "语气：轻快、抓耳\n\n"
                "2. 【日常痛点】\n"
                "镜头：生活场景实拍\n"
                "台词：\n"
                "语气：吐槽、共鸣\n\n"
                "3. 【好物/技巧展示】\n"
                "镜头：产品特写/操作演示\n"
                "台词：\n"
                "语气：自然、真实\n\n"
                "4. 【用法细节1】\n"
                "镜头：近景操作\n"
                "台词：\n"
                "语气：清晰、细致\n\n"
                "5. 【用法细节2】\n"
                "镜头：使用效果展示\n"
                "台词：\n"
                "语气：真诚、不夸张\n\n"
                "6. 【真实感受总结】\n"
                "镜头：博主正面总结\n"
                "台词：\n"
                "语气：温和、实在\n\n"
                "7. 【高互动结尾】\n"
                "镜头：微笑看向镜头\n"
                "台词：用选择题互动\n"
                "语气：亲切、轻松\n\n"
                "【拍摄建议】3条关键画面\n\n"
                "--- 强制要求 ---\n"
                "1. 画面真实可拍，贴近生活。\n"
                "2. 台词像聊天，不生硬。\n"
                "3. 不夸大、不虚假宣传。\n"
                "4. 时长饱满，内容丰富。\n"
                "5. 只输出格式内容。"
            )

        # ====================== 教育 / 学习 赛道 ======================
        elif category == "education":
            return (
                f"你是学习类干货编剧，清晰耐心、干货密集、不制造焦虑。\n"
                f"结合选题『{topic}』和素材『{description}』创作90-120秒口播脚本。\n\n"
                "--- 硬性规则 ---\n"
                "• 时长：90-120秒（500-700字）\n"
                "• 风格：口语化、清晰易懂、务实有用\n"
                "• 违禁词：最、秒杀、速成、100%掌握、天才、必过、保过、秒会、逆袭\n"
                "• 不承诺提分、不制造焦虑，只讲方法\n\n"
                "--- 输出格式（严格遵守）---\n\n"
                "【视频基础信息】\n"
                "• 时长：90-120秒\n"
                "• 建议BGM：安静专注、轻学习纯音乐\n"
                "• 热门标签：#学习方法 #知识分享 #思维提升 #高效学习\n\n"
                "【分镜脚本（逐句可拍）】\n"
                "（格式：镜头画面 | 台词 | 语气/节奏）\n\n"
                "1. 【黄金3秒】\n"
                "镜头：博主正视镜头，直击痛点\n"
                "台词：\n"
                "语气：快、抓注意力\n\n"
                "2. 【常见学习误区】\n"
                "镜头：思考/摇头画面\n"
                "台词：\n"
                "语气：点醒、共鸣\n\n"
                "3. 【学习技巧1】\n"
                "镜头：正面讲解\n"
                "台词：\n"
                "语气：清晰、耐心\n\n"
                "4. 【学习技巧2】\n"
                "镜头：步骤演示/笔记特写\n"
                "台词：\n"
                "语气：放慢、强调\n\n"
                "5. 【学习技巧3】\n"
                "镜头：手势总结\n"
                "台词：\n"
                "语气：好记、实用\n\n"
                "6. 【总结复盘】\n"
                "镜头：简洁归纳画面\n"
                "台词：\n"
                "语气：鼓励、正向\n\n"
                "7. 【高互动结尾】\n"
                "镜头：温和看镜头\n"
                "台词：用选择题互动\n"
                "语气：亲切、引导交流\n\n"
                "【拍摄建议】3条关键画面\n\n"
                "--- 强制要求 ---\n"
                "1. 画面简洁干净，适合知识类。\n"
                "2. 台词通俗，学生与成人都能懂。\n"
                "3. 无虚假承诺、不焦虑营销。\n"
                "4. 时长充足，内容饱满。\n"
                "5. 只输出格式内容。"
            )

        # ====================== 创业 / 商业 赛道 ======================
        elif category == "business":
            return (
                f"你是商业创业短视频编剧，理性务实、有深度、不画饼、不鸡汤。\n"
                f"结合选题『{topic}』和素材『{description}』创作90-120秒口播脚本。\n\n"
                "--- 硬性规则 ---\n"
                "• 时长：90-120秒（500-700字）\n"
                "• 风格：沉稳理性、逻辑清晰、口语化，不煽动暴富\n"
                "• 严禁：稳赚、暴富、躺赚、零成本、无风险、必成、保本\n"
                "• 违禁词：最、第一、顶级、暴利、100%、必赚、白手起家神话\n"
                "• 侧重模式拆解、案例分析、商业认知，不画饼\n\n"
                "--- 输出格式（严格遵守）---\n\n"
                "【视频基础信息】\n"
                "• 时长：90-120秒\n"
                "• 建议BGM：大气商务、沉稳节奏纯音乐\n"
                "• 热门标签：#商业思维 #创业认知 #商业模式 #搞钱思路\n\n"
                "【分镜脚本（逐句可拍）】\n"
                "（格式：镜头画面 | 台词 | 语气/节奏）\n\n"
                "1. 【黄金3秒】\n"
                "镜头：博主沉稳正视镜头\n"
                "台词：\n"
                "语气：快、有冲击力\n\n"
                "2. 【行业现象/误区拆解】\n"
                "镜头：手势分析/简单图表\n"
                "台词：\n"
                "语气：犀利、点透本质\n\n"
                "3. 【商业逻辑1】\n"
                "镜头：正面讲解\n"
                "台词：\n"
                "语气：清晰、理性\n\n"
                "4. 【商业逻辑2】\n"
                "镜头：案例类比画面\n"
                "台词：\n"
                "语气：强调、易懂\n\n"
                "5. 【案例/总结】\n"
                "镜头：手势总结\n"
                "台词：\n"
                "语气：深刻、接地气\n\n"
                "6. 【认知升华】\n"
                "镜头：庄重正面\n"
                "台词：\n"
                "语气：稳重、有分量\n\n"
                "7. 【高互动结尾】\n"
                "镜头：自然看向镜头\n"
                "台词：用选择题互动\n"
                "语气：成熟、引导思考\n\n"
                "【拍摄建议】3条关键画面\n\n"
                "--- 强制要求 ---\n"
                "1. 画面简洁高级，不浮夸。\n"
                "2. 台词逻辑强，不空话。\n"
                "3. 绝不承诺收益、不制造焦虑。\n"
                "4. 内容饱满有深度，时长达标。\n"
                "5. 只输出格式内容，无多余解释。"
            )

        # 默认返回 AI 科技版
        else:
            return self._get_script_prompt(topic, "ai_tech")
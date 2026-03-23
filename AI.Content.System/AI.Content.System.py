
#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
AI内容生成工厂：Reddit+X热点抓取 + 大模型视频脚本生成
支持ChatGPT+国产大模型双版本，按需切换对比使用
合规说明：仅调用官方API，抓取标题、摘要、热度数据，不抓取全文，仅做二次创作素材
"""
import os
import json
import requests
from dotenv import load_dotenv
import praw
import tweepy

# 加载环境变量（密钥统一管理，避免硬编码泄露）
load_dotenv()

# -------------------------- 配置项（需填写自己的API密钥）--------------------------
# Reddit API 配置
REDDIT_CLIENT_ID = os.getenv("REDDIT_CLIENT_ID")
REDDIT_CLIENT_SECRET = os.getenv("REDDIT_CLIENT_SECRET")
REDDIT_USER_AGENT = os.getenv("REDDIT_USER_AGENT")

# X (Twitter) API 配置
X_BEARER_TOKEN = os.getenv("X_BEARER_TOKEN")

# OpenAI ChatGPT 配置
OPENAI_API_KEY = os.getenv("OPENAI_API_KEY")
OPENAI_BASE_URL = os.getenv("OPENAI_BASE_URL", "https://api.openai.com/v1")
OPENAI_MODEL = "gpt-3.5-turbo-16k"

# 国产大模型配置（以字节豆包/文心一言通用适配为例，可替换对应厂商接口）
DOMESTIC_API_KEY = os.getenv("DOMESTIC_API_KEY")
DOMESTIC_BASE_URL = os.getenv("DOMESTIC_BASE_URL", "https://ark.cn-beijing.volces.com/api/v3")
DOMESTIC_MODEL = os.getenv("DOMESTIC_MODEL", "doubao-1000k-pro")  # 按需替换模型名

# 全局配置：选择使用的模型，可选 chatgpt / domestic / both（双版本对比）
USE_MODEL = "both"

# 热点筛选配置
TARGET_SUBREDDITS = ["ArtificialIntelligence", "ChatGPT", "tech", "MachineLearning"]
X_KEYWORDS = "#AI #ArtificialIntelligence #TechNews #OpenAI #GPT"
HOT_LIMIT = 10  # 单平台抓取热点数量
TIME_FILTER = "day"  # 时间范围：day/hour/week/month

# -------------------------- 1. Reddit 热点抓取模块 --------------------------
def fetch_reddit_hot_topics():
    """
    调用Reddit官方API，获取AI/科技板块热点
    返回：结构化热点列表（标题、链接、热度、简介、来源）
    """
    reddit_hot_list = []
    try:
        reddit = praw.Reddit(
            client_id=REDDIT_CLIENT_ID,
            client_secret=REDDIT_CLIENT_SECRET,
            user_agent=REDDIT_USER_AGENT
        )
        # 遍历目标子版块，获取热门帖子
        for subreddit_name in TARGET_SUBREDDITS:
            subreddit = reddit.subreddit(subreddit_name)
            for submission in subreddit.hot(limit=HOT_LIMIT, time_filter=TIME_FILTER):
                # 仅提取核心数据，不抓取全文
                hot_item = {
                    "platform": "Reddit",
                    "subreddit": f"r/{subreddit_name}",
                    "title": submission.title,
                    "url": submission.url,
                    "score": submission.score,  # 热度分数
                    "content_summary": submission.selftext[:200] + "..." if len(submission.selftext) > 200 else submission.selftext,
                    "created_time": submission.created_utc
                }
                reddit_hot_list.append(hot_item)
        print(f"✅ Reddit 抓取完成，共获取 {len(reddit_hot_list)} 条热点")
        return reddit_hot_list
    except Exception as e:
        print(f"❌ Reddit 抓取失败：{str(e)}")
        return []

# -------------------------- 2. X (Twitter) 热点抓取模块 --------------------------
def fetch_x_hot_topics():
    """
    调用X官方API，获取AI/科技关键词热点推文
    返回：结构化热点列表
    """
    x_hot_list = []
    try:
        # 初始化X客户端
        client = tweepy.Client(bearer_token=X_BEARER_TOKEN)
        # 搜索关键词热点，排除转发、仅保留原创
        query = f"{X_KEYWORDS} -is:retweet -is:reply lang:en"
        response = client.search_recent_tweets(
            query=query,
            max_results=HOT_LIMIT,
            tweet_fields=["created_at", "public_metrics", "lang"]
        )
        if response.data:
            for tweet in response.data:
                hot_item = {
                    "platform": "X",
                    "title": tweet.text[:100] + "..." if len(tweet.text) > 100 else tweet.text,
                    "content_summary": tweet.text,
                    "url": f"https://twitter.com/i/web/status/{tweet.id}",
                    "like_count": tweet.public_metrics["like_count"],
                    "created_time": tweet.created_at
                }
                x_hot_list.append(hot_item)
        print(f"✅ X 平台抓取完成，共获取 {len(x_hot_list)} 条热点")
        return x_hot_list
    except Exception as e:
        print(f"❌ X 平台抓取失败：{str(e)}")
        return []

# -------------------------- 3. 大模型通用提示词 --------------------------
def get_system_prompt():
    """统一脚本生成规则，保证双模型输出风格一致"""
    return """
    你是专业的AI科技短视频口播脚本师，基于海外热点内容生成高质量脚本，遵循以下规则：
    1. 脚本结构：爆款开头引入 → 核心内容解读 → 观点总结 → 互动引导
    2. 语言风格：口语化、通俗易懂、无专业壁垒，适合1-3分钟口播
    3. 合规要求：客观中立、不造谣、不夸大，标注来源，注明AI生成
    4. 格式要求：分段落标注【口播文案】、【字幕提示】、【画面建议】，方便后期制作
    5. 内容要求：仅提炼核心观点，不复制原文，加入本土化解读
    """

# -------------------------- 4. ChatGPT 脚本生成 --------------------------
def generate_by_chatgpt(hot_item, idx):
    """调用OpenAI ChatGPT生成脚本"""
    system_prompt = get_system_prompt()
    user_prompt = f"""
    基于以下海外{hot_item['platform']}热点内容，生成短视频口播脚本：
    热点标题：{hot_item['title']}
    热点摘要：{hot_item['content_summary']}
    来源：{hot_item.get('subreddit', '')} {hot_item['url']}
    """
    
    headers = {
        "Authorization": f"Bearer {OPENAI_API_KEY}",
        "Content-Type": "application/json"
    }
    payload = {
        "model": OPENAI_MODEL,
        "messages": [
            {"role": "system", "content": system_prompt},
            {"role": "user", "content": user_prompt}
        ],
        "temperature": 0.7,
        "max_tokens": 1500
    }
    
    try:
        response = requests.post(
            f"{OPENAI_BASE_URL}/chat/completions",
            headers=headers,
            json=payload,
            timeout=30
        )
        response.raise_for_status()
        return response.json()["choices"][0]["message"]["content"]
    except Exception as e:
        print(f"❌ ChatGPT生成第{idx}条脚本失败：{str(e)}")
        return None

# -------------------------- 5. 国产大模型脚本生成 --------------------------
def generate_by_domestic(hot_item, idx):
    """调用国产大模型生成脚本，兼容主流厂商接口规范"""
    system_prompt = get_system_prompt()
    user_prompt = f"""
    基于以下海外{hot_item['platform']}热点内容，生成短视频口播脚本：
    热点标题：{hot_item['title']}
    热点摘要：{hot_item['content_summary']}
    来源：{hot_item.get('subreddit', '')} {hot_item['url']}
    """
    
    headers = {
        "Authorization": f"Bearer {DOMESTIC_API_KEY}",
        "Content-Type": "application/json"
    }
    payload = {
        "model": DOMESTIC_MODEL,
        "messages": [
            {"role": "system", "content": system_prompt},
            {"role": "user", "content": user_prompt}
        ],
        "temperature": 0.7,
        "max_tokens": 1500
    }
    
    try:
        response = requests.post(
            f"{DOMESTIC_BASE_URL}/chat/completions",
            headers=headers,
            json=payload,
            timeout=30
        )
        response.raise_for_status()
        return response.json()["choices"][0]["message"]["content"]
    except Exception as e:
        print(f"❌ 国产模型生成第{idx}条脚本失败：{str(e)}")
        return None

# -------------------------- 6. 总脚本生成调度（支持双模型对比）--------------------------
def generate_video_script(hot_content_list):
    """
    调度双模型生成脚本，按需单模型/双模型对比输出
    """
    if not hot_content_list:
        print("❌ 无热点内容，无法生成脚本")
        return []
    
    script_list = []
    for idx, item in enumerate(hot_content_list, 1):
        print(f"\n📝 开始处理第{idx}条热点：{item['title']}")
        script_item = {
            "script_id": idx,
            "source_platform": item["platform"],
            "hot_title": item["title"],
            "source_url": item["url"],
            "chatgpt_script": None,
            "domestic_script": None
        }
        
        # 按需调用模型
        if USE_MODEL in ["chatgpt", "both"]:
            script_item["chatgpt_script"] = generate_by_chatgpt(item, idx)
        if USE_MODEL in ["domestic", "both"]:
            script_item["domestic_script"] = generate_by_domestic(item, idx)
        
        script_list.append(script_item)
        print(f"✅ 第{idx}条热点脚本处理完成")
    
    return script_list

# -------------------------- 7. 主函数：执行全流程 --------------------------
def main():
    print("===== AI内容生成工厂 - 双模型热点脚本生成启动 =====")
    print(f"当前使用模型：{USE_MODEL}（chatgpt/国内模型/双版本对比）")
    
    # 1. 抓取双平台热点
    reddit_hot = fetch_reddit_hot_topics()
    x_hot = fetch_x_hot_topics()
    all_hot = reddit_hot + x_hot
    
    if not all_hot:
        print("❌ 未获取到任何热点，程序退出")
        return
    
    # 2. 生成视频脚本
    script_result = generate_video_script(all_hot)
    
    # 3. 保存脚本到本地（后续可对接数据库）
    with open("ai_video_scripts.json", "w", encoding="utf-8") as f:
        json.dump(script_result, f, ensure_ascii=False, indent=2)
    
    print(f"\n===== 全流程完成 =====")
    print(f"总计抓取热点：{len(all_hot)} 条")
    print(f"总计生成脚本：{len(script_result)} 条")
    print("脚本已保存至：ai_video_scripts.json，支持双版本对比查看")

if __name__ == "__main__":
    main()
from app.crawler.reddit import fetch_reddit
from app.crawler.hackernews import fetch_hn
from app.crawler.twitter import fetch_twitter

from app.ai.topic_generator import generate_topics
from app.ai.script_generator import generate_script

from app.db.database import SessionLocal
from app.db.models import Content

def run_pipeline():
    print("开始执行内容流水线...")
    db = SessionLocal()
    count = 0

    try:
        posts = []
        posts += fetch_reddit()
        posts += fetch_hn()
        posts += fetch_twitter()

        for post in posts:
            title = post.get("title", "")
            if not title:
                continue

            # 生成选题
            print(f"generate_topics")
            topics = generate_topics(title)
            for topic in topics:
                if not topic or len(topic) < 5:  # 过滤过短的选题
                    continue
                # 生成脚本
                print(f"generate_script")
                script = generate_script(topic)
                if not script:
                    continue

                # 存入数据库
                record = Content(
                    source=post["source"],
                    title=title,
                    topic=topic,
                    script=script
                )
                db.add(record)
                count += 1

        db.commit()
        print(f"流水线执行完成，共生成 {count} 条内容")
    except Exception as e:
        # 捕获所有异常，避免服务崩溃
        print("流水线执行异常:", e)
        db.rollback()  # 异常时回滚数据库
        raise  # 重新抛出异常，让 FastAPI 返回友好提示
    finally:
        db.close()  # 确保数据库连接关闭
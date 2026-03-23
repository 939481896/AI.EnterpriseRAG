# -*- coding: utf-8 -*-
import sys
sys.path.insert(0, ".")

# 测试核心模块导入
try:
    from app.infrastructure.sources.reddit import RedditSource
    print("✅ RedditSource 导入成功")
except Exception as e:
    print(f"❌ 导入失败: {e}")
    # 打印所有可用模块路径
    print("\nPython 路径：")
    for p in sys.path:
        print(f"- {p}")
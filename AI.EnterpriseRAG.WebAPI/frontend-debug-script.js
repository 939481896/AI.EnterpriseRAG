// 前端快速诊断脚本 - 复制到浏览器Console运行

// ============================================
// 🔍 步骤 1: 检测缓存问题
// ============================================
console.log('🔍 开始诊断会话历史缓存问题...\n');

// 检查全局变量中是否有缓存对象
const possibleCacheKeys = [
  'conversationCache',
  'conversationDetails', 
  '__conversation_cache__',
  'chatCache',
  'sessionCache'
];

console.log('📦 检查全局缓存对象:');
possibleCacheKeys.forEach(key => {
  if (window[key]) {
    console.log(`  ✅ 找到缓存对象: window.${key}`, window[key]);
  }
});

// ============================================
// 🔍 步骤 2: 拦截所有fetch/axios请求
// ============================================
console.log('\n🌐 拦截所有API请求...');

const originalFetch = window.fetch;
let requestCount = 0;

window.fetch = async function(...args) {
  const url = args[0];
  const requestId = ++requestCount;
  
  console.log(`📤 [请求 #${requestId}] ${url}`);
  console.log(`   请求时间: ${new Date().toISOString()}`);
  
  try {
    const response = await originalFetch.apply(this, args);
    console.log(`📥 [响应 #${requestId}] ${url} - 状态: ${response.status}`);
    
    // 检查响应头
    const cacheControl = response.headers.get('Cache-Control');
    const requestIdHeader = response.headers.get('X-Request-Id');
    
    if (cacheControl) {
      console.log(`   Cache-Control: ${cacheControl}`);
    }
    if (requestIdHeader) {
      console.log(`   X-Request-Id: ${requestIdHeader}`);
    }
    
    return response;
  } catch (error) {
    console.error(`❌ [错误 #${requestId}] ${url}`, error);
    throw error;
  }
};

// 如果使用axios，也拦截axios
if (window.axios) {
  window.axios.interceptors.request.use(config => {
    const requestId = ++requestCount;
    config.headers['X-Debug-Request-Id'] = requestId;
    console.log(`📤 [Axios请求 #${requestId}] ${config.method.toUpperCase()} ${config.url}`);
    return config;
  });

  window.axios.interceptors.response.use(response => {
    const requestId = response.config.headers['X-Debug-Request-Id'];
    console.log(`📥 [Axios响应 #${requestId}] 状态: ${response.status}`);
    return response;
  }, error => {
    const requestId = error.config?.headers['X-Debug-Request-Id'];
    console.error(`❌ [Axios错误 #${requestId}]`, error);
    return Promise.reject(error);
  });
}

console.log('✅ API拦截器已安装\n');

// ============================================
// 🔍 步骤 3: 监听点击事件
// ============================================
console.log('👆 监听会话列表点击事件...');

let clickCount = 0;
const clickHistory = [];

document.addEventListener('click', (event) => {
  // 查找可能的会话列表项
  const target = event.target;
  const conversationItem = target.closest('[data-conversation-id], .conversation-item, .history-item');
  
  if (conversationItem) {
    const clickId = ++clickCount;
    const conversationId = conversationItem.dataset.conversationId || 
                          conversationItem.getAttribute('data-id') ||
                          '未知';
    
    const clickInfo = {
      id: clickId,
      conversationId: conversationId,
      timestamp: new Date().toISOString(),
      element: conversationItem
    };
    
    clickHistory.push(clickInfo);
    
    console.log(`\n👆 [点击 #${clickId}] 会话ID: ${conversationId}`);
    console.log(`   时间: ${clickInfo.timestamp}`);
    console.log(`   元素:`, conversationItem);
    
    // 检查是否是重复点击
    const previousClicks = clickHistory.filter(c => c.conversationId === conversationId);
    if (previousClicks.length > 1) {
      console.warn(`   ⚠️ 这是第${previousClicks.length}次点击该会话`);
    }
    
    // 等待500ms检查是否有API调用
    setTimeout(() => {
      const recentRequests = requestCount - (clickInfo.id - 1);
      if (recentRequests === 0) {
        console.error(`   ❌ 点击后没有API调用！可能被缓存了！`);
      } else {
        console.log(`   ✅ 点击后触发了 ${recentRequests} 个API请求`);
      }
    }, 500);
  }
}, true);

console.log('✅ 点击监听器已安装\n');

// ============================================
// 🔍 步骤 4: 提供辅助函数
// ============================================
window.debugChat = {
  // 查看点击历史
  getClickHistory: () => {
    console.table(clickHistory.map(c => ({
      点击ID: c.id,
      会话ID: c.conversationId,
      时间: c.timestamp
    })));
  },
  
  // 查看API请求统计
  getRequestStats: () => {
    console.log(`总请求数: ${requestCount}`);
    console.log(`总点击数: ${clickCount}`);
    console.log(`每次点击平均请求数: ${(requestCount / clickCount).toFixed(2)}`);
  },
  
  // 清除所有可能的缓存
  clearAllCaches: () => {
    possibleCacheKeys.forEach(key => {
      if (window[key]) {
        window[key] = {};
        console.log(`🗑️ 已清空: window.${key}`);
      }
    });
    
    // 清除sessionStorage和localStorage
    const keysToCheck = Object.keys(sessionStorage).concat(Object.keys(localStorage));
    keysToCheck.forEach(key => {
      if (key.includes('conversation') || key.includes('chat') || key.includes('session')) {
        sessionStorage.removeItem(key);
        localStorage.removeItem(key);
        console.log(`🗑️ 已清空存储: ${key}`);
      }
    });
    
    console.log('✅ 缓存清理完成');
  },
  
  // 测试API调用
  testAPI: async (conversationId) => {
    console.log(`🧪 测试API调用: /api/chat/history/${conversationId}`);
    try {
      const response = await fetch(`/api/chat/history/${conversationId}`, {
        headers: {
          'Authorization': localStorage.getItem('token') || sessionStorage.getItem('token'),
          'Cache-Control': 'no-cache'
        }
      });
      const data = await response.json();
      console.log('✅ API响应:', data);
      return data;
    } catch (error) {
      console.error('❌ API调用失败:', error);
    }
  },
  
  // 查看帮助
  help: () => {
    console.log(`
🔧 调试工具使用说明:

1. debugChat.getClickHistory()    - 查看所有点击历史
2. debugChat.getRequestStats()    - 查看API请求统计
3. debugChat.clearAllCaches()     - 清除所有缓存
4. debugChat.testAPI(conversationId) - 直接测试API调用
5. debugChat.help()               - 显示此帮助

现在请执行以下操作:
1. 点击第一个会话 (应该看到API调用)
2. 点击第二个会话 (应该看到API调用)
3. 点击第三个会话 (应该看到API调用)
4. 再次点击第一个会话 (检查是否有API调用)

如果第4步没有API调用，运行:
debugChat.getClickHistory()
debugChat.getRequestStats()

然后将结果发送给开发团队。
    `);
  }
};

// ============================================
// 🎯 完成提示
// ============================================
console.log('✅ 诊断工具已准备完毕！\n');
console.log('📝 使用说明:');
console.log('   1. 正常操作: 点击会话列表项');
console.log('   2. 观察Console输出的API请求日志');
console.log('   3. 如果点击后没有看到API请求，说明被缓存了');
console.log('   4. 运行 debugChat.help() 查看更多调试命令\n');

console.log('🔍 开始测试...');

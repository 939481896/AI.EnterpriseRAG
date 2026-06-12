# 前端问题修复方案

## 问题诊断

### 问题1：文档删除功能不能正常使用
**状态**: ✅ **已修复**（在之前的修复中已完成）

**后端API**:
- 端点: `DELETE /api/Document/{documentId}`
- 权限: `doc.delete`
- 状态: 已实现并正常工作

**可能的前端问题**:
1. 前端调用错误的API路径
2. 未正确传递文档ID
3. 未处理权限错误
4. 未刷新文档列表

**前端修复建议**:
```typescript
// 删除文档
async function deleteDocument(documentId: string) {
  try {
    const response = await fetch(`/api/Document/${documentId}`, {
      method: 'DELETE',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || '删除失败');
    }

    const result = await response.json();
    console.log('删除成功:', result);
    
    // ✅ 重要：删除后刷新文档列表
    await refreshDocumentList();
    
  } catch (error) {
    console.error('删除文档失败:', error);
    alert('删除文档失败: ' + error.message);
  }
}
```

---

### 问题2：查看历史对话功能不能显示历史记录
**状态**: ❌ **缺少后端API**

**问题根源**:
- 后端`ChatController`没有提供获取历史对话的接口
- 前端调用的历史对话API不存在
- 数据库有`chat_conversations`表，但没有查询接口

**现有API**:
```csharp
// 仅有问答接口，没有历史查询接口
POST /api/Chat/ask
POST /api/Chat/ask-v1
```

**缺少的API**:
```csharp
// ❌ 不存在
GET /api/Chat/history
GET /api/Chat/conversations
```

---

## 解决方案

### 修复步骤1: 添加历史对话查询接口

#### 1.1 更新接口定义

**文件**: `AI.EnterpriseRAG.Domain/Interfaces/UseCases/IChatUseCase.cs`

添加以下方法：
```csharp
/// <summary>
/// 获取用户的对话历史
/// </summary>
/// <param name="userId">用户ID</param>
/// <param name="pageSize">每页数量</param>
/// <param name="cancellationToken">取消令牌</param>
/// <returns>对话历史列表</returns>
Task<List<ChatConversationDto>> GetUserConversationsAsync(
    string userId, 
    int pageSize = 20, 
    CancellationToken cancellationToken = default);
```

#### 1.2 实现历史查询逻辑

**文件**: `AI.EnterpriseRAG.Application/UseCases/ChatUseCase.cs`

添加实现：
```csharp
public async Task<List<ChatConversationDto>> GetUserConversationsAsync(
    string userId, 
    int pageSize = 20, 
    CancellationToken cancellationToken = default)
{
    _logger.LogInformation("📋 获取用户对话历史：{UserId}，数量：{PageSize}", userId, pageSize);

    var conversations = await _chatConversationRepository.GetByUserIdAsync(userId, pageSize);

    return conversations.Select(c => new ChatConversationDto
    {
        Id = c.Id,
        Question = c.Question,
        Answer = c.Answer,
        CreateTime = c.CreateTime,
        UserId = c.UserId
    }).ToList();
}
```

#### 1.3 添加DTO

**文件**: `AI.EnterpriseRAG.Application/Dtos/ChatConversationDto.cs` (新建)

```csharp
namespace AI.EnterpriseRAG.Application.Dtos;

/// <summary>
/// 对话历史DTO
/// </summary>
public class ChatConversationDto
{
    public Guid Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public DateTime CreateTime { get; set; }
    public string UserId { get; set; } = string.Empty;
}
```

#### 1.4 添加Controller接口

**文件**: `AI.EnterpriseRAG.WebAPI/Controllers/ChatController.cs`

添加以下接口：
```csharp
/// <summary>
/// 获取对话历史
/// </summary>
/// <param name="pageSize">每页数量（默认20）</param>
/// <param name="cancellationToken">取消令牌</param>
/// <returns>对话历史列表</returns>
[HttpGet("history")]
[Authorize]
//[Permission("chat.history")]
[ProducesResponseType(typeof(Result<List<ChatConversationDto>>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
public async Task<IActionResult> GetHistory(
    [FromQuery] int pageSize = 20,
    CancellationToken cancellationToken = default)
{
    try
    {
        // 从Token获取用户ID
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
                     ?? User.FindFirstValue(ClaimTypes.Name)
                     ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(Result.Fail("用户未登录"));

        _logger.LogInformation("用户{UserId}查询对话历史，数量：{PageSize}", userId, pageSize);

        var conversations = await _chatUseCase.GetUserConversationsAsync(
            userId, 
            pageSize, 
            cancellationToken);

        return Ok(Result<List<ChatConversationDto>>.SuccessResult(
            conversations, 
            $"成功获取{conversations.Count}条对话历史"));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "获取对话历史失败");
        return BadRequest(Result.Fail($"获取对话历史失败：{ex.Message}"));
    }
}

/// <summary>
/// 删除对话记录
/// </summary>
/// <param name="conversationId">对话ID</param>
/// <param name="cancellationToken">取消令牌</param>
/// <returns>删除结果</returns>
[HttpDelete("history/{conversationId}")]
[Authorize]
//[Permission("chat.delete")]
[ProducesResponseType(typeof(Result), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(Result), StatusCodes.Status401Unauthorized)]
[ProducesResponseType(typeof(Result), StatusCodes.Status404NotFound)]
public async Task<IActionResult> DeleteConversation(
    Guid conversationId,
    CancellationToken cancellationToken = default)
{
    try
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.UniqueName)
                     ?? User.FindFirstValue(ClaimTypes.Name)
                     ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(Result.Fail("用户未登录"));

        _logger.LogInformation("用户{UserId}删除对话：{ConversationId}", userId, conversationId);

        await _chatUseCase.DeleteConversationAsync(conversationId, cancellationToken);

        return Ok(Result.SuccessResult("对话删除成功"));
    }
    catch (KeyNotFoundException ex)
    {
        return NotFound(Result.Fail(ex.Message));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "删除对话失败：{ConversationId}", conversationId);
        return BadRequest(Result.Fail($"删除对话失败：{ex.Message}"));
    }
}
```

---

## 前端调用示例

### 获取历史对话

```typescript
// 获取对话历史
async function loadChatHistory(pageSize: number = 20) {
  try {
    const response = await fetch(`/api/Chat/history?pageSize=${pageSize}`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    });

    if (!response.ok) {
      throw new Error('获取历史对话失败');
    }

    const result = await response.json();
    
    if (result.success) {
      console.log('历史对话:', result.data);
      displayConversations(result.data);
    } else {
      console.error('获取失败:', result.message);
    }
  } catch (error) {
    console.error('加载历史对话失败:', error);
  }
}

// 显示对话历史
function displayConversations(conversations: ChatConversation[]) {
  const listContainer = document.getElementById('conversation-list');
  if (!listContainer) return;

  listContainer.innerHTML = '';

  conversations.forEach(conv => {
    const item = document.createElement('div');
    item.className = 'conversation-item';
    item.innerHTML = `
      <div class="question">${escapeHtml(conv.question)}</div>
      <div class="answer">${escapeHtml(conv.answer)}</div>
      <div class="time">${formatDate(conv.createTime)}</div>
      <button onclick="deleteConversation('${conv.id}')">删除</button>
    `;
    listContainer.appendChild(item);
  });
}

// 删除对话
async function deleteConversation(conversationId: string) {
  if (!confirm('确定删除这条对话吗？')) return;

  try {
    const response = await fetch(`/api/Chat/history/${conversationId}`, {
      method: 'DELETE',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    });

    if (!response.ok) {
      throw new Error('删除失败');
    }

    const result = await response.json();
    console.log('删除成功:', result);
    
    // 刷新历史列表
    await loadChatHistory();
  } catch (error) {
    console.error('删除对话失败:', error);
    alert('删除失败: ' + error.message);
  }
}
```

---

## 完整API文档

### 1. 文档管理API

| 接口 | 方法 | 路径 | 描述 | 权限 |
|------|------|------|------|------|
| 上传文档 | POST | `/api/Document/upload` | 上传并处理文档 | `doc.upload` |
| 获取文档列表 | GET | `/api/Document/list` | 分页获取用户文档 | 无 |
| **删除文档** | DELETE | `/api/Document/{id}` | 删除指定文档 | `doc.delete` |
| 重新处理文档 | POST | `/api/Document/{id}/reprocess` | 重新处理失败的文档 | `doc.upload` |

### 2. 智能问答API

| 接口 | 方法 | 路径 | 描述 | 权限 |
|------|------|------|------|------|
| 基础问答 | POST | `/api/Chat/ask` | 标准RAG问答 | 无 |
| 高级问答 | POST | `/api/Chat/ask-v1` | HyDE+多查询+自反思 | 无 |
| **获取历史对话** | GET | `/api/Chat/history` | 获取用户对话历史 | 无 |
| **删除对话** | DELETE | `/api/Chat/history/{id}` | 删除指定对话记录 | 无 |

---

## 测试清单

### 文档删除测试

- [ ] 正常删除文档
- [ ] 删除不存在的文档（404错误）
- [ ] 无权限删除（401错误）
- [ ] 删除后刷新列表
- [ ] 删除失败后的错误提示

### 历史对话测试

- [ ] 获取历史对话列表
- [ ] 空历史状态显示
- [ ] 对话时间格式化显示
- [ ] 删除单条对话
- [ ] 删除后刷新列表
- [ ] 分页加载更多

---

## 故障排查指南

### 前端调试步骤

1. **检查网络请求**
```javascript
// 在浏览器开发者工具中检查：
// 1. Network标签 - 查看请求是否发送成功
// 2. Console标签 - 查看错误日志
// 3. Application标签 - 检查Token是否存在
```

2. **验证Token**
```javascript
const token = localStorage.getItem('token');
console.log('Token:', token ? 'exists' : 'missing');
```

3. **测试API**
```bash
# 测试获取历史对话
curl -X GET "http://localhost:5000/api/Chat/history?pageSize=20" \
  -H "Authorization: Bearer YOUR_TOKEN"

# 测试删除文档
curl -X DELETE "http://localhost:5000/api/Document/{documentId}" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### 常见错误处理

| 错误代码 | 原因 | 解决方法 |
|---------|------|---------|
| 401 | 未登录或Token过期 | 重新登录获取Token |
| 403 | 无权限 | 检查用户权限配置 |
| 404 | 资源不存在 | 验证ID是否正确 |
| 500 | 服务器错误 | 查看后端日志 |

---

## 注意事项

1. **权限验证**: 所有接口都需要`Authorization`头
2. **Token格式**: `Bearer {token}`
3. **用户ID**: 从Token自动提取，不要从前端传入
4. **错误处理**: 前端需要处理所有可能的HTTP状态码
5. **数据刷新**: 删除操作后需要刷新列表

---

## 下一步建议

1. **添加分页功能**: 历史对话支持更多分页参数
2. **搜索功能**: 按关键词搜索历史对话
3. **导出功能**: 导出对话历史为文本/JSON
4. **批量操作**: 批量删除对话记录
5. **对话分类**: 按时间/主题分类显示

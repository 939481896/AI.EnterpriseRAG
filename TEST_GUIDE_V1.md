# 🧪 V1.0 Quick Test Guide

## Critical Bug Fix: Hybrid Search Scoring

**Issue Fixed**: Hybrid Search was filtering out ALL results due to RRF scores overwriting original similarities.

---

## 🚀 Quick Test

### 1. Start Application
```bash
cd AI.EnterpriseRAG.WebAPI
dotnet run
```

### 2. Test Query (Chinese)
```bash
POST http://localhost:5000/api/chat/ask-v1
Content-Type: application/json

{
  "userId": "admin",
  "question": "房价下降的基本原则"
}
```

### 3. Check Logs (Should See)

**✅ BEFORE FIX (Buggy):**
```
📊 Vector results: 60
📊 BM25 results: 0  ← ❌ WRONG
✅ Hybrid fusion complete | Final: 20
⚠️ User admin found no valid content  ← ❌ ALL FILTERED OUT
```

**✅ AFTER FIX (Working):**
```
🔧 Tokenized '房价下降的基本原则' → 9 terms: 房, 价, 下, 降, 的  ← ✅ CORRECT
📊 Vector results: 60
📊 BM25 results: 15  ← ✅ WORKING NOW!
✅ Hybrid fusion complete | Final: 20
RRF fusion: 20 results | Score range: 0.9500-1.0000  ← ✅ PRESERVED!
✅ V1.0 RAG complete | Time: 3.52s | Chunks: 5  ← ✅ ANSWER RETURNED
```

---

## 📊 What to Verify

### 1. BM25 Is Working
```
✅ BM25 results: 10-20 (not 0!)
✅ Tokenized query shows Chinese characters
```

### 2. Similarity Scores Preserved
```
✅ Score range: 0.8000-1.0000 (not 0.01-0.02!)
✅ After filtering: 15-20 chunks (not 0!)
```

### 3. Valid Answer Returned
```json
{
  "success": true,
  "data": {
    "answer": "根据文档内容，房价下降的基本原则包括...",
    "references": [
      "文档内容1",
      "文档内容2"
    ],
    "costSeconds": 3.52
  }
}
```

---

## 🔍 Debug Commands

### Check BM25 Tokenization
```bash
# In logs, search for:
"Tokenized"
# Should see:
# Tokenized '房价下降' → 4 terms: 房, 价, 下, 降
```

### Check Similarity Preservation
```bash
# In logs, search for:
"Score range"
# Should see:
# Score range: 0.9500-1.0000
# NOT: Score range: 0.0120-0.0160 (RRF scores)
```

### Check Filtering Results
```bash
# In logs, search for:
"found no valid content"
# Should NOT appear if fix is working
```

---

## 🐛 If Still Broken

### Symptom 1: "No valid content" returned
**Check:**
```bash
# Logs should show:
✅ Hybrid fusion complete | Final: 20
✅ Score range: 0.8-1.0  ← Must be high!
```

**If showing:**
```bash
❌ Score range: 0.01-0.02  ← RRF scores not fixed
```

**Solution**: Rebuild application
```bash
dotnet clean
dotnet build
dotnet run
```

---

### Symptom 2: BM25 still returns 0
**Check:**
```bash
# Logs should show:
✅ Tokenized '房价' → 2 terms: 房, 价
```

**If showing:**
```bash
❌ Tokenized '房价' → 1 terms: 房价  ← No character split
```

**Solution**: Check `appsettings.json`
```json
"HybridSearch": {
  "MinTokenLength": 1  ← Must be 1 for Chinese!
}
```

---

## 📈 Expected Performance

| Metric | Before Fix | After Fix |
|--------|------------|-----------|
| BM25 Results | 0 | 10-20 |
| Filtered Chunks | 0 | 15-20 |
| Answer Quality | ❌ No answer | ✅ Valid answer |
| Response Time | 52s | <10s |

---

## ✅ Success Criteria

- [x] BM25 returns >0 results
- [x] Similarity scores 0.8-1.0 (not 0.01-0.02)
- [x] FilterValidChunks passes 15-20 chunks
- [x] Valid answer returned
- [x] Response time <10s

---

## 🚨 If All Tests Pass

**Next Steps:**
1. Test with multiple queries
2. Monitor production logs for "BM25 results: 0"
3. Consider upgrading to jieba tokenizer
4. Add unit tests for tokenization

---

## 📞 Support

If issues persist:
1. Check `Logs/app-.log` for detailed traces
2. Verify database has documents
3. Confirm Ollama is running: `curl http://localhost:11434/api/tags`
4. Check Qdrant: `curl http://localhost:6333/collections`

---

**Status**: 🔧 **Fix Applied - Testing Required**  
**Priority**: 🔴 **CRITICAL - Test Immediately**

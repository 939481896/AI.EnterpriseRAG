# Test RAG After Similarity Fix
# Purpose: Verify that lowering MIN_SEARCH_SIMILARITY to 0.2 fixes the issue

Write-Host "🧪 Testing RAG System After Similarity Threshold Fix" -ForegroundColor Cyan
Write-Host ""

$apiUrl = "http://localhost:5000"  # Adjust if different
$question = "四项基本原则是什么？"

Write-Host "Configuration:" -ForegroundColor Yellow
Write-Host "  API URL: $apiUrl" -ForegroundColor Gray
Write-Host "  Question: $question" -ForegroundColor Gray
Write-Host "  Expected: Similarity threshold lowered from 0.7 to 0.2" -ForegroundColor Gray
Write-Host ""

Write-Host "═══════════════════════════════════════════" -ForegroundColor Green
Write-Host "STEP 1: Test Chat API" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════" -ForegroundColor Green
Write-Host ""

$requestBody = @{
    userId = "admin"
    question = $question
} | ConvertTo-Json

Write-Host "Sending request..." -ForegroundColor Gray

try {
    $response = Invoke-RestMethod -Uri "$apiUrl/api/chat/ask" `
        -Method Post `
        -Body $requestBody `
        -ContentType "application/json" `
        -ErrorAction Stop

    Write-Host "✅ Request successful!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Response:" -ForegroundColor Cyan
    Write-Host ($response | ConvertTo-Json -Depth 10) -ForegroundColor White
    Write-Host ""

    # Check if answer is meaningful
    if ($response.data.answer -eq "知识库中未找到相关答案") {
        Write-Host "❌ STILL FAILING - Answer not found" -ForegroundColor Red
        Write-Host ""
        Write-Host "Possible reasons:" -ForegroundColor Yellow
        Write-Host "1. Application not restarted after code change" -ForegroundColor Gray
        Write-Host "2. Similarity still too high (check if 0.2 is applied)" -ForegroundColor Gray
        Write-Host "3. No vectors in Qdrant for this document" -ForegroundColor Gray
        Write-Host "4. Permission issue" -ForegroundColor Gray
    }
    elseif ($response.data.answer -match "四项基本原则") {
        Write-Host "✅ SUCCESS! RAG is working!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Answer preview:" -ForegroundColor Cyan
        Write-Host $response.data.answer.Substring(0, [Math]::Min(200, $response.data.answer.Length)) -ForegroundColor White
        Write-Host ""
        Write-Host "References count: $($response.data.references.Count)" -ForegroundColor Cyan
        Write-Host "Cost: $($response.data.costSeconds) seconds" -ForegroundColor Cyan
    }
    else {
        Write-Host "⚠️ PARTIAL SUCCESS - Got answer but might not be relevant" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "Answer:" -ForegroundColor Cyan
        Write-Host $response.data.answer -ForegroundColor White
    }

} catch {
    Write-Host "❌ Request failed!" -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Troubleshooting steps:" -ForegroundColor Yellow
    Write-Host "1. Ensure application is running: dotnet run --project AI.EnterpriseRAG.WebAPI" -ForegroundColor Gray
    Write-Host "2. Check application logs for errors" -ForegroundColor Gray
    Write-Host "3. Verify API URL is correct" -ForegroundColor Gray
}

Write-Host ""
Write-Host "═══════════════════════════════════════════" -ForegroundColor Green
Write-Host "STEP 2: Check Application Logs" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════" -ForegroundColor Green
Write-Host ""

Write-Host "Look for these patterns in your application logs:" -ForegroundColor Cyan
Write-Host ""
Write-Host "Before fix (FAILED):" -ForegroundColor Yellow
Write-Host '  [INF] ✅ 检索成功 | 结果数量: 0 | 平均相似度: 0.0000' -ForegroundColor Red
Write-Host '  [WRN] 用户admin未检索到有效内容' -ForegroundColor Red
Write-Host ""
Write-Host "After fix (SUCCESS):" -ForegroundColor Green
Write-Host '  [INF] ✅ 检索成功 | 结果数量: 3-5 | 平均相似度: 0.xxxx' -ForegroundColor Green
Write-Host '  [INF] 用户admin检索到有效分块数：X，平均相似度：0.XX' -ForegroundColor Green
Write-Host ""

Write-Host "═══════════════════════════════════════════" -ForegroundColor Green
Write-Host "STEP 3: Verify Similarity Threshold" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════" -ForegroundColor Green
Write-Host ""

$constantsFile = "AI.EnterpriseRAG.Core\Constants\LLMConstants.cs"
if (Test-Path $constantsFile) {
    $content = Get-Content $constantsFile -Raw
    if ($content -match "MIN_SEARCH_SIMILARITY\s*=\s*([\d.]+)f") {
        $threshold = $matches[1]
        if ($threshold -eq "0.2") {
            Write-Host "✅ Threshold confirmed: $threshold" -ForegroundColor Green
        }
        elseif ($threshold -eq "0.7") {
            Write-Host "❌ Threshold NOT updated: $threshold (still too high!)" -ForegroundColor Red
            Write-Host "   File needs to be saved and application restarted" -ForegroundColor Yellow
        }
        else {
            Write-Host "⚠️ Threshold set to: $threshold" -ForegroundColor Yellow
        }
    }
} else {
    Write-Host "⚠️ Constants file not found at expected location" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "═══════════════════════════════════════════" -ForegroundColor Green
Write-Host "Next Steps" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════" -ForegroundColor Green
Write-Host ""

if ($response -and $response.data.answer -ne "知识库中未找到相关答案") {
    Write-Host "✅ RAG is working! You can now:" -ForegroundColor Green
    Write-Host "1. Test with more questions" -ForegroundColor Gray
    Write-Host "2. Adjust threshold if needed (0.15-0.3 recommended for Chinese)" -ForegroundColor Gray
    Write-Host "3. Monitor performance and accuracy" -ForegroundColor Gray
} else {
    Write-Host "❌ RAG still not working. Try:" -ForegroundColor Red
    Write-Host "1. Restart the application:" -ForegroundColor Gray
    Write-Host "   Stop current process and run: dotnet run --project AI.EnterpriseRAG.WebAPI" -ForegroundColor Gray
    Write-Host ""
    Write-Host "2. Run full diagnostic:" -ForegroundColor Gray
    Write-Host "   .\diagnose-rag-issue.ps1" -ForegroundColor Gray
    Write-Host ""
    Write-Host "3. Check if vectors exist in Qdrant:" -ForegroundColor Gray
    Write-Host "   curl http://localhost:6333/collections/enterprise_rag_collection" -ForegroundColor Gray
    Write-Host ""
    Write-Host "4. Grant permission manually:" -ForegroundColor Gray
    Write-Host "   mysql -u root -p ai_enterpriserag < diagnose-rag-quick.sql" -ForegroundColor Gray
}

Write-Host ""
Write-Host "🧪 Test Complete" -ForegroundColor Cyan

# 🚀 Tailwind CSS 自动安装脚本

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Tailwind CSS 安装向导" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 检查是否在 frontend 目录
if (-not (Test-Path "package.json")) {
    Write-Host "❌ 错误：请在 frontend 目录下运行此脚本！" -ForegroundColor Red
    Write-Host "   cd frontend" -ForegroundColor Yellow
    Write-Host "   .\install-tailwind.ps1" -ForegroundColor Yellow
    exit 1
}

Write-Host "📦 步骤 1/4: 安装 Tailwind CSS..." -ForegroundColor Green
npm install -D tailwindcss postcss autoprefixer

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 安装失败！" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Tailwind CSS 安装成功！" -ForegroundColor Green
Write-Host ""

Write-Host "⚙️  步骤 2/4: 初始化配置文件..." -ForegroundColor Green
npx tailwindcss init -p

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ 初始化失败！" -ForegroundColor Red
    exit 1
}

Write-Host "✅ 配置文件创建成功！" -ForegroundColor Green
Write-Host ""

Write-Host "📝 步骤 3/4: 更新 package.json..." -ForegroundColor Green

# 检查 package.json
$packageJson = Get-Content "package.json" -Raw | ConvertFrom-Json

# 显示当前版本
Write-Host "   当前依赖:" -ForegroundColor Cyan
Write-Host "   - tailwindcss: $($packageJson.devDependencies.tailwindcss)" -ForegroundColor Gray
Write-Host "   - postcss: $($packageJson.devDependencies.postcss)" -ForegroundColor Gray
Write-Host "   - autoprefixer: $($packageJson.devDependencies.autoprefixer)" -ForegroundColor Gray

Write-Host "✅ 依赖已添加！" -ForegroundColor Green
Write-Host ""

Write-Host "📄 步骤 4/4: 检查配置文件..." -ForegroundColor Green

$filesToCheck = @(
    "tailwind.config.js",
    "postcss.config.js"
)

foreach ($file in $filesToCheck) {
    if (Test-Path $file) {
        Write-Host "   ✅ $file" -ForegroundColor Green
    } else {
        Write-Host "   ❌ $file 未找到" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  ✅ 安装完成！" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "📚 下一步操作：" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. 更新 src/main.tsx，添加 Tailwind 导入：" -ForegroundColor White
Write-Host "   import 'tailwindcss/tailwind.css'" -ForegroundColor Gray
Write-Host ""
Write-Host "2. 查看迁移指南：" -ForegroundColor White
Write-Host "   TAILWIND_MIGRATION_GUIDE.md" -ForegroundColor Gray
Write-Host ""
Write-Host "3. 查看示例代码：" -ForegroundColor White
Write-Host "   frontend/src/pages/Chat/ChatPage.tailwind.tsx" -ForegroundColor Gray
Write-Host ""
Write-Host "4. 启动开发服务器：" -ForegroundColor White
Write-Host "   npm run dev" -ForegroundColor Gray
Write-Host ""
Write-Host "5. 安装 VSCode 扩展（推荐）：" -ForegroundColor White
Write-Host "   - Tailwind CSS IntelliSense" -ForegroundColor Gray
Write-Host ""
Write-Host "🎉 祝您使用愉快！" -ForegroundColor Magenta

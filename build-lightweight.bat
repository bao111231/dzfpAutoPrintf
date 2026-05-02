@echo off
chcp 65001 >nul
title DZFP PDF 打印工具 - 轻量版打包脚本

echo ═══════════════════════════════════════════════════
echo   DZFP PDF 自动打印工具 - 轻量版打包工具
echo   (需要安装 .NET 8.0 运行时)
echo ═══════════════════════════════════════════════════
echo.

echo [1/4] 检查 .NET SDK...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ❌ 错误: 未检测到 .NET SDK
    echo 请先安装 .NET 8.0 SDK: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)
echo ✅ .NET SDK 已安装
echo.

echo [2/4] 清理旧的编译文件...
if exist "publish-lightweight" (
    rmdir /s /q "publish-lightweight"
)
echo ✅ 清理完成
echo.

echo [3/4] 正在编译并打包轻量版...
echo    (这比完整版快很多，请稍等...)
echo.

call dotnet publish -c Release -o publish-lightweight -p:PublishSingleFile=true --self-contained false

if %errorlevel% neq 0 (
    echo.
    echo ❌ 打包失败！请检查上方错误信息
    pause
    exit /b 1
)

echo.
echo ✅ 轻量版打包成功！
echo.

echo [4/4] 检查生成的文件...
echo.
echo ┌─────────────────────────────────────────────┐
│  📦 生成的文件位置:                           │
│                                             │
│  publish-lightweight\DzfpPdfPrinter.exe       │
│                                             │
│  文件大小:                                  │

for %%A in ("publish-lightweight\DzfpPdfPrinter.exe") do (
    set size=%%~zA
    set /a sizeMB=!size! / 1048576
    set /a sizeKB=!size! / 1024
    if !sizeMB! gtr 0 (
        echo │  !sizeMB! MB (!sizeKB! KB)                   │
    ) else (
        echo │  !sizeKB! KB                              │
    )
)

echo └─────────────────────────────────────────────┘
echo.

echo ═══════════════════════════════════════════════════
echo   ✨ 轻量版打包完成！
echo ═══════════════════════════════════════════════════
echo.
echo 📌 使用方法:
echo    1. 进入 publish-lightweight 文件夹
echo    2. 双击 DzfpPdfPrinter.exe 即可运行
echo    3. 需要目标电脑已安装 .NET 8.0 运行时
echo.
echo 💡 与完整版的区别:
echo    轻量版: ~15-20 MB (需要安装.NET运行时)
echo    完整版: ~150+ MB   (无需安装任何东西)
echo.
echo 📥 如果目标电脑没有 .NET 运行时:
echo    用户需安装: https://dotnet.microsoft.com/download/dotnet/8.0
echo    选择 ".NET Desktop Runtime" x64 版本
echo.
echo 是否打开 publish-lightweight 文件夹？(Y/N)
choice /C YN /M "打开文件夹"
if %errorlevel% equ 1 (
    explorer "publish-lightweight"
)

pause

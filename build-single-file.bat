@echo off
chcp 65001 >nul
title DZFP PDF 打印工具 - 一键打包脚本

echo ═══════════════════════════════════════════════════
echo   DZFP PDF 自动打印工具 - 单文件版打包工具
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
if exist "publish" (
    rmdir /s /q "publish"
)
if exist "bin" (
    rmdir /s /q "bin"
)
if exist "obj" (
    rmdir /s /q "obj"
)
echo ✅ 清理完成
echo.

echo [3/4] 正在编译并打包（这可能需要几分钟）...
echo    请耐心等待...
echo.

call dotnet publish -c Release -o publish

if %errorlevel% neq 0 (
    echo.
    echo ❌ 打包失败！请检查上方错误信息
    pause
    exit /b 1
)

echo.
echo ✅ 编译和打包成功！
echo.

echo [4/4] 检查生成的文件...
echo.
echo ┌─────────────────────────────────────────────┐
echo │  📦 生成的文件位置:                         │
echo │                                             │
echo │  publish\DzfpPdfPrinter.exe                 │
echo │                                             │
echo │  文件大小:                                  │

for %%A in ("publish\DzfpPdfPrinter.exe") do (
    set size=%%~zA
    set /a sizeMB=!size! / 1048576
    echo │  !sizeMB! MB                               │
)

echo └─────────────────────────────────────────────┘
echo.

echo ═══════════════════════════════════════════════════
echo   ✨ 打包完成！
echo ═══════════════════════════════════════════════════
echo.
echo 📌 使用方法:
echo    1. 进入 publish 文件夹
echo    2. 双击 DzfpPdfPrinter.exe 即可运行
echo    3. 可以将此 exe 文件复制到任何地方使用
echo.
echo 💡 提示:
echo    这是一个独立的 EXE 文件，包含所有依赖
echo    无需安装 .NET 运行时即可在任何 Windows 10/11 上运行
echo.
echo 是否打开 publish 文件夹？(Y/N)
choice /C YN /M "打开文件夹"
if %errorlevel% equ 1 (
    explorer "publish"
)

pause

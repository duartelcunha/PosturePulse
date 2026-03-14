@echo off
setlocal

:: ── Config ──────────────────────────────────────────────────────────
set GITHUB_USER=duartelcunha
set REPO_NAME=PosturePulse
set REPO_DESC=Lightweight Windows system tray utility for posture and hydration reminders. Built with WPF + .NET 8 featuring Mica backdrop and multi-monitor support.
set REPO_TOPICS=["wpf","dotnet","csharp","windows","health","productivity","system-tray","mica","desktop-app"]

:: ── Get token ───────────────────────────────────────────────────────
echo.
echo You need a GitHub Personal Access Token (classic) with 'repo' scope.
echo Create one at: https://github.com/settings/tokens/new
echo.
set /p GITHUB_TOKEN=Paste your token here: 

if "%GITHUB_TOKEN%"=="" (
    echo No token provided. Exiting.
    exit /b 1
)

:: ── Create repo via GitHub API ──────────────────────────────────────
echo.
echo Creating repository %GITHUB_USER%/%REPO_NAME% ...
curl -s -o response.json -w "%%{http_code}" ^
  -X POST https://api.github.com/user/repos ^
  -H "Authorization: token %GITHUB_TOKEN%" ^
  -H "Accept: application/vnd.github+json" ^
  -d "{\"name\":\"%REPO_NAME%\",\"description\":\"%REPO_DESC%\",\"private\":false,\"has_issues\":true,\"has_projects\":false,\"has_wiki\":false}" > http_code.txt

set /p HTTP_CODE=<http_code.txt
del http_code.txt

if "%HTTP_CODE%"=="201" (
    echo Repo created successfully!
) else if "%HTTP_CODE%"=="422" (
    echo Repo already exists, continuing with push...
) else (
    echo Failed to create repo. HTTP %HTTP_CODE%
    type response.json
    del response.json
    exit /b 1
)
del response.json 2>nul

:: ── Set topics ──────────────────────────────────────────────────────
curl -s -X PUT https://api.github.com/repos/%GITHUB_USER%/%REPO_NAME%/topics ^
  -H "Authorization: token %GITHUB_TOKEN%" ^
  -H "Accept: application/vnd.github+json" ^
  -d "{\"names\":%REPO_TOPICS%}" > nul

:: ── Git init and push ───────────────────────────────────────────────
echo.
echo Initializing git and pushing...

git init 2>nul
git add .
git commit -m "Initial commit: PosturePulse — posture and hydration reminder for Windows" --allow-empty

git remote remove origin 2>nul
git remote add origin https://%GITHUB_TOKEN%@github.com/%GITHUB_USER%/%REPO_NAME%.git
git branch -M main
git push -u origin main

:: ── Clean token from remote URL (security) ──────────────────────────
git remote set-url origin https://github.com/%GITHUB_USER%/%REPO_NAME%.git

echo.
echo Done! Repo live at: https://github.com/%GITHUB_USER%/%REPO_NAME%
echo.
pause

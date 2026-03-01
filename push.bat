@echo off
setlocal

:: SmokeScreen-ENGINE push script
:: Repo: https://github.com/LilToreyFTW/SMOKExSCREEN-ENGINE.git

echo Adding all changes...
git add -A

echo Committing...
git commit -m "Update: add vercel.json and fix Vercel build"

echo Pushing to GitHub...
git push origin main

echo Done.
pause

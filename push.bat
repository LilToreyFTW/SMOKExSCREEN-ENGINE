@echo off
setlocal

:: SmokeScreen-ENGINE push script
:: Repo: https://github.com/LilToreyFTW/SMOKExSCREEN-ENGINE.git

echo Adding SmokeScreen-ENGINE changes...
git add SmokeScreen-ENGINE/

echo Committing...
git commit -m "SmokeScreen-ENGINE update"

echo Pushing to GitHub...
git push origin main

echo Done.
pause

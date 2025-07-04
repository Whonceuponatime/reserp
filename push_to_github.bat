@echo off
echo Maritime ERP System - Git Push Script
echo ======================================

echo Navigating to project root...
cd /d "C:\Users\SAMSUNG\Desktop\P.Projects\reserp"

echo.
echo Adding all changes to Git...
git add .

echo.
echo Committing changes...
git commit -m "feat: Complete Maritime ERP System with fixed XAML issues

- ✅ Fixed all Material Design dependencies and XAML crashes
- ✅ Implemented functional Dashboard with real maritime data
- ✅ Created working Fleet Management system
- ✅ Fixed Systems Management interface
- ✅ Added comprehensive .gitignore for .NET projects
- ✅ Created professional README.md with full documentation
- ✅ Added MIT License
- ✅ Removed MaterialDesignThemes packages
- ✅ Applied clean WPF styling throughout application
- ✅ Ensured stable application without crashes
- ✅ Clean Architecture implementation maintained"

echo.
echo Checking if origin remote exists...
git remote -v

echo.
echo Adding GitHub remote if not exists...
git remote add origin https://github.com/whonceuponatime/reserp.git 2>nul

echo.
echo Setting upstream and pushing to GitHub...
git push -u origin main

echo.
echo ======================================
echo Git push completed!
echo Check your repository at: https://github.com/whonceuponatime/reserp
echo ======================================
pause 
@echo off
echo ==============================================
echo   BUILDING LATEX REPORT (report_final.tex)
echo ==============================================

echo [1/4] Running xelatex (First Pass)...
xelatex -interaction=nonstopmode report_final.tex
if errorlevel 1 goto error

echo [2/4] Running biber (Bibliography)...
biber report_final
if errorlevel 1 goto error

echo [3/4] Running xelatex (Second Pass)...
xelatex -interaction=nonstopmode report_final.tex
if errorlevel 1 goto error

echo [4/4] Running xelatex (Final Pass)...
xelatex -interaction=nonstopmode report_final.tex
if errorlevel 1 goto error

echo.
echo ==============================================
echo BUILD SUCCESSFUL! 
echo Mở file report_final.pdf để xem kết quả.
echo ==============================================
exit /b 0

:error
echo.
echo ==============================================
echo BUILD FAILED! Vui lòng kiểm tra lại lỗi ở trên.
echo ==============================================
exit /b 1

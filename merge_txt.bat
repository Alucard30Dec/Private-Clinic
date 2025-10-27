@echo off
setlocal enabledelayedexpansion
set "ROOT=E:\Study\WebApp\Project\Private-Clinic\Clinic"
set "OUT=E:\Study\WebApp\Project\Private-Clinic\ALL_FILES_TEXT.txt"
del "%OUT%" 2>nul

rem chỉ lấy một số phần mở rộng thường dùng của web/.NET
for /r "%ROOT%" %%F in (*.cs *.cshtml *.js *.ts *.json *.xml *.config *.sln *.csproj *.props *.targets *.md *.txt *.sql) do (
  set "P=%%~fF"
  rem loại trừ một số thư mục nặng
  echo !P! | findstr /i /r "\\\.git\\\|\\\node_modules\\\|\\\bin\\\|\\\obj\\\|\\\packages\\\|\\\dist\\\|\\\build\\\|\\\wwwroot\\\lib\\\" >nul && (
    rem skip
  ) || (
    echo.>>"%OUT%"
    echo ===== Đường dẫn: %%~fF =====>>"%OUT%"
    type "%%~fF" >>"%OUT%"
  )
)
echo Done ^> "%OUT%"

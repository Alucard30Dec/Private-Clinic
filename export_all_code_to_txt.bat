@echo off
setlocal EnableExtensions EnableDelayedExpansion

:: ========= CẤU HÌNH =========
set "ROOT=E:\Study\WebApp\Project\Private-Clinic\Clinic"
set "OUT=E:\Study\WebApp\Project\Private-Clinic\ALL_FILES_TEXT.txt"

:: Danh sách đuôi file cần xuất (có thể chỉnh tuỳ ý) - áp dụng CHỈ khi file nằm trong các thư mục được chọn
set "EXTS=.cs .cshtml .js .ts .tsx .css .scss .sql .json .xml .config .csproj .sln .md .cmd .bat .ps1"

:: Thư mục cần bỏ qua (ngăn trùng lặp mã build)
set "SKIPFOLDERS=\bin\ \obj\ \node_modules\ \packages\ \dist\ \build\ \out\ \wwwroot\lib\ .git\"

:: Chỉ bao gồm các thư mục sau
set "INCFOLDERS=\App_Start\ \Areas\ \Controllers\ \Migrations\ \Models\ \Views\"

:: ========= KHỞI TẠO =========
for %%D in ("%ROOT%") do if not exist "%%~fD" (
  echo [Loi] Khong tim thay thu muc ROOT: "%ROOT%"
  exit /b 1
)

:: Tạo thư mục cha của OUT nếu chưa có
for %%P in ("%OUT%") do if not exist "%%~dpP" mkdir "%%~dpP" >nul 2>&1

:: Xoá file OUT cũ (nếu có) để ghi mới từ đầu
if exist "%OUT%" del /f /q "%OUT%" >nul 2>&1

:: Header cho file tổng
>> "%OUT%" echo ======= EXPORT CODE =======
>> "%OUT%" echo Thoi gian: %date% %time%
>> "%OUT%" echo Thu muc goc: %ROOT%
>> "%OUT%" echo Chi gom: App_Start, Areas, Controllers, Migrations, Models, Views, Global.asax(.cs), Web.config
>> "%OUT%" echo.

:: ========= MẪU: XUẤT 1 FILE CỤ THỂ (GIỮ NGUYÊN) =========
set "ONEFILE=E:\Study\WebApp\Project\Private-Clinic\Clinic\Migrations\ClinicMigrations\202510280840531_SyncModel_AppointmentRequest.cs"

if exist "%ONEFILE%" (
  >> "%OUT%" echo ---------- FILE ----------
  >> "%OUT%" echo %ONEFILE%
  >> "%OUT%" echo --------------------------
  powershell -NoProfile -Command ^
    "Get-Content -LiteralPath '%ONEFILE%' -Raw | Out-File -FilePath '%OUT%' -Append -Encoding utf8"
  >> "%OUT%" echo.
) else (
  >> "%OUT%" echo [Canh bao] Khong tim thay file mau: %ONEFILE%
  >> "%OUT%" echo.
)

:: ========= XUẤT TẤT CẢ FILE THEO YÊU CẦU =========
echo Dang quet ma nguon trong "%ROOT%" ...
for /r "%ROOT%" %%F in (*) do (
  set "FULL=%%~fF"
  set "NAME=%%~nxF"
  set "EXT=%%~xF"

  :: Bỏ qua các thư mục không mong muốn
  set "SKIP=0"
  for %%S in (%SKIPFOLDERS%) do (
    echo(!FULL!| find /i "%%~S" >nul && set "SKIP=1"
  )
  if "!SKIP!"=="1" (
    rem Bi bo qua do nam trong thu muc skip
  ) else (
    :: Xac dinh co nam trong cac thu muc can gom khong
    set "INCLUDE=0"
    for %%I in (%INCFOLDERS%) do (
      echo(!FULL!| find /i "%%~I" >nul && set "INCLUDE=1"
    )

    :: Luon gom Global.asax, Global.asax.cs, Web.config o bat ky dau
    if /i "!NAME!"=="Web.config" set "INCLUDE=2"
    if /i "!NAME!"=="Global.asax"  set "INCLUDE=2"
    if /i "!NAME!"=="Global.asax.cs" set "INCLUDE=2"

    if "!INCLUDE!"=="0" (
      rem Khong nam trong cac thu muc duoc chon va khong phai file dac biet -> bo qua
    ) else if "!INCLUDE!"=="2" (
      :: File dac biet: ghi truc tiep khong can check EXTS
      >> "%OUT%" echo ---------- FILE ----------
      >> "%OUT%" echo %%~fF
      >> "%OUT%" echo --------------------------
      powershell -NoProfile -Command ^
        "Get-Content -LiteralPath '%%~fF' -Raw | Out-File -FilePath '%OUT%' -Append -Encoding utf8"
      >> "%OUT%" echo.
    ) else (
      :: INCLUDE==1: nam trong thu muc duoc chon -> chi xuat neu dung EXT
      set "MATCH=0"
      for %%E in (%EXTS%) do (
        if /i "%%E"=="!EXT!" set "MATCH=1"
      )
      if "!MATCH!"=="1" (
        >> "%OUT%" echo ---------- FILE ----------
        >> "%OUT%" echo %%~fF
        >> "%OUT%" echo --------------------------
        powershell -NoProfile -Command ^
          "Get-Content -LiteralPath '%%~fF' -Raw | Out-File -FilePath '%OUT%' -Append -Encoding utf8"
        >> "%OUT%" echo.
      )
    )
  )
)

>> "%OUT%" echo ======= HET =======

echo Xong! Tat ca noi dung da duoc xuat vao:
echo   "%OUT%"
endlocal

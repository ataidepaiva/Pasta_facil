@echo off
cd /d "%~dp0"
where dotnet >nul 2>&1
if errorlevel 1 (
  echo O SDK do .NET nao foi encontrado. Instale o SDK do .NET 8 e tente novamente.
  pause
  exit /b 1
)
dotnet publish PastaFacil.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o Publicado
if errorlevel 1 (
  echo Falha ao gerar o executavel. Leia as mensagens acima.
  pause
  exit /b 1
)
echo Executavel gerado na pasta Publicado.
pause

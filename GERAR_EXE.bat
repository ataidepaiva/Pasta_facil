@echo off
cd /d "%~dp0"
py -3 -m pip install --upgrade pyinstaller
py -3 -m PyInstaller --onefile --windowed --name PastaFacil pastafacil.py
pause

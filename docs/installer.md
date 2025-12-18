# Инсталлятор

WinFlow включает консольный инсталлятор для автоматической установки и настройки в Windows.

## Возможности инсталлятора

- ✅ Установка в `%LOCALAPPDATA%\WinFlow`
- ✅ Ассоциация файлов `.wflow`
- ✅ Добавление в `PATH`
- ✅ Создание ярлыка на рабочем столе
- ✅ Деинсталляция с очисткой

## Установка

### Вариант 1: Автоматическая установка

```powershell
# Клонировать и собрать проект
git clone https://github.com/silasWorked/WinFlow.git
cd WinFlow
dotnet build WinFlow.sln -c Release

# Запустить инсталлятор
dotnet run --project WinFlow/WinFlow.Installer.Cli
```

**Что делает инсталлятор:**

1. Копирует `WinFlow.Cli.exe` и зависимости в `%LOCALAPPDATA%\WinFlow`
2. Регистрирует ассоциацию `.wflow` файлов
3. Добавляет путь в `PATH` пользователя
4. Создает демо-скрипт на рабочем столе (опционально)

### Вариант 2: Установка с параметрами

```powershell
# Установка в кастомную директорию
dotnet run --project WinFlow/WinFlow.Installer.Cli -- --dir "C:\Tools\WinFlow"

# Установка с созданием демо на рабочем столе
dotnet run --project WinFlow/WinFlow.Installer.Cli -- --create-desktop-demo

# Установка с указанием пути к Release сборке
dotnet run --project WinFlow/WinFlow.Installer.Cli -- --source-dir "WinFlow\WinFlow.Cli\bin\Release\net8.0"
```

### Вариант 3: Ручная установка

```powershell
# 1. Собрать проект
dotnet build WinFlow.sln -c Release

# 2. Скопировать файлы
$targetDir = "$env:LOCALAPPDATA\WinFlow"
New-Item -ItemType Directory -Path $targetDir -Force
Copy-Item "WinFlow\WinFlow.Cli\bin\Release\net8.0\*" $targetDir -Recurse

# 3. Добавить в PATH
$currentPath = [Environment]::GetEnvironmentVariable("Path", "User")
if ($currentPath -notlike "*$targetDir*") {
    [Environment]::SetEnvironmentVariable("Path", "$currentPath;$targetDir", "User")
}

# 4. Ассоциация файлов
$regPath = "HKCU:\Software\Classes\.wflow"
New-Item -Path $regPath -Force
Set-ItemProperty -Path $regPath -Name "(Default)" -Value "WinFlowScript"

$regPath = "HKCU:\Software\Classes\WinFlowScript\shell\open\command"
New-Item -Path $regPath -Force
Set-ItemProperty -Path $regPath -Name "(Default)" -Value "`"$targetDir\WinFlow.Cli.exe`" `"%1`""
```

## Ассоциация файлов .wflow

После установки `.wflow` файлы ассоциированы с WinFlow:

```powershell
# Двойной клик на .wflow файл → автоматический запуск
# Или из командной строки:
script.wflow

# Эквивалентно:
WinFlow.Cli.exe script.wflow
```

### Проверка ассоциации

```powershell
# Проверить ассоциацию
$wflowPath = (Get-ItemProperty "HKCU:\Software\Classes\.wflow")."(Default)"
$commandPath = (Get-ItemProperty "HKCU:\Software\Classes\$wflowPath\shell\open\command")."(Default)"

Write-Host "Association: $wflowPath"
Write-Host "Command: $commandPath"
```

### Ручная ассоциация (для разработки)

```powershell
# Для Debug сборки
powershell -ExecutionPolicy Bypass -File WinFlow/WinFlow.Installer/register-wflow.ps1
```

Скрипт `register-wflow.ps1` регистрирует `.wflow` с текущей Debug сборкой.

## Добавление в PATH

После установки WinFlow доступен из любой директории:

```powershell
# Открыть новый PowerShell терминал и проверить
WinFlow.Cli --version

# Запустить скрипт из любого места
WinFlow.Cli myscript.wflow
```

### Ручное добавление в PATH

```powershell
# Добавить в PATH пользователя
$winflowPath = "$env:LOCALAPPDATA\WinFlow"
$currentPath = [Environment]::GetEnvironmentVariable("Path", "User")

if ($currentPath -notlike "*$winflowPath*") {
    [Environment]::SetEnvironmentVariable(
        "Path", 
        "$currentPath;$winflowPath", 
        "User"
    )
    Write-Host "Added to PATH: $winflowPath"
    Write-Host "Restart terminal to apply changes"
}
```

## Создание демо-скрипта

Инсталлятор может создать демо-скрипт на рабочем столе:

```powershell
dotnet run --project WinFlow/WinFlow.Installer.Cli -- --create-desktop-demo
```

Создается файл `Desktop\WinFlow-Demo.wflow`:

```wflow
#/// WinFlow Demo Script

echo Welcome to WinFlow!
echo This is a demo script showing various features

// Variables
env set name=USER value="Windows User"
echo Hello ${USER}!

// File operations
file write path="demo-output.txt" content="Demo executed successfully"
echo Created demo-output.txt

// Process execution
process.exec file="cmd.exe" args="/c echo WinFlow is awesome!"

echo Demo complete!
```

Двойной клик на файл → автоматический запуск.

## Деинсталляция

### Автоматическая деинсталляция

```powershell
dotnet run --project WinFlow/WinFlow.Installer.Cli -- --uninstall
```

**Что делает деинсталлятор:**

1. Удаляет файлы из `%LOCALAPPDATA%\WinFlow`
2. Удаляет ассоциацию `.wflow` из реестра
3. Удаляет путь из `PATH` пользователя
4. Опционально удаляет демо-скрипты

### Деинсталляция с параметрами

```powershell
# Указать кастомную директорию
dotnet run --project WinFlow/WinFlow.Installer.Cli -- --uninstall --dir "C:\Tools\WinFlow"

# Удалить только ассоциацию файлов (оставить программу)
# (пока не реализовано, используйте ручную очистку)
```

### Ручная деинсталляция

```powershell
# 1. Удалить директорию
$targetDir = "$env:LOCALAPPDATA\WinFlow"
Remove-Item $targetDir -Recurse -Force

# 2. Удалить из PATH
$currentPath = [Environment]::GetEnvironmentVariable("Path", "User")
$newPath = ($currentPath -split ';' | Where-Object { $_ -ne $targetDir }) -join ';'
[Environment]::SetEnvironmentVariable("Path", $newPath, "User")

# 3. Удалить ассоциацию файлов
Remove-Item "HKCU:\Software\Classes\.wflow" -Recurse -Force
Remove-Item "HKCU:\Software\Classes\WinFlowScript" -Recurse -Force

# 4. Удалить демо-скрипты (опционально)
Remove-Item "$env:USERPROFILE\Desktop\WinFlow-Demo.wflow" -Force
```

## Обновление версии

Для обновления на новую версию:

```powershell
# 1. Деинсталлировать старую версию
dotnet run --project WinFlow/WinFlow.Installer.Cli -- --uninstall

# 2. Обновить код
cd WinFlow
git pull origin main
dotnet build WinFlow.sln -c Release

# 3. Установить новую версию
dotnet run --project WinFlow/WinFlow.Installer.Cli
```

## Portable установка (без инсталлятора)

Для использования без установки:

```powershell
# 1. Собрать Release
dotnet build WinFlow.sln -c Release

# 2. Скопировать в любую директорию
Copy-Item "WinFlow\WinFlow.Cli\bin\Release\net8.0" "C:\PortableApps\WinFlow" -Recurse

# 3. Запускать напрямую
C:\PortableApps\WinFlow\WinFlow.Cli.exe script.wflow
```

**Преимущества portable:**
- Не требует прав администратора
- Не изменяет систему
- Легко удалить (просто удалить папку)

**Недостатки:**
- Нет ассоциации файлов
- Не в PATH (нужно указывать полный путь)

## Реестр Windows

Инсталлятор создает следующие ключи реестра:

### Ассоциация .wflow

```
HKEY_CURRENT_USER\Software\Classes\.wflow
    (Default) = "WinFlowScript"

HKEY_CURRENT_USER\Software\Classes\WinFlowScript
    (Default) = "WinFlow Script"
    
HKEY_CURRENT_USER\Software\Classes\WinFlowScript\DefaultIcon
    (Default) = "%LOCALAPPDATA%\WinFlow\WinFlow.Cli.exe,0"
    
HKEY_CURRENT_USER\Software\Classes\WinFlowScript\shell\open\command
    (Default) = "%LOCALAPPDATA%\WinFlow\WinFlow.Cli.exe" "%1"
```

### Проверка реестра

```powershell
# Проверить ключи
Get-ItemProperty "HKCU:\Software\Classes\.wflow"
Get-ItemProperty "HKCU:\Software\Classes\WinFlowScript\shell\open\command"
```

## Устранение проблем

### .wflow файлы не открываются

```powershell
# 1. Проверить ассоциацию
$assoc = (Get-ItemProperty "HKCU:\Software\Classes\.wflow" -ErrorAction SilentlyContinue)."(Default)"
if (-not $assoc) {
    Write-Host "Association not found, reinstall WinFlow"
}

# 2. Переустановить ассоциацию
powershell -ExecutionPolicy Bypass -File WinFlow/WinFlow.Installer/register-wflow.ps1
```

### WinFlow.Cli не найден в PATH

```powershell
# 1. Проверить PATH
$env:Path -split ';' | Select-String "WinFlow"

# 2. Если не найден, добавить
$winflowPath = "$env:LOCALAPPDATA\WinFlow"
[Environment]::SetEnvironmentVariable("Path", "$env:Path;$winflowPath", "User")

# 3. Перезапустить терминал
exit
```

### Ошибка "Access denied"

Инсталлятор работает без прав администратора, но если возникает ошибка:

1. Закройте все экземпляры WinFlow.Cli.exe
2. Проверьте антивирус (может блокировать)
3. Запустите PowerShell от имени пользователя (не админа)

## См. также

- [Быстрый старт](quickstart.md) — начало работы
- [CLI](cli.md) — параметры командной строки
- [Ассоциация файлов](association.md) — детали ассоциации .wflow

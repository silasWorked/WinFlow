# WinFlow — обзор и быстрый старт

WinFlow — это простой скриптовый рантайм для Windows с лаконичным синтаксисом (.wflow). Цель — заменить рутинные .bat/.ps1 задачами более высокого уровня: шаги, команды, модули.

## Установка (dev)

- Требуется .NET 8 SDK
- Сборка:

```powershell
cd C:\Users\silas\RiderProjects\WinFlow
 dotnet build WinFlow.sln -c Debug
```

- Быстрый запуск CLI:

```powershell
# Запустить встроенный демо (все возможности)
 dotnet run --project WinFlow/WinFlow.Cli -- demo.wflow --verbose

# Создать и запустить пользовательский скрипт
ni my-script.wflow -ItemType File
"echo message=`"My first WinFlow script`"" | Set-Content my-script.wflow -Encoding UTF8
 dotnet run --project WinFlow/WinFlow.Cli -- my-script.wflow
```

## Концепции

- Task → логическая операция (набор шагов)
- Step → последовательность команд
- Command → действие модуля (например, `echo`, `env set`, `file write`)
- ExecutionContext → переменные окружения, режимы (`--dry-run`, `--verbose`), рабочая папка

В MVP парсер группирует все команды файла в один Task с одним Step.

## Пример .wflow (MVP — текущее состояние)

```text
#/// WinFlow Demo
# Демонстрация всех возможностей языка

echo message="Task 1: Environment setup"
env set name=APP_NAME value="WinFlow"
env set name=APP_VERSION value="0.1.0"
env print

echo message="Task 2: File operations"
file write path="config.txt" content="APP_NAME=WinFlow"
file append path="config.txt" content="VERSION=0.1.0"

echo message="Task 3: Process execution"
process.run file="cmd.exe" args="/c echo Async process"
process.exec file="cmd.exe" args="/c echo Sync process"

echo message="Complete!"
```

Ожидаемый вывод:

```text
[task] demo
message="Task 1: Environment setup"
env set APP_NAME='WinFlow'
env set APP_VERSION='0.1.0'
APP_NAME=WinFlow
APP_VERSION=0.1.0
message="Task 2: File operations"
wrote config.txt
appended config.txt
message="Task 3: Process execution"
process run cmd.exe started (PID: 12345)
Async process
process exec cmd.exe exited with code 0
message="Complete!"
```

## Что дальше

- Секции `task <name>:` и `step`
- Аргументы в формате key=value и quoted strings
- Модули: Env, File, Process, Registry
- Поддержка переменных и подстановок

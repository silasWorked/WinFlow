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
# создать пример (если его нет)
ni demo.wflow -ItemType File -Force
"// WinFlow demo`n`necho \"Hello from WinFlow!\"`nnoop`necho \"Done.\"" | Set-Content demo.wflow -Encoding UTF8

# запустить
 dotnet run --project WinFlow/WinFlow.Cli -- demo.wflow --verbose
```

## Концепции

- Task → логическая операция (набор шагов)
- Step → последовательность команд
- Command → действие модуля (например, `echo`, `env set`, `file write`)
- ExecutionContext → переменные окружения, режимы (`--dry-run`, `--verbose`), рабочая папка

В MVP парсер группирует все команды файла в один Task с одним Step.

## Пример .wflow (MVP)

```text
// комментарии: // или #
# пустые строки игнорируются

echo "Hello from WinFlow!"
noop
echo "Done."
```

Ожидаемый вывод:

```text
[task] demo
Hello from WinFlow!
noop
Done.
```

## Что дальше

- Секции `task <name>:` и `step`
- Аргументы в формате key=value и quoted strings
- Модули: Env, File, Process, Registry
- Поддержка переменных и подстановок

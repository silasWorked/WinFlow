# WinFlow

[![CI](https://github.com/silasWorked/WinFlow/actions/workflows/ci.yml/badge.svg)](https://github.com/silasWorked/WinFlow/actions/workflows/ci.yml)
[![Release](https://github.com/silasWorked/WinFlow/actions/workflows/release.yml/badge.svg)](https://github.com/silasWorked/WinFlow/actions/workflows/release.yml)

WinFlow — экспериментальный DSL/скриптовый рантайм для Windows (.wflow).

## Структура решений/проектов

- WinFlow.Core — ядро (AST/Parsing/Runtime/Model)
- WinFlow.Cli — консольный запуск (`winflow.exe`)
- WinFlow.ShellHost — хост для дабл‑клика (минимальный консольный prompt)
- WinFlow.Installer — помощники по ассоциации `.wflow`

## Минимальный поток исполнения

1. `winflow <script.wflow> [--dry-run] [--verbose]`
2. Парсинг: `WinFlowParser.Parse(path)` → `List<FlowTask>`
3. Выполнение: `TaskExecutor.Run(tasks, context)`

На данный момент парсер возвращает заглушку: одну задачу с командой `echo`.

## Сборка

```powershell
cd WinFlow
 dotnet build WinFlow.sln -c Debug
```

## Запуск CLI (демо)

```powershell
# Создайте пустой файл demo.wflow где угодно
ni demo.wflow -ItemType File

# Запуск (verbose)
dotnet run --project WinFlow/WinFlow.Cli -- demo.wflow --verbose
```

Ожидаемый вывод — одна задача с шагами `echo`, `noop`, и др.

## Ассоциация .wflow (dev)

```powershell
# Свяжет .wflow c WinFlow.Cli.exe в Debug
powershell -ExecutionPolicy Bypass -File WinFlow/WinFlow.Installer/register-wflow.ps1
```

Для прода/установщика смотрите `WinFlow.Installer/WinFlow.Script-HKCR.reg` (пример записи в HKCR).

## Дальнейшие шаги

- Спецификация языка и реальный парсер
- Модули команд: Env, File, Process, Registry
- Логи/телеметрия, строгие коды возврата, тесты

## Релизы
- Готовые сборки публикуются в разделе Releases (workflow `release.yml` собирает win-x64 ZIP из WinFlow.Cli).

## Документация

- Обзор и быстрый старт: [docs/overview.md](docs/overview.md)
- Язык (MVP и план): [docs/language.md](docs/language.md)
- Команды (справочник): [docs/commands.md](docs/commands.md)
- CLI (аргументы и примеры): [docs/cli.md](docs/cli.md)
- Ассоциация файлов: [docs/association.md](docs/association.md)

## Инсталлятор (консоль)
- Установка по умолчанию в `%LOCALAPPDATA%\WinFlow`, ассоциация `.wflow`, добавление в `PATH`:
```powershell
dotnet run --project WinFlow/WinFlow.Installer.Cli
```
- Дополнительно создать демо на рабочем столе:
```powershell
dotnet run --project WinFlow/WinFlow.Installer.Cli -- --create-desktop-demo
```
- Деинсталляция:
```powershell
dotnet run --project WinFlow/WinFlow.Installer.Cli -- --uninstall --dir "%LOCALAPPDATA%\WinFlow"
```

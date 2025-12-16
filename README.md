# WinFlow

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

Ожидаемый вывод — задача с `echo` (заглушка парсера).

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

## Документация

- Обзор и быстрый старт: [docs/overview.md](docs/overview.md)
- Язык (MVP и план): [docs/language.md](docs/language.md)
- Команды (справочник): [docs/commands.md](docs/commands.md)
- CLI (аргументы и примеры): [docs/cli.md](docs/cli.md)
- Ассоциация файлов: [docs/association.md](docs/association.md)

# WinFlow CLI

Запуск сценариев `.wflow` через консольный интерфейс.

## Синтаксис

```text
winflow <script.wflow> [--dry-run] [--verbose]
```

- `<script.wflow>` — путь к файлу скрипта
- `--dry-run` — не исполнять, только печатать, что было бы выполнено
- `--verbose` (`-v`) — подробный вывод (диагностика, трассировка)

## Примеры

```powershell
# Обычный запуск
 dotnet run --project WinFlow/WinFlow.Cli -- demo.wflow

# Проверка без выполнения
 dotnet run --project WinFlow/WinFlow.Cli -- demo.wflow --dry-run

# Подробный вывод
 dotnet run --project WinFlow/WinFlow.Cli -- demo.wflow --verbose
```

## Коды возврата (план)
- `0` — успех
- `1-2` — проблемы с аргументами/файлом
- `10+` — ошибки выполнения

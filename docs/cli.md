---
title: CLI
nav_order: 4
---

# WinFlow CLI

Запуск сценариев `.wflow` и управление рабочей средой через консольный интерфейс.

## Основные команды

### Интерактивный шелл

```text
winflow
winflow shell
```

- Откроет отдельное окно «WinFlow Shell» с приглашением `winflow>`
- Поддерживаются встроенные команды: `help`, `exit`, `clear`, `pwd`, `cd <path>`, `verbose on|off`, `dry on|off`, `info`, `list`, `run <file.wflow>`
- Можно исполнять одиночные команды языка, например: `echo message="Hi"`, `file write path=out.txt content=...`

### Запуск скрипта

```text
winflow <script.wflow> [OPTIONS]
```

- `<script.wflow>` — путь к файлу скрипта (обязательный)
- `--dry-run` — симулировать исполнение без реальных изменений
- `--verbose`, `-v` — подробный вывод (диагностика, трассировка)

### Информационные команды

```text
winflow --version          Показать версию
winflow --help             Справка по использованию
winflow info               Системная информация (версия, ОС, архитектура)
winflow list               Список доступных встроенных команд
```

## Примеры

### Запуск скрипта

```powershell
# Обычный запуск
winflow demo.wflow

# Проверка без выполнения (dry-run)
winflow demo.wflow --dry-run

# Подробный вывод
winflow demo.wflow --verbose
```

### Информационные команды

```powershell
# Версия
winflow --version
# Вывод: WinFlow 0.1.0

# Справка
winflow --help

# Системная информация
winflow info
# Версия, .NET Runtime, ОС, архитектура, текущая папка, пользователь

# Доступные встроенные команды
winflow list
# Core, Environment, File, Process команды с описанием
```

## Коды возврата

- `0` — успех (скрипт выполнен или команда обработана)
- `1` — ошибка аргументов/использования
- `2` — файл скрипта не найден
- `10+` — ошибки во время исполнения скрипта



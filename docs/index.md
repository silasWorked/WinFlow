---
layout: default
title: WinFlow
nav_order: 0
---

# WinFlow

Простой DSL и рантайм для автоматизации Windows (.wflow).

[![CI](https://github.com/silasWorked/WinFlow/actions/workflows/ci.yml/badge.svg)](https://github.com/silasWorked/WinFlow/actions/workflows/ci.yml)
[![Release](https://github.com/silasWorked/WinFlow/actions/workflows/release.yml/badge.svg)](https://github.com/silasWorked/WinFlow/actions/workflows/release.yml)

## Быстрый старт

```powershell
# Клонировать и собрать
cd C:\Users\silas\RiderProjects\WinFlow
 dotnet build WinFlow.sln -c Debug

# Запустить демо
 dotnet run --project WinFlow/WinFlow.Cli -- demo.wflow --verbose
```

- Руководство: [Обзор и быстрый старт](overview.md)
- Справочник: [Команды](commands.md)
- CLI: [Параметры и примеры](cli.md)
- Ассоциация файлов: [Ассоциация .wflow](association.md)

## Возможности
- Лаконичный синтаксис: команды и аргументы `key=value`, кавычки
- Модули: `env`, `file`, `process` (планируется `registry`)
- CLI и Инсталлятор: запуск из консоли и установщик без прав администратора
- Интеграция с Windows: `.wflow` ассоциация, переменные окружения, процессы
- CI/CD: сборка, релизы, синхронизация Wiki и документации

## Пример скрипта
```wflow
#/// WinFlow Demo

# Environment
env set name=APP_NAME value="WinFlow"
env print

# Files
file write path="config.txt" content="APP_NAME=WinFlow"

# Process
process.exec file="cmd.exe" args="/c echo Hello"
```

Скачать последнюю версию: https://github.com/silasWorked/WinFlow/releases/latest

---
title: FAQ
nav_order: 9
---

# FAQ

## Нужен ли установленный .NET Runtime?
Сейчас требуется .NET 8 Runtime, но планируется self-contained публикация.

## Работают ли команды Windows (cmd/powershell)?
Да, используйте `process.run` или `process.exec`.

## Где хранится рабочая папка?
По умолчанию — папка скрипта (`WorkingDirectory` в ExecutionContext).

## Как включить подробный вывод?
Добавьте флаг `--verbose` (или `-v`) при запуске CLI.

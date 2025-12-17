---
title: Расширенные возможности
nav_order: 8
---

# Расширенные возможности

Эта страница описывает планируемые фичи и расширения языка.

## Переменные и подстановки
- Синтаксис: `${NAME}` внутри значений аргументов
- Источник: переменные из ExecutionContext (`env set`), переменные шага/таска
- Подстановка в `file`, `process`, `echo`

## Управление потоком
- `if <expr>: ... else: ...` — условные блоки
- `foreach VAR in LIST: ...` — циклы по спискам

## Многозадачные сценарии
- Поддержка секций `task <name>:` и `step:` для группировки
- Запуск выбранной задачи: `winflow script.wflow --task <name>`

## Модуль registry (план)
- `reg.set`/`reg.get`/`reg.delete` для HKCU/HKLM
- Типы: `REG_SZ`, `REG_DWORD`, `REG_QWORD`, `REG_MULTI_SZ`

## Публикация без рантайма
- Self-contained публикация (`PublishSelfContained=true`) для win-x64
- Чистый exe без установленного .NET Runtime

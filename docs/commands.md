---
title: Команды (справочник)
nav_order: 3
---

# Команды WinFlow

Справочник по встроенным командам. Все аргументы вида `key=value`.
Подстановки переменных: `${NAME}`.

## Базовые

### echo
- Назначение: вывести строку в лог
- Сигнатура: `echo <text>`
- Примеры: `echo "Hello"`, `echo Done`

### noop
- Назначение: заглушка, ничего не делает
- Сигнатура: `noop`

## Env

### env set
- Назначение: установить переменную контекста
- Сигнатура: `env set name=<VAR> value=<VALUE>` (или `key=`)
- Пример: `env set name=GREETING value="Hello"`

### env unset
- Назначение: удалить переменную из контекста
- Сигнатура: `env unset name=<VAR>`

### env print
- Назначение: вывести все переменные
- Сигнатура: `env print`

## File

### file write
- Назначение: создать/перезаписать файл
- Сигнатура: `file write path=<FILE> content=<TEXT>`

### file append
- Назначение: дописать в конец файла
- Сигнатура: `file append path=<FILE> content=<TEXT>`

### file mkdir
- Назначение: создать каталог
- Сигнатура: `file mkdir path=<DIR>`

### file delete
- Назначение: удалить файл/каталог
- Сигнатура: `file delete path=<FILE|DIR> [recursive=true|false]`

### file copy
- Назначение: скопировать файл
- Сигнатура: `file copy src=<FILE> dst=<FILE> [overwrite=true|false]`

## Process

### process run
- Назначение: запустить процесс (не ждать завершения)
- Сигнатура: `process run file=<EXE> args=<ARGS>`

### process exec
- Назначение: запустить процесс и ждать завершения; печатает stdout/stderr
- Сигнатура: `process exec file=<EXE> args=<ARGS>`

## Registry (Windows)

### reg set
- Назначение: создать/обновить значение реестра
- Сигнатура: `reg set hive=<HKCU|HKLM> key=<PATH> name=<NAME> value=<VAL> [type=STRING|DWORD|QWORD|MULTI|EXPAND]`
- По умолчанию: `hive=HKCU`, `type=STRING`

### reg get
- Назначение: получить значение реестра (печатает в лог)
- Сигнатура: `reg get hive=<HKCU|HKLM> key=<PATH> name=<NAME>`

### reg delete
- Назначение: удалить значение или ключ
- Сигнатура: `reg delete hive=<HKCU|HKLM> key=<PATH> [name=<NAME>]`
- Если указан `name` — удаляет значение; иначе удаляет ключ целиком

## Sleep

### sleep ms
- Назначение: задержка в миллисекундах
- Сигнатура: `sleep ms ms=<INT>`

### sleep sec
- Назначение: задержка в секундах
- Сигнатура: `sleep sec sec=<INT>`

## Net

### net download
- Назначение: скачать файл по HTTP(S)
- Сигнатура: `net download url=<URL> path=<FILE>`

## Loop

### loop repeat
- Назначение: выполнить тело N раз
- Сигнатура: `loop repeat count=<N> body="<команда>"`
- Переменные: `${index}`

### loop foreach
- Назначение: обойти элементы списка
- Сигнатура: `loop foreach items="a,b,c" [sep=","] [var=item] body="<команда>"`
- Переменные: `${index}`, `${<var>}`

---

# Для разработчиков
- Регистрация обработчиков команд — в классе `CommandDispatcher`
- Обработчик: `Action<FlowCommand, ExecutionContext>`

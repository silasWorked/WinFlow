
---

# WinFlow v0.2.3 — Новые модули

## config — Конфигурационные файлы (INI)

Работа с INI конфигурациями:

```wflow
config.create var=CFG
config.set config=${CFG} section=App key=Name value=MyApp
config.get config=${CFG} section=App key=Name var=APP_NAME
config.write config=${CFG} file=settings.ini
```

**Команды:**
- `config.create [var=<name>]` — создать конфиг
- `config.read file=<path> [var=<name>]` — прочитать INI файл
- `config.get config=<id> section=<n> key=<n> [default=<v>] [var=<v>]` — получить значение
- `config.set config=<id> section=<n> key=<n> value=<v>` — установить значение
- `config.write config=<id> file=<path>` — сохранить в файл

---

## csv — Работа с CSV таблицами

Чтение, создание, фильтрация и сортировка CSV:

```wflow
csv.create headers="name,email,age" var=PEOPLE
csv.add_row data=${PEOPLE} values="Alice,alice@mail.com,30"
csv.filter data=${PEOPLE} column=name value=Alice var=FOUND
csv.write data=${PEOPLE} file=people.csv
```

**Команды:**
- `csv.create headers=<a,b,c> [var=<name>]` — создать таблицу
- `csv.read file=<path> [has_header=true] [var=<name>]` — прочитать CSV
- `csv.add_row data=<id> values=<v1,v2> [var=<name>]` — добавить строку
- `csv.get_field row=<rowId> field=<name> [var=<name>]` — получить ячейку
- `csv.filter data=<id> column=<n> value=<v> [var=<name>]` — отфильтровать
- `csv.sort data=<id> column=<n> [order=asc|desc] [var=<name>]` — отсортировать
- `csv.write data=<id> file=<path>` — сохранить CSV

---

## xml — Работа с XML документами

Парсинг и генерация XML:

```wflow
xml.parse file=data.xml var=DOC
xml.get data=${DOC} xpath="/root/user/name" var=NAME
xml.add_element parent=${DOC} tag=log var=LOG
xml.write data=${DOC} file=output.xml
```

**Команды:**
- `xml.create [var=<name>]` — создать документ
- `xml.parse file=<path> [var=<name>]` — прочитать XML
- `xml.add_element parent=<id> tag=<n> [var=<name>]` — добавить элемент
- `xml.set_attribute element=<id> name=<n> value=<v>` — атрибут
- `xml.get data=<id> xpath=<expr> [var=<name>]` — получить по XPath
- `xml.write data=<id> file=<path>` — сохранить XML

---

## registry — Windows реестр

Дружественный доступ к реестру:

```wflow
registry.set key="Software\MyApp" value=Version data=1.0
registry.get key="Software\MyApp" value=Version var=VER
registry.delete key="Software\MyApp"
```

**Команды:**
- `registry.get key=<p> value=<n> [hive=HKCU] [default=<v>] [var=<name>]` — получить
- `registry.set key=<p> value=<n> data=<v> [hive=HKCU] [type=STRING]` — установить
- `registry.exists key=<p> [hive=HKCU] [var=<name>]` — существует?
- `registry.delete key=<p> [value=<n>] [hive=HKCU]` — удалить

---

## async — Асинхронные задачи

Параллельное выполнение:

```wflow
async.start command="echo Task 1" var=task1
async.wait task=${task1}
async.wait_all tasks=${task1},${task2}
```

**Команды:**
- `async.start command=<cmd> [var=<taskId>]` — запустить
- `async.wait task=<id> [timeout=<sec>] [var=<name>]` — ждать
- `async.wait_all tasks=<id1,id2,..>` — ждать все
- `async.status task=<id> [var=<name>]` — статус

---

## input — Интерактивный ввод

Запрос данных у пользователя:

```wflow
input.text prompt="Name: " default=Guest var=NAME
input.password prompt="Password: " var=PWD
input.confirm prompt="Proceed?" var=YES
input.choice prompt="Pick" options="red,green,blue" var=COLOR
```

**Команды:**
- `input.text prompt=<msg> [default=<v>] [var=<name>]` — текст
- `input.password prompt=<msg> [var=<name>]` — пароль (скрытый)
- `input.confirm prompt=<msg> [default=yes|no] [var=<name>]` — да/нет
- `input.choice prompt=<msg> options=<a,b,c> [var=<name>]` — выбор

---

## import — Модули и функции

Загрузка функций из внешних модулей:

```wflow
import path=lib.wflow
call name=helper arg=data
```

**Команды:**
- `import path=<script.wflow>` — импортировать функции

**Особенности:**
- Загружаются только функции (`define`)
- Верхнеуровневые команды НЕ выполняются
- Идеально для библиотек переиспользуемого кода
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


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
- `input.confirm prompt=<msg> [default=yes|no] [var=<name>]` — да/нет (по умолчанию no)
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

### file move
- Назначение: переместить файл
- Сигнатура: `file move src=<FILE> dst=<FILE> [overwrite=true|false]`

### file exists
- Назначение: проверить существование файла или каталога
- Сигнатура: `file exists path=<FILE|DIR> [var=<NAME>]`
- Результат сохраняется в переменную (по умолчанию выводится в лог)

### file read
- Назначение: прочитать содержимое файла
- Сигнатура: `file read path=<FILE> [var=<NAME>]`
- Результат сохраняется в переменную (по умолчанию выводится в лог)

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

## Log

### log config
- Назначение: настроить уровень/формат/файл логирования
- Сигнатура: `log config level=<INFO|DEBUG|WARNING|ERROR> [file=<path>] [format="[%TIME%] %LEVEL% - %MESSAGE%"]`

### log info / debug / warning / error
- Назначение: вывести сообщение с уровнем
- Сигнатура: `log.info message=<TEXT>`, `log.debug message=<TEXT>`, `log.warning message=<TEXT>`, `log.error message=<TEXT>`
- Примечание: `log.debug` выводится при level=DEBUG или флаге --verbose

## Loop

### loop repeat
- Назначение: выполнить тело N раз
- Сигнатура: `loop repeat count=<N> body="<команда>"`
- Переменные: `${index}`

### loop foreach
- Назначение: обойти элементы списка
- Сигнатура: `loop foreach items="a,b,c" [sep=","] [var=item] body="<команда>"`
- Переменные: `${index}`, `${<var>}`

## HTTP

### http.get
- Назначение: HTTP GET запрос
- Сигнатура: `http.get url=<URL> [var=<NAME>]`
- Результат сохраняется в переменную (по умолчанию выводится в лог)

### http.post
- Назначение: HTTP POST запрос
- Сигнатура: `http.post url=<URL> [body=<JSON>] [var=<NAME>]`

### http.put
- Назначение: HTTP PUT запрос
- Сигнатура: `http.put url=<URL> [body=<JSON>] [var=<NAME>]`

## JSON

### json.parse
- Назначение: парсить JSON текст
- Сигнатура: `json.parse text=<JSON> [var=<NAME>]`

### json.get
- Назначение: получить значение из JSON по пути
- Сигнатура: `json.get text=<JSON> path=<path.to.value> [var=<NAME>]`
- Путь через точку, например: `user.name` или `items.0.title`

## String

### string.replace
- Назначение: заменить подстроку
- Сигнатура: `string.replace text=<TEXT> from=<OLD> to=<NEW> [var=<NAME>]`

### string.contains
- Назначение: проверить вхождение подстроки
- Сигнатура: `string.contains text=<TEXT> pattern=<PATTERN> [var=<NAME>]`

### string.length
- Назначение: получить длину строки
- Сигнатура: `string.length text=<TEXT> [var=<NAME>]`

### string.upper
- Назначение: перевести в верхний регистр
- Сигнатура: `string.upper text=<TEXT> [var=<NAME>]`

### string.lower
- Назначение: перевести в нижний регистр
- Сигнатура: `string.lower text=<TEXT> [var=<NAME>]`

### string.trim
- Назначение: удалить пробелы с начала и конца
- Сигнатура: `string.trim text=<TEXT> [var=<NAME>]`

### string.concat
- Назначение: объединить строки
- Сигнатура: `string.concat left=<TEXT1> right=<TEXT2> [sep=<SEP>] [var=<NAME>]`

### string.format
- Назначение: форматировать строку с placeholder-ами {0}, {1}, {2}...
- Сигнатура: `string.format template=<TEXT> 0=<VAL1> 1=<VAL2> ... [var=<NAME>]`
- Пример: `string.format template="Hello {0}!" 0="World" var=GREETING`

## Array

### array.split
- Назначение: разделить строку на массив
- Сигнатура: `array.split text=<TEXT> [sep=<SEP>] [var=<NAME>]`
- По умолчанию sep=","

### array.join
- Назначение: объединить массив в строку
- Сигнатура: `array.join array=<JSON_ARRAY> [sep=<SEP>] [var=<NAME>]`

### array.length
- Назначение: получить длину массива
- Сигнатура: `array.length array=<JSON_ARRAY> [var=<NAME>]`

## Math

### math.add
- Назначение: сложение
- Сигнатура: `math.add a=<NUM> b=<NUM> [var=<NAME>]`

### math.subtract
- Назначение: вычитание
- Сигнатура: `math.subtract a=<NUM> b=<NUM> [var=<NAME>]`

### math.multiply
- Назначение: умножение
- Сигнатура: `math.multiply a=<NUM> b=<NUM> [var=<NAME>]`

### math.divide
- Назначение: деление
- Сигнатура: `math.divide a=<NUM> b=<NUM> [var=<NAME>]`

### math.round
- Назначение: округление
- Сигнатура: `math.round value=<NUM> [decimals=<N>] [var=<NAME>]`

### math.floor
- Назначение: округление вниз
- Сигнатура: `math.floor value=<NUM> [var=<NAME>]`

### math.ceil
- Назначение: округление вверх
- Сигнатура: `math.ceil value=<NUM> [var=<NAME>]`

## DateTime

### datetime.now
- Назначение: получить текущее время
- Сигнатура: `datetime.now [format=<FMT>] [var=<NAME>]`
- По умолчанию format="o" (ISO 8601)

### datetime.format
- Назначение: форматировать дату
- Сигнатура: `datetime.format date=<DATE> [format=<FMT>] [var=<NAME>]`

### datetime.parse
- Назначение: парсить дату из строки
- Сигнатура: `datetime.parse text=<TEXT> [var=<NAME>]`

### datetime.add
- Назначение: добавить время к дате
- Сигнатура: `datetime.add date=<DATE> [days=<N>] [hours=<N>] [minutes=<N>] [seconds=<N>] [var=<NAME>]`

### datetime.diff
- Назначение: вычислить разницу между датами
- Сигнатура: `datetime.diff start=<DATE> end=<DATE> [unit=seconds|minutes|hours|days] [var=<NAME>]`

## Path

### path.join
- Назначение: объединить части пути
- Сигнатура: `path.join parts=<part1,part2,part3> [var=<NAME>]`

### path.dirname
- Назначение: получить директорию файла
- Сигнатура: `path.dirname path=<PATH> [var=<NAME>]`

### path.basename
- Назначение: получить имя файла
- Сигнатура: `path.basename path=<PATH> [var=<NAME>]`

### path.extension
- Назначение: получить расширение файла
- Сигнатура: `path.extension path=<PATH> [var=<NAME>]`

### path.exists
- Назначение: проверить существование пути
- Сигнатура: `path.exists path=<PATH> [var=<NAME>]`

### path.is_directory
- Назначение: проверить, является ли путь директорией
- Сигнатура: `path.is_directory path=<PATH> [var=<NAME>]`

### path.normalize
- Назначение: нормализовать путь (получить абсолютный путь)
- Сигнатура: `path.normalize path=<PATH> [var=<NAME>]`

## Log

### log.config
- Назначение: настроить логирование
- Сигнатура: `log.config [level=INFO|DEBUG] [file=<PATH>] [format=<FMT>]`
- Формат: %TIME%, %LEVEL%, %MESSAGE%

### log.info
- Назначение: записать информационное сообщение
- Сигнатура: `log.info message=<TEXT>`

### log.debug
- Назначение: записать отладочное сообщение (только при level=DEBUG)
- Сигнатура: `log.debug message=<TEXT>`

### log.warning
- Назначение: записать предупреждение
- Сигнатура: `log.warning message=<TEXT>`

### log.error
- Назначение: записать ошибку
- Сигнатура: `log.error message=<TEXT>`

## Regex

### regex.match
- Назначение: проверить соответствие регулярному выражению
- Сигнатура: `regex.match pattern=<REGEX> text=<TEXT> [var=<NAME>]`

### regex.find
- Назначение: найти все совпадения регулярного выражения
- Сигнатура: `regex.find pattern=<REGEX> text=<TEXT> [var=<NAME>]`

### regex.replace
- Назначение: заменить по регулярному выражению
- Сигнатура: `regex.replace pattern=<REGEX> replacement=<TEXT> text=<TEXT> [var=<NAME>]`

## Archive

### archive.create
- Назначение: создать ZIP архив из директории
- Сигнатура: `archive.create source=<DIR> destination=<ZIP> [var=<NAME>]`

### archive.extract
- Назначение: распаковать ZIP архив
- Сигнатура: `archive.extract source=<ZIP> destination=<DIR>`

### archive.list
- Назначение: получить список файлов в архиве
- Сигнатура: `archive.list file=<ZIP> [var=<NAME>]`

### archive.add
- Назначение: добавить файлы в существующий архив
- Сигнатура: `archive.add archive=<ZIP> files=<file1,file2>`

## Include/Import

### include
- Назначение: выполнить другой скрипт WinFlow
- Сигнатура: `include path=<SCRIPT.wflow>`
- Выполняет все команды из указанного файла

### import
- Назначение: импортировать функции из другого скрипта
- Сигнатура: `import path=<SCRIPT.wflow>`
- Загружает только функции (define), не выполняя команды верхнего уровня

## Conditionals

### if
- Назначение: условное выполнение
- Сигнатура: `if condition=<EXPR> body=<COMMANDS> [else=<COMMANDS>]`
- Используется в многострочном синтаксисе с блоками

## Try-Catch

### try
- Назначение: обработка ошибок
- Сигнатура: `try body=<COMMANDS> [catch=<COMMANDS>]`
- Используется в многострочном синтаксисе с блоками

## Functions

### call
- Назначение: вызвать функцию
- Сигнатура: `call name=<FUNCNAME> arg0=<VAL> arg1=<VAL> ...`
- Используется для вызова функций, определенных через define

### return
- Назначение: вернуть значение из функции
- Сигнатура: `return [value=<VAL>]`

## Utility

### isset
- Назначение: проверить установлена ли переменная
- Сигнатура: `isset var=<NAME> [result=<VARNAME>]`
- По умолчанию результат сохраняется в ISSET

---

# Для разработчиков
- Регистрация обработчиков команд — в классе `CommandDispatcher`
- Обработчик: `Action<FlowCommand, ExecutionContext>`

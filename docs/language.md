# Язык WinFlow

Полное описание синтаксиса языка WinFlow — современного DSL для автоматизации Windows.

## Файл скрипта
- Кодировка: UTF-8
- Расширение: `.wflow`

## Комментарии и пустые строки
- `// ...` или `# ...` — комментарии строки
- Пустые строки игнорируются

## Подстановки переменных
- Формат: `${NAME}` — подставляет значение переменной из контекста исполнения.
- Источники значений:
  - Явно установленные через `env set`
  - Значения, устанавливаемые командами (например, переменные циклов)
- Примеры:
  - `env set name=NAME value="World"`
  - `echo "Hello ${NAME}!"` → `Hello World!`
  - В циклах доступны: `${index}` и переменная элемента (см. ниже)

## Функции (v0.2.0+)

Функции позволяют организовать код в переиспользуемые блоки с параметрами.

### Определение функций

```wflow
define имя_функции(параметр1, параметр2):
    команда1
    команда2
```

**Особенности:**
- Многострочный блок с отступом (4 пробела или tab)
- Параметры локальные (не влияют на глобальные переменные)
- Поддержка подстановки переменных `${param}`

### Вызов функций

```wflow
имя_функции(аргумент1, аргумент2)
```

**Примеры:**

```wflow
define greet(name):
    echo Hello ${name}!
    echo Welcome to WinFlow

greet("World")
greet("Alice")
```

Подробнее: [Функции](functions.md)

## Циклы

### loop.repeat — повтор N раз

```wflow
loop.repeat count=3:
    echo Iteration ${index}
```

- Доступна переменная `${index}` (0..N-1)
- Многострочное тело с отступом

### loop.foreach — итерация по элементам

```wflow
loop.foreach items="apple,banana,orange" var=item body="echo Processing: ${item} (Index: ${index})"
```

- Параметры: `items` (строка через запятую), `var` (имя переменной элемента), `body` (тело цикла)
- Доступны: `${var}`, `${index}`

Подробнее: [Продвинутые возможности](advanced.md)

## Условия

```wflow
if condition="${status} == ok" body="echo Success! && env.set name=result value=passed" else="echo Failed && exit code=1"
```

**Операторы сравнения:**
- ` == ` — равенство
- ` != ` — неравенство
- ` > ` — больше
- ` < ` — меньше
- `exists <path>` — проверка существования файла/директории

**Примечание:** Условие передается в параметре `condition`, тело в `body`, альтернатива в `else`.

Подробнее: [Продвинутые возможности](advanced.md)

## Обработка ошибок (v0.1.9+)

```wflow
try:
    file read path="config.json"
    json.parse file="config.json" var="config"
catch:
    echo Error: Config not found
    env set use_defaults=true
```

## Модули команд

### Базовые
- `echo <text>` — вывод текста
- `noop` — пустая операция
- `exit code=<N>` — выход с кодом

### Env — переменные окружения
- `env set name=VAR value="..."` — установить переменную
- `env get name=VAR` — получить значение
- `env print` — показать все переменные
- `env unset name=VAR` — удалить переменную

### File — файловые операции
- `file read path="..."` — прочитать файл
- `file write path="..." content="..."` — записать файл
- `file append path="..." content="..."` — добавить к файлу
- `file copy source="..." destination="..."` — скопировать
- `file move source="..." destination="..."` — переместить
- `file delete path="..."` — удалить
- `file exists path="..."` — проверить существование
- `file mkdir path="..."` — создать директорию

### Process — процессы
- `process.exec file="..." args="..."` — запустить и ждать
- `process.start file="..." args="..."` — запустить в фоне

### JSON (v0.1.9+)
- `json.parse file="..." var="..."` — парсить JSON
- `json.get var="..." path="..."` — получить значение
- `json.set var="..." path="..." value="..."` — установить значение

### Net — сеть (v0.1.9+)
- `net download url="..." path="..."` — загрузить файл
- `net request url="..." method="..." var="..."` — HTTP запрос

### Array — массивы (v0.1.9+)
- `array.split text="..." [sep=","] [var="..."]` — разделить строку на массив (JSON)
- `array.join array="[...]" [sep=","] [var="..."]` — объединить массив в строку
- `array.length array="[...]" [var="..."]` — получить размер массива

### String — строки
- `string.replace text="..." from="..." to="..."` — замена
- `string.contains text="..." pattern="..."` — проверка вхождения
- `string.length text="..."` — длина строки
- `string.upper text="..."` — в верхний регистр
- `string.lower text="..."` — в нижний регистр
- `string.trim text="..."` — удалить пробелы
- `string.concat left="..." right="..." [sep="..."]` — объединение строк
- `string.format template="..." 0="..." 1="..."` — форматирование

### Registry — реестр Windows
- `reg set key="..." name="..." value="..."` — установить значение
- `reg get key="..." name="..."` — получить значение
- `reg delete key="..." [name="..."]` — удалить значение
- `registry.set key="..." value="..." data="..."` — установить (с дефолтами)
- `registry.get key="..." value="..." [var="..."]` — получить (с дефолтами)
- `registry.exists key="..." [var="..."]` — проверить существование
- `registry.delete key="..." [value="..."]` — удалить

### Sleep — задержки
- `sleep.ms ms=<N>` — задержка в миллисекундах
- `sleep.sec sec=<N>` — задержка в секундах

### HTTP — HTTP запросы
- `http.get url="..." [var="..."]` — GET запрос
- `http.post url="..." [body="..."] [var="..."]` — POST запрос
- `http.put url="..." [body="..."] [var="..."]` — PUT запрос

### Math — математические операции
- `math.add a=<N> b=<N> [var="..."]` — сложение
- `math.subtract a=<N> b=<N> [var="..."]` — вычитание
- `math.multiply a=<N> b=<N> [var="..."]` — умножение
- `math.divide a=<N> b=<N> [var="..."]` — деление
- `math.round value=<N> [decimals=<N>] [var="..."]` — округление
- `math.floor value=<N> [var="..."]` — округление вниз
- `math.ceil value=<N> [var="..."]` — округление вверх

### DateTime — работа с датой и временем
- `datetime.now [format="..."] [var="..."]` — текущее время
- `datetime.format date="..." [format="..."] [var="..."]` — форматировать
- `datetime.parse text="..." [var="..."]` — парсить дату
- `datetime.add date="..." [days=<N>] [hours=<N>] [var="..."]` — добавить время
- `datetime.diff start="..." end="..." [unit="..."] [var="..."]` — разница

### Path — работа с путями
- `path.join parts="..." [var="..."]` — объединить части пути
- `path.dirname path="..." [var="..."]` — получить директорию
- `path.basename path="..." [var="..."]` — получить имя файла
- `path.extension path="..." [var="..."]` — получить расширение
- `path.exists path="..." [var="..."]` — проверить существование
- `path.is_directory path="..." [var="..."]` — проверка директории
- `path.normalize path="..." [var="..."]` — нормализовать путь

### Log — логирование
- `log.config [level="..."] [file="..."] [format="..."]` — настроить
- `log.info message="..."` — информация
- `log.debug message="..."` — отладка
- `log.warning message="..."` — предупреждение
- `log.error message="..."` — ошибка

### Regex — регулярные выражения
- `regex.match pattern="..." text="..." [var="..."]` — проверить соответствие
- `regex.find pattern="..." text="..." [var="..."]` — найти совпадения
- `regex.replace pattern="..." replacement="..." text="..." [var="..."]` — заменить

### Archive — работа с архивами
- `archive.create source="..." destination="..." [var="..."]` — создать ZIP
- `archive.extract source="..." destination="..."` — распаковать ZIP
- `archive.list file="..." [var="..."]` — список файлов в архиве
- `archive.add archive="..." files="..."` — добавить файлы

Подробный справочник: [Команды](commands.md)

## Модель исполнения

### Порядок выполнения
1. **Парсинг**: файл `.wflow` читается и парсится в AST
2. **Регистрация функций**: все `define` регистрируются глобально
3. **Выполнение команд**: команды выполняются сверху вниз
4. **Контекст**: переменные сохраняются в ExecutionContext

### Режимы запуска

```powershell
# Обычный режим
WinFlow.Cli.exe script.wflow

# Verbose — подробный вывод
WinFlow.Cli.exe script.wflow --verbose

# Dry-run — без выполнения (только парсинг)
WinFlow.Cli.exe script.wflow --dry-run
```

## Лучшие практики

### Организация кода

```wflow
#/// Script Title

// 1. Функции определяем в начале
define helper_function():
    echo Helper logic

// 2. Основная логика
env set config="production"
helper_function()

// 3. Очистка
file delete path="temp.txt"
```

### Именование переменных

```wflow
// Хорошо
env set app_name="WinFlow"
env set api_url="https://api.example.com"
env set is_valid="true"

// Плохо
env set a="WinFlow"
env set x="https://api.example.com"
env set flag="true"
```

### Обработка ошибок

```wflow
// Всегда используйте try-catch для критичных операций
try:
    net download url="${url}" path="data.json"
    json.parse file="data.json" var="data"
catch:
    echo Download failed, using cache
    file read path="cache.json"
```

## См. также

- [Быстрый старт](quickstart.md) — начало работы
- [Команды](commands.md) — полный справочник команд
- [Функции](functions.md) — работа с функциями
- [Примеры](examples.md) — готовые скрипты
- [FAQ](faq.md) — часто задаваемые вопросы

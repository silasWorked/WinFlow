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

### loop.foreach — итерация по массиву

```wflow
array.create name=items values="apple,banana,orange"

loop.foreach array=items element=item:
    echo Processing: ${item}
    echo Index: ${index}
```

- Параметры: `array` (имя массива), `element` (имя переменной)
- Доступны: `${element}`, `${index}`

Подробнее: [Продвинутые возможности](advanced.md)

## Условия

```wflow
if condition="${status}" equals="ok":
    echo Success!
    env set result=passed
else:
    echo Failed
    exit code=1
```

**Операторы сравнения:**
- `equals` / `not_equals`
- `greater` / `less`
- `contains`

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
- `array.create name="..." values="..."` — создать массив
- `array.add name="..." value="..."` — добавить элемент
- `array.get name="..." index=N` — получить элемент
- `array.length name="..."` — получить размер

### String — строки
- `string.replace text="..." find="..." replace="..."` — замена
- `string.contains text="..." find="..."` — проверка вхождения
- `string.length text="..."` — длина строки
- `string.upper text="..."` — в верхний регистр
- `string.lower text="..."` — в нижний регистр
- `string.trim text="..."` — удалить пробелы

### Registry — реестр Windows
- `reg set key="..." value="..." data="..."` — установить значение
- `reg get key="..." value="..."` — получить значение
- `reg delete key="..." value="..."` — удалить значение

### Sleep — задержки
- `sleep ms=<N>` — задержка в миллисекундах
- `sleep sec=<N>` — задержка в секундах

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

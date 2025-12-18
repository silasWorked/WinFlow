# Функции

Функции в WinFlow — это мощный инструмент для организации и переиспользования кода. В версии 0.2.0 функции работают как в настоящих языках программирования (C#, Python, JavaScript).

## Определение функций

### Базовый синтаксис

```wflow
define имя_функции(параметр1, параметр2, ...):
    команда1
    команда2
    ...
```

**Правила:**
- Функция начинается с ключевого слова `define`
- После имени в скобках перечисляются параметры (опционально)
- Двоеточие `:` завершает заголовок
- Тело функции — это блок команд с **отступом** (4 пробела или tab)
- Отступ обязателен — так парсер понимает, где заканчивается функция

### Примеры определения

#### Функция без параметров

```wflow
define say_hello():
    echo Hello from WinFlow!
    echo This is a function without parameters
```

#### Функция с одним параметром

```wflow
define greet(name):
    echo Hello ${name}!
    echo Welcome to automation world
```

#### Функция с несколькими параметрами

```wflow
define create_file(filename, content):
    echo Creating file: ${filename}
    file write path="${filename}" content="${content}"
    echo File ${filename} created successfully
```

#### Функция с командами разных модулей

```wflow
define deploy_app(app_name, version, target_dir):
    echo Deploying ${app_name} v${version} to ${target_dir}
    
    // Создаем директорию
    env set deploy_path="${target_dir}/${app_name}"
    
    // Скачиваем файлы
    env set download_url="https://releases.example.com/${app_name}-${version}.zip"
    net download url="${download_url}" path="temp.zip"
    
    // Копируем
    file copy source="temp.zip" destination="${deploy_path}/app.zip"
    
    // Очистка
    file delete path="temp.zip"
    
    echo Deployment complete!
```

## Вызов функций

### Синтаксис вызова

```wflow
имя_функции(аргумент1, аргумент2, ...)
```

**Важно:**
- Аргументы передаются **позиционно** — порядок имеет значение
- Количество аргументов должно совпадать с количеством параметров
- Аргументы могут быть строками, числами или переменными

### Примеры вызова

```wflow
// Без параметров
say_hello()

// С одним параметром
greet("Alice")
greet("Bob")

// С несколькими параметрами
create_file("config.txt", "Setting=Value")
create_file("output.log", "Application started")

// С переменными
env set username="John"
env set message="Welcome back"
greet("${username}")
```

### Использование кавычек

```wflow
// Без кавычек (простые значения)
greet(World)

// С кавычками (строки с пробелами или спецсимволами)
greet("John Doe")
create_file("my file.txt", "Content with spaces")

// Переменные (всегда с ${})
env set name="Alice"
greet("${name}")
```

## Параметры и область видимости

### Локальные параметры

Параметры функции **локальны** — они видны только внутри функции и не влияют на глобальные переменные:

```wflow
// Глобальная переменная
env set name="Global User"

define test_scope(name):
    echo Inside function: ${name}
    env set name="${name} - Modified"
    echo Still inside: ${name}

// Вызов функции
test_scope("Local User")

// Глобальная переменная не изменилась
echo Outside function: ${name}
```

Вывод:
```
Inside function: Local User
Still inside: Local User - Modified
Outside function: Global User
```

### Глобальные переменные в функциях

Функции **могут читать и изменять** глобальные переменные (кроме параметров):

```wflow
env set counter=0

define increment():
    env set counter="${counter}+1"
    echo Counter incremented to ${counter}

increment()  // Counter: 1
increment()  // Counter: 2
increment()  // Counter: 3
```

### Передача переменных как аргументов

```wflow
env set app_name="MyApp"
env set app_version="1.0.0"

define show_info(name, version):
    echo Application: ${name}
    echo Version: ${version}

// Передаем значения переменных
show_info("${app_name}", "${app_version}")
```

## Продвинутые примеры

### Функция с условиями

```wflow
define check_file(filepath):
    echo Checking file: ${filepath}
    
    file exists path="${filepath}"
    
    if condition="${EXISTS}" equals="true":
        echo File exists!
        file read path="${filepath}"
        echo Content: ${CONTENT}
    else:
        echo File not found, creating default...
        file write path="${filepath}" content="Default content"
```

### Функция с циклами

```wflow
define process_list(items):
    echo Processing items: ${items}
    loop.foreach items="${items}" var=item body="echo Processing item: ${item} && file.write path=${item}.log content=Processed"
```

### Функция с обработкой ошибок

```wflow
define safe_download(url, destination):
    echo Attempting download from ${url}
    
    try:
        net download url="${url}" path="${destination}"
        echo Download successful: ${destination}
        env set download_status=success
    catch:
        echo Download failed: ${url}
        env set download_status=failed
        
        // Создаем пустой файл как fallback
        file write path="${destination}" content="Download failed"
```

### Композиция функций

Функции могут вызывать другие функции:

```wflow
define log_message(message):
    echo [LOG] ${message}

define log_error(error):
    log_message("ERROR: ${error}")

define download_with_logging(url, path):
    log_message("Starting download: ${url}")
    
    try:
        net download url="${url}" path="${path}"
        log_message("Download completed: ${path}")
    catch:
        log_error("Failed to download ${url}")
```

### Функция-обертка для процессов

```wflow
define run_command(command, args):
    echo Executing: ${command} ${args}
    
    process.exec file="${command}" args="${args}"
    
    if condition="${LASTEXITCODE}" equals="0":
        echo Command succeeded
        env set last_command_status=ok
    else:
        echo Command failed with code ${LASTEXITCODE}
        env set last_command_status=failed
        exit code=1
```

## Паттерны использования

### Builder паттерн

```wflow
define init_project(name):
    echo Initializing project: ${name}
    env set project_name="${name}"

define add_config(key, value):
    echo Adding config: ${key}=${value}
    env set config_${key}="${value}"

define build_project():
    echo Building ${project_name}...
    echo Config loaded: ${config_environment}
    process.exec file="dotnet" args="build"

// Использование
init_project("MyApp")
add_config("environment", "production")
add_config("version", "1.0.0")
build_project()
```

### Утилиты для работы с файлами

```wflow
define backup_file(file):
    echo Backing up ${file}...
    env set backup_name="${file}.backup"
    file copy source="${file}" destination="${backup_name}"

define restore_file(file):
    echo Restoring ${file}...
    env set backup_name="${file}.backup"
    file copy source="${backup_name}" destination="${file}"

define cleanup_backups():
    echo Cleaning up backup files...
    loop.foreach array=backup_files element=file:
        file delete path="${file}"
```

### Работа с API

```wflow
define fetch_json(url, output_file):
    net download url="${url}" path="${output_file}"
    file read path="${output_file}" var=json_content
    json.parse text="${json_content}" var=data

define get_repo_info(owner, repo):
    env set api_url="https://api.github.com/repos/${owner}/${repo}"
    fetch_json("${api_url}", "repo.json")
    json.get text="${data}" path="name" var=repo_name
    json.get text="${data}" path="stargazers_count" var=stars
    echo Repository: ${repo_name}
    echo Stars: ${stars}

// Использование
get_repo_info("silasWorked", "WinFlow")
```

## Ограничения и планы

### Текущие ограничения (v0.2.0)

- ❌ **Возвращаемые значения**: функции не могут возвращать значения напрямую (планируется в v0.3.0)
- ❌ **Рекурсия**: не тестировалась, может работать некорректно
- ❌ **Значения по умолчанию**: параметры не могут иметь значения по умолчанию
- ❌ **Именованные аргументы**: только позиционная передача

### Обходные пути

**Возврат значений через переменные:**

```wflow
define calculate_sum(a, b):
    env set result="${a}+${b}"

calculate_sum(3, 5)
echo Result: ${result}  // Output: Result: 8
```

**Функция-валидатор:**

```wflow
define validate_file(path):
    file exists path="${path}"
    env set is_valid="${EXISTS}"

validate_file("config.json")

if condition="${is_valid}" equals="true":
    echo File is valid
```

## Лучшие практики

1. **Именование**: используйте понятные глагольные имена (`create_file`, `download_data`, `process_item`)
2. **Параметры**: ограничивайте количество параметров (не более 5-7)
3. **Документация**: добавляйте комментарии перед функциями
4. **Единственная ответственность**: одна функция = одна задача
5. **Обработка ошибок**: используйте `try-catch` для критичных операций

```wflow
// Хороший пример с документацией
// Функция: deploy_application
// Параметры:
//   - app_name: имя приложения
//   - version: версия для развертывания
//   - target: целевая директория
// Описание: Загружает и разворачивает приложение в указанную директорию
define deploy_application(app_name, version, target):
    try:
        echo Starting deployment of ${app_name} v${version}
        
        env set url="https://releases.example.com/${app_name}-${version}.zip"
        env set temp_file="temp_${app_name}.zip"
        
        net download url="${url}" path="${temp_file}"
        file copy source="${temp_file}" destination="${target}/${app_name}.zip"
        file delete path="${temp_file}"
        
        echo Deployment successful!
        env set deployment_status=success
    catch:
        echo Deployment failed!
        env set deployment_status=failed
```

## См. также

- [Язык WinFlow](language.md) — полное описание синтаксиса
- [Команды](commands.md) — справочник команд
- [Примеры](examples.md) — готовые примеры с функциями
- [Продвинутые возможности](advanced.md) — циклы, условия, обработка ошибок

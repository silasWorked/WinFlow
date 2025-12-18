# Быстрый старт

Добро пожаловать в WinFlow! Это руководство поможет вам быстро начать работу с языком автоматизации Windows.

## Что вам понадобится

- **Windows 10/11** (x64)
- **.NET 8 SDK** — [Скачать здесь](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Текстовый редактор** — VS Code, Notepad++, или любой другой

## Установка

### Вариант 1: Сборка из исходников (рекомендуется для разработки)

```powershell
# Клонируем репозиторий
git clone https://github.com/silasWorked/WinFlow.git
cd WinFlow

# Собираем проект
dotnet build WinFlow.sln -c Release

# Проверяем версию
.\WinFlow\WinFlow.Cli\bin\Release\net8.0\WinFlow.Cli.exe --version
```

### Вариант 2: Использование инсталлятора

```powershell
# Установка в %LOCALAPPDATA%\WinFlow
dotnet run --project WinFlow/WinFlow.Installer.Cli

# Создание демо-скрипта на рабочем столе
dotnet run --project WinFlow/WinFlow.Installer.Cli -- --create-desktop-demo
```

## Ваш первый скрипт

Создайте файл `hello.wflow`:

```wflow
#/// My First WinFlow Script

// Устанавливаем переменную
env set name=USER value="Developer"

// Выводим приветствие
echo Hello ${USER}!
echo Welcome to WinFlow automation!

// Записываем результат в файл
file write path="output.txt" content="Script executed at ${date}"

echo Done!
```

Запустите скрипт:

```powershell
.\WinFlow\WinFlow.Cli\bin\Release\net8.0\WinFlow.Cli.exe hello.wflow
```

Вывод:
```
[task] My First WinFlow Script
Hello Developer!
Welcome to WinFlow automation!
file write path='output.txt' content='Script executed at ...'
Done!
```

## Основные концепции

### 1. Комментарии

```wflow
// Однострочный комментарий
# Альтернативный формат комментария

#/// Заголовок скрипта (отображается в логах)
```

### 2. Переменные

```wflow
// Установка переменной
env set name=APP_NAME value="WinFlow"

// Использование переменной
echo Application: ${APP_NAME}

// Переменные доступны во всех командах
file write path="${APP_NAME}.txt" content="Data"
```

### 3. Команды

Все команды следуют формату: `команда аргумент1=значение аргумент2=значение`

```wflow
// Модуль env (переменные окружения)
env set name=VAR value="text"
env get name=VAR
env print

// Модуль file (файлы)
file read path="input.txt"
file write path="output.txt" content="Hello"
file copy source="a.txt" destination="b.txt"
file delete path="temp.txt"

// Модуль process (процессы)
process.exec file="cmd.exe" args="/c dir"
```

### 4. Функции

```wflow
// Определение функции с параметрами
define greet(name, message):
    echo Hello ${name}!
    echo ${message}

// Вызов функции
greet("Alice", "Welcome to WinFlow")
greet("Bob", "Have a great day")
```

## Примеры сценариев использования

### Автоматизация сборки

```wflow
#/// Build Automation Script

define build(config):
    echo Building in ${config} mode...
    process.exec file="dotnet" args="build -c ${config}"
    if condition="${LASTEXITCODE} == 0" body="echo Build successful!" else="echo Build failed && exit code=1"

build("Release")
```

### Работа с файлами

```wflow
#/// File Processing Script

// Создание резервной копии
file copy source="config.json" destination="config.backup.json"

// Чтение и изменение
file read path="config.json"
env set updated_config="${CONTENT} - Updated"
file write path="config.json" content="${updated_config}"

echo Backup created and config updated
```

### HTTP запросы

```wflow
#/// API Integration Script

// Загрузка данных с API
net download url="https://api.github.com/repos/silasWorked/WinFlow" path="repo.json"

// Чтение файла и парсинг JSON
file read path="repo.json" var=json_content
json.parse text="${json_content}" var=repo

// Получение данных из JSON
json.get text="${repo}" path="name" var=repo_name
json.get text="${repo}" path="stargazers_count" var=stars
echo Repository: ${repo_name}
echo Stars: ${stars}
```

### Цикл обработки файлов

```wflow
#/// Batch File Processing

// Создание функции для обработки файлов
define process_files():
    loop.foreach items="file1.txt,file2.txt,file3.txt" var=file body="echo Processing ${file} && file.copy src=${file} dst=backup/${file} && echo ${file} backed up"

process_files()
```

## Запуск с параметрами

### Verbose режим (подробный вывод)

```powershell
.\WinFlow.Cli.exe script.wflow --verbose
```

### Dry-run режим (без выполнения)

```powershell
.\WinFlow.Cli.exe script.wflow --dry-run
```

### Комбинация параметров

```powershell
.\WinFlow.Cli.exe script.wflow --verbose --dry-run
```

## Встроенное демо

WinFlow включает встроенный демо-скрипт, демонстрирующий все возможности:

```powershell
.\WinFlow.Cli.exe demo.wflow --verbose
```

Демо показывает:
- Работу с переменными окружения
- Файловые операции
- Запуск процессов
- JSON и HTTP
- Функции и циклы

## Следующие шаги

Теперь, когда вы знаете основы, изучите:

1. [**Язык WinFlow**](language.md) — полное описание синтаксиса
2. [**Команды**](commands.md) — справочник по всем командам
3. [**Функции**](functions.md) — продвинутая работа с функциями
4. [**Примеры**](examples.md) — готовые рецепты для типичных задач

## Получение помощи

- 📖 [FAQ](faq.md) — часто задаваемые вопросы
- 🐛 [GitHub Issues](https://github.com/silasWorked/WinFlow/issues) — сообщить об ошибке
- 💬 [Discussions](https://github.com/silasWorked/WinFlow/discussions) — задать вопрос сообществу

---

**Готовы к автоматизации? Начните создавать свои первые .wflow скрипты!** 🚀

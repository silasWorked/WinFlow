# WinFlow

Простой и мощный DSL для автоматизации Windows (.wflow) — скриптовый язык нового поколения с поддержкой функций, циклов, условий, JSON, HTTP и многого другого.

[![CI](https://github.com/silasWorked/WinFlow/actions/workflows/ci.yml/badge.svg)](https://github.com/silasWorked/WinFlow/actions/workflows/ci.yml)
[![Release](https://github.com/silasWorked/WinFlow/actions/workflows/release.yml/badge.svg)](https://github.com/silasWorked/WinFlow/actions/workflows/release.yml)
[![Version](https://img.shields.io/badge/version-0.2.0-blue.svg)](https://github.com/silasWorked/WinFlow/releases)

## ✨ Что такое WinFlow?

WinFlow — это современный скриптовый язык для Windows-автоматизации, который объединяет простоту .bat файлов с мощью Python/PowerShell. Идеален для:

- 🚀 Автоматизации рутинных задач
- 📦 Развертывания приложений
- 🔄 CI/CD скриптов
- 🛠️ DevOps операций
- 📊 Обработки данных и HTTP API

## 🎯 Ключевые возможности

### **v0.2.0 — Функции как в настоящих языках**
```wflow
define greet(name):
    echo Hello ${name}!
    echo Welcome to WinFlow

greet("World")
```

### **v0.1.9 — JSON, HTTP, массивы, try-catch**
```wflow
// HTTP запросы и JSON
net download url="https://api.github.com/repos/silasWorked/WinFlow" path="repo.json"
file read path="repo.json" var=json_data
json.parse text="${json_data}" var=repo
json.get text="${repo}" path="name" var=repo_name
json.get text="${repo}" path="stargazers_count" var=stars
echo Repository: ${repo_name}
echo Stars: ${stars}

// Try-catch для обработки ошибок
try body="file.read path=config.json var=cfg && json.parse text=${cfg} var=config" catch="echo Error: Config not found, using defaults"
```

### **Мощные конструкции языка**
```wflow
// Циклы
loop.repeat count=3 body="echo Iteration ${index}"

loop.foreach items="apple,banana,orange" var=item body="echo Processing: ${item}"

// Условия
if condition="${status} == ok" body="echo Success!" else="echo Failed"
```

### **Модули и встроенные команды**

| Модуль | Описание | Примеры команд |
|--------|----------|----------------|
| **env** | Переменные окружения | `env.set`, `env.get`, `env.print` |
| **file** | Файловые операции | `file.read`, `file.write`, `file.copy`, `file.delete` |
| **process** | Запуск процессов | `process.exec`, `process.run` |
| **json** | Работа с JSON | `json.parse`, `json.get` |
| **net** | HTTP/сеть | `net.download` |
| **http** | HTTP запросы | `http.get`, `http.post`, `http.put` |
| **array** | Массивы | `array.split`, `array.join`, `array.length` |
| **string** | Строковые операции | `string.replace`, `string.upper`, `string.lower` |
| **math** | Математика | `math.add`, `math.subtract`, `math.multiply`, `math.divide` |
| **datetime** | Дата и время | `datetime.now`, `datetime.format`, `datetime.add` |
| **path** | Работа с путями | `path.join`, `path.dirname`, `path.basename` |
| **regex** | Регулярные выражения | `regex.match`, `regex.find`, `regex.replace` |
| **archive** | Архивы (ZIP) | `archive.create`, `archive.extract`, `archive.list` |
| **log** | Логирование | `log.info`, `log.warning`, `log.error` |
| **config** | INI конфигурация | `config.read`, `config.get`, `config.set` |
| **csv** | CSV таблицы | `csv.read`, `csv.write`, `csv.filter` |
| **xml** | XML документы | `xml.parse`, `xml.get`, `xml.add_element` |
| **registry** | Реестр Windows | `registry.get`, `registry.set`, `registry.delete` |
| **async** | Асинхронность | `async.start`, `async.wait`, `async.status` |
| **input** | Ввод пользователя | `input.text`, `input.password`, `input.confirm` |

## 🚀 Быстрый старт

### Установка

```powershell
# Требуется .NET 8 SDK
git clone https://github.com/silasWorked/WinFlow.git
cd WinFlow
dotnet build WinFlow.sln -c Release
```

### Первый скрипт

Создайте файл `hello.wflow`:

```wflow
#/// My First WinFlow Script

env set name=USER value="Developer"
echo Hello ${USER}!
echo Current date: ${date}

file write path="output.txt" content="Script executed successfully"
echo Done!
```

Запустите:

```powershell
.\WinFlow\WinFlow.Cli\bin\Release\net8.0\WinFlow.Cli.exe hello.wflow
```

### Пример с функциями

Создайте `deploy.wflow`:

```wflow
#/// Deployment Script

define download_and_extract(url, target):
    echo Downloading from ${url}...
    net download url="${url}" path="temp.zip"
    file copy source="temp.zip" destination="${target}"
    echo Extracted to ${target}

define cleanup():
    echo Cleaning up temporary files...
    file delete path="temp.zip"
    echo Cleanup complete

// Использование функций
download_and_extract("https://example.com/app.zip", "C:/Apps/MyApp")
cleanup()
```

## 📚 Документация

- [**Начало работы**](docs/quickstart.md) — Быстрый старт и первые шаги
- [**Язык и синтаксис**](docs/language.md) — Полное описание языка WinFlow
- [**Команды**](docs/commands.md) — Справочник по всем командам
- [**Функции**](docs/functions.md) — Работа с функциями
- [**CLI**](docs/cli.md) — Параметры командной строки
- [**Примеры**](docs/examples.md) — Готовые примеры скриптов
- [**FAQ**](docs/faq.md) — Часто задаваемые вопросы
- [**Установка**](docs/install.md) — Детальная инструкция по установке

## 🎓 Примеры использования

### Автоматизация сборки

```wflow
define build_project(config):
    echo Building in ${config} mode...
    process.exec file="dotnet" args="build -c ${config}"
    
    if condition="${LASTEXITCODE}" equals="0":
        echo Build successful!
    else:
        echo Build failed
        exit code=1

build_project("Release")
```

### Работа с API

```wflow
define fetch_user_data(username):
    env set api_url="https://api.github.com/users/${username}"
    net download url="${api_url}" path="user.json"
    file read path="user.json" var=json_data
    json.parse text="${json_data}" var=user
    json.get text="${user}" path="login" var=user_login
    json.get text="${user}" path="public_repos" var=repos
    json.get text="${user}" path="followers" var=followers
    echo User: ${user_login}
    echo Repos: ${repos}
    echo Followers: ${followers}

fetch_user_data("octocat")
```

### Backup скрипт

```wflow
#/// Backup Script with Error Handling

define backup(source, destination):
    echo Starting backup: ${source} -> ${destination}
    
    try:
        file copy source="${source}" destination="${destination}"
        echo Backup completed successfully
        env set backup_status=success
    catch:
        echo Backup failed!
        env set backup_status=failed
        exit code=1

backup("C:/Important/Data", "D:/Backups/Data")
```

## 🛠️ Разработка

### Структура проекта

```
WinFlow/
├── WinFlow.Core/          # Ядро: парсер, AST, рантайм
│   ├── Parsing/           # Парсер .wflow файлов
│   ├── Runtime/           # Исполнение команд
│   └── Model/             # Модели данных
├── WinFlow.Cli/           # CLI приложение
├── WinFlow.ShellHost/     # Shell интеграция
└── WinFlow.Installer/     # Установщик
```

### Запуск тестов

```powershell
# Запуск всех тестовых скриптов
Get-ChildItem test-*.wflow | ForEach-Object {
    Write-Host "Testing: $($_.Name)" -ForegroundColor Cyan
    dotnet run --project WinFlow/WinFlow.Cli -- $_.FullName
}
```

## 📝 Changelog

### v0.2.0 (текущая)
- ✅ **Функции с параметрами**: `define funcname(param1, param2):` с многострочными телами
- ✅ **Локальная область видимости**: параметры не загрязняют глобальное окружение
- ✅ **Прямой вызов функций**: `funcname(arg1, arg2)`
- ✅ **Поддержка переменных в функциях**: полная подстановка `${variable}`

### v0.1.9
- ✅ JSON парсинг и манипуляция (`json.parse`, `json.get`)
- ✅ HTTP запросы (`http.get`, `http.post`, `http.put`, `net.download`)
- ✅ Массивы (`array.split`, `array.join`, `array.length`)
- ✅ Try-catch блоки для обработки ошибок
- ✅ Базовые функции (`define`, `call`)

### v0.1.8
- ✅ Циклы (`loop.repeat`, `loop.foreach`)
- ✅ Условия (`if`, `else`)
- ✅ Работа с файлами и процессами
- ✅ Переменные окружения

## 🤝 Вклад в проект

Приветствуются любые contributions! Пожалуйста:

1. Fork репозитория
2. Создайте feature branch (`git checkout -b feature/amazing-feature`)
3. Commit изменения (`git commit -m 'Add amazing feature'`)
4. Push в branch (`git push origin feature/amazing-feature`)
5. Откройте Pull Request

## 📄 Лицензия

MIT License — смотри [LICENSE](LICENSE) для деталей.

## 🔗 Ссылки

- [GitHub Repository](https://github.com/silasWorked/WinFlow)
- [Releases](https://github.com/silasWorked/WinFlow/releases)
- [Issues](https://github.com/silasWorked/WinFlow/issues)
- [Documentation](https://github.com/silasWorked/WinFlow/blob/main/SUMMARY.md)

---

**WinFlow** — делаем Windows-автоматизацию простой и приятной! 🎉

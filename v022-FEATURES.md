# WinFlow v0.2.3 Release Notes

## Новые возможности

### 📅 DateTime Module
Полноценная работа с датой и временем:

```wflow
# Получить текущее время
datetime.now var=now
datetime.now format="yyyy-MM-dd" var=today

# Форматирование
datetime.format date="2024-01-01" format="dd/MM/yyyy" var=formatted

# Парсинг
datetime.parse text="2024-12-31 23:59:59" var=parsed

# Математика с датами
datetime.add date=${now} days=7 hours=2 var=later
datetime.diff start="2024-01-01" end="2024-12-31" unit=days var=days_diff
```

**Команды:**
- `datetime.now` - получить текущую дату/время
- `datetime.format` - форматировать дату
- `datetime.parse` - распарсить строку в дату
- `datetime.add` - добавить дни/часы/минуты/секунды
- `datetime.diff` - разница между датами (days, hours, minutes, seconds, milliseconds)

---

### 📁 Path Module  
Кроссплатформенная работа с путями:

```wflow
# Объединить пути
path.join parts="C:\temp,backup,data.txt" var=fullpath

# Получить директорию и имя файла
path.dirname path="C:\Windows\System32\notepad.exe" var=dir
path.basename path="C:\Windows\System32\notepad.exe" var=name

# Расширение и проверка существования
path.extension path="document.pdf" var=ext
path.exists path="C:\Windows" var=exists
path.is_directory path="C:\Windows" var=is_dir

# Нормализация пути
path.normalize path="..\test\..\WinFlow" var=norm
```

**Команды:**
- `path.join` - объединить части пути
- `path.dirname` - получить директорию
- `path.basename` - получить имя файла
- `path.extension` - получить расширение
- `path.exists` - проверить существование
- `path.is_directory` - проверить, является ли директорией
- `path.normalize` - нормализовать путь

---

### 📝 Log Module
Профессиональное логирование с уровнями и форматами:

```wflow
# Настройка логирования
log.config level=DEBUG file=app.log format="[%TIME%] [%LEVEL%] %MESSAGE%"

# Разные уровни
log.debug message="Debug information"
log.info message="Application started"
log.warning message="Warning message"
log.error message="Error occurred"
```

**Уровни:** DEBUG, INFO, WARNING, ERROR  
**Формат:** `%TIME%`, `%LEVEL%`, `%MESSAGE%`  
**Вывод:** Консоль (с цветами) + опциональный файл

---

### ✅ isset Command
Проверка существования переменной:

```wflow
# Проверить переменную
isset var=myvar result=exists
echo ${exists}  # true или false

# Использование с условиями
isset var=API_KEY result=has_key
if condition="${has_key} == false" body="echo API_KEY not set"
```

---

### 🔍 Regex Module
Работа с регулярными выражениями:

```wflow
# Проверка на совпадение
regex.match pattern="^[a-z]+@[a-z]+\.[a-z]+$" text="user@example.com" var=valid

# Поиск всех совпадений
regex.find pattern="\d+" text="I have 42 apples" var=numbers

# Замена
regex.replace pattern="\s+" replacement="_" text="hello world" var=result
```

**Команды:**
- `regex.match` - проверка совпадения (true/false)
- `regex.find` - найти все совпадения (через запятую)
- `regex.replace` - заменить по шаблону

---

### 📦 Archive Module
Работа с ZIP архивами:

```wflow
# Создать архив из директории
archive.create source=C:\data destination=backup.zip

# Извлечь архив
archive.extract source=backup.zip destination=C:\restore

# Просмотр содержимого
archive.list file=backup.zip var=contents

# Добавить файлы
archive.add archive=backup.zip files="file1.txt,file2.txt"
```

**Команды:**
- `archive.create` - создать архив из директории
- `archive.extract` - распаковать архив
- `archive.list` - список файлов в архиве
- `archive.add` - добавить файлы в существующий архив

---

## Примеры использования

### Резервное копирование с датой

```wflow
# Получить текущую дату
datetime.now format="yyyy-MM-dd_HH-mm-ss" var=timestamp

# Создать имя архива
string.concat left="backup_" right="${timestamp}.zip" var=archive_name
path.join parts="C:\backups,${archive_name}" var=archive_path

# Создать архив
archive.create source=C:\important_data destination=${archive_path}
log.info message="Backup created: ${archive_path}"
```

### Валидация конфигурации

```wflow
# Проверить все обязательные переменные
isset var=API_KEY result=has_api
isset var=DATABASE_URL result=has_db
isset var=SECRET_KEY result=has_secret

if condition="${has_api} == false" body="log.error message='API_KEY not set'"
if condition="${has_db} == false" body="log.error message='DATABASE_URL not set'"
if condition="${has_secret} == false" body="log.error message='SECRET_KEY not set'"
```

### Обработка логов

```wflow
# Настроить логирование
datetime.now format="yyyy-MM-dd" var=date
string.concat left="logs/app-" right="${date}.log" var=logfile
log.config level=INFO file=${logfile}

# Работа с файлами
path.exists path="C:\data\input.txt" var=file_exists

if condition="${file_exists} == true" body="log.info message='Processing input file'"
if condition="${file_exists} == false" body="log.error message='Input file not found'"
```

### Очистка старых файлов

```wflow
# Найти файлы старше 30 дней
datetime.now var=now
datetime.add date=${now} days=-30 var=cutoff_date

# В будущих версиях можно будет итерировать по файлам
log.info message="Cleaning files older than ${cutoff_date}"
```

---

## Технические детали

- **Версия:** 0.2.3
- **Добавлено команд:** 29
- **Модули:** datetime (5), path (7), log (5), regex (3), archive (4), + isset
- **Совместимость:** Обратная совместимость с 0.2.1

---

## Примечания

### Формат даты datetime
Используйте стандартные .NET форматы:
- `yyyy-MM-dd` - 2024-12-18
- `dd/MM/yyyy` - 18/12/2024
- `HH:mm:ss` - 23:14:10
- `o` или `s` - ISO 8601

### Regex patterns
Используйте стандартный синтаксис .NET Regex:
- `\d+` - одна или более цифр
- `\w+` - одно или более слово
- `^...$` - начало и конец строки
- `[a-z]` - любой символ из диапазона

### Логирование
Цвета в консоли:
- ERROR - красный
- WARNING - желтый
- DEBUG - серый
- INFO - белый

---

## Обновление

Скачайте последнюю версию: https://github.com/silasWorked/WinFlow/releases/tag/v0.2.3

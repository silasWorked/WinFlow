# Вклад в проект

Спасибо за интерес к WinFlow! Мы рады любому вкладу — от исправления опечаток до реализации новых возможностей.

## Как помочь проекту?

### 🐛 Сообщить об ошибке

Нашли баг? [Создайте issue](https://github.com/silasWorked/WinFlow/issues/new) с описанием:

1. **Версия WinFlow**: `WinFlow.Cli.exe --version`
2. **ОС и версия**: Windows 10/11, build number
3. **Шаги воспроизведения**: минимальный .wflow скрипт
4. **Ожидаемое поведение**: что должно произойти
5. **Фактическое поведение**: что произошло
6. **Логи**: вывод с `--verbose`

**Шаблон issue:**
```markdown
## Описание
Краткое описание проблемы

## Воспроизведение
1. Создать файл test.wflow с кодом:
   ```wflow
   echo test
   ```
2. Запустить: `WinFlow.Cli.exe test.wflow`
3. Ошибка: ...

## Окружение
- WinFlow: 0.2.0
- OS: Windows 11 22H2
- .NET: 8.0.1

## Логи
```
[verbose output]
```
```

### 💡 Предложить идею

Есть идея улучшения? [Создайте discussion](https://github.com/silasWorked/WinFlow/discussions) с описанием:

1. **Проблема**: какую задачу решает идея
2. **Предлагаемое решение**: как это должно работать
3. **Альтернативы**: рассматривались ли другие варианты
4. **Пример использования**: .wflow код

### 📝 Улучшить документацию

Документация — важная часть проекта! Вы можете:

- Исправить опечатки и грамматические ошибки
- Добавить примеры использования
- Улучшить объяснения
- Перевести на другие языки

**Файлы документации:**
- `docs/*.md` — GitBook документация
- `README.md` — главная страница
- `SUMMARY.md` — оглавление GitBook

### 🔧 Внести код

## Настройка окружения разработки

### Требования

- **Windows 10/11** (x64)
- **.NET 8 SDK** ([скачать](https://dotnet.microsoft.com/download/dotnet/8.0))
- **Git**
- **IDE** (рекомендуется Rider или VS 2022)

### Клонирование и сборка

```powershell
# 1. Fork репозитория на GitHub

# 2. Клонировать ваш fork
git clone https://github.com/YOUR_USERNAME/WinFlow.git
cd WinFlow

# 3. Добавить upstream remote
git remote add upstream https://github.com/silasWorked/WinFlow.git

# 4. Собрать проект
dotnet build WinFlow.sln -c Debug

# 5. Запустить тесты
Get-ChildItem test-*.wflow | ForEach-Object {
    Write-Host "Testing: $($_.Name)" -ForegroundColor Cyan
    dotnet run --project WinFlow/WinFlow.Cli -- $_.FullName
}
```

### Структура проекта

```
WinFlow/
├── WinFlow.Core/              # Ядро языка
│   ├── AST/                   # Abstract Syntax Tree
│   ├── Parsing/               # Парсер .wflow файлов
│   │   └── WinFlowParser.cs   # Главный парсер
│   ├── Runtime/               # Исполнение
│   │   ├── CommandDispatcher.cs  # Диспетчер команд
│   │   └── TaskExecutor.cs    # Выполнение задач
│   └── Model/                 # Модели данных
│       ├── FlowModels.cs      # FlowTask, FlowCommand, FlowFunction
│       └── ExecutionContext.cs # Контекст выполнения
├── WinFlow.Cli/               # CLI приложение
│   └── Program.cs             # Entry point
├── WinFlow.ShellHost/         # Shell интеграция
└── WinFlow.Installer/         # Установщик
```

## Процесс разработки

### 1. Fork-and-pull flow (recommended)

Follow these steps to contribute using a fork (recommended for most contributors):

```powershell
# 0. Fork the repository on GitHub (button on the project page)

# 1. Clone your fork
git clone https://github.com/YOUR_USERNAME/WinFlow.git
cd WinFlow

# 2. Add upstream remote (if not already)
git remote add upstream https://github.com/silasWorked/WinFlow.git

# 3. Create a feature branch from upstream/main
git fetch upstream
git checkout -b feature/short-descriptive-name upstream/main

# 4. Make small, focused commits and keep your branch up-to-date:
# Commit often with clear messages (see Commit Messages section)
# If upstream/main advanced, rebase or merge regularly
git fetch upstream
git rebase upstream/main   # or: git merge upstream/main

# 5. Push to your fork and open a Pull Request
git push origin feature/short-descriptive-name
# Then open a PR on GitHub comparing your branch to upstream/main
```

Notes:
- Branch naming: `feature/<short>`, `fix/<short>`, `chore/<short>`.
- Prefer small PRs (< 500 changed lines) for easier review.

### 2. Внести изменения

**Следуйте стилю кода:**

```csharp
// ✅ Хорошо
public class MyClass
{
    private readonly string _fieldName;
    
    public string PropertyName { get; set; }
    
    public void MethodName(string parameterName)
    {
        // Код с отступами 4 пробела
        if (condition)
        {
            DoSomething();
        }
    }
}

// ❌ Плохо
public class myclass {
  private string fieldname;
  public string propertyname {get;set;}
  public void methodname(string parametername) {
    if(condition) DoSomething();
  }
}
```

**Комментарии:**
- Используйте XML-комментарии для публичных API
- Внутренняя логика — обычные комментарии
- Комментарии на русском или английском

### 3. Добавить тесты

Создайте `.wflow` файл в корне проекта:

```wflow
#/// Test: My Feature

// Тест основной функциональности
env set test_value="hello"
my_new_command input="${test_value}"

// Проверка результата
if condition="${result}" equals="expected":
    echo Test passed!
else:
    echo Test failed!
    exit code=1
```

### 4. Запустить тесты

```powershell
# Собрать проект
dotnet build WinFlow.sln -c Debug

# Запустить ваш тест
dotnet run --project WinFlow/WinFlow.Cli -- test-my-feature.wflow --verbose

# Запустить все тесты
Get-ChildItem test-*.wflow | ForEach-Object {
    dotnet run --project WinFlow/WinFlow.Cli -- $_.FullName
}
```

### 5. Commit и push

```powershell
# Добавить измененные файлы
git add .

# Commit с понятным сообщением
git commit -m "feat: Add my awesome feature

- Implement XYZ functionality
- Add tests for new feature
- Update documentation"

# Push в ваш fork
git push origin feature/my-awesome-feature
```

### 6. Создать Pull Request

1. Откройте [страницу PR](https://github.com/silasWorked/WinFlow/compare)
2. Выберите ваш fork и branch
3. Заполните описание PR:

```markdown
## Описание
Краткое описание изменений

## Связанные issues
Closes #123

## Тип изменения
- [ ] Bug fix
- [x] New feature
- [ ] Breaking change
- [ ] Documentation update

## Как протестировано?
- [x] Запущены существующие тесты
- [x] Добавлены новые тесты
- [x] Ручное тестирование

## Checklist
- [x] Код следует стилю проекта
- [x] Добавлены комментарии к сложному коду
- [x] Обновлена документация
- [x] Изменения не создают новых warnings
- [x] Добавлены тесты
```

## Соглашения

### Commit Messages

Следуем [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <subject>

<body>

<footer>
```

**Types:**
- `feat:` — новая возможность
- `fix:` — исправление бага
- `docs:` — изменения в документации
- `style:` — форматирование, пробелы (не влияет на код)
- `refactor:` — рефакторинг (не баг, не фича)
- `perf:` — улучшение производительности
- `test:` — добавление тестов
- `chore:` — обновление build, конфигурация

**Примеры:**
```
feat(parser): Add support for multiline function definitions

- Implement ParseFunctions() method
- Add indentation-based block parsing
- Update tests

Closes #42
```

```
fix(dispatcher): Function parameters don't pollute global scope

- Create isolated ExecutionContext for function calls
- Bind parameters locally
- Back-propagate non-param variables

Fixes #56
```

### Branch Naming

- `feature/описание` — новые возможности
- `fix/описание` — исправления багов
- `docs/описание` — документация
- `refactor/описание` — рефакторинг

### Code Review

Pull requests проходят code review перед merge. Ревьюеры проверят:

1. **Корректность** — код работает как задумано
2. **Тесты** — добавлены и проходят
3. **Стиль** — соответствует проекту
4. **Документация** — обновлена при необходимости
5. **Производительность** — нет явных проблем

## Добавление новой команды

### Пример: добавление команды `math.sum`

**1. Обновить CommandDispatcher.cs:**

```csharp
// В методе DispatchCommand добавить case
case "math":
    if (args.TryGetValue("sum", out var sumArgs))
    {
        HandleMathSum(cmd, context);
    }
    break;

// Добавить обработчик
private void HandleMathSum(FlowCommand cmd, ExecutionContext context)
{
    var a = GetArg(cmd, "a");
    var b = GetArg(cmd, "b");
    var resultVar = GetArg(cmd, "result", "MATH_RESULT");
    
    if (int.TryParse(a, out var numA) && int.TryParse(b, out var numB))
    {
        var sum = numA + numB;
        context.Environment[resultVar] = sum.ToString();
        
        if (_verbose)
        {
            Console.WriteLine($"math.sum a={a} b={b} result={sum}");
        }
    }
    else
    {
        throw new Exception("math.sum requires numeric arguments");
    }
}
```

**2. Добавить help в CommandHelp.cs:**

```csharp
case "math":
    return @"Math operations
    
    math.sum a=<num1> b=<num2> result=<var>
        Calculate sum of two numbers
        
        Example: math.sum a=3 b=5 result=SUM
                 echo Result: ${SUM}";
```

**3. Создать тест test-math.wflow:**

```wflow
#/// Test: Math Operations

math.sum a=3 b=5 result=SUM
echo Sum of 3 and 5: ${SUM}

// Expected output: Sum of 3 and 5: 8
```

**4. Обновить docs/commands.md:**

```markdown
### math.sum
- Назначение: вычислить сумму двух чисел
- Сигнатура: `math.sum a=<num1> b=<num2> result=<var>`
- Параметры:
  - `a` — первое число
  - `b` — второе число
  - `result` — имя переменной для результата (по умолчанию `MATH_RESULT`)
- Пример: `math.sum a=10 b=20 result=TOTAL`
```

## Вопросы?

- 📖 [Документация](quickstart.md)
- 💬 [Discussions](https://github.com/silasWorked/WinFlow/discussions)
- 🐛 [Issues](https://github.com/silasWorked/WinFlow/issues)

---

**Спасибо за вклад в WinFlow!** 🎉

# Архитектура и разработка

Техническая документация для разработчиков WinFlow.

## Обзор архитектуры

WinFlow построен на модульной архитектуре с четким разделением ответственности:

```
┌─────────────────────────────────────────┐
│         WinFlow.Cli (Entry Point)       │
│  - CLI parsing                           │
│  - Arguments handling                    │
└──────────────────┬──────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────┐
│         WinFlow.Core (Engine)           │
│                                          │
│  ┌────────────┐  ┌────────────────────┐│
│  │  Parsing   │  │      Runtime       ││
│  │            │  │                    ││
│  │  Parser ───┼──▶  CommandDispatcher││
│  │            │  │  TaskExecutor      ││
│  └────────────┘  └────────────────────┘│
│                                          │
│  ┌────────────────────────────────────┐│
│  │         Model                      ││
│  │  - FlowTask                        ││
│  │  - FlowCommand                     ││
│  │  - FlowFunction                    ││
│  │  - ExecutionContext                ││
│  └────────────────────────────────────┘│
└─────────────────────────────────────────┘
```

## Компоненты системы

### WinFlow.Cli

**Назначение**: Entry point приложения, обработка CLI аргументов

**Ключевые файлы:**
- `Program.cs` — main method, argument parsing

**Ответственность:**
```csharp
public class Program
{
    public static int Main(string[] args)
    {
        // 1. Parse CLI arguments (--verbose, --dry-run, --version)
        // 2. Load .wflow file
        // 3. Call WinFlowParser.Parse()
        // 4. Call TaskExecutor.Run()
        // 5. Return exit code
    }
}
```

### WinFlow.Core.Parsing

**Назначение**: Парсинг .wflow файлов в AST

**Ключевые классы:**

#### `WinFlowParser`

Главный парсер, преобразующий текст в структуру данных.

```csharp
public class WinFlowParser : IParser
{
    // Двухпроходный парсинг
    public List<FlowTask> Parse(string filePath)
    {
        var lines = File.ReadAllLines(filePath);
        
        // Pass 1: Collect and register functions
        var (functions, commandLines) = ParseFunctions(lines);
        
        // Pass 2: Parse commands (including function calls)
        var commands = ParseCommands(commandLines);
        
        return new List<FlowTask> { new FlowTask { Commands = commands } };
    }
    
    // Парсинг функций с многострочными телами
    private (List<FlowFunction>, List<string>) ParseFunctions(string[] lines);
    
    // Парсинг определения функции
    private FlowFunction ParseFunctionDef(string headerLine, string[] bodyLines);
    
    // Парсинг вызова функции: funcname(arg1, arg2)
    private FlowCommand ParseFunctionCall(string line);
    
    // Разбор аргументов: "val1, val2, val3" → ["val1", "val2", "val3"]
    private List<string> ParseFunctionCallArgs(string argsString);
}
```

**Особенности парсинга:**

1. **Комментарии**: `//`, `#`, `#///`
2. **Функции**: 
   - Заголовок: `define name(param1, param2):`
   - Тело: блок с отступом
3. **Команды**: `command key=value key="value with spaces"`
4. **Блоки**: `if`, `else`, `try`, `catch`, циклы

**Пример парсинга:**

```wflow
define greet(name):
    echo Hello ${name}!
    echo Welcome to WinFlow

greet("World")
```

Преобразуется в:

```csharp
// FlowFunction
{
    Name = "greet",
    Parameters = ["name"],
    Commands = [
        { Name = "echo", Args = { ["text"] = "Hello ${name}!" } },
        { Name = "echo", Args = { ["text"] = "Welcome to WinFlow" } }
    ]
}

// FlowCommand (function call)
{
    Name = "call",
    Args = {
        ["name"] = "greet",
        ["arg0"] = "World"
    }
}
```

### WinFlow.Core.Runtime

**Назначение**: Выполнение команд и управление контекстом

#### `CommandDispatcher`

Центральный диспетчер команд с поддержкой модулей.

```csharp
public class CommandDispatcher
{
    // Глобальный реестр функций
    private static Dictionary<string, FlowFunction> GlobalFunctions = new();
    
    // Регистрация функции (вызывается парсером)
    public static void RegisterFunction(FlowFunction function);
    
    // Диспетчеризация команды
    public void DispatchCommand(FlowCommand cmd, ExecutionContext context)
    {
        switch (cmd.Name)
        {
            case "echo": HandleEcho(cmd, context); break;
            case "env": HandleEnv(cmd, context); break;
            case "file": HandleFile(cmd, context); break;
            case "process": HandleProcess(cmd, context); break;
            case "json": HandleJson(cmd, context); break;
            case "net": HandleNet(cmd, context); break;
            case "array": HandleArray(cmd, context); break;
            case "loop": HandleLoop(cmd, context); break;
            case "if": HandleIf(cmd, context); break;
            case "try": HandleTry(cmd, context); break;
            case "call": HandleCall(cmd, context); break;
            // ... другие команды
        }
    }
    
    // Обработчик вызова функции
    private void HandleCall(FlowCommand cmd, ExecutionContext context)
    {
        var funcName = GetArg(cmd, "name");
        var func = GlobalFunctions[funcName];
        
        // Создать изолированный контекст
        var funcContext = new ExecutionContext 
        { 
            Environment = new Dictionary<string, string>(context.Environment)
        };
        
        // Привязать аргументы к параметрам
        for (int i = 0; i < func.Parameters.Count; i++)
        {
            var argValue = GetArg(cmd, $"arg{i}");
            funcContext.Environment[func.Parameters[i]] = ExpandVariables(argValue, context);
        }
        
        // Выполнить тело функции
        foreach (var command in func.Commands)
        {
            DispatchCommand(command, funcContext);
        }
        
        // Пропагировать изменения (кроме параметров)
        foreach (var kvp in funcContext.Environment)
        {
            if (!func.Parameters.Contains(kvp.Key))
            {
                context.Environment[kvp.Key] = kvp.Value;
            }
        }
    }
}
```

**Модули команд:**

| Модуль | Обработчик | Команды |
|--------|------------|---------|
| `echo` | `HandleEcho()` | `echo <text>` |
| `env` | `HandleEnv()` | `env set`, `env get`, `env print`, `env unset` |
| `file` | `HandleFile()` | `file read`, `file write`, `file copy`, etc. |
| `process` | `HandleProcess()` | `process.exec`, `process.start` |
| `json` | `HandleJson()` | `json.parse`, `json.get`, `json.set` |
| `net` | `HandleNet()` | `net download`, `net request` |
| `array` | `HandleArray()` | `array.create`, `array.add`, `array.get` |
| `loop` | `HandleLoop()` | `loop.repeat`, `loop.foreach` |
| `if` | `HandleIf()` | `if`, `else` |
| `try` | `HandleTry()` | `try`, `catch` |

#### `TaskExecutor`

Orchestrator выполнения задач.

```csharp
public class TaskExecutor
{
    public void Run(List<FlowTask> tasks, ExecutionContext context, bool dryRun, bool verbose)
    {
        foreach (var task in tasks)
        {
            Console.WriteLine($"[TASK] {task.Name}");
            
            foreach (var command in task.Commands)
            {
                if (dryRun)
                {
                    Console.WriteLine($"[DRY-RUN] {command.Name}");
                    continue;
                }
                
                try
                {
                    _dispatcher.DispatchCommand(command, context);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    Environment.Exit(1);
                }
            }
        }
    }
}
```

### WinFlow.Core.Model

**Назначение**: Модели данных

#### `FlowTask`

Контейнер для группы команд.

```csharp
public class FlowTask
{
    public string Name { get; set; }  // Имя задачи (обычно имя файла)
    public List<FlowCommand> Commands { get; set; } = new();
}
```

#### `FlowCommand`

Представление команды с аргументами.

```csharp
public class FlowCommand
{
    public string Name { get; set; }  // echo, env, file, etc.
    public Dictionary<string, string> Args { get; set; } = new();
    public List<FlowCommand> Body { get; set; } = new();  // Для блоков (if, loop, etc.)
    public object? Metadata { get; set; }  // Дополнительные данные
}
```

#### `FlowFunction`

Представление функции с параметрами.

```csharp
public class FlowFunction
{
    public string Name { get; set; }  // Имя функции
    public List<string> Parameters { get; set; } = new();  // [param1, param2, ...]
    public List<FlowCommand> Commands { get; set; } = new();  // Тело функции
}
```

#### `ExecutionContext`

Контекст выполнения скрипта.

```csharp
public class ExecutionContext
{
    public Dictionary<string, string> Environment { get; set; } = new();
    public Dictionary<string, FlowFunction> Functions { get; set; } = new();
    public Dictionary<string, List<string>> Arrays { get; set; } = new();
    public Dictionary<string, object> JsonData { get; set; } = new();
    
    public int LastExitCode { get; set; }
    public string LastOutput { get; set; }
}
```

## Потоки данных

### Парсинг и выполнение

```
.wflow file
    │
    ▼
WinFlowParser.Parse()
    │
    ├─▶ ParseFunctions()
    │   └─▶ RegisterFunction() → GlobalFunctions
    │
    ├─▶ ParseCommands()
    │   └─▶ ParseFunctionCall() если обнаружен вызов
    │
    ▼
List<FlowTask>
    │
    ▼
TaskExecutor.Run()
    │
    ▼
CommandDispatcher.DispatchCommand()
    │
    ├─▶ HandleEcho()
    ├─▶ HandleEnv()
    ├─▶ HandleCall() → выполнение функции
    │   │
    │   ├─▶ Создать ExecutionContext
    │   ├─▶ Привязать параметры
    │   ├─▶ Выполнить Commands
    │   └─▶ Пропагировать изменения
    │
    └─▶ ...другие обработчики
```

### Подстановка переменных

```
"Hello ${name}!"
    │
    ▼
ExpandVariables(text, context)
    │
    ├─▶ Regex.Matches(@"\$\{([^}]+)\}")
    │
    ├─▶ Для каждого match:
    │   └─▶ context.Environment[varName] ?? ""
    │
    ▼
"Hello World!"
```

## Расширение функциональности

### Добавление новой команды

**1. Обновить CommandDispatcher.cs:**

```csharp
public void DispatchCommand(FlowCommand cmd, ExecutionContext context)
{
    switch (cmd.Name)
    {
        // ... существующие команды
        
        case "mynewcommand":
            HandleMyNewCommand(cmd, context);
            break;
    }
}

private void HandleMyNewCommand(FlowCommand cmd, ExecutionContext context)
{
    // Получить аргументы
    var arg1 = GetArg(cmd, "arg1");
    var arg2 = GetArg(cmd, "arg2", "default_value");
    
    // Раскрыть переменные
    arg1 = ExpandVariables(arg1, context);
    
    // Выполнить логику
    var result = DoSomething(arg1, arg2);
    
    // Сохранить результат в контекст
    context.Environment["RESULT"] = result;
    
    // Вывод в verbose режиме
    if (_verbose)
    {
        Console.WriteLine($"mynewcommand arg1={arg1} arg2={arg2} result={result}");
    }
}
```

**2. Добавить help:**

```csharp
// CommandHelp.cs
case "mynewcommand":
    return @"My New Command
    
    mynewcommand arg1=<value> arg2=<value>
        Does something amazing
        
        Example: mynewcommand arg1=test arg2=demo";
```

**3. Добавить тест:**

```wflow
#/// Test: My New Command

mynewcommand arg1="test" arg2="value"
echo Result: ${RESULT}
```

### Добавление нового модуля

Для группировки команд (например, `database.query`, `database.connect`):

```csharp
case "database":
    if (cmd.Args.ContainsKey("connect"))
        HandleDatabaseConnect(cmd, context);
    else if (cmd.Args.ContainsKey("query"))
        HandleDatabaseQuery(cmd, context);
    else if (cmd.Args.ContainsKey("close"))
        HandleDatabaseClose(cmd, context);
    break;
```

## Тестирование

### Структура тестов

Тесты — это `.wflow` файлы в корне проекта:

```
test-*.wflow          # Основные тесты
test-echo.wflow       # echo команда
test-functions-v2.wflow  # Функции с параметрами
test-v019.wflow       # Возможности v0.1.9
```

### Запуск тестов

```powershell
# Все тесты
Get-ChildItem test-*.wflow | ForEach-Object {
    Write-Host "`nTesting: $($_.Name)" -ForegroundColor Cyan
    dotnet run --project WinFlow/WinFlow.Cli -- $_.FullName --verbose
    if ($LASTEXITCODE -ne 0) {
        Write-Host "FAILED: $($_.Name)" -ForegroundColor Red
        exit 1
    }
}

# Один тест
dotnet run --project WinFlow/WinFlow.Cli -- test-functions-v2.wflow --verbose
```

### Структура теста

```wflow
#/// Test: Feature Name

// Setup
env set test_input="value"

// Execute
mycommand input="${test_input}"

// Verify (manual or exit code)
if condition="${RESULT}" equals="expected":
    echo Test passed!
else:
    echo Test failed: expected 'expected', got '${RESULT}'
    exit code=1
```

## Производительность

### Профилирование

```csharp
// В Program.cs
var sw = Stopwatch.StartNew();
executor.Run(tasks, context, dryRun, verbose);
sw.Stop();

if (verbose)
{
    Console.WriteLine($"[PERF] Execution time: {sw.ElapsedMilliseconds}ms");
}
```

### Оптимизации

1. **Кэширование парсинга**: сохранять AST в `.wflow.cache`
2. **Ленивая подстановка**: не раскрывать переменные до использования
3. **Пулы объектов**: переиспользовать ExecutionContext

## Отладка

### Verbose режим

```powershell
WinFlow.Cli.exe script.wflow --verbose
```

Выводит:
- Каждую выполняемую команду с аргументами
- Результаты подстановки переменных
- Время выполнения

### Dry-run режим

```powershell
WinFlow.Cli.exe script.wflow --dry-run
```

Показывает:
- Что будет выполнено (без выполнения)
- Структуру парсинга
- Ошибки парсинга

### Отладка в IDE

В Rider/Visual Studio:

1. Установить breakpoint в `CommandDispatcher.DispatchCommand()`
2. Debug configuration: `WinFlow.Cli` с аргументами
3. F5 для запуска отладки

## CI/CD

### GitHub Actions

```yaml
# .github/workflows/ci.yml
name: CI

on: [push, pull_request]

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 8.0.x
      - name: Build
        run: dotnet build WinFlow.sln -c Release
      - name: Run tests
        run: |
          Get-ChildItem test-*.wflow | ForEach-Object {
            dotnet run --project WinFlow/WinFlow.Cli -- $_.FullName
          }
```

## См. также

- [Вклад в проект](contributing.md)
- [Roadmap](roadmap.md)
- [Changelog](changelog.md)

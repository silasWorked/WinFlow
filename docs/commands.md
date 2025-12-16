# Команды WinFlow (MVP)

Эта страница — справочник по командам. Сейчас доступны базовые встроенные.

## Встроенные

### echo
- Назначение: вывести строку в лог
- Сигнатура: `echo <text>`
- Аргументы:
  - `message` (внутреннее имя) — текст сообщения
- Примеры:
  - `echo "Hello"`
  - `echo Done`

### noop
- Назначение: заглушка, ничего не делает
- Сигнатура: `noop`
- Пример: `noop`

### env set
- Назначение: установить переменную контекста
- Сигнатура: `env set name=<VAR> value=<VALUE>`
- Псевдоним: можно использовать `key=` вместо `name=`
- Пример: `env set name=GREETING value="Hello"`

### env unset
- Назначение: удалить переменную из контекста
- Сигнатура: `env unset name=<VAR>`
- Пример: `env unset name=GREETING`

### env print
- Назначение: вывести переменные контекста
- Сигнатура: `env print`
- Пример: `env print`

### file write
- Назначение: создать/перезаписать файл
- Сигнатура: `file write path=<FILE> content=<TEXT>`
- Примечание: относительные пути — относительно рабочей папки скрипта
- Пример: `file write path="out.txt" content="Hello"`

### file append
- Назначение: дописать в конец файла
- Сигнатура: `file append path=<FILE> content=<TEXT>`
- Пример: `file append path="out.txt" content=" World"`

---

# Формат добавления новых команд (в ядре)
- Класс регистрируется в `CommandDispatcher`
- Сигнатура обработчика: `Execute(FlowCommand cmd, ExecutionContext ctx)`
- Ошибки сообщайте через исключения (будет перехват/логирование на уровне рантайма)

План модулей:
- Env: `env set`, `env unset`, `env print`
- File: `file write`, `file append`, `file copy`, `file delete`
- Process: `process run`, `process exec`, `process wait`
- Registry: `reg set`, `reg get`, `reg delete`

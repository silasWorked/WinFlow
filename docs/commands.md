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

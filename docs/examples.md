---
title: Примеры
nav_order: 7
---

# Примеры

## Полный демо-скрипт

```wflow
#/// WinFlow Demo - Multiple Tasks Showcase

echo message="[TASK 1] Setting up environment variables..."
env set name=APP_NAME value="WinFlow"
env set name=APP_VERSION value="0.1.2"
env print

echo message="[TASK 2] Writing configuration to file..."
file write path="app.config" content="APP_NAME=WinFlow\nAPP_VERSION=0.1.2"

# Async process (fire-and-forget)
process.run file="cmd.exe" args="/c echo background"

# Sync process (capture output)
process.exec file="cmd.exe" args="/c echo sync output"
```

## Быстрый шаблон проекта

```text
my-flow/
  demo.wflow
  README.md
```

`demo.wflow`:
```wflow
echo message="Project initialized"
```

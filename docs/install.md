---
title: Установка
nav_order: 6
---

# Установка

## Готовые сборки (рекомендуется)

1. Скачайте последнюю версию: https://github.com/silasWorked/WinFlow/releases/latest
2. Распакуйте архив `WinFlow.Cli` в удобную папку.
3. (Опционально) Добавьте папку в `PATH` пользователя.

## Установщик (консоль)

Инсталлятор добавляет ассоциацию `.wflow` и прописывает `PATH` на уровне пользователя (без прав администратора):

```powershell
# Установка по умолчанию
 dotnet run --project WinFlow/WinFlow.Installer.Cli

# Создать демо на рабочем столе
 dotnet run --project WinFlow/WinFlow.Installer.Cli -- --create-desktop-demo

# Удаление
 dotnet run --project WinFlow/WinFlow.Installer.Cli -- --uninstall --dir "%LOCALAPPDATA%\WinFlow"
```

См. также: [Ассоциация .wflow](association.md)

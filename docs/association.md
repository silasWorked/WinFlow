---
title: Ассоциация .wflow
nav_order: 5
---

# Ассоциация `.wflow` в Windows

Цель: запуск по дабл‑клику и иконка "WinFlow Script".

## Вариант для разработки (пользовательские ключи)
Скрипт: `WinFlow/WinFlow.Installer/register-wflow.ps1`

```powershell
powershell -ExecutionPolicy Bypass -File WinFlow/WinFlow.Installer/register-wflow.ps1
```

- Регистрирует:
  - `HKCU\Software\Classes\.wflow` → `WinFlow.Script`
  - `HKCU\Software\Classes\WinFlow.Script\shell\open\command` → путь к `WinFlow.Cli.exe` (Debug)

## Вариант для установщика (HKCR)
Пример REG-файла: `WinFlow/WinFlow.Installer/WinFlow.Script-HKCR.reg`

```reg
Windows Registry Editor Version 5.00

[HKEY_CLASSES_ROOT\.wflow]
@="WinFlow.Script"

[HKEY_CLASSES_ROOT\WinFlow.Script]
@="WinFlow Script"

[HKEY_CLASSES_ROOT\WinFlow.Script\DefaultIcon]
@="shell32.dll,70"

[HKEY_CLASSES_ROOT\WinFlow.Script\shell\open\command]
@="\"C:\\Program Files\\WinFlow\\winflow.exe\" \"%1\""
```

Примечания:
- Для HKCR обычно требуется админ-доступ и инсталлятор
- Убедитесь, что `winflow.exe` существует по указанному пути

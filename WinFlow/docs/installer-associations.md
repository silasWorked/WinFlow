# File Association and Context Menu (Installer)

WinFlow installer can register `.wflow` files to be opened with WinFlow CLI. This registers in the current user hive (`HKCU`) and does not require elevation.

## CLI options

- `--register-assoc` — register `.wflow` association using the CLI executable found next to installer, or provide `--assoc-target <path>` to explicitly set CLI path.
- `--unregister-assoc` — remove association keys created by the installer.
- `--no-assoc` — skip association step during normal install/uninstall.
- `--assoc-target <path>` — explicitly provide path to CLI executable to be used in registered commands.

Notes:
- Association registers `WinFlow.Script` ProgID and sets the default open command to `"<installDir>\winflow.exe" "%1"`.
- The installer also creates context menu verbs:
  - **Run with WinFlow** → `"winflow.exe" run "%1"`
  - **Debug with WinFlow** → `"winflow.exe" debug "%1"`
- This operation is safe for per-user registrations and avoids touching `HKLM`.

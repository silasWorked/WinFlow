# Installer UI (WinForms)

WinFlow provides a graphical installer prototype (`WinFlow.Installer.UI`) for Windows that lets users install or uninstall the CLI with a small, friendly UI.

## Features (MVP)
- Choose install location (default: `%LOCALAPPDATA%\WinFlow`)
- Options: register `.wflow` association, add to user PATH, create desktop demo
- Optional: explicit `assoc target` path for associating a specific `winflow.exe`
- Install and Uninstall actions with live log area showing progress

## Developer notes
- The UI calls into `WinFlow.Installer.Cli.InstallerService` which contains the actual install/uninstall logic.
- UI project: `WinFlow.Installer.UI` (WinForms) - built for .NET 8 on Windows.
- Packaging action: `.github/workflows/installer-package.yml` publishes a single-file self-contained EXE for `win-x64` on pushes to `main`.

## Running the UI locally
1. Build solution: `dotnet build WinFlow.sln`
2. Launch: `dotnet run --project WinFlow.Installer.UI` (Windows only)

## Next steps
- Add elevation workflow when HKLM install scope is introduced
- Create an MSI/MSIX packaging pipeline (WiX or MSIX) for production installer
- Add more automated UI/E2E tests (requires Windows runner)

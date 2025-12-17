# WinFlow CLI Commands

## System Commands

### Show Version
```powershell
wflow --version
# Output: WinFlow 0.1.0
```

### Show Help
```powershell
wflow --help
# Displays usage information and examples
```

### System Information
```powershell
wflow info
# Shows:
#   - Version
#   - .NET Runtime
#   - OS and Architecture
#   - Current directory
#   - Current user
```

### List Available Commands
```powershell
wflow list
# or
wflow commands
# Displays all built-in commands with descriptions
```

## Script Execution

### Basic Usage
```powershell
wflow <script.wflow>
```

### With Dry-Run (Simulation)
```powershell
wflow <script.wflow> --dry-run
# Simulates execution without making actual changes
```

### With Verbose Output
```powershell
wflow <script.wflow> --verbose
# or
wflow <script.wflow> -v
# Shows detailed execution logs
```

### Combined Options
```powershell
wflow setup.wflow --dry-run --verbose
# Run simulation with detailed logs
```

## Exit Codes

| Code | Meaning |
|------|---------|
| 0    | Success |
| 1    | Argument/usage error |
| 2    | Script file not found |
| 10+  | Execution error |

## Examples

```powershell
# Get version
wflow --version

# See available commands
wflow list

# Run script
wflow deploy.wflow

# Preview what would happen (dry-run)
wflow deploy.wflow --dry-run

# Run with diagnostics
wflow deploy.wflow --verbose

# Check system info before running
wflow info
wflow demo.wflow
```

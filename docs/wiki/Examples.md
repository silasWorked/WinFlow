# WinFlow Examples

## Full Demo Script

```wflow
#/// WinFlow Demo - Multiple Tasks Showcase
# This script demonstrates environment setup, file operations, and process execution

#/// Environment Setup Task
echo message="[TASK 1] Setting up environment variables..."
env set name=APP_NAME value="WinFlow"
env set name=APP_VERSION value="0.1.0"
env set name=WORK_DIR value="C:\temp\winflow"
env print
echo message="[TASK 1] Environment configured."

#/// File Operations Task
echo message="[TASK 2] Writing configuration to file..."
file write path="app.config" content="APP_NAME=WinFlow"
file append path="app.config" content="APP_VERSION=0.1.0"
file append path="app.config" content="TIMESTAMP=2025-12-17"
echo message="[TASK 2] Configuration file created."

#/// Process Execution - Async
echo message="[TASK 3] Starting background process..."
process.run file="cmd.exe" args="/c echo Background task started at %TIME%"
echo message="[TASK 3] Background process fired."

#/// Process Execution - Sync
echo message="[TASK 4] Running synchronous process with capture..."
process.exec file="cmd.exe" args="/c echo WinFlow is running on %COMPUTERNAME%"
echo message="[TASK 4] Process completed."

echo message="All tasks finished successfully!"
```

## Running the Demo

```powershell
# Using the CLI
WinFlow.exe demo.wflow --verbose

# Expected output shows all 4 tasks executing
```

## Simple Environment Setup

```wflow
echo message="Setting up application..."
env set name=DEBUG value="true"
env set name=LOG_LEVEL value="INFO"
env print
```

## File Creation and Editing

```wflow
file write path="config.ini" content="[Settings]"
file append path="config.ini" content="version=1.0"
file append path="config.ini" content="debug=false"
```

## Running External Programs

```wflow
# Async execution - doesn't wait for completion
process.run file="notepad.exe" args="document.txt"

# Sync execution - waits and captures output
process.exec file="cmd.exe" args="/c dir"
```

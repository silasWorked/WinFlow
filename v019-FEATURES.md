# WinFlow v0.1.9 Release

## New Features

### 1. JSON Module
Parse and navigate JSON documents:
- `json.parse text='{"key":"value"}' var=DATA` - Parse JSON
- `json.get text='{"user":{"name":"Alice"}}' path=user.name var=NAME` - Navigate with dot notation

### 2. HTTP Module  
Make HTTP requests:
- `http.get url="https://api.example.com" var=RESPONSE` - GET request
- `http.post url="https://api.example.com" body='{"data":"value"}' var=RESPONSE` - POST request
- `http.put url="https://api.example.com" body='{"data":"value"}' var=RESPONSE` - PUT request

### 3. Array Module
Work with JSON arrays:
- `array.split text="a,b,c" sep="," var=ARRAY` - Split string into array
- `array.join array='["a","b","c"]' sep="," var=RESULT` - Join array to string
- `array.length array='[1,2,3]' var=LEN` - Get array length

### 4. Try-Catch Error Handling
Safely handle errors:
- `try body="file.read path=nonexistent.txt" catch="echo Error: ${_error}"`
- Error message is stored in `${_error}` variable

### 5. Define and Call Functions
Reuse command sequences:
- `define name=greet body="echo Hello ${NAME}!"`
- `call name=greet`
- Pass arguments with arg0=, arg1=, etc: `call name=func arg0=value arg1=data`

## Example Script
```wflow
// Set up data
env.set name=API_URL value="https://jsonplaceholder.typicode.com"

// Define a function
define name=fetch_data body="http.get url=${API_URL}/posts/1 var=RESPONSE"

// Call the function
call name=fetch_data

// Parse JSON response
json.get text=${RESPONSE} path=userId var=USER_ID
echo "User ID: ${USER_ID}"
```

## All Implemented Commands (v0.1.9)
- Core: echo, noop
- env: set, unset, print
- file: write, append, mkdir, delete, copy, move, exists, read
- process: run, exec
- reg: set, get, delete (Windows)
- sleep: ms, sec
- net: download
- loop: repeat, foreach
- string: replace, contains, length, upper, lower, trim
- json: parse, get
- http: get, post, put
- array: split, join, length
- control: if (with condition operators), try-catch, include
- functions: define, call

## Version History
- v0.1.5: Core features (loops, variables, shell, conditionals)
- v0.1.6: Shell history, help command, file operations
- v0.1.7: String manipulation
- v0.1.8: --url flag for downloading scripts
- **v0.1.9: JSON, HTTP, Array operations, Try-catch, Functions**

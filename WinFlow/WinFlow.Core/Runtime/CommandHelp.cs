using System;
using System.Collections.Generic;

namespace WinFlow.Core.Runtime
{
    public static class CommandHelp
    {
        private static readonly Dictionary<string, string> HelpTexts = new(StringComparer.OrdinalIgnoreCase)
        {
            ["noop"] = "noop - No operation (placeholder)\nUsage: noop",
            ["echo"] = "echo - Output text message\nUsage: echo message=\"<text>\"\nExample: echo message=\"Hello World\"",
            
            ["env.set"] = "env.set - Set environment variable\nUsage: env.set name=<VAR> value=<VALUE>\nAlias: key= instead of name=\nExample: env.set name=APP_NAME value=\"WinFlow\"",
            ["env.unset"] = "env.unset - Remove environment variable\nUsage: env.unset name=<VAR>\nExample: env.unset name=TEMP_VAR",
            ["env.print"] = "env.print - Display all environment variables\nUsage: env.print",
            
            ["file.write"] = "file.write - Create or overwrite file\nUsage: file.write path=<FILE> content=<TEXT>\nExample: file.write path=\"config.txt\" content=\"data\"",
            ["file.append"] = "file.append - Append content to file\nUsage: file.append path=<FILE> content=<TEXT>\nExample: file.append path=\"log.txt\" content=\"new line\"",
            ["file.mkdir"] = "file.mkdir - Create directory\nUsage: file.mkdir path=<DIR>\nExample: file.mkdir path=\"output\"",
            ["file.delete"] = "file.delete - Delete file or directory\nUsage: file.delete path=<FILE|DIR> [recursive=true|false]\nExample: file.delete path=\"temp\" recursive=true",
            ["file.copy"] = "file.copy - Copy file\nUsage: file.copy src=<FILE> dst=<FILE> [overwrite=true|false]\nExample: file.copy src=\"a.txt\" dst=\"b.txt\"",
            ["file.move"] = "file.move - Move or rename file\nUsage: file.move src=<FILE> dst=<FILE> [overwrite=true|false]\nExample: file.move src=\"old.txt\" dst=\"new.txt\"",
            ["file.exists"] = "file.exists - Check if file or directory exists\nUsage: file.exists path=<PATH> [var=<VARNAME>]\nReturns: Sets variable or prints true/false\nExample: file.exists path=\"config.txt\" var=EXISTS",
            ["file.read"] = "file.read - Read file content\nUsage: file.read path=<FILE> [var=<VARNAME>]\nReturns: Sets variable or prints to console\nExample: file.read path=\"config.txt\" var=CONFIG",
            
            ["process.run"] = "process.run - Start process (async, fire-and-forget)\nUsage: process.run file=<EXE> [args=<ARGS>]\nExample: process.run file=\"notepad.exe\" args=\"readme.txt\"",
            ["process.exec"] = "process.exec - Execute process and wait (captures output)\nUsage: process.exec file=<EXE> [args=<ARGS>]\nExample: process.exec file=\"cmd.exe\" args=\"/c dir\"",
            
            ["reg.set"] = "reg.set - Set registry value (Windows)\nUsage: reg.set [hive=HKCU|HKLM] key=<PATH> name=<NAME> value=<VAL> [type=STRING|DWORD|QWORD|MULTI|EXPAND]\nDefaults: hive=HKCU, type=STRING\nExample: reg.set key=\"Software\\MyApp\" name=\"Version\" value=\"1.0\"",
            ["reg.get"] = "reg.get - Get registry value (Windows)\nUsage: reg.get [hive=HKCU|HKLM] key=<PATH> name=<NAME>\nExample: reg.get key=\"Software\\MyApp\" name=\"Version\"",
            ["reg.delete"] = "reg.delete - Delete registry value or key (Windows)\nUsage: reg.delete [hive=HKCU|HKLM] key=<PATH> [name=<NAME>]\nNote: If name is omitted, deletes entire key\nExample: reg.delete key=\"Software\\MyApp\" name=\"OldValue\"",
            
            ["sleep.ms"] = "sleep.ms - Pause execution (milliseconds)\nUsage: sleep.ms ms=<INT>\nExample: sleep.ms ms=500",
            ["sleep.sec"] = "sleep.sec - Pause execution (seconds)\nUsage: sleep.sec sec=<INT>\nExample: sleep.sec sec=2",
            
            ["net.download"] = "net.download - Download file via HTTP(S)\nUsage: net.download url=<URL> path=<FILE>\nExample: net.download url=\"https://example.com/data.zip\" path=\"data.zip\"",
            
            ["loop.repeat"] = "loop.repeat - Execute body N times\nUsage: loop.repeat count=<N> body=\"<command>\"\nVariables: ${index} (0-based)\nExample: loop.repeat count=3 body=\"echo pass ${index}\"",
            ["loop.foreach"] = "loop.foreach - Iterate over list items\nUsage: loop.foreach items=\"<list>\" [sep=\",\"] [var=item] body=\"<command>\"\nVariables: ${index}, ${<var>}\nExample: loop.foreach items=\"a;b;c\" sep=\";\" var=x body=\"echo ${x}\"",
            
            ["string.replace"] = "string.replace - Replace text in string\nUsage: string.replace text=<STR> from=<PATTERN> to=<REPLACEMENT> [var=<VARNAME>]\nExample: string.replace text=\"hello world\" from=\"world\" to=\"WinFlow\"",
            ["string.contains"] = "string.contains - Check if string contains pattern\nUsage: string.contains text=<STR> pattern=<PATTERN> [var=<VARNAME>]\nReturns: true/false\nExample: string.contains text=\"hello\" pattern=\"ell\" var=FOUND",
            ["string.length"] = "string.length - Get string length\nUsage: string.length text=<STR> [var=<VARNAME>]\nExample: string.length text=\"hello\" var=LEN",
            ["string.upper"] = "string.upper - Convert string to uppercase\nUsage: string.upper text=<STR> [var=<VARNAME>]\nExample: string.upper text=\"hello\" var=UPPER",
            ["string.lower"] = "string.lower - Convert string to lowercase\nUsage: string.lower text=<STR> [var=<VARNAME>]\nExample: string.lower text=\"HELLO\" var=LOWER",
            ["string.trim"] = "string.trim - Remove leading/trailing whitespace\nUsage: string.trim text=<STR> [var=<VARNAME>]\nExample: string.trim text=\"  hello  \" var=TRIMMED",
            
            ["json.parse"] = "json.parse - Parse and validate JSON\nUsage: json.parse text=<JSON> [var=<VARNAME>]\nExample: json.parse text='{\"key\":\"value\"}' var=DATA",
            ["json.get"] = "json.get - Extract value from JSON by path\nUsage: json.get text=<JSON> path=<PATH> [var=<VARNAME>]\nPath format: key1.key2 or key1.0 for arrays\nExample: json.get text='{\"user\":{\"name\":\"John\"}}' path=user.name var=NAME",
            
            ["http.get"] = "http.get - Make HTTP GET request\nUsage: http.get url=<URL> [var=<VARNAME>]\nExample: http.get url=\"https://api.example.com/data\" var=RESPONSE",
            ["http.post"] = "http.post - Make HTTP POST request\nUsage: http.post url=<URL> [body=<JSON>] [var=<VARNAME>]\nExample: http.post url=\"https://api.example.com/endpoint\" body='{\"key\":\"value\"}' var=RESPONSE",
            ["http.put"] = "http.put - Make HTTP PUT request\nUsage: http.put url=<URL> [body=<JSON>] [var=<VARNAME>]\nExample: http.put url=\"https://api.example.com/resource\" body='{\"key\":\"value\"}' var=RESPONSE",
            
            ["array.split"] = "array.split - Split string into JSON array\nUsage: array.split text=<STR> [sep=\",\"] [var=<VARNAME>]\nExample: array.split text=\"a,b,c\" sep=\",\" var=ARR",
            ["array.join"] = "array.join - Join JSON array into string\nUsage: array.join array=<JSON_ARRAY> [sep=\",\"] [var=<VARNAME>]\nExample: array.join array='[\"a\",\"b\",\"c\"]' sep=\"-\" var=RESULT",
            ["array.length"] = "array.length - Get JSON array length\nUsage: array.length array=<JSON_ARRAY> [var=<VARNAME>]\nExample: array.length array='[1,2,3]' var=LEN",
            
            ["if"] = "if - Conditional execution\nUsage: if condition=\"<expr>\" body=\"<command>\" [else=\"<command>\"]\nOperators: ==, !=, >, <, exists\nExample: if condition=\"${VAR} == value\" body=\"echo matched\"",
            
            ["include"] = "include - Include commands from another script\nUsage: include path=\"<script.wflow>\"\nExample: include path=\"common.wflow\"",
        };

        public static string GetHelp(string commandName)
        {
            if (HelpTexts.TryGetValue(commandName, out var help))
                return help;
            return $"No help available for '{commandName}'.";
        }

        public static bool HasHelp(string commandName)
        {
            return HelpTexts.ContainsKey(commandName);
        }

        public static IEnumerable<string> GetAllCommandNames()
        {
            return HelpTexts.Keys;
        }
    }
}

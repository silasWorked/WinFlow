using System;
using System.Collections.Generic;
using ExecutionContext = WinFlow.Core.Model.ExecutionContext;
using WinFlow.Core.Model;

namespace WinFlow.Core.Runtime
{
    public class CommandDispatcher
    {
        private readonly Dictionary<string, Action<FlowCommand, ExecutionContext>> _handlers =
            new(StringComparer.OrdinalIgnoreCase);

        public CommandDispatcher()
        {
            RegisterBuiltIns();
        }

        public void Register(string name, Action<FlowCommand, ExecutionContext> handler)
        {
            _handlers[name] = handler;
        }

        public void Execute(FlowCommand command, ExecutionContext context)
        {
            if (!_handlers.TryGetValue(command.Name, out var handler))
                throw new InvalidOperationException($"Unknown command: {command.Name}");

            if (context.DryRun)
            {
                context.Log($"[dry-run] {command.Name} {FormatArgs(command.Args)}");
                return;
            }

            handler(command, context);
        }

        private static string FormatArgs(Dictionary<string, string> args)
        {
            var parts = new List<string>();
            foreach (var kv in args)
                parts.Add($"{kv.Key}='{kv.Value}'");
            return string.Join(" ", parts);
        }

        private void RegisterBuiltIns()
        {
            Register("noop", (cmd, ctx) =>
            {
                ctx.Log("noop");
            });

            Register("echo", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("message", out var message))
                    message = string.Empty;
                ctx.Log(message);
            });

            // Env module
            Register("env.set", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("name", out var name) && !cmd.Args.TryGetValue("key", out name))
                    throw new ArgumentException("env.set requires name=<VAR> or key=<VAR>");
                if (!cmd.Args.TryGetValue("value", out var value))
                    value = string.Empty;
                ctx.Environment[name] = value;
                ctx.Log($"env set {name}='{value}'");
            });

            Register("env.unset", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("name", out var name) && !cmd.Args.TryGetValue("key", out name))
                    throw new ArgumentException("env.unset requires name=<VAR> or key=<VAR>");
                ctx.Environment.Remove(name);
                ctx.Log($"env unset {name}");
            });

            Register("env.print", (cmd, ctx) =>
            {
                foreach (var kv in ctx.Environment)
                    ctx.Log($"{kv.Key}={kv.Value}");
            });

            // File module
            Register("file.write", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("path", out var path))
                    throw new ArgumentException("file.write requires path=<file>");
                cmd.Args.TryGetValue("content", out var content);
                var full = System.IO.Path.IsPathRooted(path)
                    ? path
                    : System.IO.Path.Combine(ctx.WorkingDirectory, path);
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
                System.IO.File.WriteAllText(full, content ?? string.Empty);
                ctx.Log($"wrote {path}");
            });

            Register("file.append", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("path", out var path))
                    throw new ArgumentException("file.append requires path=<file>");
                cmd.Args.TryGetValue("content", out var content);
                var full = System.IO.Path.IsPathRooted(path)
                    ? path
                    : System.IO.Path.Combine(ctx.WorkingDirectory, path);
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
                System.IO.File.AppendAllText(full, content ?? string.Empty);
                ctx.Log($"appended {path}");
            });

            // Process module
            Register("process.run", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("file", out var file))
                    throw new ArgumentException("process.run requires file=<exe>");
                cmd.Args.TryGetValue("args", out var args);
                var proc = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = file,
                    Arguments = args ?? string.Empty,
                    WorkingDirectory = ctx.WorkingDirectory,
                    UseShellExecute = true
                });
                ctx.Log($"process run {file} started (PID: {proc?.Id})");
            });

            Register("process.exec", (cmd, ctx) =>
            {
                if (!cmd.Args.TryGetValue("file", out var file))
                    throw new ArgumentException("process.exec requires file=<exe>");
                cmd.Args.TryGetValue("args", out var args);
                var proc = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = file,
                        Arguments = args ?? string.Empty,
                        WorkingDirectory = ctx.WorkingDirectory,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };
                proc.Start();
                proc.WaitForExit();
                var output = proc.StandardOutput.ReadToEnd();
                var error = proc.StandardError.ReadToEnd();
                if (!string.IsNullOrEmpty(output)) ctx.Log(output);
                if (!string.IsNullOrEmpty(error)) ctx.Log("[error] " + error);
                ctx.Log($"process exec {file} exited with code {proc.ExitCode}");
            });
        }
    }
}
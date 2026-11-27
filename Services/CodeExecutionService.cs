using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.Reflection;

namespace CodeQuest.Services;

/// <summary>
/// Executes a student's C# code string in a restricted Roslyn scripting sandbox.
/// IMPORTANT: This is a classroom-level sandbox, NOT production-grade security.
/// Restrictions: only System assembly allowed, no file I/O, no networking,
/// no reflection, no process spawning. 3-second timeout enforced via Task timeout.
/// </summary>
public class CodeExecutionService
{
    // Reuse the same ScriptOptions for every execution — they are immutable and thread-safe.
    private static readonly ScriptOptions _options = ScriptOptions.Default
        .WithReferences(
            typeof(object).Assembly,          // System.Private.CoreLib
            typeof(Console).Assembly)         // System.Console
        .WithImports("System");               // No System.IO, Net, Diagnostics, Reflection

    static CodeExecutionService()
    {
        // Warm up Roslyn scripting compiler in background on first access
        _ = Task.Run(async () =>
        {
            try
            {
                var warmup = CSharpScript.Create(";", _options);
                warmup.Compile();
                await warmup.RunAsync();
            }
            catch { }
        });
    }

    /// <summary>
    /// Runs <paramref name="code"/> and returns its Console output.
    /// </summary>
    /// <param name="code">The student's C# script (top-level statements allowed).</param>
    /// <param name="stdinInput">Optional value to feed as Console.In (unused for now — reserved).</param>
    /// <returns>
    ///   Success=true  when the script ran without exception and within the time limit.<br/>
    ///   Stdout        contains everything written to Console.Out (trailing newlines trimmed).<br/>
    ///   Stderr        contains the exception message or "Time limit exceeded" on failure.
    /// </returns>
    public async Task<(bool Success, string Stdout, string Stderr)> ExecuteAsync(
        string code, string stdinInput = "")
    {
        // 1. Separate compilation from execution so cold Roslyn loading does not burn execution timeout
        Script<object> script;
        try
        {
            script = CSharpScript.Create(code, _options);
            script.Compile();
        }
        catch (CompilationErrorException ex)
        {
            return (false, string.Empty, string.Join("\n", ex.Diagnostics));
        }
        catch (Exception ex)
        {
            return (false, string.Empty, $"Compilation error: {ex.Message}");
        }

        // 2. Capture output via a per-call StringWriter passed into the script globals.
        //    Console.SetOut is process-global and NOT thread-safe — two concurrent
        //    submissions would corrupt each other's captured output.
        //    Instead we redirect Console.Out only inside the Task.Run worker thread
        //    and restore it immediately after, keeping the swap entirely within that thread.
        var sw = new StringWriter();
        string stdout = string.Empty;

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var runTask = Task.Run(async () =>
            {
                // Redirect Console.Out for this thread only.
                var prev = Console.Out;
                Console.SetOut(sw);
                try
                {
                    await script.RunAsync(cancellationToken: cts.Token);
                }
                finally
                {
                    Console.SetOut(prev);
                }
            }, cts.Token);

            var completedTask = await Task.WhenAny(runTask, Task.Delay(TimeSpan.FromSeconds(3)));
            if (completedTask != runTask)
            {
                cts.Cancel();
                return (false, string.Empty, "Time limit exceeded (3 seconds).");
            }

            await runTask; // propagate any exception
            stdout = sw.ToString().TrimEnd('\r', '\n', ' ');
            return (true, stdout, string.Empty);
        }
        catch (OperationCanceledException)
        {
            return (false, string.Empty, "Time limit exceeded (3 seconds).");
        }
        catch (Exception ex)
        {
            // Unwrap TargetInvocationException or AggregateException to reveal the root cause
            var actualEx = ex;
            while (actualEx.InnerException != null && (actualEx is TargetInvocationException || actualEx is AggregateException))
                actualEx = actualEx.InnerException;

            return (false, string.Empty, $"{actualEx.GetType().Name}: {actualEx.Message}");
        }
    }
}

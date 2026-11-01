using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace CodeQuest.Services;

/// <summary>
/// Executes a student's C# code string in a restricted Roslyn scripting sandbox.
/// IMPORTANT: This is a classroom-level sandbox, NOT production-grade security.
/// Restrictions: only System assembly allowed, no file I/O, no networking,
/// no reflection, no process spawning. 3-second timeout enforced via CancellationToken.
/// </summary>
public class CodeExecutionService
{
    // Reuse the same ScriptOptions for every execution — they are immutable and thread-safe.
    private static readonly ScriptOptions _options = ScriptOptions.Default
        .WithReferences(
            typeof(object).Assembly,          // System.Private.CoreLib
            typeof(Console).Assembly)         // System.Console
        .WithImports("System");               // No System.IO, Net, Diagnostics, Reflection

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
        // Capture Console.Out so we can read what the script printed.
        var originalOut = Console.Out;
        var sw = new StringWriter();
        Console.SetOut(sw);

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await CSharpScript.RunAsync(code, _options, cancellationToken: cts.Token);
            string stdout = sw.ToString().TrimEnd('\r', '\n', ' ');
            return (true, stdout, string.Empty);
        }
        catch (OperationCanceledException)
        {
            return (false, string.Empty, "Time limit exceeded (3 seconds).");
        }
        catch (CompilationErrorException ex)
        {
            // Compilation errors — return friendly message
            return (false, string.Empty, string.Join("\n", ex.Diagnostics));
        }
        catch (Exception ex)
        {
            // Runtime exceptions (NullReferenceException, DivideByZeroException, etc.)
            return (false, string.Empty, $"{ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            // Always restore Console.Out — critical in a long-running server process.
            Console.SetOut(originalOut);
        }
    }
}

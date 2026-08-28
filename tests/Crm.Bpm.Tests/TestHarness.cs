namespace Crm.Bpm.Tests;

/// Minimal runner so the engine stays testable without pulling a test framework into the solution.
public static class TestHarness
{
    private static readonly List<string> _failures = [];
    private static int _passed;

    public static async Task RunAsync(string name, Func<Task> body)
    {
        try
        {
            await body();
            _passed++;
            Console.WriteLine($"  PASS  {name}");
        }
        catch (Exception exception)
        {
            _failures.Add($"{name}: {exception.Message}");
            Console.WriteLine($"  FAIL  {name}");
            Console.WriteLine($"        {exception.Message}");
        }
    }

    public static int Report()
    {
        Console.WriteLine();
        Console.WriteLine($"{_passed} passed, {_failures.Count} failed");
        return _failures.Count == 0 ? 0 : 1;
    }

    public static void AreEqual<T>(T expected, T actual, string because)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{because}: expected '{expected}', got '{actual}'.");
        }
    }

    public static void IsTrue(bool condition, string because)
    {
        if (!condition)
        {
            throw new InvalidOperationException(because);
        }
    }
}

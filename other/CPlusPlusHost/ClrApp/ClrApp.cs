using clojure.lang;
using System.Runtime.InteropServices;

namespace ClrApp;

public static class ClrApp
{
    private static Var _evalVar = null;
    private static bool _initialized = false;

    private static void EnsureInitialized()
    {
        if (_initialized) return;
        RT.Init();
        _evalVar = RT.var("clojure.core", "eval");
        Console.WriteLine("CLR App initialized.");
        _initialized = true;
    }

    [UnmanagedCallersOnly(EntryPoint = "EvalClojure")]
    public static int EvalClojure(IntPtr ptr, int len)
    {
        try
        {
            EnsureInitialized();

            // Convert UTF‑8 input to string
            string code = Marshal.PtrToStringUTF8(ptr, len) ?? "";

            var result = _evalVar?.invoke(RT.readString(code));

            Console.WriteLine($"Received Clojure code: {code}");
            Console.WriteLine($"Evaluation result: {result}");

            return 0; // Placeholder for actual evaluation result
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error evaluating Clojure code: {ex.Message}");
            PrintStackTrace(ex);
            return -1; // Indicate an error occurred
        }
    }

    private static void PrintStackTrace(Exception ex)
    {
        if (ex.InnerException != null)
        {
            Console.WriteLine("===Start Inner Exception ===");
            PrintStackTrace(ex.InnerException);
            Console.WriteLine("=== End  Inner Exception ===");
        }

        Console.WriteLine($"Exception: {ex.GetType().FullName}");
        Console.WriteLine($"Message: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
    }
}

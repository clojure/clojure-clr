using clojure.lang;
using System.Runtime.InteropServices;

namespace ClrApp;

public static class ClrApp
{
    static Var EvalVar = null;

    static ClrApp()
    {
        RT.Init();
        EvalVar = RT.var("clojure.core", "eval");
        Console.WriteLine("CLR App initialized.");
    }

    [UnmanagedCallersOnly(EntryPoint = "EvalClojure")]
    public static int EvalClojure(IntPtr ptr, int len)
    {
        try
        {
            // Convert UTF‑8 input to string
            string code = Marshal.PtrToStringUTF8(ptr, len) ?? "";

            var result = EvalVar?.invoke(RT.readString(code));

            Console.WriteLine($"Received Clojure code: {code}");
            Console.WriteLine($"Evaluation result: {result}");

            return 0; // Placeholder for actual evaluation result
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error evaluating Clojure code: {ex.Message}");
            return -1; // Indicate an error occurred
        }
    }
}

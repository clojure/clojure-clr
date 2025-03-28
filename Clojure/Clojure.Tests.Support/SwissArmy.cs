using System;

namespace clojure.test;

// duplicates class in JVM code

public class SwissArmy
{
    public string ctorId;


    public SwissArmy() { this.ctorId = "1"; }
    public SwissArmy(int a, long b) { this.ctorId = "2"; }
    public SwissArmy(long a, int b) { this.ctorId = "3"; }
    public SwissArmy(bool a, bool b) { this.ctorId = "4"; }
    public SwissArmy(long[] a, int b) { this.ctorId = "5"; }
    public SwissArmy(String[] a, long b) { this.ctorId = "6"; }
    //public SwissArmy(int[][] a, long b) { this.ctorId = "7"; }

    public String noArgs() { return ""; }
    public String twoArgsIL(int a, long b) { return "System.Int32-System.Int64"; }
    public String twoArgsLI(long a, int b) { return "System.Int64-System.Int32"; }
    public String twoArgsBB(bool a, bool b) { return "System.Boolean-System.Boolean"; }
    //    public String twoArgsLAI(long[] a, int b) {return "long<>-int";}
    public String twoArgsSAL(String[] a, long b) { return "System.String[]-System.Int64"; }
    //    public String twoArgsMDIL(int[][] a, long b) {return "int<><>-long";}
    public String arityOverloadMethod(int a) { return "System.Int32"; }
    public String arityOverloadMethod(int a, int b) { return "System.Int32-System.Int32"; }
    public String arityOverloadMethod(int a, int b, int c) { return "System.Int32-System.Int32-System.Int32"; }
    public String doppelganger(int a, int b) { return "System.Int32-System.Int32"; }

    public static String staticNoArgs() { return ""; }
    public static String staticOneArg(bool a) { return "System.Boolean"; }
    public static String staticTwoArgsIL(int a, long b) { return "System.Int32-System.Int64"; }
    public static String staticTwoArgsLI(long a, int b) { return "System.Int64-System.Int32"; }
    public static String staticTwoArgsBB(bool a, bool b) { return "System.Boolean-System.Boolean"; }
    public static String staticTwoArgsSAL(String[] a, long b) { return "System.String[]-System.Int64"; }
    //    public static String staticTwoArgsMDIL(int[][] a, long b) {return "int<><>-long";}
    //    public static String couldReflect(long[] a, int b) {return "long<>-int";}
    public static String couldReflect(Object[] a, int b) { return "System.Object[]-System.Int32"; }
    public static String staticArityOverloadMethod(int a) { return "System.Int32"; }
    public static String staticArityOverloadMethod(int a, int b) { return "System.Int32-System.Int32"; }
    public static String staticArityOverloadMethod(int a, int b, int c) { return "System.Int32-System.Int32-System.Int32"; }
    public static String doppelganger(int a, int b, long c) { return "System.Int32-System.Int32-System.Int64"; }
}

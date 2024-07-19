// See https://aka.ms/new-console-template for more information
//using clojure.lang;
//using System.Reflection;

//Console.WriteLine("Hello, World!");


//Assembly.LoadFrom("clojure.data.priority-map.dll");

//IFn require = clojure.clr.api.Clojure.var("clojure.core", "load");
//require.invoke("clojure.data.priority-map");


clojure.lang.IPersistentMap pv = clojure.lang.PersistentHashMap.EMPTY;

for (int i = 0; i <= 500; i++)
{    
    pv = pv.assoc("a"+i, i);
}


((clojure.lang.PersistentHashMap)pv).PrintContents();

Console.ReadLine();

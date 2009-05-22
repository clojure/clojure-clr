using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using clojure.lang;
using System.Diagnostics;
using System.IO;
using Microsoft.Linq.Expressions;
using Microsoft.Scripting.Generation;

namespace clojure.console
{
    class SimpleConsole
    {
        static void Main(string[] args)
        {
            new SimpleConsole().Run();
        }



        private void Run()
        {
            Initialize();
            RunInteractiveLoop();
        }


        private void Initialize()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            Var.pushThreadBindings(
                RT.map(RT.CURRENT_NS, RT.CURRENT_NS.deref()));
            try
            {


                //LoadFromStream(new StringReader(clojure.lang.Properties.Resources.core),false);
                //RT.load("/core");
                //LoadFromStream(new StringReader(clojure.lang.Properties.Resources.core_print), false);
                //LoadFromStream(new StringReader(clojure.lang.Properties.Resources.test), false);

            }
            finally
            {
                Var.popThreadBindings();
            }

            sw.Stop();
            Console.WriteLine("Loading took {0} milliseconds.", sw.ElapsedMilliseconds);

        }

        public object LoadFromStream(PushbackTextReader rdr, bool addPrint)
        {
            object ret = null;
            object eofVal = new object();
            object form;
            while ((form = LispReader.read(rdr, false, eofVal, false)) != eofVal)
            {
                try
                {
                    LambdaExpression ast = Compiler.GenerateLambda(form, addPrint);
                    ret = ast.Compile().DynamicInvoke();
                }
                catch (Exception ex)
                {
                    if (addPrint)
                    {
                        Exception root = ex;
                        while (root.InnerException != null)
                            root = root.InnerException;

                        Console.WriteLine("Error evaluating {0}: {1}", form, root.Message);
                        Console.WriteLine(root.StackTrace);
                    }
                }
            }
            return ret;
        }


        private void RunInteractiveLoop()
        {
            LoadFromStream(new LineNumberingTextReader(Console.In), true);
        }


    }
}

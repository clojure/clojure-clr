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
            Var.pushThreadBindings(
                RT.map(RT.CURRENT_NS, RT.CURRENT_NS.deref()));
            try
            {

                Stopwatch sw = new Stopwatch();
                sw.Start();

                LoadFromStream(new StringReader(clojure.lang.Properties.Resources.core),false);
                LoadFromStream(new StringReader(clojure.lang.Properties.Resources.core_print), false);
                LoadFromStream(new StringReader(clojure.lang.Properties.Resources.test), false);

                sw.Stop();
                Console.WriteLine("Loading took {0} milliseconds.", sw.ElapsedMilliseconds);
            }
            finally
            {
                Var.popThreadBindings();
            }

        }

        public object LoadFromStream(TextReader rdr, bool addPrint)
        {
            object ret = null;
            object eofVal = new object();
            object form;
            while ((form = LispReader.read(rdr, false, eofVal, false)) != eofVal)
            {
                LambdaExpression ast = Compiler.GenerateLambda(form, addPrint);
                ret = ast.Compile().DynamicInvoke();
                //ret = CompilerHelpers.LightCompile(ast).DynamicInvoke();                
            }
            return ret;
        }


        private void RunInteractiveLoop()
        {
            LoadFromStream(System.Console.In, true);
        }


    }
}

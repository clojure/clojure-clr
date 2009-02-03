using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Scripting.Runtime;
using Microsoft.Linq.Expressions;
using Microsoft.Scripting;
using Microsoft.Scripting.Generation;
using clojure.lang;

namespace SimpleREPL
{
    class ClojureContext : LanguageContext
    {
        public ClojureContext(ScriptDomainManager manager, IDictionary<string, object> options)
            : base(manager)
        {
            Binder = new ClojureBinder(manager);
            manager.LoadAssembly(typeof(string).Assembly);
            //manager.LoadAssembly(typeof(System.Windows.Forms.Form).Assembly);
            manager.LoadAssembly(typeof(ISeq).Assembly);
            //manager.Configuration.DebugMode = true;
        }
         
        protected override Microsoft.Scripting.ScriptCode CompileSourceCode(Microsoft.Scripting.SourceUnit sourceUnit, Microsoft.Scripting.CompilerOptions options, Microsoft.Scripting.ErrorSink errorSink)
        {
            ClojureParser cp = new ClojureParser(sourceUnit);
            LambdaExpression ast;

            switch (sourceUnit.Kind)
            {
                case SourceCodeKind.InteractiveCode:
                    {
                        ScriptCodeParseResult result;
                        object code = cp.ParseInteractiveStatement(out result);
                        sourceUnit.CodeProperties = result;
                        if (result != ScriptCodeParseResult.Complete)
                            return null;
                        ast = ClojureGenerator.Generate(this, code,true);
                    }
                    break;

                default:
                    sourceUnit.CodeProperties = ScriptCodeParseResult.Complete;
                    ast = ClojureGenerator.Generate(this, cp.ParseFile(), sourceUnit);
                    break;
            }

            ast = new GlobalLookupRewriter().RewriteLambda(ast);
            return new ScriptCode(ast, sourceUnit);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace clojure.lang.CljCompiler.Ast
{
    public class ParseEvalExpr : Expr
    {
        Expr _expr;

        public ParseEvalExpr(Expr expr)
        {
            _expr = expr;
        }

        public bool HasClrType => throw new NotImplementedException();

        public Type ClrType => throw new NotImplementedException();

        public sealed class Parser : IParser
        {


            public Expr Parse(ParserContext pcon, object form)
            {
                // The whole point is just to evaluate the expression during parsing,
                // similar to  FnExpr doing ObjExpr.Compile during parsing in order to generate the function type.

                var expr = Compiler.Analyze(pcon, RT.second(form));
                expr.Eval();
                return new ParseEvalExpr(expr);
            }
        }

        public void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            Compiler.NilExprInstance.Emit(rhc, objx, ilg);
        }

        public object Eval()
        {
            return null;
        }

        public bool HasNormalExit()
        {
            return true;
        }
    }
}

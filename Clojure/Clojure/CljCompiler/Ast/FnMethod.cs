using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace clojure.lang.CljCompiler.Ast
{
    class FnMethod
    {

        #region Data

        // Java: when closures are defined inside other closures,
        // the closed over locals need to be propagated to the enclosing fn
        readonly FnMethod _parent;

        IPersistentMap _locals = null;       // localbinding => localbinding
        public IPersistentMap Locals
        {
            get { return _locals; }
            set { _locals = value; }
        }

        IPersistentMap _indexLocals = null;  // num -> localbinding
        public IPersistentMap IndexLocals
        {
            get { return _indexLocals; }
            set { _indexLocals = value; }
        }

        IPersistentVector _reqParms = null;  // localbinding => localbinding

        LocalBinding _restParm = null;

        Expr _body = null;

        FnExpr _fn;
        internal FnExpr Fn
        {
            get { return _fn; }
            set { _fn = value; }
        }

        IPersistentVector _argLocals;
        
        int _maxLocal = 0;
        public int MaxLocal
        {
            get { return _maxLocal; }
            set { _maxLocal = value; }
        }

        // int line;

        IPersistentSet _localsUsedInCatchFinally = PersistentHashSet.EMPTY;

        internal bool IsVariadic
        {
            get { return _restParm != null; }
        }


        internal int NumParams
        {
            get { return _reqParms.count() + (IsVariadic ? 1 : 0); }
        }

        internal int RequiredArity
        {
            get { return _reqParms.count(); }
        }
    
        #endregion

        #region C-tors

        public FnMethod(FnExpr fn, FnMethod parent)
        {
            _parent = parent;
            _fn = fn;
        }

        #endregion

        enum ParamParseState { Required, Rest, Done };

        internal static FnMethod Parse(FnExpr fn, ISeq form)
        {
            // ([args] body ... )

            IPersistentVector parms = (IPersistentVector)RT.first(form);
            ISeq body = RT.next(form);

            try
            {
                FnMethod method = new FnMethod(fn, (FnMethod)Compiler.METHODS.deref());
                // TODO: method.line = (Integer) LINE.deref();


                Var.pushThreadBindings(RT.map(
                    Compiler.METHODS, method,
                    Compiler.LOCAL_ENV, Compiler.LOCAL_ENV.deref(),
                    Compiler.LOOP_LOCALS, null,
                    Compiler.NEXT_LOCAL_NUM, 0));

                // register 'this' as local 0  
                Compiler.RegisterLocal(Symbol.intern(fn.ThisName ?? "fn__" + RT.nextID()), null, null);

                ParamParseState paramState = ParamParseState.Required;
                IPersistentVector argLocals = PersistentVector.EMPTY;
                int parmsCount = parms.count();

                for (int i = 0; i < parmsCount; i++)
                {
                    if (!(parms.nth(i) is Symbol))
                        throw new ArgumentException("fn params must be Symbols");
                    Symbol p = (Symbol)parms.nth(i);
                    if (p.Namespace != null)
                        throw new Exception("Can't use qualified name as parameter: " + p);
                    if (p.Equals(Compiler._AMP_))
                    {
                        if (paramState == ParamParseState.Required)
                            paramState = ParamParseState.Rest;
                        else
                            throw new Exception("Invalid parameter list");
                    }
                    else
                    {
                        LocalBinding b = Compiler.RegisterLocal(p,
                            paramState == ParamParseState.Rest ? Compiler.ISEQ : Compiler.TagOf(p),
                            null); // asdf-tag

                        argLocals = argLocals.cons(b);
                        switch (paramState)
                        {
                            case ParamParseState.Required:
                                method._reqParms = method._reqParms.cons(b);
                                break;
                            case ParamParseState.Rest:
                                method._restParm = b;
                                paramState = ParamParseState.Done;
                                break;
                            default:
                                throw new Exception("Unexpected parameter");
                        }
                    }
                }

                if (method.NumParams > Compiler.MAX_POSITIONAL_ARITY)
                    throw new Exception(string.Format("Can't specify more than {0} parameters", Compiler.MAX_POSITIONAL_ARITY));
                Compiler.LOOP_LOCALS.set(argLocals);
                method._argLocals = argLocals;
                method._body = (new BodyExpr.Parser()).Parse(body);
                return method;
            }
            finally
            {
                Var.popThreadBindings();
            }
        }


    }
}


namespace clojure.lang.CljCompiler.Ast
{
    internal class HostArg
    {
        #region Enum

        public enum ParameterType
        {
            Standard,
            ByRef
        }

        #endregion

        #region Data

        readonly ParameterType _paramType;

        public ParameterType ParamType
        {
            get { return _paramType; }
        }

        readonly Expr _argExpr;

        public Expr ArgExpr
        {
            get { return _argExpr; }
        }

        readonly LocalBinding _localBinding;

        public LocalBinding LocalBinding
        {
            get { return _localBinding; }
        }

        #endregion

        #region C-tors

        public HostArg(ParameterType paramType, Expr argExpr, LocalBinding lb)
        {
            _paramType = paramType;
            _argExpr = argExpr;
            _localBinding = lb;
        }

        #endregion
    }
}

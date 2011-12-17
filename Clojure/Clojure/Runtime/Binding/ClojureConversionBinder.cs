/**
 *   Copyright (c) Rich Hickey. All rights reserved.
 *   The use and distribution terms for this software are covered by the
 *   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
 *   which can be found in the file epl-v10.html at the root of this distribution.
 *   By using this software in any fashion, you are agreeing to be bound by
 * 	 the terms of this license.
 *   You must not remove this notice, or any other, from this software.
 **/

/**
 *   Author: David Miller
 **/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
#if CLR2
using Microsoft.Scripting.Ast;
#else
using System.Linq.Expressions;
#endif
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Actions;
using Microsoft.Scripting.Utils;

namespace clojure.lang.Runtime.Binding
{
    using Ast = Expression;
    using AstUtils = Microsoft.Scripting.Ast.Utils;
    using Microsoft.Scripting.Generation;
    using System.Collections;
    using System.Runtime.CompilerServices;


    // ripped off from IPy
    //sealed class ClojureConversionBinder : DynamicMetaObjectBinder, IExpressionSerializable //, IPythonSite  TODO: Do we need this?
    //{
    //    #region Instance variables

    //    private readonly ClojureContext _context;
    //    private readonly ConversionResultKind _kind;
    //    private readonly Type _type;
    //    private readonly bool _retObject;
    //    private CompatConversionBinder _compatConvert;

    //    #endregion

    //    #region Properties

    //    public ClojureContext       Context    { get { return _context; } }
    //    public Type                 Type       { get { return _type;    } }
    //    public ConversionResultKind ResultKind { get { return _kind;    } }

    //    internal CompatConversionBinder CompatBinder
    //    {
    //        get
    //        {
    //            if (_compatConvert == null)
    //            {
    //                _compatConvert = new CompatConversionBinder(this, Type, _kind == ConversionResultKind.ExplicitCast || _kind == ConversionResultKind.ExplicitTry);
    //            }
    //            return _compatConvert;
    //        }
    //    }

    //    #endregion

    //    #region C-tors

    //    public ClojureConversionBinder(ClojureContext context, Type type, ConversionResultKind resultKind)
    //    {
    //        Assert.NotNull(context, type);

    //        _context = context;
    //        _kind = resultKind;
    //        _type = type;
    //    }

    //    public ClojureConversionBinder(ClojureContext context, Type type, ConversionResultKind resultKind, bool retObject)
    //      {
    //        Assert.NotNull(context, type);

    //        _context = context;
    //        _kind = resultKind;
    //        _type = type;
    //        _retObject = retObject;
    //    }

    //    #endregion

    //    #region Object overrides

    //    public override string ToString()
    //    {
    //        return String.Format("Clojure Convert {0} {1}", Type, ResultKind);
    //    }

    //    public override int GetHashCode()
    //    {
    //        return base.GetHashCode() ^ _context.Binder.GetHashCode() ^ _kind.GetHashCode();
    //    }

    //    public override bool Equals(object obj)
    //    {
    //        ClojureConversionBinder ob = obj as ClojureConversionBinder;
    //        if (ob == null)
    //        {
    //            return false;
    //        }

    //        return ob._context.Binder == _context.Binder &&
    //            _kind == ob._kind && base.Equals(obj) &&
    //            _retObject == ob._retObject;
    //    }

    //    #endregion

    //    #region Binding

    //    public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
    //    {
    //        DynamicMetaObject self = target;

    //        DynamicMetaObject res = null;
    //        if (self.NeedsDeferral())
    //        {
    //            return MyDefer(self);
    //        }


    //        IClojureConvertible convertible = target as IClojureConvertible;
    //        if (convertible != null)
    //        {
    //            res = convertible.BindConvert(this);
    //        }

    //        if (res == null)
    //        {
    //            res = TryDefaultNumericConversion(self);
    //        }

    //        if (res == null)
    //        {
    //            res = BindConvert(self);
    //        }

    //        if (_retObject)
    //        {
    //            res = new DynamicMetaObject(
    //                AstUtils.Convert(res.Expression, typeof(object)),
    //                res.Restrictions
    //            );
    //        }

    //        return res;
    //    }

    //    private DynamicMetaObject TryDefaultNumericConversion(DynamicMetaObject self)
    //    {
    //        switch (Type.GetTypeCode(Type))
    //        {
    //            case TypeCode.Byte:
    //            case TypeCode.Int16:
    //            case TypeCode.Int32:
    //            case TypeCode.Int64:
    //            case TypeCode.SByte:
    //            case TypeCode.UInt16:
    //            case TypeCode.UInt32:
    //            case TypeCode.UInt64:

    //                if ( self.LimitType == typeof(Int64) )
    //                    return new DynamicMetaObject(
    //                        Ast.Convert(self.Expression,Type),
    //                        self.Restrictions.Merge(BindingRestrictions.GetTypeRestriction(self.Expression,typeof(Int64))));

    //                break;

    //            case TypeCode.Single:
    //                if (self.LimitType == typeof(Double))
    //                    return new DynamicMetaObject(
    //                        Ast.Convert(self.Expression, Type),
    //                        self.Restrictions.Merge(BindingRestrictions.GetTypeRestriction(self.Expression, typeof(Double))));
    //                break;
    //        }
    //        return null;
    //    }

    //    // ripped off from IPy
    //    private DynamicMetaObject MyDefer(DynamicMetaObject self)
    //    {
    //        return new DynamicMetaObject(
    //            Expression.Dynamic(
    //                this,
    //                ReturnType,
    //                self.Expression
    //            ),
    //            self.Restrictions
    //        );
    //    }


    //    // ripped off from IPy
    //    private DynamicMetaObject BindConvert(DynamicMetaObject self)
    //    {
    //        //PerfTrack.NoteEvent(PerfTrack.Categories.Binding, "Convert " + Type.FullName + " " + self.LimitType);
    //        //PerfTrack.NoteEvent(PerfTrack.Categories.BindingTarget, "Conversion");

    //        DynamicMetaObject res;
    //        DynamicMetaObject comConvert;
    //        if (Microsoft.Scripting.ComInterop.ComBinder.TryConvert(CompatBinder, self, out comConvert))
    //        {
    //            res = comConvert;
    //        }
    //        else
    //        {
    //            res = self.BindConvert(CompatBinder);
    //        }

    //        // if we return object and the interop binder had to put on an extra conversion
    //        // to the strong type go ahead and remove it now.
    //        if (ReturnType == typeof(object) &&
    //            res.Expression.Type != typeof(object) &&
    //            res.Expression.NodeType == ExpressionType.Convert)
    //        {
    //            res = new DynamicMetaObject(
    //                ((UnaryExpression)res.Expression).Operand,
    //                res.Restrictions
    //            );
    //        }

    //        return res;
    //    }

    //    // ripped off from IPy
    //    internal DynamicMetaObject FallbackConvert(Type returnType, DynamicMetaObject self, DynamicMetaObject errorSuggestion)
    //    {
    //        Type type = Type;
    //        DynamicMetaObject res = null;
    //        switch (Type.GetTypeCode(type))
    //        {
    //            case TypeCode.Boolean:
    //                res = MakeToBoolConversion(self);
    //                break;
    //            case TypeCode.Char:
    //                res = TryToCharConversion(self);
    //                break;
    //            case TypeCode.String:
    //                // IPy: Bytes conversion
    //                break;
    //            case TypeCode.Object:
    //                // !!! Deferral?
    //                // IPy: type.IsArray && PythonTuple conversion -- do we have anything equivalent?
                       
    //                if (type.IsGenericType && !type.IsAssignableFrom(CompilerHelpers.GetType(self.Value)))
    //                {
    //                    Type genTo = type.GetGenericTypeDefinition();

    //                    // Interface conversion helpers...
    //                    //if (genTo == typeof(IList<>))
    //                    //{
    //                    //    if (self.LimitType == typeof(string))
    //                    //    {
    //                    //        res = new DynamicMetaObject(
    //                    //            Ast.Call(
    //                    //                typeof(ClojureOps).GetMethod("MakeByteArray"),
    //                    //                AstUtils.Convert(self.Expression, typeof(string))
    //                    //            ),
    //                    //            BindingRestrictions.GetTypeRestriction(
    //                    //                self.Expression,
    //                    //                typeof(string)
    //                    //            )
    //                    //        );
    //                    //    }
    //                    //    else
    //                    //    {
    //                    //        res = TryToGenericInterfaceConversion(self, type, typeof(IList<object>), typeof(ListGenericWrapper<>));
    //                    //    }
    //                    //}
    //                    //else if (genTo == typeof(IDictionary<,>))
    //                    //{
    //                    //    res = TryToGenericInterfaceConversion(self, type, typeof(IDictionary<object, object>), typeof(DictionaryGenericWrapper<,>));
    //                    //}
    //                    //else if (genTo == typeof(IEnumerable<>))
    //                    //{
    //                    //    res = TryToGenericInterfaceConversion(self, type, typeof(IEnumerable), typeof(IEnumerableOfTWrapper<>));
    //                    //}
    //                }
    //                else if (type == typeof(IEnumerable))
    //                {
    //                    if (!typeof(IEnumerable).IsAssignableFrom(self.GetLimitType()))
    //                    {
    //                        res = ConvertToIEnumerable(this, self.Restrict(self.GetLimitType()));
    //                    }
    //                }
    //                else if (type == typeof(IEnumerator))
    //                {
    //                    if (!typeof(IEnumerator).IsAssignableFrom(self.GetLimitType()) &&
    //                        !typeof(IEnumerable).IsAssignableFrom(self.GetLimitType()) )
    //                    {
    //                        res = ConvertToIEnumerator(this, self.Restrict(self.GetLimitType()));
    //                    }
    //                }
    //                break;
    //        }

    //        if (type.IsEnum && Enum.GetUnderlyingType(type) == self.GetLimitType())
    //        {
    //            // numeric type to enum, this is ok if the value is zero
    //            object value = Activator.CreateInstance(type);

    //            return new DynamicMetaObject(
    //                Ast.Condition(
    //                    Ast.Equal(
    //                        AstUtils.Convert(self.Expression, Enum.GetUnderlyingType(type)),
    //                        AstUtils.Constant(Activator.CreateInstance(self.GetLimitType()))
    //                    ),
    //                    AstUtils.Constant(value),
    //                    Ast.Call(
    //                        typeof(ClojureOps).GetMethod("TypeErrorForBadEnumConversion").MakeGenericMethod(type),
    //                        AstUtils.Convert(self.Expression, typeof(object))
    //                    )
    //                ),
    //                self.Restrictions.Merge(BindingRestrictionsHelpers.GetRuntimeTypeRestriction(self.Expression, self.GetLimitType())),
    //                value
    //            );
    //        }

    //        return res ?? EnsureReturnType(returnType, Context.Binder.ConvertTo(Type, ResultKind, self, _context.SharedOverloadResolverFactory, errorSuggestion));
    //    }


    //    // ripped off from IPy
    //    private static DynamicMetaObject EnsureReturnType(Type returnType, DynamicMetaObject dynamicMetaObject)
    //    {
    //        if (dynamicMetaObject.Expression.Type != returnType)
    //        {
    //            dynamicMetaObject = new DynamicMetaObject(
    //                AstUtils.Convert(
    //                    dynamicMetaObject.Expression,
    //                    returnType
    //                ),
    //                dynamicMetaObject.Restrictions
    //            );
    //        }

    //        return dynamicMetaObject;
    //    }


    //    // ripped off from IPy
    //    private DynamicMetaObject MakeToBoolConversion(DynamicMetaObject self)
    //    {
    //        DynamicMetaObject res;
    //        if (self.HasValue)
    //        {
    //            self = self.Restrict(self.GetRuntimeType());
    //        }

    //        // Optimization: if we already boxed it to a bool, and now
    //        // we're unboxing it, remove the unnecessary box.
    //        if (self.Expression.NodeType == ExpressionType.Convert && self.Expression.Type == typeof(object))
    //        {
    //            var convert = (UnaryExpression)self.Expression;
    //            if (convert.Operand.Type == typeof(bool))
    //            {
    //                return new DynamicMetaObject(convert.Operand, self.Restrictions);
    //            }
    //        }

    //        if (self.GetLimitType() == typeof(DynamicNull))
    //        {
    //            // None has no __nonzero__ and no __len__ but it's always false
    //            res = MakeNoneToBoolConversion(self);
    //        }
    //        else if (self.GetLimitType() == typeof(bool))
    //        {
    //            // nothing special to convert from bool to bool
    //            res = self;
    //        }
    //        else if (typeof(IStrongBox).IsAssignableFrom(self.GetLimitType()))
    //        {
    //            // Explictly block conversion of References to bool
    //            res = MakeStrongBoxToBoolConversionError(self);
    //        }
    //        else if (self.GetLimitType().IsPrimitive || self.GetLimitType().IsEnum)
    //        {
    //            // optimization - rather than doing a method call for primitives and enums generate
    //            // the comparison to zero directly.
    //            res = MakePrimitiveToBoolComparison(self);
    //        }
    //        else
    //        {
    //            // anything non-null that doesn't fall under one of the above rules is true.  So we
    //            // fallback to the base Python conversion which will check for __nonzero__ and
    //            // __len__.  The fallback is handled by our ConvertTo site binder.
    //            return
    //                //PythonProtocol.ConvertToBool(this, self) ??
    //                new DynamicMetaObject(
    //                    AstUtils.Constant(true),
    //                    self.Restrictions
    //                );
    //        }

    //        return res;
    //    }

    //    // ripped off from IPy
    //    private static DynamicMetaObject MakeNoneToBoolConversion(DynamicMetaObject self)
    //    {
    //        // null is never true
    //        return new DynamicMetaObject(
    //            AstUtils.Constant(false),
    //            self.Restrictions
    //        );
    //    }

    //    // ripped off from IPy
    //    private static DynamicMetaObject MakePrimitiveToBoolComparison(DynamicMetaObject self)
    //    {
    //        // TODO: WHy not use Default(T) here?
    //        object zeroVal = Activator.CreateInstance(self.GetLimitType());

    //        return new DynamicMetaObject(
    //            Ast.NotEqual(
    //                AstUtils.Constant(zeroVal),
    //                self.Expression
    //            ),
    //            self.Restrictions
    //        );
    //    }

    //    // ripped off from IPy
    //    private DynamicMetaObject MakeStrongBoxToBoolConversionError(DynamicMetaObject self)
    //    {
    //        return new DynamicMetaObject(
    //            Ast.Throw(
    //                Ast.Call(
    //                    typeof(ScriptingRuntimeHelpers).GetMethod("SimpleTypeError"),
    //                    AstUtils.Constant("Can't convert a Reference<> instance to a bool")
    //                ),
    //                ReturnType
    //            ),
    //            self.Restrictions
    //        );
    //    }


    //    private DynamicMetaObject TryToCharConversion(DynamicMetaObject/*!*/ self)
    //    {
    //        DynamicMetaObject res;
    //        // we have an implicit conversion to char if the
    //        // string length == 1, but we can only represent
    //        // this is implicit via a rule.
    //        string strVal = self.Value as string;
    //        Expression strExpr = self.Expression;
    //        if (strVal == null)
    //        {
    //            Extensible<string> extstr = self.Value as Extensible<string>;
    //            if (extstr != null)
    //            {
    //                strVal = extstr.Value;
    //                strExpr =
    //                    Ast.Property(
    //                        AstUtils.Convert(
    //                            strExpr,
    //                            typeof(Extensible<string>)
    //                        ),
    //                        typeof(Extensible<string>).GetProperty("Value")
    //                    );
    //            }
    //        }

    //        // we can only produce a conversion if we have a string value...
    //        if (strVal != null)
    //        {
    //            self = self.Restrict(self.GetRuntimeType());

    //            Expression getLen = Ast.Property(
    //                AstUtils.Convert(
    //                    strExpr,
    //                    typeof(string)
    //                ),
    //                typeof(string).GetProperty("Length")
    //            );

    //            if (strVal.Length == 1)
    //            {
    //                res = new DynamicMetaObject(
    //                    Ast.Call(
    //                        AstUtils.Convert(strExpr, typeof(string)),
    //                        typeof(string).GetMethod("get_Chars"),
    //                        AstUtils.Constant(0)
    //                    ),
    //                    self.Restrictions.Merge(BindingRestrictions.GetExpressionRestriction(Ast.Equal(getLen, AstUtils.Constant(1))))
    //                );
    //            }
    //            else
    //            {
    //                res = new DynamicMetaObject(
    //                    Ast.Throw(
    //                        Ast.Call(
    //                            typeof(ClojureOps).GetMethod("TypeError"),
    //                            AstUtils.Constant("expected string of length 1 when converting to char, got '{0}'"),
    //                            Ast.NewArrayInit(typeof(object), self.Expression)
    //                        ),
    //                        ReturnType
    //                    ),
    //                    self.Restrictions.Merge(BindingRestrictions.GetExpressionRestriction(Ast.NotEqual(getLen, AstUtils.Constant(1))))
    //                );
    //            }
    //        }
    //        else
    //        {
    //            // let the base class produce the rule
    //            res = null;
    //        }

    //        return res;
    //    }

    //    // ripped off from IPy
    //    private static DynamicMetaObject TryToGenericInterfaceConversion(DynamicMetaObject self, Type toType, Type fromType, Type wrapperType)
    //    {
    //        if (fromType.IsAssignableFrom(CompilerHelpers.GetType(self.Value)))
    //        {
    //            Type making = wrapperType.MakeGenericType(toType.GetGenericArguments());

    //            self = self.Restrict(CompilerHelpers.GetType(self.Value));

    //            return new DynamicMetaObject(
    //                Ast.New(
    //                    making.GetConstructor(new Type[] { fromType }),
    //                    AstUtils.Convert(
    //                        self.Expression,
    //                        fromType
    //                    )
    //                ),
    //                self.Restrictions
    //            );
    //        }
    //        return null;
    //    }

    //    // ripped off from IPy
    //    // TODO: Need to think about this
    //    internal static DynamicMetaObject ConvertToIEnumerable(DynamicMetaObjectBinder/*!*/ conversion, DynamicMetaObject/*!*/ metaUserObject)
    //    {
    //        //PythonType pt = MetaPythonObject.GetPythonType(metaUserObject);
    //        //PythonContext pyContext = PythonContext.GetPythonContext(conversion);
    //        //CodeContext context = pyContext.SharedContext;
    //        //PythonTypeSlot pts;

    //        //if (pt.TryResolveSlot(context, "__iter__", out pts))
    //        //{
    //        //    return MakeIterRule(metaUserObject, "CreatePythonEnumerable");
    //        //}
    //        //else if (pt.TryResolveSlot(context, "__getitem__", out pts))
    //        //{
    //        //    return MakeGetItemIterable(metaUserObject, pyContext, pts, "CreateItemEnumerable");
    //        //}

    //        return null;
    //    }

    //    // ripped off from IPy
    //    // TODO: Need to think about this
    //    internal static DynamicMetaObject ConvertToIEnumerator(DynamicMetaObjectBinder/*!*/ conversion, DynamicMetaObject/*!*/ metaUserObject)
    //    {
    //        //PythonType pt = MetaPythonObject.GetPythonType(metaUserObject);
    //        //PythonContext state = PythonContext.GetPythonContext(conversion);
    //        //CodeContext context = state.SharedContext;
    //        //PythonTypeSlot pts;


    //        //if (pt.TryResolveSlot(context, "__iter__", out pts))
    //        //{
    //        //    ParameterExpression tmp = Ast.Parameter(typeof(object), "iterVal");

    //        //    return new DynamicMetaObject(
    //        //        Expression.Block(
    //        //            new[] { tmp },
    //        //            Expression.Call(
    //        //                typeof(PythonOps).GetMethod("CreatePythonEnumerator"),
    //        //                Ast.Block(
    //        //                    MetaPythonObject.MakeTryGetTypeMember(
    //        //                        state,
    //        //                        pts,
    //        //                        metaUserObject.Expression,
    //        //                        tmp
    //        //                    ),
    //        //                    Ast.Dynamic(
    //        //                        new PythonInvokeBinder(
    //        //                            state,
    //        //                            new CallSignature(0)
    //        //                        ),
    //        //                        typeof(object),
    //        //                        AstUtils.Constant(context),
    //        //                        tmp
    //        //                    )
    //        //                )
    //        //            )
    //        //        ),
    //        //        metaUserObject.Restrictions
    //        //    );
    //        //}
    //        //else if (pt.TryResolveSlot(context, "__getitem__", out pts))
    //        //{
    //        //    return MakeGetItemIterable(metaUserObject, state, pts, "CreateItemEnumerator");
    //        //}

    //        return null;
    //    }

    //    #endregion

    //    #region IExpressionSerializable Members

    //    public Expression CreateExpression()
    //    {
    //        return Expression.Call(
    //            typeof(ClojureConversionBinder).GetMethod("CreateMe"),
    //            Expression.Constant(ClojureContext.Default),
    //            Expression.Constant(Type),
    //            Expression.Constant(ResultKind),
    //            Expression.Constant(_retObject));
    //    }

    //    public static ClojureConversionBinder CreateMe(ClojureContext context, Type type, ConversionResultKind resultKind, bool retObject)
    //    {
    //        return new ClojureConversionBinder(context, type, resultKind, retObject);
    //    }

    //    #endregion    
    //}


    //// ripped off from IPy
    //// TODO: Do we need this?
    //class CompatConversionBinder : ConvertBinder
    //{
    //    private readonly ClojureConversionBinder _binder;

    //    public CompatConversionBinder(ClojureConversionBinder binder, Type toType, bool isExplicit)
    //        : base(toType, isExplicit)
    //    {
    //        _binder = binder;
    //    }

    //    public override DynamicMetaObject FallbackConvert(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
    //    {
    //        return _binder.FallbackConvert(ReturnType, target, errorSuggestion);
    //    }
    //}
}

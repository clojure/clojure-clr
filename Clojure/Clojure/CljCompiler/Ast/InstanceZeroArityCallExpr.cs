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
#if CLR2
using Microsoft.Scripting.Ast;
using Microsoft.Scripting.Actions;
using System.Dynamic;
using Microsoft.Scripting.Runtime;
using System.Reflection;
#else
using System.Linq.Expressions;
#endif
using AstUtils = Microsoft.Scripting.Ast.Utils;
using System.Reflection.Emit;
using Microsoft.Scripting.Generation;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Actions.Calls;
using Microsoft.Scripting.Utils;
using System.Threading;


namespace clojure.lang.CljCompiler.Ast
{
    class InstanceZeroArityCallExpr : HostExpr
    {
        #region Data

        readonly Expr _target;
        readonly Type _targetType; 
        readonly string _memberName;
        protected readonly string _source;
        protected readonly IPersistentMap _spanMap;
        protected readonly Symbol _tag;

        #endregion

        #region Ctors

        internal InstanceZeroArityCallExpr(string source, IPersistentMap spanMap, Symbol tag, Expr target, string memberName)
        {
            _source = source;
            _spanMap = spanMap;
            _memberName = memberName;
            _target = target;
            _tag = tag;

            _targetType = target.HasClrType ? target.ClrType : null;
        }

        #endregion

        #region Type mangling

        public override bool HasClrType
        {
            get { return _tag != null; }
        }

        public override Type ClrType
        {
            get { return Compiler.TagToType(_tag); }
        }

        #endregion

        #region Code generation

        public override Expression GenDlr(GenContext context)
        {
            return Compiler.MaybeBox(GenDlrUnboxed(context));
        }

        public override Expression GenDlrUnboxed(GenContext context)
        {
            Expression target = _target.GenDlr(context);

            Type returnType = HasClrType ? ClrType : typeof(object);

            GetMemberBinder binder = new DefaultGetZeroArityMemberBinder(_memberName, false);
            DynamicExpression dyn = Expression.Dynamic(binder, returnType, new Expression[] { target });

            Expression call = dyn;

            if ( context.Mode == CompilerMode.File )
                call = context.DynInitHelper.ReduceDyn(dyn);

            call = Compiler.MaybeAddDebugInfo(call, _spanMap);
            return call;
        }


        #endregion
    }

    public class DefaultCreateInstanceBinder : CreateInstanceBinder, IExpressionSerializable
    {
        #region Data

        readonly DefaultBinder _binder = new DefaultBinder();

        #endregion

        #region C-tors

        public DefaultCreateInstanceBinder(int argCount)
            : base(new CallInfo(argCount, DynUtils.GetArgNames(argCount)))
        {
        }

        #endregion

        #region CreateInstanceBinder methods

     

        public override DynamicMetaObject FallbackCreateInstance(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
        {
            OverloadResolverFactory factory = DefaultOverloadResolver.Factory;
            Type typeToUse = target.Value is Type ? (Type)target.Value : target.LimitType;


            IList<DynamicMetaObject> argsPlus = new List<DynamicMetaObject>(args.Length);

            foreach (DynamicMetaObject arg in args)
                argsPlus.Add(arg);

            DefaultOverloadResolver res = factory.CreateOverloadResolver(argsPlus, new CallSignature(args.Length),  CallTypes.None);

            BindingFlags flags =  BindingFlags.Public | BindingFlags.Instance;
            IList<MethodBase> methods = new List<MethodBase>(typeToUse.GetConstructors(flags).Where<MethodBase>(x =>  x.GetParameters().Length == args.Length));

            if (methods.Count > 0)
            {
                DynamicMetaObject dmo = _binder.CallMethod(res, methods);
                dmo = DynUtils.MaybeBoxReturnValue(dmo);

                //; Console.WriteLine(dmo.Expression.DebugView);
                return dmo;
            }

            return errorSuggestion ??
                new DynamicMetaObject(
                    Expression.Throw(
                        Expression.New(typeof(MissingMethodException).GetConstructor(new Type[] { typeof(string) }),
                            Expression.Constant("Cannot find constructor matching args")),
                        typeof(object)),
                    target.Restrictions.Merge(BindingRestrictions.Combine(args)));
        }

        #endregion

        #region IExpressionSerializable Members

        public Expression CreateExpression()
        {
            return Expression.Call(typeof(DefaultCreateInstanceBinder).GetMethod("CreateMe"),
                Expression.Constant(this.CallInfo.ArgumentCount));
        }

        public static DefaultCreateInstanceBinder CreateMe(int argCount)
        {
            return new DefaultCreateInstanceBinder(argCount);
        }

        #endregion
    }


    public class DefaultInvokeMemberBinder : InvokeMemberBinder, IExpressionSerializable
    {
        #region Data

        readonly DefaultBinder _binder = new DefaultBinder();
        readonly bool _isStatic;

        #endregion

        #region C-tors

        public DefaultInvokeMemberBinder(string name, int argCount, bool isStatic)
            : base(name, false, new CallInfo(argCount, DynUtils.GetArgNames(argCount)))
        {
            _isStatic = isStatic;
        }

        #endregion

         #region InvokeMemberBinder methods

        public override DynamicMetaObject FallbackInvokeMember(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
        {
            OverloadResolverFactory factory = DefaultOverloadResolver.Factory;
            Type typeToUse = _isStatic && target.Value is Type ? (Type)target.Value : target.LimitType;


            IList<DynamicMetaObject> argsPlus = new List<DynamicMetaObject>(args.Length + (_isStatic ? 0 : 1));
            if ( ! _isStatic )
                argsPlus.Add(target);
            foreach (DynamicMetaObject arg in args)
                argsPlus.Add(arg);

            DefaultOverloadResolver res = factory.CreateOverloadResolver(argsPlus, new CallSignature(args.Length), _isStatic ? CallTypes.None : CallTypes.ImplicitInstance);

            BindingFlags flags = BindingFlags.InvokeMethod | BindingFlags.Public | (_isStatic ? BindingFlags.Static : BindingFlags.Instance);
            IList<MethodBase> methods = new List<MethodBase>(typeToUse.GetMethods(flags).Where<MethodBase>(x => x.Name == Name && x.GetParameters().Length == args.Length));

            if (methods.Count > 0)
            {

                DynamicMetaObject dmo = _binder.CallMethod(res, methods);
                dmo = DynUtils.MaybeBoxReturnValue(dmo);

                //; Console.WriteLine(dmo.Expression.DebugView);
                return dmo;
            }

            return errorSuggestion ??
                new DynamicMetaObject(
                    Expression.Throw(
                        Expression.New(typeof(MissingMethodException).GetConstructor(new Type[] { typeof(string) }),
                            Expression.Constant(String.Format("Cannot find member {0} matching args", this.Name))),
                        typeof(object)),
                    target.Restrictions.Merge(BindingRestrictions.Combine(args)));
        }

        public override DynamicMetaObject FallbackInvoke(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IExpressionSerializable Members

        public Expression CreateExpression()
        {
            return Expression.Call(typeof(DefaultInvokeMemberBinder).GetMethod("CreateMe"),
                Expression.Constant(this.Name),
                Expression.Constant(this.CallInfo.ArgumentCount),
                Expression.Constant(this._isStatic));
        }

        public static DefaultInvokeMemberBinder CreateMe(string name, int argCount, bool isStatic)
        {
            return new DefaultInvokeMemberBinder(name, argCount,isStatic);
        }

        #endregion
    }

    public class DefaultGetZeroArityMemberBinder : GetMemberBinder, IExpressionSerializable
    {
        #region Data

        readonly DefaultBinder _binder = new DefaultBinder();
        readonly bool _isStatic;

        #endregion

        #region C-tor

        public DefaultGetZeroArityMemberBinder(string name, bool isStatic)
            : base(name, true)
        {
            _isStatic = isStatic;
        }

        #endregion

        #region GetMemberBinder Members

        public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
        {
            Expression instanceExpr = _isStatic ? null : Expression.Convert(target.Expression, target.LimitType);
            Type typeToUse = _isStatic && target.Value is Type ? (Type)target.Value : target.LimitType;

            BindingRestrictions restrictions = target.Restrictions.Merge(BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType));
            BindingFlags flags = BindingFlags.Public;
            if (_isStatic)
                flags |= BindingFlags.Static;
            else
                flags |= BindingFlags.Instance;


            FieldInfo finfo = typeToUse.GetField(Name, flags);
            if (finfo != null)
                return DynUtils.MaybeBoxReturnValue(new DynamicMetaObject(Expression.Field(instanceExpr, finfo), restrictions));

            PropertyInfo pinfo = typeToUse.GetProperty(Name, flags);
            if (pinfo != null)
                return DynUtils.MaybeBoxReturnValue(new DynamicMetaObject(Expression.Property(instanceExpr, pinfo), restrictions));

            MethodInfo minfo = typeToUse.GetMethod(Name, flags, Type.DefaultBinder, Type.EmptyTypes, new ParameterModifier[0]);
            if (minfo != null)
                return DynUtils.MaybeBoxReturnValue(new DynamicMetaObject(Expression.Call(instanceExpr, minfo), restrictions));

            return errorSuggestion ??
                new DynamicMetaObject(
                    Expression.Throw(
                        Expression.New(typeof(MissingMethodException).GetConstructor(new Type[] { typeof(string) }),
                            Expression.Constant(String.Format("Cannot find {0} field/proprerty/member name {1}", _isStatic ? "static" : "instance", this.Name))),
                        typeof(object)),
                    target.Restrictions);

        }

        #endregion

        #region IExpressionSerializable Members

        public Expression CreateExpression()
        {
            return Expression.Call(typeof(DefaultGetZeroArityMemberBinder).GetMethod("CreateMe"),
                Expression.Constant(this.Name),
                Expression.Constant(this._isStatic));
        }

        public static DefaultGetZeroArityMemberBinder CreateMe(string name, bool isStatic)
        {
            return new DefaultGetZeroArityMemberBinder(name, isStatic);
        }

        #endregion
    }

    class DynUtils
    {
        #region Misc

        public static T[] ArrayInsert<T>(T item, IList<T> list)
        {
            T[] res = new T[list.Count + 1];
            res[0] = item;
            list.CopyTo(res, 1);
            return res;
        }

        #endregion

        #region Argument names

        static Dictionary<int, string[]> _argNamesCache = new Dictionary<int, string[]>();

        public static string[] GetArgNames(int argCount)
        {
            string[] names;
            if (!_argNamesCache.TryGetValue(argCount, out names))
            {
                names = CreateArgNames(argCount);
                _argNamesCache[argCount] = names;
            }
            return names;
        }

        private static string[] CreateArgNames(int argCount)
        {
            string[] names = new string[argCount];
            for (int i = 0; i < argCount; i++)
                names[i] = "arg" + i.ToString();
            return names;
        }

        #endregion

        #region  Boxing support

        public static DynamicMetaObject MaybeBoxReturnValue(DynamicMetaObject res)
        {
            if (res.Expression.Type.IsValueType)
            {
                res = AddBoxing(res);
            }
            else if (res.Expression.Type == typeof(void))
            {
                res = new DynamicMetaObject(
                    Expression.Block(
                        res.Expression,
                        Expression.Constant(null)
                    ),
                    res.Restrictions
                );
            }

            return res;
        }


        public static DynamicMetaObject AddBoxing(DynamicMetaObject res)
        {
            if (res.Expression.Type.IsValueType)
            {
                res = new DynamicMetaObject(
                    AddBoxing(res.Expression),
                    res.Restrictions
                );
            }
            return res;
        }

        public static Expression AddBoxing(Expression res)
        {
            return AstUtils.Convert(res, typeof(object));
        }


        #endregion

        #region  Creating invoke member dynamic expressions

        //public static Expression CreateInvokeMemberExpression(DynInitHelper dih, ParameterExpression thisParm, string name, Type returnType, params Expression[] argExprs)
        //{
        //    int argCount = argExprs.Length;
        //    InvokeMemberBinder imb = new DefaultInvokeMemberBinder(name, argCount);
        //    Expression[] exprs = ArrayInsert<Expression>(thisParm, argExprs);
        //    DynamicExpression dyn = Expression.Dynamic(imb, returnType, exprs);
        //    Expression dynReplace = dih.ReduceDyn(dyn);
        //    return dynReplace;
        //}

        //public static LambdaExpression CreateInvokeMemberLambda(DynInitHelper dih, ParameterExpression[] parms,
        //    string name, Type returnType, params Expression[] argExprs)
        //{
        //    ParameterExpression thisParm = Expression.Parameter(typeof(object), "this");
        //    Expression dynReplace = CreateInvokeMemberExpression(dih, thisParm, name, returnType, argExprs);
        //    ParameterExpression[] allParms = ArrayInsert<ParameterExpression>(thisParm, parms);
        //    LambdaExpression lambda = Expression.Lambda(dynReplace, allParms);
        //    return lambda;
        //}

        #endregion

        #region

        //public static Expression CreateGetZeroArityMemberExpression(DynInitHelper dih, ParameterExpression thisParm, Type returnType, string name, bool isStatic)
        //{
        //    GetMemberBinder imb = new DefaultGetZeroArityMemberBinder(name, isStatic);
        //    DynamicExpression dyn = Expression.Dynamic(imb, returnType, new Expression[] { thisParm });
        //    Expression dynReplace = dih.ReduceDyn(dyn);
        //    return dynReplace;
        //}

        //public static LambdaExpression CreateGetZeroArityMemberLambda(DynInitHelper dih, ParameterExpression[] parms,
        //    string name, bool isStatic, Type returnType)
        //{
        //    ParameterExpression thisParm = Expression.Parameter(typeof(object), "this");
        //    Expression dynReplace = CreateGetZeroArityMemberExpression(dih, thisParm, returnType, name, isStatic);
        //    ParameterExpression[] allParms = ArrayInsert<ParameterExpression>(thisParm, parms);
        //    LambdaExpression lambda = Expression.Lambda(dynReplace, allParms);
        //    return lambda;
        //}

        #endregion
    }

    static class StolenFromDlrCollectionExtensions
    {
        // From Microsoft.Scripting.Utils.CollectionExtensions
        // Name needs to be different so it doesn't conflict with Enumerable.Select
        internal static U[] Map<T, U>(this ICollection<T> collection, Microsoft.Scripting.Utils.Func<T, U> select)
        {
            int count = collection.Count;
            U[] result = new U[count];
            count = 0;
            foreach (T t in collection)
            {
                result[count++] = select(t);
            }
            return result;
        }

    }

    class DynInitHelper
    {
        #region Data

        int _id = 0;
        AssemblyGen _assemblyGen;
        TypeBuilder _typeBuilder;
        TypeGen _typeGen;

        List<FieldBuilder> _fieldBuilders = new List<FieldBuilder>();
        List<Expression> _fieldInits = new List<Expression>();

        Dictionary<Type, Type> _delegateTypes;

        #endregion

        #region Ctors and factories

        public DynInitHelper(AssemblyGen ag, string typeName)
        {
            _assemblyGen = ag;
            _typeBuilder = ag.DefinePublicType(typeName, typeof(object), true);
            _typeGen = new TypeGen(ag, _typeBuilder);
        }

        #endregion

        #region Dynamic expression rewriting

        /// <summary>
        /// Reduces the provided DynamicExpression into site.Target(site, *args).
        /// </summary>
        public Expression ReduceDyn(DynamicExpression node)
        {
            Type delegateType;
            if (RewriteDelegate(node.DelegateType, out delegateType))
            {
                node = Expression.MakeDynamic(delegateType, node.Binder, node.Arguments);
            }

            CallSite cs = CallSite.Create(node.DelegateType, node.Binder);
            Expression access = RewriteCallSite(cs, _typeGen);


            // ($site = siteExpr).Target.Invoke($site, *args)
            ParameterExpression site = Expression.Variable(cs.GetType(), "$site");

            return Expression.Block(
                new[] { site },
                Expression.Call(
                    Expression.Field(
                        Expression.Assign(site, access),
                        cs.GetType().GetField("Target")
                    ),
                    node.DelegateType.GetMethod("Invoke"),
                    DynUtils.ArrayInsert(site, node.Arguments)
                )
            );
        }


        private Expression RewriteCallSite(CallSite site, TypeGen tg)
        {
            IExpressionSerializable serializer = site.Binder as IExpressionSerializable;
            if (serializer == null)
            {
                throw new ArgumentException("Generating code from non-serializable CallSiteBinder.");
            }

            Type siteType = site.GetType();

            FieldBuilder fb = tg.AddStaticField(siteType, "sf" + (_id++).ToString());
            Expression init = Expression.Call(siteType.GetMethod("Create"), serializer.CreateExpression());

            _fieldBuilders.Add(fb);
            _fieldInits.Add(init);


            Type t = init.Type;
            if (t.IsGenericType)
            {
                Type[] args = t.GetGenericArguments()[0].GetGenericArguments(); ;
                // skip the first one, it is the site.
                for (int k = 1; k < args.Length; k++)
                {
                    Type p = args[k];
                    if (!p.Assembly.GetName().Name.Equals("mscorlib") && !p.Assembly.GetName().Name.Equals("Clojure"))
                        Console.WriteLine("Found {0}", p.ToString());
                }
            }

            // rewrite the node...
            return Expression.Field(null, fb);
        }

        #endregion

        #region DLR code generation : stolen code

        // The following code is lifted from DLR because of protection levels on various pieces.

        // From Microsoft.Scripting.Generation.ToDiskRewriter
        private bool RewriteDelegate(Type delegateType, out Type newDelegateType)
        {
            if (!ShouldRewriteDelegate(delegateType))
            {
                newDelegateType = null;
                return false;
            }

            if (_delegateTypes == null)
            {
                _delegateTypes = new Dictionary<Type, Type>();
            }

            // TODO: should caching move to AssemblyGen?
            if (!_delegateTypes.TryGetValue(delegateType, out newDelegateType))
            {
                MethodInfo invoke = delegateType.GetMethod("Invoke");

                newDelegateType = /* _typeGen.AssemblyGen. */MakeDelegateType(
                    delegateType.Name,
                    invoke.GetParameters().Map(p => p.ParameterType),
                    invoke.ReturnType
                );

                _delegateTypes[delegateType] = newDelegateType;
            }

            return true;
        }

  
        // From Microsoft.Scripting.Generation.ToDiskRewriter
        private bool ShouldRewriteDelegate(Type delegateType)
        {
            // We need to replace a transient delegateType with one stored in
            // the assembly we're saving to disk.
            //
            // One complication:
            // SaveAssemblies mode prevents us from detecting the module as
            // transient. If that option is turned on, always replace delegates
            // that live in another AssemblyBuilder

            var module = delegateType.Module as ModuleBuilder;
            if (module == null)
            {
                return false;
            }

            if (module.IsTransient())
            {
                return true;
            }

            if (Snippets.Shared.SaveSnippets && module.Assembly != _assemblyGen.AssemblyBuilder)
            {
                return true;
            }

            return false;
        }

        // From Microsoft.Scripting.Generation.AssemblyGen
        // Adapted to not being a method of AssemblyGen, which causes me to copy a WHOLE BUNCH of stuff.

        internal Type MakeDelegateType(string name, Type[] parameters, Type returnType)
        {
            TypeBuilder builder = /* _assemblyGen. */DefineType(name, typeof(MulticastDelegate), DelegateAttributes, false);
            builder.DefineConstructor(CtorAttributes, CallingConventions.Standard, _DelegateCtorSignature).SetImplementationFlags(ImplAttributes);
            builder.DefineMethod("Invoke", InvokeAttributes, returnType, parameters).SetImplementationFlags(ImplAttributes);
            return builder.CreateType();
        }

        // From Microsoft.Scripting.Generation.AssemblyGen
        private int _index;
        internal TypeBuilder DefineType(string name, Type parent, TypeAttributes attr, bool preserveName)
        {
            ContractUtils.RequiresNotNull(name, "name");
            ContractUtils.RequiresNotNull(parent, "parent");

            StringBuilder sb = new StringBuilder(name);
            if (!preserveName)
            {
                int index = Interlocked.Increment(ref _index);
                sb.Append("$");
                sb.Append(index);
            }

            // There is a bug in Reflection.Emit that leads to 
            // Unhandled Exception: System.Runtime.InteropServices.COMException (0x80131130): Record not found on lookup.
            // if there is any of the characters []*&+,\ in the type name and a method defined on the type is called.
            sb.Replace('+', '_').Replace('[', '_').Replace(']', '_').Replace('*', '_').Replace('&', '_').Replace(',', '_').Replace('\\', '_');

            name = sb.ToString();

            return /* _myModule */ _assemblyGen.ModuleBuilder.DefineType(name, attr, parent);
        }
        private const MethodAttributes CtorAttributes = MethodAttributes.RTSpecialName | MethodAttributes.HideBySig | MethodAttributes.Public;
        private const MethodImplAttributes ImplAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed;
        private const MethodAttributes InvokeAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual;
        private const TypeAttributes DelegateAttributes = TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.AnsiClass | TypeAttributes.AutoClass;
        private static readonly Type[] _DelegateCtorSignature = new Type[] { typeof(object), typeof(IntPtr) };


        #endregion


        #region Finalizing

        public void FinalizeType()
        {
            CreateStaticCtor();
            _typeGen.FinishType();
        }

        void CreateStaticCtor()
        {
            ConstructorBuilder ctorB = _typeBuilder.DefineConstructor(MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
            ILGen gen = new ILGen(ctorB.GetILGenerator());
            gen.EmitString(String.Format("Entering cctor for {0}",_assemblyGen.AssemblyBuilder.FullName));
            gen.EmitCall(typeof(System.Console), "WriteLine", new Type[] { typeof(string) });

            for (int i = 0; i < _fieldBuilders.Count; i++)
            {
                FieldBuilder fb = _fieldBuilders[i];
                Expression fbInit = _fieldInits[i];
                string setterName = String.Format("{0}_setter", fb.Name);

                MethodBuilder mbSetter = _typeBuilder.DefineMethod(
                    setterName,
                    MethodAttributes.Public | MethodAttributes.Static,
                    CallingConventions.Standard,
                    fbInit.Type,
                    Type.EmptyTypes);
                LambdaExpression initL = Expression.Lambda(Expression.Assign(Expression.Field(null, fb), fbInit));
                initL.CompileToMethod(mbSetter);

                gen.EmitString("Ready to call " + setterName);
                gen.EmitCall(typeof(System.Console), "WriteLine", new Type[] { typeof(string) });

                gen.EmitCall(mbSetter);
                gen.Emit(OpCodes.Pop);
            }

            gen.EmitString("After calls, before return");
            gen.EmitCall(typeof(System.Console), "WriteLine", new Type[] { typeof(string) });

            gen.Emit(OpCodes.Ret);
        }

        #endregion
    }
}

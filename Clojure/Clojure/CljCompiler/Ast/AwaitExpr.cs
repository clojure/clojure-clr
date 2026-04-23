
/**
 *   Copyright (c) Rich Hickey. All rights reserved.
 *   The use and distribution terms for this software are covered by the
 *   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
 *   which can be found in the file epl-v10.html at the root of this distribution.
 *   By using this software in any fashion, you are agreeing to be bound by
 * 	 the terms of this license.
 *   You must not remove this notice, or any other, from this software.
 **/

#if NET11_0_OR_GREATER

using System.Reflection;
using System.Reflection.Emit;

using System;
using System.Threading.Tasks;

namespace clojure.lang.CljCompiler.Ast;

public class AwaitExpr : Expr
{
        #region Data

        readonly Expr _taskExpr;
        public Expr TaskExpr => _taskExpr;

        readonly Type _resultType;
        readonly MethodInfo _awaitMethod;

        #endregion
    
        #region Ctors

        public AwaitExpr(Expr taskExpr, Type resultType, MethodInfo awaitMethod)
        {
            _taskExpr = taskExpr;
            _resultType = resultType;
            _awaitMethod = awaitMethod;
        }

        #endregion

        #region Type mangling

        public bool HasClrType => true;

        public Type ClrType => _resultType == typeof(void) ? typeof(object) : _resultType;

        #endregion

        #region Parsing

        public sealed class Parser : IParser
        {
            public Expr Parse(ParserContext pcon, object frm)
            {
                ISeq form = (ISeq)frm;

                if (!Compiler.RuntimeAsyncAvailable)
                    throw new ParseException(
                        "(await* ...) requires runtime async support");

                if (RT.count(form) != 2)
                    throw new ParseException(
                        "Wrong number of arguments to await*, expected: (await* expr)");

                ObjMethod method = (ObjMethod)Compiler.MethodVar.deref();

                if (method is null)
                    throw new ParseException(
                        "(await* ...) must appear inside a function body");

                if (Compiler.InCatchFinallyVar.deref() is not null)
                    throw new ParseException(
                        "(await* ...) cannot appear inside a catch, finally, or fault handler");

                if (!method.IsAsync)
                    throw new ParseException(
                        "(await* ...) can only be used inside a ^:async function or (async ...) block");

                //if (pcon.Rhc == RHC.Eval)
                //    return Compiler.Analyze(pcon,
                //        RT.list(RT.list(Compiler.FnOnceSym, PersistentVector.EMPTY, form)),
                //        "await__" + RT.nextID());

                Expr taskExpr = Compiler.Analyze(
                    pcon.SetRhc(RHC.Expression).SetAssign(false),
                    RT.second(form));

                //Type taskType;
                //if (taskExpr.HasClrType)
                //{
                //    taskType = taskExpr.ClrType;

                //    bool isTaskType =
                //        taskType == typeof(Task)
                //        || taskType == typeof(ValueTask)
                //        || (taskType.IsGenericType &&
                //            (taskType.GetGenericTypeDefinition() == typeof(Task<>)
                //             || taskType.GetGenericTypeDefinition() == typeof(ValueTask<>)));

                //    if (!isTaskType)
                //    {
                //        if (taskType == typeof(object))
                //            taskType = typeof(Task<object>);
                //        else
                //            throw new ParseException(
                //                $"(await* ...) requires a Task, Task<T>, ValueTask, or ValueTask<T>, got: {taskType.FullName}");
                //    }
                //}
                //else
                //{
                //    taskType = typeof(Task<object>);
                //}

                Type taskType = taskExpr.HasClrType ? taskExpr.ClrType : null;

                MethodInfo awaitMethod =
                    Compiler.AsyncMethodCache.ResolveAwaitMethod(taskType, out Type resultType);

                if (awaitMethod is null)
                    throw new ParseException(
                         $"(await* ...) requires a Task, Task<T>, ValueTask, or ValueTask<T>, got: {taskType?.FullName ?? "null"}");

                //method.HasAwait = true;

                return new AwaitExpr(taskExpr, resultType, awaitMethod);
            }
        }

        #endregion

        #region eval

        public object Eval()
        {
            throw new InvalidOperationException("Can't eval await*");
        }

        #endregion

        #region Code generation

        public void Emit(RHC rhc, ObjExpr objx, CljILGen ilg)
        {
            _taskExpr.Emit(RHC.Expression, objx, ilg);

            if (_taskExpr.HasClrType && _taskExpr.ClrType == typeof(object))
            {
                ilg.Emit(OpCodes.Castclass, typeof(Task<object>));
            }

            ilg.Emit(OpCodes.Call, _awaitMethod);

            if (_resultType != typeof(void))
            {
                if (rhc == RHC.Statement)
                    ilg.Emit(OpCodes.Pop);
                else if (_resultType.IsValueType)
                    ilg.Emit(OpCodes.Box, _resultType);
            }
            else
            {
                if (rhc != RHC.Statement)
                    ilg.Emit(OpCodes.Ldnull);
            }
        }

        public bool HasNormalExit() => true;

        #endregion

}


#endif
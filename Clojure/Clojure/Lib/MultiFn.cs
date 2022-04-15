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
using System.Runtime.CompilerServices;
using System.Threading;

namespace clojure.lang
{
    /// <summary>
    /// Represents a multifunction.
    /// </summary>
    /// <remarks>See the Clojure documentation for more details.</remarks>
    public class MultiFn : AFn, IDisposable
    {
        #region Data

        /// <summary>
        /// The function that dispatches calls to the correct method.
        /// </summary>
        readonly IFn _dispatchFn;

        /// <summary>
        /// The default dispatch value.
        /// </summary>
        readonly object _defaultDispatchVal;

        /// <summary>
        /// The hierarchy for this defmulti.
        /// </summary>
        readonly IRef _hierarchy;

        /// <summary>
        /// The name of this multifunction.
        /// </summary>
        readonly string _name;

        /// <summary>
        /// The methods defined for this multifunction.
        /// </summary>
        volatile IPersistentMap _methodTable;

        /// <summary>
        /// The methods defined for this multifunction.
        /// </summary>
        public IPersistentMap MethodTable
        {
            get { return _methodTable; }
        }

        /// <summary>
        /// Method preferences.
        /// </summary>
        volatile IPersistentMap _preferTable;

        /// <summary>
        /// Method preferences.
        /// </summary>
        public IPersistentMap PreferTable
        {
            get { return _preferTable; }
        }
        
        /// <summary>
        /// Cache of previously encountered dispatch-value to method mappings.
        /// </summary>
        volatile IPersistentMap _methodCache;

        /// <summary>
        /// Hierarchy on which cached computations are based.
        /// </summary>
        volatile object _cachedHierarchy;
        readonly ReaderWriterLockSlim _rw;
        bool _disposed;

        //static readonly Var _assoc = RT.var("clojure.core", "assoc");
        //static readonly Var _dissoc = RT.var("clojure.core", "dissoc");
        //static readonly Var _isa = RT.var("clojure.core", "isa?", null);  -- loading order dependent. bad.
        static readonly Var _isa = RT.var("clojure.core", "isa?");
        static readonly Var _parents = RT.var("clojure.core", "parents");
        //static readonly Var _hierarchy = RT.var("clojure.core", "global-hierarchy", null);

        #endregion

        #region C-tors & factory methods

        /// Construct a multifunction.
        /// </summary>
        /// <param name="name">The name</param>
        /// <param name="dispatchFn">The dispatch function.</param>
        /// <param name="defaultDispatchVal">The default dispatch value.</param>
        /// <param name="hierarchy">The hierarchy for this multifunction</param>
        public MultiFn(string name, IFn dispatchFn, object defaultDispatchVal, IRef hierarchy)
        {
            _name = name;
            _dispatchFn = dispatchFn;
            _defaultDispatchVal = defaultDispatchVal;
            _methodTable = PersistentHashMap.EMPTY;
            _methodCache = MethodTable;
            _preferTable = PersistentHashMap.EMPTY;
            _hierarchy = hierarchy;
            _cachedHierarchy = null;
            _rw = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        }

        #endregion

        #region External interface

        /// <summary>
        /// Add a new method to this multimethod.
        /// </summary>
        /// <param name="dispatchVal">The discriminator value for this method.</param>
        /// <param name="method">The method code.</param>
        /// <returns>This multifunction.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public MultiFn addMethod(object dispatchVal, IFn method)
        {
            _rw.EnterWriteLock();
            try
            {
                _methodTable = MethodTable.assoc(dispatchVal, method);
                ResetCache();
                return this;
            }
            finally
            {
                _rw.ExitWriteLock();
            }
        }


        /// <summary>
        /// Remove a method.
        /// </summary>
        /// <param name="dispatchVal">The dispatch value for the multimethod.</param>
        /// <returns>This multifunction.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public MultiFn removeMethod(object dispatchVal)
        {
            _rw.EnterWriteLock();
            try
            {
                _methodTable = MethodTable.without(dispatchVal);
                ResetCache();
                return this;
            }
            finally
            {
                _rw.ExitWriteLock();
            }
        }

        /// <summary>
        /// Add a preference for one method over another.
        /// </summary>
        /// <param name="dispatchValX">The more preferred dispatch value.</param>
        /// <param name="dispatchValY">The less preferred dispatch value.</param>
        /// <returns>This multifunction.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public MultiFn preferMethod(object dispatchValX, object dispatchValY)
        {
            _rw.EnterWriteLock();
            try
            {
                if (Prefers(_hierarchy.deref(),dispatchValY, dispatchValX))
                    throw new InvalidOperationException(String.Format("Preference conflict in multimethod {0}: {1} is already preferred to {2}", _name, dispatchValY, dispatchValX));
                _preferTable = PreferTable.assoc(dispatchValX,
                    RT.conj((IPersistentCollection)RT.get(_preferTable, dispatchValX, PersistentHashSet.EMPTY),
                            dispatchValY));
                ResetCache();
                return this;
            }
            finally
            {
                _rw.ExitWriteLock();
            }
        }

        #endregion

        #region Implementation details

        /// <summary>
        /// Is one value preferred over another?
        /// </summary>
        /// <param name="x">The first dispatch value.</param>
        /// <param name="y">The second dispatch value.</param>
        /// <returns><value>true</value> if <paramref name="x"/> is preferred over <paramref name="y"/></returns>
        private bool Prefers(object hierarchy, object x, object y)
        {
            IPersistentSet xprefs = (IPersistentSet)PreferTable.valAt(x);
            if (xprefs != null && xprefs.contains(y))
                return true;
            for (ISeq ps = RT.seq(_parents.invoke(hierarchy,y)); ps != null; ps = ps.next())
                if (Prefers(hierarchy, x, ps.first()))
                    return true;
            for (ISeq ps = RT.seq(_parents.invoke(hierarchy, x)); ps != null; ps = ps.next())
                if (Prefers(hierarchy, ps.first(), y))
                    return true;
            return false;
        }

        /// <summary>
        /// Check the hierarchy.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private bool IsA(object hierarchy, object x, object y)
        {
            return RT.booleanCast(_isa.invoke(hierarchy, x, y));
        }

        /// <summary>
        /// Determine if one dispatch is preferred over another.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private bool Dominates(object hierarchy, object x, object y)
        {
            return Prefers(hierarchy, x, y) || IsA(hierarchy, x, y);
        }


        /// <summary>
        /// Reset the method cache.
        /// </summary>
        /// <returns></returns>
        private IPersistentMap ResetCache()
        {
            _rw.EnterWriteLock();
            try
            {
                _methodCache = MethodTable;
                _cachedHierarchy = _hierarchy.deref();
                return _methodCache;
            }
            finally
            {
                _rw.ExitWriteLock();
            }
        }

        /// <summary>
        /// Get the method for a dispatch value.
        /// </summary>
        /// <param name="dispatchVal">The dispatch value.</param>
        /// <returns>The preferred method for the value.</returns>
        /// <remarks>lower initial letter for core.clj compatibility</remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public IFn getMethod(object dispatchVal)
        {
            if (_cachedHierarchy != _hierarchy.deref())
                ResetCache();

            IFn targetFn = (IFn)_methodCache.valAt(dispatchVal);
            if (targetFn != null)
                return targetFn;

            return FindAndCacheBestMethod(dispatchVal);
        }

        private IFn GetFn(object dispatchVal)
        {
            IFn targetFn = getMethod(dispatchVal);
            if (targetFn == null)
                throw new ArgumentException(String.Format("No method for dispatch value: {0}", dispatchVal));
            return targetFn;
        }

        /// <summary>
        /// Get the method for a dispatch value and cache it.
        /// </summary>
        /// <param name="dispatchVal">The disaptch value.</param>
        /// <returns>The mest method.</returns>
        private IFn FindAndCacheBestMethod(object dispatchVal)
        {
            _rw.EnterWriteLock();
            object bestValue;
            IPersistentMap mt = _methodTable;
            IPersistentMap pt = _preferTable;
            object ch = _cachedHierarchy;
            try
            {
                IMapEntry bestEntry = null;

                foreach (IMapEntry me in MethodTable)
                {
                    if (IsA(ch, dispatchVal, me.key()))
                    {
                        if (bestEntry == null || Dominates(ch, me.key(), bestEntry.key()))
                            bestEntry = me;
                        if (!Dominates(ch, bestEntry.key(), me.key()))
                            throw new ArgumentException(String.Format("Multiple methods in multimethod {0} match dispatch value: {1} -> {2} and {3}, and neither is preferred",
                                _name, dispatchVal, me.key(), bestEntry.key()));
                    }
                }
                if (bestEntry == null)
                {
                    bestValue = _methodTable.valAt(_defaultDispatchVal);
                    if (bestValue == null)
                        return null;
                }
                else
                    bestValue = bestEntry.val();
            }
            finally
            {
                _rw.ExitWriteLock();
            }

            // ensure basis has stayed stable throughout, else redo
            _rw.EnterWriteLock();
            try
            {
                if (mt == _methodTable
                    && pt == _preferTable
                    && ch == _cachedHierarchy
                    && _cachedHierarchy == _hierarchy.deref())
                {
                    // place in cache
                    _methodCache = _methodCache.assoc(dispatchVal, bestValue);
                    return (IFn)bestValue;
                }
                else
                {
                    ResetCache();
                    return FindAndCacheBestMethod(dispatchVal);
                }
            }
            finally
            {
                _rw.ExitWriteLock();
            }
        }

        #endregion

        #region core.clj compatibility

        /// <summary>
        /// Get the map of dispatch values to dispatch fns.
        /// </summary>
        /// <returns>The map of dispatch values to dispatch fns.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public IPersistentMap getMethodTable()
        {
            return MethodTable;
        }


        /// <summary>
        /// Get the map of preferred value to set of other values.
        /// </summary>
        /// <returns>The map of preferred value to set of other values.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public IPersistentMap getPreferTable()
        {
            return PreferTable;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public MultiFn reset()
        {
            _rw.EnterWriteLock();
            try
            {
                _methodTable = _methodCache = _preferTable = PersistentHashMap.EMPTY;
                _cachedHierarchy = null;
                return this;
            }
            finally
            {
                _rw.ExitWriteLock();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "ClojureJVM name match")]
        public IFn dispatchFn()
        {
            return _dispatchFn;
        }

        #endregion

        #region IFn members

#pragma warning disable IDE0059 // Unnecessary assignment of a value

        public override object invoke()
        {
            return GetFn(_dispatchFn.invoke()).invoke();
        }

        public override object invoke(object arg1)
        {
            return GetFn(_dispatchFn.invoke(arg1)).
                    invoke(Util.Ret1(arg1, arg1 = null));
        }

        public override object invoke(object arg1, object arg2)
        {
            return GetFn(_dispatchFn.invoke(arg1, arg2)).
                    invoke(Util.Ret1(arg1, arg1 = null),
                            Util.Ret1(arg2, arg2 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3)
        {
            return GetFn(_dispatchFn.invoke(arg1, arg2, arg3)).
                    invoke(Util.Ret1(arg1, arg1 = null),
                            Util.Ret1(arg2, arg2 = null),
                             Util.Ret1(arg3, arg3 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4)
        {
            return GetFn(_dispatchFn.invoke(arg1, arg2, arg3, arg4)).
                    invoke(Util.Ret1(arg1, arg1 = null),
                            Util.Ret1(arg2, arg2 = null),
                             Util.Ret1(arg3, arg3 = null),
                             Util.Ret1(arg4, arg4 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4, object arg5)
        {
            return GetFn(_dispatchFn.invoke(arg1, arg2, arg3, arg4, arg5)).
                    invoke(Util.Ret1(arg1, arg1 = null),
                            Util.Ret1(arg2, arg2 = null),
                             Util.Ret1(arg3, arg3 = null),
                             Util.Ret1(arg4, arg4 = null),
                             Util.Ret1(arg5, arg5 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6)
        {
            return GetFn(_dispatchFn.invoke(arg1, arg2, arg3, arg4, arg5, arg6)).
                    invoke(Util.Ret1(arg1, arg1 = null),
                            Util.Ret1(arg2, arg2 = null),
                             Util.Ret1(arg3, arg3 = null),
                             Util.Ret1(arg4, arg4 = null),
                             Util.Ret1(arg5, arg5 = null),
                             Util.Ret1(arg6, arg6 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7)
        {
            return GetFn(_dispatchFn.invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7)).
                    invoke(Util.Ret1(arg1, arg1 = null),
                            Util.Ret1(arg2, arg2 = null),
                             Util.Ret1(arg3, arg3 = null),
                             Util.Ret1(arg4, arg4 = null),
                             Util.Ret1(arg5, arg5 = null),
                             Util.Ret1(arg6, arg6 = null),
                             Util.Ret1(arg7, arg7 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                             object arg8)
        {
            return GetFn(_dispatchFn.invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8)).
                    invoke(Util.Ret1(arg1, arg1 = null),
                            Util.Ret1(arg2, arg2 = null),
                             Util.Ret1(arg3, arg3 = null),
                             Util.Ret1(arg4, arg4 = null),
                             Util.Ret1(arg5, arg5 = null),
                             Util.Ret1(arg6, arg6 = null),
                             Util.Ret1(arg7, arg7 = null),
                             Util.Ret1(arg8, arg8 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                             object arg8, object arg9)
        {
            return GetFn(_dispatchFn.invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9)).
                    invoke(Util.Ret1(arg1, arg1 = null),
                            Util.Ret1(arg2, arg2 = null),
                             Util.Ret1(arg3, arg3 = null),
                             Util.Ret1(arg4, arg4 = null),
                             Util.Ret1(arg5, arg5 = null),
                             Util.Ret1(arg6, arg6 = null),
                             Util.Ret1(arg7, arg7 = null),
                             Util.Ret1(arg8, arg8 = null),
                             Util.Ret1(arg9, arg9 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                             object arg8, object arg9, object arg10)
        {
            return GetFn(_dispatchFn.invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10)).
                    invoke(Util.Ret1(arg1, arg1 = null),
                            Util.Ret1(arg2, arg2 = null),
                             Util.Ret1(arg3, arg3 = null),
                             Util.Ret1(arg4, arg4 = null),
                             Util.Ret1(arg5, arg5 = null),
                             Util.Ret1(arg6, arg6 = null),
                             Util.Ret1(arg7, arg7 = null),
                             Util.Ret1(arg8, arg8 = null),
                             Util.Ret1(arg9, arg9 = null),
                             Util.Ret1(arg10, arg10 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                             object arg8, object arg9, object arg10, object arg11)
        {
            return GetFn(_dispatchFn.invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11)).
                    invoke(Util.Ret1(arg1, arg1 = null),
                            Util.Ret1(arg2, arg2 = null),
                             Util.Ret1(arg3, arg3 = null),
                             Util.Ret1(arg4, arg4 = null),
                             Util.Ret1(arg5, arg5 = null),
                             Util.Ret1(arg6, arg6 = null),
                             Util.Ret1(arg7, arg7 = null),
                             Util.Ret1(arg8, arg8 = null),
                             Util.Ret1(arg9, arg9 = null),
                             Util.Ret1(arg10, arg10 = null),
                             Util.Ret1(arg11, arg11 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                             object arg8, object arg9, object arg10, object arg11, object arg12)
        {
            return GetFn(_dispatchFn.invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12)).
                    invoke(Util.Ret1(arg1, arg1 = null),
                            Util.Ret1(arg2, arg2 = null),
                             Util.Ret1(arg3, arg3 = null),
                             Util.Ret1(arg4, arg4 = null),
                             Util.Ret1(arg5, arg5 = null),
                             Util.Ret1(arg6, arg6 = null),
                             Util.Ret1(arg7, arg7 = null),
                             Util.Ret1(arg8, arg8 = null),
                             Util.Ret1(arg9, arg9 = null),
                             Util.Ret1(arg10, arg10 = null),
                             Util.Ret1(arg11, arg11 = null),
                             Util.Ret1(arg12, arg12 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                             object arg8, object arg9, object arg10, object arg11, object arg12, object arg13)
        {
            return GetFn(_dispatchFn.invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13)).
                    invoke(Util.Ret1(arg1, arg1 = null),
                            Util.Ret1(arg2, arg2 = null),
                             Util.Ret1(arg3, arg3 = null),
                             Util.Ret1(arg4, arg4 = null),
                             Util.Ret1(arg5, arg5 = null),
                             Util.Ret1(arg6, arg6 = null),
                             Util.Ret1(arg7, arg7 = null),
                             Util.Ret1(arg8, arg8 = null),
                             Util.Ret1(arg9, arg9 = null),
                             Util.Ret1(arg10, arg10 = null),
                             Util.Ret1(arg11, arg11 = null),
                             Util.Ret1(arg12, arg12 = null),
                             Util.Ret1(arg13, arg13 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                             object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14)
        {
            return GetFn(
                    _dispatchFn.invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14)).
                    invoke(Util.Ret1(arg1, arg1 = null),
                            Util.Ret1(arg2, arg2 = null),
                             Util.Ret1(arg3, arg3 = null),
                             Util.Ret1(arg4, arg4 = null),
                             Util.Ret1(arg5, arg5 = null),
                             Util.Ret1(arg6, arg6 = null),
                             Util.Ret1(arg7, arg7 = null),
                             Util.Ret1(arg8, arg8 = null),
                             Util.Ret1(arg9, arg9 = null),
                             Util.Ret1(arg10, arg10 = null),
                             Util.Ret1(arg11, arg11 = null),
                             Util.Ret1(arg12, arg12 = null),
                             Util.Ret1(arg13, arg13 = null),
                             Util.Ret1(arg14, arg14 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                             object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14,
                             object arg15)
        {
            return GetFn(
                    _dispatchFn.invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14,
                                      arg15)).
                    invoke(Util.Ret1(arg1, arg1 = null),
                            Util.Ret1(arg2, arg2 = null),
                             Util.Ret1(arg3, arg3 = null),
                             Util.Ret1(arg4, arg4 = null),
                             Util.Ret1(arg5, arg5 = null),
                             Util.Ret1(arg6, arg6 = null),
                             Util.Ret1(arg7, arg7 = null),
                             Util.Ret1(arg8, arg8 = null),
                             Util.Ret1(arg9, arg9 = null),
                             Util.Ret1(arg10, arg10 = null),
                             Util.Ret1(arg11, arg11 = null),
                             Util.Ret1(arg12, arg12 = null),
                             Util.Ret1(arg13, arg13 = null),
                             Util.Ret1(arg14, arg14 = null),
                             Util.Ret1(arg15, arg15 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                             object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14,
                             object arg15, object arg16)
        {
            return GetFn(
                    _dispatchFn.invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14,
                                      arg15, arg16)).
                    invoke(Util.Ret1(arg1, arg1 = null),
                            Util.Ret1(arg2, arg2 = null),
                             Util.Ret1(arg3, arg3 = null),
                             Util.Ret1(arg4, arg4 = null),
                             Util.Ret1(arg5, arg5 = null),
                             Util.Ret1(arg6, arg6 = null),
                             Util.Ret1(arg7, arg7 = null),
                             Util.Ret1(arg8, arg8 = null),
                             Util.Ret1(arg9, arg9 = null),
                             Util.Ret1(arg10, arg10 = null),
                             Util.Ret1(arg11, arg11 = null),
                             Util.Ret1(arg12, arg12 = null),
                             Util.Ret1(arg13, arg13 = null),
                             Util.Ret1(arg14, arg14 = null),
                             Util.Ret1(arg15, arg15 = null),
                             Util.Ret1(arg16, arg16 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                             object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14,
                             object arg15, object arg16, object arg17)
        {
            return GetFn(
                    _dispatchFn.invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14,
                                      arg15, arg16, arg17)).
                    invoke(Util.Ret1(arg1, arg1 = null),
                            Util.Ret1(arg2, arg2 = null),
                             Util.Ret1(arg3, arg3 = null),
                             Util.Ret1(arg4, arg4 = null),
                             Util.Ret1(arg5, arg5 = null),
                             Util.Ret1(arg6, arg6 = null),
                             Util.Ret1(arg7, arg7 = null),
                             Util.Ret1(arg8, arg8 = null),
                             Util.Ret1(arg9, arg9 = null),
                             Util.Ret1(arg10, arg10 = null),
                             Util.Ret1(arg11, arg11 = null),
                             Util.Ret1(arg12, arg12 = null),
                             Util.Ret1(arg13, arg13 = null),
                             Util.Ret1(arg14, arg14 = null),
                             Util.Ret1(arg15, arg15 = null),
                             Util.Ret1(arg16, arg16 = null),
                             Util.Ret1(arg17, arg17 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                             object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14,
                             object arg15, object arg16, object arg17, object arg18)
        {
            return GetFn(
                    _dispatchFn.invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14,
                                      arg15, arg16, arg17, arg18)).
                    invoke(Util.Ret1(arg1, arg1 = null),
                            Util.Ret1(arg2, arg2 = null),
                             Util.Ret1(arg3, arg3 = null),
                             Util.Ret1(arg4, arg4 = null),
                             Util.Ret1(arg5, arg5 = null),
                             Util.Ret1(arg6, arg6 = null),
                             Util.Ret1(arg7, arg7 = null),
                             Util.Ret1(arg8, arg8 = null),
                             Util.Ret1(arg9, arg9 = null),
                             Util.Ret1(arg10, arg10 = null),
                             Util.Ret1(arg11, arg11 = null),
                             Util.Ret1(arg12, arg12 = null),
                             Util.Ret1(arg13, arg13 = null),
                             Util.Ret1(arg14, arg14 = null),
                             Util.Ret1(arg15, arg15 = null),
                             Util.Ret1(arg16, arg16 = null),
                             Util.Ret1(arg17, arg17 = null),
                             Util.Ret1(arg18, arg18 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                             object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14,
                             object arg15, object arg16, object arg17, object arg18, object arg19)
        {
            return GetFn(
                    _dispatchFn.invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14,
                                      arg15, arg16, arg17, arg18, arg19)).
                    invoke(Util.Ret1(arg1, arg1 = null),
                            Util.Ret1(arg2, arg2 = null),
                             Util.Ret1(arg3, arg3 = null),
                             Util.Ret1(arg4, arg4 = null),
                             Util.Ret1(arg5, arg5 = null),
                             Util.Ret1(arg6, arg6 = null),
                             Util.Ret1(arg7, arg7 = null),
                             Util.Ret1(arg8, arg8 = null),
                             Util.Ret1(arg9, arg9 = null),
                             Util.Ret1(arg10, arg10 = null),
                             Util.Ret1(arg11, arg11 = null),
                             Util.Ret1(arg12, arg12 = null),
                             Util.Ret1(arg13, arg13 = null),
                             Util.Ret1(arg14, arg14 = null),
                             Util.Ret1(arg15, arg15 = null),
                             Util.Ret1(arg16, arg16 = null),
                             Util.Ret1(arg17, arg17 = null),
                             Util.Ret1(arg18, arg18 = null),
                             Util.Ret1(arg19, arg19 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                             object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14,
                             object arg15, object arg16, object arg17, object arg18, object arg19, object arg20)
        {
            return GetFn(
                    _dispatchFn.invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14,
                                      arg15, arg16, arg17, arg18, arg19, arg20)).
                    invoke(Util.Ret1(arg1, arg1 = null),
                            Util.Ret1(arg2, arg2 = null),
                             Util.Ret1(arg3, arg3 = null),
                             Util.Ret1(arg4, arg4 = null),
                             Util.Ret1(arg5, arg5 = null),
                             Util.Ret1(arg6, arg6 = null),
                             Util.Ret1(arg7, arg7 = null),
                             Util.Ret1(arg8, arg8 = null),
                             Util.Ret1(arg9, arg9 = null),
                             Util.Ret1(arg10, arg10 = null),
                             Util.Ret1(arg11, arg11 = null),
                             Util.Ret1(arg12, arg12 = null),
                             Util.Ret1(arg13, arg13 = null),
                             Util.Ret1(arg14, arg14 = null),
                             Util.Ret1(arg15, arg15 = null),
                             Util.Ret1(arg16, arg16 = null),
                             Util.Ret1(arg17, arg17 = null),
                             Util.Ret1(arg18, arg18 = null),
                             Util.Ret1(arg19, arg19 = null),
                             Util.Ret1(arg20, arg20 = null));
        }

        public override object invoke(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7,
                             object arg8, object arg9, object arg10, object arg11, object arg12, object arg13, object arg14,
                             object arg15, object arg16, object arg17, object arg18, object arg19, object arg20, params object[] args)
        {
            return GetFn(
                    _dispatchFn.invoke(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14,
                                      arg15, arg16, arg17, arg18, arg19, arg20, args)).
                    invoke(Util.Ret1(arg1, arg1 = null),
                            Util.Ret1(arg2, arg2 = null),
                             Util.Ret1(arg3, arg3 = null),
                             Util.Ret1(arg4, arg4 = null),
                             Util.Ret1(arg5, arg5 = null),
                             Util.Ret1(arg6, arg6 = null),
                             Util.Ret1(arg7, arg7 = null),
                             Util.Ret1(arg8, arg8 = null),
                             Util.Ret1(arg9, arg9 = null),
                             Util.Ret1(arg10, arg10 = null),
                             Util.Ret1(arg11, arg11 = null),
                             Util.Ret1(arg12, arg12 = null),
                             Util.Ret1(arg13, arg13 = null),
                             Util.Ret1(arg14, arg14 = null),
                             Util.Ret1(arg15, arg15 = null),
                             Util.Ret1(arg16, arg16 = null),
                             Util.Ret1(arg17, arg17 = null),
                             Util.Ret1(arg18, arg18 = null),
                             Util.Ret1(arg19, arg19 = null),
                             Util.Ret1(arg20, arg20 = null),
                            args);
        }
#pragma warning restore IDE0059 // Unnecessary assignment of a value

        #endregion

        #region IDisposable members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    ((IDisposable)_rw).Dispose();
                }

                _disposed = true;
            }
        }
        #endregion
    }
}

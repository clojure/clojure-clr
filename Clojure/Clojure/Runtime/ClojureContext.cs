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
using Microsoft.Scripting.Runtime;
using clojure.lang.Runtime.Binding;
using System.Reflection;
using System.Threading;
using Microsoft.Scripting;
using Microsoft.Scripting.Actions;
using System.Dynamic;

namespace clojure.lang.Runtime
{
    public sealed class ClojureContext : LanguageContext
    {
        #region Constants

        public const string ClojureDisplayName = "ClojureCLR";
        public const string ClojureNames = "ClojureCLR;Clojure;clj";
        public const string ClojureFileExtensions = ".clj";

        private static readonly Guid ClojureLanguageGuid = new Guid("7ED35044-DF7D-4D20-B8A2-A4C4847B26B3");
        private static readonly Guid LanguageVendor_Clojure = new Guid("57EA02D4-9851-411F-9BAB-F12B4F88B960");

        #endregion

        #region Instance variables

        private readonly ClojureOptions _options;
        private readonly ClojureBinder _binder;
        private readonly ClojureOverloadResolverFactory _sharedOverloadResolverFactory;

        #endregion

        #region Properties

        public override Guid    LanguageGuid    { get { return ClojureLanguageGuid; } }
        public override Version LanguageVersion { get { return new AssemblyName(typeof(ClojureContext).Assembly.FullName).Version; } }
        public override LanguageOptions Options { get { return ClojureOptions; } }
        public override Guid    VendorGuid      { get { return LanguageVendor_Clojure; } } 
        internal ClojureOptions ClojureOptions  { get { return _options; } }

        internal ClojureBinder Binder { get { return _binder; } }

        /// <summary>
        /// Returns an overload resolver for the current ClojureContext.  
        /// </summary>
        internal ClojureOverloadResolverFactory SharedOverloadResolverFactory
        {
            get
            {
                return _sharedOverloadResolverFactory;
            }
        }

        #endregion

        #region Default context

        static ClojureContext _default;

        public static ClojureContext Default
        {
            get { return _default; }
        }

        #endregion

        #region C-tors

        public ClojureContext(ScriptDomainManager manager, IDictionary<string, object> options)
            : base(manager)
        {
            _options = new ClojureOptions(options);
            _binder = new ClojureBinder(this);
            _sharedOverloadResolverFactory = new ClojureOverloadResolverFactory(_binder);

            Interlocked.CompareExchange(ref _default, this, null);
        }

        #endregion

        #region Required overload

        public override ScriptCode CompileSourceCode(SourceUnit sourceUnit, CompilerOptions options, ErrorSink errorSink)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}

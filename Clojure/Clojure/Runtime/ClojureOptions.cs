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
using System.Collections.ObjectModel;
using System.Collections.Generic;

using Microsoft.Scripting;


namespace clojure.lang.Runtime
{
    [Serializable]
    public sealed class ClojureOptions : LanguageOptions
    {
        private readonly ReadOnlyCollection<string> _arguments;


        /// <summary>
        /// Gets the collection of command line arguments.
        /// </summary>
        public ReadOnlyCollection<string>/*!*/ Arguments
        {
            get { return _arguments; }
        }


        public ClojureOptions()
            : this(null)
        {
        }

        public ClojureOptions(IDictionary<string, object> options)
            :base(options)
        {
            _arguments = GetStringCollectionOption(options, "Arguments") ?? EmptyStringCollection;
        }


    }
}

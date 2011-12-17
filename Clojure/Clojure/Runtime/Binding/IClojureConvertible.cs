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


using System.Dynamic;

namespace clojure.lang.Runtime.Binding
{
    // Ripped off from IPy
    // Though it is pretty inevitable.
    // Many metaobject classes implement this in IronPython.
    // Because Clojure runs mostly on naked CLR objects,
    //   the only metaobject class that needs this is above IFn, 
    //   primarily to support generic type inferencing,
    //   or more generally conversion to delegate types.

    interface IClojureConvertible
    {
        //DynamicMetaObject BindConvert(ClojureConversionBinder binder);
    }
}

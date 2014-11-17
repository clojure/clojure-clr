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


namespace clojure.lang
{
    public interface IAtom
    {
        object swap(IFn f);
        object swap(IFn f, object arg);
        object swap(IFn f, object arg1, object arg2);
        object swap(IFn f, object x, object y, ISeq args);
        bool compareAndSet(object oldv, object newv);
        object reset(object newval);
    }
}

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

using NUnit.Framework;

using clojure.lang;


namespace Clojure.Tests.LibTests
{
    // TODO: Add tests for PersistentTreeSet
    class PersistentTreeSetTests
    {
    }

    [TestFixture]
    public class PersistentTreeSet_IObj_Tests : IObjTests
    {

        [SetUp]
        public void Setup()
        {
            IPersistentMap meta = new DummyMeta();

            PersistentTreeSet m = PersistentTreeSet.create(RT.seq(PersistentVector.create("a", "b")));

            _objWithNullMeta = (IObj)m;
            _obj = _objWithNullMeta.withMeta(meta);
            _expectedType = typeof(PersistentTreeSet);
        }
    }
}

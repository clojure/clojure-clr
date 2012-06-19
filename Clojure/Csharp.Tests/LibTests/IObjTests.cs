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
    /// <summary>
    /// Base class for testing the IMeta interface functionality.
    /// </summary>
    public abstract class IObjTests : AssertionHelper
    {
        /// <summary>
        /// Object to test for null meta.  Set null if no test.  Initialize in Setup.
        /// </summary>
        protected IObj _objWithNullMeta;


        /// <summary>
        /// The object to test.  Initialize in Setup.
        /// </summary>
        protected IObj _obj;

        /// <summary>
        /// Expected type of return from withMeta.  Set null if no test.  Initialize in Setup.
        /// </summary>
        protected Type _expectedType;

        /// <summary>
        /// Test if same object with no change in meta.  Set to false to skip test.
        /// </summary>
        protected bool _testNoChange = true;


        IPersistentMap _meta = null;

        void InitMocks()
        {
            _meta = new DummyMeta();
        }
            

        [Test]
        public void withMeta_has_correct_meta()
        {
            InitMocks();
            IObj obj2 = _obj.withMeta(_meta);
            Expect(obj2.meta(), SameAs(_meta));
        }

        [Test]
        public void withMeta_returns_correct_type()
        {
            if (_expectedType == null)
                return;

            InitMocks();
            IObj obj2 = _obj.withMeta(_meta);
            Expect(obj2, TypeOf(_expectedType));
        }

        [Test]
        public void withMeta_returns_self_if_no_change()
        {
            if (_testNoChange)
            {
                IObj obj2 = _obj.withMeta(_obj.meta());
                Expect(obj2, SameAs(_obj));
            }
        }

        [Test]
        public void Verify_Null_Meta()
        {
            if (_objWithNullMeta == null)
                return;
            Expect(_objWithNullMeta.meta(), Null);
        }
    }
}

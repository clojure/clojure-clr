using System;
using System.Reflection;
using clojure.lang;
using NUnit.Framework;

namespace Clojure.Tests.LibTests
{
    [TestFixture]
    public class GenDelegateTests
    {
        [OneTimeSetUp]
        public void Setup()
        {
            RT.Init();
        }

        // Helper IFn that records what it was called with
        class RecordingFn : AFn
        {
            public object[] LastArgs;
            public object ReturnValue;

            public RecordingFn(object returnValue = null)
            {
                ReturnValue = returnValue;
            }

            public override object invoke()
            {
                LastArgs = Array.Empty<object>();
                return ReturnValue;
            }

            public override object invoke(object arg1)
            {
                LastArgs = new[] { arg1 };
                return ReturnValue;
            }

            public override object invoke(object arg1, object arg2)
            {
                LastArgs = new[] { arg1, arg2 };
                return ReturnValue;
            }
        }

        [Test]
        public void DelegateInvokeHasNoHiddenParameters()
        {
            var fn = new RecordingFn("hello");
            var del = GenDelegate.Create(typeof(Func<string>), fn);

            // The delegate's Method.GetParameters() must have exactly 0 parameters.
            // The old Expression.Lambda approach injected a hidden Closure parameter.
            var parameters = del.Method.GetParameters();
            Assert.That(parameters, Has.Length.EqualTo(0),
                "Func<string> delegate should have no parameters (no hidden Closure)");
        }

        [Test]
        public void DelegateWithParametersMatchesSignature()
        {
            var fn = new RecordingFn("result");
            var del = GenDelegate.Create(typeof(Func<int, string, string>), fn);

            var parameters = del.Method.GetParameters();
            Assert.That(parameters, Has.Length.EqualTo(2));
            Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(int)));
            Assert.That(parameters[1].ParameterType, Is.EqualTo(typeof(string)));
        }

        [Test]
        public void DelegateInvokesIFnCorrectly()
        {
            var fn = new RecordingFn("hello from IFn");
            var del = (Func<string>)GenDelegate.Create(typeof(Func<string>), fn);

            var result = del();

            Assert.That(result, Is.EqualTo("hello from IFn"));
            Assert.That(fn.LastArgs, Has.Length.EqualTo(0));
        }

        [Test]
        public void DelegateBoxesValueTypeArgs()
        {
            var fn = new RecordingFn("ok");
            var del = (Func<int, string>)GenDelegate.Create(typeof(Func<int, string>), fn);

            del(42);

            Assert.That(fn.LastArgs, Has.Length.EqualTo(1));
            Assert.That(fn.LastArgs[0], Is.EqualTo(42));
            Assert.That(fn.LastArgs[0], Is.InstanceOf<object>(),
                "int argument should be boxed to object for IFn.invoke");
        }

        [Test]
        public void DelegateUnboxesValueTypeReturn()
        {
            var fn = new RecordingFn(99);
            var del = (Func<int>)GenDelegate.Create(typeof(Func<int>), fn);

            var result = del();

            Assert.That(result, Is.EqualTo(99));
        }

        [Test]
        public void VoidDelegateWorks()
        {
            var fn = new RecordingFn(null);
            var del = (Action)GenDelegate.Create(typeof(Action), fn);

            Assert.DoesNotThrow(() => del());
            Assert.That(fn.LastArgs, Has.Length.EqualTo(0));
        }

        [Test]
        public void DelegateWithTwoArgsPassesBothCorrectly()
        {
            var fn = new RecordingFn("combined");
            var del = (Func<string, int, string>)GenDelegate.Create(typeof(Func<string, int, string>), fn);

            var result = del("hello", 7);

            Assert.That(result, Is.EqualTo("combined"));
            Assert.That(fn.LastArgs[0], Is.EqualTo("hello"));
            Assert.That(fn.LastArgs[1], Is.EqualTo(7));
        }
    }
}

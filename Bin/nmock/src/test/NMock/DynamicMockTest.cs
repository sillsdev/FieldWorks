// Copyright c 2002, Joe Walnes, Chris Stevenson, Owen Rogers
// See LICENSE.txt for details.

using NUnit.Framework;
using System;
using System.Collections;
using NMock.Constraints;

namespace NMock
{

	[TestFixture] public class DynamicMockTest : Assertion
	{
		#region types
		public interface IBaseBlah
		{
			string GetSuperString();
		}

		public interface IBlah : IBaseBlah
		{
			object DoStuff(string name);
		}

		private class CustomMock : DynamicMock
		{
			public CustomMock(Type t) : base(t) {}

			public override object Invoke(string name, object[] args, string[] typeNames)
			{
				return "CUSTOM";
			}
		}
		public class SameClass
		{
			public virtual string A() { return c() ? b() : "Default"; }
			protected virtual string b() { throw new Exception("Should not have called b()"); }
			protected virtual bool c() { return true; }
		}

		public interface IValueType
		{
			ArrayList Query(string symbol, DateTime arg2);
		}

		public class Thingy
		{
			public void X() {}
		}
		public interface IOverloadedMethods
		{
			void DoStuff(string one, int two);
			void DoStuff(string one);
		}
		public interface TwoMethods
		{
			void Foo(String a);
			int Bar(String a);
		}
		public interface IWithParams
		{
			void WithLeadingParameter(int i, params object[] args);
			void WithoutLeadingParameter(params object[] args);
		}
		public class WithNonEmptyConstructor
		{
			public WithNonEmptyConstructor(string unused)
			{
			}
		}
		public interface IWithProperty
		{
			string Name { get; set; }
		}
		public class ClassWithCastedReturnValue
		{
			public virtual object Method()
			{
				return null;
			}
		}
		public interface IWithOutParam
		{
			void MethodWithOutParam(string s, out int a);
		}
		#endregion

		[Test] public void HasADefaultNameBasedOnMockedType()
		{
			IMock mock = new DynamicMock(typeof(IBlah));
			Assertion.AssertEquals("MockIBlah", mock.Name);
		}

		[Test] public void CanBeExplicitlyNamed()
		{
			IMock mock = new DynamicMock(typeof(IBlah), "XBlah");
			Assertion.AssertEquals("XBlah", mock.Name);
		}

		[Test] public void DynamicallyImplementsAnInterface()
		{
			IMock mock = new DynamicMock(typeof(IBlah));

			mock.ExpectAndReturn("DoStuff", "world", "hello");

			IBlah blah = (IBlah)mock.MockInstance;
			Assertion.AssertEquals("world", blah.DoStuff("hello"));

			mock.Verify();
		}

		[Test] public void NamedDynamicMockImplementsAnInterface()
		{
			IMock mock = new DynamicMock(typeof(IBlah), "XBlah");

			mock.ExpectAndReturn("DoStuff", "world", "hello");

			IBlah blah = (IBlah)mock.MockInstance;
			Assertion.AssertEquals("world", blah.DoStuff("hello"));

			mock.Verify();
		}

		[Test] public void CanBeCustomisedByOverridingCallMethod()
		{
			IMock mock = new CustomMock(typeof(IBlah));
			IBlah blah = (IBlah)mock.MockInstance;
			Assertion.AssertEquals("CUSTOM", blah.DoStuff("hello"));
			mock.Verify();
		}

		[Test] public void StubbedMethodsCanBeCalledByOtherMethodsWithinObject()
		{
			DynamicMock mock = new DynamicMock(typeof(SameClass));
			mock.Ignore("A");
			mock.Ignore("c");
			SameClass sc = (SameClass)mock.MockInstance;

			mock.SetupResult("b", "hello");

			Assertion.AssertEquals("hello", sc.A());
		}

		[Test] public void CanSetStubsAndExpectationsOnMethodsInTheSameClass()
		{
			DynamicMock mock = new DynamicMock(typeof(SameClass));
			mock.Ignore("A");
			SameClass sc = (SameClass)mock.MockInstance;

			mock.ExpectAndReturn("c", true);
			mock.SetupResult("b", "hello");

			AssertEquals("Should have overriden B()", "hello", sc.A());

			mock.Verify();
		}

		[Test] public void MockInstanceCanBeUsedAsValueInAnExpectation()
		{
			DynamicMock mockThingy = new DynamicMock(typeof(Thingy));
			Thingy thingy = (Thingy)mockThingy.MockInstance;
			Mock m2 = new Mock("x");

			m2.Expect("y", thingy);
			m2.Invoke("y", new object[] { thingy }, new string[] { "NMock.DynamicMockTest.Thingy" });
			m2.Verify();
		}

		[Test] [ExpectedException(typeof(VerifyException))] public void ExpectationWillFailIfValueDoesntMatchMockInstance()
		{
			DynamicMock m1 = new DynamicMock(typeof(Thingy));
			Thingy thingy = (Thingy)m1.MockInstance;
			Mock m2 = new Mock("x");

			m2.Expect("y", thingy);
			m2.Invoke("y", new object[] { "something else" }, new string[] { "System.String" });
		}

		[Test] public void CanExpectMultipleInputsAndReturnAValue()
		{
			IMock mock = new DynamicMock(typeof(IValueType));
			ArrayList ret = new ArrayList();
			DateTime date = DateTime.Now;
			mock.ExpectAndReturn("Query", ret, "hello", date);

			IValueType blah = (IValueType)mock.MockInstance;
			Assertion.AssertEquals(ret, blah.Query("hello", date));

			mock.Verify();
		}

		[Test] public void CanMockMembersInheritedFromBaseInterfaces()
		{
			IMock mock = new DynamicMock(typeof(IBlah));
			mock.ExpectAndReturn("GetSuperString", "some string");

			IBlah b = (IBlah) mock.MockInstance;

			Assertion.AssertEquals("some string", b.GetSuperString());
		}

		[Test] public void CanMockMembersWithAParamsArgument()
		{
			IMock mock = new DynamicMock(typeof(IWithParams));
			mock.Expect("WithLeadingParameter", 1, new Object[] {1, 2, 3});

			IWithParams p = (IWithParams)mock.MockInstance;
			p.WithLeadingParameter(1, 1, 2, 3);
			mock.Verify();
		}
		[Test]
		[ExpectedException(typeof(MissingMethodException))]
		public void CannotYetMockMembersWithOnlyAParamsArgument()
		{
			IMock mock = new DynamicMock(typeof(IWithParams));
			mock.Expect("WithoutLeadingParameter", new Object[] {1, 2, 3});

			IWithParams p = (IWithParams)mock.MockInstance;
			p.WithoutLeadingParameter(1, 2, 3);
			mock.Verify();
		}
		[Test]
		public void CanMockOverloadedMethods()
		{
			IMock mock = new DynamicMock(typeof(IOverloadedMethods));
			mock.Expect("DoStuff", "one", 2);
			mock.Expect("DoStuff", "one");

			IOverloadedMethods instance = (IOverloadedMethods)mock.MockInstance;
			instance.DoStuff("one", 2);
			instance.DoStuff("one");
			mock.Verify();
		}
		[Test] public void CanMockMethodWithConstraint()
		{
			IMock mock = new DynamicMock(typeof(TwoMethods));
			mock.Expect("Foo", new StartsWith("Hello"));
			mock.ExpectAndReturn("Bar", 5, new NotNull());

			TwoMethods instance = (TwoMethods)mock.MockInstance;
			instance.Foo("Hello World");
			Assertion.AssertEquals("Should get a result", 5, instance.Bar("not null"));

			mock.Verify();
		}
		[Test]
		[ExpectedException(typeof(NotSupportedException))]
		public void CannotCreateMockInstanceWithNonEmptyConstructor()
		{
			IMock mock = new DynamicMock(typeof(WithNonEmptyConstructor));
			WithNonEmptyConstructor nonEmpty = (WithNonEmptyConstructor)mock.MockInstance;
		}

		[Test] public void CanSetAndGetPropertiesOnAMockedInterface()
		{
			DynamicMock mock = new DynamicMock(typeof(IWithProperty));
			IWithProperty withProperty = (IWithProperty)mock.MockInstance;

			mock.ExpectAndReturn("Name", "fred");
			mock.Expect("Name", "joe");

			AssertEquals("Should be property Name", "fred", withProperty.Name);
			withProperty.Name = "joe";

			mock.Verify();
		}

		[Ignore("SetupResult doesn't work for properties")]
		[Test] public void SetAndGetPropertiesDoesNotWorkWithSetupReturn()
		{
			DynamicMock mock = new DynamicMock(typeof(IWithProperty));
			IWithProperty withProperty = (IWithProperty)mock.MockInstance;

			mock.SetupResult("Name", "fred");
			mock.Expect("Name", "jim");

			AssertEquals("Should be property Name", "fred", withProperty.Name);
			withProperty.Name = "jim";

			mock.Verify();
		}

		[Test]
		public void MethodThatNeedsCastedReturnValue()
		{
			DynamicMock mock = new DynamicMock(typeof(ClassWithCastedReturnValue));

			short[] expectedResult = new short[] { 10 };
			mock.SetupResult("Method", expectedResult);

			ClassWithCastedReturnValue obj = (ClassWithCastedReturnValue)mock.MockInstance;
			short[] ret = (short[])obj.Method();

			Assertion.AssertEquals(expectedResult, ret);
		}

		[Test]
		public void MethodWithOutParamsFixedCall()
		{
			DynamicMock mock = new DynamicMock(typeof(IWithOutParam));

			mock.SetupResult("MethodWithOutParam", null, new string[] { "System.String", "System.Int32&" },
				new object[] { null, 4711 } );

			int ret = 0;
			IWithOutParam obj = (IWithOutParam)mock.MockInstance;
			obj.MethodWithOutParam("a", out ret);
			Assertion.AssertEquals(4711, ret);
			obj.MethodWithOutParam("a", out ret);
			Assertion.AssertEquals(4711, ret);
		}

		[Test]
		public void MethodWithOutParamsVariableCall()
		{
			DynamicMock mock = new DynamicMock(typeof(IWithOutParam));

			mock.ExpectAndReturn("MethodWithOutParam", null, null, new string[] { "System.String", "System.Int32&" },
				new object[] { null, 4711 } );
			mock.ExpectAndReturn("MethodWithOutParam", null, null, new string[] { "System.String", "System.Int32&" },
				new object[] { null, 4712 } );

			int ret = 0;
			IWithOutParam obj = (IWithOutParam)mock.MockInstance;
			obj.MethodWithOutParam("a", out ret);
			Assertion.AssertEquals(4711, ret);
			obj.MethodWithOutParam("b", out ret);
			Assertion.AssertEquals(4712, ret);
		}

		[Test]
		public void ReuseGeneratedAssembly()
		{
			DynamicMock mock = new DynamicMock(typeof(SameClass));
			mock.Ignore("A");
			mock.Ignore("c");
			SameClass sc = (SameClass)mock.MockInstance;

			mock.SetupResult("b", "hello");
			Assertion.AssertEquals("hello", sc.A());

			mock = new DynamicMock(typeof(SameClass));
			mock.Ignore("A");
			sc = (SameClass)mock.MockInstance;

			mock.ExpectAndReturn("c", true);
			mock.SetupResult("b", "hello");

			AssertEquals("Should have overriden B()", "hello", sc.A());
			mock.Verify();
		}
	}
}

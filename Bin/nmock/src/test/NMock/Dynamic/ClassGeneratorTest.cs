// Copyright c 2002, Joe Walnes, Chris Stevenson, Owen Rogers
// See LICENSE.txt for details.

using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;


using NUnit.Framework;
using NMock;
using NMock.Constraints;

namespace NMock.Dynamic
{
	#region types
	public interface IThingy
	{
		void NoArgs();
		void Another();
		void WithSimpleArg(string s);
		void WithTwoArgs(string a, string b);
		void WithThreeArgs(string a, string b, string c);
		void WithLotsOfArgs(string a, string b, string c, string d, string e, string f);
		void WithOtherArgs(int x, bool y, object o, IList list);
		void WithParams(int i, params string[] extra);
		void WithLongParam(long l);
		void WithIntParam(int i);
		object simpleReturn();
		string stringReturn();
		int intReturn();
		bool boolReturn();
		double doubleReturn();
		decimal decimalReturn();
		MyStruct structReturn();
		IThingy AThingy();
		MyEnum getEnum();
		string ReadProperty { get; }
		string WriteProperty { set; }
		string AProperty { get; set; }
		string get_SpecialProperty(int n);
		void set_SpecialProperty(string s, int n);
	}

	public struct MyStruct
	{
		public int x;
	}

	class X
	{
		private IMock mock = null;

		string stringReturn() { return (string)mock.Invoke("stringReturn", new object[0], new string[0]); }
		decimal decimalReturn() { return (decimal)mock.Invoke("decimalReturn", new object[0], new string[0]); }
		MyStruct structReturn() { return (MyStruct)mock.Invoke("structReturn", new object[0], new string[0]); }
	}

	public interface ISolidThingy
	{
		string NonVirtualProperty { get; }
		string OtherProperty { get; }
	}

	public abstract class AbstractThingy : ISolidThingy
	{
		// internal and protected internal methods must be overridable!
		public virtual string VirtualMethod() { return "xx"; }
		public abstract string AbstractMethod();
		protected internal virtual string ProtectedInternalMethod() { return "xx"; }

		// cannot override
		public static string StaticMethod()   { return "xx"; }
		public string NonVirtualMethod()       { return "xx"; }
		internal string NonVirtualInternalMethod() { return "xx"; }
		private string privateMethod()             { return "xx"; }
		protected virtual string protectedMethod() { return "xx"; }
		string defaultInternalMethod()             { return "xx"; }
		public override string ToString()     { return "xx"; }	// method is ignored

		// implemented interface members/methods are defined as final (ie. non-virtual)
		public string NonVirtualProperty
		{
			get { return "xx"; }
		}
		public virtual string OtherProperty
		{
			get { return "xx"; }
		}
	}

	public class RealThingy: AbstractThingy
	{
		public override string AbstractMethod()
		{ return "xx"; }
		public override string OtherProperty
		{
			get { return base.OtherProperty; }
		}
	}

	public class ClassWithInternalMethod
	{
		internal virtual string InternalMethod() { return "xx"; }
	}

	public enum MyEnum
	{
		A, B, C, D
	}
	public class ConcreteThing
	{
		public virtual void NoArgs() { Assertion.Fail("Should have been overriden"); }
	}
	public class ClassThatNeedsAdditionalReference
	{
		public virtual void Load(System.Data.SqlClient.SqlConnection con, int id)
		{
		}
	}
	public class ClassWithOutParams
	{
		public virtual int Test(out int a, out string s, out ClassWithOutParams c)
		{
			a = 0;
			s = string.Empty;
			c = new ClassWithOutParams();
			return 0;
		}
	}
	public interface InterfaceWithOutParams
	{
		int Test(out int a, out string s, out InterfaceWithOutParams c);
	}
	public class ClassWithRefParams
	{
		public virtual int Test(ref int a, ref string s, ref ClassWithRefParams c)
		{
			return 0;
		}
	}
	public interface InterfaceWithRefParams
	{
		int Test(ref int a, ref string s, ref InterfaceWithRefParams c);
	}
	#endregion

	[TestFixture] public class ClassGeneratorInvocationHandlerTest
	{
		public class InvocationHandlerImpl : IInvocationHandler
		{
			public string expectedMethodName;
			public bool wasCalled = false;

			public object Invoke(string methodName, object[] args, string[] typeNames)
			{
				Assertion.AssertEquals("should be method name", expectedMethodName, methodName);
				Assertion.AssertEquals("Should be no args", 0, args.Length);
				Assertion.AssertEquals("Should be no args", 0, typeNames.Length);
				wasCalled = true;
				return null;
			}
		}

		[Test] public void CreateGenericProxy()
		{
			InvocationHandlerImpl handler = new InvocationHandlerImpl();
			ClassGenerator cg = new ClassGenerator(typeof(IThingy), handler);
			IThingy thingy = (IThingy)cg.Generate();

			handler.expectedMethodName = "NoArgs";

			thingy.NoArgs();

			Assertion.Assert("Should have been called ", handler.wasCalled);
		}
	}

	[TestFixture] public class ClassGeneratorTest
	{
		private ClassGenerator cg;
		private IMock mock;
		private IThingy thingy;

		[SetUp] public void SetUp()
		{
			mock = new Mock("Test Mock");
			cg = new ClassGenerator(typeof(IThingy), mock);
			thingy = (IThingy)cg.Generate();
		}

		[Test] public void CallMethodIsCalled()
		{
			mock.Expect("NoArgs");
			thingy.NoArgs();
			mock.Verify();
		}

		[Test] public void CallMethodWithReturn()
		{
			object x = "sdfs";
			mock.ExpectAndReturn("simpleReturn", x);
			object result = thingy.simpleReturn();
			Assertion.AssertEquals(x, result);
			mock.Verify();
		}

		[Test] public void CallMethodWithReturnAndCast()
		{
			string x = "sdfs";
			mock.ExpectAndReturn("stringReturn", x);
			string result = thingy.stringReturn();
			Assertion.AssertEquals(x, result);
			mock.Verify();
		}

		[Test] public void CallMethodWithWeirdObjectReturn()
		{
			IThingy t = thingy;
			mock.ExpectAndReturn("AThingy", t);
			IThingy result = thingy.AThingy();
			Assertion.AssertEquals(thingy, result);
			mock.Verify();
		}

		[Test] public void CallMethodWithReturnInt()
		{
			mock.ExpectAndReturn("intReturn", 7);
			int result = thingy.intReturn();
			Assertion.AssertEquals(7, result);
			mock.Verify();
		}

		[Test] public void CallMethodWithReturnBoxings()
		{
			mock.ExpectAndReturn("boolReturn", true);
			mock.ExpectAndReturn("doubleReturn", 1234567891234E+10);
			Assertion.Assert(thingy.boolReturn());
			Assertion.AssertEquals(1234567891234E+10, thingy.doubleReturn());
			mock.Verify();
		}

		[Test]
		public void CallMethodWithReturnDecimal()
		{
			decimal d = new decimal(3);
			mock.ExpectAndReturn("decimalReturn", d);
			decimal result = thingy.decimalReturn();
			Assertion.AssertEquals(new decimal(3), result);
			mock.Verify();
		}

		[Test]
		public void CallMethodWithStruct()
		{
			MyStruct str = new MyStruct();
			str.x = 3;
			mock.ExpectAndReturn("structReturn", str);
			MyStruct result = thingy.structReturn();
			Assertion.AssertEquals(str, result);
			mock.Verify();
		}

		[Test] public void CallMethodWithReturnEnum()
		{
			mock.ExpectAndReturn("getEnum", MyEnum.C);
			MyEnum result = thingy.getEnum();
			Assertion.AssertEquals(MyEnum.C, result);
			mock.Verify();
		}

		[Test] [ExpectedException(typeof(System.IO.IOException))] public void CallMethodTheThrowsException()
		{
			mock.ExpectAndThrow("boolReturn", new System.IO.IOException());
			thingy.boolReturn();
		}

		[Test] public void CallMethodWithStringParameterExpectation()
		{
			mock.Expect("WithSimpleArg", new StartsWith("he"));
			thingy.WithSimpleArg("hello");
			mock.Verify();
		}

		[Test] public void CallMethodWithStringParameter()
		{
			mock.Expect("WithSimpleArg", "hello");
			thingy.WithSimpleArg("hello");
			mock.Verify();
		}

		[Test] public void CallMethodWithIntParameter()
		{
			mock.Expect("WithIntParam", 1);
			thingy.WithIntParam(1);
			mock.Verify();
		}
		[Test] [ExpectedException(typeof(VerifyException))] public void CallMethodWithParamExpectationsThatFails()
		{
			mock.Expect("WithSimpleArg", new IsEqual("hello"));
			thingy.WithSimpleArg("goodbye");
			mock.Verify();
		}

		[Test] public void CallMethodWithTwoParamExpectations()
		{
			mock.Expect("WithTwoArgs", new IsEqual("hello"), new IsEqual("world"));
			thingy.WithTwoArgs("hello", "world");
			mock.Verify();
		}

		[Test]
		[ExpectedException(typeof(VerifyException))]
		public void CallMethodWithTwoParamExpectationsThatFails()
		{
			mock.Expect("WithTwoArgs", new IsEqual("hello"), new IsEqual("world"));
			thingy.WithTwoArgs("hello", "moon");
			mock.Verify();
		}

		[Test] public void CallMethodWithThreeParamExpectations()
		{
			mock.Expect("WithThreeArgs", new IsEqual("hello"), new IsEqual("the"), new IsEqual("world"));
			thingy.WithThreeArgs("hello", "the", "world");
			mock.Verify();
		}

		[Test] public void CallMethodWithLotsOfArgsExpectations()
		{
			mock.Expect("WithLotsOfArgs", new IsEqual("hello"), new IsEqual("world"), new IsEqual("is"), new IsEqual("this"), new IsEqual("the"), new IsEqual("end"));
			thingy.WithLotsOfArgs("hello", "world", "is", "this", "the", "end");
			mock.Verify();
		}

		[Test] public void CallMethodWithOtherArgs()
		{
			IList l = new ArrayList();
			mock.Expect("WithOtherArgs", new IsEqual(6), new IsEqual(true), new IsNull(), new IsEqual(l));
			thingy.WithOtherArgs(6, true, null, l);
			mock.Verify();
		}
		[Test] public void CallMethodWithVariableNumberOfParams()
		{
			mock.Expect("WithParams", 1, new object[]{"string1", "string2"});
			thingy.WithParams(1, "string1", "string2");
			mock.Verify();
		}
		[Test] public void CallMethodWithLongParam()
		{
			mock.Expect("WithLongParam", 5L);
			thingy.WithLongParam(5);
			mock.Verify();
		}

		[Test] public void CallReadOnlyProperty()
		{
			mock.ExpectAndReturn("ReadProperty", "hello");
			mock.ExpectAndReturn("ReadProperty", "world");
			Assertion.AssertEquals("hello", thingy.ReadProperty);
			Assertion.AssertEquals("world", thingy.ReadProperty);
			mock.Verify();
		}

		[Test] public void WriteOnlyPropertyExpectations()
		{
			mock.Expect("WriteProperty", "hello");
			mock.Expect("WriteProperty", "world");
			thingy.WriteProperty = "hello";
			thingy.WriteProperty = "world";
			mock.Verify();
		}

		[Test] public void ReadAndWriteProperty()
		{
			mock.Expect("AProperty", "hello");
			mock.Expect("AProperty", "world");
			mock.ExpectAndReturn("AProperty", "good");
			mock.ExpectAndReturn("AProperty", "bye");
			thingy.AProperty = "hello";
			thingy.AProperty = "world";
			Assertion.AssertEquals("good", thingy.AProperty);
			Assertion.AssertEquals("bye", thingy.AProperty);
			mock.Verify();
		}

		[Test]
		public void ReadAndWriteSpecialProperty()
		{
			mock.Expect("set_SpecialProperty", "hello", 1);
			mock.Expect("set_SpecialProperty", "world", 2);
			mock.ExpectAndReturn("get_SpecialProperty", "good", 3);
			mock.ExpectAndReturn("get_SpecialProperty", "bye", 4);
			thingy.set_SpecialProperty("hello", 1);
			thingy.set_SpecialProperty("world", 2);
			Assertion.AssertEquals("good", thingy.get_SpecialProperty(3));
			Assertion.AssertEquals("bye", thingy.get_SpecialProperty(4));
			mock.Verify();
		}


		[Test] public void CanExtendAbstractClass()
		{
			cg = new ClassGenerator(typeof(AbstractThingy), mock);
			AbstractThingy s = (AbstractThingy)cg.Generate();

			mock.ExpectAndReturn("VirtualMethod", "hello");
			mock.ExpectAndReturn("GetHashCode", 123);
			mock.ExpectAndReturn("AbstractMethod", "fish");
			mock.ExpectAndReturn("ProtectedInternalMethod", "white");

			Assertion.AssertEquals("hello", s.VirtualMethod());
			Assertion.AssertEquals(123, s.GetHashCode());
			Assertion.AssertEquals("fish", s.AbstractMethod());
			Assertion.AssertEquals("white", s.ProtectedInternalMethod());

			mock.Verify();
		}

		[Test]
		public void CanExtendDerivedClass()
		{
			cg = new ClassGenerator(typeof(RealThingy), mock);
			RealThingy s = (RealThingy)cg.Generate();

			mock.ExpectAndReturn("VirtualMethod", "hello");
			mock.ExpectAndReturn("GetHashCode", 123);
			mock.ExpectAndReturn("AbstractMethod", "fish");
			mock.ExpectAndReturn("ProtectedInternalMethod", "white");
			mock.ExpectAndReturn("OtherProperty", "abc");

			Assertion.AssertEquals("hello", s.VirtualMethod());
			Assertion.AssertEquals(123, s.GetHashCode());
			Assertion.AssertEquals("fish", s.AbstractMethod());
			Assertion.AssertEquals("white", s.ProtectedInternalMethod());
			Assertion.AssertEquals("abc", s.OtherProperty);

			mock.Verify();
		}

		[Test] public void CanExtendConcreteClass()
		{
			ConcreteThing concrete = (ConcreteThing)(new ClassGenerator(typeof(ConcreteThing), mock)).Generate();

			mock.Expect("NoArgs");
			concrete.NoArgs();
			mock.Verify();
		}

		// Limitations
		[Test] public void CannotOverrideNonVirtualFeatures()
		{
			cg = new ClassGenerator(typeof(AbstractThingy), mock);
			AbstractThingy s = (AbstractThingy)cg.Generate();

			mock.SetupResult("NonVirtualMethod", "non virtual method");
			mock.SetupResult("NonVirtualProperty", "non virtual property");

			Assertion.AssertEquals("xx", s.NonVirtualMethod());
			Assertion.AssertEquals("xx", s.NonVirtualProperty);
			mock.Verify();
		}

		[Test] public void DoesNotOverrideToString()
		{
			cg = new ClassGenerator(typeof(AbstractThingy), mock);
			AbstractThingy s = (AbstractThingy)cg.Generate();

			mock.SetupResult("ToString", "to string");

			Assertion.AssertEquals("xx", s.ToString());
			mock.Verify();
		}

		[Test] public void IgnoresInternalMethodsBecauseOfAssemblyVisibility()
		{
			ArrayList methodsToIgnore = new ArrayList();
			new ClassGenerator(typeof(ClassWithInternalMethod), mock, methodsToIgnore).Generate();
			Assertion.Assert("Should include InternalMethod", methodsToIgnore.Contains("InternalMethod"));
		}

		[Test]
		public void ClassNeedsAdditionalReferences()
		{
			ArrayList methodsToIgnore = new ArrayList();
			new ClassGenerator(typeof(ClassThatNeedsAdditionalReference),
				mock, methodsToIgnore).Generate();
		}
		[Test]
		public void ClassWithOutParams()
		{
			ArrayList methodsToIgnore = new ArrayList();
			new ClassGenerator(typeof(ClassWithOutParams),
				mock, methodsToIgnore).Generate();
		}
		[Test]
		public void InterfaceWithOutParams()
		{
			ArrayList methodsToIgnore = new ArrayList();
			new ClassGenerator(typeof(InterfaceWithOutParams),
				mock, methodsToIgnore).Generate();
		}
		[Test]
		public void ClassWithRefParams()
		{
			ArrayList methodsToIgnore = new ArrayList();
			new ClassGenerator(typeof(ClassWithRefParams),
				mock, methodsToIgnore).Generate();
		}
		[Test]
		public void InterfaceWithRefParams()
		{
			ArrayList methodsToIgnore = new ArrayList();
			new ClassGenerator(typeof(InterfaceWithRefParams),
				mock, methodsToIgnore).Generate();
		}

		[Test]
		public void WindowsForm()
		{
			ArrayList methodsToIgnore = new ArrayList();
			new ClassGenerator(typeof(System.Windows.Forms.Form),
				mock, methodsToIgnore).Generate();
		}

		[Test]
		public void InterfaceThatEndsWithProperty()
		{
			ArrayList methodsToIgnore = new ArrayList();
			new ClassGenerator(typeof(ISolidThingy),
				mock, methodsToIgnore).Generate();
		}
	}
}

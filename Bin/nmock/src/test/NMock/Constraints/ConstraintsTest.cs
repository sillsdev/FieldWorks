// Copyright c 2002, Joe Walnes, Chris Stevenson, Owen Rogers
// See LICENSE.txt for details.

using NUnit.Framework;
using System.Text.RegularExpressions;
using System.Collections;
namespace NMock.Constraints
{

	[TestFixture]
	public class ConstraintsTest
	{

		private IConstraint c;

		[Test]
		public void IsNull()
		{
			c = new IsNull();
			Assertion.Assert(c.Eval(null));
			Assertion.Assert(!c.Eval(new object()));
			Assertion.Assert(!c.Eval(1));
			Assertion.Assert(!c.Eval(true));
			Assertion.Assert(!c.Eval(false));
		}

		[Test]
		public void NotNull()
		{
			c = new NotNull();
			Assertion.Assert(!c.Eval(null));
			Assertion.Assert(c.Eval(new object()));
		}

		[Test]
		public void IsEqual()
		{
			object o1 = new object();
			object o2 = new object();
			c = new IsEqual(o1);
			Assertion.Assert(c.Eval(o1));
			Assertion.Assert(!c.Eval(o2));
			Assertion.Assert(!c.Eval(null));

			int i1 = 1;
			int i2 = 2;
			c = new IsEqual(i1);
			Assertion.Assert(c.Eval(i1));
			Assertion.Assert(c.Eval(1));
			Assertion.Assert(!c.Eval(i2));
			Assertion.Assert(!c.Eval(2));
		}

		[Test]
		public void IsEqualWhenArray()
		{
			object[] o1 = new object[] { 1, 2, 3 };
			object[] o2 = new object[] { 1, 2, 4 };

			c = new IsEqual(new object[] { 1, 2, 3 });
			Assertion.Assert("should be equal", c.Eval(o1));
			Assertion.Assert("shouldn't be equal", !c.Eval(o2));
			Assertion.Assert("it ain't null", !c.Eval(null));
		}

		[Test]
		public void NotEqual()
		{
			object o1 = new object();
			object o2 = new object();
			c = new NotEqual(o1);
			Assertion.Assert(!c.Eval(o1));
			Assertion.Assert(c.Eval(o2));
			Assertion.Assert(c.Eval(null));

			int i1 = 1;
			int i2 = 2;
			c = new NotEqual(i1);
			Assertion.Assert(!c.Eval(i1));
			Assertion.Assert(!c.Eval(1));
			Assertion.Assert(c.Eval(i2));
			Assertion.Assert(c.Eval(2));
		}

		[Test]
		public void IsAnything()
		{
			c = new IsAnything();
			Assertion.Assert(c.Eval(null));
			Assertion.Assert(c.Eval(0));
			Assertion.Assert(c.Eval(99));
			Assertion.Assert(c.Eval(-2));
			Assertion.Assert(c.Eval(true));
			Assertion.Assert(c.Eval(false));
			Assertion.Assert(c.Eval(""));
			Assertion.Assert(c.Eval("hello"));
			Assertion.Assert(c.Eval(new object()));
		}

		[Test]
		public void IsType()
		{
			c = new IsTypeOf(typeof(System.IO.TextReader));
			Assertion.Assert(!c.Eval(null));
			Assertion.Assert(c.Eval(new System.IO.StringReader("")));
			Assertion.Assert(!c.Eval(new System.IO.StringWriter()));
		}

		[Test]
		public void Not()
		{
			Assertion.Assert(new Not(new False()).Eval(null));
			Assertion.Assert(!new Not(new True()).Eval(null));
		}

		[Test]
		public void And()
		{
			Assertion.Assert( new And(new True() , new True() ).Eval(null));
			Assertion.Assert(!new And(new True() , new False()).Eval(null));
			Assertion.Assert(!new And(new False(), new True() ).Eval(null));
			Assertion.Assert(!new And(new False(), new False()).Eval(null));
		}

		[Test]
		public void Or()
		{
			Assertion.Assert( new Or(new True() , new True() ).Eval(null));
			Assertion.Assert( new Or(new True() , new False()).Eval(null));
			Assertion.Assert( new Or(new False(), new True() ).Eval(null));
			Assertion.Assert(!new Or(new False(), new False()).Eval(null));
		}

		[Test]
		public void IsIn()
		{
			c = new IsIn(2, 3, 5);
			Assertion.Assert(!c.Eval(1));
			Assertion.Assert(c.Eval(2));
			Assertion.Assert(c.Eval(3));
			Assertion.Assert(!c.Eval(4));
			Assertion.Assert(c.Eval(5));
			Assertion.Assert(!c.Eval(6));
			Assertion.Assert(!c.Eval(null));

			int[] array = {1, 2};
			c = new IsIn(array);
			Assertion.Assert(c.Eval(1));
			Assertion.Assert(c.Eval(2));
			Assertion.Assert(!c.Eval(3));
		}

		[Test]
		public void NotIn()
		{
			c = new NotIn(1, 2);
			Assertion.Assert(!c.Eval(1));
			Assertion.Assert(!c.Eval(2));
			Assertion.Assert(c.Eval(3));

			int[] array = {1, 2};
			c = new NotIn(array);
			Assertion.Assert(!c.Eval(1));
			Assertion.Assert(!c.Eval(2));
			Assertion.Assert(c.Eval(3));
		}

		[Test]
		public void IsEqualIgnoreCase()
		{
			c = new IsEqualIgnoreCase("heLLo");
			Assertion.Assert(c.Eval("HELLO"));
			Assertion.Assert(c.Eval("hello"));
			Assertion.Assert(c.Eval("HelLo"));
			Assertion.Assert(!c.Eval("abcde"));
		}

		[Test]
		public void StripSpace()
		{
			Assertion.AssertEquals("Hello World", IsEqualIgnoreWhiteSpace.StripSpace("Hello\n  \n World"));
			Assertion.AssertEquals("Hello World", IsEqualIgnoreWhiteSpace.StripSpace(" Hello World "));
			Assertion.AssertEquals("", IsEqualIgnoreWhiteSpace.StripSpace("  "));
		}

		[Test]
		public void TestIsEqualIgnoreWhiteSpace()
		{
			c = new IsEqualIgnoreWhiteSpace("Hello World   how\n are we?");
			Assertion.Assert(c.Eval("Hello World how are we?"));
			Assertion.Assert(c.Eval("   Hello World   how are \n\n\twe?"));
			Assertion.Assert(!c.Eval("HelloWorld how are we?"));
			Assertion.Assert(!c.Eval("Hello World how are we"));
		}

		[Test]
		public void IsMatch()
		{
			c = new IsMatch(new Regex(@"^th[aeiou]\w* .*$"));
			Assertion.Assert(c.Eval("the world"));
			Assertion.Assert(!c.Eval("theworld"));
			Assertion.Assert(!c.Eval("ThE world"));
			Assertion.Assert(!c.Eval(" the world"));
			Assertion.Assert(c.Eval("thats nice"));
			Assertion.Assert(!c.Eval(new object()));
			Assertion.Assert(!c.Eval(null));

			c = new IsMatch(@"^th[aeiou]\w* .*$");
			Assertion.Assert(c.Eval("the world"));
			Assertion.Assert(!c.Eval("theworld"));
			Assertion.Assert(!c.Eval("ThE world"));

			c = new IsMatch(@"^th[aeiou]\w* .*$", false);
			Assertion.Assert(c.Eval("the world"));
			Assertion.Assert(!c.Eval("theworld"));
			Assertion.Assert(!c.Eval("ThE world"));

			c = new IsMatch(@"^th[aeiou]\w* .*$", true);
			Assertion.Assert(c.Eval("the world"));
			Assertion.Assert(!c.Eval("theworld"));
			Assertion.Assert(c.Eval("ThE world"));

		}

		[Test]
		public void IsCloseTo()
		{
			c = new IsCloseTo(1.0, 0.5);

			Assertion.Assert(c.Eval(1.0));
			Assertion.Assert(c.Eval(0.5));
			Assertion.Assert(c.Eval(1.5));

			Assertion.Assert(c.Eval(1));
			Assertion.Assert(c.Eval(0.5f));
			Assertion.Assert(c.Eval(new decimal(1.5)));

			Assertion.Assert(!c.Eval(0.49));
			Assertion.Assert(!c.Eval(1.51));
			Assertion.Assert(!c.Eval(-1.0));

			Assertion.Assert(c.Eval("1.2"));
			Assertion.Assert(!c.Eval("0.2"));
			Assertion.Assert(!c.Eval("hello"));

			Assertion.Assert(!c.Eval(null));
			Assertion.Assert(!c.Eval(0));
			Assertion.Assert(!c.Eval(0.0));
		}

		#region Property Tests
		[Test]
		public void PropertyIs()
		{
			ThingWithProps t = new ThingWithProps();

			// test property equals a value
			Assertion.Assert(new PropertyIs("MyProp", "hello").Eval(t));
			Assertion.Assert(!new PropertyIs("MyProp", "bye").Eval(t));

			// test property using another constraint
			Assertion.Assert(new PropertyIs("MyProp", new IsMatch("ell")).Eval(t));
			Assertion.Assert(!new PropertyIs("MyProp", new IsMatch("sfsl")).Eval(t));

			Assertion.AssertEquals(
				"Property MyProp: <x>",
				new PropertyIs("MyProp", new IsEqual("x")).Message);
		}

		[Test]
		public void PropertyIsWithNullValue()
		{
			Assertion.Assert(!new PropertyIs("Blah", new IsAnything()).Eval(null));
		}

		[Test]
		public void PropertyIsWithNestedProperties()
		{
			ThingWithProps t = new ThingWithProps();

			Assertion.Assert(new PropertyIs("MyProp.Length", 5).Eval(t));
			Assertion.Assert(!new PropertyIs("MyProp.Length", 9).Eval(t));
		}

		class ThingWithProps
		{
			public string MyProp
			{
				get { return "hello"; }
			}
		}
		#endregion

		[Test]
		public void CollectingConstraint()
		{
			CollectingConstraint c = new CollectingConstraint();
			Assertion.Assert(c.Eval("test"));
			Assertion.AssertEquals("test", c.Parameter);

			Assertion.Assert("eval should always be true", c.Eval(null));
			Assertion.AssertNull(c.Parameter);

		}

		[Test]
		public void Delegate()
		{
			c = new Constraint(new Constraint.Method(myEval));
			myFlag = false;
			Assertion.Assert(c.Eval(null));
			Assertion.Assert(!c.Eval(null));
			Assertion.Assert(c.Eval(null));
			Assertion.Assert(!c.Eval(null));
		}

		[Test]
		public void Messages()
		{
			Assertion.AssertEquals("null", new IsNull().Message);
			Assertion.AssertEquals("", new IsAnything().Message);
			Assertion.AssertEquals("IN <hi>, <1>, <bye>", new IsIn("hi", 1, "bye").Message);
			Assertion.AssertEquals("<hi>", new IsEqual("hi").Message);
			Assertion.AssertEquals("typeof <System.String>", new IsTypeOf(typeof(System.String)).Message);
			Assertion.AssertEquals("NOT DUMMY", new Not(new Dummy()).Message);
			Assertion.AssertEquals("DUMMY AND DUMMY", new And(new Dummy(), new Dummy()).Message);
			Assertion.AssertEquals("DUMMY OR DUMMY", new Or(new Dummy(), new Dummy()).Message);
			Assertion.AssertEquals("NOT null", new NotNull().Message);
			Assertion.AssertEquals("NOT <hi>", new NotEqual("hi").Message);
			Assertion.AssertEquals("NOT IN <hi>, <1>, <bye>", new NotIn("hi", 1, "bye").Message);
			Assertion.AssertEquals("<hi>", new IsEqualIgnoreCase("hi").Message);
			Assertion.AssertEquals("<hi>", new IsEqualIgnoreWhiteSpace("hi").Message);
			Assertion.AssertEquals("<hi>", new IsMatch("hi").Message);
			Assertion.AssertEquals("<7>", new IsCloseTo(7.0, 1.1).Message);
			Assertion.AssertEquals("Custom Constraint", new Constraint(new Constraint.Method(myEval)).Message);
		}
		private bool myFlag;

		private bool myEval(object val)
		{
			myFlag = !myFlag;
			return myFlag;
		}

		class True : IConstraint
		{
			public bool Eval(object val)
			{
				return true;
			}

			public string Message
			{
				get { return null; }
			}

			public object ExtractActualValue(object actual)
			{
				return actual;
			}
		}

		class False : IConstraint
		{
			public bool Eval(object val)
			{
				return false;
			}

			public string Message
			{
				get { return null; }
			}

			public object ExtractActualValue(object actual)
			{
				return actual;
			}
		}

		class Dummy : IConstraint
		{
			public bool Eval(object val)
			{
				return false;
			}

			public string Message
			{
				get { return "DUMMY"; }
			}

			public object ExtractActualValue(object actual)
			{
				return actual;
			}
		}

		#region ExtractActualValue test
		class ExtractingConstraint : IsEqual
		{
			public ExtractingConstraint(object expected) : base(expected) {}


			public override object ExtractActualValue(object actual)
			{
				return ((string)actual)[0].ToString();
			}
		}

		[Test]
		public void ExtractingActualValue()
		{
			ExtractingConstraint constraint = new ExtractingConstraint("E");

			Assertion.AssertEquals("Should be modified value", "A", constraint.ExtractActualValue("ACTUAL"));
			Assertion.Assert("Should match", constraint.Eval("EQUALS FIRST CHAR"));
			Assertion.Assert("Should be different", new Not(constraint).Eval("NOT EQUAL"));
		}
		#endregion
	}

}

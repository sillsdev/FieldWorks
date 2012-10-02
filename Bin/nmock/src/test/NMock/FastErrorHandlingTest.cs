using NUnit.Framework;
using System;

namespace NMock
{

	/// <summary>
	/// looking at the code, I think fixing this for one case (SetupResult or Expect)
	/// should fix it in all places (at least that's the intent)
	///
	/// I didn't use ExpectException because I wanted to specify the error message
	/// </summary>
	[TestFixture]
	public class FastErrorHandlingTest : Assertion
	{
		public class Empty
		{
		}

		public class Full
		{
			public virtual string Foo()
			{
				return "foo";
			}

			public string Bar(string s)
			{
				return "bar";
			}
		}

		public class OtherFull
		{
			public virtual string Foo
			{
				get { return "foo"; }
			}

			public string Bar
			{
				get { return "bar"; }
			}
		}

		private IMock empty;
		private IMock full;

		[SetUp]
		public void SetUp()
		{
			empty = new DynamicMock(typeof(Empty));
			full = new DynamicMock(typeof(Full));
		}

		[Test]
		public void ExpectWithMissingMethod()
		{
			try
			{
				empty.Expect("Foo");
				Fail();
			}
			catch (MissingMethodException e)
			{
				AssertEquals("method <Foo> not defined", e.Message);
			}
		}

		[Test]
		[ExpectedException(typeof(MissingMethodException))]
		public void ExpectAndReturnWithMissingMethod()
		{
			full.ExpectAndReturn("xxx", null);
		}

		[Test]
		public void SetupResultWithWrongType()
		{
			try
			{
				full.SetupResult("Foo", true);
				Fail();
			}
			catch (ArgumentException e)
			{
				AssertEquals("method <Foo> returns a System.String", e.Message);
			}
		}

		[Test]
		public void FailWhenMockedMethodNotVirtual()
		{
			try
			{
				full.Expect("Bar", "test");
				Fail();
			}
			catch(ArgumentException e)
			{
				AssertEquals("Method <Bar> is not virtual", e.Message);
			}
		}

		[Test]
		public void FailWhenMockedPropertyNotVirtual()
		{
			IMock otherFull = new DynamicMock(typeof(OtherFull));

			try
			{
				otherFull.Expect("Bar", "test");
				Fail();
			}
			catch(ArgumentException e)
			{
				AssertEquals("Property <Bar> is not virtual", e.Message);
			}
		}
	}

}

// Contributed by Luke Maxon <ltmaxon@thoughtworks.com>

using System;
using System.Collections;
using NUnit.Framework;

namespace NMock.Constraints
{

	[TestFixture]
	public class IsArrayEqualTest
	{
		[Test]
		public void NotObjectArrays()
		{
			object o1 = new object();
			object[] o2 = new object[] {};

			IsListEqual c = new IsListEqual(o1);
			Assertion.Assert(!c.Eval(o2));
			Assertion.Assert(!c.Eval(o1));
			Assertion.Assert(!c.Eval(null));
		}

		[Test]
		public void EmptyArrayEqual()
		{
			object[] o1 = new object[] {};
			object[] o2 = new object[] {};

			IsListEqual c = new IsListEqual(o1);
			Assertion.Assert(c.Eval(o2));
			Assertion.Assert(c.Eval(o1));
			Assertion.Assert(!c.Eval(null));
		}

		[Test]
		public void DifferentSizeEqual()
		{
			object[] o1 = new object[1] {"foo"};
			object[] o2 = new object[2] {"foo", "bar"};

			IsListEqual c = new IsListEqual(o1);
			Assertion.Assert(!c.Eval(o2));
		}

		[Test]
		public void EqualSingleElement()
		{
			object[] o1 = new object[1] {"foo"};
			object[] o2 = new object[1] {"foo"};

			IsListEqual c = new IsListEqual(o1);
			Assertion.Assert(c.Eval(o2));
		}

		[Test]
		public void NotEqualSingleElement()
		{
			object[] o1 = new object[1] {"foo"};
			object[] o2 = new object[1] {"bar"};

			IsListEqual c = new IsListEqual(o1);
			Assertion.Assert(!c.Eval(o2));
		}

		[Test]
		public void EqualMultipleElements()
		{
			object[] o1 = new object[2] {"foo", "bar"};
			object[] o2 = new object[2] {"foo", "bar"};

			IsListEqual c = new IsListEqual(o1);
			Assertion.Assert(c.Eval(o2));
		}

		[Test]
		public void NotEqualMultipleElements()
		{
			object[] o1 = new object[2] {"foo", "bar"};
			object[] o2 = new object[2] {"foo", "baz"};

			IsListEqual c = new IsListEqual(o1);
			Assertion.Assert(!c.Eval(o2));
		}
	}

	[TestFixture]
	public class IsListEqualTest
	{
		[Test]
		public void NotIList()
		{
			object o1 = new object();
			IList o2 = new ArrayList();

			IsListEqual c = new IsListEqual(o1);
			Assertion.Assert(!c.Eval(o2));
			Assertion.Assert(!c.Eval(o1));
			Assertion.Assert(!c.Eval(null));
		}

		[Test]
		public void EmptyIListEqual()
		{
			IList o1 = new ArrayList();
			IList o2 = new ArrayList();

			IsListEqual c = new IsListEqual(o1);
			Assertion.Assert(c.Eval(o2));
			Assertion.Assert(c.Eval(o1));
			Assertion.Assert(!c.Eval(null));
		}

		[Test]
		public void DifferentSizeEqual()
		{
			IList o1 = new ArrayList(new object[] {"foo"});
			IList o2 = new ArrayList(new object[2] {"foo", "bar"});

			IsListEqual c = new IsListEqual(o1);
			Assertion.Assert(!c.Eval(o2));
		}

		[Test]
		public void EqualSingleElement()
		{
			IList o1 = new ArrayList(new object[1] {"foo"});
			IList o2 = new ArrayList(new object[1] {"foo"});

			IsListEqual c = new IsListEqual(o1);
			Assertion.Assert(c.Eval(o2));
		}

		[Test]
		public void NotEqualSingleElement()
		{
			IList o1 = new ArrayList(new object[1] {"foo"});
			IList o2 = new ArrayList(new object[1] {"bar"});

			IsListEqual c = new IsListEqual(o1);
			Assertion.Assert(!c.Eval(o2));
		}

		[Test]
		public void EqualMultipleElements()
		{
			IList o1 = new ArrayList(new object[2] {"foo", "bar"});
			IList o2 = new ArrayList(new object[2] {"foo", "bar"});

			IsListEqual c = new IsListEqual(o1);
			Assertion.Assert(c.Eval(o2));
		}

		[Test]
		public void NotEqualMultipleElements()
		{
			IList o1 = new ArrayList(new object[2] {"foo", "bar"});
			IList o2 = new ArrayList(new object[2] {"foo", "baz"});

			IsListEqual c = new IsListEqual(o1);
			Assertion.Assert(!c.Eval(o2));
		}
	}

}
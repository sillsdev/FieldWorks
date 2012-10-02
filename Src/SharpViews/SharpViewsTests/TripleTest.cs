using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace SIL.FieldWorks.SharpViews.SharpViewsTests
{
	[TestFixture]
	public class TripleTest: SIL.FieldWorks.Test.TestUtils.BaseTest
	{
		[Test]
		public void TripleAsKey()
		{
			Triple<int, string, int> key1 = new Triple<int, string, int>(3, "foo", 6);
			Triple<int, string, int> key1b = new Triple<int, string, int>(3, "foo", 6);
			Triple<int, string, int> key2 = new Triple<int, string, int>(4, "foo", 6);
			Triple<int, string, int> key3 = new Triple<int, string, int>(3, "bar", 6);
			Triple<int, string, int> key4 = new Triple<int, string, int>(3, "foo", 7);

			Dictionary<Triple<int, string, int>, int> test = new Dictionary<Triple<int, string, int>, int>();
			test[key1] = 5;
			test[key2] = 6;
			test[key3] = 7;
			test[key4] = 12;
			Assert.AreEqual(5, test[key1], "retrieve a value");
			Assert.AreEqual(6, test[key2], "retrieve with first different");
			Assert.AreEqual(7, test[key3], "retrieve with second different");
			Assert.AreEqual(12, test[key4], "retrieve with third different");
			Assert.AreEqual(5, test[key1b], "retrieve a value with equal but not RefEqual key");
		}
	}
}

using System;
using NUnit.Framework;

namespace NMock
{

	[TestFixture]
	public class VerifyExceptionTest
	{

		[Test]
		public void Message()
		{
			VerifyException e = new VerifyException("Boo", "<Wee>", 44);
			Assertion.AssertEquals("Boo\nexpected:<Wee>\n but was:<44>", e.Message);
			Assertion.AssertEquals("Boo", e.Reason);
			Assertion.AssertEquals("<Wee>", e.Expected);
			Assertion.AssertEquals(44,    e.Actual);
		}

	}

}
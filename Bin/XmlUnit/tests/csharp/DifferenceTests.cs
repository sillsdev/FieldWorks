namespace XmlUnit.Tests {
	using System;
	using NUnit.Framework;
	using XmlUnit;

	[TestFixture]
	public class DifferenceTests {
		private Difference minorDifference;

		[SetUp] public void CreateMinorDifference() {
			DifferenceType id = DifferenceType.ATTR_SEQUENCE_ID;
			Assertion.AssertEquals(false, Differences.isMajorDifference(id));
			minorDifference = new Difference(id);
		}

		[Test] public void ToStringContainsId() {
			string commentDifference = minorDifference.ToString();
			string idValue = "type: " + (int)DifferenceType.ATTR_SEQUENCE_ID;
			Assertion.AssertEquals("contains " + idValue,
								   true,
								   commentDifference.IndexOfAny(idValue.ToCharArray()) > 0);
		}
	}
}

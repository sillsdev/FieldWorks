// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: PunctuationDlgTests.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------

using NUnit.Framework;
using SIL.FieldWorks.Test.TestUtils;
using SILUBS.SharedScrUtils;
using SIL.Utils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// Tests for the PunctuationDlg.
	/// </summary>
	[TestFixture]
	public class PunctuationDlgTests : BaseTest
	{
		/// <summary>
		/// Verifies that matched pairs validation works for valid entries.
		/// </summary>
		[Test]
		public void ValidateMatchedPairs_ValidEntries()
		{
			using (var punctDlg = new PunctuationDlg())
			{
				var pairList = new MatchedPairList();
				var pair = new MatchedPair { Open = "(", Close = ")" };
				pairList.Add(pair);

				bool result = ReflectionHelper.GetBoolResult(punctDlg, "ValidateMatchedPairs",
					new object[] { pairList, false });
				Assert.IsTrue(result);
			}
		}

		/// <summary>
		/// Verifies that matched pairs validation rejects invalid entries.
		/// </summary>
		[Test]
		public void ValidateMatchedPairs_InvalidEntries()
		{
			using (var punctDlg = new PunctuationDlg())
			{
				var pairList = new MatchedPairList();
				var pair = new MatchedPair { Close = ")" };
				pairList.Add(pair);

				bool result = ReflectionHelper.GetBoolResult(punctDlg, "ValidateMatchedPairs",
				new object[] { pairList, false });
				Assert.IsFalse(result);
			}
		}
	}
}

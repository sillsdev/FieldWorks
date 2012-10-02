// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009 SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: PunctuationDlgTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using NUnit.Framework;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// Tests for the PunctuationDlg.
	/// </summary>
	[TestFixture]
	public class PunctuationDlgTests
	{
		/// <summary>
		/// Verifies that matched pairs validation works for valid entries.
		/// </summary>
		[Test]
		public void ValidateMatchedPairs_ValidEntries()
		{
			PunctuationDlg punctDlg = new PunctuationDlg();
			MatchedPairList pairList = new MatchedPairList();
			MatchedPair pair = new MatchedPair();
			pair.Open = "(";
			pair.Close = ")";
			pairList.Add(pair);

			bool result = ReflectionHelper.GetBoolResult(punctDlg, "ValidateMatchedPairs",
				new object[] { pairList, false });
			Assert.IsTrue(result);
		}

		/// <summary>
		/// Verifies that matched pairs validation rejects invalid entries.
		/// </summary>
		[Test]
		public void ValidateMatchedPairs_InvalidEntries()
		{
			PunctuationDlg punctDlg = new PunctuationDlg();
			MatchedPairList pairList = new MatchedPairList();
			MatchedPair pair = new MatchedPair();
			pair.Close = ")";
			pairList.Add(pair);

			bool result = ReflectionHelper.GetBoolResult(punctDlg, "ValidateMatchedPairs",
				new object[] {pairList, false});
			Assert.IsFalse(result);
		}
	}
}

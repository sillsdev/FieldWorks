// --------------------------------------------------------------------------------------------
#region /// Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: HfModifyDlgVcTests.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.Common.PrintLayout
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests the header/footer view constructor
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class HfModifyDlgVcTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private HFModifyDlgVC m_vc;

		public override void FixtureSetup()
		{
			base.FixtureSetup();

			m_vc = new HFModifyDlgVC(new HFDialogsPageInfo(true), Cache.DefaultVernWs, DateTime.Now, Cache);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a TsString to use as a page number label
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestPageNumber()
		{
			ITsString tssPageLabel = m_vc.PageNumber;
			Assert.AreEqual("[page]", tssPageLabel.Text);
			Assert.AreEqual(2, tssPageLabel.get_Properties(0).IntPropCount);
			Assert.AreEqual(0, tssPageLabel.get_Properties(0).StrPropCount);
			int var;
			Assert.AreEqual(Cache.DefaultUserWs, tssPageLabel.get_Properties(0).GetIntPropValues((int)FwTextPropType.ktptWs, out var));
		}
	}
}

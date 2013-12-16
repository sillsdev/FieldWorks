// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: HfModifyDlgVcTests.cs
// Responsibility: TE Team

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

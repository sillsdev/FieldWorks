// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwStyleSheetTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.FDO.Cellar;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FwStyleSheetTests : InMemoryFdoTestBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to make the test data for the tests
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			base.CreateTestData();
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Test that the StyleCollection class works correct with composed and decomposed
		/// form of strings (TE-6090)
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void StyleCollectionWorksWithUpperAscii()
		{
			string styleName = "\u00e1bc";
			StStyle style = new StStyle();
			Cache.LangProject.StylesOC.Add(style);
			style.Name = styleName;

			FwStyleSheet.StyleInfoCollection styleCollection = new FwStyleSheet.StyleInfoCollection();
			styleCollection.Add(new BaseStyleInfo(style));

			Assert.IsTrue(styleCollection.Contains(styleName.Normalize(NormalizationForm.FormC)));
			Assert.IsTrue(styleCollection.Contains(styleName.Normalize(NormalizationForm.FormD)));
			Assert.IsTrue(styleCollection.Contains(styleName.Normalize(NormalizationForm.FormKC)));
			Assert.IsTrue(styleCollection.Contains(styleName.Normalize(NormalizationForm.FormKD)));
		}
	}
}

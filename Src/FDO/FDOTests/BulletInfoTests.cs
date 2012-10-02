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
// File: BulletInfoTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using NUnit.Framework;

using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class BulletInfoTests: BaseTest
	{
		private StyleInfoTable m_infoTable;

		#region Setup/Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Setups this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void Setup()
		{
			m_infoTable = new StyleInfoTable("Normal", null);
			BaseStyleInfo styleInfo = new BaseStyleInfo();
			m_infoTable.Add("TestStyle", styleInfo);
			m_infoTable.ConnectStyles();
		}
		#endregion

		#region Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that we can decode a BLOB that was created with the C++ styles dialog.
		/// In this scenario all the properties of the font info are explictly set.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DecodeBackwardCompatible_EverythingSet()
		{
			// Setup what we expect
			FontInfo expectedFontInfo = m_infoTable["TestStyle"].FontInfoForWs(-1);
			expectedFontInfo.m_fontName = new InheritableStyleProp<string>("Algerian");
			expectedFontInfo.m_fontSize = new InheritableStyleProp<int>(8000);
			expectedFontInfo.m_fontColor = new InheritableStyleProp<Color>(Color.Red);
			expectedFontInfo.m_backColor = new InheritableStyleProp<Color>(Color.White);
			expectedFontInfo.m_underline = new InheritableStyleProp<FwUnderlineType>(FwUnderlineType.kuntNone);
			expectedFontInfo.m_underlineColor = new InheritableStyleProp<Color>(Color.Black);
			expectedFontInfo.m_bold = new InheritableStyleProp<bool>(false);
			expectedFontInfo.m_italic = new InheritableStyleProp<bool>(false);
			expectedFontInfo.m_superSub = new InheritableStyleProp<FwSuperscriptVal>(FwSuperscriptVal.kssvOff);
			expectedFontInfo.m_offset = new InheritableStyleProp<int>(0);

			// Here's the BLOB.
			// we got these values by looking at the memory when debugging Data Notebook.
			byte[] byteBlob = new byte[] {0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x06, 0x00, 0x40, 0x1f, 0x00, 0x00, 0x07, 0x00, 0x00, 0x00, 0x00,
				0x00, 0x08, 0x00, 0xff, 0x00, 0x00, 0x00, 0x09, 0x00, 0xff, 0xff, 0xff, 0x00,
				0x0a, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x41, 0x00, 0x6c, 0x00, 0x67,
				0x00, 0x65, 0x00, 0x72, 0x00, 0x69, 0x00, 0x61, 0x00, 0x6e, 0x00, 0x00, 0x00 };
			string blob = new string(Encoding.Unicode.GetChars(byteBlob));

			BulletInfo bulletInfo = new BulletInfo();
			bulletInfo.EncodedFontInfo = blob;

			Assert.AreEqual(expectedFontInfo, bulletInfo.FontInfo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that we can decode a BLOB that was created with the C++ styles dialog.
		/// In this scenario some of the properties of the font info aren't set.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DecodeBackwardCompatible_PartiallySet()
		{
			// Setup what we expect
			FontInfo expectedFontInfo = m_infoTable["TestStyle"].FontInfoForWs(-1);
			expectedFontInfo.m_fontName = new InheritableStyleProp<string>("Algerian");
			expectedFontInfo.m_fontSize = new InheritableStyleProp<int>(8000);
			expectedFontInfo.m_fontColor = new InheritableStyleProp<Color>(Color.Red);
			expectedFontInfo.m_underline = new InheritableStyleProp<FwUnderlineType>(FwUnderlineType.kuntNone);
			expectedFontInfo.m_underlineColor = new InheritableStyleProp<Color>(Color.Black);
			expectedFontInfo.m_superSub = new InheritableStyleProp<FwSuperscriptVal>(FwSuperscriptVal.kssvOff);
			expectedFontInfo.m_offset = new InheritableStyleProp<int>(0);

			// Here's the BLOB.
			// we got these values by looking at the memory when debugging Data Notebook.
			byte[] byteBlob = new byte[] {0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x06, 0x00, 0x40, 0x1f, 0x00, 0x00, 0x07, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x08, 0x00, 0xff, 0x00, 0x00, 0x00, 0x0a, 0x00, 0x00, 0x00, 0x00,
				0x00, 0x01, 0x00, 0x41, 0x00, 0x6c, 0x00, 0x67, 0x00, 0x65, 0x00, 0x72, 0x00,
				0x69, 0x00, 0x61, 0x00, 0x6e, 0x00, 0x00, 0x00 };
			string blob = new string(Encoding.Unicode.GetChars(byteBlob));

			BulletInfo bulletInfo = new BulletInfo();
			bulletInfo.EncodedFontInfo = blob;

			Assert.AreEqual(expectedFontInfo, bulletInfo.FontInfo);
		}
		#endregion
	}
}

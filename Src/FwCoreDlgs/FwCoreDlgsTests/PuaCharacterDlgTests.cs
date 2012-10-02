#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: PuaCharacterDlgTests.cs
// Responsibility: FW Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// These test test the PuaCharacterDlg dialog and the PuaCharacter tab on the WritingSystemPropertiesDialog.
	/// </summary>
	[TestFixture]
	public class PuaCharacterDlgTests: BaseTest
	{
		private InMemoryFdoCache m_inMemoryCache;
		DummyWritingSystemPropertiesDialog m_dlgWsProps;

		/// <summary>
		///
		/// </summary>
		[SetUp]
		public void Init()
		{
			StringUtils.InitIcuDataDir();
			m_inMemoryCache = InMemoryFdoCache.CreateInMemoryFdoCache();
			m_inMemoryCache.InitializeLangProject();
			m_inMemoryCache.Cache.LanguageWritingSystemFactoryAccessor.BypassInstall = true;
			m_inMemoryCache.InitializeWritingSystemEncodings();

			m_dlgWsProps = new DummyWritingSystemPropertiesDialog(m_inMemoryCache.Cache);

			// "show" the dialog box (the actually gui will never be loaded)
			// When in test mode the dialog will not call its base ShowDialog
			m_dlgWsProps.CallShowDialog();
		}

		/// <summary>
		///
		/// </summary>
		[TearDown]
		public void Teardown()
		{
			if (m_dlgWsProps != null)
			{
				m_dlgWsProps.Dispose();
				m_dlgWsProps = null;
			}
			m_inMemoryCache.Dispose();
		}

		/// <summary>
		/// make sure we clean up IcuDataDir after running tests.
		/// </summary>
		[TestFixtureTearDown]
		public void SuiteTeardown()
		{
			StringUtils.InitIcuDataDir();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the "add" functionality on the PUA tab of the WritingSystemPropertiesDialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("When LanguageWritingSystemFactoryAccessor.BypassInstall = false, this is actually changing pua info in the en.xml in DistFiles/Languages, and writing out skeleton LDFs for wss setup in m_inMemoryCache.InitializeWritingSystemEncodings()")]
		public void CreateNewPuaCharacter()
		{
			// Make some test characters
			PUACharacter newPuaChar =
				new PUACharacter("E001","ARABIC-INDIC DIGIT THREE;Nd;0;L;;3;3;3;N;;;;;");
			PUACharacter copyOfNewPuaChar = new PUACharacter(newPuaChar);
			PUACharacter modifiedPuaChar =
				new PUACharacter("E001","CHEESE DIGIT THREE;Lu;0;L;;;;;N;;;0063;0664;0665");
			PUACharacter copyOfModifiedPuaChar = new PUACharacter(modifiedPuaChar);

			// "click" new
			m_dlgWsProps.PressBtnNewPUA(newPuaChar);
			CheckPuaCharactersMatch(copyOfNewPuaChar, "using the 'new' button", m_dlgWsProps);

			// "click" modify
			m_dlgWsProps.PressBtnModifyPUA(modifiedPuaChar);
			CheckPuaCharactersMatch(copyOfModifiedPuaChar, "using the 'modify' button", m_dlgWsProps);

			// "click" okay
			m_dlgWsProps.PressOk();
			TestUtils.Check_PUA(0xE001, "CHEESE DIGIT THREE", LgGeneralCharCategory.kccLu);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// tests the disabling of uppercase field based on the General Category of Numeric Digit
		/// </summary>
		/// <remarks>intention to expand to may different tests eventually</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DisabledFields()
		{
			PUACharacter newPuaChar =
				new PUACharacter("EABC", "arabic-indic digit three;Nd;0;AN;;3;3;3;N;;;ABBB;ABBB;ABBB");
			PUACharacter correctedPuaChar =
				new PUACharacter("EABC", "ARABIC-INDIC DIGIT THREE;Nd;0;AN;;3;3;3;N;;;;;");

			PUACharacterDlg newDlgBox = new PUACharacterDlg();

			//			m_puaChar.GeneralCategory = newDlgBoxSelectedItem;

			m_dlgWsProps.PressBtnNewPUA(newPuaChar);

			//			PUACharacterDlg
			CheckPuaCharactersMatch(correctedPuaChar, "when setting the general category...",
				m_dlgWsProps);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the "add" functionality on the PUA tab of the WritingSystemPropertiesDialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateNewPuaCharacterWithBadData()
		{
			PUACharacter newPuaChar =
				new PUACharacter("E000", "LATIN CAPITAL LETTER H;Lu;0;L;;;;;N;;;0068;0664;0665");
			PUACharacter copyOfNewPuaChar =
				new PUACharacter("E000", "LATIN CAPITAL LETTER H;Lu;0;L;;;;;N;;;0068;0664;0665");

			m_dlgWsProps.PressBtnNewPUA(newPuaChar);
			CheckPuaCharactersMatch(copyOfNewPuaChar, "using the 'new' button", m_dlgWsProps);

			PUACharacter modifiedPuaChar =
				new PUACharacter("E000", "CHEESE DIGIT THREE;Lu;0;AN;;3;3;3;N;;;0063;0664;0665");
			PUACharacter copyOfModifiedPuaChar = new PUACharacter(modifiedPuaChar);

			m_dlgWsProps.PressBtnModifyPUA(modifiedPuaChar);
			CheckPuaCharactersMatch(copyOfModifiedPuaChar, "using the 'modify' button",
				m_dlgWsProps);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Assert that PUACharacter match what we expect.
		/// </summary>
		/// <param name="puaCharacterExpected">The expected PUACharacter</param>
		/// <param name="message">The message to append, e.g. "using the 'new' button"</param>
		/// <param name="wsPropsDlg">The ws props DLG.</param>
		/// ------------------------------------------------------------------------------------
		private void CheckPuaCharactersMatch(
			PUACharacter puaCharacterExpected, string message,
			DummyWritingSystemPropertiesDialog wsPropsDlg)
		{
			Assert.IsTrue(wsPropsDlg.IsPuaCodepointAdded(puaCharacterExpected.CodePoint),
				"Didn't add the PUACharacter " + puaCharacterExpected.CodePoint + " " + message);
			Assert.AreEqual(puaCharacterExpected.ToString(),
				wsPropsDlg.GetPuaString(puaCharacterExpected),
				"Didn't modify the PUACharacter " + message);
			Assert.IsTrue(wsPropsDlg.IsPuaAdded(puaCharacterExpected),
				"Didn't modify the PUACharacter " + message);
		}
	}
}

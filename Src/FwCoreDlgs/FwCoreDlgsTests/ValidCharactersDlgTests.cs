// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ValidCharactersDlgTests.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the ValidCharactersDlg.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ValidCharactersDlgTests : BaseTest
	{
		private DummyValidCharactersDlg m_dlg;

		/// <summary/>
		[SetUp]
		public void SetUp()
		{
			m_dlg = new DummyValidCharactersDlg();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Cleans up results after each test
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public void TearDown()
		{
			m_dlg.Dispose();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that we can add a single base character from the manual character entry
		/// text box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddSingleCharacter_ValidManualEntry_SingleBaseCharacter()
		{
			m_dlg.ManualCharEntryTextBox.Text = "A";

			m_dlg.CallAddSingleCharacter(m_dlg.ManualCharEntryTextBox);

			m_dlg.ValidCharsGridMngr.VerifyCharacters(new[] { "A" });
			Assert.AreEqual(String.Empty, m_dlg.ManualCharEntryTextBox.Text,
				"The manual entry text box should be cleared after adding the character.");
			Assert.AreEqual(0, m_dlg.MessageBoxText.Count,
				"No message boxes should have been displayed");
			Assert.AreEqual(0, m_dlg.BeepCount);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that a lone diacritic typed in the manual character entry text box will get
		/// wiped out and the user will get a message telling them how to deal with diacritics.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddSingleCharacter_InvalidManualEntry_LoneDiacritic()
		{
			m_dlg.ManualCharEntryTextBox.Text = "\u0301";

			Assert.AreEqual(String.Empty, m_dlg.ManualCharEntryTextBox.Text,
				"The manual entry text box should be cleared.");
			Assert.AreEqual(1, m_dlg.MessageBoxText.Count, "One message box should have been displayed");
			Assert.AreEqual(FwCoreDlgs.kstidLoneDiacriticNotValid, m_dlg.MessageBoxText[0]);
			Assert.AreEqual(0, m_dlg.BeepCount);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that a space followed by a diacritic typed in the manual character entry
		/// text box will get wiped out and the user will get a message telling them how to
		/// deal with diacritics.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddSingleCharacter_InvalidManualEntry_DiacriticWithLeadingSpace()
		{
			m_dlg.ManualCharEntryTextBox.Text = " \u0301";

			Assert.AreEqual(String.Empty, m_dlg.ManualCharEntryTextBox.Text,
				"The manual entry text box should be cleared.");
			Assert.AreEqual(1, m_dlg.MessageBoxText.Count, "One message box should have been displayed");
			Assert.AreEqual(FwCoreDlgs.kstidLoneDiacriticNotValid, m_dlg.MessageBoxText[0]);
			Assert.AreEqual(0, m_dlg.BeepCount);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that two spaces typed in the manual character entry text box will get
		/// cleared and the user will get beeped.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddSingleCharacter_InvalidManualEntry_TwoSpaces()
		{
			m_dlg.ManualCharEntryTextBox.Text = "  ";

			Assert.AreEqual(String.Empty, m_dlg.ManualCharEntryTextBox.Text,
				"The manual entry text box should be cleared.");
			Assert.AreEqual(0, m_dlg.MessageBoxText.Count, "No message boxes should have been displayed");
			Assert.AreEqual(1, m_dlg.BeepCount);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that an undefined character typed in the manual character entry text box
		/// will get cleared and the user will get beeped.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddSingleCharacter_InvalidManualEntry_BogusChar()
		{
			m_dlg.ManualCharEntryTextBox.Text = "\u5678"; // see DummyCharPropEngine.get_GeneralCategory

			Assert.AreEqual(String.Empty, m_dlg.ManualCharEntryTextBox.Text,
				"The manual entry text box should be cleared.");
			Assert.AreEqual(1, m_dlg.BeepCount, "One beep should have been issued");
			Assert.AreEqual(0, m_dlg.MessageBoxText.Count, "No message boxes should have been displayed");
			Assert.AreEqual(1, m_dlg.BeepCount);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that we can add a letter from the Unicode value text box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddSingleCharacter_ValidUnicodeEntry_SingleLetter()
		{
			m_dlg.UnicodeValueTextBox.Text = "0067";
			m_dlg.CallAddSingleCharacter(m_dlg.UnicodeValueTextBox);
			m_dlg.ValidCharsGridMngr.VerifyCharacters(new[] { "g" });

			Assert.AreEqual(String.Empty, m_dlg.UnicodeValueTextBox.Text,
				"The Unicode text box should be cleared after adding the character.");
			Assert.AreEqual(0, m_dlg.MessageBoxText.Count, "No message boxes should have been displayed");
			Assert.AreEqual(0, m_dlg.BeepCount);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that an error message is displayed if the user attempts to add a lone
		/// dicritic from the Unicode value text box. TE-8339
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddSingleCharacter_InvalidUnicodeEntry_Diacritic()
		{
			m_dlg.UnicodeValueTextBox.Text = "0301";
			m_dlg.CallAddSingleCharacter(m_dlg.UnicodeValueTextBox);
			m_dlg.ValidCharsGridMngr.VerifyCharacters(new string[] {  });

			Assert.AreEqual("0301", m_dlg.UnicodeValueTextBox.Text,
				"The Unicode text box should not be cleared to give the user a chance to correct the problem.");
			Assert.AreEqual(1, m_dlg.MessageBoxText.Count, "One message box should have been displayed");
			Assert.AreEqual(FwCoreDlgs.kstidLoneDiacriticNotValid, m_dlg.MessageBoxText[0]);
			Assert.AreEqual(0, m_dlg.BeepCount);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that an error message is displayed if the user attempts to add an undefined
		/// character from the Unicode value text box. TE-8339
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddSingleCharacter_InvalidUnicodeEntry_BogusChar()
		{
			m_dlg.UnicodeValueTextBox.Text = "5678";
			m_dlg.CallAddSingleCharacter(m_dlg.UnicodeValueTextBox);
			m_dlg.ValidCharsGridMngr.VerifyCharacters(new string[] {  });

			Assert.AreEqual("5678", m_dlg.UnicodeValueTextBox.Text,
				"The Unicode text box should not be cleared to give the user a chance to correct the problem.");
			Assert.AreEqual(1, m_dlg.MessageBoxText.Count, "One message box should have been displayed");
			Assert.AreEqual(ResourceHelper.GetResourceString("kstidUndefinedCharacterMsg"),
				m_dlg.MessageBoxText[0]);
			Assert.AreEqual(0, m_dlg.BeepCount);
		}

		#region Dummy objects for InvokeFromNewProject test (FWR-3660)
		private class Fwr3660ValidCharactersDlg: ValidCharactersDlg
		{
			public Fwr3660ValidCharactersDlg(FdoCache cache, IWritingSystemContainer container,
				IWritingSystem ws)
				: base(cache, container, null, null, ws, "dymmy")
			{
			}
		}

		private class DummyWritingSystem: IWritingSystem
		{

			#region IWritingSystem Members

			public string Abbreviation { get; set; }

			public Palaso.WritingSystems.Collation.ICollator Collator
			{
				get { throw new NotImplementedException(); }
			}

			public void Copy(IWritingSystem source)
			{
				throw new NotImplementedException();
			}

			public string DisplayLabel
			{
				get { return string.Empty; }
			}

			public string IcuLocale
			{
				get { throw new NotImplementedException(); }
			}

			public string RFC5646
			{
				get { throw new NotImplementedException(); }
			}

			public bool IsGraphiteEnabled { get; set; }

			public LanguageSubtag LanguageSubtag { get; set; }

			public string LegacyMapping { get; set; }

			public bool MarkedForDeletion { get; set; }

			public string MatchedPairs { get; set; }

			public bool Modified { get; set; }

			public string PunctuationPatterns { get; set; }

			public string QuotationMarks { get; set; }

			public RegionSubtag RegionSubtag { get; set; }

			public ScriptSubtag ScriptSubtag { get; set; }

			public string SortRules { get; set; }

			public Palaso.WritingSystems.WritingSystemDefinition.SortRulesType SortUsing { get; set; }

			public string ValidChars { get; set; }

			public bool ValidateCollationRules(out string message)
			{
				throw new NotImplementedException();
			}

			public VariantSubtag VariantSubtag { get; set; }

			public void WriteLdml(System.Xml.XmlWriter writer)
			{
				throw new NotImplementedException();
			}

			public IWritingSystemManager WritingSystemManager
			{
				get { throw new NotImplementedException(); }
			}

			#endregion

			#region ILgWritingSystem Members

			public ILgCharacterPropertyEngine CharPropEngine
			{
				get { throw new NotImplementedException(); }
			}

			public int CurrentLCID { get; set; }

			public string DefaultFontFeatures { get; set; }

			public string DefaultFontName
			{
				get { return "Times New Roman"; }
				set { throw new NotImplementedException(); }
			}

			public int Handle
			{
				get { throw new NotImplementedException(); }
			}

			public string ISO3
			{
				get { throw new NotImplementedException(); }
			}

			public string Id
			{
				get { return "en"; }
			}

			public void InterpretChrp(ref LgCharRenderProps chrp)
			{
				throw new NotImplementedException();
			}

			public string Keyboard { get; set; }

			public int LCID { get; set; }

			public string LanguageName
			{
				get { throw new NotImplementedException(); }
			}

			public bool RightToLeftScript { get; set; }

			public string SpellCheckingId { get; set; }

			public IRenderEngine get_Renderer(IVwGraphics vg)
			{
				throw new NotImplementedException();
			}

			#endregion
		}

		private class DummyWritingSystemContainer: IWritingSystemContainer
		{

			public DummyWritingSystemContainer()
			{
				DefaultVernacularWritingSystem = new DummyWritingSystem();
			}
			#region IWritingSystemContainer Members

			public void AddToCurrentAnalysisWritingSystems(IWritingSystem ws)
			{
				throw new NotImplementedException();
			}

			public void AddToCurrentVernacularWritingSystems(IWritingSystem ws)
			{
				throw new NotImplementedException();
			}

			public IEnumerable<IWritingSystem> AllWritingSystems
			{
				get { throw new NotImplementedException(); }
			}

			public ICollection<IWritingSystem> AnalysisWritingSystems
			{
				get { throw new NotImplementedException(); }
			}

			public IList<IWritingSystem> CurrentAnalysisWritingSystems
			{
				get { throw new NotImplementedException(); }
			}

			public IList<IWritingSystem> CurrentPronunciationWritingSystems
			{
				get { throw new NotImplementedException(); }
			}

			public IList<IWritingSystem> CurrentVernacularWritingSystems
			{
				get { throw new NotImplementedException(); }
			}

			public IWritingSystem DefaultAnalysisWritingSystem { get; set; }

			public IWritingSystem DefaultPronunciationWritingSystem
			{
				get { throw new NotImplementedException(); }
			}

			public IWritingSystem DefaultVernacularWritingSystem { get; set; }

			public ICollection<IWritingSystem> VernacularWritingSystems
			{
				get { throw new NotImplementedException(); }
			}

			#endregion
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests invoking from the New Project dialog where we don't have a cache yet
		/// (FWR-3660)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InvokeFromNewProject()
		{
			using (var dlg = new Fwr3660ValidCharactersDlg(null, new DummyWritingSystemContainer(),
				new DummyWritingSystem()))
			{
				Assert.NotNull(dlg);
			}
		}
	}

	#region DummyValidCharactersDlg
	internal class DummyValidCharactersDlg: ValidCharactersDlg
	{
		#region Member variables
		public List<string> MessageBoxText { get; set; }
		public int BeepCount { get; set; }
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up the ValidCharactersDlg for testing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyValidCharactersDlg()
		{
			ReflectionHelper.SetField(this, "m_chrPropEng", new DummyCharPropEngine());
			MessageBoxText = new List<string>();
		}

		/// <summary>
		/// Create a ValidCharsGridMngr for testing
		/// </summary>
		protected override ValidCharGridsManager CreateValidCharGridsManager()
		{
			return new DummyValidCharsGridMngr();
		}

		/// <summary>
		/// Exposes txtManualCharEntry text box
		/// </summary>
		public FwTextBox ManualCharEntryTextBox
		{
			get { return (FwTextBox)ReflectionHelper.GetField(this, "txtManualCharEntry"); }
		}

		/// <summary>
		/// Exposes txtUnicodeValue text box
		/// </summary>
		public FwTextBox UnicodeValueTextBox
		{
			get { return (FwTextBox)ReflectionHelper.GetField(this, "txtUnicodeValue"); }
		}

		/// <summary>
		/// Exposes AddSingleCharacter method
		/// </summary>
		public void CallAddSingleCharacter(FwTextBox txt)
		{
			AddSingleCharacter(txt);
		}

		/// <summary>
		/// Exposes m_validCharsGridMngr
		/// </summary>
		public DummyValidCharsGridMngr ValidCharsGridMngr
		{
			get { return (DummyValidCharsGridMngr)m_validCharsGridMngr; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows a message box to warn the user about an invalid operation.
		/// </summary>
		/// <param name="message">The message for the user.</param>
		/// ------------------------------------------------------------------------------------
		protected override void ShowMessageBox(string message)
		{
			MessageBoxText.Add(message);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Keeps track of the number of beeps that have been issued in the ValidCharactersDlg
		/// during a test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void IssueBeep()
		{
			BeepCount++;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If the specified character is defined as a PUA character in m_langDef, returns its
		/// character type; otherwise, returns a value that indicates whether it is a valid
		/// character as defined by the Unicode Standard.
		/// </summary>
		/// <param name="chr">The character (may consist of more than one Unicode codepoint.</param>
		/// ------------------------------------------------------------------------------------
		protected override ValidCharacterType GetCharacterType(string chr)
		{
			return (!String.IsNullOrEmpty(chr) && chr[0] != 0x5678) ?
				ValidCharacterType.DefinedUnknown : ValidCharacterType.None;
		}
	}
	#endregion

	#region DummyValidCharsGridMngr
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Override of a ValidCharsGridsManager for testing.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DummyValidCharsGridMngr : ValidCharactersDlg.ValidCharGridsManager
	{
		internal List<string> m_charsInGrid = new List<string>();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the character to a list (rather than to a grid).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected internal override void AddCharacter(string chr, ValidCharacterType type,  bool notUsed)
		{
			m_charsInGrid.Add(chr);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that the expected characters were added.
		/// </summary>
		/// <param name="expectedChars">The characters that we expect would be added to the
		/// grid for a particular test.</param>
		/// ------------------------------------------------------------------------------------
		public void VerifyCharacters(string[] expectedChars)
		{
			Assert.AreEqual(expectedChars.Length, m_charsInGrid.Count,
				"Expected number of characters in ValidCharsGridsManager does not match actual");
			foreach (string character in expectedChars)
			{
				Assert.IsTrue(m_charsInGrid.Contains(character),
					character + " had not been added to the ValidCharsGridsManager");
			}
		}
	}
	#endregion
}

// ---------------------------------------------------------------------------------------------
// Copyright (c) 2008-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: RepeatedWordsTests.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System.Collections.Generic;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Utils;

namespace SILUBS.ScriptureChecks
{
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// Test the Characters Scripture check using a data source that passes tokens similar
	/// to those produced by TE.
	/// </summary>
	/// ------------------------------------------------------------------------------------
	[TestFixture]
	public class CharactersCheckUnitTest_Fw : ScrChecksTestBase
	{
		private TestChecksDataSource m_dataSource;

		#region Initialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set up that happens before every test runs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();
			m_dataSource = new TestChecksDataSource();
			m_check = new CharactersCheck(m_dataSource);
		}
		#endregion

		#region Tests
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Character check for some simple cases that don't freak your mind out.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Basic()
		{
			m_dataSource.SetParameterValue("ValidCharacters", "a b c d e");

			m_dataSource.m_tokens.Add(new DummyTextToken("gha bcdefi",
				TextType.Verse, true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens, RecordError);

			Assert.AreEqual(4, m_errors.Count);
			CheckError(0, m_dataSource.m_tokens[0].Text, 0, "g", "Invalid or unknown character");
			CheckError(1, m_dataSource.m_tokens[0].Text, 1, "h", "Invalid or unknown character");
			CheckError(2, m_dataSource.m_tokens[0].Text, 8, "f", "Invalid or unknown character");
			CheckError(3, m_dataSource.m_tokens[0].Text, 9, "i", "Invalid or unknown character");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the AlwaysValidCharacters check for some simple cases.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void AlwaysValidChars()
		{
			m_dataSource.SetParameterValue("AlwaysValidCharacters", "12345\u2028");
			m_dataSource.m_tokens.Add(new DummyTextToken("ej53427\u20281fi",
				TextType.Verse, true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens, RecordError);

			Assert.AreEqual(5, m_errors.Count);
			CheckError(0, m_dataSource.m_tokens[0].Text, 0, "e", "Invalid or unknown character");
			CheckError(1, m_dataSource.m_tokens[0].Text, 1, "j", "Invalid or unknown character");
			CheckError(2, m_dataSource.m_tokens[0].Text, 6, "7", "Invalid or unknown character");
			CheckError(3, m_dataSource.m_tokens[0].Text, 9, "f", "Invalid or unknown character");
			CheckError(4, m_dataSource.m_tokens[0].Text, 10, "i", "Invalid or unknown character");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Character check with diacritic characters.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Diacritics()
		{
			m_dataSource.SetParameterValue("ValidCharacters", "a b c d e a\u0301 e\u0301");

			// 02 JUN 2008, Phil Hopper:  InvalidCharacters is not currently used.
			//m_dataSource.SetParameterValue("InvalidCharacters", "f g h a\u0302 e\u0302");

			m_dataSource.m_tokens.Add(new DummyTextToken("aa\u0301bcdea\u0302e\u0303",
				TextType.Verse, true, false, "Paragraph"));
			m_check.Check(m_dataSource.TextTokens, RecordError);

			Assert.AreEqual(2, m_errors.Count);
			CheckError(0, m_dataSource.m_tokens[0].Text, 7, "a\u0302",
				"Invalid or unknown character diacritic combination"); // invalid character
			CheckError(1, m_dataSource.m_tokens[0].Text, 9, "e\u0303",
				"Invalid or unknown character diacritic combination"); // unknown character
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Character check with different writing systems.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void DifferentWritingSystems()
		{
			// Set the valid characters for different writing systems. The vernacular doesn't
			// specify a locale.
			m_dataSource.SetParameterValue("ValidCharacters", "a b c d e f g");
			m_dataSource.SetParameterValue("ValidCharacters_en", "h i j k l m n");
			m_dataSource.SetParameterValue("ValidCharacters_fr", "o p q r s t u");

			m_dataSource.m_tokens.Add(new DummyTextToken("abcdefgh",
				TextType.Verse, true, false, "Paragraph"));
			m_dataSource.m_tokens.Add(new DummyTextToken("hijklmno",
				TextType.Verse, true, false, "Paragraph", string.Empty, "en"));
			m_dataSource.m_tokens.Add(new DummyTextToken("aopqrstu",
				TextType.Verse, true, false, "Paragraph", string.Empty, "fr"));

			m_check.Check(m_dataSource.TextTokens, RecordError);

			Assert.AreEqual(3, m_errors.Count);
			CheckError(0, m_dataSource.m_tokens[0].Text, 7, "h", "Invalid or unknown character");
			CheckError(1, m_dataSource.m_tokens[1].Text, 7, "o", "Invalid or unknown character");
			CheckError(2, m_dataSource.m_tokens[2].Text, 0, "a", "Invalid or unknown character");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Character check doesn't crash if the valid characters list is not set.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void UnsetValidCharactersList()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("abcdefgh",
				TextType.Verse, true, false, "Paragraph"));

			// This should not crash, even if the valid characters list has not been set.
			List<TextTokenSubstring> refs =
				CheckInventory.GetReferences(m_dataSource.TextTokens, string.Empty);

			Assert.AreEqual(8, refs.Count);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Character check with different writing systems.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void InventoryMode()
		{
			m_dataSource.m_tokens.Add(new DummyTextToken("Eph. 2:10",
				TextType.Verse, true, false, "Paragraph", string.Empty, "en"));
			m_dataSource.m_tokens.Add(new DummyTextToken("For we are God's workmanship...",
				TextType.Verse, true, false, "Paragraph"));

			List<TextTokenSubstring> refs =
				CheckInventory.GetReferences(m_dataSource.TextTokens, string.Empty);

			// We requested only the default vernacular.
			// Should only get references from the second token.
			Assert.AreEqual(31, refs.Count);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that ParseCharacterSequences returns characters with their following diacritics.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ParseCharacterSequences_Diacritics()
		{
			// Arabic letter alef with madda above
			// Arabic letter yeh with hamza above and immediately followed by a character without
			// diacritics (Arabic letter zain)
			string charsWithDiacritics = "\u0627\u0653 \u064A\u0654\u0632";

			// set up for this test.
			m_dataSource.m_tokens.Add(new DummyTextToken(charsWithDiacritics,
				TextType.Verse, true, false, "Paragraph"));
			m_check = new CharactersCheck(m_dataSource);
			ReflectionHelper.SetField(m_check, "m_categorizer", m_dataSource.CharacterCategorizer);

			// Get the parsed character sequences.
			List<string> parsedChars = new List<string>();
			foreach (string character in ((CharactersCheck)m_check).ParseCharacterSequences(charsWithDiacritics))
				parsedChars.Add(character);

			// Confirm that we have four characters with the expected contents.
			Assert.AreEqual(4, parsedChars.Count, "We expected four characters");
			Assert.AreEqual("\u0627\u0653", parsedChars[0]);
			Assert.AreEqual(" ", parsedChars[1]);
			Assert.AreEqual("\u064A\u0654", parsedChars[2]);
			Assert.AreEqual("\u0632", parsedChars[3]);
		}
		#endregion
	}
}

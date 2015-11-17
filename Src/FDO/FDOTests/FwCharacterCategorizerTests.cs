// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests use of the FwCharacterCategorizer that uses ICU instead of .NET to determine
	/// the category of characters for a particular writing system.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FwCharacterCategorizerTests : MemoryOnlyBackendProviderTestBase
	{
		#region Member variables
		private int m_wsEn;
		#endregion

		#region Constants
		private const string ksXmlHeader = "<?xml version=\"1.0\" encoding=\"utf-16\"?>";
		#endregion

		#region Test setup and tear down
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set up before each test is run.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();

			m_wsEn = Cache.WritingSystemFactory.get_Engine("en").Handle;
		}
		#endregion

		#region Tests
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting punctuation and word-forming charcters when the '#' is not defined
		/// as a word-forming character.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void SymbolPunctuationOnly()
		{
			var validChars = ValidCharacters.Load(ksXmlHeader +
				"<ValidCharacters><WordForming>a\uFFFCb\uFFFCc\uFFFCd\uFFFCe</WordForming>" +
				"<Numeric>1\uFFFC2\uFFFC3\uFFFC4\uFFFC5</Numeric>" +
				"<Other>'\uFFFC-\uFFFC#</Other>" +
				"</ValidCharacters>", "Test WS", null, null, FwDirectoryFinder.LegacyWordformingCharOverridesFile);
			ILgCharacterPropertyEngine lgCharPropEngineEn = Cache.WritingSystemFactory.get_CharPropEngine(
				m_wsEn);

			FwCharacterCategorizer categorizer = new FwCharacterCategorizer(validChars, lgCharPropEngineEn);
			Assert.IsTrue(categorizer.IsPunctuation('#'));
			Assert.IsFalse(categorizer.IsWordFormingCharacter('#'));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the WordsAndPuncs class when the '.' character is defined as a word-forming
		/// character.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void WordAndPuncs_OverridePunc()
		{
			var validChars = ValidCharacters.Load(ksXmlHeader +
				"<ValidCharacters><WordForming>a\uFFFCb\uFFFCc\uFFFCd\uFFFCe\uFFFC.</WordForming>" +
				"<Numeric>1\uFFFC2\uFFFC3\uFFFC4\uFFFC5</Numeric>" +
				"<Other>'\uFFFC-\uFFFC#</Other>" +
				"</ValidCharacters>", "Test WS", null, null, FwDirectoryFinder.LegacyWordformingCharOverridesFile);
			ILgCharacterPropertyEngine lgCharPropEngineEn = Cache.WritingSystemFactory.get_CharPropEngine(
				m_wsEn);

			FwCharacterCategorizer categorizer = new FwCharacterCategorizer(validChars, lgCharPropEngineEn);

			List<WordAndPunct> wordsAndPunc = categorizer.WordAndPuncts("abc.de");

			// We expect one word to be returned.
			Assert.AreEqual(1, wordsAndPunc.Count);
			Assert.AreEqual("abc.de", wordsAndPunc[0].Word);
		}


		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FwCharacterCategorizer class when the WordsAndPuncs method processes a
		/// string containing only spaces.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void WordAndPuncs_Spaces()
		{
			var validChars = ValidCharacters.Load(ksXmlHeader +
				"<ValidCharacters><WordForming>a\uFFFCb\uFFFCc</WordForming>" +
				"<Numeric>1\uFFFC2\uFFFC3\uFFFC4\uFFFC5</Numeric>" +
				"<Other>-\uFFFCU+0020</Other>" +
				"</ValidCharacters>", "Test WS", null, null, FwDirectoryFinder.LegacyWordformingCharOverridesFile);
			var english = Cache.ServiceLocator.WritingSystemManager.Get("en");
			var lgCharPropEngineEn = Cache.WritingSystemFactory.get_CharPropEngine(english.Handle);

			FwCharacterCategorizer categorizer = new FwCharacterCategorizer(validChars, lgCharPropEngineEn);

			List<WordAndPunct> wordsAndPunc = categorizer.WordAndPuncts(" ");
			Assert.AreEqual(0, wordsAndPunc.Count);

			wordsAndPunc = categorizer.WordAndPuncts("   ");
			Assert.AreEqual(0, wordsAndPunc.Count);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FwCharacterCategorizer class when the WordsAndPuncs method processes an
		/// empty string.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void WordAndPuncs_EmptyString()
		{
			var validChars = ValidCharacters.Load(ksXmlHeader +
				"<ValidCharacters><WordForming>a\uFFFCb\uFFFCc</WordForming>" +
				"<Numeric>1\uFFFC2\uFFFC3\uFFFC4\uFFFC5</Numeric>" +
				"<Other>-\uFFFCU+0020</Other>" +
				"</ValidCharacters>", "Test WS", null, null, FwDirectoryFinder.LegacyWordformingCharOverridesFile);
			var english = Cache.ServiceLocator.WritingSystemManager.Get("en");
			var lgCharPropEngineEn = Cache.WritingSystemFactory.get_CharPropEngine(
				english.Handle);

			FwCharacterCategorizer categorizer = new FwCharacterCategorizer(validChars, lgCharPropEngineEn);

			List<WordAndPunct> wordsAndPunc = categorizer.WordAndPuncts("");

			// We expect one word to be returned.
			Assert.AreEqual(0, wordsAndPunc.Count);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the WordsAndPuncs class when the '.' character is not defined as a word-forming
		/// character.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void WordAndPuncs_NoOverridePunc()
		{
			var validChars = ValidCharacters.Load(ksXmlHeader +
				"<ValidCharacters><WordForming>a\uFFFCb\uFFFCc\uFFFCd\uFFFCe</WordForming>" +
				"<Numeric>1\uFFFC2\uFFFC3\uFFFC4\uFFFC5</Numeric>" +
				"<Other>'\uFFFC-\uFFFC#</Other>" +
				"</ValidCharacters>", "Test WS", null, null, FwDirectoryFinder.LegacyWordformingCharOverridesFile);
			ILgCharacterPropertyEngine lgCharPropEngineEn = Cache.WritingSystemFactory.get_CharPropEngine(
				m_wsEn);

			FwCharacterCategorizer categorizer = new FwCharacterCategorizer(validChars, lgCharPropEngineEn);

			List<WordAndPunct> wordsAndPunc = categorizer.WordAndPuncts("abc.de");

			// We expect two words to be returned.
			Assert.AreEqual(2, wordsAndPunc.Count);
			Assert.AreEqual("abc", wordsAndPunc[0].Word);
			Assert.AreEqual("de", wordsAndPunc[1].Word);
		}
		#endregion
	}
}

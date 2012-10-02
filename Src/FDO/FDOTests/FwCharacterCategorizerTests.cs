using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests use of the FwCharacterCategorizer that uses ICU instead of .NET to determine
	/// the category of characters for a particular writing system.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FwCharacterCategorizerTests : InMemoryFdoTestBase
	{
		#region Member variables
		private InMemoryFdoCache m_cache;
		private IWritingSystem m_ws;
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
		[SetUp]
		public void SetUp()
		{
			CheckDisposed();
			if (m_cache != null)
				m_cache.Dispose();

			m_cache = InMemoryFdoCache.CreateInMemoryFdoCache();
			m_cache.InitializeWritingSystemEncodings();
			ILgWritingSystemFactory lgwsf = m_cache.Cache.LanguageWritingSystemFactoryAccessor;
			m_ws = lgwsf.get_EngineOrNull(InMemoryFdoCache.s_wsHvos.En);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Cleanup after each test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public void Teardown()
		{
			CheckDisposed();
			m_cache.Dispose();
			m_cache = null;
			m_ws = null;
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
			ValidCharacters validChars = ValidCharacters.Load(ksXmlHeader +
				"<ValidCharacters><WordForming>a\uFFFCb\uFFFCc\uFFFCd\uFFFCe</WordForming>" +
				"<Numeric>1\uFFFC2\uFFFC3\uFFFC4\uFFFC5</Numeric>" +
				"<Other>'\uFFFC-\uFFFC#</Other>" +
				"</ValidCharacters>", "Test WS", null);

			ILgCharacterPropertyEngine lgCharPropEngineEn = (ILgCharacterPropertyEngine)
				m_cache.Cache.LanguageWritingSystemFactoryAccessor.get_CharPropEngine(
				InMemoryFdoCache.s_wsHvos.En);

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
			ValidCharacters validChars = ValidCharacters.Load(ksXmlHeader +
				"<ValidCharacters><WordForming>a\uFFFCb\uFFFCc\uFFFCd\uFFFCe\uFFFC.</WordForming>" +
				"<Numeric>1\uFFFC2\uFFFC3\uFFFC4\uFFFC5</Numeric>" +
				"<Other>'\uFFFC-\uFFFC#</Other>" +
				"</ValidCharacters>", "Test WS", null);

			ILgCharacterPropertyEngine lgCharPropEngineEn = (ILgCharacterPropertyEngine)
				m_cache.Cache.LanguageWritingSystemFactoryAccessor.get_CharPropEngine(
				InMemoryFdoCache.s_wsHvos.En);

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
			ValidCharacters validChars = ValidCharacters.Load(ksXmlHeader +
				"<ValidCharacters><WordForming>a\uFFFCb\uFFFCc</WordForming>" +
				"<Numeric>1\uFFFC2\uFFFC3\uFFFC4\uFFFC5</Numeric>" +
				"<Other>-\uFFFCU+0020</Other>" +
				"</ValidCharacters>", "Test WS", null);

			ILgCharacterPropertyEngine lgCharPropEngineEn = (ILgCharacterPropertyEngine)
				m_cache.Cache.LanguageWritingSystemFactoryAccessor.get_CharPropEngine(
				InMemoryFdoCache.s_wsHvos.En);

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
			ValidCharacters validChars = ValidCharacters.Load(ksXmlHeader +
				"<ValidCharacters><WordForming>a\uFFFCb\uFFFCc</WordForming>" +
				"<Numeric>1\uFFFC2\uFFFC3\uFFFC4\uFFFC5</Numeric>" +
				"<Other>-\uFFFCU+0020</Other>" +
				"</ValidCharacters>", "Test WS", null);

			ILgCharacterPropertyEngine lgCharPropEngineEn = (ILgCharacterPropertyEngine)
				m_cache.Cache.LanguageWritingSystemFactoryAccessor.get_CharPropEngine(
				InMemoryFdoCache.s_wsHvos.En);

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
			ValidCharacters validChars = ValidCharacters.Load(ksXmlHeader +
				"<ValidCharacters><WordForming>a\uFFFCb\uFFFCc\uFFFCd\uFFFCe</WordForming>" +
				"<Numeric>1\uFFFC2\uFFFC3\uFFFC4\uFFFC5</Numeric>" +
				"<Other>'\uFFFC-\uFFFC#</Other>" +
				"</ValidCharacters>", "Test WS", null);

			ILgCharacterPropertyEngine lgCharPropEngineEn = (ILgCharacterPropertyEngine)
				m_cache.Cache.LanguageWritingSystemFactoryAccessor.get_CharPropEngine(
				InMemoryFdoCache.s_wsHvos.En);

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

using System.Collections.Generic;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.WritingSystems;
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
			CoreWritingSystemDefinition ws = Cache.ServiceLocator.WritingSystemManager.Create("th");
			ws.CharacterSets.Clear();
			ws.CharacterSets.Add(new CharacterSetDefinition("main") {Characters = {"a", "b", "c", "d", "e"}});
			ws.CharacterSets.Add(new CharacterSetDefinition("numeric") {Characters = {"1", "2", "3", "4", "5"}});
			ws.CharacterSets.Add(new CharacterSetDefinition("punctuation") {Characters = {"'", "-", "#"}});
			ValidCharacters validChars = ValidCharacters.Load(ws);
			ILgCharacterPropertyEngine lgCharPropEngineEn = Cache.WritingSystemFactory.get_CharPropEngine(m_wsEn);

			var categorizer = new FwCharacterCategorizer(validChars, lgCharPropEngineEn);
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
			CoreWritingSystemDefinition ws = Cache.ServiceLocator.WritingSystemManager.Create("th");
			ws.CharacterSets.Clear();
			ws.CharacterSets.Add(new CharacterSetDefinition("main") {Characters = {"a", "b", "c", "d", "e", "."}});
			ws.CharacterSets.Add(new CharacterSetDefinition("numeric") {Characters = {"1", "2", "3", "4", "5"}});
			ws.CharacterSets.Add(new CharacterSetDefinition("punctuation") {Characters = {"'", "-", "#"}});
			ValidCharacters validChars = ValidCharacters.Load(ws);
			ILgCharacterPropertyEngine lgCharPropEngineEn = Cache.WritingSystemFactory.get_CharPropEngine(m_wsEn);

			var categorizer = new FwCharacterCategorizer(validChars, lgCharPropEngineEn);

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
			CoreWritingSystemDefinition ws = Cache.ServiceLocator.WritingSystemManager.Create("th");
			ws.CharacterSets.Clear();
			ws.CharacterSets.Add(new CharacterSetDefinition("main") {Characters = {"a", "b", "c"}});
			ws.CharacterSets.Add(new CharacterSetDefinition("numeric") {Characters = {"1", "2", "3", "4", "5"}});
			ws.CharacterSets.Add(new CharacterSetDefinition("punctuation") {Characters = {"-", " "}});
			ValidCharacters validChars = ValidCharacters.Load(ws);
			ILgCharacterPropertyEngine lgCharPropEngineEn = Cache.WritingSystemFactory.get_CharPropEngine(m_wsEn);

			var categorizer = new FwCharacterCategorizer(validChars, lgCharPropEngineEn);

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
			CoreWritingSystemDefinition ws = Cache.ServiceLocator.WritingSystemManager.Create("th");
			ws.CharacterSets.Clear();
			ws.CharacterSets.Add(new CharacterSetDefinition("main") {Characters = {"a", "b", "c"}});
			ws.CharacterSets.Add(new CharacterSetDefinition("numeric") {Characters = {"1", "2", "3", "4", "5"}});
			ws.CharacterSets.Add(new CharacterSetDefinition("punctuation") {Characters = {"-", " "}});
			ValidCharacters validChars = ValidCharacters.Load(ws);
			ILgCharacterPropertyEngine lgCharPropEngineEn = Cache.WritingSystemFactory.get_CharPropEngine(m_wsEn);

			var categorizer = new FwCharacterCategorizer(validChars, lgCharPropEngineEn);

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
			CoreWritingSystemDefinition ws = Cache.ServiceLocator.WritingSystemManager.Create("th");
			ws.CharacterSets.Clear();
			ws.CharacterSets.Add(new CharacterSetDefinition("main") {Characters = {"a", "b", "c", "d", "e"}});
			ws.CharacterSets.Add(new CharacterSetDefinition("numeric") {Characters = {"1", "2", "3", "4", "5"}});
			ws.CharacterSets.Add(new CharacterSetDefinition("punctuation") {Characters = {"'", "-", "#"}});
			ValidCharacters validChars = ValidCharacters.Load(ws);
			ILgCharacterPropertyEngine lgCharPropEngineEn = Cache.WritingSystemFactory.get_CharPropEngine(m_wsEn);

			var categorizer = new FwCharacterCategorizer(validChars, lgCharPropEngineEn);

			List<WordAndPunct> wordsAndPunc = categorizer.WordAndPuncts("abc.de");

			// We expect two words to be returned.
			Assert.AreEqual(2, wordsAndPunc.Count);
			Assert.AreEqual("abc", wordsAndPunc[0].Word);
			Assert.AreEqual("de", wordsAndPunc[1].Word);
		}
		#endregion
	}
}

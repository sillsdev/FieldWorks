// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using NUnit.Framework;
using SIL.LCModel.Core.WritingSystems;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.WritingSystems;

namespace SIL.FieldWorks.FwCoreDlgs
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
			ws.CharacterSets.Add(new CharacterSetDefinition("punctuation") {Characters = {"'", "-", "#"}});
			ValidCharacters validChars = ValidCharacters.Load(ws);

			var categorizer = new FwCharacterCategorizer(validChars);
			Assert.That(categorizer.IsPunctuation('#'), Is.True);
			Assert.That(categorizer.IsWordFormingCharacter('#'), Is.False);
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
			ws.CharacterSets.Add(new CharacterSetDefinition("punctuation") {Characters = {"'", "-", "#"}});
			ValidCharacters validChars = ValidCharacters.Load(ws);

			var categorizer = new FwCharacterCategorizer(validChars);

			List<WordAndPunct> wordsAndPunc = categorizer.WordAndPuncts("abc.de");

			// We expect one word to be returned.
			Assert.That(wordsAndPunc.Count, Is.EqualTo(1));
			Assert.That(wordsAndPunc[0].Word, Is.EqualTo("abc.de"));
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
			ws.CharacterSets.Add(new CharacterSetDefinition("punctuation") {Characters = {"-", " "}});
			ValidCharacters validChars = ValidCharacters.Load(ws);

			var categorizer = new FwCharacterCategorizer(validChars);

			List<WordAndPunct> wordsAndPunc = categorizer.WordAndPuncts(" ");
			Assert.That(wordsAndPunc.Count, Is.EqualTo(0));

			wordsAndPunc = categorizer.WordAndPuncts("   ");
			Assert.That(wordsAndPunc.Count, Is.EqualTo(0));
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
			ws.CharacterSets.Add(new CharacterSetDefinition("punctuation") {Characters = {"-", " "}});
			ValidCharacters validChars = ValidCharacters.Load(ws);

			var categorizer = new FwCharacterCategorizer(validChars);

			List<WordAndPunct> wordsAndPunc = categorizer.WordAndPuncts("");

			// We expect one word to be returned.
			Assert.That(wordsAndPunc.Count, Is.EqualTo(0));
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
			ws.CharacterSets.Add(new CharacterSetDefinition("punctuation") {Characters = {"'", "-", "#"}});
			ValidCharacters validChars = ValidCharacters.Load(ws);

			var categorizer = new FwCharacterCategorizer(validChars);

			List<WordAndPunct> wordsAndPunc = categorizer.WordAndPuncts("abc.de");

			// We expect two words to be returned.
			Assert.That(wordsAndPunc.Count, Is.EqualTo(2));
			Assert.That(wordsAndPunc[0].Word, Is.EqualTo("abc"));
			Assert.That(wordsAndPunc[1].Word, Is.EqualTo("de"));
		}
		#endregion
	}
}

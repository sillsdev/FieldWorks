// Copyright (c) 2008-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class CharacterCategorizerTests
	{
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the WordAndPuncts() method using a simple sentence
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void WordAndPuncts_simple()
		{
			CharacterCategorizer cat = new CharacterCategorizer("", "", "");
			IEnumerable<WordAndPunct> words = cat.WordAndPuncts("This is my test.");
			using (IEnumerator<WordAndPunct> wordCollection = words.GetEnumerator())
			{
				Assert.That(wordCollection.MoveNext(), Is.True);
				CheckWordAndPunct(wordCollection.Current, "This", " ", 0);
				Assert.That(wordCollection.MoveNext(), Is.True);
				CheckWordAndPunct(wordCollection.Current, "is", " ", 5);
				Assert.That(wordCollection.MoveNext(), Is.True);
				CheckWordAndPunct(wordCollection.Current, "my", " ", 8);
				Assert.That(wordCollection.MoveNext(), Is.True);
				CheckWordAndPunct(wordCollection.Current, "test", ".", 11);
				Assert.That(wordCollection.MoveNext(), Is.False);
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the WordAndPuncts() method when a word contains a number
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void WordAndPuncts_numberInWord()
		{
			CharacterCategorizer cat = new CharacterCategorizer("", "", "");
			IEnumerable<WordAndPunct> words = cat.WordAndPuncts("This is test1.");
			using (IEnumerator<WordAndPunct> wordCollection = words.GetEnumerator())
			{
				Assert.That(wordCollection.MoveNext(), Is.True);
				CheckWordAndPunct(wordCollection.Current, "This", " ", 0);
				Assert.That(wordCollection.MoveNext(), Is.True);
				CheckWordAndPunct(wordCollection.Current, "is", " ", 5);
				Assert.That(wordCollection.MoveNext(), Is.True);
				CheckWordAndPunct(wordCollection.Current, "test1", ".", 8);
				Assert.That(wordCollection.MoveNext(), Is.False);
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the WordAndPuncts() method when the token starts with a space and then has a
		/// sequence of space-delimited digits.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void WordAndPuncts_initialSpace()
		{
			CharacterCategorizer cat = new CharacterCategorizer("", "", "");
			IEnumerable<WordAndPunct> words = cat.WordAndPuncts(" Dude ");
			using (IEnumerator<WordAndPunct> wordCollection = words.GetEnumerator())
			{
				Assert.That(wordCollection.MoveNext(), Is.True);
				CheckWordAndPunct(wordCollection.Current, "Dude", " ", 1);
				Assert.That(wordCollection.MoveNext(), Is.False);
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the WordAndPuncts() method when the token starts with a space and then has a
		/// sequence of space-delimited digits.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void WordAndPuncts_initialSpaceFollowedByNumbers()
		{
			CharacterCategorizer cat = new CharacterCategorizer("", "", "");
			IEnumerable<WordAndPunct> words = cat.WordAndPuncts("1 2 3");
			using (IEnumerator<WordAndPunct> wordCollection = words.GetEnumerator())
			{
				Assert.That(wordCollection.MoveNext(), Is.True);
				CheckWordAndPunct(wordCollection.Current, "1", " ", 0);
				Assert.That(wordCollection.MoveNext(), Is.True);
				CheckWordAndPunct(wordCollection.Current, "2", " ", 2);
				Assert.That(wordCollection.MoveNext(), Is.True);
				CheckWordAndPunct(wordCollection.Current, "3", "", 4);
				Assert.That(wordCollection.MoveNext(), Is.False);
			}
		}

		#region Helper methods
		private void CheckWordAndPunct(WordAndPunct wordAndPunct, string word, string punct, int offset)
		{
			Assert.That(wordAndPunct.Word, Is.EqualTo(word), "The word is not correct");
			Assert.That(wordAndPunct.Punct, Is.EqualTo(punct), "The punctuation is not correct");
			Assert.That(wordAndPunct.Offset, Is.EqualTo(offset), "The offset is not correct");
		}
		#endregion
	}
}

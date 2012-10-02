// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: InterfacesTests.cs
// Responsibility: Wussow
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace SILUBS.SharedScrUtils
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
				Assert.IsTrue(wordCollection.MoveNext());
				CheckWordAndPunct(wordCollection.Current, "This", " ", 0);
				Assert.IsTrue(wordCollection.MoveNext());
				CheckWordAndPunct(wordCollection.Current, "is", " ", 5);
				Assert.IsTrue(wordCollection.MoveNext());
				CheckWordAndPunct(wordCollection.Current, "my", " ", 8);
				Assert.IsTrue(wordCollection.MoveNext());
				CheckWordAndPunct(wordCollection.Current, "test", ".", 11);
				Assert.IsFalse(wordCollection.MoveNext());
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
				Assert.IsTrue(wordCollection.MoveNext());
				CheckWordAndPunct(wordCollection.Current, "This", " ", 0);
				Assert.IsTrue(wordCollection.MoveNext());
				CheckWordAndPunct(wordCollection.Current, "is", " ", 5);
				Assert.IsTrue(wordCollection.MoveNext());
				CheckWordAndPunct(wordCollection.Current, "test1", ".", 8);
				Assert.IsFalse(wordCollection.MoveNext());
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
				Assert.IsTrue(wordCollection.MoveNext());
				CheckWordAndPunct(wordCollection.Current, "Dude", " ", 1);
				Assert.IsFalse(wordCollection.MoveNext());
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
				Assert.IsTrue(wordCollection.MoveNext());
				CheckWordAndPunct(wordCollection.Current, "1", " ", 0);
				Assert.IsTrue(wordCollection.MoveNext());
				CheckWordAndPunct(wordCollection.Current, "2", " ", 2);
				Assert.IsTrue(wordCollection.MoveNext());
				CheckWordAndPunct(wordCollection.Current, "3", "", 4);
				Assert.IsFalse(wordCollection.MoveNext());
			}
		}

		#region Helper methods
		private void CheckWordAndPunct(WordAndPunct wordAndPunct, string word, string punct, int offset)
		{
			Assert.AreEqual(word, wordAndPunct.Word, "The word is not correct");
			Assert.AreEqual(punct, wordAndPunct.Punct, "The punctuation is not correct");
			Assert.AreEqual(offset, wordAndPunct.Offset, "The offset is not correct");
		}
		#endregion
	}
}

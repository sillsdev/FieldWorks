// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TextBoxExtensionsTests.cs
// ---------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using NUnit.Framework;

namespace SIL.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for extension methods on the TextBoxBase class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class TextBoxExtensionsTests
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MoveSelectedWord method when the initial string is empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveSelectedWord_EmptyString()
		{
			TextBox txt = new TextBox();

			Assert.IsFalse(txt.MoveSelectedWord(true));
			Assert.AreEqual(String.Empty, txt.Text);
			Assert.IsFalse(txt.MoveSelectedWord(false));
			Assert.AreEqual(String.Empty, txt.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MoveSelectedWord method when the initial string is only a single word and
		/// no characters are selected or the whole word is selected.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveSelectedWord_OneWord()
		{
			const string initial = "dog";
			TextBox txt = new TextBox();
			txt.Text = initial;

			Assert.IsFalse(txt.MoveSelectedWord(true));
			Assert.AreEqual(initial, txt.Text);
			Assert.IsFalse(txt.MoveSelectedWord(false));
			Assert.AreEqual(initial, txt.Text);

			txt.SelectionStart = 3;
			Assert.IsFalse(txt.MoveSelectedWord(true));
			Assert.AreEqual(initial, txt.Text);
			Assert.IsFalse(txt.MoveSelectedWord(false));
			Assert.AreEqual(initial, txt.Text);

			txt.SelectionStart = 0;
			txt.SelectionLength = 3;
			Assert.IsFalse(txt.MoveSelectedWord(true));
			Assert.AreEqual(initial, txt.Text);
			Assert.IsFalse(txt.MoveSelectedWord(false));
			Assert.AreEqual(initial, txt.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MoveSelectedWord method when the initial string is only a single word and
		/// the middle part of that word is selected.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveSelectedWord_OneWord_MiddleOfWordSelected()
		{
			const string initial = "dog";
			TextBox txt = new TextBox();

			txt.Text = initial;
			txt.SelectionStart = 1;
			txt.SelectionLength = 1;
			Assert.IsTrue(txt.MoveSelectedWord(true));
			Assert.AreEqual("dgo", txt.Text);
			Assert.AreEqual(2, txt.SelectionStart);
			Assert.AreEqual(1, txt.SelectionLength);

			txt.Text = initial;
			txt.SelectionStart = 1;
			txt.SelectionLength = 1;
			Assert.IsTrue(txt.MoveSelectedWord(false));
			Assert.AreEqual("odg", txt.Text);
			Assert.AreEqual(0, txt.SelectionStart);
			Assert.AreEqual(1, txt.SelectionLength);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MoveSelectedWord method when the initial string is only a single word and
		/// the last part of that word is selected.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveSelectedWord_OneWord_EndOfWordSelected()
		{
			const string initial = "dog";
			TextBox txt = new TextBox();

			txt.Text = initial;
			txt.SelectionStart = 2;
			txt.SelectionLength = 1;
			Assert.IsFalse(txt.MoveSelectedWord(true));
			Assert.AreEqual(initial, txt.Text);

			txt.Text = initial;
			txt.SelectionStart = 2;
			txt.SelectionLength = 1;
			Assert.IsTrue(txt.MoveSelectedWord(false));
			Assert.AreEqual("gdo", txt.Text);
			Assert.AreEqual(0, txt.SelectionStart);
			Assert.AreEqual(1, txt.SelectionLength);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MoveSelectedWord method when the initial string is only a single word and
		/// the first part of that word is selected.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveSelectedWord_OneWord_StartOfWordSelected()
		{
			const string initial = "dog";
			TextBox txt = new TextBox();

			txt.Text = initial;
			txt.SelectionStart = 0;
			txt.SelectionLength = 1;
			Assert.IsTrue(txt.MoveSelectedWord(true));
			Assert.AreEqual("ogd", txt.Text);
			Assert.AreEqual(2, txt.SelectionStart);
			Assert.AreEqual(1, txt.SelectionLength);

			txt.Text = initial;
			txt.SelectionStart = 0;
			txt.SelectionLength = 1;
			Assert.IsFalse(txt.MoveSelectedWord(false));
			Assert.AreEqual(initial, txt.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MoveSelectedWord method when the initial string is only a single word with
		/// a leading space and the space is selected.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveSelectedWord_OneWord_SpaceBefore_SpaceSelected()
		{
			const string initial = " dog";
			TextBox txt = new TextBox();

			txt.Text = initial;
			txt.SelectionStart = 0;
			txt.SelectionLength = 1;
			Assert.IsTrue(txt.MoveSelectedWord(true));
			Assert.AreEqual("dog ", txt.Text);
			Assert.AreEqual(3, txt.SelectionStart);
			Assert.AreEqual(1, txt.SelectionLength);

			txt.Text = initial;
			txt.SelectionStart = 0;
			txt.SelectionLength = 1;
			Assert.IsFalse(txt.MoveSelectedWord(false));
			Assert.AreEqual(initial, txt.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MoveSelectedWord method when the initial string is only a single word with
		/// a leading space and the word is selected.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveSelectedWord_OneWord_SpaceBefore_WordSelected()
		{
			const string initial = " dog";
			TextBox txt = new TextBox();

			txt.Text = initial;
			txt.SelectionStart = 1;
			txt.SelectionLength = 3;
			Assert.IsFalse(txt.MoveSelectedWord(true));
			Assert.AreEqual(initial, txt.Text);

			txt.Text = initial;
			txt.SelectionStart = 1;
			txt.SelectionLength = 3;
			Assert.IsTrue(txt.MoveSelectedWord(false));
			Assert.AreEqual("dog ", txt.Text);
			Assert.AreEqual(0, txt.SelectionStart);
			Assert.AreEqual(3, txt.SelectionLength);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MoveSelectedWord method when the initial string is only a single word with
		/// a leading space and the insertion point is in the word.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveSelectedWord_OneWord_SpaceBefore_IPInWord()
		{
			const string initial = " dog";
			TextBox txt = new TextBox();

			txt.Text = initial;
			txt.SelectionStart = 2;
			txt.SelectionLength = 0;
			Assert.IsFalse(txt.MoveSelectedWord(true));
			Assert.AreEqual(initial, txt.Text);

			txt.Text = initial;
			txt.SelectionStart = 2;
			txt.SelectionLength = 0;
			Assert.IsTrue(txt.MoveSelectedWord(false));
			Assert.AreEqual("dog ", txt.Text);
			Assert.AreEqual(0, txt.SelectionStart);
			Assert.AreEqual(3, txt.SelectionLength);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MoveSelectedWord method when the initial string is only a single word with
		/// a leading space and the insertion point is before the space.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveSelectedWord_OneWord_SpaceBefore_IPBeforeSpace()
		{
			const string initial = " dog";
			TextBox txt = new TextBox();

			txt.Text = initial;
			txt.SelectionStart = 0;
			txt.SelectionLength = 0;
			Assert.IsFalse(txt.MoveSelectedWord(true));
			Assert.AreEqual(initial, txt.Text);
			Assert.IsFalse(txt.MoveSelectedWord(false));
			Assert.AreEqual(initial, txt.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MoveSelectedWord method when the initial string is only a single word with
		/// a leading space and the insertion point is after the space.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveSelectedWord_OneWord_SpaceBefore_IPAfterSpace()
		{
			const string initial = " dog";
			TextBox txt = new TextBox();

			txt.Text = initial;
			txt.SelectionStart = 1;
			txt.SelectionLength = 0;
			Assert.IsFalse(txt.MoveSelectedWord(true));
			Assert.AreEqual(initial, txt.Text);
			Assert.IsTrue(txt.MoveSelectedWord(false));
			Assert.AreEqual("dog ", txt.Text);
			Assert.AreEqual(0, txt.SelectionStart);
			Assert.AreEqual(3, txt.SelectionLength);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MoveSelectedWord method when the initial string is only a single word with
		/// a leading space and the end of the word is selected.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveSelectedWord_OneWord_SpaceBefore_EndOfWordSelected()
		{
			const string initial = " dog";
			TextBox txt = new TextBox();

			txt.Text = initial;
			txt.SelectionStart = 2;
			txt.SelectionLength = 2;
			Assert.IsFalse(txt.MoveSelectedWord(true));
			Assert.AreEqual(initial, txt.Text);

			txt.Text = initial;
			txt.SelectionStart = 2;
			txt.SelectionLength = 2;
			Assert.IsTrue(txt.MoveSelectedWord(false));
			Assert.AreEqual("og d", txt.Text);
			Assert.AreEqual(0, txt.SelectionStart);
			Assert.AreEqual(2, txt.SelectionLength);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MoveSelectedWord method when the initial string is only a single word with
		/// a trailing space and the space is selected.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveSelectedWord_OneWord_SpaceAfter_SpaceSelected()
		{
			const string initial = "dog ";
			TextBox txt = new TextBox();

			txt.Text = initial;
			txt.SelectionStart = 3;
			txt.SelectionLength = 1;
			Assert.IsFalse(txt.MoveSelectedWord(true));
			Assert.AreEqual(initial, txt.Text);

			txt.Text = initial;
			txt.SelectionStart = 3;
			txt.SelectionLength = 1;
			Assert.IsTrue(txt.MoveSelectedWord(false));
			Assert.AreEqual(" dog", txt.Text);
			Assert.AreEqual(0, txt.SelectionStart);
			Assert.AreEqual(1, txt.SelectionLength);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MoveSelectedWord method when the initial string is only a single word with
		/// a trailing space and the word is selected.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveSelectedWord_OneWord_SpaceAfter_WordSelected()
		{
			const string initial = "dog ";
			TextBox txt = new TextBox();

			txt.Text = initial;
			txt.SelectionStart = 0;
			txt.SelectionLength = 3;
			Assert.IsTrue(txt.MoveSelectedWord(true));
			Assert.AreEqual(" dog", txt.Text);
			Assert.AreEqual(1, txt.SelectionStart);
			Assert.AreEqual(3, txt.SelectionLength);

			txt.Text = initial;
			txt.SelectionStart = 0;
			txt.SelectionLength = 3;
			Assert.IsFalse(txt.MoveSelectedWord(false));
			Assert.AreEqual(initial, txt.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MoveSelectedWord method when the initial string is only a single word with
		/// a trailing space and the insertion point is in the word.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveSelectedWord_OneWord_SpaceAfter_IPInWord()
		{
			const string initial = "dog ";
			TextBox txt = new TextBox();

			txt.Text = initial;
			txt.SelectionStart = 1;
			txt.SelectionLength = 0;
			Assert.IsTrue(txt.MoveSelectedWord(true));
			Assert.AreEqual(" dog", txt.Text);
			Assert.AreEqual(1, txt.SelectionStart);
			Assert.AreEqual(3, txt.SelectionLength);

			txt.Text = initial;
			txt.SelectionStart = 1;
			txt.SelectionLength = 0;
			Assert.IsFalse(txt.MoveSelectedWord(false));
			Assert.AreEqual(initial, txt.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MoveSelectedWord method when the initial string is only a single word with
		/// a trailing space and the insertion point is before the space.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveSelectedWord_OneWord_SpaceAfter_IPBeforeSpace()
		{
			const string initial = "dog ";
			TextBox txt = new TextBox();

			txt.Text = initial;
			txt.SelectionStart = 3;
			txt.SelectionLength = 0;
			Assert.IsTrue(txt.MoveSelectedWord(true));
			Assert.AreEqual(" dog", txt.Text);
			Assert.AreEqual(1, txt.SelectionStart);
			Assert.AreEqual(3, txt.SelectionLength);

			txt.Text = initial;
			txt.SelectionStart = 3;
			txt.SelectionLength = 0;
			Assert.IsFalse(txt.MoveSelectedWord(false));
			Assert.AreEqual(initial, txt.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MoveSelectedWord method when the initial string is only a single word with
		/// a trailing space and the insertion point is after the space.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveSelectedWord_OneWord_SpaceAfter_IPAfterSpace()
		{
			const string initial = "dog ";
			TextBox txt = new TextBox();

			txt.Text = initial;
			txt.SelectionStart = 4;
			txt.SelectionLength = 0;
			Assert.IsFalse(txt.MoveSelectedWord(true));
			Assert.AreEqual(initial, txt.Text);
			Assert.IsFalse(txt.MoveSelectedWord(false));
			Assert.AreEqual(initial, txt.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MoveSelectedWord method when the initial string is only a single word with
		/// a trailing space and the beginning of the word is selected.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveSelectedWord_OneWord_SpaceAfter_StartOfWordSelected()
		{
			const string initial = "dog ";
			TextBox txt = new TextBox();

			txt.Text = initial;
			txt.SelectionStart = 0;
			txt.SelectionLength = 2;
			Assert.IsTrue(txt.MoveSelectedWord(true));
			Assert.AreEqual("g do", txt.Text);
			Assert.AreEqual(2, txt.SelectionStart);
			Assert.AreEqual(2, txt.SelectionLength);

			txt.Text = initial;
			txt.SelectionStart = 0;
			txt.SelectionLength = 2;
			Assert.IsFalse(txt.MoveSelectedWord(false));
			Assert.AreEqual(initial, txt.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MoveSelectedWord method when a single word is selected in a multi-word
		/// string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveSelectedWord_Normal_SingleWordSelected_ForwardThenBackward()
		{
			const string initial = "The quick brown fox ate the poor wimpy dog.";
			TextBox txt = new TextBox();

			txt.Text = initial;
			txt.SelectionStart = 4;
			txt.SelectionLength = 5;
			Assert.IsTrue(txt.MoveSelectedWord(true));
			Assert.AreEqual("The brown quick fox ate the poor wimpy dog.", txt.Text);
			Assert.AreEqual(9, txt.SelectionStart);
			Assert.AreEqual(6, txt.SelectionLength);

			Assert.IsTrue(txt.MoveSelectedWord(false));
			Assert.AreEqual(initial, txt.Text);
			Assert.AreEqual(3, txt.SelectionStart);
			Assert.AreEqual(6, txt.SelectionLength);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MoveSelectedWord method when a single word is selected in a multi-word
		/// string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveSelectedWord_Normal_SingleWordSelected_BackwardThenForward()
		{
			const string initial = "The quick brown fox ate the poor wimpy dog.";
			TextBox txt = new TextBox();

			txt.Text = initial;
			txt.SelectionStart = 10;
			txt.SelectionLength = 5;
			Assert.IsTrue(txt.MoveSelectedWord(false));
			Assert.AreEqual("The brown quick fox ate the poor wimpy dog.", txt.Text);
			Assert.AreEqual(4, txt.SelectionStart);
			Assert.AreEqual(6, txt.SelectionLength);

			Assert.IsTrue(txt.MoveSelectedWord(true));
			Assert.AreEqual(initial, txt.Text);
			Assert.AreEqual(10, txt.SelectionStart);
			Assert.AreEqual(6, txt.SelectionLength);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MoveSelectedWord method to move a single selected word forward repeatedly
		/// to the end of a string that ends with punctuation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveSelectedWord_Normal_SingleWordSelected_ForwardToEnd_FinalPunctuation()
		{
			const string initial = "The quick brown fox ate\u0301 the poor wimpy dog.";
			TextBox txt = new TextBox();

			txt.Text = initial;
			int prevStart = txt.SelectionStart = 4;
			txt.SelectionLength = 5;
			int steps = 0;
			while (txt.MoveSelectedWord(true))
			{
				Assert.IsTrue(prevStart < txt.SelectionStart);
				Assert.AreEqual(6, txt.SelectionLength);
				prevStart = txt.SelectionStart;
				steps++;
			}
			Assert.AreEqual(7, steps);
			// 0         1         2         3         4
			// 01234567890123456789012345678901234567890123
			// The brown fox ate' the poor wimpy dog quick.
			Assert.AreEqual("The brown fox ate\u0301 the poor wimpy dog quick.", txt.Text);
			Assert.AreEqual(37, txt.SelectionStart);
			Assert.AreEqual(6, txt.SelectionLength);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MoveSelectedWord method to move a single selected word forward repeatedly
		/// to the end of a string that does not end with punctuation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveSelectedWord_Normal_SingleWordSelected_ForwardToEnd_NoPunctuation()
		{
			const string initial = "The quick brown fox ate the poor wimpy dog";
			TextBox txt = new TextBox();

			// Start with second word
			txt.Text = initial;
			int prevStart = txt.SelectionStart = 4;
			txt.SelectionLength = 5;
			int steps = 0;
			while (txt.MoveSelectedWord(true))
			{
				Assert.IsTrue(prevStart < txt.SelectionStart);
				Assert.AreEqual(6, txt.SelectionLength);
				prevStart = txt.SelectionStart;
				steps++;
			}
			Assert.AreEqual(7, steps);
			//               0         1         2         3         4
			//               0123456789012345678901234567890123456789012
			Assert.AreEqual("The brown fox ate the poor wimpy dog quick", txt.Text);
			Assert.AreEqual(36, txt.SelectionStart);
			Assert.AreEqual(6, txt.SelectionLength);

			// Start with first word
			txt.Text = initial;
			prevStart = txt.SelectionStart = 0;
			txt.SelectionLength = 3;
			steps = 0;
			while (txt.MoveSelectedWord(true))
			{
				Assert.IsTrue(prevStart < txt.SelectionStart);
				Assert.AreEqual(4, txt.SelectionLength);
				prevStart = txt.SelectionStart;
				steps++;
			}
			Assert.AreEqual(8, steps);
			//               0         1         2         3         4
			//               0123456789012345678901234567890123456789012
			Assert.AreEqual("quick brown fox ate the poor wimpy dog The", txt.Text);
			Assert.AreEqual(38, txt.SelectionStart);
			Assert.AreEqual(4, txt.SelectionLength);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MoveSelectedWord method to move a single selected word backward repeatedly
		/// to the start of a string that begins with punctuation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveSelectedWord_Normal_SingleWordSelected_BackwardToStart_InitialPunctuation()
		{
			// 0         1         2         3         4
			// 01234567890123456789012345678901234567890123456
			// ?Por que' comio' el zorro al pobre perro debil?
			const string initial = "\u00BFPor que\u0301 comio\u0301 el zorro al pobre perro debil?";
			TextBox txt = new TextBox();

			txt.Text = initial;
			int prevStart = txt.SelectionStart = 41;
			txt.SelectionLength = 5;
			int steps = 0;
			while (txt.MoveSelectedWord(false))
			{
				Assert.IsTrue(prevStart > txt.SelectionStart);
				Assert.AreEqual(6, txt.SelectionLength);
				prevStart = txt.SelectionStart;
				steps++;
			}
			Assert.AreEqual(8, steps);
			Assert.AreEqual("\u00BFdebil Por que\u0301 comio\u0301 el zorro al pobre perro?", txt.Text);
			Assert.AreEqual(1, txt.SelectionStart);
			Assert.AreEqual(6, txt.SelectionLength);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MoveSelectedWord method to move a single selected word backward repeatedly
		/// to the start of a string that does not begin with punctuation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveSelectedWord_Normal_SingleWordSelected_BackwardToStart_NoPunctuation()
		{
			//                      0         1         2         3         4
			//                      0123456789012345678901234567890123456789012
			const string initial = "The quick brown fox ate the poor wimpy dog";
			TextBox txt = new TextBox();

			// Start with second-to-last word
			txt.Text = initial;
			int prevStart = txt.SelectionStart = 33;
			txt.SelectionLength = 5;
			int steps = 0;
			while (txt.MoveSelectedWord(false))
			{
				Assert.IsTrue(prevStart > txt.SelectionStart);
				Assert.AreEqual(6, txt.SelectionLength);
				prevStart = txt.SelectionStart;
				steps++;
			}
			Assert.AreEqual(7, steps);
			Assert.AreEqual("wimpy The quick brown fox ate the poor dog", txt.Text);
			Assert.AreEqual(0, txt.SelectionStart);
			Assert.AreEqual(6, txt.SelectionLength);

			// Start with last word
			txt.Text = initial;
			prevStart = txt.SelectionStart = 39;
			txt.SelectionLength = 3;
			steps = 0;
			while (txt.MoveSelectedWord(false))
			{
				Assert.IsTrue(prevStart > txt.SelectionStart);
				Assert.AreEqual(4, txt.SelectionLength);
				prevStart = txt.SelectionStart;
				steps++;
			}
			Assert.AreEqual(8, steps);
			//               0         1         2         3         4
			//               0123456789012345678901234567890123456789012
			Assert.AreEqual("dog The quick brown fox ate the poor wimpy", txt.Text);
			Assert.AreEqual(0, txt.SelectionStart);
			Assert.AreEqual(4, txt.SelectionLength);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MoveSelectedWord method when a word immediately preceding a comma is
		/// selected.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveSelectedWord_MoveSelectedWordForwardPastComma()
		{
			//                      0         1         2         3         4
			//                      0123456789012345678901234567890123456789012
			const string initial = "The quick brown fox ate the poor, wimpy dog.";
			TextBox txt = new TextBox();

			txt.Text = initial;
			txt.SelectionStart = 28;
			txt.SelectionLength = 4;
			Assert.IsTrue(txt.MoveSelectedWord(true));
			//               0         1         2         3         4
			//               0123456789012345678901234567890123456789012
			Assert.AreEqual("The quick brown fox ate the, poor wimpy dog.", txt.Text);
			Assert.AreEqual(28, txt.SelectionStart);
			Assert.AreEqual(5, txt.SelectionLength);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MoveSelectedWord method when the insertion point is in a word immediately
		/// preceding a comma.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveSelectedWord_IpInWordBeforeComma()
		{
			//                      0         1         2         3         4
			//                      0123456789012345678901234567890123456789012
			const string initial = "The quick brown fox ate the poor, wimpy dog.";
			TextBox txt = new TextBox();

			txt.Text = initial;
			txt.SelectionStart = 29;
			txt.SelectionLength = 0;
			Assert.IsTrue(txt.MoveSelectedWord(true));
			//               0         1         2         3         4
			//               0123456789012345678901234567890123456789012
			Assert.AreEqual("The quick brown fox ate the wimpy poor, dog.", txt.Text);
			Assert.AreEqual(33, txt.SelectionStart);
			Assert.AreEqual(6, txt.SelectionLength);

			txt.Text = initial;
			txt.SelectionStart = 29;
			txt.SelectionLength = 0;
			Assert.IsTrue(txt.MoveSelectedWord(false));
			//               0         1         2         3         4
			//               0123456789012345678901234567890123456789012
			Assert.AreEqual("The quick brown fox ate poor, the wimpy dog.", txt.Text);
			Assert.AreEqual(24, txt.SelectionStart);
			Assert.AreEqual(6, txt.SelectionLength);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MoveSelectedWord method when a word immediately preceding a comma is
		/// selected.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveSelectedWord_MoveSelectedWordBackwardPastComma()
		{
			//                      0         1         2         3         4
			//                      0123456789012345678901234567890123456789012
			const string initial = "The quick brown fox ate the poor, wimpy dog.";
			TextBox txt = new TextBox();

			txt.Text = initial;
			txt.SelectionStart = 34;
			txt.SelectionLength = 5;
			Assert.IsTrue(txt.MoveSelectedWord(false));
			//               0         1         2         3         4
			//               0123456789012345678901234567890123456789012
			Assert.AreEqual("The quick brown fox ate the wimpy poor, dog.", txt.Text);
			Assert.AreEqual(28, txt.SelectionStart);
			Assert.AreEqual(6, txt.SelectionLength);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MoveSelectedWord method when a word immediately following sentence-initial
		/// punctuation is selected.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveSelectedWord_AdvanceWordFollowingSentenceInitialPunc()
		{
			//                      0         1         2         3         4
			//                      0123456789012345678901234567890123456789012
			const string initial = "\u00BFnoto\u0301 Que\u0301 Potifar Jose\u0301 Potifar familia?";
			TextBox txt = new TextBox();

			txt.Text = initial;
			txt.SelectionStart = 1;
			txt.SelectionLength = 5;
			Assert.IsTrue(txt.MoveSelectedWord(true));
			Assert.AreEqual("\u00BFQue\u0301 noto\u0301 Potifar Jose\u0301 Potifar familia?", txt.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MoveSelectedWord method when sentence-initial punctuation is selected.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveSelectedWord_AdvanceInitialPunc()
		{
			//                      0         1         2         3         4
			//                      0123456789012345678901234567890123456789012
			const string initial = "\u00BFQue\u0301 le dijo Potifar a Jose\u0301?";
			TextBox txt = new TextBox();

			txt.Text = initial;
			txt.SelectionStart = 0;
			txt.SelectionLength = 1;
			Assert.IsTrue(txt.MoveSelectedWord(true));
			//               0         1         2         3         4
			//               0123456789012345678901234567890123456789012
			Assert.AreEqual("Que\u0301 \u00BFle dijo Potifar a Jose\u0301?", txt.Text);
		}
	}
}

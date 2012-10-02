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
// File: TextBoxExtensions.cs
// ---------------------------------------------------------------------------------------------
using System;
using System.Linq;
using System.Windows.Forms;

namespace SIL.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class TextBoxExtensions
	{
		public static bool MoveSelectedWord(this TextBoxBase txt, bool forward)
		{
			if (txt.TextLength == 0)
				return false;
			int selStart = txt.SelectionStart;
			int selLim = selStart + txt.SelectionLength;
			if (selStart == selLim)
			{
				while (selStart > 0 && !Char.IsWhiteSpace(txt.Text[selStart - 1]))
					selStart--;
				while (selLim < txt.TextLength && !Char.IsWhiteSpace(txt.Text[selLim]))
					selLim++;
				if (selStart == selLim)
					return false;
			}
			int attemptsToIncluceWhitespace = 0;
			bool lookBeforeWord = forward;
			while (!Char.IsWhiteSpace(txt.Text[selStart]) && !Char.IsWhiteSpace(txt.Text[selLim - 1]) && attemptsToIncluceWhitespace < 2)
			{
				if (lookBeforeWord)
				{
					if (selStart > 1 && Char.IsWhiteSpace(txt.Text[selStart - 1]))
						selStart--;
				}
				else
				{
					if (selLim < txt.TextLength - 1 && Char.IsWhiteSpace(txt.Text[selLim]))
						selLim++;
				}
				lookBeforeWord = !lookBeforeWord;
				attemptsToIncluceWhitespace++;
			}

			int selLength = selLim - selStart;
			string sMove = txt.Text.Substring(selStart, selLength);
			int ichDest;
			if (forward)
			{
				ichDest = selLim;

				//First move past any whitespace immediately following the selection.
				while (ichDest < txt.TextLength && Char.IsWhiteSpace(txt.Text[ichDest]))
					ichDest++;

				int adj = (Char.IsWhiteSpace(sMove[0])) ? 0 : 1;
				int test = ichDest;
				while (ichDest < txt.TextLength && !Char.IsWhiteSpace(txt.Text[test]) &&
					(!Char.IsPunctuation(txt.Text[test]) || txt.Text.Skip(test + 1).Any(ch => !Char.IsPunctuation(ch))))
				{
					ichDest++;
					test = ichDest - adj;
				}

				if (ichDest == selLim)
					return false; // Didn't find a new place to jump to

				// Now see if we need to move the space to the other side of the word
				if (Char.IsWhiteSpace(sMove[selLength - 1]) &&
					(ichDest == txt.TextLength || Char.IsPunctuation(txt.Text[ichDest])))
				{
					sMove = sMove[selLength - 1] + sMove.Substring(0, selLength - 1);
				}

				// Adjust insertion location by the number of characters that will be moved.
				ichDest -= selLength;
			}
			else
			{
				ichDest = selStart;

				//First move past any whitespace immediately preceding the selection.
				while (ichDest > 0 && Char.IsWhiteSpace(txt.Text[ichDest - 1]))
					ichDest--;

				int adj = (Char.IsWhiteSpace(sMove[selLength - 1])) ? 0 : 1;
				int test = ichDest - 1;
				while (ichDest > 0 && !Char.IsWhiteSpace(txt.Text[test]) &&
					(!Char.IsPunctuation(txt.Text[ichDest - 1]) || (ichDest > 1 && txt.Text.Take(ichDest - 1).Any(ch => !Char.IsPunctuation(ch)))))
				{
					ichDest--;
					test = ichDest - 1 + adj;
				}

				if (ichDest == selStart)
					return false; // Didn't find a new place to jump to

				// Now see if we need to move the space to the other side of the word
				if (Char.IsWhiteSpace(sMove[0]) &&
					(ichDest == 0 || Char.IsPunctuation(txt.Text[ichDest - 1])))
				{
					sMove = sMove.Substring(1, selLength - 1) + sMove[0];
				}
			}

			txt.Text = txt.Text.Remove(selStart, selLength).Insert(ichDest, sMove);
			txt.SelectionStart = ichDest;
			txt.SelectionLength = selLength;
			return true;
		}
	}
}

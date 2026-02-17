// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using System.Diagnostics;
using System.IO;
using SIL.FieldWorks.Common.FwUtils;

namespace SILUBS.ScriptureChecks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Unit tests for the PunctuationCheck class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public abstract class ScrChecksTestBase
	{
		protected List<RecordErrorEventArgs> m_errors;
		protected IScriptureCheck m_check;

		#region Setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test setup (runs before each test).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public virtual void TestSetup()
		{
			m_errors = new List<RecordErrorEventArgs>();
			// This is worthless, so we won't do it: dataSource.GetText(0, 0);
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Record errors returned by a check. The check calls this delegate whenever an error
		/// is found.
		/// </summary>
		/// <param name="args">Information about the potential inconsistency being reported</param>
		/// ------------------------------------------------------------------------------------
		public void RecordError(RecordErrorEventArgs args)
		{
			m_errors.Add(args);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks the error.
		/// </summary>
		/// <param name="iError">The index of the error.</param>
		/// <param name="tokenText">The text of the token containing the error.</param>
		/// <param name="offset">The offset to the start of the problem data.</param>
		/// <param name="problemData">The invalid punctuation pattern, character, improperly
		/// capitalized word, repeated word, etc.</param>
		/// <param name="errorMessage">The error message.</param>
		/// ------------------------------------------------------------------------------------
		protected void CheckError(int iError, string tokenText, int offset, string problemData,
			string errorMessage)
		{
			//int length = problemData.Length;
			//StringBuilder bldr = new StringBuilder();
			//for (int iTok = 0; iTok < m_errors[iError].toks.Count; iTok++)
			//{
			//    ITextToken tok = m_errors[iError].toks[iTok];
			//    Assert.That(tok.Text, Is.EqualTo(tokenText[iTok]));
			//    if (iTok > 0 && (tok.TextType == TextType.VerseNumber ||
			//        tok.TextType == TextType.ChapterNumber))
			//    {
			//        continue;
			//    }
			//    if (offset + length > tok.Text.Length)
			//    {
			//        string substring = tok.Text.Substring(offset);
			//        bldr.Append(substring);
			//        offset = 0;
			//        length -= substring.Length;
			//    }
			//    else
			//    {
			//        bldr.Append(tok.Text.Substring(offset, length));
			//        Assert.That(iTok, Is.EqualTo(m_errors[iError].toks.Count -1), "We've now found enough characters, so there should be no more tokens");
			//    }
			//}
			//Assert.That(bldr.ToString(), Is.EqualTo(problemData));

			Assert.That(m_errors[iError].Tts.FirstToken.Text, Is.EqualTo(tokenText));
			Assert.That(m_errors[iError].Tts.Text, Is.EqualTo(problemData));
			Assert.That(m_errors[iError].Tts.Offset, Is.EqualTo(offset));
			Assert.That(m_errors[iError].CheckId, Is.EqualTo(m_check.CheckId));
			Assert.That(m_errors[iError].Tts.Message, Is.EqualTo(errorMessage));
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the IScrCheckInventory interface for the check (or null if the check does not
		/// implement this interface).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected IScrCheckInventory CheckInventory
		{
			get { return m_check as IScrCheckInventory; }
		}
		#endregion
	}
}

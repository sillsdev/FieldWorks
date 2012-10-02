using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using System.Diagnostics;
using System.IO;
using SILUBS.SharedScrUtils;

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
			//    Assert.AreEqual(tokenText[iTok], tok.Text);
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
			//        Assert.AreEqual(m_errors[iError].toks.Count -1, iTok, "We've now found enough characters, so there should be no more tokens");
			//    }
			//}
			//Assert.AreEqual(problemData, bldr.ToString());

			Assert.AreEqual(tokenText, m_errors[iError].Tts.FirstToken.Text);
			Assert.AreEqual(problemData, m_errors[iError].Tts.Text);
			Assert.AreEqual(offset, m_errors[iError].Tts.Offset);
			Assert.AreEqual(m_check.CheckId, m_errors[iError].CheckId);
			Assert.AreEqual(errorMessage, m_errors[iError].Tts.Message);
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

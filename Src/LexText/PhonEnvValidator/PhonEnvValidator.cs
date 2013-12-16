// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: PhonEnvValidator.cs
// Responsibility: AndyBlack
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Diagnostics;
using System.Collections;
using System.Text;

using Tools;
using System.Xml;

namespace SIL.FieldWorks.FDO.Validation
{
	/// <summary>
	/// Summary description for PhonEnvRecognizer.
	/// </summary>
	public class PhonEnvRecognizer
	{
		private bool m_fSuccess;
		private string m_sErrorMessage;
		private PhonEnvParser m_parser;
		private string[] m_saNaturalClasses;
		private string[] m_saSegments;


		/// <summary>
		/// Constructor.
		/// </summary>
		public PhonEnvRecognizer()
		{
		}
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="saNaturalClasses"></param>
		/// <param name="saSegments"></param>
		public PhonEnvRecognizer(string[] saSegments, string[] saNaturalClasses)
		{
			m_saSegments = saSegments;
			m_saNaturalClasses = saNaturalClasses;
		}


		/// <summary>
		/// Reset the segments.
		/// </summary>
		/// <param name="saSegments">The new set of segments.</param>
		public void ResetSegments(string[] saSegments)
		{
			m_parser.ResetSegments(saSegments);

		}

		/// <summary>
		/// Reset the set of natural classes.
		/// </summary>
		/// <param name="saNaturalClasses"></param>
		public void ResetNaturalClasses(string[] saNaturalClasses)
		{
			m_parser.ResetNaturalClasses(saNaturalClasses);

		}

		/// <summary>
		/// Attempt to recognize the given phonological environment string
		/// </summary>
		/// <param name="sEnvironment">phonological environment to recognize</param>
		/// <returns>true if environment is recognized; false otherwise</returns>
		public bool Recognize(string sEnvironment)
		{
			m_fSuccess = false; // Start distrustful.
			try
			{
				InitParser(sEnvironment, m_saNaturalClasses, m_saSegments);
				m_parser.Parse(sEnvironment);
				if (m_parser.Success)
					m_fSuccess = true;
				else
					m_sErrorMessage = m_parser.ErrorMessage;
			}
			catch (CSToolsException exc)
			{
				StringBuilder sb = new StringBuilder();
				if (m_parser.ErrorMessage == null)
				{
					sb.Append("<phonEnv status=\"syntax\" pos=\"");
					if (m_parser.Position != -1)
						sb.Append(m_parser.Position.ToString());
					else
						sb.Append(exc.nChar.ToString());
					sb.Append("\" syntaxErrType=\"");
					sb.Append(m_parser.SyntaxErrorType.ToString());
					sb.Append("\">");
					// Fix the string to be safe for XML.
					if (sEnvironment != null && sEnvironment != "")
					{
						sEnvironment = sEnvironment.Replace("&", "&amp;");
						sEnvironment = sEnvironment.Replace("<", "&lt;");
						sEnvironment = sEnvironment.Replace(">", "&gt;");
					}
					sb.Append(sEnvironment);
					sb.Append("</phonEnv>");
				}
				else
				{
					sb.Append(m_parser.ErrorMessage);
				}
				m_sErrorMessage = sb.ToString();
				m_parser.Success = false;
			}
			return m_fSuccess;
		}

		private void InitParser(string sEnvironment, string[] saNaturalClasses, string[] saSegments)
		{
			m_parser = new PhonEnvParser();
			m_parser.m_symbols.erh.throwExceptions = true;
#if TestingOnly
			m_parser.m_debug = true;
#endif
			m_parser.ResetNaturalClasses(saNaturalClasses);
			m_parser.ResetSegments(saSegments);
			// The generated parser needs to know the environment string
			// in order to build an error message
			// when an undefined natural class or segment is discovered.
			m_parser.Input = sEnvironment;
			// We assume it will parse successfully.
			// (This is one way to keep track of this,
			// given the nature of the parser generator we're using.)
			m_parser.Success = true;
			m_parser.SyntaxErrorType = SIL.FieldWorks.FDO.Validation.PhonEnvParser.SyntaxErrType.unknown;
			m_parser.Position = -1;
		}

		/// <summary>
		/// Get error message.
		/// </summary>
		public string ErrorMessage
		{
			get { return m_sErrorMessage; }
		}

		/// <summary>
		/// Get result of recognizer.
		/// </summary>
		public bool Result
		{
			get { return m_fSuccess; }
		}

		/// <summary>
		/// Determine whether an optional item or a natural class can be inserted at the
		/// location given by ichEnd and ichAnchor.
		/// </summary>
		public static bool CanInsertItem(string sEnv, int ichEnd, int ichAnchor)
		{
			Debug.Assert(sEnv != null);
			Debug.Assert(ichEnd <= sEnv.Length && ichAnchor <= sEnv.Length);
			if (ichEnd < 0 || ichAnchor < 0)
				return false;
			int ichSlash = sEnv.IndexOf('/');
			if ((ichSlash < 0) || (ichEnd <= ichSlash) || (ichAnchor <= ichSlash))
				return false;
			// ensure that ichAnchor <= ichEnd.
			int ichT = ichAnchor;
			ichAnchor = Math.Min(ichAnchor, ichEnd);
			ichEnd = Math.Max(ichT, ichEnd);
			int ichHash = sEnv.IndexOf('#');
			if (ichHash < 0)
				return true;
			int ichHash2 = sEnv.IndexOf('#', ichHash + 1);
			if (ichHash2 >= 0)
			{
				// With 2 #, must be between them, or straddling at least one of them.
				if (ichAnchor <= ichHash)
					return (ichEnd > ichHash);
				else if (ichEnd > ichHash2)
					return (ichAnchor <= ichHash2);
				else
					return true;
			}
			else
			{
				// With 1 #, must be on same side as the _, or straddling the #.
				int ichBar = sEnv.IndexOf('_');
				if (ichBar < 0)
					return true;
				if (ichBar < ichHash)
					return (ichAnchor <= ichHash);
				else
					return (ichEnd > ichHash);
			}
		}

		/// <summary>
		/// Determine whether a hash mark (#) can be inserted at the location given by ichEnd
		/// and ichAnchor.
		/// </summary>
		public static bool CanInsertHashMark(string sEnv, int ichEnd, int ichAnchor)
		{
			Debug.Assert(sEnv != null);
			Debug.Assert(ichEnd <= sEnv.Length && ichAnchor <= sEnv.Length);
			if (ichEnd < 0 || ichAnchor < 0)
				return false;
			int ichSlash = sEnv.IndexOf('/');
			if ((ichSlash < 0) || (ichEnd <= ichSlash) || (ichAnchor <= ichSlash))
				return false;
			// ensure that ichAnchor <= ichEnd.
			int ichT = ichAnchor;
			ichAnchor = Math.Min(ichAnchor, ichEnd);
			ichEnd = Math.Max(ichT, ichEnd);
			// Check whether ichAnchor is at the beginning of the environment (after the /).
			bool fBegin = CheckForOnlyWhiteSpace(sEnv, ichSlash + 1, ichAnchor);
			// Check whether ichEnd is at the end of the environment.
			bool fEnd = CheckForOnlyWhiteSpace(sEnv, ichEnd, sEnv.Length);
			if (!fBegin && !fEnd)
				return false;	// we must be at the beginning or end!
			int ichHash = sEnv.IndexOf('#');
			if (ichHash >= 0)
			{
				// At least one # exists, look for another.
				int ichHash2 = sEnv.IndexOf('#', ichHash + 1);
				if (ichHash2 < 0)
				{
					// Only 1 # exists, check we're on the opposite side of the _.
					int ichBar = sEnv.IndexOf('_');
					if (ichBar < 0)
					{
						// No _, so we have to analyze the position of the existing #.
						// If we have an illegal #, don't allow the new # unless the old one is
						// being replaced.
						bool fBeginningHash = CheckForOnlyWhiteSpace(sEnv, ichSlash + 1, ichHash);
						bool fEndingHash = CheckForOnlyWhiteSpace(sEnv, ichHash + 1, sEnv.Length);
						if (fBeginningHash)
							return fEnd;
						else if (fEndingHash)
							return fBegin;
						else
							return (ichAnchor <= ichHash) && (ichEnd > ichHash);
					}
					else if (ichBar < ichHash)
					{
						return fBegin;
					}
					else
					{
						return fEnd;
					}
				}
				else
				{
					// Only 2 # may ever exist!
					return false;
				}
			}
			else
			{
				return true;
			}
		}

		private static bool CheckForOnlyWhiteSpace(string sEnv, int ichFirst, int ichLast)
		{
			int cch = ichLast - ichFirst;
			if (cch > 0)
			{
				char[] rgch = sEnv.ToCharArray(ichFirst, cch);
				for (int ich = 0; ich < rgch.Length; ++ich)
				{
					if (!System.Char.IsWhiteSpace(rgch[ich]))
					{
						return false;
					}
				}
			}
			return true;
		}

		/// <summary>
		/// Convert XML message returned from environ validator to English
		/// </summary>
		/// <param name="strRep1">The environment string itself</param>
		/// <param name="sXmlMessage">XML returned from validator</param>
		/// <param name="pos">position value</param>
		/// <param name="sMessage">The created message</param>
		public static void CreateErrorMessageFromXml(string strRep1, string sXmlMessage, out int pos, out string sMessage)
		{
			string strRep = strRep1;
			if (strRep1 == null)
				strRep = "";
			XmlDocument xdoc = new XmlDocument();
			string sStatus = "";
			pos = 0;
			try
			{
				// The validator message, unfortunately, may be invalid XML if
				// there were XML reserved characters in the environment.
				// until we get that fixed, at least don't crash, just draw squiggly under the entire word
				xdoc.LoadXml(sXmlMessage);
				XmlAttribute posAttr = xdoc.DocumentElement.Attributes["pos"];
				pos = (posAttr != null) ? Convert.ToInt32(posAttr.Value) : 0;
				XmlAttribute statusAttr = xdoc.DocumentElement.Attributes["status"];
				sStatus = statusAttr.InnerText;
			}
			catch
			{
				// Eat the exception.
			}
			int len = strRep.Length;
			if (pos >= len)
				pos = Math.Max(0, len - 1); // make sure something will show
			//todo: if the string itself will be part of this message, this needs
			// to put the right places in the right writing systems. note that
			//there is a different constructor we can use which takes a sttext.
			StringBuilder bldrMsg = new StringBuilder();
			bldrMsg.AppendFormat(SIL.FieldWorks.Validator.Strings.ksBadEnv, strRep);
			if (sStatus == "class")
			{
				int iRightBracket = strRep.Substring(pos).IndexOf(']');
				string sClass = strRep.Substring(pos, iRightBracket);
				bldrMsg.AppendFormat(SIL.FieldWorks.Validator.Strings.ksBadClassInEnv, sClass);
			}
			if (sStatus == "segment")
			{
				string sPhoneme = strRep.Substring(pos);
				bldrMsg.AppendFormat(SIL.FieldWorks.Validator.Strings.ksBadPhonemeInEnv, sPhoneme);
			}
			if (sStatus == "missingClosingParen")
			{
				bldrMsg.AppendFormat(SIL.FieldWorks.Validator.Strings.ksMissingCloseParenInEnv, strRep.Substring(pos));
			}
			if (sStatus == "missingOpeningParen")
			{
				bldrMsg.AppendFormat(SIL.FieldWorks.Validator.Strings.ksMissingOpenParenInEnv, strRep.Substring(pos));
			}
			if (sStatus == "missingClosingSquareBracket")
			{
				bldrMsg.AppendFormat(SIL.FieldWorks.Validator.Strings.ksMissingCloseBracketInEnv, strRep.Substring(pos));
			}
			if (sStatus == "missingOpeningSquareBracket")
			{
				bldrMsg.AppendFormat(SIL.FieldWorks.Validator.Strings.ksMissingOpenBracketInEnv, strRep.Substring(pos));
			}
			if (sStatus == "syntax")
			{
				bldrMsg.AppendFormat(SIL.FieldWorks.Validator.Strings.ksBadEnvSyntax, strRep.Substring(pos));
			}
			sMessage = bldrMsg.ToString();
		}
	}
}

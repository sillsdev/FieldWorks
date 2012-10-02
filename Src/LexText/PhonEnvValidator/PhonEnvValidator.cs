// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: PhonEnvValidator.cs
// Responsibility: AndyBlack
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Collections;
using System.Text;

using Tools;

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
				SYMBOL ast = m_parser.Parse(sEnvironment);
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
	}
}

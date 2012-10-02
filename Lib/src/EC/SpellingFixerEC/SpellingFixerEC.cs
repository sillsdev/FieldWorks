using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.ComponentModel;            // for Description
using System.Drawing;                   // for Font
using System.IO;                        // for File
using System.Reflection;                // for DefaultMemberAttribute
using System.Text;                      // for Encoding
using System.Data;                      // for DataTable
using System.Diagnostics;               // for Debug
using System.CodeDom.Compiler;          // for TempFileCollection
using ECInterfaces;
using SilEncConverters40;

namespace SpellingFixerEC
{
	/// <summary>
	/// An EncConverter plug-in to help with a Spelling fixer helper
	/// </summary>
	[DefaultMemberAttribute("SpellFixerEncConverterName")]
	[Description("When instantiated, this object will query the user for the project to use (e.g. Hindi) for subsequent 'AssignCorrectSpelling' calls. Make this object global scope to keep from having to select the project with each usage."), Category("Data")]
	[ComVisible(true)]
	[ClassInterface(ClassInterfaceType.AutoDual)]
	public class SpellingFixerEC
	{
		public const string cstrAttributeFontToUse = "SpellingFixer Display Font";
		public const string cstrAttributeFontSizeToUse = "SpellingFixer Display Font Size";
		public const string cstrAttributeWordBoundaryDelimiter = "SpellingFixer Word Boundary Delimiter";
		public const string cstrAttributeNonWordChars = "SpellingFixer punctuation and whitespace characters";
		public const string cstrDefaultPunctuationAndWhitespace = "' ' tab nl '.' ',' '!' ':' ';' '-' \"'\" '\"' '‘' '’' '“' '”' '(' ')' '[' ']' '{' '}'";
		public const string cstrV3DefaultPunctuationAndWhitespaceAdds = " '?'";
		public const string cstrDefaultWordBoundaryDelimiter = "#";

		public const string cstrSFConverterPrefix = "Clean Spelling for ";
		internal const string cstrCaption = "Spelling Fixer";
		internal const string strQuotedFormat = "\"{0}\"";
		private const string cstrPrecWhiteSpace = "prec(ws)";
		private const string cstrFollWhiteSpace = "fol(ws)";
		private const string cstrIndentation = "    ";
		private const string cstrCommentFormat = " c rule added while fixing: '{0}'";
		private const char chSpace = ' ';

		private System.Drawing.Font m_font;
		private string m_strWordBoundaryDelimiter;
		private string m_strNonWordChars;
		private string m_strConverterSpec;
		private string m_strEncConverterName;
		private bool m_bLegacy;
		private int m_cp = 1252;

		// leave a default constructor which *doesn't* automatically log-in to a project for
		//  COM clients that want to use CscProject via SelectProject below.
		public SpellingFixerEC()
		{
		}

		/// <summary>
		/// If you use the default ctor, and want to log into a SpellFixer project (as opposed to CscProject)
		/// then use this method.
		/// </summary>
		public void LoginProject()
		{
			LoginSF login = new LoginSF();
			if (login.ShowDialog() == DialogResult.OK)
			{
				m_font = login.FontToUse;
				m_bLegacy = login.IsLegacy;
				if (m_bLegacy)
					m_cp = login.CpToUse;
				m_strConverterSpec = login.ConverterSpec;
				SpellFixerEncConverterName = login.EncConverterName;
				WordBoundaryDelimiter = login.WordBoundaryDelimiter;
				PunctuationAndWhiteSpace = login.Punctuation;
			}
			else
				throw new ExternalException("No project selected");
		}

		public SpellingFixerEC(string strProjectName)
		{
			LoginSF login = new LoginSF();
			if (login.LoadProject(strProjectName))
			{
				m_font = login.FontToUse;
				m_bLegacy = login.IsLegacy;
				if (m_bLegacy)
					m_cp = login.CpToUse;
				m_strConverterSpec = login.ConverterSpec;
				SpellFixerEncConverterName = login.EncConverterName;
				WordBoundaryDelimiter = login.WordBoundaryDelimiter;
				PunctuationAndWhiteSpace = login.Punctuation;
			}
			else
				throw new ExternalException("No project selected");
		}

		public SpellingFixerEC(string strProjectName, Font font, string strConverterSpec, string strEncConverterName,
			[Optional, DefaultParameterValue(SpellingFixerEC.cstrDefaultWordBoundaryDelimiter)] string strWordBoundaryDelimiter,
			[Optional, DefaultParameterValue(cstrDefaultPunctuationAndWhitespace)] string strPunctuationAndWhiteSpace,
			[Optional, DefaultParameterValue(false)] bool bLegacy, [Optional, DefaultParameterValue(1252)] int cp)
		{
			m_font = font;
			m_bLegacy = bLegacy;
			if (m_bLegacy)
				m_cp = cp;
			m_strConverterSpec = strConverterSpec;
			SpellFixerEncConverterName = strEncConverterName;
			WordBoundaryDelimiter = strWordBoundaryDelimiter;
			PunctuationAndWhiteSpace = strPunctuationAndWhiteSpace;
		}

		// somehow, I forgot the question mark in v1-2. I can't change the const string because it's
		//  used to find the string in existing tables (via IndexOf) and if I add it there, it'll fail :-(
		public static string GetDefaultPunctuation
		{
			get { return cstrDefaultPunctuationAndWhitespace + cstrV3DefaultPunctuationAndWhitespaceAdds; }
		}

		[Description("The EncConverters process type flag for the SpellingFixerEC converters."), Category("Data")]
		static public ProcessTypeFlags SFProcessType
		{
			get { return ProcessTypeFlags.SpellingFixerProject; }
		}

		[Description("Returns the name of the EncConverter to use to correct the spelling for the selected project."), Category("Data")]
		public string SpellFixerEncConverterName
		{
			get { return m_strEncConverterName; }
			set { m_strEncConverterName = value; }
		}

		[Description("Returns the font associated with the selected project."), Category("Data")]
		public Font ProjectFont
		{
			get { return m_font; }
		}

		[Description("Returns the instance of the IEncConverter interface to use to correct the spelling for the selected project."), Category("Data")]
		public IEncConverter SpellFixerEncConverter
		{
			get
			{
				if (m_strEncConverterName == null)
					return null;

				EncConverters aECs = new EncConverters();
				IEncConverter aEC = null;
				if (aECs.ContainsKey(m_strEncConverterName))
					aEC = aECs[m_strEncConverterName];
				else
				{
					aEC = new CcEncConverter();
					string strDummy = null;
					int nProcType = 0;
					ConvType eConvType = (m_bLegacy) ? ConvType.Legacy_to_Legacy : ConvType.Unicode_to_Unicode;
					aEC.Initialize(m_strEncConverterName, m_strConverterSpec, ref strDummy, ref strDummy,
						ref eConvType, ref nProcType, m_cp, m_cp, true);
				}

				return aEC;
			}
		}

		private string WordBoundaryDelimiter
		{
			get { return m_strWordBoundaryDelimiter; }
			set { m_strWordBoundaryDelimiter = value; }
		}

		private string PunctuationAndWhiteSpace
		{
			get { return m_strNonWordChars; }
			set { m_strNonWordChars = value; }
		}

		[Description("Call this method with the misspelled word and it will prompt you for the corrected spelling, BUT only if the table is empty (to avoid the CC error of attempting to Convert on an empty table)."), Category("Action")]
		public void QueryForSpellingCorrectionIfTableEmpty(string strBadWord)
		{
			// this is just a convenience method, so if things aren't configured correctly, just exit
			if (String.IsNullOrEmpty(m_strConverterSpec))
				return;

			// even if the file exists, it might have no rules, so double-check
			if (File.Exists(m_strConverterSpec))
			{
				DataTable myTable;
				Encoding enc = GetEncoding;
				if (    !InitializeDataTableFromCCTable(m_strConverterSpec, enc, WordBoundaryDelimiter, out myTable)
					||  (myTable.Rows.Count > 0))
				{
					return; // don't query for a record if there are already spelling corrections in the file
				}
			}
			else
			{
				LoginSF.CreateCCTable(m_strConverterSpec, SpellFixerEncConverterName, PunctuationAndWhiteSpace, null, !this.m_bLegacy);
			}

			QueryAndAppend(strBadWord);
		}

		protected void QueryAndAppend(string strBadWord)
		{
			QueryGoodSpelling aQuery = new QueryGoodSpelling(m_font);
			if (aQuery.ShowDialog(strBadWord, strBadWord, strBadWord, false) == DialogResult.OK)
			{
				// if it was legacy encoded, then we need to convert the data to narrow using
				//  the code page the user specified (or we got out of the repository)
				Encoding enc = GetEncoding;

				// get a stream writer for these encoding and append
				StreamWriter sw = new StreamWriter(m_strConverterSpec, true, enc);
				sw.WriteLine(FormatSubstitutionRule(aQuery.BadSpelling, aQuery.GoodSpelling, WordBoundaryDelimiter, strBadWord));
				sw.Flush();
				sw.Close();
			}
		}

		[Description("Call this method with the misspelled word and it will prompt you for the corrected spelling."), Category("Action")]
		public void AssignCorrectSpelling(string strBadWord)
		{
			if (String.IsNullOrEmpty(m_strConverterSpec))
				throw new ExternalException("No project selected! Did you open a project?");

			// in case it was deleted by the user, recreate it now.
			if (File.Exists(m_strConverterSpec))
			{
				// the file already exists... see if this word would otherwise already be altered by the cc table
				if (ChaChaChaChaChanges(strBadWord))
				{
					DialogResult res = MessageBox.Show(String.Format("There is already a replacement rule that affects the string ({0}). {1}{1}Click 'Retry' to display the existing rule or 'Ignore' to add a new rule{1}(which must be a longer string than the existing rule to override it).", strBadWord, Environment.NewLine), cstrCaption, MessageBoxButtons.AbortRetryIgnore);
					if (res == DialogResult.Abort)
					{
						return;
					}
					else if (res == DialogResult.Retry)
					{
						this.FindReplacementRule(strBadWord);
						return;
					}
				}
			}
			else
			{
				LoginSF.CreateCCTable(m_strConverterSpec, SpellFixerEncConverterName, PunctuationAndWhiteSpace, null, !this.m_bLegacy);
			}

			QueryAndAppend(strBadWord);
		}

		[Description("Call this method with a misspelled word and it's replacement and they will be added to the fixup table."), Category("Action")]
		public void AssignCorrectSpelling(string strBadWord, string strReplacement)
		{
			// in case it was deleted by the user, recreate it now.
			if (File.Exists(m_strConverterSpec))
			{
				// the file already exists... see if this word would otherwise already be altered by the cc table
				if (ChaChaChaChaChanges(strBadWord))
				{
					DialogResult res = MessageBox.Show(String.Format("There is already a replacement rule that affects the string ({0}). {2}{2}Click 'Retry' to display the existing rule or 'Ignore' to continuing adding the new rule ({0})->({1}).", strBadWord, strReplacement, Environment.NewLine), cstrCaption, MessageBoxButtons.AbortRetryIgnore);
					if (res == DialogResult.Abort)
					{
						return;
					}
					else if (res == DialogResult.Retry)
					{
						this.FindReplacementRule(strBadWord);
						return;
					}
				}
			}
			else
			{
				LoginSF.CreateCCTable(m_strConverterSpec, SpellFixerEncConverterName, PunctuationAndWhiteSpace, null, !this.m_bLegacy);
			}

			// if it was legacy encoded, then we need to convert the data to narrow using
			//  the code page the user specified (or we got out of the repository)
			Encoding enc = GetEncoding;

			// get a stream writer for this encoding and append
			StreamWriter sw = new StreamWriter(m_strConverterSpec, true, enc);
			sw.WriteLine(FormatSubstitutionRule(strBadWord, strReplacement, WordBoundaryDelimiter, strBadWord));
			sw.Flush();
			sw.Close();
		}

		[Description("Bring up the Fix Spelling dialog box with the replacement rule which changes (or results in) the give word."), Category("Action")]
		public void FindReplacementRule(string strWord)
		{
			// first make sure the CC table exists
			if ((m_strConverterSpec != null) && File.Exists(m_strConverterSpec))
			{
				CleanWord(ref strWord);

				// Open the CC table that has the mappings and put them in a DataTable.
				DataTable myTable;
				Encoding enc = GetEncoding;
				if (InitializeDataTableFromCCTable(m_strConverterSpec, enc, WordBoundaryDelimiter, out myTable))
				{
					// temporary filename for temporary CC tables (to check portions of the file at a time)
					string strTempName = Path.GetTempFileName();

					// get a CC table EncConverter
					IEncConverter aEC = new EncConverters().NewEncConverterByImplementationType(EncConverters.strTypeSILcc);

					// check to make sure that the whole table has a rule which changes it (it might not)
					int nFoundIndex = -1;
					if (ChaChaChaChaChanges(aEC, m_strConverterSpec, strWord))
					{
						// do a binary search to find the one replacement rule that causes a change
						int nLength = myTable.Rows.Count, nIndex = 0;
						nFoundIndex = nIndex;
						DataTable tblTestingRules = GetDataTable;

						while (nLength > 1)
						{
							// check the lower half
							int nLowHalfLength = nLength / 2;
							// GetPortionOfTable(myTable, nIndex, nLowHalfLength, ref tblTestingRules);
							if (ChaChaChaChaChanges(aEC, strTempName, enc, strWord, myTable, nIndex, nLowHalfLength))
							{
								// found in the lower half
								nFoundIndex = nIndex;
								nLength = nLowHalfLength;
							}
							else
							{
								// otherwise check in the upper half
								// GetPortionOfTable(myTable, nIndex + nLowHalfLength, nLength - nLowHalfLength, ref tblTestingRules);
								if (ChaChaChaChaChanges(aEC, strTempName, enc, strWord, myTable, nIndex + nLowHalfLength, nLength - nLowHalfLength))
								{
									// found in the upper half
									nIndex += nLowHalfLength;
									nFoundIndex = nIndex;
									nLength -= nLowHalfLength;
								}
							}
						}
					}

					// clean up the temporary file.
					File.Delete(strTempName);

					// if we didn't see any rules that manipulate the input string, then see if any generate
					//  the input string (i.e. compare the word against the right-hand side)
					if (nFoundIndex == -1)
					{
						// let's trim it of external spaces first
						strWord = strWord.Trim();
						for (nFoundIndex = 0; nFoundIndex < myTable.Rows.Count; nFoundIndex++)
						{
							DataRow row = myTable.Rows[nFoundIndex];
							if (strWord == (string)row[strColumnRhs])
								break;
						}
					}

					if (nFoundIndex == myTable.Rows.Count)
					{
						// none found
						MessageBox.Show(String.Format("There are no substitution rules that apply to this word ({0})!", strWord), cstrCaption);
					}

					else if ((nFoundIndex >= 0) && (nFoundIndex < myTable.Rows.Count))
					{
						DataRow row = myTable.Rows[nFoundIndex];
						QueryGoodSpelling aQuery = new QueryGoodSpelling(m_font);
						DialogResult res = aQuery.ShowDialog((string)row[strColumnLhs], (string)row[strColumnRhs], GetComment(row), true);
						bool bRewrite = false;
						if (res == DialogResult.Abort)
						{
							// this means to delete the bad substitution rule
							myTable.Rows.RemoveAt(nFoundIndex);
							bRewrite = true;
						}

						// if the user clicks OK and has made a change...
						if ((res == DialogResult.OK)
							&& (((string)row[strColumnLhs] != aQuery.BadSpelling)
								|| ((string)row[strColumnRhs] != aQuery.GoodSpelling)
								)
						)
						{
							// update the table and rewrite
							row[strColumnLhs] = aQuery.BadSpelling;
							row[strColumnRhs] = aQuery.GoodSpelling;
							row[strColumnCmt] = strWord;
							bRewrite = true;
						}

						if (bRewrite)
						{
							// write the newly updated DataTable
							LoginSF.ReWriteCCTableHeader(m_strConverterSpec, PunctuationAndWhiteSpace, enc);
							AppendCCTableFromDataTable(m_strConverterSpec, enc, WordBoundaryDelimiter, PunctuationAndWhiteSpace, myTable);
						}
					}
				}
			}
		}

		[Description("Use this method to edit the list of spelling fixes")]
		public void EditSpellingFixes()
		{
			if (m_strConverterSpec != null)
			{
				// Open the CC table that has the mappings and put them in a DataTable.
				if (!File.Exists(m_strConverterSpec))
				{
					LoginSF.CreateCCTable(m_strConverterSpec, SpellFixerEncConverterName, PunctuationAndWhiteSpace, null, !this.m_bLegacy);
				}

				// if it was legacy encoded, then we need to convert the data to narrow using
				//  the code page the user specified (or we got out of the repository)
				Encoding enc = GetEncoding;
				DataTable myTable;
				if (InitializeDataTableFromCCTable(m_strConverterSpec, enc, WordBoundaryDelimiter, out myTable))
				{
					// now put up an editable grid with this data.
					DialogResult res = DialogResult.Cancel;
					try
					{
						ViewBadGoodPairsDlg dlg = new ViewBadGoodPairsDlg(myTable, m_font);
						res = dlg.ShowDialog();
					}
#if DEBUG
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, SpellingFixerEC.cstrCaption);
					}
#else
					catch { }
#endif

					if (res == DialogResult.OK)
					{
						LoginSF.ReWriteCCTableHeader(m_strConverterSpec, PunctuationAndWhiteSpace, enc);
						AppendCCTableFromDataTable(m_strConverterSpec, enc, WordBoundaryDelimiter, PunctuationAndWhiteSpace, myTable);
					}
				}
			}
		}

		protected Encoding GetEncoding
		{
			get
			{
				// if it was legacy encoded, then we need to convert the data to narrow using
				//  the code page the user specified (or we got out of the repository)
				Encoding enc = null;
				if (m_bLegacy)
				{
					int cp = m_cp;
					if (m_cp == EncConverters.cnSymbolFontCodePage)
						cp = EncConverters.cnIso8859_1CodePage;
					enc = Encoding.GetEncoding(cp);
				}
				else
					enc = new UTF8Encoding();

				return enc;
			}
		}

		protected static DataTable GetDataTable
		{
			get
			{
				DataTable myTable = new DataTable("SpellingFixesList");
				myTable.Columns.Add(new DataColumn(strColumnLhs, typeof(string)));
				myTable.Columns.Add(new DataColumn(strColumnRhs, typeof(string)));
				myTable.Columns.Add(new DataColumn(strColumnCmt, typeof(string)));
				return myTable;
			}
		}

		// clean input word
		private static void CleanWord(ref string str)
		{
			// then strip off invalid chars (that sometimes come in from Word)
			if (str != null)
			{
				int nIndexBadChar = 0;
				char[] aBadChars = new char[] { '\r', '\n' };
				while ((nIndexBadChar = str.IndexOfAny(aBadChars)) != -1)
					str = str.Remove(nIndexBadChar, 1);
			}
		}

		private static string GetComment(DataRow row)
		{
			string strRet = null;
			if (row[strColumnCmt] != System.DBNull.Value)
				strRet = row[strColumnCmt].ToString();
			return strRet;
		}

		private bool ChaChaChaChaChanges(string strWord)
		{
			IEncConverter aEC = new EncConverters().NewEncConverterByImplementationType(EncConverters.strTypeSILcc);
			return ChaChaChaChaChanges(aEC, m_strConverterSpec, strWord);
		}

		private bool ChaChaChaChaChanges(IEncConverter aEC, string strFileName, string strWord)
		{
			string strDummy = null;
			int lProcessType = (int)SpellingFixerEC.SFProcessType;
			ConvType eConvType = (m_bLegacy) ? ConvType.Legacy_to_Legacy : ConvType.Unicode_to_Unicode;
			aEC.Initialize("dummyname", strFileName, ref strDummy, ref strDummy, ref eConvType, ref lProcessType, 0, 0, true);
			return (aEC.Convert(strWord) != strWord);
		}

		private bool ChaChaChaChaChanges(IEncConverter aEC, string strFileName, Encoding enc, string strWord, DataTable tblData, int nTableIndex, int nNumRows)
		{
			this.WriteCCTableFromDataTable(strFileName, enc, tblData, nTableIndex, nNumRows);
			return ChaChaChaChaChanges(aEC, strFileName, strWord);
		}

		private void GetPortionOfTable(DataTable myTable, int nIndex, int nLength, ref DataTable tblTestingRules)
		{
			tblTestingRules.Clear();
			for (int i = nIndex; (nLength-- > 0); nIndex++)
			{
				DataRow row = myTable.Rows[nIndex];
				DataRow newRow = tblTestingRules.NewRow();
				newRow[strColumnLhs] = row[strColumnLhs];
				newRow[strColumnRhs] = row[strColumnRhs];
				newRow[strColumnCmt] = row[strColumnCmt];
				tblTestingRules.Rows.Add(newRow);
			}
		}

		internal static string FormatSubstitutionRule(string strBad, string strGood, string strWordBoundaryDelimiter, string strCommentWord)
		{
			// if the user indicated a word boundary condition (i.e. #pete, ete#, or #pete#)
			//  then we have to put special stuff in the CC table to search for
			//  preceding or trailing whitespace.
			// spuriously, there may be certain characters which we can't tolerate
			CleanWord(ref strBad);
			CleanWord(ref strGood);
			CleanWord(ref strCommentWord);

			string strDelimiter = strWordBoundaryDelimiter;
			int nDelimiterLen = strDelimiter.Length;
			string strLhsFormat = null;
			try
			{
				if (strBad.Substring(0, nDelimiterLen) == strDelimiter)
				{
					strLhsFormat = cstrPrecWhiteSpace + chSpace;
					strBad = strBad.Remove(0, nDelimiterLen);
				}
			}
			catch { }    // don't care, but don't want to check lengths to avoid ArgumentOutOfRangeException

			strLhsFormat += strQuotedFormat;

			int nIndex = strBad.Length - nDelimiterLen;
			try
			{
				if (strBad.Substring(nIndex) == strDelimiter)
				{
					strBad = strBad.Substring(0, nIndex);
					strLhsFormat += chSpace + cstrFollWhiteSpace;
				}
			}
			catch { }    // don't care, but don't want to check lengths to avoid ArgumentOutOfRangeException

			string str = cstrIndentation + String.Format(strLhsFormat, strBad) + " > " + String.Format(strQuotedFormat, strGood);
			if (!String.IsNullOrEmpty(strCommentWord))
				str += String.Format(cstrCommentFormat, strCommentWord);
			return str;
		}

		internal const string strColumnLhs = "Bad Spelling";
		internal const string strColumnRhs = "Good Spelling";
		internal const string strColumnCmt = "Comment";

		internal static void AppendCCTableFromDataTable
			(
			string strConverterSpec,
			Encoding enc,
			string strWordBoundaryDelimiter,
			string strPunctuationAndWhiteSpace,
			DataTable myTable
			)
		{
			// get a stream writer to write the new pairs
			StreamWriter sw = new StreamWriter(strConverterSpec, true, enc);

			AppendCCTableFromDataTable(sw, strWordBoundaryDelimiter, strPunctuationAndWhiteSpace, myTable, 0, myTable.Rows.Count);

			sw.Flush();
			sw.Close();
		}

		internal void WriteCCTableFromDataTable(string strFilename, Encoding enc, DataTable tbl, int nTableIndex, int nNumRows)
		{
			if (File.Exists(strFilename))
				File.Delete(strFilename);

			StreamWriter sw = new StreamWriter(strFilename, false, enc);
			LoginSF.CreateCCTable(sw, SpellFixerEncConverterName, PunctuationAndWhiteSpace, null, !m_bLegacy);
			AppendCCTableFromDataTable(sw, WordBoundaryDelimiter, PunctuationAndWhiteSpace, tbl, nTableIndex, nNumRows);
			sw.Flush();
			sw.Close();
		}

		internal static void AppendCCTableFromDataTable
			(
			StreamWriter sw,
			string strWordBoundaryDelimiter,
			string strPunctuationAndWhiteSpace,
			DataTable myTable,
			int nTableIndex,
			int nNumRows
			)
		{
			// iterate the rows and write the pairs
			Debug.Assert(nTableIndex + nNumRows <= myTable.Rows.Count);
			for (int i = nTableIndex; nNumRows-- > 0; i++)
			{
				try
				{
					DataRow row = myTable.Rows[i];

					string strBadSpelling = row[strColumnLhs].ToString();
					string strGoodSpelling = row[strColumnRhs].ToString();
					string strCommentWord = GetComment(row);

					sw.WriteLine(FormatSubstitutionRule(strBadSpelling, strGoodSpelling, strWordBoundaryDelimiter, strCommentWord));
				}
				catch { }
			}
		}

		internal static StreamReader InitReaderPastHeader(string strConverterSpec, Encoding enc)
		{
			// get a stream writer for these encoding and append
			StreamReader sr = new StreamReader(strConverterSpec, enc);

			// skip past the header lines
			string line = null;
			do
			{
				line = sr.ReadLine();

				if (line == null)
					throw new ExternalException(String.Format("The substitution mapping file (i.e. '{0}') appears to be from a previous version of SpellFixer. Create a new project and manually copy over the spelling substitutions from the existing mapping file to the new project mapping file using a text editor like Notepad", strConverterSpec));

			} while (line != LoginSF.cstrLastHeaderLine);

			return sr;
		}

		internal static bool InitializeDataTableFromCCTable
			(
			string strConverterSpec,
			Encoding enc,
			string strWordBoundaryDelimiter,
			out DataTable myTable
			)
		{
			// get a stream writer for these encoding and append
			StreamReader sr = InitReaderPastHeader(strConverterSpec, enc);

			myTable = GetDataTable;
			string line = null;
			while ((line = sr.ReadLine()) != null)
			{
				string strLhs = null;
				try
				{
					// we have a preceding white space qualifier if the first part of the
					//  string is the cstrPrecWhiteSpace.
					if (line.Substring(cstrIndentation.Length, cstrPrecWhiteSpace.Length) == cstrPrecWhiteSpace)
						strLhs = strWordBoundaryDelimiter;
				}
				catch { }    // don't care, but don't want to check lengths (sometimes gives ArgumentOutOfRangeException)

				int nLhsLeftIdx = line.IndexOf('\"', 0) + 1;
				int nLhsRightIdx = line.IndexOf('\"', nLhsLeftIdx);
				Debug.Assert((nLhsLeftIdx != -1) && (nLhsRightIdx != -1) && ((nLhsRightIdx - nLhsLeftIdx) < line.Length));
				if ((nLhsLeftIdx != -1) && (nLhsRightIdx != -1) && ((nLhsRightIdx - nLhsLeftIdx) < line.Length))
				{
					strLhs += line.Substring(nLhsLeftIdx, nLhsRightIdx - nLhsLeftIdx);

					bool bFollWhiteSpace = (line.IndexOf(cstrFollWhiteSpace, nLhsRightIdx + 1) != -1);
					if (bFollWhiteSpace)
						strLhs += strWordBoundaryDelimiter;

					int nRhsLeftIdx = line.IndexOf('\"', nLhsRightIdx + 1) + 1;
					int nRhsRightIdx = line.IndexOf('\"', nRhsLeftIdx);
					Debug.Assert((nRhsLeftIdx != -1) && (nRhsRightIdx != -1) && ((nRhsRightIdx - nRhsLeftIdx) < line.Length));
					if ((nRhsLeftIdx != -1) && (nRhsRightIdx != -1) && ((nRhsRightIdx - nRhsLeftIdx) < line.Length))
					{
						string strRhs = line.Substring(nRhsLeftIdx, nRhsRightIdx - nRhsLeftIdx);
						string strCommentWord = null;
						nRhsRightIdx += cstrCommentFormat.Length - 3;
						if (nRhsRightIdx <= line.Length)
							strCommentWord = line.Substring(nRhsRightIdx, line.Length - nRhsRightIdx - 1);

						DataRow rowNew = myTable.NewRow();
						rowNew[strColumnLhs] = strLhs;
						rowNew[strColumnRhs] = strRhs;
						rowNew[strColumnCmt] = strCommentWord;
						myTable.Rows.Add(rowNew);
					}
				}
			}
			sr.Close();

			return true;
		}

		[Description("Returns the name of the EncConverter to use to correct the spelling for the selected project."), Category("Data")]
		public override string ToString()
		{
			return SpellFixerEncConverterName;
		}
	}
}

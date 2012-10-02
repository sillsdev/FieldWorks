using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace LocaleStrings
{
	class POString
	{
		static int s_cLines = 0;
		List<string> m_rgsUserComments = null;
		List<string> m_rgsAutoComments = null;
		List<string> m_rgsReference = null;
		List<string> m_rgsFlags = null;
		List<string> m_rgsMsgId = null;
		List<string> m_rgsMsgStr = null;
		int m_cDup = 0;
		bool m_fObsolete = false;

		public POString(string[] rgsComment, string[] rgsId, string[] rgsVal)
		{
			if (rgsComment != null)
			{
				m_rgsAutoComments = new List<string>(rgsComment.Length);
				for (int i = 0; i < rgsComment.Length; ++i)
					m_rgsAutoComments.Add(rgsComment[i]);
			}
			m_rgsMsgId = new List<string>(rgsId.Length);
			for (int i = 0; i < rgsId.Length; ++i)
				m_rgsMsgId.Add(rgsId[i]);
			m_rgsMsgStr = new List<string>(rgsVal.Length);
			for (int i = 0; i < rgsVal.Length; ++i)
				m_rgsMsgStr.Add(rgsVal[i]);
			m_cDup = 1;
		}

		public POString()
		{
			m_cDup = 1;
		}

		static public POString ReadFromFile(StreamReader srIn)
		{
			// Move past any blank lines, checking for the end of file.
			if (srIn.EndOfStream)
				return null;
			string s = srIn.ReadLine();
			++s_cLines;
			if (s != null)
				s = s.Trim();
			while (String.IsNullOrEmpty(s))
			{
				if (srIn.EndOfStream)
					return null;
				s = srIn.ReadLine();
				++s_cLines;
				if (s != null)
					s = s.Trim();
			}
			POString pos = new POString();
			bool fMsgId = false;
			bool fMsgStr = false;
			bool fError = false;
			do
			{
				if (s == "#")
				{
					pos.AddUserComment("");
				}
				else if (s.StartsWith("# "))
				{
					pos.AddUserComment(s.Substring(2).TrimStart());
				}
				else if (s.StartsWith("#."))
				{
					pos.AddAutoComment(s.Substring(2).TrimStart());
				}
				else if (s.StartsWith("#:"))
				{
					pos.AddReference(s.Substring(2).TrimStart());
				}
				else if (s.StartsWith("#,"))
				{
					pos.AddFlags(s.Substring(2).TrimStart());
				}
				else if (s.ToLower().StartsWith("msgid"))
				{
					fMsgId = true;
					if (fMsgStr)
						fError = true;
					pos.AddMsgIdLine(s);
				}
				else if (s.ToLower().StartsWith("msgstr"))
				{
					if (!fMsgId)
						fError = true;
					fMsgId = false;
					fMsgStr = true;
					pos.AddMsgStrLine(s);
				}
				else if (s.StartsWith("\""))
				{
					if (fMsgId)
						pos.AddMsgIdLine(s);
					else if (fMsgStr)
						pos.AddMsgStrLine(s);
					else
						fError = true;
				}
				else if (s.StartsWith("#~"))
				{
					pos.IsObsolete = true;
					if (pos.UserComments != null)
						pos.UserComments.Clear();
					if (pos.AutoComments != null)
						pos.AutoComments.Clear();
					if (pos.Reference != null)
						pos.Reference.Clear();
					if (pos.Flags != null)
					   pos.Flags.Clear();
				   if (pos.MsgId != null)
						pos.MsgId.Clear();
					if (pos.MsgStr != null)
						pos.MsgStr.Clear();
				}
				else
				{
					fError = true;
				}
				if (fError)
				{
					Console.WriteLine("INVALID PO FILE: ERROR ON LINE " + s_cLines);
					throw new Exception("BAD PO FILE");
				}
				if (srIn.EndOfStream)
					break;
				s = srIn.ReadLine();
				++s_cLines;
				if (s != null)
					s = s.Trim();
			} while (!String.IsNullOrEmpty(s));

			return pos;
		}

		internal void AddUserComment(string s)
		{
			if (m_rgsUserComments == null)
				m_rgsUserComments = new List<string>();
			if (!m_rgsUserComments.Contains(s))
				m_rgsUserComments.Add(s);
		}

		internal void AddAutoComment(string s)
		{
			if (m_rgsAutoComments == null)
				m_rgsAutoComments = new List<string>();
			if (!m_rgsAutoComments.Contains(s))
			{
				if (s.StartsWith("/"))
				{
					m_rgsAutoComments.Add(s);
				}
				else
				{
					// Keep these comments in order at the beginning.
					int idx = m_rgsAutoComments.FindIndex(0, POString.IsPathComment);
					if (idx < 0)
						idx = m_rgsAutoComments.Count;
					m_rgsAutoComments.Insert(idx, s);
				}
			}
		}

		private static bool IsPathComment(string s)
		{
			return s.StartsWith("/");
		}

		internal void AddReference(string s)
		{
			if (m_rgsReference == null)
				m_rgsReference = new List<string>();
			if (!m_rgsReference.Contains(s))
				m_rgsReference.Add(s);
		}

		internal void AddFlags(string s)
		{
			if (m_rgsFlags == null)
				m_rgsFlags = new List<string>();
			if (!m_rgsFlags.Contains(s))
				m_rgsFlags.Add(s);
		}

		internal void AddMsgIdLine(string s)
		{
			if (m_rgsMsgId == null)
				m_rgsMsgId = new List<string>();
			int idx1 = s.IndexOf('"');
			int idx2 = s.LastIndexOf('"');
			if (idx1 == -1 || idx2 == -1 || idx1 == idx2)
				throw new Exception("Invalid msgid line: not quoted");
			++idx1;
			m_rgsMsgId.Add(s.Substring(idx1, idx2 - idx1));
		}

		internal void AddMsgStrLine(string s)
		{
			if (m_rgsMsgStr == null)
				m_rgsMsgStr = new List<string>();
			int idx1 = s.IndexOf('"');
			int idx2 = s.LastIndexOf('"');
			if (idx1 == -1 || idx2 == -1 || idx1 == idx2)
				throw new Exception("Invalid msgstr line: not quoted");
			++idx1;
			m_rgsMsgStr.Add(DeQuote(s.Substring(idx1, idx2 - idx1)));
		}


		private string DeQuote(string sValue)
		{
			int idx = sValue.IndexOf('\\');
			while (idx >= 0)
			{
				if (idx + 1 < sValue.Length)
				{
					sValue = sValue.Remove(idx, 1);
					switch (sValue[idx])
					{
						case 'n':
							sValue = sValue.Remove(idx, 1);
							sValue = sValue.Insert(idx, "\n");
							break;
						case '"':
							break;
						case '\\':
							break;
						// The next two aren't handled on extract, but they may exist...
						case 't':
							sValue = sValue.Remove(idx, 1);
							sValue = sValue.Insert(idx, "\t");
							break;
						case 'r':
							sValue = sValue.Remove(idx, 1);
							sValue = sValue.Insert(idx, "\r");
							break;
						default:
							sValue = sValue.Insert(idx, "\\");	// put it back...
							break;
					}
				}
				++idx;
				if (idx >= sValue.Length)
					break;
				idx = sValue.IndexOf('\\', idx);
			}
			return sValue;
		}

		internal List<string> UserComments
		{
			get { return m_rgsUserComments; }
		}

		internal List<string> AutoComments
		{
			get { return m_rgsAutoComments; }
		}

		internal List<string> Reference
		{
			get { return m_rgsReference; }
		}

		internal List<string> Flags
		{
			get { return m_rgsFlags; }
		}

		internal List<string> MsgId
		{
			get { return m_rgsMsgId; }
		}

		internal List<string> MsgStr
		{
			get { return m_rgsMsgStr; }
			set { m_rgsMsgStr = value; }
		}

		/// <summary>
		/// CompareMsgIds two POString objects with the purpose of sorting the PO file by msgid.
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <returns></returns>
		public static int CompareMsgIds(POString x, POString y)
		{
			if (x.m_rgsMsgId == null && y.m_rgsMsgId == null)
				return 0;
			else if (x.m_rgsMsgId == null)
				return -1;
			else if (y.m_rgsMsgId == null)
				return 1;
			for (int i = 0; i < x.m_rgsMsgId.Count && i < y.m_rgsMsgId.Count; ++i)
			{
				string s1 = RemoveLeadingCruft(x.m_rgsMsgId[i].Replace("&", "").Trim());
				string s2 = RemoveLeadingCruft(y.m_rgsMsgId[i].Replace("&", "").Trim());
				int n = s1.CompareTo(s2);
				if (n != 0)
					return n;
			}
			if (x.m_rgsMsgId.Count < y.m_rgsMsgId.Count)
				return -1;
			else if (x.m_rgsMsgId.Count > y.m_rgsMsgId.Count)
				return 1;
			else
				return x.m_rgsMsgId[0].CompareTo(y.m_rgsMsgId[0]);
		}

		/// <summary>
		/// Remove leading characters from the string that we don't want to compare, at least
		/// not if we don't have to.
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		private static string RemoveLeadingCruft(string s)
		{
			string s1 = s.Replace("\\n", "\n");
			s1 = s1.Replace("\\t", "\t");
			while (s1.Length > 0 && !Char.IsLetter(s1[0]))
				s1 = s1.Remove(0, 1);
			return s1;
		}

		public bool HasSameMsgId(POString that)
		{
			if (this.m_rgsMsgId == null && that.m_rgsMsgId == null)
				return true;
			else if (this.m_rgsMsgId == null || that.m_rgsMsgId == null)
				return false;
			for (int i = 0; i < this.m_rgsMsgId.Count && i < that.m_rgsMsgId.Count; ++i)
			{
				string s1 = this.m_rgsMsgId[i];
				string s2 = that.m_rgsMsgId[i];
				if (s1 != s2)
					return false;
			}
			return this.m_rgsMsgId.Count == that.m_rgsMsgId.Count;
		}

		public bool IsObsolete
		{
			get { return m_fObsolete; }
			set { m_fObsolete = value; }
		}

		/// <summary>
		/// Write the contents of this object to the given StreamWriter.
		/// </summary>
		/// <param name="swOut"></param>
		internal void Write(StreamWriter swOut)
		{
			if (m_rgsReference != null)
			{
				for (int i = 0; i < m_rgsReference.Count; ++i)
					WriteComment(m_rgsReference[i], ':', swOut);
			}
			if (m_rgsAutoComments != null)
			{
				// Sort the set of path comments generated from the XML configuration files
				int idx = m_rgsAutoComments.FindIndex(0, IsPathComment);
				if (idx >= 0)
				{
					m_rgsAutoComments.Sort(idx, m_rgsAutoComments.Count - idx, null);
				}
				for (int i = 0; i < m_rgsAutoComments.Count; ++i)
					WriteComment(m_rgsAutoComments[i], '.', swOut);
			}
			if (m_cDup > 1)
				WriteComment(String.Format("(String used {0} times.)", m_cDup), '.', swOut);
			if (m_rgsFlags != null)
			{
				for (int i = 0; i < m_rgsFlags.Count; ++i)
					WriteComment(m_rgsFlags[i], ',', swOut);
			}
			if (m_rgsUserComments != null)
			{
				for (int i = 0; i < m_rgsUserComments.Count; ++i)
					WriteComment(m_rgsUserComments[i], ' ', swOut);
			}
			WriteMsgBundle("msgid", m_rgsMsgId, swOut);
			WriteMsgBundle("msgstr", m_rgsMsgStr, swOut);
			swOut.WriteLine("");
		}

		/// <summary>
		/// Write the strings associated with either the Id or the value.
		/// </summary>
		/// <param name="sType"></param>
		/// <param name="rgs"></param>
		/// <param name="swOut"></param>
		private void WriteMsgBundle(string sType, List<string> rgs, StreamWriter swOut)
		{
			swOut.Write(string.Format("{0} ", sType));
			if (rgs.Count != 1 && !(rgs.Count == 2 && String.IsNullOrEmpty(rgs[1])))
				WriteQuotedLine("", false, swOut);
			for (int i = 0; i < rgs.Count; ++i)
			{
				bool fLast = i + 1 == rgs.Count;
				if (!fLast || !String.IsNullOrEmpty(rgs[i]) || rgs.Count == 1)
				{
					if (sType == "msgstr")
						WriteQuotedLine(ReQuote(rgs[i]), !fLast, swOut);
					else
						WriteQuotedLine(rgs[i], !fLast, swOut);
				}
			}
		}

		/// <summary>
		/// Write a comment of the given type to the PO file.
		/// </summary>
		/// <param name="sComment">comment string</param>
		/// <param name="chFlag">flag character indicating type of comment</param>
		/// <param name="swOut">output StreamWriter</param>
		public static void WriteComment(string sComment, char chFlag, StreamWriter swOut)
		{
			swOut.WriteLine("#{0} {1}", chFlag, sComment);
		}

		/// <summary>
		/// write a quoted line to the output PO file, optionally appending "\\n" to the end.
		/// </summary>
		/// <param name="sVal">string value</param>
		/// <param name="fAppendNewline">true if "\\n" should be added to the end of the string</param>
		/// <param name="swOut">output StreamWriter</param>
		public static void WriteQuotedLine(string sVal, bool fAppendNewline, StreamWriter swOut)
		{
			if (fAppendNewline && !sVal.EndsWith("\\n"))
			{
				swOut.WriteLine("\"{0}\\n\"", sVal);
			}
			else
			{
				swOut.WriteLine("\"{0}\"", sVal);
			}
		}

		private static string ReQuote(string sValue)
		{
			string s1 = sValue.Replace("\\", "\\\\");
			string s2 = s1.Replace("\"", "\\\"");
			string s3 = s2.Replace("\t", "\\t");
			string s4 = s3.Replace("\n", "\\n");
			string s5 = s4.Replace("\r", "\\r");
			return s5;
		}

		/// <summary>
		/// Since we're creating a single POT, we want to merge strings that are identical.
		/// </summary>
		/// <param name="rgsPOStrings"></param>
		internal static void MergeDuplicateStrings(List<POString> rgsPOStrings)
		{
			for (int i = 1; i < rgsPOStrings.Count; ++i)
			{
				if (rgsPOStrings[i - 1].HasSameMsgId(rgsPOStrings[i]))
				{
					if (rgsPOStrings[i].m_rgsAutoComments != null)
					{
						for (int j = 0; j < rgsPOStrings[i].m_rgsAutoComments.Count; ++j)
						{
							rgsPOStrings[i - 1].AddAutoComment(rgsPOStrings[i].m_rgsAutoComments[j]);
						}
					}
					rgsPOStrings.RemoveAt(i);
					++rgsPOStrings[i - 1].m_cDup;
					--i;
				}
			}
		}

		/// <summary>
		///
		/// </summary>
		public bool HasEmptyMsgStr
		{
			get
			{
				if (m_rgsMsgStr == null || m_rgsMsgStr.Count == 0)
					return true;
				else if (m_rgsMsgStr.Count > 1)
					return false;
				else
					return String.IsNullOrEmpty(m_rgsMsgStr[0]);
			}
		}

		/// <summary>
		///
		/// </summary>
		public string MsgIdAsString()
		{
			return StringListAsString(m_rgsMsgId);
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public string MsgStrAsString()
		{
			return StringListAsString(m_rgsMsgStr);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="rgs"></param>
		/// <returns></returns>
		private static string StringListAsString(List<string> rgs)
		{
			if (rgs == null || rgs.Count == 0)
				return "";
			if (rgs.Count == 1 && String.IsNullOrEmpty(rgs[0]))
				return "";
			StringBuilder bldr = new StringBuilder();
			for (int i = 0; i < rgs.Count; ++i)
			{
				string s = rgs[i];
				int idx = s.IndexOf('\\');
				while (idx >= 0)
				{
					if (idx + 1 < s.Length)
					{
						s = s.Remove(idx, 1);
						if (s[idx] == 'n')
						{
							s = s.Remove(idx, 1);
							s = s.Insert(idx, "\n");
						}
						else if (s[idx] == 't')
						{
							s = s.Remove(idx, 1);
							s = s.Insert(idx, "\t");
						}
					}
					if (idx + 1 < s.Length)
					{
						idx = s.IndexOf('\\', idx + 1);
					}
					else
					{
						break;
					}
				}
				bldr.Append(s);
			}
			return bldr.ToString();
		}
	}
}

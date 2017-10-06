// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SIL.FieldWorks.Build.Tasks.Localization
{
	/// <summary>
	/// Currently a duplicate of the class in bin/src/LocaleStrings. Manages information from one entry in a PO localization file.
	/// </summary>
	internal class PoString
	{
		private static int s_NumberOfLines;
		private int _usageCount;

		public PoString(IReadOnlyCollection<string> comments, IEnumerable<string> ids, IEnumerable<string> values)
		{
			if (comments != null)
			{
				AutoComments = new List<string>(comments);
			}
			MsgIds = new List<string>(ids);
			MsgStrings = new List<string>(values);
			_usageCount = 1;
		}

		public PoString()
		{
			_usageCount = 1;
		}

		public static PoString ReadFromFile(StreamReader inputStream)
		{
			// Move past any blank lines, checking for the end of file.
			if (inputStream.EndOfStream)
				return null;
			var s = inputStream.ReadLine();
			++s_NumberOfLines;
			s = s?.Trim();
			while (string.IsNullOrEmpty(s))
			{
				if (inputStream.EndOfStream)
					return null;
				s = inputStream.ReadLine();
				++s_NumberOfLines;
				s = s?.Trim();
			}
			var pos = new PoString();
			var fMsgId = false;
			var fMsgStr = false;
			var fError = false;
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
					pos.UserComments?.Clear();
					pos.AutoComments?.Clear();
					pos.Reference?.Clear();
					pos.Flags?.Clear();
					pos.MsgIds?.Clear();
					pos.MsgStrings?.Clear();
				}
				else
				{
					fError = true;
				}
				if (fError)
				{
					Console.WriteLine("INVALID PO FILE: ERROR ON LINE " + s_NumberOfLines);
					throw new Exception("BAD PO FILE");
				}
				if (inputStream.EndOfStream)
					break;
				s = inputStream.ReadLine();
				++s_NumberOfLines;
				s = s?.Trim();
			} while (!string.IsNullOrEmpty(s));

			return pos;
		}

		internal void AddUserComment(string s)
		{
			if (UserComments == null)
				UserComments = new List<string>();
			if (!UserComments.Contains(s))
				UserComments.Add(s);
		}

		internal void AddAutoComment(string s)
		{
			if (AutoComments == null)
				AutoComments = new List<string>();
			if (AutoComments.Contains(s))
				return;

			if (s.StartsWith("/"))
			{
				AutoComments.Add(s);
			}
			else
			{
				// Keep these comments in order at the beginning.
				var idx = AutoComments.FindIndex(0, IsPathComment);
				if (idx < 0)
					idx = AutoComments.Count;
				AutoComments.Insert(idx, s);
			}
		}

		private static bool IsPathComment(string s)
		{
			return s.StartsWith("/");
		}

		internal void AddReference(string s)
		{
			if (Reference == null)
				Reference = new List<string>();
			if (!Reference.Contains(s))
				Reference.Add(s);
		}

		internal void AddFlags(string s)
		{
			if (Flags == null)
				Flags = new List<string>();
			if (!Flags.Contains(s))
				Flags.Add(s);
		}

		internal void AddMsgIdLine(string s)
		{
			if (MsgIds == null)
				MsgIds = new List<string>();
			var idx1 = s.IndexOf('"');
			var idx2 = s.LastIndexOf('"');
			if (idx1 == -1 || idx2 == -1 || idx1 == idx2)
				throw new Exception("Invalid msgid line: not quoted");
			++idx1;
			MsgIds.Add(s.Substring(idx1, idx2 - idx1));
		}

		internal void AddMsgStrLine(string s)
		{
			if (MsgStrings == null)
				MsgStrings = new List<string>();
			var idx1 = s.IndexOf('"');
			var idx2 = s.LastIndexOf('"');
			if (idx1 == -1 || idx2 == -1 || idx1 == idx2)
				throw new Exception("Invalid msgstr line: not quoted");
			++idx1;
			MsgStrings.Add(DeQuote(s.Substring(idx1, idx2 - idx1)));
		}

		private static string DeQuote(string value)
		{
			var iBackslash = value.IndexOf('\\');
			while (iBackslash >= 0)
			{
				if (iBackslash + 1 < value.Length)
				{
					value = value.Remove(iBackslash, 1);
					switch (value[iBackslash])
					{
						case 'n':
							value = value.Remove(iBackslash, 1);
							value = value.Insert(iBackslash, "\n");
							break;
						case '"':
							break;
						case '\\':
							break;
						// The next two aren't handled on extract, but they may exist...
						case 't':
							value = value.Remove(iBackslash, 1);
							value = value.Insert(iBackslash, "\t");
							break;
						case 'r':
							value = value.Remove(iBackslash, 1);
							value = value.Insert(iBackslash, "\r");
							break;
						default:
							value = value.Insert(iBackslash, "\\");	// put it back...
							break;
					}
				}
				++iBackslash;
				if (iBackslash >= value.Length)
					break;
				iBackslash = value.IndexOf('\\', iBackslash);
			}
			return value;
		}

		internal List<string> UserComments { get; private set; }

		internal List<string> AutoComments { get; private set; }

		internal List<string> Reference { get; private set; }

		internal List<string> Flags { get; private set; }

		internal List<string> MsgIds { get; private set; }

		internal List<string> MsgStrings { get; set; }

		/// <summary>
		/// CompareMsgIds two POString objects with the purpose of sorting the PO file by msgid.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static int CompareMsgIds(PoString left, PoString right)
		{
			if (left.MsgIds == null && right.MsgIds == null)
				return 0;
			if (left.MsgIds == null)
				return -1;
			if (right.MsgIds == null)
				return 1;
			for (var i = 0; i < left.MsgIds.Count && i < right.MsgIds.Count; ++i)
			{
				var s1 = RemoveLeadingCruft(left.MsgIds[i].Replace("&", "").Trim());
				var s2 = RemoveLeadingCruft(right.MsgIds[i].Replace("&", "").Trim());
				var n = string.Compare(s1, s2, StringComparison.Ordinal);
				if (n != 0)
					return n;
			}
			if (left.MsgIds.Count < right.MsgIds.Count)
				return -1;
			return left.MsgIds.Count > right.MsgIds.Count ? 1 : string.Compare(left.MsgIds[0], right.MsgIds[0], StringComparison.Ordinal);
		}

		/// <summary>
		/// Remove leading characters from the string that we don't want to compare, at least
		/// not if we don't have to.
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		private static string RemoveLeadingCruft(string s)
		{
			var s1 = s.Replace("\\n", "\n");
			s1 = s1.Replace("\\t", "\t");
			while (s1.Length > 0 && !char.IsLetter(s1[0]))
				s1 = s1.Remove(0, 1);
			return s1;
		}

		public bool HasSameMsgId(PoString other)
		{
			if (MsgIds == null && other.MsgIds == null)
				return true;
			if (MsgIds == null || other.MsgIds == null)
				return false;
			for (var i = 0; i < MsgIds.Count && i < other.MsgIds.Count; ++i)
			{
				var s1 = MsgIds[i];
				var s2 = other.MsgIds[i];
				if (s1 != s2)
					return false;
			}
			return MsgIds.Count == other.MsgIds.Count;
		}

		public bool IsObsolete { get; set; }

		/// <summary>
		/// Write the contents of this object to the given StreamWriter.
		/// </summary>
		/// <param name="outputStream"></param>
		internal void Write(StreamWriter outputStream)
		{
			if (Reference != null)
			{
				foreach (var reference in Reference)
					WriteComment(reference, ':', outputStream);
			}
			if (AutoComments != null)
			{
				// Sort the set of path comments generated from the XML configuration files
				var idx = AutoComments.FindIndex(0, IsPathComment);
				if (idx >= 0)
				{
					AutoComments.Sort(idx, AutoComments.Count - idx, null);
				}
				foreach (var comment in AutoComments)
					WriteComment(comment, '.', outputStream);
			}
			if (_usageCount > 1)
				WriteComment($"(String used {_usageCount} times.)", '.', outputStream);
			if (Flags != null)
			{
				foreach (var flag in Flags)
					WriteComment(flag, ',', outputStream);
			}
			if (UserComments != null)
			{
				foreach (var comment in UserComments)
					WriteComment(comment, ' ', outputStream);
			}
			WriteMsgBundle("msgid", MsgIds, outputStream);
			WriteMsgBundle("msgstr", MsgStrings, outputStream);
			outputStream.WriteLine("");
		}

		/// <summary>
		/// Write the strings associated with either the Id or the value.
		/// </summary>
		/// <param name="type"></param>
		/// <param name="strings"></param>
		/// <param name="outputStream"></param>
		private static void WriteMsgBundle(string type, IReadOnlyList<string> strings, StreamWriter outputStream)
		{
			outputStream.Write($"{type} ");
			if (strings.Count != 1 && !(strings.Count == 2 && string.IsNullOrEmpty(strings[1])))
				WriteQuotedLine("", false, outputStream);
			for (var i = 0; i < strings.Count; ++i)
			{
				var fLast = i + 1 == strings.Count;
				if (fLast && string.IsNullOrEmpty(strings[i]) && strings.Count != 1)
					continue;

				WriteQuotedLine(type == "msgstr" ? ReQuote(strings[i]) : strings[i], !fLast, outputStream);
			}
		}

		/// <summary>
		/// Write a comment of the given type to the PO file.
		/// </summary>
		/// <param name="comment">comment string</param>
		/// <param name="flag">flag character indicating type of comment</param>
		/// <param name="outputStream">output StreamWriter</param>
		public static void WriteComment(string comment, char flag, StreamWriter outputStream)
		{
			outputStream.WriteLine("#{0} {1}", flag, comment);
		}

		/// <summary>
		/// write a quoted line to the output PO file, optionally appending "\\n" to the end.
		/// </summary>
		/// <param name="value">string value</param>
		/// <param name="appendNewline">true if "\\n" should be added to the end of the string</param>
		/// <param name="outputStream">output StreamWriter</param>
		public static void WriteQuotedLine(string value, bool appendNewline, StreamWriter outputStream)
		{
			if (appendNewline && !value.EndsWith("\\n"))
			{
				outputStream.WriteLine("\"{0}\\n\"", value);
			}
			else
			{
				outputStream.WriteLine("\"{0}\"", value);
			}
		}

		private static string ReQuote(string value)
		{
			var s1 = value.Replace("\\", "\\\\");
			var s2 = s1.Replace("\"", "\\\"");
			var s3 = s2.Replace("\t", "\\t");
			var s4 = s3.Replace("\n", "\\n");
			var s5 = s4.Replace("\r", "\\r");
			return s5;
		}

		/// <summary>
		/// Since we're creating a single POT, we want to merge strings that are identical.
		/// </summary>
		/// <param name="poStrings"></param>
		internal static void MergeDuplicateStrings(List<PoString> poStrings)
		{
			for (var i = 1; i < poStrings.Count; ++i)
			{
				if (!poStrings[i - 1].HasSameMsgId(poStrings[i]))
					continue;
				if (poStrings[i].AutoComments != null)
				{
					foreach (var comment in poStrings[i].AutoComments)
					{
						poStrings[i - 1].AddAutoComment(comment);
					}
				}
				poStrings.RemoveAt(i);
				++poStrings[i - 1]._usageCount;
				--i;
			}
		}

		/// <summary>
		///
		/// </summary>
		public bool HasEmptyMsgStr
		{
			get
			{
				if (MsgStrings == null || MsgStrings.Count == 0)
					return true;
				return MsgStrings.Count <= 1 && string.IsNullOrEmpty(MsgStrings[0]);
			}
		}

		/// <summary>
		///
		/// </summary>
		public string MsgIdAsString()
		{
			return StringListAsString(MsgIds);
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public string MsgStrAsString()
		{
			return StringListAsString(MsgStrings);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="strings"></param>
		/// <returns></returns>
		private static string StringListAsString(IReadOnlyList<string> strings)
		{
			if (strings == null || strings.Count == 0)
				return "";
			if (strings.Count == 1 && string.IsNullOrEmpty(strings[0]))
				return "";
			var bldr = new StringBuilder();
			foreach (var originalString in strings)
			{
				var s = originalString;
				var iBackslash = s.IndexOf('\\');
				while (iBackslash >= 0)
				{
					if (iBackslash + 1 < s.Length)
					{
						s = s.Remove(iBackslash, 1);
						switch (s[iBackslash])
						{
							case 'n':
								s = s.Remove(iBackslash, 1);
								s = s.Insert(iBackslash, "\n");
								break;
							case 't':
								s = s.Remove(iBackslash, 1);
								s = s.Insert(iBackslash, "\t");
								break;
						}
					}
					if (iBackslash + 1 < s.Length)
					{
						iBackslash = s.IndexOf('\\', iBackslash + 1);
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

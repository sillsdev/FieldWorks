// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SIL.FieldWorks.Build.Tasks.Localization
{
	/// <summary>
	/// This class was copied from the LocaleStrings project in https://github.com/sillsdev/fwsupporttools
	/// </summary>
	public class POString
	{
		private int _mCDup;

		public POString(IEnumerable<string> comments, IEnumerable<string> ids) : this (comments, ids, new[] {""}) {}

		public POString(IEnumerable<string> comments, IEnumerable<string> ids, IEnumerable<string> values)
		{
			if (comments != null)
			{
				AutoComments = new List<string>(comments);
			}
			MsgId = new List<string>(ids);
			MsgStr = new List<string>(values);

			_mCDup = 1;
		}

		public POString()
		{
			_mCDup = 1;
		}

		public override string ToString()
		{
			return $"POString msgid=\"{MsgIdAsString()}\"";
		}

		public static int InputLineNumber { get; private set; }

		/// <remarks>
		/// REVIEW (Hasso) 2020.02: bad! bad! bad!
		/// ENHANCE (Hasso) 2020.02: `InputLineNumber = 0` should go in the constructor of a new class, PoFileReader
		/// </remarks>
		public static void ResetInputLineNumber()
		{
			InputLineNumber = 0;
		}

		public static POString ReadFromFile(TextReader srIn)
		{
			// Move past any blank lines, checking for the end of file.
			var s = srIn.ReadLine();
			if (s == null)
				return null;
			++InputLineNumber;
			s = s.Trim();
			while (string.IsNullOrEmpty(s))
			{
				s = srIn.ReadLine();
				if (s == null)
					return null;
				++InputLineNumber;
				s = s.Trim();
			}
			var pos = new POString();
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
					if (pos.IsObsolete)
						pos.UserComments.Add(s.Substring(1));
					else
						pos.AddUserComment(s.Substring(2));
				}
				else if (s.StartsWith("#."))
				{
					if (pos.IsObsolete)
						pos.UserComments.Add(s.Substring(1));
					else
						pos.AddAutoComment(s.Substring(2).TrimStart());
				}
				else if (s.StartsWith("#:"))
				{
					if (pos.IsObsolete)
						pos.UserComments.Add(s.Substring(1));
					else
						pos.AddReference(s.Substring(2).TrimStart());
				}
				else if (s.StartsWith("#,"))
				{
					if (pos.IsObsolete)
						pos.UserComments.Add(s.Substring(1));
					else
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
					if (!pos.IsObsolete)
					{
						if (pos.UserComments != null)
						{
							for (var i = 0; i < pos.UserComments.Count; ++i)
							{
								if (pos.UserComments[i] != null)
									pos.UserComments[i] = " " + pos.UserComments[i];
							}
						}

						pos.AutoComments?.Clear();
						pos.References?.Clear();
						pos.Flags?.Clear();
						pos.MsgId?.Clear();
						pos.MsgStr?.Clear();
						pos.IsObsolete = true;
					}
					pos.AddUserComment(s.Substring(1));
				}
				else if (string.IsNullOrEmpty(s))
				{
					Console.WriteLine("Ignoring invalid empty line at line {0}", InputLineNumber);
				}
				else
				{
					fError = true;
				}
				if (fError)
				{
					Console.WriteLine("INVALID PO FILE: ERROR ON LINE " + InputLineNumber);
					throw new Exception("BAD PO FILE");
				}
				s = srIn.ReadLine();
				if (s == null)
					break;
				++InputLineNumber;
				s = s.Trim();
			} while (!string.IsNullOrEmpty(s) || pos.MsgId == null || pos.MsgStr == null);
			// Multiline messages start with an empty line in PO files.  Remove it from our data.
			// (Otherwise we add an extra "\n" line on output.)
			if (pos.MsgId != null && pos.MsgId.Count > 1 && string.IsNullOrEmpty(pos.MsgId[0]))
				pos.MsgId.RemoveAt(0);
			if (pos.MsgStr != null && pos.MsgStr.Count > 1 && string.IsNullOrEmpty(pos.MsgStr[0]))
				pos.MsgStr.RemoveAt(0);
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
			if (s.StartsWith("/") || s.StartsWith("(String used "))
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

		private void AddReference(string s)
		{
			if (References == null)
				References = new List<string>();
			if (!References.Contains(s))
				References.Add(s);
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
			if (MsgId == null)
				MsgId = new List<string>();
			var idx1 = s.IndexOf('"');
			var idx2 = s.LastIndexOf('"');
			if (idx1 == -1 || idx2 == -1 || idx1 == idx2)
				throw new Exception("Invalid msgid line: not quoted");
			++idx1;
			MsgId.Add(s.Substring(idx1, idx2 - idx1));
		}

		internal void AddMsgStrLine(string s)
		{
			if (MsgStr == null)
				MsgStr = new List<string>();
			var idx1 = s.IndexOf('"');
			var idx2 = s.LastIndexOf('"');
			if (idx1 == -1 || idx2 == -1 || idx1 == idx2)
				throw new Exception("Invalid msgstr line: not quoted");
			++idx1;
			MsgStr.Add(DeQuote(s.Substring(idx1, idx2 - idx1)));
		}


		private static string DeQuote(string sValue)
		{
			var idx = sValue.IndexOf('\\');
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

		internal List<string> UserComments { get; private set; }

		internal List<string> AutoComments { get; private set; }

		internal List<string> References { get; private set; }

		internal List<string> Flags { get; private set; }

		internal List<string> MsgId { get; private set; }

		internal List<string> MsgStr { get; set; }

		/// <summary>
		/// CompareMsgIds two POString objects with the purpose of sorting the PO file by msgid.
		/// </summary>
		public static int CompareMsgIds(POString x, POString y)
		{
			if (x.MsgId == null && y.MsgId == null)
				return 0;
			if (x.MsgId == null)
				return -1;
			if (y.MsgId == null)
				return 1;

			// First, ignore case and cruft so that similar strings are close together
			for (var i = 0; i < x.MsgId.Count && i < y.MsgId.Count; ++i)
			{
				var s1 = RemoveCruft(x.MsgId[i]);
				var s2 = RemoveCruft(y.MsgId[i]);
				var n = string.Compare(s1, s2, StringComparison.OrdinalIgnoreCase);
				if (n != 0)
					return n;
			}
			if (x.MsgId.Count < y.MsgId.Count)
				return -1;
			if (x.MsgId.Count > y.MsgId.Count)
				return 1;

			// If the strings are otherwise identical, sort including case and cruft so that
			// identical strings can be merged without the interference of not-quite-identical strings
			return string.Compare(x.MsgId[0], y.MsgId[0], StringComparison.Ordinal);
		}

		/// <summary>
		/// Remove leading and control characters from the string that we don't want to compare, at least not if we don't have to.
		/// </summary>
		private static string RemoveCruft(string s)
		{
			// Remove hotkey markers and trim whitespace
			var s1 = s.Replace("&", "").Replace("_", "").Trim()
				// unescape newlines and tabs
				.Replace(@"\n", "\n").Replace(@"\t", "\t");
			// Remove any leading non-letters (format markers, etc.)
			while (s1.Length > 0 && !char.IsLetter(s1[0]))
				s1 = s1.Substring(1);
			return s1;
		}

		public bool HasSameMsgId(POString that)
		{
			if (MsgId == null && that.MsgId == null)
				return true;
			if (MsgId == null || that.MsgId == null || MsgId.Count != that.MsgId.Count)
				return false;
			for (var i = 0; i < MsgId.Count; ++i)
			{
				var s1 = MsgId[i];
				var s2 = that.MsgId[i];
				if (s1 != s2)
					return false;
			}
			return true;
		}

		public bool IsObsolete { get; set; }

		/// <summary>
		/// Write the contents of this object to the given TextWriter.
		/// </summary>
		internal void Write(TextWriter swOut)
		{
			if (UserComments != null)
			{
				if (IsObsolete)
				{
					foreach (var comment in UserComments)
						swOut.WriteLine("#" + comment);
					return;
				}
				foreach (var comment in UserComments)
					WriteComment(comment, ' ', swOut);
			}
			if (References != null)
			{
				foreach (var reference in References)
					WriteComment(reference, ':', swOut);
			}
			if (AutoComments != null)
			{
				// Sort the set of path comments generated from the XML configuration files
				var idx = AutoComments.FindIndex(0, IsPathComment);
				if (idx >= 0)
				{
					var cPath = AutoComments.Count - idx;
					var lastComment = AutoComments[AutoComments.Count - 1];
					if (_mCDup == 1 && lastComment.StartsWith("(String used "))
						--cPath;
					AutoComments.Sort(idx, cPath, null);
				}
				foreach (var comment in AutoComments)
					WriteComment(comment, '.', swOut);
			}
			if (_mCDup > 1)
				WriteComment($"(String used {_mCDup} times.)", '.', swOut);
			if (Flags != null)
			{
				foreach (var flag in Flags)
					WriteComment(flag, ',', swOut);
			}
			WriteMsgBundle("msgid", MsgId, swOut);
			WriteMsgBundle("msgstr", MsgStr, swOut);
			swOut.WriteLine("");
		}

		/// <summary>
		/// Write the strings associated with either the Id or the value.
		/// </summary>
		private static void WriteMsgBundle(string sType, List<string> rgs, TextWriter swOut)
		{
			swOut.Write($"{sType} ");
			if (rgs.Count > 1 && !(rgs.Count == 2 && string.IsNullOrEmpty(rgs[1])))
				WriteQuotedLine("", swOut);
			for (var i = 0; i < rgs.Count; ++i)
			{
				var fFinalLine = i + 1 == rgs.Count;
				if (rgs.Count == 1 || !string.IsNullOrEmpty(rgs[i]) || !fFinalLine)
				{
					WriteQuotedLine(sType == "msgstr" ? ReQuote(rgs[i]) : rgs[i], swOut);
				}
			}
		}

		/// <summary>
		/// Write a comment of the given type to the PO file.
		/// </summary>
		/// <param name="comment">comment string</param>
		/// <param name="chFlag">flag character indicating type of comment</param>
		/// <param name="swOut">output TextWriter</param>
		public static void WriteComment(string comment, char chFlag, TextWriter swOut)
		{
			if (chFlag == ' ')
				swOut.WriteLine("# {0}", comment);
			else
				swOut.WriteLine("#{0} {1}", chFlag, comment);
		}

		/// <summary>
		/// write a quoted line to the output PO file.
		/// </summary>
		/// <param name="value">string value</param>
		/// <param name="swOut">output TextWriter</param>
		public static void WriteQuotedLine(string value, TextWriter swOut)
		{
				swOut.WriteLine("\"{0}\"", value);
		}

		private static string ReQuote(string value)
		{
			return value.Replace(@"\", @"\\").Replace(@"""", @"\""").Replace("\t", @"\t").Replace("\n", @"\n").Replace("\r", @"\r");
		}

		/// <summary>
		/// Since we're creating a single POT, we want to merge strings that are identical.
		/// </summary>
		internal static void MergeDuplicateStrings(List<POString> poStrings)
		{
			for (var i = 1; i < poStrings.Count; i++)
			{
				if (poStrings[i - 1].HasSameMsgId(poStrings[i]))
				{
					if (poStrings[i].AutoComments != null)
					{
						foreach (var comment in poStrings[i].AutoComments)
						{
							poStrings[i - 1].AddAutoComment(comment);
						}
					}
					poStrings.RemoveAt(i);
					poStrings[i - 1]._mCDup++;
					i--;
				}
			}
		}

		public bool HasEmptyMsgStr
		{
			get
			{
				if (MsgStr == null || MsgStr.Count == 0)
					return true;
				if (MsgStr.Count > 1)
					return false;
				return string.IsNullOrEmpty(MsgStr[0]);
			}
		}

		public string MsgIdAsString()
		{
			return StringListAsString(MsgId);
		}

		public string MsgStrAsString()
		{
			return StringListAsString(MsgStr);
		}

		private static string StringListAsString(List<string> strings)
		{
			if (strings == null || strings.Count == 0)
				return "";
			if (strings.Count == 1 && string.IsNullOrEmpty(strings[0]))
				return "";
			var bldr = new StringBuilder();
			foreach (var str in strings)
			{
				var s = str;
				var idx = s.IndexOf('\\');
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

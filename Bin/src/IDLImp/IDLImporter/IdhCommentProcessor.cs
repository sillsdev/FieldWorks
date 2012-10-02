// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: IdhCommentProcessor.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using antlr;

namespace SIL.FieldWorks.Tools
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class that is able to extract comments from an IDH file.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class IdhCommentProcessor
	{
		#region CommentInfo class
		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Holds comment and children (e.g. for a class these are the methods/properties)
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		public class CommentInfo
		{
			private StringBuilder m_commentBldr;

			private Dictionary<string, CommentInfo> m_Children;
			private Dictionary<string, string> m_Attributes;
			private int m_LineNumber;
			private bool m_fClean;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="T:CommentInfo"/> class.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public CommentInfo()
			{
				m_commentBldr = new StringBuilder();
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="T:CommentInfo"/> class.
			/// </summary>
			/// <param name="comment">The comment.</param>
			/// <param name="children">The children.</param>
			/// <param name="lineNo">The line number where current comment starts</param>
			/// --------------------------------------------------------------------------------
			public CommentInfo(string comment, Dictionary<string, CommentInfo> children,
				int lineNo)
				: this()
			{
				m_commentBldr.Append(comment);
				m_Children = children;
				m_LineNumber = lineNo;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets or sets (appends) the comment string.
			/// </summary>
			/// <value>The comment.</value>
			/// --------------------------------------------------------------------------------
			public string Comment
			{
				get
				{
					m_commentBldr.Replace("//", "");
					m_commentBldr.Replace("\t", " ");
					m_commentBldr.Replace("  ", " "); // replace two spaces with one
					return m_commentBldr.ToString().TrimEnd('\r', '\n', ' ');
				}
				set { m_commentBldr.Append(value); }
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets or sets the children.
			/// </summary>
			/// <value>The children.</value>
			/// --------------------------------------------------------------------------------
			public Dictionary<string, CommentInfo> Children
			{
				get
				{
					if (m_Children == null)
						m_Children = new Dictionary<string, CommentInfo>();
					return m_Children;
				}
				set
				{
					if (m_Children == null)
						m_Children = value;
					else
					{
						foreach (string key in value.Keys)
						{
							if (!m_Children.ContainsKey(key))
								m_Children.Add(key, value[key]);
						}
					}

				}
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the attributes.
			/// </summary>
			/// <value>The attributes.</value>
			/// --------------------------------------------------------------------------------
			public Dictionary<string, string> Attributes
			{
				get
				{
					if (m_Attributes == null)
						m_Attributes = new Dictionary<string, string>();
					return m_Attributes;
				}
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets or sets the line number.
			/// </summary>
			/// <value>The line number.</value>
			/// --------------------------------------------------------------------------------
			public int LineNumber
			{
				get { return m_LineNumber; }
				set { m_LineNumber = value; }
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Clean up the comments.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public void CleanupComment()
			{
				if (!m_fClean)
				{
					m_commentBldr.Replace("&", "&amp;");
					m_commentBldr.Replace("<", "&lt;");
					m_commentBldr.Replace(">", "&gt;");
				}
				m_fClean = true;

				ConvertSurveyorTag(@"@null\{(?<content>[^}]*)\}", ""); // no output
				ConvertSurveyorTag(@"/\*:Ignore ([^/*:]|/|\*|:)+/\*:End Ignore\*/", ""); // no output
				ConvertSurveyorTag(@"//:>([^\n]+)\n", ""); // no output

				ConvertSurveyorTag(@"@h3\{Hungarian: (?<content>[^}]*)\}", "Hungarian: <c>{0}</c>");
				ConvertSurveyorTag(@"@h3\{(?<content>[^}]*)\}", "<h3>{0}</h3>");
				ConvertSurveyorTag(@"@b\{(?<content>[^}]*)\}", "<b>{0}</b>");
				ConvertSurveyorTag(@"@i\{(?<content>[^}]*)\}", "<i>{0}</i>");
				ConvertSurveyorTag(@"@code\{(?<content>[^}]*)\}", "<code>{0}</code>");
				ConvertSurveyorTag(@"@return (?<content>[^\n]+\n)", "{0}");
				//ConvertSurveyorTag(@"@HTTP\{(?<content>[^}]*)\}", @"<see href=""{0}"">{0}</see>");

				// Remove //--- and /*********** lines
				Regex line = new Regex(@"(\r\n)?(/\*|//)?(-|\*)+(\r\n|/)?");
				if (line.IsMatch(m_commentBldr.ToString()))
				{
					MatchCollection matches = line.Matches(m_commentBldr.ToString());
					for (int i = matches.Count; i > 0; i--)
					{
						Match match = matches[i - 1];
						m_commentBldr.Remove(match.Index, match.Length);
					}
				}

				Regex param = new Regex(@"@param (?<name>(\w|/)+) (?<comment>([^@]|@i|@b)*)");
				if (param.IsMatch(m_commentBldr.ToString()))
				{
					MatchCollection matches = param.Matches(m_commentBldr.ToString());
					for (int i = matches.Count; i > 0; i--)
					{
						Match match = matches[i - 1];
						m_commentBldr.Remove(match.Index, match.Length);

						string paramName = IDLConversions.ConvertParamName(match.Groups["name"].Value);
						if (!Children.ContainsKey(paramName))
						{
							Console.WriteLine("Parameter mentioned in @param doesn't exist: {0}",
								match.Groups["name"].Value);
							continue;
						}
						StringBuilder bldr = Children[paramName]
							.m_commentBldr;
						bldr.Length = 0;
						bldr.Append(match.Groups["comment"].Value);
						Children[paramName].m_fClean = true;
					}
				}

				Regex exception = new Regex(@"@exception (?<hresult>\w+) (?<comment>([^@]|@i|@b)*)");
				if (exception.IsMatch(m_commentBldr.ToString()))
				{
					MatchCollection matches = exception.Matches(m_commentBldr.ToString());
					for (int i = matches.Count; i > 0; i--)
					{
						Match match = matches[i - 1];
						m_commentBldr.Remove(match.Index, match.Length);
						string exceptionType = TranslateHResultToException(match.Groups["hresult"].Value);
						if (Attributes.ContainsKey("exception"))
							Attributes["exception"] = Attributes["exception"] + "," + exceptionType;
						else
							Attributes.Add("exception", exceptionType);
						Attributes.Add(exceptionType, string.Format("{0} ({1})",
							match.Groups["comment"].Value.Replace("//", "").TrimEnd('\n', '\r', ' '),
							match.Groups["hresult"].Value));
					}
				}

				Regex list = new Regex(@"@list\{(?<content>[^}]*)\}");
				if (list.IsMatch(m_commentBldr.ToString()))
				{
					MatchCollection matches = list.Matches(m_commentBldr.ToString());
					for (int i = matches.Count; i > 0; i--)
					{
						Match match = matches[i - 1];
						m_commentBldr.Remove(match.Index, match.Length);
						if (i == matches.Count)
							m_commentBldr.Insert(match.Index, string.Format(" {0}</list>", Environment.NewLine));
						m_commentBldr.Insert(match.Index,
							string.Format("<item><description>{0} {1}{0} </description></item>",
							Environment.NewLine, match.Groups["content"].Value));
						if (i == 1)
							m_commentBldr.Insert(match.Index, string.Format(@"<list type=""bullet"">{0} ",
								Environment.NewLine));
					}
				}

				if (m_commentBldr.Length > 0)
				{
					using (StringReader reader = new StringReader(m_commentBldr.ToString()))
					{
						StringBuilder bldr = new StringBuilder();
						SurveyorLexer lexer = new SurveyorLexer(bldr, reader);
						lexer.setLine(LineNumber);
						SurveyorParser parser = new SurveyorParser(bldr, lexer);
						parser.surveyorTags();
						m_commentBldr = bldr;
					}
				}

				// Do this at the end because we might need it above to determine the end of the other tags
				ConvertSurveyorTag(@"@end", ""); // no output
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Translates the HResult to an exception.
			/// </summary>
			/// <param name="hresult">The HResult.</param>
			/// <returns>The exception that .NET will throw</returns>
			/// --------------------------------------------------------------------------------
			private string TranslateHResultToException(string hresult)
			{
				#region HRESULTS
				switch (hresult)
				{
					case "MSEE_E_APPDOMAINUNLOADED":
						return "AppDomainUnloadedException";
					case "COR_E_APPLICATION":
						return "ApplicationException";
					case "COR_E_ARGUMENT":
					case "E_INVALIDARG":
						return "ArgumentException";
					case "COR_E_ARGUMENTOUTOFRANGE":
						return "ArgumentOutOfRangeException";
					case "COR_E_ARITHMETIC":
					case "ERROR_ARITHMETIC_OVERFLOW":
						return "ArithmeticException";
					case "COR_E_ARRAYTYPEMISMATCH":
						return "ArrayTypeMismatchException";
					case "COR_E_BADIMAGEFORMAT":
					case "ERROR_BAD_FORMAT":
						return "BadImageFormatException";
					case "COR_E_COMEMULATE_ERROR":
						return "COMEmulateException";
					case "COR_E_CONTEXTMARSHAL":
						return "ContextMarshalException";
					case "COR_E_CORE":
						return "CoreException";
					case "NTE_FAIL":
						return "CryptographicException";
					case "COR_E_DIRECTORYNOTFOUND":
					case "ERROR_PATH_NOT_FOUND":
						return "DirectoryNotFoundException";
					case "COR_E_DIVIDEBYZERO":
						return "DivideByZeroException";
					case "COR_E_DUPLICATEWAITOBJECT":
						return "DuplicateWaitObjectException";
					case "COR_E_ENDOFSTREAM":
						return "EndOfStreamException";
					//case "COR_E_TYPELOAD":
					//    return "EntryPointNotFoundException";
					case "COR_E_EXCEPTION":
						return "Exception";
					case "COR_E_EXECUTIONENGINE":
						return "ExecutionEngineException";
					case "COR_E_FIELDACCESS":
						return "FieldAccessException";
					case "COR_E_FILENOTFOUND":
					case "ERROR_FILE_NOT_FOUND":
						return "FileNotFoundException";
					case "COR_E_FORMAT":
						return "FormatException";
					case "COR_E_INDEXOUTOFRANGE":
						return "IndexOutOfRangeException";
					case "COR_E_INVALIDCAST":
					case "E_NOINTERFACE":
						return "InvalidCastException";
					case "COR_E_INVALIDCOMOBJECT":
						return "InvalidComObjectException";
					case "COR_E_INVALIDFILTERCRITERIA":
						return "InvalidFilterCriteriaException";
					case "COR_E_INVALIDOLEVARIANTTYPE":
						return "InvalidOleVariantTypeException";
					case "COR_E_INVALIDOPERATION":
						return "InvalidOperationException";
					case "COR_E_IO":
						return "IOException";
					case "COR_E_MEMBERACCESS":
						return "AccessException";
					case "COR_E_METHODACCESS":
						return "MethodAccessException";
					case "COR_E_MISSINGFIELD":
						return "MissingFieldException";
					case "COR_E_MISSINGMANIFESTRESOURCE":
						return "MissingManifestResourceException";
					case "COR_E_MISSINGMEMBER":
						return "MissingMemberException";
					case "COR_E_MISSINGMETHOD":
						return "MissingMethodException";
					case "COR_E_MULTICASTNOTSUPPORTED":
						return "MulticastNotSupportedException";
					case "COR_E_NOTFINITENUMBER":
						return "NotFiniteNumberException";
					case "E_NOTIMPL":
						return "NotImplementedException";
					case "COR_E_NOTSUPPORTED":
						return "NotSupportedException";
					case "COR_E_NULLREFERENCE":
					case "E_POINTER":
						return "NullReferenceException";
					case "COR_E_OUTOFMEMORY":
					case "E_OUTOFMEMORY":
						return "OutOfMemoryException";
					case "COR_E_OVERFLOW":
						return "OverflowException";
					case "COR_E_PATHTOOLONG":
					case "ERROR_FILENAME_EXCED_RANGE":
						return "PathTooLongException";
					case "COR_E_RANK":
						return "RankException";
					case "COR_E_REFLECTIONTYPELOAD":
						return "ReflectionTypeLoadException";
					case "COR_E_REMOTING":
						return "RemotingException";
					case "COR_E_SAFEARRAYTYPEMISMATCH":
						return "SafeArrayTypeMismatchException";
					case "COR_E_SECURITY":
						return "SecurityException";
					case "COR_E_SERIALIZATION":
						return "SerializationException";
					case "COR_E_STACKOVERFLOW":
					case "ERROR_STACK_OVERFLOW":
						return "StackOverflowException";
					case "COR_E_SYNCHRONIZATIONLOCK":
						return "SynchronizationLockException";
					case "COR_E_SYSTEM":
						return "SystemException";
					case "COR_E_TARGET":
						return "TargetException";
					case "COR_E_TARGETINVOCATION":
						return "TargetInvocationException";
					case "COR_E_TARGETPARAMCOUNT":
						return "TargetParameterCountException";
					case "COR_E_THREADABORTED":
						return "ThreadAbortException";
					case "COR_E_THREADINTERRUPTED":
						return "ThreadInterruptedException";
					case "COR_E_THREADSTATE":
						return "ThreadStateException";
					case "COR_E_THREADSTOP":
						return "ThreadStopException";
					case "COR_E_TYPELOAD":
						return "TypeLoadException";
					case "COR_E_TYPEINITIALIZATION":
						return "TypeInitializationException";
					case "COR_E_VERIFICATION":
						return "VerificationException";
					case "COR_E_WEAKREFERENCE":
						return "WeakReferenceException";
					case "COR_E_VTABLECALLSNOTSUPPORTED":
						return "VTableCallsNotSupportedException";
					default:
						return "COMException";
				}
				#endregion
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Converts a surveyor tag to a tag suitable for XML documentation.
			/// </summary>
			/// <param name="regexString">The regex string.</param>
			/// <param name="outputFormat">The output format.</param>
			/// --------------------------------------------------------------------------------
			private void ConvertSurveyorTag(string regexString, string outputFormat)
			{
				Regex regex = new Regex(regexString);
				if (regex.IsMatch(m_commentBldr.ToString()))
				{
					MatchCollection matches = regex.Matches(m_commentBldr.ToString());
					for (int i = matches.Count; i > 0; i--)
					{
						Match match = matches[i - 1];
						m_commentBldr.Remove(match.Index, match.Length);
						m_commentBldr.Insert(match.Index, string.Format(outputFormat,
							match.Groups["content"].Value));
					}
				}
			}

		}
		#endregion

		private Dictionary<string, CommentInfo> m_Comments;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:IdhCommentProcessor"/> class.
		/// </summary>
		/// <param name="fileNames">List of names of IDH files.</param>
		/// ------------------------------------------------------------------------------------
		public IdhCommentProcessor(StringCollection fileNames)
		{
			m_Comments = new Dictionary<string,CommentInfo>();

			if (fileNames == null || fileNames.Count == 0)
				return;

			foreach (string fileName in fileNames)
			{
				StringBuilder fileContent = new StringBuilder();
				using (StreamReader reader = new StreamReader(fileName))
				{
					IdhLexer lexer = new IdhLexer(reader);
					lexer.setFilename(fileName);
					IdhParser parser = new IdhParser(lexer);
					parser.setFilename(fileName);
					CommentInfo info = parser.idhfile();

					foreach (string iface in info.Children.Keys)
						m_Comments.Add(iface, info.Children[iface]);
				}
			}

			CleanupComments(m_Comments);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the comments.
		/// </summary>
		/// <value>The comments.</value>
		/// ------------------------------------------------------------------------------------
		public Dictionary<string, CommentInfo> Comments
		{
			get { return m_Comments; }
		}

		#region Private methods to read comments from IDH file

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Cleanups the comments.
		/// </summary>
		/// <param name="comments">The comments.</param>
		/// ------------------------------------------------------------------------------------
		private void CleanupComments(Dictionary<string, CommentInfo> comments)
		{
			foreach (string key in comments.Keys)
			{
				CommentInfo info = comments[key];
				info.CleanupComment();
				CleanupComments(info.Children);
			}
		}

		#endregion
	}
}

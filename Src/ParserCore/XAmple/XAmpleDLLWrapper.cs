// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using SIL.PlatformUtilities;

namespace SIL.FieldWorks.WordWorks.Parser.XAmple
{
	public delegate IntPtr SpecAmpleCreateSetup();
	public delegate string SpecAmpleDeleteSetup(IntPtr pSetupIo);
	public delegate string SpecAmpleLoadControlFiles(IntPtr pSetupIo, string pszAnalysisDataFileIn, string pszDictCodeTableIn, string pszDictOrthoChangeTable_in, string pszTextInputControlFile_in);
	public delegate string SpecAmpleLoadDictionary(IntPtr pSetupio, string pszFilePathin, string pszDictType);
	public delegate string SpecAmpleLoadGrammarFile(IntPtr pSetupio, string pszGrammarFilein);
	public delegate string SpecAmpleParseFile(IntPtr pSetupio, string pszInFilePathin, string pszOutFilePathin);
	public delegate string SpecAmpleParseText(IntPtr pSetupio, string pszInputTextin, string pszUseTextIn);
	public delegate string SpecAmpleGetAllAllomorphs(IntPtr pSetupio, string pszRestOfWordin, string pszStatein);
	public delegate string SpecAmpleApplyInputChangesToWord(IntPtr pSetupio, string pszWordin);
	public delegate string SpecAmpleSetParameter(IntPtr pSetupio, string pszNamein, string pszValuein);
	public delegate string SpecAmpleAddSelectiveAnalysisMorphs(IntPtr pSetupio, string pszMorphsin);
	public delegate string SpecAmpleRemoveSelectiveAnalysisMorphs(IntPtr pSetupio);
	public delegate string SpecAmpleReset(IntPtr pSetupio);
	public delegate string SpecAmpleReportVersion(IntPtr pSetupio);
	public delegate string SpecAmpleInitializeMorphChecking(IntPtr pSetupio);
	public delegate string SpecAmpleCheckMorphReferences(IntPtr pSetupio);
	public delegate string SpecAmpleInitializeTrace(IntPtr pSetupio);
	public delegate string SpecAmpleGetTrace(IntPtr pSetupio);
	public delegate int SpecAmpleThreadId();

	internal sealed class XAmpleDLLWrapper : IDisposable
	{
		[DllImport("xample.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr AmpleCreateSetup();

		[DllImport("xample.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr AmpleDeleteSetup(IntPtr pSetupIo);

		internal string AmpleDeleteSetupMarshaled(IntPtr pSetupIo)
		{
			var ret = AmpleDeleteSetup(pSetupIo);
			return Marshal.PtrToStringAnsi(ret);
		}

		[DllImport("xample.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr AmpleLoadControlFiles(IntPtr pSetupIo, string pszAnalysisDataFileIn, string pszDictCodeTableIn, string pszDictOrthoChangeTableIn, string pszTextInputControlFileIn);

		internal string AmpleLoadControlFilesMarshaled(IntPtr pSetupIo, string pszAnalysisDataFileIn, string pszDictCodeTableIn, string pszDictOrthoChangeTableIn, string pszTextInputControlFileIn)
		{
			var ret = AmpleLoadControlFiles(pSetupIo, pszAnalysisDataFileIn, pszDictCodeTableIn, pszDictOrthoChangeTableIn, pszTextInputControlFileIn);
			return Marshal.PtrToStringAnsi(ret);
		}

		[DllImport("xample.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr AmpleLoadDictionary(IntPtr pSetupIo, string pszFilePathIn, string pszDictType);

		internal string AmpleLoadDictionaryMarshaled(IntPtr pSetupIo, string pszFilePathIn, string pszDictType)
		{
			var ret = AmpleLoadDictionary(pSetupIo, pszFilePathIn, pszDictType);
			return Marshal.PtrToStringAnsi(ret);
		}

		[DllImport("xample.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr AmpleLoadGrammarFile(IntPtr pSetupIo, string pszGrammarFileIn);

		internal string AmpleLoadGrammarFileMarshaled(IntPtr pSetupIo, string pszGrammarFileIn)
		{
			var ret = AmpleLoadGrammarFile(pSetupIo, pszGrammarFileIn);
			return Marshal.PtrToStringAnsi(ret);
		}

		[DllImport("xample.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr AmpleParseText(IntPtr pSetupIo, byte[] pszInputTextIn, string pszUseTextIn);

		internal string AmpleParseTextMarshaled(IntPtr pSetupIo, string pszInputTextIn, string pszUseTextIn)
		{
			int bufferSize = Encoding.UTF8.GetByteCount(pszInputTextIn);
			byte[] temp = new byte[bufferSize + 1]; // +1 for NULL term
			int sizeWritten = Encoding.UTF8.GetBytes(pszInputTextIn, 0, pszInputTextIn.Length, temp, 0);
			Debug.Assert(sizeWritten == bufferSize);
			var ret = AmpleParseText(pSetupIo, temp, pszUseTextIn);
			return PtrToString(ret, Encoding.UTF8);
		}

		[DllImport("xample.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr AmpleGetAllAllomorphs(IntPtr pSetupIo, string pszRestOfWordIn, string pszState_in);

		internal string AmpleGetAllAllomorphsMarshaled(IntPtr pSetupIo, string pszRestOfWordIn, string pszStateIn)
		{
			var ret = AmpleGetAllAllomorphs(pSetupIo, pszRestOfWordIn, pszStateIn);
			return Marshal.PtrToStringAnsi(ret);
		}

		[DllImport("xample.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr AmpleApplyInputChangesToWord(IntPtr pSetupIo, string pszWordIn);

		internal string AmpleApplyInputChangesToWordMarshaled(IntPtr pSetupIo, string pszWordIn)
		{
			var ret = AmpleApplyInputChangesToWord(pSetupIo, pszWordIn);
			return Marshal.PtrToStringAnsi(ret);
		}

		[DllImport("xample.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr AmpleSetParameter(IntPtr pSetupIo, string pszNameIn, string pszValueIn);

		internal string AmpleSetParameterMarshaled(IntPtr pSetupIo, string pszNameIn, string pszValueIn)
		{
			var ret = AmpleSetParameter(pSetupIo, pszNameIn, pszValueIn);
			return Marshal.PtrToStringAnsi(ret);
		}

		[DllImport("xample.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr AmpleAddSelectiveAnalysisMorphs(IntPtr pSetupIo, string pszMorphsIn);

		internal string AmpleAddSelectiveAnalysisMorphsMarshaled(IntPtr pSetupIo, string pszMorphsIn)
		{
			var ret = AmpleAddSelectiveAnalysisMorphs(pSetupIo, pszMorphsIn);
			return Marshal.PtrToStringAnsi(ret);
		}

		[DllImport("xample.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr AmpleRemoveSelectiveAnalysisMorphs(IntPtr pSetupIo);

		internal string AmpleRemoveSelectiveAnalysisMorphMarshaled(IntPtr pSetupIo)
		{
			var ret = AmpleRemoveSelectiveAnalysisMorphs(pSetupIo);
			return Marshal.PtrToStringAnsi(ret);
		}

		[DllImport("xample.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr AmpleReset(IntPtr pSetupIo);

		internal string AmpleResetMarshaled(IntPtr pSetupIo)
		{
			var ret = AmpleReset(pSetupIo);
			return Marshal.PtrToStringAnsi(ret);
		}

		[DllImport("xample.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr AmpleReportVersion(IntPtr pSetupIo);

		internal string AmpleReportVersionMarshaled(IntPtr pSetupIo)
		{
			var ret = AmpleReportVersion(pSetupIo);
			return Marshal.PtrToStringAnsi(ret);
		}

		[DllImport("xample.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr AmpleInitializeMorphChecking(IntPtr pSetupIo);

		internal string AmpleInitializeMorphCheckingMarshaled(IntPtr pSetupIo)
		{
			var ret = AmpleInitializeMorphChecking(pSetupIo);
			return Marshal.PtrToStringAnsi(ret);
		}

		[DllImport("xample.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr AmpleCheckMorphReferences(IntPtr pSetupIo);

		internal string AmpleCheckMorphReferencesMarshaled(IntPtr pSetupIo)
		{
			var ret = AmpleCheckMorphReferences(pSetupIo);
			return Marshal.PtrToStringAnsi(ret);
		}

		[DllImport("xample.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr AmpleInitializeTraceString(IntPtr pSetupIo);

		internal string AmpleInitializeTraceStringMarshaled(IntPtr pSetupIo)
		{
			var ret = AmpleInitializeTraceString(pSetupIo);
			return Marshal.PtrToStringAnsi(ret);
		}

		[DllImport("xample.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern IntPtr AmpleGetTraceString(IntPtr pSetupIo);

		internal string AmpleGetTraceStringMarshaled(IntPtr pSetupIo)
		{
			var ret = AmpleGetTraceString(pSetupIo);
			return PtrToString(ret, Encoding.UTF8);
		}

		[DllImport("xample.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern int AmpleThreadId();

		static string PtrToString(IntPtr ptr, Encoding encoding)
		{
			var i = 0;
			byte b;
			var bytes = new List<byte>();
			while ((b = Marshal.ReadByte(ptr, i)) != 0)
			{
				bytes.Add(b);
				i++;
			}
			return encoding.GetString(bytes.ToArray());
		}

		public XAmpleDLLWrapper()
		{
			m_options = new AmpleOptions();
			Comment = '|';
		}

		#region Disposable stuff
#if DEBUG
		/// <summary/>
		~XAmpleDLLWrapper()
		{
			Dispose(false);
		}
#endif

		/// <summary/>
		private bool IsDisposed
		{
			get;
			set;
		}

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		private void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}
			if (disposing)
			{
				// dispose managed and unmanaged objects
				RemoveSetup();
			}
			IsDisposed = true;
		}
		#endregion

		public SpecAmpleParseFile AmpleParseFile { get; set; }
		public SpecAmpleParseText AmpleParseTextDelegate { get; set; }
		public SpecAmpleGetAllAllomorphs AmpleGetAllAllomorphsDelegate { get; set; }
		public SpecAmpleApplyInputChangesToWord AmpleApplyInputChangesToWordDelegate { get; set; }
		public SpecAmpleSetParameter AmpleSetParameterDelegate { get; set; }
		public SpecAmpleAddSelectiveAnalysisMorphs AmpleAddSelectiveAnalysisMorphsDelegate { get; set; }
		public SpecAmpleRemoveSelectiveAnalysisMorphs AmpleRemoveSelectiveAnalysisMorphsDelegate { get; set; }
		public char Comment { get; set; }

		internal string ParseString(string sInput)
		{
			var lpszResult = AmpleSetParameterDelegate(m_setup, "TraceAnalysis", "OFF");
			ThrowIfError(lpszResult);

			lpszResult = AmpleParseTextDelegate(GetSetup(), sInput, "n");
			ThrowIfError(lpszResult);

			return lpszResult;
		}

		internal string TraceString(string input, string sSelectedMorphs)
		{
			//Guarantee that tracing has been turned on to XML form
			var lpszResult = AmpleSetParameterDelegate(m_setup, "TraceAnalysis", "XML");
			ThrowIfError(lpszResult);

			// add any selected morphs
			lpszResult = AmpleAddSelectiveAnalysisMorphsDelegate(m_setup, sSelectedMorphs ?? " ");
			ThrowIfError(lpszResult);
			// Do trace
			m_ampleInitializeTrace(GetSetup());
			lpszResult = AmpleParseTextDelegate(GetSetup(), input, "n");
			//don't bother returning the result, just the trace
			ThrowIfError(lpszResult);
			// remove any selected morphs
			lpszResult = AmpleRemoveSelectiveAnalysisMorphsDelegate(m_setup);
			ThrowIfError(lpszResult);
			//Guarantee that tracing has been turned off
			lpszResult = AmpleSetParameterDelegate(m_setup, "TraceAnalysis", "OFF");
			ThrowIfError(lpszResult);
			return m_ampleGetTrace(GetSetup());
		}

		private void AssignDelegates()
		{
			m_ampleReset = AmpleResetMarshaled;
			m_ampleLoadControlFiles = AmpleLoadControlFilesMarshaled;
			m_ampleLoadDictionary = AmpleLoadDictionaryMarshaled;
			m_ampleCreateSetup = AmpleCreateSetup;
			m_ampleDeleteSetup = AmpleDeleteSetupMarshaled;
			m_ampleReportVersion = AmpleReportVersionMarshaled;
			m_ampleInitializeMorphChecking = AmpleInitializeMorphCheckingMarshaled;
			m_ampleCheckMorphReferences = AmpleCheckMorphReferencesMarshaled;
			m_ampleLoadGrammarFile = AmpleLoadGrammarFileMarshaled;
			m_ampleInitializeTrace = AmpleInitializeTraceStringMarshaled;
			m_ampleGetTrace = AmpleGetTraceStringMarshaled;
			m_pfAmpleThreadId = AmpleThreadId;

			AmpleParseFile = AmpleParseFile;
			AmpleParseTextDelegate = AmpleParseTextMarshaled;
			AmpleGetAllAllomorphsDelegate = AmpleGetAllAllomorphsMarshaled;
			AmpleApplyInputChangesToWordDelegate = AmpleApplyInputChangesToWordMarshaled;
			AmpleSetParameterDelegate = AmpleSetParameterMarshaled;
			AmpleAddSelectiveAnalysisMorphsDelegate = AmpleAddSelectiveAnalysisMorphsMarshaled;
			AmpleRemoveSelectiveAnalysisMorphsDelegate = AmpleRemoveSelectiveAnalysisMorphMarshaled;
		}

		internal void Init()
		{
			// TODO: Currently we are using fixed DllImports.
			AssignDelegates();

			if (m_setup != IntPtr.Zero)
			{
				RemoveSetup();
			}

			m_setup = AmpleCreateSetup();
			if (m_setup == IntPtr.Zero)
			{
				throw new ApplicationException("Could not create and Setup Ample DLL");
			}
		}

		internal void LoadFiles(string lspzFixedFilesDir, string lspzDynamicFilesDir, string lspzDatabaseName)
		{
			CheckPtr(m_setup);

			var lpszCdTable = $"{lspzFixedFilesDir}{Path.DirectorySeparatorChar}cd.tab";
			var lpszAdCtl = $"{lspzDynamicFilesDir}{Path.DirectorySeparatorChar}{lspzDatabaseName}adctl.txt";
			var lpszGram = $"{lspzDynamicFilesDir}{Path.DirectorySeparatorChar}{lspzDatabaseName}gram.txt";
			var lpszDict = $"{lspzDynamicFilesDir}{Path.DirectorySeparatorChar}{lspzDatabaseName}lex.txt";

			m_ampleReset(m_setup);

			SetOptions();

			// LOAD THE CONTROL FILES
			// ortho
			string sResult = m_ampleLoadControlFiles(m_setup, lpszAdCtl, lpszCdTable, null, null);
			// INTX
			ThrowIfError(sResult, lpszAdCtl, lpszCdTable);

			//LOAD ROOT DICTIONARIES
			sResult = m_ampleLoadDictionary(m_setup, lpszDict, "u");
			ThrowIfError(sResult, lpszDict);

			//LOAD GRAMMAR FILE
			sResult = m_ampleLoadGrammarFile(m_setup, lpszGram);
			ThrowIfError(sResult, lpszGram);
		}

		internal void SetParameter(string lspzName, string lspzValue)
		{
			if (lspzName == "MaxAnalysesToReturn")
			{
				m_options.MaxAnalysesToReturn = int.Parse(lspzValue);
			}
		}

		internal IntPtr GetSetup()
		{
			CheckPtr(m_setup);

			return m_setup;
		}

		internal void SetLogFile(string lpszPath)
		{
			throw new NotImplementedException();
		}

		internal int GetAmpleThreadId()
		{
			return Platform.IsWindows ? AmpleThreadId() : 0;
		}

		#region Protected members
		private SpecAmpleReset m_ampleReset;
		private SpecAmpleLoadControlFiles m_ampleLoadControlFiles;
		private SpecAmpleLoadDictionary m_ampleLoadDictionary;
		private SpecAmpleCreateSetup m_ampleCreateSetup;
		private SpecAmpleDeleteSetup m_ampleDeleteSetup;
		private SpecAmpleReportVersion m_ampleReportVersion;
		private SpecAmpleInitializeMorphChecking m_ampleInitializeMorphChecking;
		private SpecAmpleCheckMorphReferences m_ampleCheckMorphReferences;
		private SpecAmpleLoadGrammarFile m_ampleLoadGrammarFile;
		private SpecAmpleInitializeTrace m_ampleInitializeTrace;
		private SpecAmpleGetTrace m_ampleGetTrace;
		private SpecAmpleThreadId m_pfAmpleThreadId;
		private string m_logPath;
		private AmpleOptions m_options;
		private IntPtr m_setup;

		private void ThrowIfError(string result, params string[] filenames)
		{
			// REVIEW JohnH(RandyR):
			// Isn't the none in the XML supposed to have quote marks around it
			// in order to be valid XML?
			// Answer: much of the ample stuff is actually SGML. I don't have the code to check, but
			//chances are I wrote this right to first-time and it's just not good XML.
			//if there is an error tag
			// but doesn't say "none"
			if ((result.Contains("<error")) && (!result.Contains("<error code=none>")))
			{
				string t = Path.GetTempFileName();
				using (var stream = new StreamWriter(t))
				{
					stream.Write(result);
					stream.Close();
					var message = filenames.Length == 0 ? result : $"{result}; Files: {string.Join(",", filenames)}";
					throw new ApplicationException(message);
				}
			}
		}

		private void CheckPtr(IntPtr p)
		{
			if (p == IntPtr.Zero)
				throw new ApplicationException("ptr should not Equals IntPtr.Zero");
		}

		private void RemoveSetup()
		{
			// just quit if we weren't initialized (or if we've already cleaned up)
			if(m_setup == IntPtr.Zero)
			{
				return;
			}

			string sResult;
			if (m_ampleReset != null)
			{
				sResult = m_ampleReset(m_setup);
				ThrowIfError(sResult);
			}
			if (m_ampleDeleteSetup != null)
			{
				sResult = m_ampleDeleteSetup(m_setup);
				ThrowIfError(sResult);
			}

			m_setup = IntPtr.Zero;
		}

		private void SetOptions()
		{
			var lpszComment = Comment.ToString();

			var sResult = AmpleSetParameterDelegate(m_setup, "BeginComment", lpszComment);
			ThrowIfError(sResult);

			sResult = AmpleSetParameterDelegate(m_setup, "MaxMorphnameLength", m_options.MaxMorphnameLength.ToString());
			ThrowIfError(sResult);

			sResult = AmpleSetParameterDelegate(m_setup, "MaxTrieDepth", "3");
			ThrowIfError(sResult);

			sResult = AmpleSetParameterDelegate(m_setup, "RootGlosses", m_options.OutputRootGlosses ? "TRUE" : "FALSE");
			ThrowIfError(sResult);

			sResult = AmpleRemoveSelectiveAnalysisMorphsDelegate(m_setup);
			ThrowIfError(sResult);

			sResult = AmpleSetParameterDelegate(m_setup, "TraceAnalysis", m_options.Trace ? "XML" : "OFF");
			ThrowIfError(sResult);

			/* hab 1999.06.25 */
			sResult = AmpleSetParameterDelegate(m_setup, "CheckMorphReferences", m_options.CheckMorphnames ? "TRUE" : "FALSE");
			ThrowIfError(sResult);
			/* hab 1999.06.25 */

			sResult = AmpleSetParameterDelegate(m_setup, "OutputDecomposition", m_options.WriteDecompField ? "TRUE" : "FALSE");
			ThrowIfError(sResult);

			sResult = AmpleSetParameterDelegate(m_setup, "OutputOriginalWord", m_options.WriteWordField ? "TRUE" : "FALSE");
			ThrowIfError(sResult);

			sResult = AmpleSetParameterDelegate(m_setup, "OutputProperties", m_options.WritePField ? "TRUE" : "FALSE");
			ThrowIfError(sResult);

			sResult = AmpleSetParameterDelegate(m_setup, "ShowPercentages", m_options.ReportAmbiguityPercentages ? "TRUE" : "FALSE");
			ThrowIfError(sResult);

			//jdh june 13 2000

			sResult = AmpleSetParameterDelegate(m_setup, "OutputStyle", m_options.OutputStyle);
			ThrowIfError(sResult);

			sResult = AmpleSetParameterDelegate(m_setup, "AllomorphIds", "TRUE");
			ThrowIfError(sResult);

			//jdh feb 2003
			sResult = AmpleSetParameterDelegate(m_setup, "MaxAnalysesToReturn", m_options.MaxAnalysesToReturn.ToString());
			ThrowIfError(sResult);

			// hab 07 dec 2005
			sResult = AmpleSetParameterDelegate(m_setup, "RecognizeOnly", "TRUE");
			ThrowIfError(sResult);
		}
		#endregion
	}
}

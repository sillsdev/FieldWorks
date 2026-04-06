// Copyright (c) 2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using XAmpleManagedWrapper;

namespace XAmpleWithToneParse
{
	public class XAmpleDLLWrapperForTonePars : XAmpleManagedWrapper.XAmpleDLLWrapper
	{
		[DllImport("xample.dll", CallingConvention = CallingConvention.Cdecl)]
		internal static extern new IntPtr AmpleParseFile(
			IntPtr pSetupIo,
			byte[] pszInputTextIn,
			string pszUseTextIn
		);

		public SpecAmpleParseFile AmpleParseFileDelegate { get; set; }

		internal string AmpleParseFileMarshaled(
			IntPtr pSetupIo,
			string pszInFilePathin,
			string pszOutFilePathin
		)
		{
			int bufferSize = Encoding.UTF8.GetByteCount(pszInFilePathin);
			byte[] temp = new byte[bufferSize + 1]; // +1 for NULL term
			int sizeWritten = Encoding.UTF8.GetBytes(
				pszInFilePathin,
				0,
				pszInFilePathin.Length,
				temp,
				0
			);
			//Debug.Assert(sizeWritten == bufferSize);
			var ret = AmpleParseFile(pSetupIo, temp, pszOutFilePathin);
			return PtrToString(ret, Encoding.UTF8);
		}

		static string PtrToString(IntPtr ptr, Encoding encoding)
		{
			int i = 0;
			byte b;
			var bytes = new List<byte>();
			while ((b = Marshal.ReadByte(ptr, i)) != 0)
			{
				bytes.Add(b);
				i++;
			}
			return encoding.GetString(bytes.ToArray());
		}

		public void InitForTonePars()
		{
			Init();
			AmpleParseFileDelegate = AmpleParseFileMarshaled;
		}

		public string ParseFileForTonePars(string inputFile, string outputFile)
		{
			string lpszResult = AmpleSetParameterDelegate(m_setup, "TraceAnalysis", "OFF");
			ThrowIfError(lpszResult);

			lpszResult = AmpleParseFileDelegate(GetSetup(), inputFile, outputFile);
			ThrowIfError(lpszResult);

			return lpszResult;
		}

		public void LoadFilesForTonePars(
			string lspzFixedFilesDir,
			string lspzDynamicFilesDir,
			string lspzDatabaseName,
			string lpszIntxCtl,
			int maxToReturn
		)
		{
			CheckPtr(m_setup);

			var lpszCdTable = string.Format(
				"{0}{1}cd.tab",
				lspzFixedFilesDir,
				Path.DirectorySeparatorChar
			);
			var lpszAdCtl = string.Format(
				"{0}{1}{2}adctl.txt",
				lspzDynamicFilesDir,
				Path.DirectorySeparatorChar,
				lspzDatabaseName
			);
			var lpszGram = string.Format(
				"{0}{1}{2}gram.txt",
				lspzDynamicFilesDir,
				Path.DirectorySeparatorChar,
				lspzDatabaseName
			);
			var lpszDict = string.Format(
				"{0}{1}{2}lex.txt",
				lspzDynamicFilesDir,
				Path.DirectorySeparatorChar,
				lspzDatabaseName
			);

			m_ampleReset(m_setup);

			SetOptionsForTonePars(maxToReturn);

			// LOAD THE CONTROL FILES
			// ortho
			string sResult = m_ampleLoadControlFiles(
				m_setup,
				lpszAdCtl,
				lpszCdTable,
				null,
				lpszIntxCtl
			);
			// INTX
			ThrowIfError(sResult, lpszAdCtl, lpszCdTable);

			//LOAD ROOT DICTIONARIES
			sResult = m_ampleLoadDictionary(m_setup, lpszDict, "u");
			ThrowIfError(sResult, lpszDict);

			//LOAD GRAMMAR FILE
			sResult = m_ampleLoadGrammarFile(m_setup, lpszGram);
			ThrowIfError(sResult, lpszGram);
		}

		protected void SetOptionsForTonePars(int maxToReturn)
		{
			m_options.OutputStyle = "Ana";
			m_options.MaxAnalysesToReturn = maxToReturn;
			SetOptions();
		}
	}
}

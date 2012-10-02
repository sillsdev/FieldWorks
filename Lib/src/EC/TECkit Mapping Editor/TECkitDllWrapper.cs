using System;
using System.Collections.Generic;
using System.Text;
using ECInterfaces;
using SilEncConverters40;
using System.IO;
using System.Runtime.InteropServices;   // DLLImport

namespace TECkit_Mapping_Editor
{
	class TECkitDllWrapper
	{
		#region DLLImport Statements

		[DllImport("TECkit_Compiler_x86", SetLastError = true)]
		static extern unsafe int TECkit_Compile(
			byte* txt,
			UInt32 len,
			byte doCompression,
			Delegate errFunc,
			void* userData,
			Byte** outTable,
			UInt32* outLen);

		#endregion DLLImport Statements

		public unsafe static void CompileMap(string strFilename, ref string strCompiledFilename)
		{
			m_lstErrorMsgs.Clear(); // start with an empty list of errors so we can detect them.

			int status = 0;
			FileStream fileMap = new FileStream(strFilename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			byte[] pTxt = new byte[fileMap.Length];
			uint nMapSize = (uint)fileMap.Read(pTxt, 0, (int)fileMap.Length);
			byte* compiledTable = (byte*)0;
			UInt32 compiledSize = 0;

			errFunc dsplyErr = new errFunc(TECkitDllWrapper.DisplayCompilerError);
			byte[] baName = Encoding.ASCII.GetBytes(strFilename);

			fixed (byte* lpTxt = pTxt)
			fixed (byte* lpName = baName)
				status = TECkit_Compile(
					lpTxt,
					nMapSize,
					(byte)1,   // docompression
					dsplyErr,
					lpName,
					&compiledTable,
					&compiledSize);

			// any errors occur?
			if (m_lstErrorMsgs.Count > 0)
			{
				string strErrs = null;
				foreach( string strErr in m_lstErrorMsgs)
					strErrs += strErr + Environment.NewLine;

				ApplicationException e = new ApplicationException(strErrs);
				throw e;
			}
			else if (status == (int)ErrStatus.NoError)
			{
				// put the data from TEC into a managed byte array for the following Write
				byte[] baOut = new byte[compiledSize];
				ECNormalizeData.ByteStarToByteArr(compiledTable, (int)compiledSize, baOut);

				// save the compiled mapping (but if it fails because it's locked, then
				//  try to save it with a temporary name.
				FileStream fileTec = null;
				try
				{
					fileTec = File.OpenWrite(strCompiledFilename);
				}
				catch (System.IO.IOException)
				{
					// temporary filename for temporary CC tables (to check portions of the file at a time)
					strCompiledFilename = Path.GetTempFileName();
					strCompiledFilename = strCompiledFilename.Remove(strCompiledFilename.Length - 3) + "tec";
					fileTec = File.OpenWrite(strCompiledFilename);
				}

				fileTec.Write(baOut, 0, (int)compiledSize);
				fileTec.Close();
			}
		}

		protected static List<string> m_lstErrorMsgs = new List<string>();

		internal static string cstrLineNumClue = " at line ";

		unsafe delegate void errFunc(byte* pThis, byte* msg, byte* param, UInt32 line);
		static unsafe void DisplayCompilerError(byte* pszName, byte* msg, byte* param, UInt32 line)
		{
			byte[] baMsg = ECNormalizeData.ByteStarToByteArr(msg);
			Encoding enc = Encoding.ASCII;
			string str = new string(enc.GetChars(baMsg));

			if (param != (byte*)0)
			{
				baMsg = ECNormalizeData.ByteStarToByteArr(param);
				str += String.Format(": \"{0}\"", new string(enc.GetChars(baMsg)));
			}

			if (line != 0)
			{
				str += cstrLineNumClue;
				str += line.ToString();
			}

			m_lstErrorMsgs.Add(str);
		}
	}
}

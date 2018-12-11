// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

// This tool was created mainly for KenZ as he is the 'Import Wizard'!   :-)
// It uses the underlying classes in Sfm2Xml to give additional information about
// sfms used in a file:
// - it will give a byte count for all data in the file excluding the sfms
//   ex:
//		[0xE9] < > 140
//		[0xEA] < > 25
//
// - it will give a count for each sfm used in the file excluding inline sfms
//   ex:
//		s	= 8
//		v	= 48
//
// - it will give a list of each sfm and each sfm following it and a count for that pair of sfms
//   ex:
//		p - v	= 6
//		q - b	= 4
//		q - q	= 16
//

namespace SfmStats
{
	class Program
	{
		// static class member variables
		static string nl = System.Environment.NewLine;
		static string m_FileName;
		static string m_OutputFileName;
		//static bool m_createdTempFile = false;
		static byte[] m_NL;

		/// <summary>
		/// Put out a usage statement for the program.
		/// </summary>
		private static void Usage()
		{
			System.Diagnostics.Process proc = System.Diagnostics.Process.GetCurrentProcess();
			string name = proc.MainWindowTitle;

			Console.Write(name + nl + nl);
			Console.Write("Usage: " + name + " inputFileName [-o outputFileName]" + nl);
			Console.Write("-------------------------------------------" + nl);
			Console.Write("inputFileName      - name of the SFM file to gather statistics on." + nl);
			Console.Write("-o outputFileName  - optional name of output file name (will create a temp file by default)" + nl + nl);
			Console.Write("This tool will give information on the bytes used in the data portion of the input file," + nl);
			Console.Write("each sfm found and the count of occurances in the file and" + nl);
			Console.Write("it will list each sfm and unique sfm that follows it with an occurance count."+nl+nl);
			Console.Write("The byte count and display includes the actual byte so that the output file" + nl);
			Console.Write("can be viewed with any font to view what the glyph was for that byte / code-point." + nl);
			Console.Write("That byte count does not include the bytes used in the SFMs." + nl);
			Console.Write(nl + nl);
		}

		/// <summary>
		/// Put out a header into the output file giving the source of the data, exe and date.
		/// </summary>
		/// <returns></returns>
		static string OutputHeader()
		{
			System.Diagnostics.Process proc = System.Diagnostics.Process.GetCurrentProcess();
			string name = proc.MainWindowTitle;

			StringBuilder sb = new StringBuilder();
			sb.AppendFormat("{0}{1}", "------------------------------------------------------------------------------", nl);
			sb.AppendFormat("{0}{1}{2}", "- This output has been created by ", name, nl);
			sb.AppendFormat("{0}{1}{2}", "- From the input file: ", m_FileName, nl);
			sb.AppendFormat("{0}{1}{2}", "- On: ", DateTime.Now.ToString(), nl);
			sb.AppendFormat("{0}{1}", "------------------------------------------------------------------------------", nl);
			return sb.ToString();
		}

		static void LaunchViewer()
		{

		}

		/// <summary>
		/// Main entry point and command line processor.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		static int Main(string[] args)
		{
			if (args.Length == 0 || args.Length == 2 || args.Length > 3)
			{
				Usage();
				return -1;
			}
			m_FileName = args[0];
			if (args.Length == 3)
			{
				if (args[1].ToLowerInvariant() != "-o")
				{
					Usage();
					return -2;
				}
				else
					m_OutputFileName = args[2];
			}
			if (m_FileName == m_OutputFileName)
			{
				Usage();
				Console.WriteLine("*** ERROR: Can't specify the same file for both input and output."+nl+nl);
				return -3;
			}
			if (File.Exists(m_FileName) == false)
			{
				Usage();
				Console.WriteLine("*** ERROR: Can't open the specified input file: " + m_FileName + "." + nl + nl);
				return -4;
			}

			return DoTheWork();
		}

		/// <summary>
		/// Simple wraper over a converter in the Sfm2Xml namespace
		/// </summary>
		/// <param name="dataIn">string data in</param>
		/// <returns>byte array out</returns>
		static byte[] MakeBytes(string dataIn)
		{
			return Sfm2Xml.Converter.WideToMulti(dataIn, System.Text.Encoding.ASCII);
		}

		/// <summary>
		/// For each byte used in the input file, give a count of the number of times it is used.
		/// Also output the binary byte value so it can be viewed with a special font if needed
		/// to see what glyph it represented in the project font.
		/// </summary>
		/// <param name="byteCounts">count for each byte</param>
		/// <param name="w">writer to use to output the results</param>
		static void OutputByteCountInfo(int[] byteCounts, BinaryWriter w)
		{
			// now show the Byte Count statistics just collected on this file
			StringBuilder sb = new StringBuilder();
			sb.AppendFormat("{0}{1}{2}{3,6}{4,9}{5}{6}{7}", "ByteCount info (excluding SFMs):", nl, "index", "char", "count", nl, "--------------------", nl);
			w.Write(MakeBytes(sb.ToString()));

			byte[] a = new byte[] { (byte)'[', (byte)'0', (byte)'x' };
			byte[] b = new byte[] { (byte)']', (byte)' ', (byte)'<' };
			byte[] c = new byte[] { (byte)'>', (byte)' ', };
			for (int i = 0; i < 256; i++)	// process all bytes (0x00 - 0xff)
			{
				if (byteCounts[i] == 0)		// only output a count if it was used in the src file
					continue;

				byte value = (byte)i;
				if (i < 0x20 && char.GetUnicodeCategory(Convert.ToChar(i)) == System.Globalization.UnicodeCategory.Control)
					value = (byte)'*';		// use '*' for points less than 0x20 that are 'control' type points

				// now write out the formated line
				w.Write(a);
				w.Write(Sfm2Xml.Converter.WideToMulti(i.ToString("X2"), System.Text.Encoding.ASCII));
				w.Write(b);
				w.Write(value);
				w.Write(c);
				w.Write(Sfm2Xml.Converter.WideToMulti(byteCounts[i].ToString("N0"), System.Text.Encoding.ASCII));
				w.Write(m_NL);
			}
		}

		/// <summary>
		/// Output a count for each sfm in the input file, sorted by sfm.
		/// </summary>
		/// <param name="sfmSorted"></param>
		/// <param name="w"></param>
		/// <param name="reader"></param>
		static void OutputSfmInfo(System.Collections.ArrayList sfmSorted, BinaryWriter w, Sfm2Xml.SfmFileReaderEx reader)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(nl);
			sb.AppendFormat("{0}{1}{2,5}{3,9}{4}{5}{6}", "SfmCount info:", nl, "SFM", "count", nl, "--------------", nl);
			w.Write(MakeBytes(sb.ToString()));

			byte[] a = new byte[] { (byte)'\t', (byte)'=', (byte)' ' };
			foreach (string sfm in sfmSorted)
			{
				w.Write(MakeBytes(sfm));
				w.Write(a);
				w.Write(MakeBytes((reader.GetSFMCount(sfm)).ToString("N0")));
				w.Write(m_NL);
			}
		}

		/// <summary>
		/// Output each sfm and the sfm that followed it with the number of occurances in the input file.
		/// Each sfm should be listed, except for the case where the last sfm in the file is only used once.
		/// </summary>
		/// <param name="sfmSorted"></param>
		/// <param name="w"></param>
		/// <param name="reader"></param>
		static void OutputFollowedByInfo(System.Collections.ArrayList sfmSorted, BinaryWriter w, Sfm2Xml.SfmFileReaderEx reader)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendFormat("{0}{1}{2}{3}{4}", nl, "SFM Followed by Info:", nl, "--------------------------------------", nl);
			sb.AppendFormat("{0}({1}){2}", reader.FileName, reader.Count, nl);
			w.Write(MakeBytes(sb.ToString()));

			byte[] a = new byte[] { (byte)' ', (byte)'-', (byte)' ' };
			byte[] b = new byte[] { (byte)'\t', (byte)'=', (byte)' ' };

			Dictionary<string, Dictionary<string, int>> fbInfo = reader.GetFollowedByInfo();
			foreach (string sfm in sfmSorted)
			{
				if (fbInfo.ContainsKey(sfm) == false)	// case where sfm is only used once and is last marker
					continue;

				Dictionary<string, int> kvp = fbInfo[sfm];
				System.Collections.ArrayList sfm2 = new System.Collections.ArrayList(kvp.Keys);
				sfm2.Sort();

				foreach (string sfmNext in sfm2)
				{
					w.Write(MakeBytes(sfm));
					w.Write(a);
					w.Write(MakeBytes(sfmNext));
					w.Write(b);
					w.Write(MakeBytes((kvp[sfmNext]).ToString("N0")));
					w.Write(m_NL);
				}
			}
		}

		/// <summary>
		/// Remove the byte counts of the SFMs from the bytecount array.  That way the bytecount info
		/// will be from the data only.
		/// </summary>
		/// <param name="byteCounts">int counts for each byte index</param>
		/// <param name="sfms">list of sfms</param>
		/// <param name="reader">actual reader object</param>
		static void RemoveSFMsFromByteCount(ref int[] byteCounts, System.Collections.ArrayList sfms, Sfm2Xml.SfmFileReaderEx reader)
		{
			foreach (string sfm in sfms)
			{
				int count = reader.GetSFMCount(sfm);
				byteCounts[(byte)'\\'] -= count;
				byte[] bytes = MakeBytes(sfm);
				foreach (byte b in bytes)
				{
					byteCounts[b] -= count;
				}
			}
		}

		/// <summary>
		/// Main routine to call and run the process.
		/// </summary>
		/// <returns></returns>
		static int DoTheWork()
		{
			m_NL = Sfm2Xml.Converter.WideToMulti(System.Environment.NewLine, System.Text.Encoding.ASCII);

			if (m_OutputFileName == null)// || File.Exists
			{
				m_OutputFileName = Path.GetTempFileName();	// get a temporaryfile name and full path
				Console.WriteLine("Created temp file for output: " + m_OutputFileName);
//				m_createdTempFile = true;
			}

			FileStream outputFile = new FileStream(m_OutputFileName, FileMode.Create);
			BinaryWriter bWriter = new BinaryWriter(outputFile);

			try
			{
				// collect the statistics from the file
				Sfm2Xml.SfmFileReaderEx reader = new Sfm2Xml.SfmFileReaderEx(m_FileName);
				int[] byteCounts = reader.GetByteCounts;

				// get a sorted list of sfm values
				System.Collections.ArrayList sfmSorted = new System.Collections.ArrayList(reader.SfmInfo);
				sfmSorted.Sort();
				RemoveSFMsFromByteCount(ref byteCounts, sfmSorted, reader);
				bWriter.Write(MakeBytes(OutputHeader()));
				OutputByteCountInfo(byteCounts, bWriter);
				OutputSfmInfo(sfmSorted, bWriter, reader);
				OutputFollowedByInfo(sfmSorted, bWriter, reader);
			}
			finally
			{
				if (bWriter != null)
					bWriter.Close();
				if (outputFile != null)
					outputFile.Close();
			}
			return 0;
		}
	}
}

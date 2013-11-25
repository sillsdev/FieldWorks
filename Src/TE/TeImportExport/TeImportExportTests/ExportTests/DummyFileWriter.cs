// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DummyFileWriter.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System.IO;
using System.Text;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using SIL.Utils;

namespace SIL.FieldWorks.TE.ExportTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class mimics the FileWriter, but just keeps the "written" text data in memory.
	/// Allows for quick verification of export output in tests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyFileWriter : FileWriter
	{
		private List<string> m_list = new List<string>();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open the dummy file - do nothing
		/// </summary>
		/// <param name="fileName"></param>
		/// ------------------------------------------------------------------------------------
		public override void Open(string fileName)
		{
			CheckDisposed();

			m_list = new List<string>();
			if (FileUtils.FileExists(fileName))
			{
				using (TextReader tr = FileUtils.OpenFileForRead(fileName, Encoding.ASCII))
				{
					string line;
					while ((line = tr.ReadLine()) != null)
						m_list.Add(line);
					tr.Close();
				}
			}

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Write a string to the last line of the file
		/// </summary>
		/// <param name="outString"></param>
		/// ------------------------------------------------------------------------------------
		public override void Write(string outString)
		{
			CheckDisposed();

			int lineCount = m_list.Count;
			if (lineCount == 0)
				m_list.Add(outString);
			else
			{
				// Append the new string to the end of the last string
				string line = m_list[lineCount - 1];
				m_list[lineCount - 1] = line + outString;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Write a line of text to the dummy file
		/// </summary>
		/// <param name="outString">The out string.</param>
		/// <param name="outputBlankLine">if set to <c>true</c> [output blank line].</param>
		/// ------------------------------------------------------------------------------------
		public override void WriteLine(string outString, bool outputBlankLine)
		{
			CheckDisposed();

			Write(outString);
			WriteLine();
			if (outputBlankLine)
				m_list.Add(string.Empty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Write a line to the dummy file
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void WriteLine()
		{
			CheckDisposed();

			int count = m_list.Count;
			if (count > 0)
			{
				// trim spaces from the last string before starting a new one
				m_list[count - 1] = m_list[count - 1].TrimEnd(' ');

				// Start a new line on the list if the last line is not empty.
				if (m_list[count - 1].Length != 0)
					m_list.Add(string.Empty);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Close the dummy file
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void Close()
		{
			CheckDisposed();

			m_list = null;
		}

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the count of lines in the dummy file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int LineCount
		{
			get
			{
				CheckDisposed();

				int count = m_list.Count;
				// If the last line is empty, then ignore it.
				if (count > 0 && m_list[count - 1] == string.Empty)
					return count - 1;

				return count;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value reflecting if the next output would go at the start of a new line.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool AtStartOfLine
		{
			get
			{
				CheckDisposed();
				return m_list[m_list.Count - 1].Length == 0;
			}
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve a line from the dummy file
		/// </summary>
		/// <param name="lineNumber"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public string Line(int lineNumber)
		{
			CheckDisposed();

			if (lineNumber < LineCount)
				return m_list[lineNumber];
			throw new ArgumentOutOfRangeException("lineNumber", lineNumber,
				"Line does not exist in the dummy file");
		}

		#region VerifyOutput
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the line.
		/// </summary>
		/// <param name="lineContents">The line contents.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int FindLine(string lineContents)
		{
			CheckDisposed();

			int i = m_list.IndexOf(lineContents);
			Assert.IsTrue(i >= 0, "Could not find " + lineContents + " in output.");
			return i;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify part of the output from an export operation by comparing it against a list
		/// of expected strings.
		/// </summary>
		/// <param name="expectedStrings">The expected strings.</param>
		/// <param name="startAt">The line to start at.</param>
		/// ------------------------------------------------------------------------------------
		public void VerifyNLinesOfOutput(string[] expectedStrings, int startAt)
		{
			CheckDisposed();

			VerifyNLinesOfOutput(expectedStrings, startAt, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify the output from an export operation by comparing it against a list
		/// of expected strings.
		/// </summary>
		/// <param name="expectedStrings">The expected strings.</param>
		/// <param name="startAt">The line to start at.</param>
		/// <param name="fPartial">If set to <c>true</c> then only compare as many lines as are
		/// in the list of expected strings.</param>
		/// ------------------------------------------------------------------------------------
		private void VerifyNLinesOfOutput(string[] expectedStrings, int startAt, bool fPartial)
		{
			int countMax = (fPartial) ? expectedStrings.Length : Math.Max(expectedStrings.Length, LineCount);

			for (int iExpected = 0; iExpected < countMax; iExpected++)
			{
				int iLines = startAt + iExpected;
				// Deal with cases where one or both of the lines is missing
				if (iExpected >= expectedStrings.Length)
				{
					DumpStringContext(expectedStrings, iExpected);
					Assert.Fail("Line # {0}.{2}\tExpected <EOF>, but was <{1}>", iLines, Line(iLines), Environment.NewLine);
				}
				if (iLines >= LineCount)
				{
					DumpStringContext(expectedStrings, iExpected);
					Assert.Fail("Line # {0}.{2}\tExpected <{1}>, but was <EOF>", iLines, expectedStrings[iExpected], Environment.NewLine);
				}

				// check to see if the lines are different
				if (expectedStrings[iExpected] != Line(iLines))
				{
					DumpStringContext(expectedStrings, iExpected);
					Assert.Fail("Line # {0}{3}\tExpected: <{1}>{3}\tbut was:  <{2}>",
						iLines, expectedStrings[iExpected], Line(iLines), Environment.NewLine);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify the output from an export operation by comparing it against a list
		/// of expected strings.
		/// </summary>
		/// <param name="expectedStrings"></param>
		/// ------------------------------------------------------------------------------------
		internal void VerifyOutput(string[] expectedStrings)
		{
			CheckDisposed();
			VerifyNLinesOfOutput(expectedStrings, 0, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Dumps the string context.
		/// </summary>
		/// <param name="expectedStrings">The expected strings.</param>
		/// <param name="index">The index.</param>
		/// ------------------------------------------------------------------------------------
		private void DumpStringContext(string[] expectedStrings, int index)
		{
			// dump from
			int start = (index - 5) < 0 ? 0 : index - 5;
			int end = index + 5;

			// show the expected strings
			Debug.WriteLine("Expected Strings:");
			for (int i = start; i <= end; i++)
			{
				if (i < expectedStrings.Length)
				{
					string currentIndicator = (i == index) ? "*" : " ";
					Debug.WriteLine(currentIndicator + i.ToString() + " " + expectedStrings[i]);
				}
			}

			// show the file strings
			Debug.WriteLine("File Strings:");
			for (int i = start; i <= end; i++)
			{
				if (i < LineCount)
				{
					string currentIndicator = (i == index) ? "*" : " ";
					Debug.WriteLine(currentIndicator + i.ToString() + " " + Line(i));
				}
			}
		}
		#endregion
	}
}

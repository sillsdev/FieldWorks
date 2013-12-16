// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ExportUSFMTests.cs
// Responsibility: Edge
//
// <remarks>
// </remarks>

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.TE;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.FDOTests;
//using SIL.FieldWorks.Common.Utils;
//using SIL.FieldWorks.FDO.Scripture;

namespace SIL.FieldWorks.AcceptanceTests.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for ExportUSFMTests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ExportUSFMTests : ScrInMemoryFdoTestBase
	{

		#region Setup and Teardown
		#endregion

		#region Tests (duh)
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Export USFM option
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportUSFM_ParatextVernacular()
		{
			string fileName = Path.Combine(Path.GetTempPath(), "~usfmfile~.txt");
			FilteredScrBooks filter = FilteredScrBooks.GetFilterInstance(Cache, 1);
			while (filter.BookCount > 0)
				filter.Remove(0);
			// add the book of James to the book filter
			filter.Add(Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[1]);

			// perform Paratext vernacular export
			try
			{
				ExportUsfm export = new ExportUsfm(Cache, filter, fileName);
				export.MarkupSystem = MarkupType.Paratext;
				export.Run();

				VerifyFile("SIL.FieldWorks.AcceptanceTests.TE.ExportJasParatext.ptx",
					fileName);
			}
			finally
			{
				try
				{
					File.Delete(fileName);
				}
				catch {}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Export Toolbox option
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportUSFM_ToolboxVernacular()
		{
			string fileName = Path.Combine(Path.GetTempPath(), "~usfmfile~.txt");
			FilteredScrBooks filter = FilteredScrBooks.GetFilterInstance(Cache, 1);
			while (filter.BookCount > 0)
				filter.Remove(0);
			// add the book of James to the book filter
			filter.Add(Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[1]);

			// perform Toolbox vernacular export
			try
			{
				ExportUsfm export = new ExportUsfm(Cache, filter, fileName);
				export.MarkupSystem = MarkupType.Toolbox;
				export.Run();
				VerifyFile("SIL.FieldWorks.AcceptanceTests.TE.ExportJasToolbox.sf",
					fileName);
			}
			finally
			{
				try
				{
					File.Delete(fileName);
				}
				catch {}
			}
		}
		#endregion

		#region helper functions
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify that the file matches the given expected resource stream.
		/// </summary>
		/// <param name="expectedResourceStream">file resource containing our expected data\
		/// </param>
		/// <param name="filePath">file to verify</param>
		/// ------------------------------------------------------------------------------------
		private void VerifyFile(string expectedResourceStream, string filePath)
		{
			// verify the contents of the file.
			Stream stream =	this.GetType().Assembly.GetManifestResourceStream(
				expectedResourceStream);
			StreamReader expectedStream = new StreamReader(stream);
			StreamReader exportedStream = new StreamReader(filePath);
			string expectedString;
			string fileString;
			int lineNumber = 1;

			try
			{
				while ((expectedString = expectedStream.ReadLine()) != null)
				{
					fileString = exportedStream.ReadLine();
					// REVIEW: There may be hard line break characters in the test data which
					// do not exist in the expected data.  For now, remove them before comparing
					// the strings.
					fileString = fileString.Replace("\u2028", "");
					Assert.IsNotNull(fileString, "Unexpected EOF in exported file at line " + lineNumber);
					Assert.AreEqual(expectedString, fileString, "File line " + lineNumber + " differs:" +
						 "\n\texpected: <" + expectedString +
						">\n\t but was: <" + fileString + ">");

					lineNumber++;
				}
				Assert.IsNull(fileString = exportedStream.ReadLine(),
					"Extra lines in file beyond expected");
			}
			finally
			{
				expectedStream.Close();
				exportedStream.Close();
			}

		}
		#endregion
	}
}

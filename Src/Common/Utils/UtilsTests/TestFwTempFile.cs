// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TestFwTempFile.cs
// Responsibility: John Hatton
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.IO;

using NUnit.Framework;

namespace SIL.FieldWorks.Common.Utils
{
	/// <summary></summary>
	[TestFixture]
	public class FwTempFile_Test
	{
		const string kcontent = "This is a test.";

		/// <summary>
		/// Constructor.
		/// </summary>
		public FwTempFile_Test()
		{
		}

		/// <summary></summary>
		protected void CheckAndDispose(FwTempFile orange)
		{
			string tempPath = orange.CloseAndGetPath();
			StreamReader reader = null;
			try
			{
				reader = File.OpenText(tempPath);
				string s = reader.ReadToEnd();
				Assert.AreEqual(kcontent, s, "Contents of temp file did not match what I wrote to it.");
			}
			finally
			{
				if (reader != null)
					reader.Close();
				orange.Dispose();
			}
			Assert.IsFalse(File.Exists(tempPath), "Temp file was not deleted.");
		}


		/// <summary>
		/// </summary>
		[Test]
		public void Basic()
		{
			FwTempFile orange = new FwTempFile();
			orange.Writer.Write(kcontent);
			CheckAndDispose(orange);
		}

		/// <summary>
		/// </summary>
		[Test]
		public void Detach()
		{
			FwTempFile orange = new FwTempFile();
			orange.Writer.Write(kcontent);
			string path = orange.CloseAndGetPath();
			StreamReader reader = File.OpenText(path);

			orange.Detach();
			// This will close the writer.
			orange.Dispose();

			reader.Close();
			File.Delete(path);
		}

		/// <summary>
		/// </summary>
		[Test]
		public void CantDelete()
		{
			FwTempFile orange = new FwTempFile();
			orange.Writer.Write(kcontent);
			string path = orange.CloseAndGetPath();
			StreamReader reader = File.OpenText(path);
			orange.Dispose();
		}

		/// <summary>
		/// </summary>
		[Test]
		public void CustomExtension()
		{
			FwTempFile orange = new FwTempFile(".htm");
			orange.Writer.Write(kcontent);
			CheckAndDispose(orange);
		}

		/// <summary></summary>
		[Test]
		public void HTMLVersion()
		{
			FwTempFile orange = new FwTempHtmlFile();
			orange.Writer.Write(kcontent);
			CheckAndDispose(orange);
		}
		/// <summary>
		///
		/// </summary>
		[Test]
		public void CreateTempFileAndGetPathTest()
		{
			string sFile = FwTempFile.CreateTempFileAndGetPath("tmp");
			Assert.IsNotNull(sFile);
			Assert.IsTrue(sFile.Length > 0, "Expected temp file name to be non-empty");
			if (File.Exists(sFile))
				File.Delete(sFile);
		}
	}
}

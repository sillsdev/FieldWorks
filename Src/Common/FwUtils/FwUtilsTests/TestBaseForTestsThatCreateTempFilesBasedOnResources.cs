// Copyright (c) 2009-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using SIL.CoreImpl;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// This test base is used with tests that require text files that are stored in a resource. It saves the text files to the temporary
	/// directory and then cleans them up when the text fixture is finished.
	/// </summary>
	[TestFixture]
	public class TestBaseForTestsThatCreateTempFilesBasedOnResources
	{
		private static List<string> s_foldersToDelete = new List<string>();

		/// <summary></summary>
		[TestFixtureTearDown]
		public void FixtureTearDown()
		{
			CleanUpTempFolders();
		}

		/// <summary></summary>
		protected string CreateTempTestFiles(Type resourcesType, string folderName)
		{
			string folderPath = StringTableTests.CreateTestResourceFiles(resourcesType, folderName);
			s_foldersToDelete.Add(folderPath);
			return folderPath;
		}

		/// <summary></summary>
		internal static void CleanUpTempFolders()
		{
			foreach (string folder in s_foldersToDelete)
			{
				try
				{
					Directory.Delete(folder, true);
				}
				catch (IOException) { }
			}

			s_foldersToDelete.Clear();
		}
	}
}

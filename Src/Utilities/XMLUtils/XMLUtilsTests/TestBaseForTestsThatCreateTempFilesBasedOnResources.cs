// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TestBaseForTestsThatCreateTempFilesBasedOnResources.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using System.IO;
using System.Reflection;
using System.Resources;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class TestBaseForTestsThatCreateTempFilesBasedOnResources : BaseTest
	{
		private static List<string> s_foldersToDelete = new List<string>();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureTearDown]
		public void FixtureTearDown()
		{
			CleanUpTempFolders();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected string CreateTempTestFiles(Type resourcesType, string folderName)
		{
			string folderPath = Path.Combine(Path.GetTempPath(), folderName);
			s_foldersToDelete.Add(folderPath);
			PropertyInfo[] props = resourcesType.GetProperties(BindingFlags.Static | BindingFlags.NonPublic);

			foreach (PropertyInfo pi in props)
			{
				if (pi.PropertyType == typeof(string) && pi.Name.StartsWith(folderName + "__"))
					CreateSingleTempTestFile(pi.Name);
			}
			return folderPath;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual ResourceManager ResourceMgr
		{
			get { return Properties.Resources.ResourceManager; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CreateSingleTempTestFile(string resName)
		{
			string path = resName.Replace("__", Path.DirectorySeparatorChar.ToString());
			path = path.Replace("_DASH_", "-");
			path = path.Replace("_", ".");
			path = Path.Combine(Path.GetTempPath(), path);
			string folder = Path.GetDirectoryName(path);
			if (!Directory.Exists(folder))
				Directory.CreateDirectory(folder);
			File.WriteAllText(path, ResourceMgr.GetString(resName), Encoding.UTF8);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal static void CleanUpTempFolders()
		{
			foreach (string folder in s_foldersToDelete)
			{
				try
				{
					Directory.Delete(folder, true);
				}
				catch { }
			}

			s_foldersToDelete.Clear();
		}
	}
}

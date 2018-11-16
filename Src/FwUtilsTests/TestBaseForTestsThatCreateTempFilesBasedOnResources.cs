// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary />
	[TestFixture]
	public class TestBaseForTestsThatCreateTempFilesBasedOnResources
	{
		private static List<string> s_foldersToDelete = new List<string>();

		/// <summary />
		[TestFixtureSetUp]
		public virtual void FixtureSetup()
		{ /* Do nothing here. */ }

		/// <summary />
		[TestFixtureTearDown]
		public virtual void FixtureTearDown()
		{
			CleanUpTempFolders();
		}

		/// <summary />
		[SetUp]
		public virtual void TestSetup()
		{ /* Do nothing here. */ }

		/// <summary />
		[TearDown]
		public virtual void TestTeardown()
		{ /* Do nothing here. */ }

		/// <summary />
		protected string CreateTempTestFiles(Type resourcesType, string folderName)
		{
			var folderPath = Path.Combine(Path.GetTempPath(), folderName);
			s_foldersToDelete.Add(folderPath);
			var props = resourcesType.GetProperties(BindingFlags.Static | BindingFlags.NonPublic);
			// TODO-Linux: System.Boolean System.Type::op_Equality(System.Type,System.Type)
			// is marked with [MonoTODO] and might not work as expected in 4.0.
			foreach (var pi in props.Where(pi => pi.PropertyType == typeof(string) && pi.Name.StartsWith(folderName + "__")))
			{
				CreateSingleTempTestFile(pi.Name);
			}
			return folderPath;
		}

		/// <summary />
		protected virtual ResourceManager ResourceMgr => Properties.Resources.ResourceManager;

		/// <summary />
		private void CreateSingleTempTestFile(string resName)
		{
			var path = resName.Replace("__", Path.DirectorySeparatorChar.ToString());
			path = path.Replace("_DASH_", "-");
			path = path.Replace("_", ".");
			path = Path.Combine(Path.GetTempPath(), path);
			var folder = Path.GetDirectoryName(path);
			if (!Directory.Exists(folder))
			{
				Directory.CreateDirectory(folder);
			}
			File.WriteAllText(path, ResourceMgr.GetString(resName), Encoding.UTF8);
		}

		/// <summary />
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
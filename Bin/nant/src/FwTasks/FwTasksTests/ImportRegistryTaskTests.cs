// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ImportRegistryTaskTests.cs
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
using Microsoft.Win32;
using NAnt.Core;

namespace SIL.FieldWorks.Build.Tasks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DummyImportRegistryTask : ImportRegistryTask
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Does it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void DoIt()
		{
			ExecuteTask();
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ImportRegistryTaskTests
	{
		private string m_RegFileName;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Setup for the test fixture
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			m_RegFileName = Path.GetTempFileName();
			File.WriteAllText(m_RegFileName, @"REGEDIT4
[HKEY_CLASSES_ROOT\FwTasksTests]
@=""ImportRegistry""
[HKEY_CLASSES_ROOT\FwTasksTests\Subkey\Test]
""bla""=""bla""");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Teardown for the test fixture
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureTearDown]
		public void FixtureTearDown()
		{
			File.Delete(m_RegFileName);

			try
			{
				Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\FwTasksTests");
			}
			catch
			{
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests import into the registry.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Import()
		{
			DummyImportRegistryTask task = new DummyImportRegistryTask();
			task.RegistryFile = m_RegFileName;
			task.Unregister = false;
			task.PerUser = true;
			task.DoIt();

			RegistryKey testKey = Registry.CurrentUser.OpenSubKey(@"Software\Classes\FwTasksTests");
			Assert.AreEqual("ImportRegistry", testKey.GetValue(null));
			RegistryKey subKey = testKey.OpenSubKey("Subkey\\Test");
			Assert.AreEqual("bla", subKey.GetValue("bla"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests unregistering based on registry file - all keys and values from the file
		/// should be deleted.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Unregister()
		{
			DummyImportRegistryTask task = new DummyImportRegistryTask();
			task.RegistryFile = m_RegFileName;
			task.Unregister = true;
			task.PerUser = true;
			task.DoIt();

			Assert.IsNull(Registry.CurrentUser.OpenSubKey(@"Software\Classes\FwTasksTests"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests unsupported binary value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(BuildException))]
		public void Unsupported_Binary()
		{
			string regFilename = Path.GetTempFileName();
			File.WriteAllText(regFilename, @"REGEDIT4
[HKEY_CLASSES_ROOT\FwTasksTests\Unsupported]
""bla""=hex:00,01,02,03,04,05,06,07");
			try
			{
				DummyImportRegistryTask task = new DummyImportRegistryTask();
				task.RegistryFile = regFilename;
				task.Unregister = false;
				task.PerUser = true;
				task.DoIt();
			}
			finally
			{
				File.Delete(regFilename);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests unsupported DWORD value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(BuildException))]
		public void Unsupported_DWORD()
		{
			string regFilename = Path.GetTempFileName();
			File.WriteAllText(regFilename, @"REGEDIT4
[HKEY_CLASSES_ROOT\FwTasksTests\Unsupported]
""dword32""=dword:00000032");
			try
			{
				DummyImportRegistryTask task = new DummyImportRegistryTask();
				task.RegistryFile = regFilename;
				task.Unregister = false;
				task.PerUser = true;
				task.DoIt();
			}
			finally
			{
				File.Delete(regFilename);
			}
		}
	}
}

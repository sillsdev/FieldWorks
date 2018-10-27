// Copyright (c) 2010-201 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks
{
	/// <summary />
	[TestFixture]
	public class FieldWorksTests
	{
		#region GetProjectMatchStatus tests

		/// <summary>
		/// Tests the GetProjectMatchStatus method on FieldWorks with a matching project
		/// </summary>
		[Test]
		public void GetProjectMatchStatus_Match()
		{
			ReflectionHelper.SetProperty(typeof(FieldWorks), "InSingleProcessMode", false);
			ReflectionHelper.SetProperty(typeof(FieldWorks), "WaitingForUserOrOtherFw", false);
			ReflectionHelper.SetProperty(typeof(FieldWorks), "Project", new ProjectId(BackendProviderType.kXML, "monkey"));

			Assert.AreEqual(ProjectMatch.ItsMyProject, ReflectionHelper.GetResult(typeof(FieldWorks), "GetProjectMatchStatus", new ProjectId(BackendProviderType.kXML, "monkey")));
		}

		/// <summary>
		/// Tests the GetProjectMatchStatus method on FieldWorks with a different project
		/// </summary>
		[Test]
		public void GetProjectMatchStatus_NotMatch()
		{
			ReflectionHelper.SetProperty(typeof(FieldWorks), "InSingleProcessMode", false);
			ReflectionHelper.SetProperty(typeof(FieldWorks), "WaitingForUserOrOtherFw", false);
			ReflectionHelper.SetProperty(typeof(FieldWorks), "Project", new ProjectId(BackendProviderType.kXML, "primate"));

			Assert.AreEqual(ProjectMatch.ItsNotMyProject, ReflectionHelper.GetResult(typeof(FieldWorks), "GetProjectMatchStatus", new ProjectId(BackendProviderType.kXML, "monkey")));
		}

		/// <summary>
		/// Tests the GetProjectMatchStatus method on FieldWorks when the project has yet to
		/// be determined
		/// </summary>
		[Test]
		public void GetProjectMatchStatus_DontKnow()
		{
			ReflectionHelper.SetProperty(typeof(FieldWorks), "InSingleProcessMode", false);
			ReflectionHelper.SetProperty(typeof(FieldWorks), "WaitingForUserOrOtherFw", false);
			ReflectionHelper.SetProperty(typeof(FieldWorks), "Project", null);

			Assert.AreEqual(ProjectMatch.DontKnowYet, ReflectionHelper.GetResult(typeof(FieldWorks), "GetProjectMatchStatus", new ProjectId(BackendProviderType.kXML, "monkey")));
		}

		/// <summary>
		/// Tests the GetProjectMatchStatus method on FieldWorks when waiting on another
		/// FieldWorks process
		/// </summary>
		[Test]
		public void GetProjectMatchStatus_WaitingForFw()
		{
			ReflectionHelper.SetProperty(typeof(FieldWorks), "InSingleProcessMode", false);
			ReflectionHelper.SetProperty(typeof(FieldWorks), "WaitingForUserOrOtherFw", true);
			ReflectionHelper.SetProperty(typeof(FieldWorks), "Project", new ProjectId(BackendProviderType.kXML, "monkey"));

			Assert.AreEqual(ProjectMatch.WaitingForUserOrOtherFw, ReflectionHelper.GetResult(typeof(FieldWorks), "GetProjectMatchStatus", new ProjectId(BackendProviderType.kXML, "monkey")));
		}

		/// <summary>
		/// Tests the GetProjectMatchStatus method on FieldWorks when in "single process mode"
		/// </summary>
		[Test]
		public void GetProjectMatchStatus_SingleProcessMode()
		{
			ReflectionHelper.SetProperty(typeof(FieldWorks), "InSingleProcessMode", true);
			ReflectionHelper.SetProperty(typeof(FieldWorks), "WaitingForUserOrOtherFw", true);
			ReflectionHelper.SetProperty(typeof(FieldWorks), "Project", new ProjectId(BackendProviderType.kXML, "monkey"));

			Assert.AreEqual(ProjectMatch.SingleProcessMode, ReflectionHelper.GetResult(typeof(FieldWorks), "GetProjectMatchStatus", new ProjectId(BackendProviderType.kXML, "monkey")));
		}

		#endregion

		/// <summary />
		[Test]
		public void EnsureValidLinkedFilesFolderCore_IfUsingDefaultDir_CreatesDirIfNotExist()
		{
			EnsureValidLinkedFilesFolderCore_TestHelper(defaultFolder =>
			{
				var configuredFolder = defaultFolder;
				Assert.That(FileUtils.DirectoryExists(configuredFolder), Is.False, "Unit test not testing what it's supposed to");
				FieldWorks.EnsureValidLinkedFilesFolderCore(configuredFolder, defaultFolder);
				Assert.That(FileUtils.DirectoryExists(configuredFolder), Is.True, "Should have created directory");
			});
		}

		/// <summary />
		[Test]
		public void EnsureValidLinkedFilesFolderCore_IfUsingDefaultDirAndItExists_DoesntCrashOrAnything()
		{
			EnsureValidLinkedFilesFolderCore_TestHelper(defaultFolder =>
			{
				// Make default linked files directory already exist
				FileUtils.EnsureDirectoryExists(defaultFolder);
				// Not crash or anything
				Assert.DoesNotThrow(() => FieldWorks.EnsureValidLinkedFilesFolderCore(defaultFolder, defaultFolder));
			});
		}

		/// <summary />
		[Test]
		public void EnsureValidLinkedFilesFolderCore_NonDefaultLocation_NotCreateNonExistentDir()
		{
			EnsureValidLinkedFilesFolderCore_TestHelper(defaultFolder =>
			{
				var configuredFolder = FileUtils.ChangePathToPlatform("/nondefaultAndNonexistentPath");

				Assert.That(defaultFolder, Is.Not.EqualTo(configuredFolder), "Unit test not set up right");
				Assert.That(FileUtils.DirectoryExists(configuredFolder), Is.False, "Unit test not testing what it's supposed to");

				FieldWorks.EnsureValidLinkedFilesFolderCore(configuredFolder, defaultFolder);
				Assert.That(FileUtils.DirectoryExists(configuredFolder), Is.False, "Shouldn't have just made the nondefault directory");
			});
		}

		/// <summary />
		[Test]
		public void EnsureValidLinkedFilesFolderCore_NonDefaultLocationAndExists_DoesntCrashOrAnything()
		{
			EnsureValidLinkedFilesFolderCore_TestHelper(defaultFolder =>
			{
				var configuredFolder = FileUtils.ChangePathToPlatform("/nondefaultPath");

				// Make linked files directory already exist
				FileUtils.EnsureDirectoryExists(configuredFolder);

				Assert.That(defaultFolder, Is.Not.EqualTo(configuredFolder), "Unit test not set up right");
				Assert.That(FileUtils.DirectoryExists(configuredFolder), Is.True, "Unit test not testing what it's supposed to");

				// Not crash or anything
				FieldWorks.EnsureValidLinkedFilesFolderCore(configuredFolder, defaultFolder);
			});
		}

		/// <summary>
		/// Unit test helper to set up environment in which to test EnsureValidLinkedFilesFolderCore.
		/// testToExecute takes (string defaultFolder, LcmCache cache).
		/// </summary>
		public void EnsureValidLinkedFilesFolderCore_TestHelper(Action<string> testToExecute)
		{
			var defaultFolder = FileUtils.ChangePathToPlatform("/ProjectDir/LinkedFiles");

			var fileOs = new MockFileOS();
			try
			{
				FileUtils.Manager.SetFileAdapter(fileOs);

				testToExecute(defaultFolder);
			}
			finally
			{
				FileUtils.Manager.Reset();
			}
		}
	}
}
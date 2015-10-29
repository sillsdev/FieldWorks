// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices.BackupRestore;

namespace SIL.FieldWorks.FDO.FDOTests.BackupRestore
{
	/// <summary>
	/// Testing class for LinkedFiles tests that need to subclass a method that pops up a dialog.
	/// </summary>
	public class ProjectRestoreTestService : ProjectRestoreService
	{
		/// <summary>
		/// Provide a version of the basic ProjectRestoreService that overrides some UI
		/// we don't want to show up in tests (a dialog).
		/// </summary>
		/// <param name="settings"></param>
		public ProjectRestoreTestService(RestoreProjectSettings settings)
			: base(settings, new DummyFdoUI(), FwDirectoryFinder.ConverterConsoleExe, FwDirectoryFinder.DbExe)
		{
			PutFilesInProject = false;
			SimulateOKResult = true;
		}

		/// <summary>
		/// Set to 'true' to simulate user clicking radio button to put LinkedFiles in the default location.
		/// Leave 'false' to simulate user clicking radio button to use non-standard location.
		/// </summary>
		public bool PutFilesInProject { get; set; }

		/// <summary>
		/// Set to false to simulate user clicking Cancel, otherwise simulates DialogResult.OK.
		/// </summary>
		public bool SimulateOKResult { get; set; }

		/// <summary>
		/// Test override to provide input in place of dialog.
		/// </summary>
		protected override bool CanRestoreLinkedFilesToProjectsFolder()
		{
			return SimulateOKResult && PutFilesInProject;
		}

		/// <summary>
		/// Test override to throw exception instead of showing a dialog.
		/// </summary>
		/// <param name="linkedFilesPathPersisted"></param>
		/// <param name="filesContainedInLinkdFilesFolder"></param>
		protected override void CouldNotRestoreLinkedFilesToOriginalLocation(string linkedFilesPathPersisted,
			Dictionary<string, DateTime> filesContainedInLinkdFilesFolder)
		{
			throw new ApplicationException(string.Format(
				"Reached CouldNotRestoreLinkedFilesToOriginalLocation method: linkedFilesPathPersisted = {0}",
				linkedFilesPathPersisted));
		}
	}
}

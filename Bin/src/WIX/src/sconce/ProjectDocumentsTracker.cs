//--------------------------------------------------------------------------------------------------
// <copyright file="ProjectDocumentsTracker.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
// Wrapper class around the Visual Studio environment's IVsTrackProjectDocuments2 implementation.
// </summary>
//--------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Runtime.InteropServices;
	using Microsoft.VisualStudio.Shell.Interop;

	/// <summary>
	/// Provides useful wrapper methods around the Visual Studio environment's IVsTrackProjectDocuments2 implementation.
	/// </summary>
	public class ProjectDocumentsTracker
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(ProjectDocumentsTracker);

		private Project project;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="ProjectDocumentsTracker"/> class.
		/// </summary>
		/// <param name="project">The project whose documents we want to track.</param>
		public ProjectDocumentsTracker(Project project)
		{
			Tracer.VerifyNonNullArgument(project, "project");
			this.project = project;
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets an <see cref="IVsSolution"/> interface pointer.
		/// </summary>
		private IVsSolution Solution
		{
			get
			{
				return (IVsSolution)this.project.ServiceProvider.GetServiceOrThrow(typeof(SVsSolution), typeof(IVsSolution), classType, "Solution");
			}
		}

		/// <summary>
		/// Gets the wrapped <see cref="IVsRunningDocumentTable"/>.
		/// </summary>
		private IVsTrackProjectDocuments2 Tracker
		{
			get
			{
				return (IVsTrackProjectDocuments2)this.project.ServiceProvider.GetServiceOrThrow(typeof(SVsTrackProjectDocuments), typeof(IVsTrackProjectDocuments2), classType, "Tracker");
			}
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Queries the environment to see whether the directories in the project can be renamed.
		/// </summary>
		/// <param name="oldPaths">The paths to the old directories.</param>
		/// <param name="newPaths">The paths to the new directories.</param>
		/// <returns>true if the directories can be renamed; otherwise, false.</returns>
		public bool CanRenameDirectories(string[] oldPaths, string[] newPaths)
		{
			Tracer.VerifyNonNullArgument(oldPaths, "oldPaths");
			Tracer.VerifyNonNullArgument(newPaths, "newPaths");

			if (oldPaths.Length != newPaths.Length)
			{
				throw new ArgumentException("The old and new path arrays must match in size.", "oldPaths");
			}

			int length = oldPaths.Length;

			// If the arrays are zero-length, then there's nothing to rename.
			if (length == 0)
			{
				return true;
			}

			// Fill in the flags array (since there are no flags yet, we can leave the array at its default value).
			VSQUERYRENAMEDIRECTORYFLAGS[] flags = new VSQUERYRENAMEDIRECTORYFLAGS[length];
			VSQUERYRENAMEDIRECTORYRESULTS[] summaryResult = new VSQUERYRENAMEDIRECTORYRESULTS[length];
			VSQUERYRENAMEDIRECTORYRESULTS[] results = new VSQUERYRENAMEDIRECTORYRESULTS[length];
			int hr = this.Tracker.OnQueryRenameDirectories(this.project, length, oldPaths, newPaths, flags, summaryResult, results);
			NativeMethods.ThrowOnFailure(hr);

			return (summaryResult[0] == VSQUERYRENAMEDIRECTORYRESULTS.VSQUERYRENAMEDIRECTORYRESULTS_RenameOK);
		}

		/// <summary>
		/// Queries the environment to see whether the directory in the project can be renamed.
		/// </summary>
		/// <param name="oldPath">The path to the old directory.</param>
		/// <param name="newPath">The path to the new directory.</param>
		/// <returns>true if the directory can be renamed; otherwise, false.</returns>
		public bool CanRenameDirectory(string oldPath, string newPath)
		{
			return this.CanRenameDirectories(new string[] { oldPath }, new string[] { newPath });
		}

		/// <summary>
		/// Queries the environment to see whether the file in the project can be renamed.
		/// </summary>
		/// <param name="oldPath">The path to the old file name.</param>
		/// <param name="newPath">The path to the new file name.</param>
		/// <returns>true if the file can be renamed; otherwise, false.</returns>
		public bool CanRenameFile(string oldPath, string newPath)
		{
			int fRenameCanContinue;
			int hr = this.Tracker.OnQueryRenameFile(this.project, oldPath, newPath, VSRENAMEFILEFLAGS.VSRENAMEFILEFLAGS_NoFlags, out fRenameCanContinue);
			NativeMethods.ThrowOnFailure(hr);
			return (fRenameCanContinue != 0);
		}

		/// <summary>
		/// Queries the environment to see whether the project file can be renamed.
		/// </summary>
		/// <param name="oldPath">The path to the old project file name.</param>
		/// <param name="newPath">The path to the new project file name.</param>
		/// <returns>true if the project file can be renamed; otherwise, false.</returns>
		public bool CanRenameProject(string oldPath, string newPath)
		{
			int fRenameCanContinue;
			int hr = this.Solution.QueryRenameProject(this.project, oldPath, newPath, 0, out fRenameCanContinue);
			NativeMethods.ThrowOnFailure(hr);
			return (fRenameCanContinue != 0);
		}

		/// <summary>
		/// Notifies the environment that directories have been renamed.
		/// </summary>
		/// <param name="oldPaths">The paths to the old directories.</param>
		/// <param name="newPaths">The paths to the new directories.</param>
		public void OnDirectoriesRenamed(string[] oldPaths, string[] newPaths)
		{
			Tracer.VerifyNonNullArgument(oldPaths, "oldPaths");
			Tracer.VerifyNonNullArgument(newPaths, "newPaths");

			if (oldPaths.Length != newPaths.Length)
			{
				throw new ArgumentException("The old and new path arrays must match in size.", "oldPaths");
			}

			int length = oldPaths.Length;

			// If the arrays are zero-length, then there's nothing to do.
			if (length == 0)
			{
				return;
			}

			// Fill in the flags array (since there are no flags yet, we can leave the array at its default value).
			VSRENAMEDIRECTORYFLAGS[] flags = new VSRENAMEDIRECTORYFLAGS[length];
			int hr = this.Tracker.OnAfterRenameDirectories(this.project, length, oldPaths, newPaths, flags);
			NativeMethods.ThrowOnFailure(hr);
		}

		/// <summary>
		/// Notifies the environment that a directory has been renamed.
		/// </summary>
		/// <param name="oldPath">The path to the old directory.</param>
		/// <param name="newPath">The path to the new directory.</param>
		public void OnDirectoryRenamed(string oldPath, string newPath)
		{
			this.OnDirectoriesRenamed(new string[] { oldPath }, new string[] { newPath });
		}

		/// <summary>
		/// Notifies the environment that a file has been renamed.
		/// </summary>
		/// <param name="oldPath">The path to the old file name.</param>
		/// <param name="newPath">The path to the new file name.</param>
		public void OnFileRenamed(string oldPath, string newPath)
		{
			int hr = this.Tracker.OnAfterRenameFile(this.project, oldPath, newPath, VSRENAMEFILEFLAGS.VSRENAMEFILEFLAGS_NoFlags);
			NativeMethods.ThrowOnFailure(hr);
		}

		/// <summary>
		/// Notifies the environment that a project file has been renamed.
		/// </summary>
		/// <param name="oldPath">The path to the old project file name.</param>
		/// <param name="newPath">The path to the new project file name.</param>
		/// <remarks>
		/// Unlike the other On* methods, this method handles updating the RDT.
		/// </remarks>
		public void OnProjectRenamed(string oldPath, string newPath)
		{
			int hr = this.Solution.OnAfterRenameProject(this.project, oldPath, newPath, 0);
			NativeMethods.ThrowOnFailure(hr);
		}
		#endregion
	}
}
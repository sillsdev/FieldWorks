//--------------------------------------------------------------------------------------------------
// <copyright file="FileChangeNotificationSuspender.cs" company="Microsoft">
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
// Utility class for suspending and resuming file change notifications to the Visual Studio environment.
// </summary>
//--------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using Microsoft.VisualStudio.Shell.Interop;

	/// <summary>
	/// Utility class for suspending and resuming file change notifications to the Visual Studio
	/// environment, which is useful during file renames, saving, and other non-atomic operations.
	/// </summary>
	public class FileChangeNotificationSuspender : IDisposable
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private const int IgnoreChanges = 1;
		private const int ResumeNotification = 0;

		private static readonly Type classType = typeof(FileChangeNotificationSuspender);

		private IVsDocDataFileChangeControl docDataFileChangeControl;
		private string filePath;
		private bool suspended;
		#endregion

		#region Constructors / Finalizer
		//==========================================================================================
		// Constructors / Finalizer
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="FileChangeNotificationSuspender"/> class.
		/// </summary>
		/// <param name="filePath">The absolute path of the file to suspend notifications about.</param>
		public FileChangeNotificationSuspender(string filePath)
		{
			Tracer.VerifyStringArgument(filePath, "filePath");
			this.filePath = filePath;
			this.Suspend();
		}

		/// <summary>
		/// Finalizer.
		/// </summary>
		~FileChangeNotificationSuspender()
		{
			Tracer.Fail("The finalizer for {0} should not have been invoked. Please explicitly call Dispose().", this.GetType().Name);
			this.Dispose(false);
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets the absolute path of the file of which change notifications are suspended.
		/// </summary>
		public string FilePath
		{
			get { return this.filePath; }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Disposes managed and native resources.
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Resumes file change notifications to the environment for the wrapped file.
		/// </summary>
		public void Resume()
		{
			if (!this.suspended)
			{
				return;
			}

			// Get the environment's change notifier.
			IVsFileChangeEx changeNotifier = Package.Instance.Context.GetService(typeof(SVsFileChangeEx)) as IVsFileChangeEx;
			Tracer.WriteLineIf(classType, "Resume", Tracer.Level.Warning, changeNotifier == null, "Could not get an instance of the IVsChangeEx interface.");
			if (changeNotifier != null)
			{
				// Tell the environment to resume sending change notifications for the file.
				int hr = changeNotifier.IgnoreFile(0, this.FilePath, ResumeNotification);
				Tracer.WriteLineIf(classType, "Resume", Tracer.Level.Warning, NativeMethods.Failed(hr), "Could not tell the environment to resume file change notifications to '{0}': Hr=0x{1:x}", this.FilePath, hr);
			}

			// Tell the environment to resume sending change notifications to editors.
			if (this.docDataFileChangeControl != null)
			{
				int hr = this.docDataFileChangeControl.IgnoreFileChanges(ResumeNotification);
				Tracer.WriteLineIf(classType, "Resume", Tracer.Level.Warning, NativeMethods.Failed(hr), "Could not tell the environment to resume file change notifications to editors of file '{0}': Hr=0x{1:x}", this.FilePath, hr);
				this.docDataFileChangeControl = null;
			}

			// At this point we can consider ourself resumed.
			this.suspended = false;
			Tracer.WriteLineVerbose(classType, "Resume", "Resumed file change notifications for '{0}'.", this.FilePath);
		}

		/// <summary>
		/// Suspends any file change notifications to the environment for the wrapped file in
		/// preparation for a multi-stage file operation such as a rename.
		/// </summary>
		public void Suspend()
		{
			// We only want to suspend once.
			if (this.suspended)
			{
				return;
			}

			// Get the environment's change notifier.
			IVsFileChangeEx changeNotifier = Package.Instance.Context.GetService(typeof(SVsFileChangeEx)) as IVsFileChangeEx;
			if (changeNotifier == null)
			{
				Tracer.WriteLineWarning(classType, "Suspend", "Could not get an instance of the IVsChangeEx interface.");
				return;
			}

			// Tell the environment to stop sending change notifications for the file.
			int hr = changeNotifier.IgnoreFile(0, this.FilePath, IgnoreChanges);
			Tracer.WriteLineIf(classType, "Suspend", Tracer.Level.Warning, NativeMethods.Failed(hr), "Could not tell the environment to ignore file changes to '{0}': Hr=0x{1:x}", this.FilePath, hr);
			NativeMethods.ThrowOnFailure(hr);

			// Get the IVsDocDataFileChangeControl interface from the DocumentData. We need this
			// to suspend file change notifications to all editors.
			DocumentInfo docInfo = Package.Instance.Context.RunningDocumentTable.FindByPath(this.FilePath);
			if (docInfo != null)
			{
				this.docDataFileChangeControl = docInfo.DocumentData as IVsDocDataFileChangeControl;
				if (this.docDataFileChangeControl != null)
				{
					hr = this.docDataFileChangeControl.IgnoreFileChanges(IgnoreChanges);
					NativeMethods.ThrowOnFailure(hr);
				}
			}

			// At this point we can consider ourself suspended.
			this.suspended = true;
			Tracer.WriteLineVerbose(classType, "Suspend", "Suspended file change notifications for '{0}'.", this.FilePath);
		}

		/// <summary>
		/// Disposes managed and native resources.
		/// </summary>
		/// <param name="disposing">Indicates whether the caller is the finalizer or not.</param>
		protected virtual void Dispose(bool disposing)
		{
			this.Resume();
		}
		#endregion
	}
}
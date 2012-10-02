//-------------------------------------------------------------------------------------------------
// <copyright file="LaunchPad.cs" company="Microsoft">
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
// Options for launching an executable via the IVsLaunchPad
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Globalization;
	using System.Runtime.InteropServices;
	using Microsoft.VisualStudio.Shell.Interop;

	/// <summary>
	/// Wraps an <see cref="IVsLaunchPad"/> object in a .NET-friendly way.
	/// </summary>
	/// <remarks>
	/// Visual Studio has a handy little tool that will make life a lot easier when
	/// executing a command-line tool. It will parse the lines automatically and
	/// add them to the task pane. It also takes care of cleaning up the process.
	/// </remarks>
	public class LaunchPad
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(LaunchPad);

		private string arguments;
		private string filename;
		private _LAUNCHPAD_FLAGS flags = _LAUNCHPAD_FLAGS.LPF_PipeStdoutToOutputWindow | _LAUNCHPAD_FLAGS.LPF_PipeStdoutToTaskList;
		private _vstaskbitmap taskItemBitmap = _vstaskbitmap.BMP_COMPILE;
		private VSTASKCATEGORY taskPadCategory = VSTASKCATEGORY.CAT_BUILDCOMPILE;
		private IVsLaunchPad launchPad;
		private string workingDirectory;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		public LaunchPad() : this(String.Empty, String.Empty)
		{
		}

		public LaunchPad(string filename) : this(filename, String.Empty)
		{
		}

		public LaunchPad(string filename, string arguments)
		{
			this.filename = filename.Trim();
			this.arguments = arguments.Trim();
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		public string Arguments
		{
			get { return this.arguments; }
			set
			{
				if (value != null)
				{
					value.Trim();
				}
				this.arguments = value;
			}
		}

		public string CommandLine
		{
			get
			{
				string fileName = PackageUtility.QuoteString(this.Filename);
				if (this.Arguments == null || this.Arguments.Length == 0)
				{
					return fileName;
				}
				string commandLine = fileName + " " + this.Arguments;
				return commandLine;
			}
		}

		public string Filename
		{
			get { return this.filename; }
			set
			{
				if (value != null)
				{
					value.Trim();
				}
				this.filename = value;
			}
		}

		public _LAUNCHPAD_FLAGS Flags
		{
			get { return this.flags; }
			set { this.flags = value; }
		}

		public _vstaskbitmap TaskItemBitmap
		{
			get { return this.taskItemBitmap; }
			set { this.taskItemBitmap = value; }
		}

		public VSTASKCATEGORY TaskPadCategory
		{
			get { return this.taskPadCategory; }
			set { this.taskPadCategory = value; }
		}

		public string WorkingDirectory
		{
			get { return this.workingDirectory; }
			set { this.workingDirectory = value; }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Executes the command line, piping output to the specified output pane.
		/// </summary>
		/// <param name="outputPane">The output pane to write message to.</param>
		/// <param name="launchPadEvents">Receives event callbacks on the progress of the process.</param>
		/// <returns>Value returned by the process.</returns>
		public int ExecuteCommand(IVsOutputWindowPane outputPane, IVsLaunchPadEvents launchPadEvents)
		{
			Tracer.VerifyNonNullArgument(outputPane, "outputPane");
			Tracer.VerifyNonNullArgument(launchPadEvents, "launchPadEvents");

			// Create the IVsLaunchPad object if it hasn't been created yet.
			if (this.launchPad == null)
			{
				this.launchPad = this.CreateLaunchPad();
			}

			uint processExitCode;
			uint flags = unchecked((uint)this.Flags);
			uint taskPadCategory = unchecked((uint)this.TaskPadCategory);
			uint taskItemBitmap = unchecked((uint)this.TaskItemBitmap);
			int hr = this.launchPad.ExecCommand(null, this.CommandLine, this.WorkingDirectory, flags, outputPane, taskPadCategory, taskItemBitmap, null, launchPadEvents, out processExitCode, null);
			if (NativeMethods.Failed(hr))
			{
				string debugMessage = PackageUtility.SafeStringFormatInvariant("Error in attempting to launch command '{0}': Hr=0x{1:x}", this.CommandLine, hr);
				Package.Instance.Context.NotifyInternalError(ResourceId.IDS_E_BUILD, debugMessage);
				Tracer.Fail(debugMessage);
			}

			return (int)processExitCode;
		}

		/// <summary>
		/// Creates a new <see cref="IVsLaunchPad"/> object.
		/// </summary>
		/// <returns>A <see cref="IVsLaunchPad"/> object.</returns>
		private IVsLaunchPad CreateLaunchPad()
		{
			IVsLaunchPad launchPad = null;

			// Get a IVsLaunchPadFactory from the environment to use in creating our IVsLaunchPad.
			IVsLaunchPadFactory launchPadFactory = (IVsLaunchPadFactory)Package.Instance.Context.ServiceProvider.GetService(typeof(IVsLaunchPadFactory));
			if (launchPadFactory != null)
			{
				int hr = launchPadFactory.CreateLaunchPad(out launchPad);
				NativeMethods.ThrowOnFailure(hr);
			}

			return launchPad;
		}
		#endregion
	}
}

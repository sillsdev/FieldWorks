//-------------------------------------------------------------------------------------------------
// <copyright file="BuildableProjectConfiguration.cs" company="Microsoft">
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
// Provides configuration information to the Visual Studio shell about a buildable WiX project.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Globalization;
	using System.Runtime.InteropServices;
	using Microsoft.VisualStudio.Shell.Interop;

	public class BuildableProjectConfiguration : IVsBuildableProjectCfg, IVsLaunchPadEvents
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(BuildableProjectConfiguration);

		protected const int VS_BUILDABLEPROJECTCFGOPTS_REBUILD = 1;
		protected const int VS_BUILDABLEPROJECTCFGOPTS_BUILD_SELECTION_ONLY = 2;
		protected const int VS_BUILDABLEPROJECTCFGOPTS_BUILD_ACTIVE_DOCUMENT_ONLY = 4;

		private bool buildSuccessful = true;
		private bool cancelBuild;
		private bool isBuilding;
		private IVsOutputWindowPane outputPane;
		private ProjectConfiguration projectConfiguration;
		private VsBuildStatusEventListenerCollection eventListeners = new VsBuildStatusEventListenerCollection();
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		public BuildableProjectConfiguration(ProjectConfiguration projectConfiguration)
		{
			Tracer.VerifyNonNullArgument(projectConfiguration, "projectConfiguration");
			this.projectConfiguration = projectConfiguration;
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		public Project Project
		{
			get { return this.ProjectConfiguration.Project; }
		}

		public ProjectConfiguration ProjectConfiguration
		{
			get { return this.projectConfiguration; }
		}

		protected bool BuildSuccessful
		{
			get { return this.buildSuccessful; }
		}

		protected bool CancelBuild
		{
			get { return this.cancelBuild; }
		}

		/// <summary>
		/// Gets a value indicating whether the build is currently running.
		/// </summary>
		protected bool IsBuilding
		{
			get { return this.isBuilding; }
			set { this.isBuilding = value; }
		}

		/// <summary>
		/// Gets a value indicating whether the project needs to be built.
		/// </summary>
		protected virtual bool IsUpToDate
		{
			get { return false; }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		#region IVsBuildableProjectCfg Members
		int IVsBuildableProjectCfg.AdviseBuildStatusCallback(IVsBuildStatusCallback pIVsBuildStatusCallback, out uint pdwCookie)
		{
			pdwCookie = this.eventListeners.Add(pIVsBuildStatusCallback);
			return NativeMethods.S_OK;
		}

		int IVsBuildableProjectCfg.get_ProjectCfg(out IVsProjectCfg ppIVsProjectCfg)
		{
			ppIVsProjectCfg = this.ProjectConfiguration;
			return NativeMethods.S_OK;
		}

		int IVsBuildableProjectCfg.QueryStartBuild(uint dwOptions, int[] pfSupported, int[] pfReady)
		{
			// dwOptions and pfReady are unused according to the documentation.
			int supportedAndReady = Convert.ToInt32(!this.IsBuilding);
			if (pfSupported != null && pfSupported.Length > 0)
			{
				pfSupported[0] = supportedAndReady;
			}
			if (pfReady != null && pfReady.Length > 0)
			{
				pfReady[0] = supportedAndReady;
			}
			return NativeMethods.S_OK;
		}

		int IVsBuildableProjectCfg.QueryStartClean(uint dwOptions, int[] pfSupported, int[] pfReady)
		{
			// dwOptions and pfReady are unused according to the documentation.
			int supportedAndReady = Convert.ToInt32(!this.IsBuilding);
			if (pfSupported != null && pfSupported.Length > 0)
			{
				pfSupported[0] = supportedAndReady;
			}
			if (pfReady != null && pfReady.Length > 0)
			{
				pfReady[0] = supportedAndReady;
			}
			return NativeMethods.S_OK;
		}

		int IVsBuildableProjectCfg.QueryStartUpToDateCheck(uint dwOptions, int[] pfSupported, int[] pfReady)
		{
			// dwOptions and pfReady are unused according to the documentation.
			int supportedAndReady = Convert.ToInt32(!this.IsBuilding);
			if (pfSupported != null && pfSupported.Length > 0)
			{
				pfSupported[0] = supportedAndReady;
			}
			if (pfReady != null && pfReady.Length > 0)
			{
				pfReady[0] = supportedAndReady;
			}
			return NativeMethods.S_OK;
		}

		int IVsBuildableProjectCfg.QueryStatus(out int pfBuildDone)
		{
			pfBuildDone = Convert.ToInt32(this.BuildSuccessful);
			return NativeMethods.S_OK;
		}

		int IVsBuildableProjectCfg.StartBuild(IVsOutputWindowPane pIVsOutputWindowPane, uint dwOptions)
		{
			Tracer.VerifyNonNullArgument(pIVsOutputWindowPane, "pIVsOutputWindowPane");

			if ((dwOptions & VS_BUILDABLEPROJECTCFGOPTS_REBUILD) == VS_BUILDABLEPROJECTCFGOPTS_REBUILD)
			{
				this.Rebuild(pIVsOutputWindowPane);
			}
			else
			{
				this.Build(pIVsOutputWindowPane);
			}

			return NativeMethods.S_OK;
		}

		int IVsBuildableProjectCfg.StartClean(IVsOutputWindowPane pIVsOutputWindowPane, uint dwOptions)
		{
			Tracer.VerifyNonNullArgument(pIVsOutputWindowPane, "pIVsOutputWindowPane");
			this.Clean(pIVsOutputWindowPane);
			return NativeMethods.S_OK;
		}

		int IVsBuildableProjectCfg.StartUpToDateCheck(IVsOutputWindowPane pIVsOutputWindowPane, uint dwOptions)
		{
			// If the project is up to date, then we return S_OK; otherwise VS wants E_FAIL.
			return (this.IsUpToDate ? NativeMethods.S_OK : NativeMethods.E_FAIL);
		}

		int IVsBuildableProjectCfg.Stop(int fSync)
		{
			if (this.isBuilding)
			{
				this.cancelBuild = true;
				// TODO: Implement synchronous stopping if fSync is true.
			}
			return NativeMethods.S_OK;
		}

		int IVsBuildableProjectCfg.UnadviseBuildStatusCallback(uint dwCookie)
		{
			this.eventListeners.Remove(dwCookie);
			return NativeMethods.S_OK;
		}

		int IVsBuildableProjectCfg.Wait(uint dwMilliseconds, int fTickWhenMessageQNotEmpty)
		{
			// The documentation says this method is obsolete and not to use.
			return NativeMethods.S_OK;
		}
		#endregion

		#region IVsLaunchPadEvents Members
		int IVsLaunchPadEvents.Tick(ref int pfCancel)
		{
			bool continueBuild = this.eventListeners.OnTick();
			if (!continueBuild)
			{
				this.cancelBuild = true;
				pfCancel = 1;
			}
			return NativeMethods.S_OK;
		}
		#endregion

		/// <summary>
		/// Performs an incremental build (or full build if the project does not support incremental builds).
		/// </summary>
		/// <param name="outputPane">The window to output build messages to.</param>
		/// <returns>true if the build was successful; otherwise, false.</returns>
		public virtual bool Build(IVsOutputWindowPane outputPane)
		{
			return this.BuildOperation(outputPane, new InternalBuildOperation(this.BuildInternal));
		}

		/// <summary>
		/// Performs a clean build, which should clean any intermediate and output files.
		/// </summary>
		/// <param name="outputPane">The window to output build messages to.</param>
		/// <returns>true if the build was successful; otherwise, false.</returns>
		public virtual bool Clean(IVsOutputWindowPane outputPane)
		{
			return this.BuildOperation(outputPane, new InternalBuildOperation(this.CleanInternal));
		}

		/// <summary>
		/// Performs a full build, which is normally a clean followed by an incremental build.
		/// </summary>
		/// <param name="outputPane">The window to output build messages to.</param>
		/// <returns>true if the build was successful; otherwise, false.</returns>
		public virtual bool Rebuild(IVsOutputWindowPane outputPane)
		{
			return this.BuildOperation(outputPane, new InternalBuildOperation(this.RebuildInternal));
		}

		/// <summary>
		/// Used for subclasses to just implement the "meat" of their build without having to worry
		/// about initial build setup and cleanup. <see cref="Build"/> will call this method at the
		/// appropriate time.
		/// </summary>
		/// <param name="outputPane">The window to output message to.</param>
		/// <returns>true if the build succeeded; otherwise, false.</returns>
		protected virtual bool BuildInternal(IVsOutputWindowPane outputPane)
		{
			return true;
		}

		/// <summary>
		/// Used for subclasses to just implement the "meat" of their clean operation without having to worry
		/// about initial build setup and cleanup. <see cref="Clean"/> will call this method at the
		/// appropriate time.
		/// </summary>
		/// <param name="outputPane">The window to output message to.</param>
		/// <returns>true if the build succeeded; otherwise, false.</returns>
		protected virtual bool CleanInternal(IVsOutputWindowPane outputPane)
		{
			return true;
		}

		/// <summary>
		/// Used for subclasses to just implement the "meat" of their rebuild operation without having to worry
		/// about intitial build setup and cleanup. <see cref="Rebuild"/> will call this method at the
		/// appropriate time. The default implementation simply calls <see cref="CleanInternal"/> followed
		/// by <see cref="BuildInternal"/>.
		/// </summary>
		/// <param name="outputPane">The window to output message to.</param>
		/// <returns>true if the build succeeded; otherwise, false.</returns>
		protected virtual bool RebuildInternal(IVsOutputWindowPane outputPane)
		{
			bool successful = this.CleanInternal(outputPane);
			successful = successful && this.BuildInternal(outputPane);

			return successful;
		}

		/// <summary>
		/// Sets the <see cref="IsBuilding"/> and <see cref="BuildSuccessful"/> flags and lets the
		/// environment know that the build is done.
		/// </summary>
		/// <param name="successful">Indicates whether the build was successful or not.</param>
		protected virtual void FinishBuild(bool successful)
		{
			this.isBuilding = false;
			this.buildSuccessful = successful;
			this.outputPane = null;

			// Let the environment know that we're done building.
			this.eventListeners.OnBuildEnd(successful);
		}

		/// <summary>
		/// Makes sure that another build is not processing, resets the build flags, and lets
		/// the environment know that the build has started.
		/// </summary>
		/// <param name="outputPane">The <see cref="IVsOutputWindowPane"/> to use for writing messages to the environment.</param>
		/// <returns>true if the build should proceed; otherwise, false.</returns>
		protected virtual bool PrepareBuild(IVsOutputWindowPane outputPane)
		{
			// Check to make sure another build is not happening.
			if (this.IsBuilding)
			{
				string message = Package.Instance.Context.NativeResources.GetString(ResourceId.IDS_ANOTHERPROJECTBUILDING);
				Package.Instance.Context.ShowMessageBox(message, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST, OLEMSGICON.OLEMSGICON_WARNING);
				Tracer.WriteLineInformation(classType, "PrepareBuild", "Another build is already running. Skipping this build.");
				return false;
			}

			this.IsBuilding = true;
			this.buildSuccessful = false;
			this.outputPane = outputPane;

			// Let the environment know that we've started the build.
			this.cancelBuild = !this.eventListeners.OnBuildBegin();
			if (this.CancelBuild)
			{
				this.FinishBuild(false);
				return false;
			}
			return true;
		}

		/// <summary>
		/// Ticks the build by sending a Tick event to the build listeners, who have a chance to cancel the build.
		/// </summary>
		/// <returns>true if the build should continue; otherwise, false.</returns>
		protected bool TickBuild()
		{
			// Tick once and see if we should continue.
			if (this.CancelBuild || !this.eventListeners.OnTick())
			{
				this.cancelBuild = true;
				this.FinishBuild(false);
				return false;
			}
			return true;
		}

		/// <summary>
		/// Writes a blank line to the output window pane.
		/// </summary>
		protected void WriteLineToOutputWindow()
		{
			this.WriteLineToOutputWindow(String.Empty, null);
		}

		/// <summary>
		/// Writes a formatted message to the output window pane, terminated with a new line.
		/// </summary>
		/// <param name="message">The string to format.</param>
		/// <param name="args">The arguments to insert into the formatted string.</param>
		protected void WriteLineToOutputWindow(string message, params object[] args)
		{
			this.WriteToOutputWindow(message + Environment.NewLine, args);
		}

		/// <summary>
		/// Writes a formatted message to the output window pane.
		/// </summary>
		/// <param name="message">The string to format.</param>
		/// <param name="args">The arguments to insert into the formatted string.</param>
		protected void WriteToOutputWindow(string message, params object[] args)
		{
			Tracer.Assert(this.outputPane != null, "The IVsOutputWindowPane has not been set yet. Call PrepareBuild first.");
			string formattedMessage;
			if (args == null || args.Length == 0)
			{
				formattedMessage = message;
			}
			else
			{
				formattedMessage = PackageUtility.SafeStringFormat(CultureInfo.CurrentCulture, message, args);
			}
			this.outputPane.OutputString(formattedMessage);
		}


		/// <summary>
		/// Sets up the build system in preparation for a build operation (clean, build, rebuild) and calls
		/// the operation delegate at the appropriate place. This also handles any errors that occur and
		/// makes sure the build isn't in a weird state.
		/// </summary>
		/// <param name="outputPane">The window to output build messages to.</param>
		/// <param name="operationDelegate">The method to call after setup has occurred and before cleanup.</param>
		/// <returns>true if the operation was successful; otherwise, false.</returns>
		private bool BuildOperation(IVsOutputWindowPane outputPane, InternalBuildOperation operationDelegate)
		{
			Tracer.VerifyNonNullArgument(outputPane, "outputPane");

			if (!this.PrepareBuild(outputPane))
			{
				return false;
			}

			bool successful = false;

			try
			{
				successful = operationDelegate(outputPane);
			}
			catch (Exception e)
			{
				if (ErrorUtility.IsExceptionUnrecoverable(e))
				{
					throw;
				}

				this.WriteLineToOutputWindow("There was an error while building the project.");

				// Append the "Consult the trace log message"
				string message = String.Empty;
				PackageUtility.AppendConsultTraceMessage(ref message);
				if (!String.IsNullOrEmpty(message))
				{
					this.WriteLineToOutputWindow(message);
				}

				this.WriteLineToOutputWindow("Exception: {0}", e);
				this.WriteLineToOutputWindow();

				successful = false;
			}
			finally
			{
				this.FinishBuild(successful);
			}

			return successful;
		}
		#endregion

		#region Delegates
		//==========================================================================================
		// Delegates
		//==========================================================================================

		private delegate bool InternalBuildOperation(IVsOutputWindowPane outputPane);
		#endregion
	}
}

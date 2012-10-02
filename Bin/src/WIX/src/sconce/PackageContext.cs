//-------------------------------------------------------------------------------------------------
// <copyright file="PackageContext.cs" company="Microsoft">
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
// Contains package context information, such as helper classes and settings.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Collections;
	using System.Diagnostics;
	using System.Globalization;
	using System.IO;
	using System.Runtime.InteropServices;
	using System.Text;
	using System.Threading;
	using Microsoft.VisualStudio.Shell.Interop;

	using ResId = ResourceId;

	// <summary>
	// Contains package context information, such as helper classes and settings.
	// </summary>
	public class PackageContext : IDisposable
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(PackageContext);

		private ManagedResourceManager managedResources;
		private NativeResourceManager nativeResources;
		private RunningDocumentTable runningDocumentTable;
		private ServiceProvider serviceProvider;
		private PackageSettings settings;
		private IVsUIHierarchyWindow solutionExplorer;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="PackageContext"/> class.
		/// </summary>
		/// <param name="serviceProvider">
		/// The <see cref="ServiceProvider"/> instance to use for getting services from the environment.
		/// </param>
		public PackageContext(ServiceProvider serviceProvider)
		{
			Tracer.VerifyNonNullArgument(serviceProvider, "serviceProvider");
			this.serviceProvider = serviceProvider;

			// Get an IUIHostLocale instance and Visual Studio's locale
			IUIHostLocale hostLocale = this.GetService(typeof(SUIHostLocale)) as IUIHostLocale;
			Tracer.Assert(hostLocale != null, "Cannot get Visual Studio's locale. Defaulting to current thread's locale.");
			int lcid = Thread.CurrentThread.CurrentUICulture.LCID;
			if (hostLocale != null)
			{
				uint lcidUnsigned;
				int hr = hostLocale.GetUILocale(out lcidUnsigned);
				if (NativeMethods.Succeeded(hr))
				{
					lcid = (int)lcidUnsigned;
				}
				else
				{
					Tracer.Fail("Cannot get Visual Studio's locale. Defaulting to current thread's locale.");
				}
			}

			// Initialize our helpers
			this.managedResources = this.CreateManagedResourceManager();
			this.nativeResources = new NativeResourceManager(lcid);
			this.settings = this.CreatePackageSettings(this.ServiceProvider);
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets a value indicating whether the solution is currently building or deploying.
		/// </summary>
		public bool IsSolutionBuilding
		{
			get
			{
				// Get the build manager from VS
				IVsSolutionBuildManager solutionBuildMgr = this.ServiceProvider.GetServiceOrThrow(typeof(SVsSolutionBuildManager), typeof(IVsSolutionBuildManager), classType, "IsSolutionBuilding") as IVsSolutionBuildManager;

				// We have to verify that the environment is not busy right now
				int busy;
				NativeMethods.ThrowOnFailure(solutionBuildMgr.QueryBuildManagerBusy(out busy));
				return (busy != 0);
			}
		}

		/// <summary>
		/// Gets the managed resource manager for the package.
		/// </summary>
		public ManagedResourceManager ManagedResources
		{
			get { return this.managedResources; }
		}

		/// <summary>
		/// Gets the native resource manager for the package.
		/// </summary>
		public NativeResourceManager NativeResources
		{
			get { return this.nativeResources; }
		}

		/// <summary>
		/// Gets the wrapped RDT (Running Document Table).
		/// </summary>
		public RunningDocumentTable RunningDocumentTable
		{
			get
			{
				if (this.runningDocumentTable == null)
				{
					this.runningDocumentTable = new RunningDocumentTable(this.ServiceProvider);
				}
				return this.runningDocumentTable;
			}
		}

		/// <summary>
		/// Gets the service provider for the package.
		/// </summary>
		public ServiceProvider ServiceProvider
		{
			get { return this.serviceProvider; }
		}

		/// <summary>
		/// Gets the package settings.
		/// </summary>
		public PackageSettings Settings
		{
			get { return this.settings; }
		}

		/// <summary>
		/// Gets the Solution Explorer window frame from the environment.
		/// </summary>
		public IVsUIHierarchyWindow SolutionExplorer
		{
			get
			{
				if (this.solutionExplorer == null)
				{
					// Try to get the solution explorer window frame.
					Guid solutionExplorerGuid = VsGuids.SolutionExplorer;
					IVsWindowFrame frame;
					IVsUIShell uiShell = this.ServiceProvider.GetVsUIShell(classType, "SolutionExplorer");
					NativeMethods.ThrowOnFailure(uiShell.FindToolWindow(0, ref solutionExplorerGuid, out frame));

					// Get the IVsWindowPane for the Solution Explorer, which will be the IVsUIHierarchyWindow.
					object pvar;
					NativeMethods.ThrowOnFailure(frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocView, out pvar));
					Tracer.Assert(pvar != null, "Cannot get a pointer to the Solution Explorer IVsWindowPane.");
					if (pvar != null)
					{
						IVsWindowPane pane = (IVsWindowPane)pvar;
						this.solutionExplorer = (IVsUIHierarchyWindow)pane;
					}

					// We'd better have a valid pointer at this point.
					if (this.solutionExplorer == null)
					{
						string message = "Cannot get a valid pointer to the IVsUIHierarchyWindow for the Solution Explorer.";
						Tracer.Fail(message);
						this.NotifyInternalError(message);
					}
				}
				return this.solutionExplorer;
			}
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Cleans up managed and native resources.
		/// </summary>
		public void Dispose()
		{
			this.Dispose(true);
		}

		/// <summary>
		/// Gets a service from the environment using the cached service provider.
		/// </summary>
		/// <param name="serviceType">An object that specifies the type of service object to get.</param>
		/// <returns>
		/// A service object of type <paramref name="serviceType"/>, or null if there is no service
		/// object of type <paramref name="serviceType"/>.
		/// </returns>
		public object GetService(Type serviceType)
		{
			return this.ServiceProvider.GetService(serviceType);
		}

		/// <summary>
		/// Shows a message box with an internal error message and the specified debug information (if it's a debug build).
		/// </summary>
		/// <param name="debugInformation">The extra information to show in a debug build.</param>
		public void NotifyInternalError(string debugInformation)
		{
			this.NotifyInternalError(ResId.IDS_E_INTERNALERROR, debugInformation);
		}

		/// <summary>
		/// Shows a message box with the specified error message and the specified debug information (if it's a debug build).
		/// </summary>
		/// <param name="messageId">The resource identifier of the message to show.</param>
		/// <param name="debugInformation">The extra information to show in a debug build.</param>
		public void NotifyInternalError(ResId messageId, string debugInformation)
		{
			string title = this.NativeResources.GetString(messageId);
			string message = String.Empty;
			PackageUtility.AppendConsultTraceMessage(ref message);
			PackageUtility.AppendDebugInformation(ref message, debugInformation);
			this.ShowErrorMessageBox(title, message);
		}

		/// <summary>
		/// Shows a yes/no dialog box with the No button selected by default and a question icon.
		/// </summary>
		/// <param name="message">The message to show the user.</param>
		/// <returns>true if the user clicked Yes; otherwise, false.</returns>
		public bool PromptYesNo(string message)
		{
			return this.PromptYesNo(null, message, OLEMSGICON.OLEMSGICON_QUERY);
		}

		/// <summary>
		/// Shows a yes/no dialog box with the No button selected by default.
		/// </summary>
		/// <param name="title">The first line of the message box (can be null).</param>
		/// <param name="message">The second line of the message box if <paramref name="title"/> is null; otherwise the first line.</param>
		/// <param name="icon">The icon to show on the message box.</param>
		/// <returns>true if the user clicked Yes; otherwise, false.</returns>
		public bool PromptYesNo(string title, string message, OLEMSGICON icon)
		{
			VsMessageBoxResult result = this.ShowMessageBox(title, message, OLEMSGBUTTON.OLEMSGBUTTON_YESNO, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_SECOND, icon);
			return (result == VsMessageBoxResult.Yes);
		}

		/// <summary>
		/// Shows an error-type message box (with an exclamation icon and an OK button) with the specified message.
		/// </summary>
		/// <param name="messageId">The resource identifier of the message to show.</param>
		/// <param name="args">An array of arguments to use for formatting the message.</param>
		public void ShowErrorMessageBox(ResId messageId, params object[] args)
		{
			string message = this.NativeResources.GetString(messageId);
			if (args != null && args.Length > 0)
			{
				message = String.Format(CultureInfo.CurrentUICulture, message, args);
			}
			this.ShowErrorMessageBox(null, message);
		}

		/// <summary>
		/// Shows an error-type message box (with an exclamation icon and an OK button) with the specified message.
		/// </summary>
		/// <param name="message">The message to show.</param>
		public void ShowErrorMessageBox(string message)
		{
			this.ShowErrorMessageBox(null, message);
		}

		/// <summary>
		/// Shows an error-type message box (with an exclamation icon and an OK button) with the specified message.
		/// </summary>
		/// <param name="titleId">The resource identifier of the first line of the message box (not the caption).</param>
		/// <param name="messageId">The resource identifier of the second line (or paragraph) of the message box.</param>
		public void ShowErrorMessageBox(ResId titleId, ResId messageId)
		{
			string title = this.NativeResources.GetString(titleId);
			string message = this.NativeResources.GetString(messageId);
			this.ShowErrorMessageBox(title, message);
		}

		/// <summary>
		/// Shows an error-type message box (with an exclamation icon and an OK button) with the specified message.
		/// </summary>
		/// <param name="title">The first line of the message box (not the caption).</param>
		/// <param name="message">The second line (or paragraph) of the message box.</param>
		public void ShowErrorMessageBox(string title, string message)
		{
			this.ShowMessageBox(title, message, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST, OLEMSGICON.OLEMSGICON_CRITICAL);
		}

		/// <summary>
		/// Shows the message box that Visual Studio uses for prompts.
		/// </summary>
		/// <param name="message">The message to show the user.</param>
		/// <param name="buttons">The buttons to show on the message box.</param>
		/// <param name="defaultButton">The button that will have the default focus.</param>
		/// <param name="icon">The icon to show on the message box.</param>
		/// <returns>One of the <see cref="VsMessageBoxResult"/> values, indicating which button was pressed.</returns>
		public VsMessageBoxResult ShowMessageBox(string message, OLEMSGBUTTON buttons, OLEMSGDEFBUTTON defaultButton, OLEMSGICON icon)
		{
			return this.ShowMessageBox(null, message, buttons, defaultButton, icon);
		}

		/// <summary>
		/// Shows the message box that Visual Studio uses for prompts.
		/// </summary>
		/// <param name="title">The first line of the message box (can be null).</param>
		/// <param name="message">The second line of the message box if <paramref name="title"/> is null; otherwise the first line.</param>
		/// <param name="buttons">The buttons to show on the message box.</param>
		/// <param name="defaultButton">The button that will have the default focus.</param>
		/// <param name="icon">The icon to show on the message box.</param>
		/// <returns>One of the <see cref="VsMessageBoxResult"/> values, indicating which button was pressed.</returns>
		public VsMessageBoxResult ShowMessageBox(string title, string message, OLEMSGBUTTON buttons, OLEMSGDEFBUTTON defaultButton, OLEMSGICON icon)
		{
			Guid emptyGuid = Guid.Empty;
			int result;
			IVsUIShell uiShell = this.ServiceProvider.GetVsUIShell(classType, "ShowMessageBox");
			NativeMethods.ThrowOnFailure(uiShell.ShowMessageBox(0, ref emptyGuid, title, message, null, 0, buttons, defaultButton, icon, 0, out result));
			return (VsMessageBoxResult)result;
		}

		/// <summary>
		/// Shows the Visual Studio open file dialog.
		/// </summary>
		/// <param name="dialogTitle">The title for the dialog box.</param>
		/// <param name="filter">The filter for the dialog.</param>
		/// <returns>The paths to the chosen files or null if the user canceled the dialog.</returns>
		public string[] ShowOpenFileDialog(string dialogTitle, string filter)
		{
			return this.ShowOpenFileDialog(dialogTitle, filter, null);
		}

		/// <summary>
		/// Shows the Visual Studio open file dialog.
		/// </summary>
		/// <param name="dialogTitle">The title for the dialog box.</param>
		/// <param name="filter">The filter for the dialog.</param>
		/// <param name="initialDirectory">The initial starting directory. Can be null to use the current directory.</param>
		/// <returns>An array of paths to the chosen files or an empty array if the user canceled the dialog.</returns>
		public string[] ShowOpenFileDialog(string dialogTitle, string filter, string initialDirectory)
		{
			ArrayList fileNames = new ArrayList();
			int bufferSize = NativeMethods.MAX_PATH;

			// Get the HWND to use for the modal file dialog.
			IntPtr hwnd;
			IVsUIShell uiShell = this.ServiceProvider.GetVsUIShell(classType, "ShowOpenFileDialog");
			NativeMethods.ThrowOnFailure(uiShell.GetDialogOwnerHwnd(out hwnd));

			// Create a native string buffer for the file name.
			IntPtr pwzFileName = Marshal.StringToHGlobalUni(new string('\0', bufferSize));

			try
			{
				// Fill in open file options structure.
				VSOPENFILENAMEW[] openFileOptions = new VSOPENFILENAMEW[1];
				openFileOptions[0].lStructSize = (uint)Marshal.SizeOf(typeof(VSOPENFILENAMEW));
				openFileOptions[0].hwndOwner = hwnd;
				openFileOptions[0].pwzDlgTitle = dialogTitle;
				openFileOptions[0].pwzFileName = pwzFileName;
				openFileOptions[0].nMaxFileName = (uint)bufferSize;
				openFileOptions[0].pwzFilter = filter;
				openFileOptions[0].pwzInitialDir = initialDirectory;
				openFileOptions[0].dwFlags = (uint)(VsOpenFileDialogFlags.AllowMultiSelect);

				// Open the Visual Studio open dialog.
				int hr = uiShell.GetOpenFileNameViaDlg(openFileOptions);
				bool canceled = (hr == NativeMethods.OLE_E_PROMPTSAVECANCELLED);
				if (NativeMethods.Failed(hr) && !canceled)
				{
					NativeMethods.ThrowOnFailure(hr);
				}

				// Get the file name(s).
				if (openFileOptions[0].pwzFileName != IntPtr.Zero && !canceled)
				{
					// We want to get the entire buffered string because if multiple files were selected then it has
					// the following format: directory\0file1\0file2\0...fileN\0\0. Note that it ends with two null
					// terminators.
					string rawDialogPath = Marshal.PtrToStringUni(openFileOptions[0].pwzFileName, bufferSize);

					// These will hold our currently parsed values.
					StringBuilder directory = new StringBuilder();
					StringBuilder fileName = new StringBuilder();
					bool parsingDirectory = true;

					// Walk over the raw string to pull out the directory and the file names.
					for (int i = 0; i < rawDialogPath.Length; i++)
					{
						char c = rawDialogPath[i];
						char nextC = (i + 1 < rawDialogPath.Length ? rawDialogPath[i + 1] : '\0');

						// If we've hit a null termination, then we have to stop parsing for a second and add an
						// item to our array.
						if (c != '\0')
						{
							if (parsingDirectory)
							{
								directory.Append(c);
							}
							else
							{
								fileName.Append(c);
							}
						}
						else
						{
							if (parsingDirectory)
							{
								parsingDirectory = false;
							}
							else
							{
								// We've seen another file, so let's add the absolute path to our array.
								string absolutePath = Path.Combine(directory.ToString(), fileName.ToString());
								absolutePath = PackageUtility.CanonicalizeFilePath(absolutePath);
								fileNames.Add(absolutePath);

								// Clear the file name StringBuilder for the next round.
								fileName.Length = 0;
							}

							// If we are at the double null termination then we can quit parsing.
							if (nextC == '\0')
							{
								// If the user only selected one file, then our parsed directory should be the full file name.
								if (fileNames.Count == 0)
								{
									fileNames.Add(directory.ToString());
								}
								break;
							}
						}
					}
				}
			}
			finally
			{
				// Release the string buffer.
				if (pwzFileName != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(pwzFileName);
				}
			}

			return (string[])fileNames.ToArray(typeof(string));
		}

		/// <summary>
		/// Gives subclasses a chance to create a new strongly-typed <see cref="ManagedResourceManager"/> instance.
		/// </summary>
		/// <returns>A new <see cref="ManagedResourceManager"/> instance.</returns>
		protected virtual ManagedResourceManager CreateManagedResourceManager()
		{
			return new ManagedResourceManager();
		}

		/// <summary>
		/// Gives subclasses a chance to create a new strongly-typed <see cref="PackageSettings"/> instance.
		/// </summary>
		/// <param name="serviceProvider">The <see cref="ServiceProvider"/> to use.</param>
		/// <returns>A new <see cref="PackageSettings"/> instance.</returns>
		protected virtual PackageSettings CreatePackageSettings(ServiceProvider serviceProvider)
		{
			return new PackageSettings(serviceProvider);
		}

		/// <summary>
		/// Cleans up managed and native resources.
		/// </summary>
		/// <param name="disposing">Indicates whether this is being called from the finalizer.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (this.serviceProvider != null)
				{
					this.serviceProvider.Dispose();
					this.serviceProvider = null;
				}
			}
		}
		#endregion
	}
}

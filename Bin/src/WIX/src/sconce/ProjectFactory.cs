//-------------------------------------------------------------------------------------------------
// <copyright file="ProjectFactory.cs" company="Microsoft">
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
// Integrates the custom WiX project into the Visual Studio environment.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Diagnostics;
	using System.IO;
	using System.Globalization;
	using System.Runtime.InteropServices;
	using Microsoft.VisualStudio.Shell.Interop;

	using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;

	/// <summary>
	/// Implements the IVsProjectFactory and IVsOwnedProjectFactory interfaces, which handle
	/// the creation of our custom projects.
	/// </summary>
	public class ProjectFactory : IVsProjectFactory, IVsOwnedProjectFactory
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(ProjectFactory);
		private Package parentPackage;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		///     Initializes a new instance of the <see cref="ProjectFactory"/> class.
		/// </summary>
		public ProjectFactory(Package parent)
		{
			Tracer.VerifyNonNullArgument(parent, "parent");
			this.parentPackage = parent;
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		#region IVsOwnedProjectFactory Members
		int IVsOwnedProjectFactory.InitializeForOwner(string pszFilename, string pszLocation, string pszName, uint grfCreateFlags, ref Guid iidProject, uint cookie, out System.IntPtr ppvProject, out int pfCanceled)
		{
			// TODO:  Add ProjectFactory.InitializeForOwner implementation
			ppvProject = IntPtr.Zero;
			pfCanceled = 0;
			return 0;
		}

		int IVsOwnedProjectFactory.PreCreateForOwner(object pUnkOwner, out object ppUnkInner, out uint pCookie)
		{
			// TODO:  Add ProjectFactory.PreCreateForOwner implementation
			ppUnkInner = null;
			pCookie = 0;
			return 0;
		}
		#endregion

		#region IVsProjectFactory Members
		int IVsProjectFactory.CanCreateProject(string pszFilename, uint grfCreateFlags, out int pfCanCreate)
		{
			// Right now we can always create a project. If we ever find a state where it's not
			// valid to create a new project, then implement this method.
			pfCanCreate = 1;
			return NativeMethods.S_OK;
		}

		int IVsProjectFactory.Close()
		{
			return NativeMethods.S_OK;
		}

		int IVsProjectFactory.CreateProject(string pszFilename, string pszLocation, string pszName, uint grfCreateFlags, ref Guid iidProject, out IntPtr ppvProject, out int pfCanceled)
		{
			IntPtr pUnk = IntPtr.Zero;
			pfCanceled = 0;
			ppvProject = IntPtr.Zero;

			bool loadedSuccessfully = false;

			try
			{
				Tracer.VerifyStringArgument(pszFilename, "pszFilename");

				__VSCREATEPROJFLAGS createFlags = (__VSCREATEPROJFLAGS)grfCreateFlags;

				// Get the right version of the project serializer.
				ProjectSerializer serializer = this.CreateSerializer(pszFilename);

				// Do we need to suppress any load failures from being reported to the end user.
				serializer.SilentFailures = ((createFlags & __VSCREATEPROJFLAGS.CPF_SILENT) == __VSCREATEPROJFLAGS.CPF_SILENT);

				// Now we need to load the project, either from a template file or from an existing file.
				bool openExisting = ((createFlags & __VSCREATEPROJFLAGS.CPF_OPENFILE) == __VSCREATEPROJFLAGS.CPF_OPENFILE);
				bool openFromTemplate = ((createFlags & __VSCREATEPROJFLAGS.CPF_CLONEFILE) == __VSCREATEPROJFLAGS.CPF_CLONEFILE);
				Tracer.Assert((openExisting && !openFromTemplate) || (!openExisting && openFromTemplate), "The grfCreateFlags are incorrect. You can't have both opening existing and opening from template. Flags={0}", createFlags);

				if (openExisting)
				{
					Tracer.WriteLineInformation(classType, "IVsProjectFactory.CreateProject", "Attempting to load project: File name={0} Location={1} Name={2} GUID={3}.", pszFilename, pszLocation, pszName, iidProject.ToString("B").ToUpper(CultureInfo.InvariantCulture));
					loadedSuccessfully = serializer.Load(pszFilename);

					if (loadedSuccessfully)
					{
						Tracer.WriteLineInformation(classType, "IVsProjectFactory.CreateProject", "Successfully loaded project '{0}'.", pszFilename);
					}
					else
					{
						Tracer.WriteLineInformation(classType, "IVsProjectFactory.CreateProject", "There were errors in loading project '{0}'.", pszFilename);
					}
				}
				else
				{
					Tracer.WriteLineInformation(classType, "IVsProjectFactory.CreateProject", "Attempting to create a new project from a template: File name={0} Location={1} Name={2} GUID={3}.", pszFilename, pszLocation, pszName, iidProject.ToString("B").ToUpper(CultureInfo.InvariantCulture));
					Tracer.VerifyStringArgument(pszLocation, "pszLocation");
					Tracer.VerifyStringArgument(pszName, "pszName");

					string destinationFile = Path.Combine(pszLocation, pszName);
					loadedSuccessfully = serializer.LoadFromTemplate(pszFilename, destinationFile);

					if (loadedSuccessfully)
					{
						Tracer.WriteLineInformation(classType, "IVsProjectFactory.CreateProject", "Successfully loaded project '{0}'.", pszFilename);
					}
					else
					{
						Tracer.WriteLineInformation(classType, "IVsProjectFactory.CreateProject", "There were errors in loading project '{0}'.", pszFilename);
					}
				}

				if (loadedSuccessfully)
				{
					// Once we've loaded the project, we need to return the COM object that the environment is requesting.
					pUnk = Marshal.GetIUnknownForObject(serializer.Project);
					int hr = Marshal.QueryInterface(pUnk, ref iidProject, out ppvProject);
					Tracer.Assert(NativeMethods.Succeeded(hr), "Cannot get the requested project interface ({0}): returned {1}", iidProject.ToString("B").ToUpper(CultureInfo.InvariantCulture), hr);
					NativeMethods.ThrowOnFailure(hr);
				}
			}
			catch (Exception e)
			{
				Package.Instance.Context.NotifyInternalError(e.ToString());
			}
			finally
			{
				if (pUnk != IntPtr.Zero)
				{
					Marshal.Release(pUnk);
				}
			}

			return (loadedSuccessfully ? NativeMethods.S_OK : NativeMethods.E_FAIL);
		}

		int IVsProjectFactory.SetSite(IOleServiceProvider psp)
		{
			return NativeMethods.S_OK;
		}
		#endregion

		/// <summary>
		/// Creates a new <see cref="ProjectSerializer"/> to use for deserializing the specified project file.
		/// </summary>
		/// <param name="filename">The path to the file to deserialize.</param>
		/// <returns>A <see cref="ProjectSerializer"/> object used for deserializing the specified project file.</returns>
		protected virtual ProjectSerializer CreateSerializer(string filename)
		{
			return new ProjectSerializer();
		}

		private object GetService(Type serviceType)
		{
			Tracer.Assert(this.parentPackage != null, "Parent package is null.");
			object service = parentPackage.GetService(serviceType);
			return service;
		}
		#endregion
	}
}

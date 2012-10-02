//-------------------------------------------------------------------------------------------------
// <copyright file="ProjectConfiguration.cs" company="Microsoft">
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
// Contains the ProjectConfiguration class.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Globalization;
	using System.IO;
	using Microsoft.VisualStudio.OLE.Interop;
	using Microsoft.VisualStudio.Shell.Interop;

	/// <summary>
	/// Provides configuration information to the Visual Studio shell about a project.
	/// </summary>
	public class ProjectConfiguration : DirtyableObject, ICloneable, IComparable, IVsCfg, IVsProjectCfg, ISpecifyPropertyPages
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(ProjectConfiguration);

		private BuildableProjectConfiguration buildableProjectConfiguration;
		private Project project;
		private string name;
		private string relativeIntermediateDirectory;
		private string relativeOutputDirectory;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		public ProjectConfiguration(Project project, string name)
		{
			Tracer.VerifyNonNullArgument(project, "project");
			Tracer.VerifyStringArgument(name, "name");
			this.project = project;
			this.name = name;
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		public BuildableProjectConfiguration BuildableProjectConfiguration
		{
			get
			{
				if (this.buildableProjectConfiguration == null)
				{
					this.buildableProjectConfiguration = this.CreateBuildableProjectConfiguration();
				}
				return this.buildableProjectConfiguration;
			}
		}

		public string Name
		{
			get { return this.name; }
		}

		public Project Project
		{
			get { return this.project; }
			set
			{
				Tracer.VerifyNonNullArgument(value, "Project");
				if (this.Project != value)
				{
					this.project = value;
					this.UpdateOutputFiles();
					this.MakeDirty();
				}
			}
		}

		/// <summary>
		/// Gets or sets the directory relative to the project file where intermediate build files will be placed.
		/// </summary>
		public string RelativeIntermediateDirectory
		{
			get
			{
				if (!String.IsNullOrEmpty(this.relativeIntermediateDirectory))
				{
					return this.relativeIntermediateDirectory;
				}

				return this.RelativeOutputDirectory;
			}

			set
			{
				Tracer.VerifyStringArgument(value, "RelativeIntermediateDirectory");
				if (this.RelativeIntermediateDirectory != value)
				{
					this.relativeIntermediateDirectory = value;
					this.UpdateOutputFiles();
					this.MakeDirty();
				}
			}
		}

		/// <summary>
		/// Gets or sets the directory relative to the project file where output files will be built.
		/// </summary>
		public string RelativeOutputDirectory
		{
			get { return this.relativeOutputDirectory; }
			set
			{
				Tracer.VerifyStringArgument(value, "RelativeOutputDirectory");
				if (this.RelativeOutputDirectory != value)
				{
					this.relativeOutputDirectory = value;
					this.UpdateOutputFiles();
					this.MakeDirty();
				}
			}
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		public virtual ProjectConfiguration Clone()
		{
			return this.Clone(this.Name);
		}

		public virtual ProjectConfiguration Clone(string newName)
		{
			// We don't want to call the constructor here because we may actually be a subclass
			// instead of a pure ProjectConfiguration.
			ProjectConfiguration clonedCopy = (ProjectConfiguration)this.MemberwiseClone();

			// These two member are usually set in the constructor so we'll set them here to
			// simulate the constructor.
			clonedCopy.project = this.Project;
			clonedCopy.name = newName;

			// Finish the cloning operation.
			this.CloneInto(clonedCopy);

			return clonedCopy;
		}

		public override string ToString()
		{
			return this.Name;
		}

		#region ICloneable Members
		object ICloneable.Clone()
		{
			return this.Clone();
		}
		#endregion

		#region IComparable Members
		int IComparable.CompareTo(object obj)
		{
			if (!(obj is ProjectConfiguration))
			{
				throw new ArgumentException(PackageUtility.SafeStringFormatInvariant("obj must be of type '{0}'.", this.GetType().Name));
			}

			if (obj == null)
			{
				return 1;
			}

			return String.Compare(this.Name, ((ProjectConfiguration)obj).Name, StringComparison.CurrentCultureIgnoreCase);
		}
		#endregion

		#region IVsCfg Members
		int IVsCfg.get_DisplayName(out string pbstrDisplayName)
		{
			return ((IVsProjectCfg)this).get_DisplayName(out pbstrDisplayName);
		}

		int IVsCfg.get_IsDebugOnly(out int pfIsDebugOnly)
		{
			return ((IVsProjectCfg)this).get_IsDebugOnly(out pfIsDebugOnly);
		}

		int IVsCfg.get_IsReleaseOnly(out int pfIsReleaseOnly)
		{
			return ((IVsProjectCfg)this).get_IsReleaseOnly(out pfIsReleaseOnly);
		}
		#endregion

		#region IVsProjectCfg Members
		int IVsProjectCfg.EnumOutputs(out IVsEnumOutputs ppIVsEnumOutputs)
		{
			// The documentation says this is obsolete, so don't do anything.
			ppIVsEnumOutputs = null;
			return NativeMethods.E_NOTIMPL;
		}

		int IVsProjectCfg.get_BuildableProjectCfg(out IVsBuildableProjectCfg ppIVsBuildableProjectCfg)
		{
			ppIVsBuildableProjectCfg = this.BuildableProjectConfiguration;
			return NativeMethods.S_OK;
		}

		int IVsProjectCfg.get_CanonicalName(out string pbstrCanonicalName)
		{
			pbstrCanonicalName = this.Name;
			return NativeMethods.S_OK;
		}

		int IVsProjectCfg.get_DisplayName(out string pbstrDisplayName)
		{
			pbstrDisplayName = this.Name;
			return NativeMethods.S_OK;
		}

		int IVsProjectCfg.get_IsDebugOnly(out int pfIsDebugOnly)
		{
			// The documentation says this is obsolete, so don't do anything.
			pfIsDebugOnly = 0;
			return NativeMethods.E_NOTIMPL;
		}

		int IVsProjectCfg.get_IsPackaged(out int pfIsPackaged)
		{
			// The documentation says this is obsolete, so don't do anything.
			pfIsPackaged = 0;
			return NativeMethods.E_NOTIMPL;
		}

		int IVsProjectCfg.get_IsReleaseOnly(out int pfIsReleaseOnly)
		{
			// The documentation says this is obsolete, so don't do anything.
			pfIsReleaseOnly = 0;
			return NativeMethods.E_NOTIMPL;
		}

		int IVsProjectCfg.get_IsSpecifyingOutputSupported(out int pfIsSpecifyingOutputSupported)
		{
			// The documentation says this is obsolete, so don't do anything.
			pfIsSpecifyingOutputSupported = 0;
			return NativeMethods.E_NOTIMPL;
		}

		int IVsProjectCfg.get_Platform(out Guid pguidPlatform)
		{
			// The documentation says this is obsolete, so don't do anything.
			pguidPlatform = Guid.Empty;
			return NativeMethods.E_NOTIMPL;
		}

		int IVsProjectCfg.get_ProjectCfgProvider(out IVsProjectCfgProvider ppIVsProjectCfgProvider)
		{
			// The documentation says this is obsolete, so don't do anything.
			ppIVsProjectCfgProvider = null;
			return NativeMethods.E_NOTIMPL;
		}

		int IVsProjectCfg.get_RootURL(out string pbstrRootURL)
		{
			// The documentation says to specify the url with the prefix file:///
			pbstrRootURL = "file:///" + Path.Combine(this.Project.RootDirectory, this.RelativeOutputDirectory);
			return NativeMethods.S_OK;
		}

		int IVsProjectCfg.get_TargetCodePage(out uint puiTargetCodePage)
		{
			// The documentation says this is obsolete, so don't do anything.
			puiTargetCodePage = 0;
			return NativeMethods.E_NOTIMPL;
		}

		int IVsProjectCfg.get_UpdateSequenceNumber(ULARGE_INTEGER[] puliUSN)
		{
			// The documentation says this is obsolete, so don't do anything.
			if (puliUSN != null)
			{
				puliUSN[0] = new ULARGE_INTEGER();
				puliUSN[0].QuadPart = 0;
			}
			return NativeMethods.E_NOTIMPL;
		}

		int IVsProjectCfg.OpenOutput(string szOutputCanonicalName, out IVsOutput ppIVsOutput)
		{
			// The documentation says this is obsolete, so don't do anything.
			ppIVsOutput = null;
			return NativeMethods.E_NOTIMPL;
		}

		#endregion

		#region ISpecifyPropertyPages Members
		void ISpecifyPropertyPages.GetPages(CAUUID[] pPages)
		{
			pPages[0] = new CAUUID();
			pPages[0].cElems = 0;
		}
		#endregion

		/// <summary>
		/// Provides a way for subclasses to pass in an already-created, more specific <see cref="ProjectConfiguration"/> to clone.
		/// </summary>
		/// <param name="clonedCopy">The <see cref="ProjectConfiguration"/> to clone into.</param>
		protected virtual void CloneInto(ProjectConfiguration clonedCopy)
		{
			clonedCopy.buildableProjectConfiguration = null;
			clonedCopy.relativeOutputDirectory = this.relativeOutputDirectory;

			if (this.IsDirty)
			{
				clonedCopy.MakeDirty();
			}
			else
			{
				clonedCopy.ClearDirty();
			}
		}

		/// <summary>
		/// Gives subclasses the ability to create a type-specific <see cref="BuildableProjectConfiguration"/> object that will be stored in this object.
		/// </summary>
		/// <returns></returns>
		protected virtual BuildableProjectConfiguration CreateBuildableProjectConfiguration()
		{
			return new BuildableProjectConfiguration(this);
		}

		/// <summary>
		/// Gives the subclass a chance to update its output file paths when the relative file path has changed.
		/// </summary>
		protected virtual void UpdateOutputFiles()
		{
		}
		#endregion
	}
}

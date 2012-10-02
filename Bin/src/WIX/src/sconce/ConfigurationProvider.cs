//-------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationProvider.cs" company="Microsoft">
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
// Contains the ConfigurationProvider class.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Collections;
	using System.Collections.Specialized;
	using System.ComponentModel;
	using System.Globalization;
	using Microsoft.VisualStudio.Shell.Interop;

	/// <summary>
	/// Provides configuration information to the Visual Studio shell.
	/// </summary>
	public sealed class ConfigurationProvider : DirtyableObject, ICloneable, IVsCfgProvider, IVsCfgProvider2, IVsProjectCfgProvider
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(ConfigurationProvider);

		private Project project;
		private ProjectConfigurationCollection projectConfigurations;
		private VsCfgProviderEventListenerCollection eventListeners = new VsCfgProviderEventListenerCollection();
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		public ConfigurationProvider(Project project)
		{
			Tracer.VerifyNonNullArgument(project, "project");
			this.project = project;
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		public Project Project
		{
			get { return this.project; }
		}

		public ProjectConfigurationCollection ProjectConfigurations
		{
			get
			{
				if (this.projectConfigurations == null)
				{
					this.projectConfigurations = new ProjectConfigurationCollection(this.Project);

					// Add ourself as a listener to the project configuration collection.
					this.projectConfigurations.CollectionChanged += new CollectionChangeEventHandler(this.ConfigCollectionChanged);
				}
				return this.projectConfigurations;
			}
		}

		/// <summary>
		/// Returns a value indicating whether one or more contained <see cref="IDirtyable"/> objects
		/// are dirty.
		/// </summary>
		protected override bool AreContainedObjectsDirty
		{
			get { return this.ProjectConfigurations.IsDirty; }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Creates a deep copy of this object.
		/// </summary>
		/// <returns>A deep copy of this object.</returns>
		public ConfigurationProvider Clone()
		{
			return this.Clone(this.Project);
		}

		/// <summary>
		/// Creates a deep copy of this object with the new project instead of the cloned project.
		/// </summary>
		/// <returns>A deep copy of this object.</returns>
		public ConfigurationProvider Clone(Project newProject)
		{
			ConfigurationProvider clone = new ConfigurationProvider(newProject);

			// Clone the configurations collection.
			clone.projectConfigurations = (ProjectConfigurationCollection)this.ProjectConfigurations.Clone();
			clone.projectConfigurations.CollectionChanged += new CollectionChangeEventHandler(clone.ConfigCollectionChanged);
			foreach (ProjectConfiguration config in clone.projectConfigurations)
			{
				config.Project = newProject;
			}

			// Clone the listeners collection.
			clone.eventListeners = (VsCfgProviderEventListenerCollection)this.eventListeners.Clone();

			return clone;
		}

		#region ICloneable Members
		object ICloneable.Clone()
		{
			return this.Clone();
		}
		#endregion

		#region IVsCfgProvider Members
		int IVsCfgProvider.GetCfgs(uint celt, IVsCfg[] rgpcfg, uint[] pcActual, uint[] prgfFlags)
		{
			return ((IVsCfgProvider2)this).GetCfgs(celt, rgpcfg, pcActual, prgfFlags);
		}
		#endregion

		#region IVsCfgProvider2 Members
		int IVsCfgProvider2.AddCfgsOfCfgName(string pszCfgName, string pszCloneCfgName, int fPrivate)
		{
			Tracer.VerifyStringArgument(pszCfgName, "pszCfgName");

			// If we need to clone, then get the configurtaions to clone.
			if (pszCloneCfgName != null && pszCloneCfgName.Length > 0 && this.ProjectConfigurations.Contains(pszCloneCfgName))
			{
				ProjectConfiguration source = this.ProjectConfigurations[pszCloneCfgName];
				ProjectConfiguration clonedConfig = source.Clone(pszCfgName);
				this.ProjectConfigurations.Add(clonedConfig);
			}
			else
			{
				// Create a new configuration, since there was nothing to clone.
				ProjectConfiguration newConfig = new ProjectConfiguration(this.Project, pszCfgName);
				this.ProjectConfigurations.Add(newConfig);
			}

			return NativeMethods.S_OK;
		}

		int IVsCfgProvider2.AddCfgsOfPlatformName(string pszPlatformName, string pszClonePlatformName)
		{
			return NativeMethods.E_NOTIMPL;
		}

		int IVsCfgProvider2.AdviseCfgProviderEvents(IVsCfgProviderEvents pCPE, out uint pdwCookie)
		{
			pdwCookie = this.eventListeners.Add(pCPE);
			return NativeMethods.S_OK;
		}

		int IVsCfgProvider2.DeleteCfgsOfCfgName(string pszCfgName)
		{
			Tracer.VerifyStringArgument(pszCfgName, "pszCfgName");
			if (this.ProjectConfigurations.Contains(pszCfgName))
			{
				this.ProjectConfigurations.Remove(pszCfgName);
			}
			return NativeMethods.S_OK;
		}

		int IVsCfgProvider2.DeleteCfgsOfPlatformName(string pszPlatformName)
		{
			return NativeMethods.E_NOTIMPL;
		}

		int IVsCfgProvider2.GetCfgNames(uint celt, string[] rgbstr, uint[] pcActual)
		{
			if (celt == 0)
			{
				// When celt (the count of elements) is zero, then the caller is requesting the
				// total size of the array, which will be returned in pcActual.
				pcActual[0] = (uint)this.ProjectConfigurations.Count;
			}
			else
			{
				// If celt is non-zero, but the array is null, then we've got a problem.
				Tracer.VerifyNonNullArgument(rgbstr, "rgbstr");

				// celt could very well be larger than our array, so we have to stop looping
				// when celt or Count is reached, whichever is lower.
				int totalToRetrieve = Math.Min((int)celt, this.ProjectConfigurations.Count);
				for (int i = 0; i < totalToRetrieve; i++)
				{
					rgbstr[i] = this.ProjectConfigurations[i].Name;
				}
				pcActual[0] = (uint)totalToRetrieve;
			}

			return NativeMethods.S_OK;
		}

		int IVsCfgProvider2.GetCfgOfName(string pszCfgName, string pszPlatformName, out IVsCfg ppCfg)
		{
			Tracer.Assert(pszPlatformName == null || pszPlatformName.Length == 0, "We don't support platforms, so the environment shouldn't be sending us a platform name.");
			ppCfg = this.ProjectConfigurations[pszCfgName];

			// If we didn't find a configuration return DISP_E_MEMBERNOTFOUND.
			return (ppCfg != null ? NativeMethods.S_OK : NativeMethods.DISP_E_MEMBERNOTFOUND);
		}

		int IVsCfgProvider2.GetCfgProviderProperty(int propid, out object pvar)
		{
			pvar = null;

			__VSCFGPROPID vsPropId = (__VSCFGPROPID)propid;
			switch (vsPropId)
			{
				case __VSCFGPROPID.VSCFGPROPID_SupportsCfgAdd:
				case __VSCFGPROPID.VSCFGPROPID_SupportsCfgDelete:
				case __VSCFGPROPID.VSCFGPROPID_SupportsCfgRename:
					pvar = true;
					break;

				case __VSCFGPROPID.VSCFGPROPID_SupportsPlatformAdd:
				case __VSCFGPROPID.VSCFGPROPID_SupportsPlatformDelete:
					pvar = false;
					break;
			}

			Tracer.WriteLineVerbose(classType, "IVsCfgProvider2.GetCfgProviderProperty", "Requested property '{0}' is {1}.", vsPropId, pvar);
			return NativeMethods.S_OK;
		}

		int IVsCfgProvider2.GetCfgs(uint celt, IVsCfg[] rgpcfg, uint[] pcActual, uint[] prgfFlags)
		{
			// prgfFlags is ignored because according to the documentation, the flags are currently not used.

			if (celt == 0)
			{
				// When celt (the count of elements) is zero, then the caller is requesting the
				// total size of the array, which will be returned in pcActual.
				pcActual[0] = (uint)this.ProjectConfigurations.Count;
			}
			else
			{
				// If celt is non-zero, but the array is null, then we've got a problem.
				Tracer.VerifyNonNullArgument(rgpcfg, "rgpcfg");

				// celt could very well be larger than our array, so we have to stop looping
				// when celt or Count is reached, whichever is lower.
				int totalToRetrieve = Math.Min((int)celt, this.ProjectConfigurations.Count);
				for (int i = 0; i < totalToRetrieve; i++)
				{
					rgpcfg[i] = (IVsCfg)this.ProjectConfigurations[i];
				}
				pcActual[0] = (uint)totalToRetrieve;
			}

			return NativeMethods.S_OK;
		}

		int IVsCfgProvider2.GetPlatformNames(uint celt, string[] rgbstr, uint[] pcActual)
		{
			pcActual[0] = 0;
			return NativeMethods.S_OK;
		}

		int IVsCfgProvider2.GetSupportedPlatformNames(uint celt, string[] rgbstr, uint[] pcActual)
		{
			pcActual[0] = 0;
			return NativeMethods.S_OK;
		}

		int IVsCfgProvider2.RenameCfgsOfCfgName(string pszOldName, string pszNewName)
		{
			// TODO:  Add ConfigurationProvider.RenameCfgsOfCfgName implementation
			return 0;
		}

		int IVsCfgProvider2.UnadviseCfgProviderEvents(uint dwCookie)
		{
			this.eventListeners.Remove(dwCookie);
			return NativeMethods.S_OK;
		}
		#endregion

		#region IVsProjectCfgProvider Members
		int IVsProjectCfgProvider.get_UsesIndependentConfigurations(out int pfUsesIndependentConfigurations)
		{
			// The documentation says this is obsolete, so don't do anything.
			pfUsesIndependentConfigurations = 0;
			return NativeMethods.E_NOTIMPL;
		}

		int IVsProjectCfgProvider.GetCfgs(uint celt, IVsCfg[] rgpcfg, uint[] pcActual, uint[] prgfFlags)
		{
			return ((IVsCfgProvider2)this).GetCfgs(celt, rgpcfg, pcActual, prgfFlags);
		}

		int IVsProjectCfgProvider.OpenProjectCfg(string szProjectCfgCanonicalName, out IVsProjectCfg ppIVsProjectCfg)
		{
			// TODO:  Add ConfigurationProvider.OpenProjectCfg implementation
			ppIVsProjectCfg = null;
			return 0;
		}
		#endregion

		/// <summary>
		/// Clears the dirty flag for any contained <see cref="IDirtyable"/> objects.
		/// </summary>
		protected override void ClearDirtyOnContainedObjects()
		{
			this.ProjectConfigurations.ClearDirty();
		}

		private void ConfigCollectionChanged(object sender, CollectionChangeEventArgs e)
		{
			ProjectConfiguration config = (ProjectConfiguration)e.Element;
			if (e.Action == CollectionChangeAction.Add)
			{
				this.eventListeners.OnCfgNameAdded(config.Name);
			}
			else if (e.Action == CollectionChangeAction.Remove)
			{
				this.eventListeners.OnCfgNameDeleted(config.Name);
			}
		}
		#endregion
	}
}

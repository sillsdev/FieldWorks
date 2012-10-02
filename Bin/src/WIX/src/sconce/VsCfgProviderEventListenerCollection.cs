//-------------------------------------------------------------------------------------------------
// <copyright file="VsCfgProviderEventListenerCollection.cs" company="Microsoft">
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
// Collection class for IVsHierarchyEvents event listeners.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Collections;

	using Microsoft.VisualStudio.Shell.Interop;

	public sealed class VsCfgProviderEventListenerCollection : EventListenerCollection, IVsCfgProviderEventsHelper
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(VsCfgProviderEventListenerCollection);
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		public VsCfgProviderEventListenerCollection()
		{
		}
		#endregion

		#region Indexers
		//==========================================================================================
		// Indexers
		//==========================================================================================

		public IVsCfgProviderEvents this[int index]
		{
			get { return (IVsCfgProviderEvents)this.GetAt(index); }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		public uint Add(IVsCfgProviderEvents listener)
		{
			return base.Add(listener);
		}

		/// <summary>
		/// Clones this object by performing a shallow copy of the collection items.
		/// </summary>
		/// <returns>A shallow copy of this object.</returns>
		public override object Clone()
		{
			VsCfgProviderEventListenerCollection clone = new VsCfgProviderEventListenerCollection();
			this.CloneInto(clone);
			return clone;
		}

		/// <summary>
		/// Notifies all of the listeners that a configuration name has been added.
		/// </summary>
		/// <param name="name">The name of the newly added configuration.</param>
		public void OnCfgNameAdded(string name)
		{
			Tracer.VerifyStringArgument(name, "name");

			// Let all of our listeners know that the hierarchy needs to be refreshed.
			Tracer.WriteLine(classType, "OnCfgNameAdded", Tracer.Level.Information, "Notifying all of our listeners that the configuration '{0}' has been added.", name);

			// There are some cases where the collection is changed while we're iterating it.
			// To be safe, we'll create a copy of the collection and iterate over that.
			// We just want a shallow copy, though, and not a deep (Clone) copy.
			ArrayList clone = new ArrayList(this.Values);
			foreach (IVsCfgProviderEvents eventItem in clone)
			{
				try
				{
					eventItem.OnCfgNameAdded(name);
				}
				catch (Exception e)
				{
					Tracer.WriteLine(classType, "OnCfgNameAdded", Tracer.Level.Warning, "There was an exception in one of the listener's event handling code: {0}", e.ToString());
				}
			}
		}

		/// <summary>
		/// Notifies all of the listeners that a configuration name has been deleted.
		/// </summary>
		/// <param name="name">The name of the newly deleted configuration.</param>
		public void OnCfgNameDeleted(string name)
		{
			Tracer.VerifyStringArgument(name, "name");

			// Let all of our listeners know that the hierarchy needs to be refreshed.
			Tracer.WriteLine(classType, "OnCfgNameDeleted", Tracer.Level.Information, "Notifying all of our listeners that the configuration '{0}' has been deleted.", name);

			// There are some cases where the collection is changed while we're iterating it.
			// To be safe, we'll create a copy of the collection and iterate over that.
			// We just want a shallow copy, though, and not a deep (Clone) copy.
			ArrayList clone = new ArrayList(this.Values);
			foreach (IVsCfgProviderEvents eventItem in clone)
			{
				try
				{
					eventItem.OnCfgNameDeleted(name);
				}
				catch (Exception e)
				{
					Tracer.WriteLine(classType, "OnCfgNameDeleted", Tracer.Level.Warning, "There was an exception in one of the listener's event handling code: {0}", e.ToString());
				}
			}
		}

		/// <summary>
		/// Notifies all of the listeners that a configuration name has been changed.
		/// </summary>
		/// <param name="oldName">The old name of the configuration that was changed.</param>
		/// <param name="newName">The new name of the configuration that was changed.</param>
		public void OnCfgNameRenamed(string oldName, string newName)
		{
			Tracer.VerifyStringArgument(oldName, "oldName");
			Tracer.VerifyStringArgument(newName, "newName");

			// Let all of our listeners know that the hierarchy needs to be refreshed.
			Tracer.WriteLine(classType, "OnCfgNameRenamed", Tracer.Level.Information, "Notifying all of our listeners that the configuration '{0}' has been renamed to '{1}'.", oldName, newName);

			// There are some cases where the collection is changed while we're iterating it.
			// To be safe, we'll create a copy of the collection and iterate over that.
			// We just want a shallow copy, though, and not a deep (Clone) copy.
			ArrayList clone = new ArrayList(this.Values);
			foreach (IVsCfgProviderEvents eventItem in clone)
			{
				try
				{
					eventItem.OnCfgNameRenamed(oldName, newName);
				}
				catch (Exception e)
				{
					Tracer.WriteLine(classType, "OnCfgNameRenamed", Tracer.Level.Warning, "There was an exception in one of the listener's event handling code: {0}", e.ToString());
				}
			}
		}

		/// <summary>
		/// Notifies all of the listeners that a platform name has been added.
		/// </summary>
		/// <param name="platformName">The name of the newly added platform.</param>
		public void OnPlatformNameAdded(string platformName)
		{
			Tracer.VerifyStringArgument(platformName, "platformName");

			// Let all of our listeners know that the hierarchy needs to be refreshed.
			Tracer.WriteLine(classType, "OnPlatformNameAdded", Tracer.Level.Information, "Notifying all of our listeners that the platform '{0}' has been added.", platformName);

			// There are some cases where the collection is changed while we're iterating it.
			// To be safe, we'll create a copy of the collection and iterate over that.
			// We just want a shallow copy, though, and not a deep (Clone) copy.
			ArrayList clone = new ArrayList(this.Values);
			foreach (IVsCfgProviderEvents eventItem in clone)
			{
				try
				{
					eventItem.OnPlatformNameAdded(platformName);
				}
				catch (Exception e)
				{
					Tracer.WriteLine(classType, "OnPlatformNameAdded", Tracer.Level.Warning, "There was an exception in one of the listener's event handling code: {0}", e.ToString());
				}
			}
		}

		/// <summary>
		/// Notifies all of the listeners that a platform name has been deleted.
		/// </summary>
		/// <param name="platformName">The name of the newly deleted platform.</param>
		public void OnPlatformNameDeleted(string platformName)
		{
			Tracer.VerifyStringArgument(platformName, "platformName");

			// Let all of our listeners know that the hierarchy needs to be refreshed.
			Tracer.WriteLine(classType, "OnPlatformNameDeleted", Tracer.Level.Information, "Notifying all of our listeners that the platform '{0}' has been deleted.", platformName);

			// There are some cases where the collection is changed while we're iterating it.
			// To be safe, we'll create a copy of the collection and iterate over that.
			// We just want a shallow copy, though, and not a deep (Clone) copy.
			ArrayList clone = new ArrayList(this.Values);
			foreach (IVsCfgProviderEvents eventItem in clone)
			{
				try
				{
					eventItem.OnPlatformNameDeleted(platformName);
				}
				catch (Exception e)
				{
					Tracer.WriteLine(classType, "OnPlatformNameDeleted", Tracer.Level.Warning, "There was an exception in one of the listener's event handling code: {0}", e.ToString());
				}
			}
		}
		#endregion

		#region IVsCfgProviderEventsHelper Members
		int IVsCfgProviderEventsHelper.AdviseCfgProviderEvents(IVsCfgProviderEvents pCPE, out uint pdwCookie)
		{
			pdwCookie = this.Add(pCPE);
			return NativeMethods.S_OK;
		}

		int IVsCfgProviderEventsHelper.NotifyOnCfgNameAdded(string pszCfgName)
		{
			this.OnCfgNameAdded(pszCfgName);
			return NativeMethods.S_OK;
		}

		int IVsCfgProviderEventsHelper.NotifyOnCfgNameDeleted(string pszCfgName)
		{
			this.OnCfgNameDeleted(pszCfgName);
			return NativeMethods.S_OK;
		}

		int IVsCfgProviderEventsHelper.NotifyOnCfgNameRenamed(string pszOldName, string lszNewName)
		{
			this.OnCfgNameRenamed(pszOldName, lszNewName);
			return NativeMethods.S_OK;
		}

		int IVsCfgProviderEventsHelper.NotifyOnPlatformNameAdded(string pszPlatformName)
		{
			this.OnPlatformNameAdded(pszPlatformName);
			return NativeMethods.S_OK;
		}

		int IVsCfgProviderEventsHelper.NotifyOnPlatformNameDeleted(string pszPlatformName)
		{
			this.OnPlatformNameDeleted(pszPlatformName);
			return NativeMethods.S_OK;
		}

		int IVsCfgProviderEventsHelper.UnadviseCfgProviderEvents(uint dwCookie)
		{
			this.Remove(dwCookie);
			return NativeMethods.S_OK;
		}
		#endregion
	}
}
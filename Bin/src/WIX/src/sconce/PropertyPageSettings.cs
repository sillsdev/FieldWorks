//-------------------------------------------------------------------------------------------------
// <copyright file="PropertyPageSettings.cs" company="Microsoft">
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
// Contains the PropertyPageSettings class.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Collections;
	using System.ComponentModel;
	using System.Globalization;

	/// <summary>
	/// Provides configuration information to the Visual Studio shell about a project.
	/// </summary>
	public abstract class PropertyPageSettings : PropertyGridTypeDescriptor, IDirtyable, ICloneable
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(PropertyPageSettings);

		private bool isDirty;
#if USE_NET20_FRAMEWORK
		private Hashtable dirtyPropertyNames = new Hashtable(StringComparer.OrdinalIgnoreCase);
#else
		private Hashtable dirtyPropertyNames = new Hashtable(new CaseInsensitiveHashCodeProvider(CultureInfo.InvariantCulture), new CaseInsensitiveComparer(CultureInfo.InvariantCulture));
#endif
		private PropertyPage ownerPage;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="PropertyPageSettings"/> class.
		/// </summary>
		protected PropertyPageSettings()
		{
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Returns a value indicating whether this object is in a dirty state or if any of the
		/// contained <see cref="IDirtyable"/> objects are in a dirty state.
		/// </summary>
		[Browsable(false)]
		public bool IsDirty
		{
			get
			{
				bool dirty = this.isDirty;

				if (!dirty)
				{
					dirty = this.AreContainedObjectsDirty;
				}

				return dirty;
			}
		}

		/// <summary>
		/// Returns a value indicating whether one or more contained <see cref="IDirtyable"/> objects
		/// are dirty.
		/// </summary>
		protected virtual bool AreContainedObjectsDirty
		{
			get { return false; }
		}
		#endregion

		#region Events
		//==========================================================================================
		// Events
		//==========================================================================================

		/// <summary>
		/// Raised when the dirty state has changed.
		/// </summary>
		[Browsable(false)]
		public event EventHandler DirtyStateChanged;

		/// <summary>
		/// Raised whenever a property changes.
		/// </summary>
		[Browsable(false)]
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Clears the dirty flag for the implementing object and any <see cref="IDirtyable"/>
		/// objects that it contains.
		/// </summary>
		public void ClearDirty()
		{
			this.isDirty = false;
			this.dirtyPropertyNames.Clear();
			this.ClearDirtyOnContainedObjects();
			this.OnDirtyStateChanged(EventArgs.Empty);
		}

		/// <summary>
		/// Returns a deep copy of this instance.
		/// </summary>
		/// <returns>A deep copy of this instance.</returns>
		public virtual object Clone()
		{
			return this.MemberwiseClone();
		}

		/// <summary>
		/// Returns the name that is displayed in the right hand side of the Properties window drop-down combo box.
		/// </summary>
		/// <returns>The class name of the object, or null if the class does not have a name.</returns>
		public override string GetClassName()
		{
			return SconceStrings.ProjectPropertiesClassName;
		}

		/// <summary>
		/// Returns the name that is displayed in the left hand side of the Properties window drop-down combo box.
		/// </summary>
		/// <returns>The name of the object, or null if the class does not have a name.</returns>
		public override string GetComponentName()
		{
			if (this.ownerPage != null)
			{
				return this.ownerPage.Project.RootNode.Caption;
			}
			return SconceStrings.ProjectPropertiesProjectFile;
		}

		/// <summary>
		/// Returns a value indicating whether the specified property is dirty.
		/// </summary>
		/// <param name="propertyName">The name of the property for which to query the dirty status.</param>
		/// <returns>true if the property is dirty; otherwise, false.</returns>
		public bool IsPropertyDirty(string propertyName)
		{
			return this.dirtyPropertyNames.ContainsKey(propertyName);
		}

		/// <summary>
		/// Attaches the settings to a property page.
		/// </summary>
		/// <param name="page">The page to own the settings.</param>
		internal void AttachToPropertyPage(PropertyPage page)
		{
			this.ownerPage = page;
		}

		/// <summary>
		/// Detaches from the current property page by setting the reference to null.
		/// </summary>
		internal void DetachPropertyPage()
		{
			this.ownerPage = null;
			this.dirtyPropertyNames.Clear();
		}

		/// <summary>
		/// Sets the dirty flag for just this object and not any contained <see cref="IDirtyable"/> objects.
		/// </summary>
		protected void MakeDirty()
		{
			this.isDirty = true;
			this.OnDirtyStateChanged(EventArgs.Empty);
		}

		/// <summary>
		/// Clears the dirty flag for any contained <see cref="IDirtyable"/> objects.
		/// </summary>
		protected virtual void ClearDirtyOnContainedObjects()
		{
		}

		/// <summary>
		/// Raises the <see cref="DirtyStateChanged"/> event.
		/// </summary>
		/// <param name="e">The <see cref="EventArgs"/> object that contains the event data.</param>
		protected virtual void OnDirtyStateChanged(EventArgs e)
		{
			if (this.DirtyStateChanged != null)
			{
				this.DirtyStateChanged(this, e);
			}
		}

		/// <summary>
		/// Raises the <see cref="PropertyChanged"/> event and marks the dirty flag.
		/// </summary>
		/// <param name="e">The <see cref="PropertyChangedEventArgs"/> object that contains the event data.</param>
		protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			// Add the property name to our dirty property name dictionary
			if (!this.dirtyPropertyNames.ContainsKey(e.PropertyName))
			{
				this.dirtyPropertyNames.Add(e.PropertyName, e.PropertyName);
			}

			this.MakeDirty();

			// Raise the event
			if (this.PropertyChanged != null)
			{
				this.PropertyChanged(this, e);
			}
		}
		#endregion
	}
}

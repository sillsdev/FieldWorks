//-------------------------------------------------------------------------------------------------
// <copyright file="DirtyableObject.cs" company="Microsoft">
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
// Contains the DirtyableObject class.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.ComponentModel;

	/// <summary>
	/// Base class useful for implementing the <see cref="IDirtyable"/> interface.
	/// </summary>
	public class DirtyableObject : IDirtyable
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private bool isDirty;
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
			this.ClearDirtyOnContainedObjects();
			this.OnDirtyStateChanged(EventArgs.Empty);
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
		#endregion
	}
}
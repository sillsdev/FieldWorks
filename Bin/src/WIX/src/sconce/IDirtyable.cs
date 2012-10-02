//-------------------------------------------------------------------------------------------------
// <copyright file="IDirtyable.cs" company="Microsoft">
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
// Interface for indicating that an object can be in a dirty state.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;

	/// <summary>
	/// Interface for indicating that an object can be in a dirty/clean state.
	/// </summary>
	public interface IDirtyable
	{
		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Returns a value indicating whether the object is in a dirty state or if any of the
		/// contained <see cref="IDirtyable"/> objects are in a dirty state.
		/// </summary>
		bool IsDirty { get; }
		#endregion

		#region Events
		//==========================================================================================
		// Events
		//==========================================================================================

		/// <summary>
		/// Raised when the dirty state has changed.
		/// </summary>
		event EventHandler DirtyStateChanged;
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Clears the dirty flag for the implementing object and any <see cref="IDirtyable"/>
		/// objects that it contains.
		/// </summary>
		void ClearDirty();
		#endregion
	}
}
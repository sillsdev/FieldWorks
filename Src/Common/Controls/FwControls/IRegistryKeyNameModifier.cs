// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: IRegistryKeyNameModifier.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using Microsoft.Win32;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This interface allows for modifying registry key names. Useful for adding context or
	/// application-specific info to a more general-purpose key that comes from Persistence.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IRegistryKeyNameModifier
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Modify the key name.
		/// </summary>
		/// <param name="key">Original (base) settings key</param>
		/// <param name="fWriteable">Get write access to the key</param>
		/// <returns>the modified key</returns>
		/// ------------------------------------------------------------------------------------
		RegistryKey ModifyKey(RegistryKey key, bool fWriteable);
	}
}

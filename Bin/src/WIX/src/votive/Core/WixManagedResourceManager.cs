//--------------------------------------------------------------------------------------------------
// <copyright file="WixManagedResourceManager.cs" company="Microsoft">
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
// Contains the WixManagedResourceManager class.
// </summary>
//--------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
	using System;
	using System.Globalization;
	using System.Resources;
	using Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure;

	/// <summary>
	/// Contains utility functions for retrieving resources from the managed satellite assemblies.
	/// </summary>
	public class WixManagedResourceManager : ManagedResourceManager
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static ResourceManager thisAssemblyManager = new ResourceManager(typeof(WixManagedResourceManager).Namespace + ".Strings", typeof(WixManagedResourceManager).Assembly);
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="WixManagedResourceManager"/> class.
		/// </summary>
		public WixManagedResourceManager()
			: base(thisAssemblyManager)
		{
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Returns a value indicating whether the specified name is a defined string resource name.
		/// </summary>
		/// <param name="name">The resource identifier to check.</param>
		/// <returns>true if the string identifier is defined in our assembly; otherwise, false.</returns>
		public override bool IsStringDefined(string name)
		{
			return WixStrings.IsValidStringName(name);
		}
		#endregion
	}
}
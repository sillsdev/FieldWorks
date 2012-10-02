//-------------------------------------------------------------------------------------------------
// <copyright file="WixPackageContext.cs" company="Microsoft">
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
// Contains the WixPackageContext class.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
	using System;
	using Microsoft.VisualStudio.Shell.Interop;
	using Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure;

	/// <summary>
	/// Contains WiX-specific package context information, such as helper classes and settings.
	/// </summary>
	internal class WixPackageContext : PackageContext
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="WixPackageContext"/> class.
		/// </summary>
		/// <param name="serviceProvider">
		/// The <see cref="ServiceProvider"/> instance to use for getting services from the environment.
		/// </param>
		public WixPackageContext(ServiceProvider serviceProvider) : base(serviceProvider)
		{
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets the package settings.
		/// </summary>
		public new WixPackageSettings Settings
		{
			get { return (WixPackageSettings)base.Settings; }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Creates the wix-specific managed resource manager.
		/// </summary>
		/// <returns>A new instance of the <see cref="WixManagedResourceManager"/> class.</returns>
		protected override ManagedResourceManager CreateManagedResourceManager()
		{
			return new WixManagedResourceManager();
		}

		/// <summary>
		/// Creates a new strongly-typed instance of the <see cref="WixPackageSettings"/> class.
		/// </summary>
		/// <param name="serviceProvider">The <see cref="ServiceProvider"/> to use.</param>
		/// <returns>A new <see cref="WixPackageSettings"/> instance.</returns>
		protected override PackageSettings CreatePackageSettings(ServiceProvider serviceProvider)
		{
			return new WixPackageSettings(serviceProvider);
		}
		#endregion
	}
}

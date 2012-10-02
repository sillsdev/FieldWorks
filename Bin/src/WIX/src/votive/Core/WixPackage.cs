//-------------------------------------------------------------------------------------------------
// <copyright file="WixPackage.cs" company="Microsoft">
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
// Core implementation for the WiX Visual Studio package.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
	using System;
	using System.Runtime.InteropServices;
	using Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure;

	/// <summary>
	/// Implements and/or provides all of the required interfaces and services to allow the
	/// Microsoft Windows Installer XML (WiX) project to be integrated into the Visual Studio
	/// environment.
	/// </summary>
	[Guid("B0AB1F0F-7B08-47FD-8E7C-A5C0EC855568")]
	internal sealed class WixPackage : Package
	{
		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="WixPackage"/> class.
		/// </summary>
		public WixPackage()
		{
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets the singleton instance of the currently running WiX Visual Studio package.
		/// </summary>
		public static new WixPackage Instance
		{
			get { return (WixPackage)Package.Instance; }
		}

		/// <summary>
		/// Gets the Wix context associated with this package.
		/// </summary>
		public new WixPackageContext Context
		{
			get { return (WixPackageContext)base.Context; }
		}

		/// <summary>
		/// Gets the GUID for the WiX project type that should be registered with Visual Studio.
		/// </summary>
		public override Guid ProjectTypeGuid
		{
			get { return typeof(WixProject).GUID; }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Creates a new type-specific <see cref="WixPackageContext"/> object.
		/// </summary>
		/// <param name="serviceProvider">The <see cref="ServiceProvider"/> instance to use for getting services from the environment.</param>
		/// <returns>A new <see cref="WixPackageContext"/> object.</returns>
		protected override PackageContext CreatePackageContext(ServiceProvider serviceProvider)
		{
			return new WixPackageContext(serviceProvider);
		}

		/// <summary>
		/// Provides a way for subclasses to create a new type-specific project factory.
		/// </summary>
		/// <returns>A new <see cref="ProjectFactory"/> object.</returns>
		protected override ProjectFactory CreateProjectFactory()
		{
			return new WixProjectFactory(this);
		}
		#endregion
	}
}
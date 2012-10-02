//--------------------------------------------------------------------------------------------------
// <copyright file="LocalizedDisplayNameAttribute.cs" company="Microsoft">
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
// Subclasses DisplayNameAttribute to allow for localized strings retrieved from the resource assembly.
// </summary>
//--------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.ComponentModel;

	/// <summary>
	/// Subclasses <see cref="DisplayNameAttribute"/> to allow for localized strings retrieved
	/// from the resource assembly.
	/// </summary>
	[AttributeUsage(AttributeTargets.Event | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Class)]
	public class LocalizedDisplayNameAttribute : DisplayNameAttribute
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private bool initialized;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="LocalizedDisplayNameAttribute"/> class.
		/// </summary>
		/// <param name="name">The name of the string resource to get.</param>
		public LocalizedDisplayNameAttribute(string name)
			: base(name)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LocalizedDisplayNameAttribute"/> class.
		/// </summary>
		/// <param name="id">The sconce string identifier to get.</param>
		public LocalizedDisplayNameAttribute(SconceStrings.StringId id)
			: base(id.ToString())
		{
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Gets the display name for a property, event, or public void method that takes no
		/// arguments stored in this attribute.
		/// </summary>
		public override string DisplayName
		{
			get
			{
				if (!this.initialized)
				{
					string localizedString = Package.Instance.Context.ManagedResources.GetString(this.DisplayNameValue);
					if (localizedString != null)
					{
						this.DisplayNameValue = localizedString;
					}
					this.initialized = true;
				}
				return this.DisplayNameValue;
			}
		}
		#endregion
	}
}
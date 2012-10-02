//--------------------------------------------------------------------------------------------------
// <copyright file="LocalizedDescriptionAttribute.cs" company="Microsoft">
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
// Subclasses DescriptionAttribute to allow for localized strings retrieved from the resource assembly.
// </summary>
//--------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.ComponentModel;

	/// <summary>
	/// Subclasses <see cref="DescriptionAttribute"/> to allow for localized strings retrieved
	/// from the resource assembly.
	/// </summary>
	[AttributeUsage(AttributeTargets.All)]
	public class LocalizedDescriptionAttribute : DescriptionAttribute
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
		/// Initializes a new instance of the <see cref="LocalizedDescriptionAttribute"/> class.
		/// </summary>
		/// <param name="name">The name of the string resource to get.</param>
		public LocalizedDescriptionAttribute(string name)
			: base(name)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LocalizedDescriptionAttribute"/> class.
		/// </summary>
		/// <param name="id">The sconce string identifier to get.</param>
		public LocalizedDescriptionAttribute(SconceStrings.StringId id)
			: base(id.ToString())
		{
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Gets the description stored in this attribute.
		/// </summary>
		public override string Description
		{
			get
			{
				if (!this.initialized)
				{
					string localizedDescription = Package.Instance.Context.ManagedResources.GetString(this.DescriptionValue);
					if (localizedDescription != null)
					{
						this.DescriptionValue = localizedDescription;
					}
					this.initialized = true;
				}
				return this.DescriptionValue;
			}
		}
		#endregion
	}
}
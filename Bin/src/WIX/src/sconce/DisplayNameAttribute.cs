//--------------------------------------------------------------------------------------------------
// <copyright file="DisplayNameAttribute.cs" company="Microsoft">
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
// Specifies the display name for a property, event, or public void method which takes no arguments.
// </summary>
//--------------------------------------------------------------------------------------------------

#if !USE_NET20_FRAMEWORK

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;

	/// <summary>
	/// Specifies the display name for a property, event, or public void method which takes no arguments.
	/// </summary>
	/// <remarks>
	/// This attribute was introduced in the .NET Framework 2.0, so we'll only define it in the
	/// Votive 2003 version.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Event | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Class)]
	public class DisplayNameAttribute : Attribute
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		/// <summary>
		/// Specifies the default value for the <see cref="DisplayNameAttribute"/>. This field is read-only.
		/// </summary>
		public static readonly DisplayNameAttribute Default = new DisplayNameAttribute();

		private string displayName;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="DisplayNameAttribute"/> class.
		/// </summary>
		public DisplayNameAttribute() : this(String.Empty)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DisplayNameAttribute"/> class using the display name.
		/// </summary>
		/// <param name="displayName">The display name.</param>
		public DisplayNameAttribute(string displayName)
		{
			this.displayName = displayName;
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets the display name for a property, event, or public void method that takes no
		/// arguments stored in this attribute.
		/// </summary>
		public virtual string DisplayName
		{
			get { return this.displayName; }
		}

		/// <summary>
		/// Gets or sets the display name.
		/// </summary>
		protected string DisplayNameValue
		{
			get { return this.displayName; }
			set { this.displayName = value; }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Determines if this attribute is the default.
		/// </summary>
		/// <returns>true if the attribute is the default value for this attribute class; otherwise, false.</returns>
		public override bool IsDefaultAttribute()
		{
			return this.Equals(Default);
		}

		/// <summary>
		/// Determines whether two <see cref="DisplayNameAttribute"/> instances are equal.
		/// </summary>
		/// <param name="obj">The <see cref="DisplayNameAttribute"/> to test the value equality of.</param>
		/// <returns>true if the value of the given object is equal to that of the current; otherwise, false.</returns>
		public override bool Equals(object obj)
		{
			if (obj == this)
			{
				return true;
			}

			DisplayNameAttribute attribute = obj as DisplayNameAttribute;
			if (attribute != null)
			{
				return (attribute.DisplayName == this.DisplayName);
			}
			return false;
		}

		/// <summary>
		/// Returns the hash code for this instance.
		/// </summary>
		/// <returns>A hash code for the current <see cref="DisplayNameAttribute"/>.</returns>
		public override int GetHashCode()
		{
			return this.DisplayName.GetHashCode();
		}
		#endregion
	}
}

#endif // !USE_NET20_FRAMEWORK

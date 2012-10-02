//--------------------------------------------------------------------------------------------------
// <copyright file="PropertyGridPropertyDescriptor.cs" company="Microsoft">
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
// Allows for a customizable display name for a property at design time.
// </summary>
//--------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.ComponentModel;

	/// <summary>
	/// Allows for a customizable display name for a property at design time as well as controlling
	/// when a property is bolded in the property grid.
	/// </summary>
	/// <remarks>
	/// Believe it or not, the .NET Framework does not contain any classes derived from <see cref="PropertyDescriptor"/>
	/// that allow for the customization of the property's display name.
	/// </remarks>
	public class PropertyGridPropertyDescriptor : PropertyDescriptor
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private bool containsDefaultValueAttribute;
		private string displayName;
		private PropertyDescriptor property;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="PropertyGridPropertyDescriptor"/> class.
		/// </summary>
		/// <param name="property">The <see cref="PropertyDescriptor"/> to wrap.</param>
		public PropertyGridPropertyDescriptor(PropertyDescriptor property) : base(property)
		{
			Tracer.VerifyNonNullArgument(property, "property");

			this.property = property;
			Attribute attribute = property.Attributes[typeof(DisplayNameAttribute)];
			if (attribute is DisplayNameAttribute)
			{
				this.displayName = ((DisplayNameAttribute)attribute).DisplayName;
			}
			else
			{
				this.displayName = property.Name;
			}

			DefaultValueAttribute defaultValueAttribute = property.Attributes[typeof(DefaultValueAttribute)] as DefaultValueAttribute;
			this.containsDefaultValueAttribute = (defaultValueAttribute != null);
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets the type of the component this property is bound to.
		/// </summary>
		public override Type ComponentType
		{
			get { return this.property.ComponentType; }
		}

		/// <summary>
		/// Gets the name that can be displayed in a window, such as a Properties window.
		/// </summary>
		public override string DisplayName
		{
			get { return this.displayName; }
		}

		/// <summary>
		/// Gets a value indicating whether this property is read-only.
		/// </summary>
		public override bool IsReadOnly
		{
			get { return this.property.IsReadOnly; }
		}

		/// <summary>
		/// Gets the type of the property.
		/// </summary>
		public override Type PropertyType
		{
			get { return this.property.PropertyType; }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Returns whether resetting an object changes its value.
		/// </summary>
		/// <param name="component">The component to test for reset capability.</param>
		/// <returns>true if resetting the component changes its value; otherwise, false.</returns>
		public override bool CanResetValue(object component)
		{
			return this.property.CanResetValue(component);
		}

		/// <summary>
		/// Gets the current value of the property on a component.
		/// </summary>
		/// <param name="component">The component with the property for which to retrieve the value.</param>
		/// <returns>The value of a property for a given component.</returns>
		public override object GetValue(object component)
		{
			return this.property.GetValue(component);
		}

		/// <summary>
		/// Resets the value for this property of the component to the default value.
		/// </summary>
		/// <param name="component">The component with the property value that is to be reset to the default value.</param>
		public override void ResetValue(object component)
		{
			this.property.ResetValue(component);
		}

		/// <summary>
		/// Sets the value of the component to a different value.
		/// </summary>
		/// <param name="component">The component with the property value that is to be set.</param>
		/// <param name="value">The new value.</param>
		public override void SetValue(object component, object value)
		{
			this.property.SetValue(component, value);
		}

		/// <summary>
		/// Determines a value indicating whether the value of this property needs to be persisted.
		/// </summary>
		/// <param name="component">The component with the property to be examined for persistence.</param>
		/// <returns>true if the property should be persisted; otherwise, false.</returns>
		public override bool ShouldSerializeValue(object component)
		{
			if (this.containsDefaultValueAttribute)
			{
				return this.property.ShouldSerializeValue(component);
			}

			if (component is PropertyPageSettings)
			{
				return ((PropertyPageSettings)component).IsPropertyDirty(this.Name);
			}

			// By returning false here, the property will never be bolded in the property grid
			return false;
		}
		#endregion
	}
}
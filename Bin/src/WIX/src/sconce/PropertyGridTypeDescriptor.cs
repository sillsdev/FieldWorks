//--------------------------------------------------------------------------------------------------
// <copyright file="PropertyGridTypeDescriptor.cs" company="Microsoft">
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
// Implements an ICustomTypeDescriptor to account for the DisplayNameAttribute.
// </summary>
//--------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Collections;
	using System.ComponentModel;

	/// <summary>
	/// Implements an <see cref="ICustomTypeDescriptor"/> to account for the <see cref="DisplayNameAttribute"/>.
	/// </summary>
	public class PropertyGridTypeDescriptor : ICustomTypeDescriptor
	{
		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="PropertyGridTypeDescriptor"/> class.
		/// </summary>
		public PropertyGridTypeDescriptor()
		{
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Returns a collection of type <see cref="Attribute"/> for this object.
		/// </summary>
		/// <returns>An <see cref="AttributeCollection"/> with the attributes for this object.</returns>
		public AttributeCollection GetAttributes()
		{
			return TypeDescriptor.GetAttributes(this, true);
		}

		/// <summary>
		/// Returns the name that is displayed in the right hand side of the Properties window drop-down combo box.
		/// </summary>
		/// <returns>The class name of the object, or null if the class does not have a name.</returns>
		public virtual string GetClassName()
		{
			return this.GetType().Name;
		}

		/// <summary>
		/// Returns the name that is displayed in the left hand side of the Properties window drop-down combo box.
		/// </summary>
		/// <returns>The name of the object, or null if the class does not have a name.</returns>
		public virtual string GetComponentName()
		{
			return TypeDescriptor.GetComponentName(this, true);
		}

		/// <summary>
		/// Returns a type converter for this object.
		/// </summary>
		/// <returns>A <see cref="TypeConverter"/> that is the converter for this object, or null if there is no <b>TypeConverter</b> for this object.</returns>
		public TypeConverter GetConverter()
		{
			return TypeDescriptor.GetConverter(this, true);
		}

		/// <summary>
		/// Returns the default event for this object.
		/// </summary>
		/// <returns>An <see cref="EventDescriptor"/> that represents the default event for this object, or null if this object does not have events.</returns>
		public EventDescriptor GetDefaultEvent()
		{
			return TypeDescriptor.GetDefaultEvent(this, true);
		}

		/// <summary>
		/// Returns the default property for this object.
		/// </summary>
		/// <returns>An <see cref="PropertyDescriptor"/> that represents the default property for this object, or null if this object does not have properties.</returns>
		public PropertyDescriptor GetDefaultProperty()
		{
			return TypeDescriptor.GetDefaultProperty(this, true);
		}

		/// <summary>
		/// Returns an editor of the specified type for this object.
		/// </summary>
		/// <param name="editorBaseType">A <see cref="Type"/> that represents the editor for this object.</param>
		/// <returns>An <see cref="Object"/> of the specified type that is the editor for this object, or null if the editor cannot be found.</returns>
		public object GetEditor(Type editorBaseType)
		{
			return TypeDescriptor.GetEditor(this, editorBaseType, true);
		}

		/// <summary>
		/// Returns the events for this instance of a component.
		/// </summary>
		/// <returns>An <see cref="EventDescriptorCollection"/> that represents the events for this component instance.</returns>
		public EventDescriptorCollection GetEvents()
		{
			return TypeDescriptor.GetEvents(this, true);
		}

		/// <summary>
		/// Returns the events for this instance of a component using the attribute array as a filter.
		/// </summary>
		/// <param name="attributes">An array of type <see cref="Attribute"/> that is used as a filter.</param>
		/// <returns>An <see cref="EventDescriptorCollection"/> that represents the events for this component instance that match the given set of attributes.</returns>
		public EventDescriptorCollection GetEvents(Attribute[] attributes)
		{
			return TypeDescriptor.GetEvents(this, attributes, true);
		}

		/// <summary>
		/// Returns the properties for this instance of a component.
		/// </summary>
		/// <returns>An <see cref="PropertyDescriptorCollection"/> that represents the properties for this component instance.</returns>
		public PropertyDescriptorCollection GetProperties()
		{
			return TypeDescriptor.GetProperties(this, true);
		}

		/// <summary>
		/// Returns the properties for this instance of a component using the attribute array as a filter.
		/// </summary>
		/// <param name="attributes">An array of type <see cref="Attribute"/> that is used as a filter.</param>
		/// <returns>An <see cref="PropertyDescriptorCollection"/> that represents the properties for this component instance that match the given set of attributes.</returns>
		public PropertyDescriptorCollection GetProperties(Attribute[] attributes)
		{
			PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(this, attributes, true);
			ArrayList descriptors = new ArrayList(properties.Count);

			// Loop through the property descriptores that we got from the type descriptor to create
			// our own custom DisplayNamePropertyDescriptor objects that wrap the default descriptors.
			foreach (PropertyDescriptor property in properties)
			{
				PropertyGridPropertyDescriptor displayNameProperty = new PropertyGridPropertyDescriptor(property);
				descriptors.Add(displayNameProperty);
			}

			PropertyDescriptor[] array = (PropertyDescriptor[])descriptors.ToArray(typeof(PropertyDescriptor));
			return new PropertyDescriptorCollection(array);
		}

		/// <summary>
		/// Returns the object that this value is a member of.
		/// </summary>
		/// <param name="pd">A <see cref="PropertyDescriptor"/> that represents the property whose owner is to be found.</param>
		/// <returns>An <see cref="Object"/> that represents the owner of the specified property.</returns>
		public object GetPropertyOwner(PropertyDescriptor pd)
		{
			return this;
		}
		#endregion
	}
}
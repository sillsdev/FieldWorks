// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FwButtonDesigner.cs
// Responsibility: EberhardB
// Last reviewed:
//
// Implementation of FwButtonDesigner which contains additional design-time functionality for
// FwButton.
// --------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms.Design;

namespace SIL.FieldWorks.Common.Controls.Design
{
	/// <summary>
	/// Extends design-time behavior for components that extend
	/// <see href="SIL.FieldWorks.Common.Controls.FwButton.html">FwButton</see>.
	/// </summary>
	internal class FwButtonDesigner: ControlDesigner
	{
		/// <summary>
		/// Adjusts the set of properties the component exposes through a TypeDescriptor.
		/// </summary>
		/// <param name="properties">An IDictionary containing the properties for the class of
		/// the component.</param>
		protected override void PreFilterProperties(IDictionary properties)
		{
			base.PreFilterProperties(properties);

			// Change category for the following properties to make it a little nicer
			properties["Image"] = TypeDescriptor.CreateProperty(
				((PropertyDescriptor)properties["Image"]).ComponentType,
				(PropertyDescriptor)properties["Image"],
				new Attribute[] {new CategoryAttribute("Appearance: Image")});
			properties["ImageAlign"] = TypeDescriptor.CreateProperty(
				((PropertyDescriptor)properties["ImageAlign"]).ComponentType,
				(PropertyDescriptor)properties["ImageAlign"],
				new Attribute[] {new CategoryAttribute("Appearance: Image")});
			properties["ImageIndex"] = TypeDescriptor.CreateProperty(
				((PropertyDescriptor)properties["ImageIndex"]).ComponentType,
				(PropertyDescriptor)properties["ImageIndex"],
				new Attribute[] {new CategoryAttribute("Appearance: Image")});
			properties["ImageList"] = TypeDescriptor.CreateProperty(
				((PropertyDescriptor)properties["ImageList"]).ComponentType,
				(PropertyDescriptor)properties["ImageList"],
				new Attribute[] {new CategoryAttribute("Appearance: Image")});
		}

		/// <summary>
		/// Allows a designer to change or remove items from the set of properties that it
		/// exposes through a TypeDescriptor
		/// </summary>
		/// <param name="properties">The properties for the class of the component.</param>
		protected override void PostFilterProperties(IDictionary properties)
		{
			base.PostFilterProperties(properties);

			// Remove Button.FlatStyle attribute, because we have our own
			properties.Remove("FlatStyle");
		}
	}

}

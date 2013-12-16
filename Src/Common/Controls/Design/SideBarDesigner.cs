// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: SideBarDesigner.cs
// Responsibility: EberhardB
// Last reviewed:
//
// Implementation of SideBarDesigner which contains additional design-time functionality for
// SideBar.
// --------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace SIL.FieldWorks.Common.Controls.Design
{
	/// <summary>
	/// Extends design-time behavior for components that extend
	/// <see href="SIL.FieldWorks.Common.Controls.SideBar.html">SideBar</see>.
	/// </summary>
	public class SideBarDesigner: ControlDesigner
	{
		/// <summary>
		/// Initializes a new instance of the SideBarDesigner class.
		/// </summary>
		public SideBarDesigner()
		{
		}

		/// <summary>
		/// Allows a designer to change or remove items from the set of properties that it
		/// exposes through a TypeDescriptor
		/// </summary>
		/// <param name="properties">The properties for the class of the component.</param>
		protected override void PostFilterProperties(IDictionary properties)
		{
			// Set new default value for BackColor
			PropertyDescriptor property = (PropertyDescriptor)properties["BackColor"];
			properties["BackColor"] = TypeDescriptor.CreateProperty(property.ComponentType,
				property, new DefaultValueAttribute(System.Drawing.SystemColors.ControlDark));

			// Set default value for Dock to left
			property = (PropertyDescriptor)properties["Dock"];
			properties["Dock"] = TypeDescriptor.CreateProperty(property.ComponentType,
				property, new DefaultValueAttribute(DockStyle.Left));

			// Remove some properties
			properties.Remove("BackgroundImage");

			base.PostFilterProperties(properties);
		}

	}
}

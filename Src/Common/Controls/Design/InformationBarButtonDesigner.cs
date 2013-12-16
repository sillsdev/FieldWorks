// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: InformationBarButtonDesigner.cs
// Responsibility: ToddJ
// Last reviewed:
//
// --------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms.Design;

namespace SIL.FieldWorks.Common.Controls.Design
{
	/// <summary>
	/// Extends design-time behavior for components that extend
	/// <see href="SIL.FieldWorks.Common.Controls.InformationBarButton.html">InformationBarButton</see>.
	/// </summary>
	public class InformationBarButtonDesigner : ControlDesigner
	{
		/// <summary>
		/// Initializes a new instance of the InformationBarButtonDesigner class.
		/// </summary>
		public InformationBarButtonDesigner()
		{
		}

		/// <summary>
		/// Adjusts the set of properties the component exposes through a TypeDescriptor.
		/// </summary>
		/// <param name="properties">An IDictionary containing the properties for the class of
		/// the component.</param>
		protected override void PreFilterProperties(IDictionary properties)
		{
			base.PreFilterProperties(properties);

			PropertyDescriptor property = (PropertyDescriptor)properties["Size"];
			properties["Size"] = TypeDescriptor.CreateProperty(property.ComponentType,
				property, BrowsableAttribute.No, DesignerSerializationVisibilityAttribute.Hidden);
		}
	}
}

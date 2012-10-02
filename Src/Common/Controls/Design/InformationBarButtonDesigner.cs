// --------------------------------------------------------------------------------------------
// <copyright from='2002' to='2002' company='SIL International'>
//    Copyright (c) 2002, SIL International. All Rights Reserved.
// </copyright>
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

// --------------------------------------------------------------------------------------------
// <copyright from='2002' to='2002' company='SIL International'>
//    Copyright (c) 2002, SIL International. All Rights Reserved.
// </copyright>
//
// File: SideBarTabDesigner.cs
// Responsibility: EberhardB
// Last reviewed:
//
// Implementation of SideBarTabDesigner which contains additional design-time functionality for
// SideBarTab.
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
	/// <see href="SIL.FieldWorks.Common.Controls.SideBarTab.html">SideBarTab</see>.
	/// </summary>
	public class SideBarTabDesigner: ControlDesigner
	{
		/// <summary>
		/// Initializes a new instance of the SideBarTabDesigner class.
		/// </summary>
		public SideBarTabDesigner()
		{
		}

		/// <summary>
		/// We shadow the Enabled property, so that the Tab (and the buttons) is not
		/// selectable by clicking the mouse
		/// </summary>
		public bool Enabled
		{
			get { return (bool)ShadowProperties["Enabled"]; }
			set { ShadowProperties["Enabled"] = value; }
		}

		/// <summary>
		/// Initialization. Save original Enabled property and work with Enabled=false at
		/// design time.
		/// </summary>
		/// <param name="component"></param>
		public override void Initialize(IComponent component)
		{
			base.Initialize(component);

			Control control = component as Control;

			if (control == null)
			{
				throw new ArgumentException();
			}

			Enabled = control.Enabled;

			control.Enabled = false;
		}

		/// <summary>
		/// Add our shadowed property.
		/// </summary>
		/// <param name="properties"></param>
		protected override void PreFilterProperties(IDictionary properties)
		{
			base.PreFilterProperties(properties);

			properties["Enabled"] = TypeDescriptor.CreateProperty(typeof(SideBarTabDesigner),
				(PropertyDescriptor)properties["Enabled"], new Attribute[0]);
		}

		/// <summary>
		/// Remove or hide some properties.
		/// </summary>
		/// <param name="properties">The properties for the class of the component.</param>
		protected override void PostFilterProperties(IDictionary properties)
		{
			// Set new default value for BackColor
			PropertyDescriptor property = (PropertyDescriptor)properties["BackColor"];
			properties["BackColor"] = TypeDescriptor.CreateProperty(property.ComponentType,
				property, new DefaultValueAttribute(System.Drawing.SystemColors.Control));

			// Remove some of the properties
			properties.Remove("Dock");
			properties.Remove("DockPadding");
			properties.Remove("AutoScroll");
			properties.Remove("AutoScrollMargin");
			properties.Remove("AutoScrollMinSize");
			properties.Remove("BackgroundImage");
			properties.Remove("TabStop");
			properties.Remove("TabIndex");

			// And hide others
			property = (PropertyDescriptor)properties["Size"];
			properties["Size"] = TypeDescriptor.CreateProperty(property.ComponentType,
				property, BrowsableAttribute.No, DesignerSerializationVisibilityAttribute.Hidden);
			property = (PropertyDescriptor)properties["Location"];
			properties["Location"] = TypeDescriptor.CreateProperty(property.ComponentType,
				property, BrowsableAttribute.No, DesignerSerializationVisibilityAttribute.Hidden);
			property = (PropertyDescriptor)properties["Anchor"];
			properties["Anchor"] = TypeDescriptor.CreateProperty(property.ComponentType,
				property, BrowsableAttribute.No, DesignerSerializationVisibilityAttribute.Hidden);

			base.PostFilterProperties(properties);
		}

	}
}

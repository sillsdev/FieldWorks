// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: SideBarButtonDesigner.cs
// Responsibility: EberhardB
// Last reviewed:
//
// Implementation of SideBarButtonDesigner which contains additional design-time functionality for
// SideBarButton.
// --------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Forms.Design;

namespace SIL.FieldWorks.Common.Controls.Design
{
	/// <summary>
	/// Extends design-time behavior for components that extend
	/// <see href="SIL.FieldWorks.Common.Controls.SideBarButton.html">SideBarButton</see>.
	/// </summary>
	/// <remarks>Main purpose of this designer is to hide all the unnecessary properties
	/// </remarks>
	public class SideBarButtonDesigner: ControlDesigner
	{
		/// <summary>
		/// Initializes a new instance of the SideBarButtonDesigner class.
		/// </summary>
		public SideBarButtonDesigner()
		{
		}

		/// <summary>
		/// Allows a designer to change or remove items from the set of properties that it
		/// exposes through a TypeDescriptor
		/// </summary>
		/// <param name="properties">The properties for the class of the component.</param>
		protected override void PostFilterProperties(IDictionary properties)
		{
			StringCollection toRemove = new StringCollection();

			foreach (string key in properties.Keys)
			{
				// Remove everything except Text and ImageIndex
				if (key != "Text" && key != "ImageIndex" && !key.StartsWith("Name")
					&& !key.StartsWith("ImageList") && !key.StartsWith("Height")
					&& !key.StartsWith("HelpText"))
				{
					toRemove.Add(key);
				}
			}

			foreach (string str in toRemove)
			{
				properties.Remove(str);
			}

			PropertyDescriptor property = (PropertyDescriptor)properties["ImageList"];
			// set ImageList property to not browsable
			properties["ImageList"] = TypeDescriptor.CreateProperty(property.ComponentType,
				property, BrowsableAttribute.No, DesignerSerializationVisibilityAttribute.Hidden);
			property = (PropertyDescriptor)properties["ImageListLarge"];
			properties["ImageListLarge"] = TypeDescriptor.CreateProperty(property.ComponentType,
				property, BrowsableAttribute.No);
			property = (PropertyDescriptor)properties["ImageListSmall"];
			properties["ImageListSmall"] = TypeDescriptor.CreateProperty(property.ComponentType,
				property, BrowsableAttribute.No);
			property = (PropertyDescriptor)properties["Height"];
			properties["Height"] = TypeDescriptor.CreateProperty(property.ComponentType,
				property, BrowsableAttribute.No, DesignerSerializationVisibilityAttribute.Hidden);
			property = (PropertyDescriptor)properties["HeightLarge"];
			properties["HeightLarge"] = TypeDescriptor.CreateProperty(property.ComponentType,
				property, BrowsableAttribute.No);
			property = (PropertyDescriptor)properties["HeightSmall"];
			properties["HeightSmall"] = TypeDescriptor.CreateProperty(property.ComponentType,
				property, BrowsableAttribute.No);

			base.PostFilterProperties(properties);
		}
	}
}

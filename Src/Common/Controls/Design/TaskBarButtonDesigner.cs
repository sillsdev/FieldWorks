// --------------------------------------------------------------------------------------------
// <copyright from='2002' to='2002' company='SIL International'>
//    Copyright (c) 2002, SIL International. All Rights Reserved.
// </copyright>
//
// File: TaskBarButtonDesigner.cs
// Responsibility: EberhardB
// Last reviewed:
//
// Implementation of TaskBarButtonDesigner which contains additional design-time functionality for
// TaskBarButton.
// --------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Forms.Design;

namespace SIL.FieldWorks.Common.Controls.Design
{
	/// <summary>
	/// Main purpose of this designer is to hide all the unnecessary properties
	/// </summary>
	public class TaskBarButtonDesigner: ControlDesigner
	{
		public TaskBarButtonDesigner()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		protected override void PreFilterProperties(IDictionary properties)
		{
			base.PreFilterProperties(properties);

			StringCollection toRemove = new StringCollection();

			foreach (string key in properties.Keys)
			{
				// Remove everything except Text and ImageIndex
				if (key != "Text" && key != "ImageIndex" && !key.StartsWith("Name")
					&& !key.StartsWith("ImageList") && !key.StartsWith("Height"))
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
		}
	}
}

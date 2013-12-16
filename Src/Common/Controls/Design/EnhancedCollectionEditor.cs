// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: EnhancedCollectionEditor.cs
// Responsibility: EberhardB
// Last reviewed:
//
// Enhances the standard CollectionEditor class: Shows toolbar and help for PropertyGrid.
// --------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.ComponentModel.Design;
using System.Reflection;

namespace SIL.FieldWorks.Common.Controls.Design
{
	/// <summary>
	/// Enhances the standard CollectionEditor class: Shows toolbar and help for PropertyGrid.
	/// </summary>
	public class EnhancedCollectionEditor : CollectionEditor
	{
		/// <summary>
		/// Initializes a new instance of the CollectionEditor class using the specified
		/// collection type.
		/// </summary>
		/// <param name="type">The type of the collection for this editor to edit.</param>
		public EnhancedCollectionEditor(Type type)
			: base(type)
		{
		}

		/// <summary>
		/// Overriden, so that we can change the properties of the PropertyGrid. This is done
		/// via Reflection.
		/// </summary>
		/// <returns>The form</returns>
		protected override CollectionForm CreateCollectionForm()
		{
			CollectionEditor.CollectionForm form = base.CreateCollectionForm();

			Type t =  form.GetType();

			// Get the private variable PropertyGrid.propertyBrowser via Reflection
			FieldInfo fieldInfo = t.GetField("propertyBrowser",
				BindingFlags.NonPublic | BindingFlags.Instance);

			if (fieldInfo != null)
			{
				PropertyGrid propertyGrid = (PropertyGrid)fieldInfo.GetValue(form);
				if (propertyGrid != null)
				{
					propertyGrid.ToolbarVisible = true;
					propertyGrid.HelpVisible = true;
					propertyGrid.BackColor = SystemColors.Control;
				}
			}

			return form;
		}

	}
}

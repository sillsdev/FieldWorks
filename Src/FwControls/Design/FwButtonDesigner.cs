// Copyright (c) 2002-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections;
using System.ComponentModel;
using System.Windows.Forms.Design;

namespace SIL.FieldWorks.Common.Controls.Design
{
	/// <summary>
	/// Extends design-time behavior for components that extend
	/// <see href="SIL.FieldWorks.Common.Controls.FwButton.html">FwButton</see>.
	/// </summary>
	internal class FwButtonDesigner : ControlDesigner
	{
		/// <inheritdoc />
		protected override void PreFilterProperties(IDictionary properties)
		{
			base.PreFilterProperties(properties);

			// Change category for the following properties to make it a little nicer
			properties["Image"] = TypeDescriptor.CreateProperty(((PropertyDescriptor)properties["Image"]).ComponentType, (PropertyDescriptor)properties["Image"], new CategoryAttribute("Appearance: Image"));
			properties["ImageAlign"] = TypeDescriptor.CreateProperty(((PropertyDescriptor)properties["ImageAlign"]).ComponentType, (PropertyDescriptor)properties["ImageAlign"], new CategoryAttribute("Appearance: Image"));
			properties["ImageIndex"] = TypeDescriptor.CreateProperty(((PropertyDescriptor)properties["ImageIndex"]).ComponentType, (PropertyDescriptor)properties["ImageIndex"], new CategoryAttribute("Appearance: Image"));
			properties["ImageList"] = TypeDescriptor.CreateProperty(((PropertyDescriptor)properties["ImageList"]).ComponentType, (PropertyDescriptor)properties["ImageList"], new CategoryAttribute("Appearance: Image"));
		}

		/// <inheritdoc />
		protected override void PostFilterProperties(IDictionary properties)
		{
			base.PostFilterProperties(properties);

			// Remove Button.FlatStyle attribute, because we have our own
			properties.Remove("FlatStyle");
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ******");
			base.Dispose(disposing);
		}
	}
}

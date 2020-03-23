// Copyright (c) 2002-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections;
using System.Diagnostics;
using System.Windows.Forms.Design;

namespace LanguageExplorer.Controls.Designer
{
	/// <summary>
	/// Implementation of FwHelpButtonDesigner which contains additional design-time
	/// functionality for FwHelpButton.
	/// </summary>
	internal sealed class FwHelpButtonDesigner : ControlDesigner
	{
		/// <inheritdoc />
		public override void InitializeNewComponent(IDictionary defaultValues)
		{
			base.InitializeNewComponent(defaultValues);
			Control.Text = "&Help";
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ******");
			base.Dispose(disposing);
		}
	}
}
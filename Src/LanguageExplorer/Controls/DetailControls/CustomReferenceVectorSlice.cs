// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// This class should be extended by any custom reference vector slices.
	/// </summary>
	internal abstract class CustomReferenceVectorSlice : ReferenceVectorSlice
	{
		/// <summary />
		protected CustomReferenceVectorSlice(Control control)
			: base(control)
		{
		}

		public override void FinishInit()
		{
			SetFieldFromConfig();
			base.FinishInit();
		}
	}
}
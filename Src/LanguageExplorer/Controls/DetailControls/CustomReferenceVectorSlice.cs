// Copyright (c) 2003-2018 SIL International
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
		/// <summary>
		/// Initializes a new instance of the <see cref="CustomReferenceVectorSlice"/> class.
		/// </summary>
		/// <param name="control">The control.</param>
		protected CustomReferenceVectorSlice(Control control)
			: base(control)
		{
		}

		public override void FinishInit()
		{
			CheckDisposed();

			SetFieldFromConfig();
			base.FinishInit();
		}
	}
}
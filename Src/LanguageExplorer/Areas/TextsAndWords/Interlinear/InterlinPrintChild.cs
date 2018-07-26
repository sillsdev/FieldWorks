// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// The modification of the main class suitable for this view.
	/// </summary>
	internal partial class InterlinPrintChild : InterlinDocRootSiteBase
	{
		public InterlinPrintChild()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Pull this out into a separate method so InterlinPrintChild can make an InterlinPrintVc.
		/// </summary>
		protected override void MakeVc()
		{
			Vc = new InterlinPrintVc(m_cache);
		}

		/// <summary>
		/// Activate() is disabled by default in ReadOnlyViews, but PrintView does want to show selections.
		/// </summary>
		protected override bool AllowDisplaySelection => true;
	}
}
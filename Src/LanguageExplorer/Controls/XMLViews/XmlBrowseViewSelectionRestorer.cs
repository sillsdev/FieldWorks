// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.RootSites;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// SelectionRestorer used in the XmlBrowseViewBase to more accuratly scroll the selection
	/// to the correct location.
	/// </summary>
	internal class XmlBrowseViewSelectionRestorer : SelectionRestorer
	{
		/// <summary />
		public XmlBrowseViewSelectionRestorer(XmlBrowseViewBase browseView) : base(browseView)
		{
		}

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting
		/// unmanaged resources.
		/// </summary>
		protected override void Dispose(bool fDisposing)
		{
			base.Dispose(fDisposing);
			if (fDisposing)
			{
				((XmlBrowseViewBase) m_rootSite)?.RestoreScrollPosition(null);
			}
		}
	}
}

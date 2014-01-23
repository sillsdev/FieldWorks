// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: XmlBrowseViewSelectionRestorer.cs
// Responsibility: FLEx Team

using System;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.Common.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// SelectionRestorer used in the XmlBrowseViewBase to more accuratly scroll the selection
	/// to the correct location.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class XmlBrowseViewSelectionRestorer : SelectionRestorer
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="XmlBrowseViewSelectionRestorer"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public XmlBrowseViewSelectionRestorer(XmlBrowseViewBase browseView) : base(browseView)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting
		/// unmanaged resources.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool fDisposing)
		{
			base.Dispose(fDisposing);
			if (fDisposing)
			{
				if (m_rootSite != null)
					((XmlBrowseViewBase)m_rootSite).OnRestoreScrollPosition(null);
			}
		}
	}
}

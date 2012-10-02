// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: XmlBrowseViewSelectionRestorer.cs
// Responsibility: FLEx Team
// ---------------------------------------------------------------------------------------------
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

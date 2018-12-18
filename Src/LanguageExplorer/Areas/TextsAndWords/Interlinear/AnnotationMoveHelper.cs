// Copyright (c) 2004-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Windows.Forms;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// Currently detects whether we've inserted a paragraph break (with the Enter key)
	/// and move annotations into the new paragraph.
	/// </summary>
	internal class AnnotationMoveHelper : ListUpdateHelper
	{
		RawTextPane m_rootSite;

		internal AnnotationMoveHelper(RawTextPane site, KeyPressEventArgs e)
			: base(new ListUpdateHelperParameterObject { MyRecordList = site.MyRecordList })
		{
			m_rootSite = site;
			if (!CanEdit())
			{
				return;
			}
			SkipShowRecord = true;
		}

		internal bool CanEdit()
		{
			return m_rootSite.RootHvo != 0 && m_rootSite != null && !m_rootSite.IsDisposed && !m_rootSite.ReadOnlyView && m_rootSite.Vc.Editable;
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + " ******");
			base.Dispose(disposing);
		}
	}
}
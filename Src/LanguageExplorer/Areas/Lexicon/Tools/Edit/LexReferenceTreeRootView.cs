// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.Framework.DetailControls;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// Summary description for LexReferenceTreeRootView.
	/// </summary>
	internal sealed class LexReferenceTreeRootView : AtomicReferenceView
	{
		/// <summary />
		public LexReferenceTreeRootView() : base()
		{
		}

		/// <summary />
		public override void SetReferenceVc()
		{
			CheckDisposed();

			m_atomicReferenceVc = new LexReferenceTreeRootVc(m_fdoCache,
					m_rootObj.Hvo, m_rootFlid, m_displayNameProperty);
		}
	}
}

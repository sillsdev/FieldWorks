// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Framework.DetailControls;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// Summary description for LexReferencePairView.
	/// </summary>
	internal sealed class LexReferencePairView : AtomicReferenceView
	{
		/// <summary />
		private ICmObject m_displayParent = null;

		/// <summary />
		public LexReferencePairView() : base()
		{
		}

		/// <summary />
		public override void SetReferenceVc()
		{
			CheckDisposed();

			m_atomicReferenceVc = new LexReferencePairVc(m_fdoCache, m_rootFlid, m_displayNameProperty);
			if (m_displayParent != null)
				(m_atomicReferenceVc as LexReferencePairVc).DisplayParent = m_displayParent;
		}

		/// <summary />
		public ICmObject DisplayParent
		{
			set
			{
				CheckDisposed();

				m_displayParent = value;
				if (m_atomicReferenceVc != null)
					(m_atomicReferenceVc as LexReferencePairVc).DisplayParent = value;
			}
		}
	}
}

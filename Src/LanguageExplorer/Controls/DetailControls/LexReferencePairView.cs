// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary />
	internal sealed class LexReferencePairView : AtomicReferenceView
	{
		/// <summary />
		private ICmObject m_displayParent;

		/// <summary />
		public override void SetReferenceVc()
		{
			m_atomicReferenceVc = new LexReferencePairVc(m_cache, m_rootFlid, m_displayNameProperty);
			if (m_displayParent != null)
			{
				((LexReferencePairVc)m_atomicReferenceVc).DisplayParent = m_displayParent;
			}
		}

		/// <summary />
		internal ICmObject DisplayParent
		{
			set
			{
				m_displayParent = value;
				if (m_atomicReferenceVc != null)
				{
					((LexReferencePairVc)m_atomicReferenceVc).DisplayParent = value;
				}
			}
		}
	}
}
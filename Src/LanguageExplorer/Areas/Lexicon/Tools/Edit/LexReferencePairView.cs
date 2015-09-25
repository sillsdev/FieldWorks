// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework.DetailControls;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// Summary description for LexReferencePairView.
	/// </summary>
	internal class LexReferencePairView : AtomicReferenceView
	{
		/// <summary />
		protected ICmObject m_displayParent = null;

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

	/// <summary>
	///  View constructor for creating the view details.
	/// </summary>
	internal class LexReferencePairVc : AtomicReferenceVc
	{
		/// <summary />
		protected ICmObject m_displayParent = null;

		/// <summary />
		public LexReferencePairVc(FdoCache cache, int flid, string displayNameProperty)
			: base (cache, flid, displayNameProperty)
		{
		}

		/// <summary />
		protected override int HvoOfObjectToDisplay(IVwEnv vwenv, int hvo)
		{
			ISilDataAccess sda = vwenv.DataAccess;
			int chvo = sda.get_VecSize(hvo, m_flid);
			if (chvo < 2)
				return 0;
			int hvoItem = sda.get_VecItem(hvo, m_flid, 0);
			if (m_displayParent != null && hvoItem == m_displayParent.Hvo)
				hvoItem = sda.get_VecItem(hvo, m_flid, 1);
			return hvoItem;
		}

		/// <summary />
		protected override void DisplayObjectProperty(IVwEnv vwenv, int hvo)
		{
			vwenv.AddObj(hvo, this, AtomicReferenceView.kFragObjName);
		}

		/// <summary />
		public ICmObject DisplayParent
		{
			set { m_displayParent = value; }
		}
	}
}

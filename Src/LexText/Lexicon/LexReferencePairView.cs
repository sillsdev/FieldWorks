using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework.DetailControls;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// Summary description for LexReferencePairView.
	/// </summary>
	public class LexReferencePairView : AtomicReferenceView
	{
		protected int m_hvoDisplayParent = 0;

		public LexReferencePairView() : base()
		{
		}

		public override void SetReferenceVc()
		{
			CheckDisposed();

			m_atomicReferenceVc = new LexReferencePairVc(m_fdoCache, m_rootFlid, m_displayNameProperty);
			if (m_hvoDisplayParent != 0)
				(m_atomicReferenceVc as LexReferencePairVc).DisplayParentHvo = m_hvoDisplayParent;
		}

		public int DisplayParentHvo
		{
			set
			{
				CheckDisposed();

				m_hvoDisplayParent = value;
				if (m_atomicReferenceVc != null)
					(m_atomicReferenceVc as LexReferencePairVc).DisplayParentHvo = value;
			}
		}
	}

	/// <summary>
	///  View constructor for creating the view details.
	/// </summary>
	public class LexReferencePairVc : AtomicReferenceVc
	{
		protected int m_hvoDisplayParent = 0;

		public LexReferencePairVc(FdoCache cache, int flid, string displayNameProperty)
			: base (cache, flid, displayNameProperty)
		{
		}

		protected override int HvoOfObjectToDisplay(IVwEnv vwenv, int hvo)
		{
			ISilDataAccess sda = vwenv.DataAccess;
			int chvo = sda.get_VecSize(hvo, m_flid);
			if (chvo < 2)
				return 0;
			int hvoItem = sda.get_VecItem(hvo, m_flid, 0);
			if (hvoItem == m_hvoDisplayParent)
				hvoItem = sda.get_VecItem(hvo, m_flid, 1);
			return hvoItem;
		}

		protected override void DisplayObjectProperty(IVwEnv vwenv, int hvo)
		{
			vwenv.AddObj(hvo, this, AtomicReferenceView.kFragObjName);
		}

		public int DisplayParentHvo
		{
			set
			{
				CheckDisposed();
				m_hvoDisplayParent = value;
			}
		}
	}
}

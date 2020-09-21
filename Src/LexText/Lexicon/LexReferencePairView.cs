// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// Summary description for LexReferencePairView.
	/// </summary>
	public class LexReferencePairView : AtomicReferenceView
	{
		protected ICmObject m_displayParent = null;

		public LexReferencePairView() : base()
		{
		}

		public override void SetReferenceVc()
		{
			CheckDisposed();

			m_atomicReferenceVc = new LexReferencePairVc(m_cache, m_rootFlid, m_displayNameProperty);
			if (m_displayParent != null)
				(m_atomicReferenceVc as LexReferencePairVc).DisplayParent = m_displayParent;
		}

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
	public class LexReferencePairVc : AtomicReferenceVc
	{
		protected ICmObject m_displayParent = null;

		public LexReferencePairVc(LcmCache cache, int flid, string displayNameProperty)
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
			if (m_displayParent != null && hvoItem == m_displayParent.Hvo)
				hvoItem = sda.get_VecItem(hvo, m_flid, 1);
			return hvoItem;
		}

		protected override void DisplayObjectProperty(IVwEnv vwenv, int hvo)
		{
			vwenv.AddObj(hvo, this, AtomicReferenceView.kFragObjName);
		}

		public ICmObject DisplayParent
		{
			set { m_displayParent = value; }
		}
	}
}

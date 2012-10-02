using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework.DetailControls;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// Summary description for LexReferenceCollectionView.
	/// </summary>
	public class LexReferenceCollectionView : VectorReferenceView
	{
		protected int m_hvoDisplayParent = 0;

		public LexReferenceCollectionView()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
		}

		protected override VectorReferenceVc CreateVectorReferenceVc()
		{
			LexReferenceCollectionVc vc = new LexReferenceCollectionVc(m_fdoCache, m_rootFlid, m_displayNameProperty, m_displayWs);
			if (m_hvoDisplayParent != 0)
				vc.DisplayParentHvo = m_hvoDisplayParent;
			return vc;
		}

		public int DisplayParentHvo
		{
			set
			{
				CheckDisposed();

				m_hvoDisplayParent = value;
				if (m_VectorReferenceVc != null)
					(m_VectorReferenceVc as LexReferenceCollectionVc).DisplayParentHvo = value;
			}
		}

		protected override void UpdateTimeStampsIfNeeded(int[] hvos)
		{
			for (int i = 0; i < hvos.Length; ++i)
			{
				ICmObject cmo = CmObject.CreateFromDBObject(m_fdoCache, hvos[i]);
				(cmo as CmObject).UpdateTimestampForVirtualChange();
			}
		}

		#region Component Designer generated code
		/// <summary>
		/// The Superclass handles everything except our Name property.
		/// </summary>
		private void InitializeComponent()
		{
			this.Name = "LexReferenceCollectionView";
		}
		#endregion
	}

	/// <summary>
	///  View constructor for creating the view details.
	/// </summary>
	public class LexReferenceCollectionVc : VectorReferenceVc
	{

		protected int m_hvoDisplayParent = 0;

		/// <summary>
		/// Constructor for the Vector Reference View Constructor Class.
		/// </summary>
		public LexReferenceCollectionVc(FdoCache cache, int flid, string displayNameProperty, string displayWs)
			: base (cache, flid, displayNameProperty, displayWs)
		{
		}

		/// <summary>
		/// Calling vwenv.AddObjVec() in Display() and implementing DisplayVec() seems to
		/// work better than calling vwenv.AddObjVecItems() in Display().  Theoretically
		/// this should not be case, but experience trumps theory every time.  :-) :-(
		/// </summary>
		public override void DisplayVec(IVwEnv vwenv, int hvo, int tag, int frag)
		{
			CheckDisposed();

			ISilDataAccess da = vwenv.DataAccess;
			int count = da.get_VecSize(hvo, tag);
			// Show everything in the collection except the current element from the main display.
			for (int i = 0; i < count; ++i)
			{
				int hvoItem = da.get_VecItem(hvo, tag, i);
				if (hvoItem == m_hvoDisplayParent)
					continue;
				vwenv.AddObj(hvoItem, this,	VectorReferenceView.kfragTargetObj);
				vwenv.AddSeparatorBar();
			}
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

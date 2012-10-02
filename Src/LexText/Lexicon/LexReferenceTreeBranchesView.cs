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
	/// Summary description for LexReferenceTreeBranchesView.
	/// </summary>
	public class LexReferenceTreeBranchesView : VectorReferenceView
	{
		public LexReferenceTreeBranchesView()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
		}

		protected override VectorReferenceVc CreateVectorReferenceVc()
		{
			return new LexReferenceTreeBranchesVc(m_fdoCache, m_rootFlid, m_displayNameProperty, m_displayWs);
		}

		protected override void Delete()
		{
			Delete(LexEdStrings.ksUndoDeleteRef, LexEdStrings.ksRedoDeleteRef);
		}

		protected override void UpdateTimeStampsIfNeeded(int[] hvos)
		{
#if WANTPORTMULTI
			for (int i = 0; i < hvos.Length; ++i)
			{
				ICmObject cmo = ICmObject.CreateFromDBObject(m_fdoCache, hvos[i]);
				(cmo as ICmObject).UpdateTimestampForVirtualChange();
			}
#endif
		}

		#region Component Designer generated code
		/// <summary>
		/// The Superclass handles everything except our Name property.
		/// </summary>
		private void InitializeComponent()
		{
			this.Name = "LexReferenceTreeBranchesView";
		}
		#endregion
	}

	/// <summary>
	///  View constructor for creating the view details.
	/// </summary>
	public class LexReferenceTreeBranchesVc : VectorReferenceVc
	{
		/// <summary>
		/// Constructor for the Vector Reference View Constructor Class.
		/// </summary>
		public LexReferenceTreeBranchesVc(FdoCache cache, int flid, string displayNameProperty, string displayWs)
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
			ISilDataAccess da = vwenv.DataAccess;
			int count = da.get_VecSize(hvo, tag);
			// Tree Branches consist of everything FOLLOWING the first element which is the Tree root.
			for (int i = 1; i < count; ++i)
			{
				vwenv.AddObj(da.get_VecItem(hvo, tag, i), this,
					VectorReferenceView.kfragTargetObj);
				vwenv.AddSeparatorBar();
			}
		}
	}
}

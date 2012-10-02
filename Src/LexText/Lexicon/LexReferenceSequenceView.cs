using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Data;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// Summary description for LexReferenceSequenceView.
	/// </summary>
	public class LexReferenceSequenceView : VectorReferenceView
	{
		protected int m_hvoDisplayParent = 0;

		public LexReferenceSequenceView()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
		}

		protected override VectorReferenceVc CreateVectorReferenceVc()
		{
			LexReferenceSequenceVc vc = new LexReferenceSequenceVc(m_fdoCache, m_rootFlid, m_displayNameProperty, m_displayWs);
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
					(m_VectorReferenceVc as LexReferenceSequenceVc).DisplayParentHvo = value;
			}
		}

		/// <summary>
		/// Currently only LexReferenceSequenceView displays a full sequence for lexical relations sequence.
		/// (e.g. Calendar), so we can use ReferenceSequenceUi for handling the moving of items through context menu.
		/// </summary>
		/// <param name="where"></param>
		/// <param name="hvo"></param>
		/// <returns></returns>
		protected override bool HandleRightClickOnObject(int hvo)
		{
			if (hvo == 0)
				return false;

			ReferenceBaseUi ui = new ReferenceSequenceUi(Cache, m_rootObj, m_rootFlid, hvo);
			if (ui != null)
			{
				//Debug.WriteLine("hvo=" + hvo.ToString() + " " + ui.Object.ShortName + "  " + ui.Object.ToString());
				return ui.HandleRightClick(Mediator, this, true);
			}

			return false;
		}

		/// <summary>
		/// This handles deleting the "owning" sense or entry from a calendar type lex
		/// reference by posting a message instead of simply removing the sense or entry from
		/// the reference vector.  This keeps things nice and tidy on the screen, and behaving
		/// like users would (or ought to) expect.  See LT-4114.
		/// </summary>
		/// <param name="sel"></param>
		/// <param name="dpt"></param>
		/// <returns></returns>
		public override VwDelProbResponse OnProblemDeletion(IVwSelection sel,
			VwDelProbType dpt)
		{
			CheckDisposed();

			int cvsli;
			int hvoObj;
			if (!CheckForValidDelete(sel, out cvsli, out hvoObj))
			{
				return VwDelProbResponse.kdprAbort;
			}
			else if (hvoObj == m_hvoDisplayParent)
			{
				// We need to handle this the same way as the delete command in the slice menu,
				// but can't do it directly because we've stacked up an undo handler.
				m_mediator.PostMessage("DataTreeDelete", null);
				return VwDelProbResponse.kdprDone;
			}
			else
			{
				return DeleteObjectFromVector(sel, cvsli, hvoObj);
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
			this.Name = "LexReferenceSequenceView";
		}
		#endregion
	}

	/// <summary>
	///  View constructor for creating the view details.
	/// </summary>
	public class LexReferenceSequenceVc : VectorReferenceVc
	{
		protected int m_hvoDisplayParent = 0;

		/// <summary>
		/// Constructor for the Vector Reference View Constructor Class.
		/// </summary>
		public LexReferenceSequenceVc(FdoCache cache, int flid, string displayNameProperty, string displayWs)
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
			// Show everything in the sequence including current element from the main display.
			for (int i = 0; i < count; ++i)
			{
				int hvoItem = da.get_VecItem(hvo, tag, i);
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

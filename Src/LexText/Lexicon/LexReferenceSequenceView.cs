using System.Diagnostics.CodeAnalysis;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework.DetailControls;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// Summary description for LexReferenceSequenceView.
	/// </summary>
	public class LexReferenceSequenceView : VectorReferenceView
	{
		protected ICmObject m_displayParent = null;

		public LexReferenceSequenceView()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
		}

		protected override VectorReferenceVc CreateVectorReferenceVc()
		{
			LexReferenceSequenceVc vc = new LexReferenceSequenceVc(m_fdoCache, m_rootFlid, m_displayNameProperty, m_displayWs);
			if (m_displayParent != null)
				vc.DisplayParent = m_displayParent;
			return vc;
		}

		public ICmObject DisplayParent
		{
			set
			{
				CheckDisposed();

				m_displayParent = value;
				if (m_VectorReferenceVc != null)
					(m_VectorReferenceVc as LexReferenceSequenceVc).DisplayParent = value;
			}
		}

		/// <summary>
		/// Currently only LexReferenceSequenceView displays a full sequence for lexical relations sequence.
		/// (e.g. Calendar), so we can use ReferenceSequenceUi for handling the moving of items through context menu.
		/// </summary>
		/// <param name="where"></param>
		/// <param name="hvo"></param>
		/// <returns></returns>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="See comment above 'new ReferenceSequenceUi()'")]
		protected override bool HandleRightClickOnObject(int hvo)
		{
			if (hvo == 0)
				return false;

			// We do NOT want a Using here. The temporary colleague created inside HandleRightClick should dispose
			// of the object. (Not working as of the time of writing, but disposing it makes a much more definite
			// problem, because it is gone before the user can choose one of the menu items. (FWR-2798 reopened)
			ReferenceBaseUi ui = new ReferenceSequenceUi(Cache, m_rootObj, m_rootFlid, hvo);
			ui.InitializeFlexComponent(PropertyTable, Publisher, Subscriber);
			return ui.HandleRightClick(this, true);
		}

		/// <summary>
		/// This handles deleting the "owning" sense or entry from a calendar type lex
		/// reference by posting a message instead of simply removing the sense or entry from
		/// the reference vector.  This keeps things nice and tidy on the screen, and behaving
		/// like users would (or ought to) expect.  See LT-4114.
		/// </summary>
		protected override void Delete()
		{
			var sel = m_rootb.Selection;
			int cvsli;
			int hvoObj;
			if (CheckForValidDelete(sel, out cvsli, out hvoObj))
			{
				if (m_displayParent != null && hvoObj == m_displayParent.Hvo)
				{
					// We need to handle this the same way as the delete command in the slice menu.
					Publisher.Publish("DataTreeDelete", null);
				}
				else
				{
					DeleteObjectFromVector(sel, cvsli, hvoObj, LexEdStrings.ksUndoDeleteRef, LexEdStrings.ksRedoDeleteRef);
				}
			}
		}

		protected override void UpdateTimeStampsIfNeeded(int[] hvos)
		{
#if WANTPORTMULTI
			for (int i = 0; i < hvos.Length; ++i)
			{
				ICmObject cmo = m_fdoCache.GetObject(hvos[i]);
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
			this.Name = "LexReferenceSequenceView";
		}
		#endregion
	}

	/// <summary>
	///  View constructor for creating the view details.
	/// </summary>
	public class LexReferenceSequenceVc : VectorReferenceVc
	{
		protected ICmObject m_displayParent = null;

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

		public ICmObject DisplayParent
		{
			set { m_displayParent = value; }
		}
	}
}

using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.IText;
using System.Drawing;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Discourse
{
	/// <summary>
	/// This class displays a one-line ribbon of interlinear text which keeps adding more at the end
	/// as stuff at the start gets moved into the main chart.
	/// </summary>
	public class InterlinRibbon : SimpleRootSite, IInterlinRibbon
	{
		internal const int kfragRibbonAnnotations = 2000000; // should be distinct from ones used in InterlinVc

		protected FdoCache m_cache;

		// If we are a plain InterlinRibbon, this is hvoStText.
		// If we are the DialogInterlinRibbon sub-class, this is hvoCca.
		int m_hvoRoot;

		protected int m_AnnotationListId;
		RibbonVc m_vc;
		InterlinLineChoices m_lineChoices;
		int m_iEndSelLim;
		int m_hvoEndSelLim;

		/// <summary>
		/// Make one.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoStText"></param>
		public InterlinRibbon(FdoCache cache, int hvoRoot) : base()
		{
			m_cache = cache;
			m_iEndSelLim = -1;
			m_hvoEndSelLim = 0;
			WritingSystemFactory = cache.LanguageWritingSystemFactoryAccessor;
			m_hvoRoot = hvoRoot;
			this.ReadOnlyView = true;
			this.ShowRangeSelAfterLostFocus = true;
		}

		internal InterlinLineChoices LineChoices
		{
			get { return m_lineChoices; }
			set { m_lineChoices = value; }
		}

		protected int HvoRoot
		{
			get { return m_hvoRoot; }
		}

		internal FdoCache Cache
		{
			get { return m_cache; }
		}

		public int EndSelLimitIndex
		{
			get { return m_iEndSelLim; }
			set { m_iEndSelLim = value; }
		}

		public int SelLimAnn
		{
			get { return m_hvoEndSelLim; }
			set { m_hvoEndSelLim = value; }
		}

		/// <summary>
		/// Return the annotations the user has selected.
		/// </summary>
		public int[] SelectedAnnotations
		{
			get
			{
				if (RootBox.Selection == null)
					return new int[0];
				TextSelInfo info = new TextSelInfo(RootBox);
				int anchor = info.ContainingObjectIndex(info.Levels(false) - 1, false);
				int end = info.ContainingObjectIndex(info.Levels(true) - 1, true);
				int first = Math.Min(anchor, end);
				int last = Math.Max(anchor, end);
				// JohnT: I don't know why this happens, but somehow we get a selection even when the view is empty.
				// And it yields negative anchor values (-1). We've also had crash reports (LT-7658) that appear to
				// result from trying to get an out-of-range item. I'm not sure how that can happen (it's not repeatable,
				// unfortunately) but put in some extra checks as defensive programming.
				int cannotations = m_cache.GetVectorSize(HvoRoot, AnnotationListId);
				first = Math.Min(first, cannotations - 1);
				last = Math.Min(last, cannotations - 1);
				if (first < 0 || last < 0)
					return new int[0];
				int[] result = new int[last - first + 1];
				for (int i = first; i <= last; i++)
				{
					result[i - first] = m_cache.GetVectorItem(HvoRoot, AnnotationListId, i);
				}
				return result;
			}
		}

		protected bool m_InSelectionChanged = false;

		/// <summary>
		/// This override ensures that we always have whole objects selected.
		/// Enhance: it may cause flicker during drag, in which case, we may change to only do it on mouse up,
		/// or only IF the mouse is up.
		/// </summary>
		/// <param name="prootb"></param>
		/// <param name="vwselNew"></param>
		public override void SelectionChanged(IVwRootBox prootb, IVwSelection vwselNew)
		{
			base.SelectionChanged(prootb, vwselNew);
			if (m_InSelectionChanged)
				return;
			if (RootBox.Selection == null)
				return;
			if (!(this is DialogInterlinRibbon)) // We want the selection in the dialog to behave differently.
			{
				TextSelInfo info = new TextSelInfo(RootBox);
				int end = Math.Max(info.ContainingObjectIndex(info.Levels(true) - 1, true),
					info.ContainingObjectIndex(info.Levels(false) - 1, false));
				SelectUpTo(end);
			}
		}

		protected void SelectUpTo(int end1)
		{
			if (HvoRoot == 0)
				return;
			int end = Math.Min(end1, m_cache.GetVectorSize(HvoRoot, AnnotationListId) - 1);
			if (end < 0)
				return;
			if (EndSelLimitIndex > -1 && EndSelLimitIndex < end)
				end = EndSelLimitIndex;
			try
			{
				m_InSelectionChanged = true;
				SelLevInfo[] levelsA = new SelLevInfo[1];
				levelsA[0].ihvo = 0;
				levelsA[0].tag = AnnotationListId;
				SelLevInfo[] levelsE = new SelLevInfo[1];
				levelsE[0].ihvo = end;
				levelsE[0].tag = AnnotationListId;
				RootBox.MakeTextSelInObj(0, 1, levelsA, 1, levelsE, false, false, false, true, true);
			}
			finally
			{
				m_InSelectionChanged = false;
			}
		}

		public void SetRoot(int hvoStText)
		{
			// Note: do not avoid  calling ChangeOrMakeRoot when hvoText == m_hvoRoot. The reconstruct
			// may be needed when the ribbon contents have changed, e.g., because objects were deleted
			// when the base text changed.
			m_hvoRoot = hvoStText;
			if (RootBox != null)
			{
				ChangeOrMakeRoot(HvoRoot, m_vc, kfragRibbonAnnotations, this.StyleSheet);
				MakeInitialSelection();
			}
		}

		public override void MakeRoot()
		{
			CheckDisposed();

			m_rootb = VwRootBoxClass.Create();
			m_rootb.SetSite(this);

			m_vc = new RibbonVc(this);

			if (m_lineChoices == null)
			{
				// fall-back (mainly for testing).
				m_lineChoices = new InterlinLineChoices(0, m_cache.DefaultAnalWs, m_cache.LangProject);
				m_lineChoices.Add(InterlinLineChoices.kflidWord);
				m_lineChoices.Add(InterlinLineChoices.kflidWordGloss);
			}
			m_vc.LineChoices = m_lineChoices;
			// may be needed..normally happens when the VC displays a top-level paragraph.
			//SetupRealVernWsForDisplay(m_cache.LangProject.ActualWs(LangProject.kwsVernInParagraph,
			//    hvo, (int)StText.StTextTags.kflidParagraphs));

			m_rootb.DataAccess = m_cache.MainCacheAccessor;
			m_rootb.SetRootObject(HvoRoot, m_vc, kfragRibbonAnnotations, this.StyleSheet);

			base.MakeRoot();
			m_rootb.Activate(VwSelectionState.vssOutOfFocus); // Makes selection visible even before ever got focus.\
			MakeInitialSelection();
		}

		public const string kAnnotationListClass = "StText";
		public const string kAnnotationListField = "CCUnchartedAnnotations";
		public virtual int AnnotationListId
		{
			get
			{
				if (m_AnnotationListId == 0)
				{
					m_AnnotationListId = DummyVirtualHandler.InstallDummyHandler(m_cache.VwCacheDaAccessor,
						kAnnotationListClass, kAnnotationListField,
						(int)FieldType.kcptReferenceSequence).Tag;
				}
				return m_AnnotationListId;
			}
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			MakeInitialSelection();
		}

		/// <summary>
		/// Suppress wrapping by allowing it to be as wide as desired.
		/// Todo: for RTL we will have to do something tricky about horizontal scrolling to see the actual text.
		/// </summary>
		/// <param name="prootb"></param>
		/// <returns></returns>
		public override int GetAvailWidth(IVwRootBox prootb)
		{
			return Int32.MaxValue;
		}

		#region IInterlinRibbon Members

		public virtual void MakeInitialSelection()
		{
			SelectFirstAnnotation();
		}

		public void SelectFirstAnnotation()
		{
			SelectUpTo(0);
		}

		#endregion

	}

	internal class RibbonVc : InterlinVc
	{
		InterlinRibbon m_ribbon;
		public RibbonVc(InterlinRibbon ribbon)
			: base(ribbon.Cache)
		{
			m_ribbon = ribbon;
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			switch (frag)
			{
				case InterlinRibbon.kfragRibbonAnnotations:
					if (hvo == 0)
						return;
					vwenv.OpenParagraph();
					vwenv.AddObjVecItems(m_ribbon.AnnotationListId, this, InterlinVc.kfragBundle);
					vwenv.CloseParagraph();
					break;
				case kfragBundle:
					if (m_ribbon.SelLimAnn == hvo)
					{
						vwenv.set_IntProperty((int)FwTextPropType.ktptPadTrailing,
							(int)FwTextPropVar.ktpvMilliPoint, 5000);
						vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTrailing,
							(int)FwTextPropVar.ktpvMilliPoint, 2000);
						vwenv.set_IntProperty((int)FwTextPropType.ktptBorderColor,
							(int)FwTextPropVar.ktpvDefault,
							(int)ColorUtil.ConvertColorToBGR(Color.Blue));
					}
					base.Display(vwenv, hvo, frag);
					break;
				default:
					base.Display(vwenv, hvo, frag);
					break;
			}
		}

		protected override void GetSegmentLevelTags(FdoCache cache)
		{
			// do nothing (we don't need tags above bundle level).
		}
	}

	/// <summary>
	/// Used to display interlinear text from a Constituent Chart Annotation in a dialog.
	/// </summary>
	public class DialogInterlinRibbon : InterlinRibbon
	{
		// In this subclass, we set the root later.
		public DialogInterlinRibbon(FdoCache cache) : base(cache, 0)
		{
		}

		public const string kovrAnnotationListClass = "CmAnnotation";
		public const string kovrAnnotationListField = "CCAWfics";

		public override int AnnotationListId
		{
			get
			{
				if (m_AnnotationListId == 0)
				{
					m_AnnotationListId = DummyVirtualHandler.InstallDummyHandler(m_cache.VwCacheDaAccessor,
						kovrAnnotationListClass, kovrAnnotationListField,
						(int)FieldType.kcptReferenceSequence).Tag;
				}
				return m_AnnotationListId;
			}
		}

		public override void MakeInitialSelection()
		{
			SelectUpToEnd();
		}

		private void SelectUpToEnd()
		{
			SelectUpTo(Cache.GetVectorSize(HvoRoot, AnnotationListId) - 1);
		}

		/// <summary>
		/// This override ensures that we always have whole objects selected.
		/// Enhance: it may cause flicker during drag, in which case, we may change to only do it on mouse up,
		/// or only IF the mouse is up.
		/// </summary>
		/// <param name="prootb"></param>
		/// <param name="vwselNew"></param>
		public override void SelectionChanged(IVwRootBox prootb, IVwSelection vwselNew)
		{
			base.SelectionChanged(prootb, vwselNew);
			if (m_InSelectionChanged)
				return;
			if (RootBox.Selection == null)
				return;
			TextSelInfo info = new TextSelInfo(RootBox);
			int end = Math.Max(info.ContainingObjectIndex(info.Levels(true) - 1, true),
				info.ContainingObjectIndex(info.Levels(false) - 1, false));
			int begin = Math.Min(info.ContainingObjectIndex(info.Levels(true) - 1, true),
				info.ContainingObjectIndex(info.Levels(false) - 1, false));
			SelectRange(begin, end);
		}

		private void SelectRange(int begin1, int end1)
		{
			if (HvoRoot == 0)
				return;
			int end = Math.Min(end1, m_cache.GetVectorSize(HvoRoot, AnnotationListId) - 1);
			int begin = Math.Min(begin1, end);
			if (end < 0 || begin < 0)
				return;
			try
			{
				m_InSelectionChanged = true;
				SelLevInfo[] levelsA = new SelLevInfo[1];
				levelsA[0].ihvo = begin;
				levelsA[0].tag = AnnotationListId;
				SelLevInfo[] levelsE = new SelLevInfo[1];
				levelsE[0].ihvo = end;
				levelsE[0].tag = AnnotationListId;
				RootBox.MakeTextSelInObj(0, 1, levelsA, 1, levelsE, false, false, false, true, true);
			}
			finally
			{
				m_InSelectionChanged = false;
			}
		}
	}
}

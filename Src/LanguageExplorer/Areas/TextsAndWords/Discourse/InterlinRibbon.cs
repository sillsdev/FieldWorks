// Copyright (c) 2008-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Areas.TextsAndWords.Discourse
{
	/// <summary>
	/// This class displays a one-line ribbon of interlinear text which keeps adding more at the end
	/// as stuff at the start gets moved into the main chart.
	/// </summary>
	internal class InterlinRibbon : InterlinDocRootSiteBase, IInterlinRibbon
	{
		internal const int kfragRibbonWordforms = 2000000; // should be distinct from ones used in InterlinVc

		#region Member Variables

		// If we are a plain InterlinRibbon, this is hvoStText.
		// If we are the DialogInterlinRibbon sub-class, this is hvoWordGroup.

		protected int m_occurenceListId = -2011; // flid for charting ribbon
		private int m_iEndSelLim;
		private AnalysisOccurrence m_endSelLimPoint;
		// Lazy initialization provides a chance for subclass to use its own
		// version of OccurenceListId
		private InterlinRibbonDecorator m_sda;
		protected bool m_InSelectionChanged;

		#endregion

		/// <summary />
		public InterlinRibbon(LcmCache cache, int hvoRoot)
		{
			Cache = cache;
			m_iEndSelLim = -1;
			m_endSelLimPoint = null;
			m_wsf = cache.WritingSystemFactory;
			HvoRoot = hvoRoot;
			ReadOnlyView = true;
			m_fShowRangeSelAfterLostFocus = true;
		}

		protected override void MakeVc()
		{
			Vc = new RibbonVc(this);
		}

		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
			}
			Vc = null;

			base.Dispose(disposing);
		}

		#region Properties

		protected internal InterlinLineChoices RibbonLineChoices
		{
			get { return LineChoices; }
			set { LineChoices = value; }
		}

		public virtual int OccurenceListId => m_occurenceListId;

		public ISilDataAccessManaged Decorator => m_sda ?? (m_sda = new InterlinRibbonDecorator(Cache, OccurenceListId));

		protected internal int HvoRoot { get; private set; }

		/// <summary>
		/// Setter handles PropChanged
		/// </summary>
		public int EndSelLimitIndex
		{
			get { return m_iEndSelLim; }
			set
			{
				m_iEndSelLim = value;
				EmitPropChanged();
			}
		}

		public AnalysisOccurrence SelLimOccurrence
		{
			get { return m_endSelLimPoint; }
			set
			{
				m_endSelLimPoint = value;
				EmitPropChanged();
			}
		}

		private void EmitPropChanged()
		{
			if (RootBox == null)
			{
				return;
			}
			var canns = Decorator.get_VecSize(HvoRoot, OccurenceListId);
			RootBox.PropChanged(HvoRoot, OccurenceListId, 0, canns, canns); // Pretend all are replaced
		}

		/// <summary>
		/// Return the wordforms the user has selected.
		/// </summary>
		public AnalysisOccurrence[] SelectedOccurrences
		{
			get
			{
				var myDeco = Decorator as InterlinRibbonDecorator;
				if (RootBox.Selection == null || myDeco == null)
				{
					return new AnalysisOccurrence[0];
				}
				var info = new TextSelInfo(RootBox);
				var anchor = info.ContainingObjectIndex(info.Levels(false) - 1, false);
				var end = info.ContainingObjectIndex(info.Levels(true) - 1, true);
				var first = Math.Min(anchor, end);
				var last = Math.Max(anchor, end);
				// JohnT: I don't know why this happens, but somehow we get a selection even when the view is empty.
				// And it yields negative anchor values (-1). We've also had crash reports (LT-7658) that appear to
				// result from trying to get an out-of-range item. I'm not sure how that can happen (it's not repeatable,
				// unfortunately) but put in some extra checks as defensive programming.
				var cwordforms = Decorator.get_VecSize(HvoRoot, OccurenceListId);
				first = Math.Min(first, cwordforms - 1);
				last = Math.Min(last, cwordforms - 1);
				if (first < 0 || last < 0)
				{
					return new AnalysisOccurrence[0];
				}
				var result = new AnalysisOccurrence[last - first + 1];
				for (var i = first; i <= last; i++)
				{
					var fakeHvo = Decorator.get_VecItem(HvoRoot, OccurenceListId, i);
					result[i - first] = myDeco.OccurrenceFromHvo(fakeHvo) as LocatedAnalysisOccurrence;
				}
				return result;
			}
		}

		public bool IsRightToLeft => Vc != null && Vc.RightToLeft;

		protected override void GetScrollOffsets(out int dxd, out int dyd)
		{
			base.GetScrollOffsets(out dxd, out dyd);
			if (IsRightToLeft)
			{
				dxd -= Width - RootBox.Width - 4; // 4 seems to be about right to keep the ribbon off the margin.
			}
		}

		#endregion

		/// <summary>
		/// Replaces cached ribbon words with input wordforms.
		/// Handles PropChanged, as UOW won't emit PropChanged to private Ribbon Decorator items.
		/// </summary>
		public void CacheRibbonItems(List<AnalysisOccurrence> wordForms)
		{
			var cwords = wordForms.Count;
			var fragList = new LocatedAnalysisOccurrence[cwords];
			for (var i = 0; i < cwords; i++)
			{
				var word = wordForms[i];
				var begOffset = word.GetMyBeginOffsetInPara();
				fragList[i] = new LocatedAnalysisOccurrence(word.Segment, word.Index, begOffset);
			}
			var oldLim = Decorator.get_VecSize(HvoRoot, OccurenceListId);
			Debug.Assert((Decorator as InterlinRibbonDecorator) != null, "No ribbon decorator!");
			((InterlinRibbonDecorator)Decorator).CacheRibbonItems(fragList);
			RootBox?.PropChanged(HvoRoot, OccurenceListId, 0, cwords, oldLim);
		}

		/// <summary>
		/// This override ensures that we always have whole objects selected.
		/// Enhance: it may cause flicker during drag, in which case, we may change to only do it on mouse up,
		/// or only IF the mouse is up.
		/// </summary>
		protected virtual void HandleSelectionChange(object sender, VwSelectionArgs args)
		{
			if (m_InSelectionChanged || RootBox.Selection == null)
			{
				return;
			}
			var info = new TextSelInfo(RootBox);
			var end = Math.Max(info.ContainingObjectIndex(info.Levels(true) - 1, true), info.ContainingObjectIndex(info.Levels(false) - 1, false));
			SelectUpTo(end);
		}

		protected void SelectUpTo(int end1)
		{
			if (HvoRoot == 0 || RootBox == null)
			{
				return;
			}
			var end = Math.Min(end1, Decorator.get_VecSize(HvoRoot, OccurenceListId) - 1);
			if (end < 0)
			{
				return;
			}
			if (EndSelLimitIndex > -1 && EndSelLimitIndex < end)
			{
				end = EndSelLimitIndex;
			}
			try
			{
				m_InSelectionChanged = true;
				var levelsA = new SelLevInfo[1];
				levelsA[0].ihvo = 0;
				levelsA[0].tag = OccurenceListId;
				var levelsE = new SelLevInfo[1];
				levelsE[0].ihvo = end;
				levelsE[0].tag = OccurenceListId;
				RootBox.MakeTextSelInObj(0, levelsA.Length, levelsA, levelsE.Length, levelsE, false, false, false, true, true);
			}
			finally
			{
				m_InSelectionChanged = false;
			}
		}

		protected override void AddDecorator()
		{
			RootBox.DataAccess = Decorator;
		}

		public override void SetRoot(int hvoStText)
		{
			// Note: do not avoid  calling ChangeOrMakeRoot when hvoText == _hvoRoot. The reconstruct
			// may be needed when the ribbon contents have changed, e.g., because objects were deleted
			// when the base text changed.
			HvoRoot = hvoStText;
			if (RootBox == null)
			{
				return;
			}
			SetRootInternal(hvoStText);
			ChangeOrMakeRoot(HvoRoot, Vc, kfragRibbonWordforms, StyleSheet);
			AddDecorator();
			MakeInitialSelection();
		}

		public override void MakeRoot()
		{
			base.MakeRoot();

			EnsureVc();
			if (LineChoices == null)
			{
				// fall-back (mainly for testing).
				LineChoices = new InterlinLineChoices(Cache.LangProject, Cache.DefaultVernWs, Cache.DefaultAnalWs);
				LineChoices.Add(InterlinLineChoices.kflidWord);
				LineChoices.Add(InterlinLineChoices.kflidWordGloss);
			}
			Vc.LineChoices = LineChoices;
			SetRootInternal(HvoRoot);

			RootBox.DataAccess = Decorator;
			RootBox.SetRootObject(HvoRoot, Vc, kfragRibbonWordforms, this.StyleSheet);

			RootBox.Activate(VwSelectionState.vssOutOfFocus); // Makes selection visible even before ever got focus.
			MakeInitialSelection();
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			MakeInitialSelection();
		}

		/// <summary>
		/// Called when the editing helper is created.
		/// </summary>
		protected override void OnEditingHelperCreated()
		{
			m_editingHelper.VwSelectionChanged += HandleSelectionChange;
		}

		/// <summary>
		/// Suppress wrapping by allowing it to be as wide as desired.
		/// Todo: for RTL we will have to do something tricky about horizontal scrolling to see the actual text.
		/// </summary>
		public override int GetAvailWidth(IVwRootBox prootb)
		{
			// 10 million is larger then ever likely to be needed and safe for using in views calculations
			return 10000000;
		}

		#region IInterlinRibbon Members

		public virtual void MakeInitialSelection()
		{
			SelectFirstOccurence();
		}

		public void SelectFirstOccurence()
		{
			SelectUpTo(0);
		}

		#endregion

		private sealed class RibbonVc : InterlinVc
		{
			private readonly InterlinRibbon m_ribbon;

			internal RibbonVc(InterlinRibbon ribbon)
				: base(ribbon.Cache)
			{
				m_ribbon = ribbon;
			}

			public override void Display(IVwEnv vwenv, int hvo, int frag)
			{
				switch (frag)
				{
					case kfragRibbonWordforms:
						if (hvo == 0)
						{
							return;
						}
						if (m_ribbon.IsRightToLeft)
						{
							vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
							vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalLeft);
						}
						vwenv.set_IntProperty((int)FwTextPropType.ktptMarginLeading, (int)FwTextPropVar.ktpvMilliPoint, 10000);
						vwenv.OpenDiv();
						vwenv.OpenParagraph();
						AddLabelPile(vwenv, m_cache, true);
						vwenv.AddObjVecItems(m_ribbon.OccurenceListId, this, InterlinVc.kfragBundle);
						vwenv.CloseParagraph();
						vwenv.CloseDiv();
						break;
					case kfragBundle:
						// Review: will this lead to multiple spurious blue lines?
						if (m_ribbon.SelLimOccurrence != null && m_ribbon.SelLimOccurrence.Analysis.Hvo == (m_ribbon.Decorator as InterlinRibbonDecorator).OccurrenceFromHvo(hvo).Analysis.Hvo)
						{
							vwenv.set_IntProperty((int)FwTextPropType.ktptPadTrailing, (int)FwTextPropVar.ktpvMilliPoint, 5000);
							vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTrailing, (int)FwTextPropVar.ktpvMilliPoint, 2000);
							vwenv.set_IntProperty((int)FwTextPropType.ktptBorderColor, (int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(Color.Blue));
						}
						base.Display(vwenv, hvo, frag);
						break;
					default:
						base.Display(vwenv, hvo, frag);
						break;
				}
			}

			/// <summary>
			/// In this case, the 'hvo' is a dummy for the cached AnalysisOccurrence.
			/// </summary>
			protected override void AddWordBundleInternal(int hvo, IVwEnv vwenv)
			{
				SetupAndOpenInnerPile(vwenv);
				var frag = (m_ribbon.Decorator as InterlinRibbonDecorator).OccurrenceFromHvo(hvo) as LocatedAnalysisOccurrence;
				DisplayAnalysisAndCloseInnerPile(vwenv, frag, false);
			}

			protected override void GetSegmentLevelTags(LcmCache cache)
			{
				// do nothing (we don't need tags above bundle level).
			}
		}
	}
}
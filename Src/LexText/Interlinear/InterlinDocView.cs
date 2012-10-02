using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing;
using System.ComponentModel;
using System.IO;
using System.Xml;
using System.Runtime.InteropServices;
using System.Text;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.XWorks;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.COMInterfaces;
using XCore;
using SIL.Utils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.WordWorks.Parser;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// InterlinDocView displays a complete interlinear text.
	/// </summary>
	public class InterlinDocView : RecordDocView
	{
		public InterlinDocView()
		{
		}
		protected override RootSite ConstructRoot()
		{
			return new InterlinDocChild();
		}
	}

	/// <summary>
	/// This is the main class for the interlinear text control view of a whole interlinear document
	/// </summary>
	public class InterlinDocChild : RootSite, IChangeRootObject, IVwNotifyChange
	{
		/// <summary>
		/// Use this to do the Add/RemoveNotifications, since it can be used in the unmanaged section of Dispose.
		/// (If m_sda is COM, that is.)
		/// Doing it there will be safer, since there was a risk of it not being removed
		/// in the managed section, as when disposing was done by the Finalizer.
		/// </summary>
		private ISilDataAccess m_sda;
		protected InterlinVc m_vc;
		internal int m_hvoRoot; // An IStText
		int m_hvoAnnotation; // Annotation currently being edited through overlay Sandbox; 0 if none.
		int m_hvoAnalysis; // Original analysis of m_hvoAnnotation when Sandbox starts to edit it.
		FocusBoxController m_focusBoxController; // Control for Sandbox.
		public event AnnotationSelectedEventHandler AnnnotationSelected;
		bool m_fForEditing; // true if we will be editing.
		bool m_fRootBoxNeedsUpdate = false;

		// These variables control enabling the "Add Free Translation," "Add Literal
		// Translation," and "Add Note buttons.
		bool m_fCanAddFreeTrans = false;
		bool m_fCanAddLitTrans = false;
		bool m_fCanAddNote = false;
		//private FreeTransEditMonitor m_ftMonitor;
		Dictionary<int, FreeTransEditMonitor> m_dictFtMonitor = new Dictionary<int, FreeTransEditMonitor>();

		/// <summary>
		/// This variable records the information needed to ensure the insertion point is placed
		/// on the correct line of a multilingual annotation when replacing a user prompt.
		/// See LT-9421.
		/// </summary>
		int m_cpropPrevForInsert = -1;

		/// <summary>
		/// This is the property that each 'in context' object has that points at one of the WfiX classes as the
		/// analysis of the word. When we go to the full model, it will be a property of CmAnnotation.
		/// </summary>
		public static int TagAnalysis
		{
			get { return (int)CmBaseAnnotation.CmAnnotationTags.kflidInstanceOf; }
			// (int) TxtWordformInContext.TxtWordformInContextTags.kflidAnalysis;
		}

		/// <summary>
		/// This is the property of StTxtPara that holds a list of annotation objects (the ones that have the
		/// TagAnalysis property).
		/// </summary>
		public static int TagAnnList
		{
			get { return (int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginObject; }
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoEntryToDisplay"></param>
		/// <param name="wsVern"></param>
		/// <param name="ler"></param>
		/// <returns></returns>
		static internal ITsString GetLexEntryTss(FdoCache cache, int hvoEntryToDisplay, int wsVern, ILexEntryRef ler)
		{
			LexEntryVc vcEntry = new LexEntryVc(cache);
			vcEntry.WritingSystemCode = wsVern;
			TsStringCollectorEnv collector = new TsStringCollectorEnv(null, cache.MainCacheAccessor, hvoEntryToDisplay);
			collector.RequestAppendSpaceForFirstWordInNewParagraph = false;
			vcEntry.Display(collector, hvoEntryToDisplay, (int)VcFrags.kfragHeadWord);
			if (ler != null)
				vcEntry.Display(collector, ler.Hvo, LexEntryVc.kfragVariantTypes);
			return collector.Result;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="morphBundle"></param>
		/// <param name="wsVern"></param>
		/// <returns></returns>
		static internal ITsString GetLexEntryTss(FdoCache cache, IWfiMorphBundle morphBundle, int wsVern)
		{
			LexEntryVc vcEntry = new LexEntryVc(cache);
			vcEntry.WritingSystemCode = wsVern;
			TsStringCollectorEnv collector = new TsStringCollectorEnv(null, cache.MainCacheAccessor, morphBundle.Hvo);
			collector.RequestAppendSpaceForFirstWordInNewParagraph = false;
			vcEntry.Display(collector, morphBundle.Hvo, (int)LexEntryVc.kfragEntryAndVariant);
			return collector.Result;
		}


		/// <summary>
		/// True if we will be doing editing (display sandbox, restrict field order choices, etc.).
		/// </summary>
		public bool ForEditing
		{
			get { return m_fForEditing; }
			set { m_fForEditing = value; }
		}

		/// <summary>
		/// Allow the sandbox to be a message target, if it is visible.
		/// </summary>
		/// <returns></returns>
		public override IxCoreColleague[] GetMessageTargets()
		{
			if (!IsFocusBoxInstalled || FocusBox.InterlinWordControl == null)
				return base.GetMessageTargets();
			return new IxCoreColleague[] { FocusBox.InterlinWordControl, this };
		}

		/// <summary>
		/// Override to turn off the trick that makes the VC leave a blank spot for the sandbox.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		public override bool OnPrint(object args)
		{
			int hvoSandbox = m_vc.SandboxAnnotation;
			try
			{
				m_vc.SandboxAnnotation = 0;
				return base.OnPrint(args);
			}
			finally
			{
				m_vc.SandboxAnnotation = hvoSandbox;
			}
		}

		/// <summary>
		/// Make one. Everything interesting happens when it is given a root object, however.
		/// </summary>
		/// <param name="para"></param>
		/// <param name="cache"></param>
		public InterlinDocChild()
			: base(null)
		{
			this.RightMouseClickedEvent += new FwRightMouseClickEventHandler(InterlinDocChild_RightMouseClickedEvent);
			this.DoSpellCheck = true;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			// Do this, before calling base.
			// m_sda COM object block removed due to crash in Finializer thread LT-6124

			if (disposing)
			{
				// Do this, before calling base.
				if (m_sda != null)
					m_sda.RemoveNotification(this);

				RightMouseClickedEvent -= new FwRightMouseClickEventHandler(InterlinDocChild_RightMouseClickedEvent);

				// make sure we're not visible, so we don't end up processing OnVisibleChanged
				// during base.Dispose();
				this.Visible = false;
				DisposeFtMonitor();
			}

			base.Dispose(disposing);

			if (disposing)
			{
				// This is actually harmful to do, because the InterlinDocChild does not yet know it is
				// Disposing, and tries to Layout when the Sandbox is removed from its child list,
				// and tries to recreate its handle and root box, and eventually crashes.
				// RandyR: I don't think it will be harmful in this new context.
				if (m_focusBoxController != null)
				{
					m_focusBoxController.Resize -= new EventHandler(m_focusBox_Resize);

					if (!Controls.Contains(FocusBox))
						FocusBox.Dispose();
				}

				if (m_vc != null)
					m_vc.Dispose();
			}
			m_sda = null;
			m_focusBoxController = null;
			m_vc = null;
		}

		public int HvoAnnotation
		{
			get { return m_hvoAnnotation; }
			set
			{
				m_hvoAnnotation = value;
				if (m_vc != null)
					m_vc.SandboxAnnotation = value;
			}
		}

		internal int HvoAnalysis
		{
			get { return m_hvoAnalysis; }
			set
			{
				m_hvoAnalysis = value;
			}
		}

		/// <summary>
		/// Return annotation Hvo of the current focus box or commented segment.
		/// </summary>
		/// <returns></returns>
		public virtual int AnnotationHvoClosestToSelection()
		{
			int hvoAnnotation = 0;

			if (this.HvoAnnotation != 0)
			{
				// this should be the CmBaseAnnotation for the wordform in the Focus Box.
				// Verify this is a CmBaseAnnotation before we return.
				hvoAnnotation = this.HvoAnnotation;
			}
			else
			{
				// see if our selection is in a comment annotation.
				//Debug.Assert(m_rootb != null);  this can happen for brand-new projects.  See LT-9660.
				if (m_rootb == null)
					return 0;
				IVwSelection sel = m_rootb.Selection;
				if (sel == null)
					return 0;

				// Out variables for AllTextSelInfo.
				int cvsli = sel.CLevels(false);
				cvsli--; // CLevels includes the string property itself, but AllTextSelInfo doesn't need it.
				int ihvoRoot;
				int tagTextProp;
				int cpropPrevious;
				int ichAnchor;
				int ichEnd;
				int ws;
				bool fAssocPrev;
				int ihvoEnd;
				ITsTextProps ttpBogus;
				// Main array of information retrived from sel.
				SelLevInfo[] rgvsli = SelLevInfo.AllTextSelInfo(sel, cvsli,
					out ihvoRoot, out tagTextProp, out cpropPrevious, out ichAnchor, out ichEnd,
					out ws, out fAssocPrev, out ihvoEnd, out ttpBogus);

				if (tagTextProp == (int)CmAnnotation.CmAnnotationTags.kflidComment)
				{
					// get the CmBaseAnnotation object for this comment (e.g. sentence-segment).
					Debug.Assert(rgvsli.Length > 1);
					hvoAnnotation = rgvsli[1].hvo;		// Verify this is CmBaseAnnotation below.
				}
			}
			if (!HvoIsRealBaseAnnotation(hvoAnnotation))
				return 0;
			return hvoAnnotation;
		}

		protected bool HvoIsRealBaseAnnotation(int hvoAnnotation)
		{
			if (hvoAnnotation <= 0 && !Cache.IsDummyObject(hvoAnnotation))
				return false;
			// Verify the hvo we are examining is for a CmBaseAnnotation.
			int clsid = Cache.GetClassOfObject(hvoAnnotation);
			if (clsid != CmBaseAnnotation.kClassId)
				return false;
			return true;
		}

		// Object hvoTarget is wanted as a leaf on a tree rooted at hvoStart, where
		// the first level branches are found by following property tags[0], the
		// second level by following tags[1] from the objects at the second level, and
		// so on. Return a path to the object in the form of the indexes that
		// must be used in each property to find the target, or -1 (and null) if not found.
		// Also returns the corresponding parent objects (hvos[0] = hvoStart).
		// Thus, hvos[n+1] is the value of property tags[n] of object hvos[n] at index indexes[n].
		// hvoTarget is the value of property tags[ctags] of object hvos[ctags] at index indexes[ctags].
		// The return value (equal to indexes[indexes.Length -1) is where the object
		// was found in the final property.
		int IndexOf(int hvoStart, int[] tags, int hvoTarget, out int[] indexes, out int[] hvos)
		{
			if (hvoTarget == 0)
			{
				indexes = hvos = null;
				return -1;
			}
			ISilDataAccess sda = m_rootb.DataAccess;
			indexes = new int[tags.Length];
			hvos = new int[tags.Length];
			if (FindNestedObject(hvoStart, tags, hvoTarget, indexes, hvos, 0, sda))
				return indexes[indexes.Length - 1];
			else
			{
				indexes = null;
				return -1;
			}
		}

		// Recursive helper method for IndexOf. Handles searching one property of one object (and
		// its children) starting at a particular depth in the tree.
		bool FindNestedObject(int hvoStart, int[] tags, int hvoTarget, int[] indexes, int[] hvos, int idepth,
			ISilDataAccess sda)
		{
			int tag = tags[idepth];
			int chvo = sda.get_VecSize(hvoStart, tag);
			for (int i = 0; i < chvo; ++i)
			{
				int hvo = sda.get_VecItem(hvoStart, tag, i);
				if (idepth == tags.Length - 1)
				{
					if (hvo == hvoTarget)
					{
						indexes[idepth] = i;
						hvos[idepth] = hvoStart;
						return true;
					}
				}
				else
				{
					if (FindNestedObject(hvo, tags, hvoTarget, indexes, hvos, idepth + 1, sda))
					{
						indexes[idepth] = i;
						hvos[idepth] = hvoStart;
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Select the word indicated by the text-wordform-in-context (twfic) annotation.
		/// Note that this does not save any changes made in the Sandbox. It is mainly used
		/// when the view is read-only.
		/// </summary>
		/// <param name="hvoAnn"></param>
		public virtual void SelectAnnotation(int hvoAnn)
		{
			// If we're already displaying this one don't do anything. Avoids lots of flicker.
			if (m_hvoAnnotation == hvoAnn && IsFocusBoxInstalled)
				return;
			if (!m_vc.CanBeAnalyzed(hvoAnn))
				return;
			ISilDataAccess sda = Cache.MainCacheAccessor;
			int annInstanceOfRAHvo = sda.get_ObjectProp(hvoAnn, (int)CmAnnotation.CmAnnotationTags.kflidInstanceOf);
#if DEBUG
			// We should assert that ann is Twfic
			int twficType = CmAnnotationDefn.Twfic(Cache).Hvo;
			int annoType = sda.get_ObjectProp(hvoAnn, (int)CmAnnotation.CmAnnotationTags.kflidAnnotationType);
			Debug.Assert(annoType == twficType, "Given annotation type should be twfic("
				+ twficType + ") but was " + annoType + ".");
			Debug.Assert(annInstanceOfRAHvo != 0, "Given annotation must have analysis.");
#endif
			HideAndDestroyFocusBox(false);
			// This may be overkill, but if we don't refresh here,
			// the previous sandbox may become invisible, or the new sandbox misaligned.
			// It's important to destroy the sandbox first, otherwise, RefreshDisplay
			// will destroy AND RECREATE it in the old position. Also, by default
			// RefreshDisplay will save changes. We don't want to do that here,
			// because any changes are probably phony (such as from one with morphology
			// to one without, because morphology is not visible).
			//RefreshDisplay();
			// JohnT: RefreshDisplay is bad, because (a) it does a Reconstruct(), which is expensive;
			// and (b) it resets the current annotation, so we almost get recursive.
			// I think all we need here is to update the display so everything visible is properly positioned.
			// Something even weaker that just ensured all boxes are real and laid out would
			// probalbly do, in fact.
			//this.Update();
			TriggerAnnotationSelected(hvoAnn, annInstanceOfRAHvo, true);
		}

		/// <summary>
		/// Something about the display of hvoAnnotation has changed...perhaps it has become or ceased to
		/// be the current annotation displayed using the Sandbox, or the Sandbox changed size. Produce
		/// a PropChanged that makes the system think it has been replaced (with itself) to refresh the
		/// relevant part of the display.
		/// </summary>
		/// <param name="hvoAnnotation"></param>
		void SimulateReplaceAnnotation(int hvoAnnotation)
		{
			int[] indexes;
			int[] hvos;
			ISilDataAccess sda = m_rootb.DataAccess;
			int ihvoOld = IndexOf(m_hvoRoot,
				new int[] { (int)StText.StTextTags.kflidParagraphs, m_vc.ktagParaSegments, m_vc.ktagSegmentForms },
				hvoAnnotation, out indexes, out hvos);
			if (ihvoOld == -1)
			{
				Debug.WriteLine("tried to replace annotation, but could not find it");
				return;
			}
			// Simluate replacing the wordform in the relevant segment with itself. This lets the VC Display method run again, this
			// time possibly getting a different answer about whether hvoAnnotation is the current annotation, or about the
			// size of the Sandbox.
			sda.PropChanged(null, (int)PropChangeType.kpctNotifyAll, hvos[2], m_vc.ktagSegmentForms, indexes[2], 1, 1);
		}

		// Set the size of the sandbox on the VC...if it exists yet.
		void SetSandboxSize()
		{
			// Make the focus box the size it really needs to be for the current object.
			if (m_focusBoxController != null)
			{
				try
				{
					// This will adjust its size, but we already know we're trying to do that,
					// so we don't want the notification...it attempts work we're already in
					// the middle of and may confuse things or produce flashing.
					m_focusBoxController.Resize -= new EventHandler(m_focusBox_Resize);
					m_focusBoxController.AdjustSizeAndLocationForControls(true);
				}
				finally
				{
					m_focusBoxController.Resize += new EventHandler(m_focusBox_Resize);
				}
			}
			SetSandboxSizeForVc();
			// This should make it big enough not to scroll.
			//if (FocusBox.InterlinWordControl != null && FocusBox.InterlinWordControl.RootBox != null)
			//	FocusBox.InterlinWordControl.Size = new Size(FocusBox.InterlinWordControl.RootBox.Width + 1, FocusBox.InterlinWordControl.RootBox.Height + 1);
		}

		// Set the VC size to match the sandbox. Return true if it changed.
		bool SetSandboxSizeForVc()
		{
			if (m_vc == null || ExistingFocusBox == null)
				return false;
			//FocusBox.PerformLayout();
			int dpiX, dpiY;
			using (Graphics g = CreateGraphics())
			{
				dpiX = (int)g.DpiX;
				dpiY = (int)g.DpiY;
			}
			int width = FocusBox.Width;
			if (width > 10000)
			{
				//				Debug.Assert(width < 10000); // Is something taking the full available width of MaxInt/2?
				width = 500; // arbitrary, may allow something to work more or less
			}
			Size newSize = new Size(width * 72000 / dpiX,
				FocusBox.Height * 72000 / dpiY);
			if (newSize.Width == m_vc.SandboxSize.Width && newSize.Height == m_vc.SandboxSize.Height)
				return false;

			m_vc.SandboxSize = newSize;
			return true;
		}

		/// <summary>
		/// Return the selection that corresponds to the SandBox position.
		/// </summary>
		/// <returns></returns>
		internal IVwSelection MakeSandboxSel()
		{
			if (m_hvoRoot == 0 || m_hvoAnnotation == 0)
				return null;
			return SelectWficInIText(m_hvoAnnotation);
		}

		protected internal IVwSelection SelectWficInIText(int hvoAnn)
		{
			Debug.Assert(hvoAnn != 0);
			Debug.Assert(m_hvoRoot != 0);

			int[] indexes;
			int[] hvos;
			int[] tags = new int[] { (int)StText.StTextTags.kflidParagraphs,
									   m_vc.ktagParaSegments,
									   m_vc.ktagSegmentForms };
			ISilDataAccess sda = m_rootb.DataAccess;
			int ihvoOld = IndexOf(m_hvoRoot, tags, hvoAnn, out indexes, out hvos);
			if (ihvoOld == -1)
			{
				Debug.WriteLine("could not find annotation");
				return null;
			}
			// Now use that to make a selection.
			SelLevInfo[] rgvsli = new SelLevInfo[3];
			rgvsli[0].ihvo = indexes[2]; // 0 specifies where wf is in segment.
			rgvsli[0].tag = tags[2];
			rgvsli[1].ihvo = indexes[1]; // 1 specifies where segment is in para
			rgvsli[1].tag = tags[1];
			rgvsli[2].ihvo = indexes[0]; // 2 specifies were para is in IStText.
			rgvsli[2].tag = tags[0];
			// top prop is atomic, leave index 0. Specifies displaying the contents of the Text.
			IVwSelection sel = null;
			try
			{
				if (this is InterlinPrintChild || this is InterlinTaggingChild)
				{
					// InterlinPrintChild and InterlinTaggingChild have no Sandbox,
					// so they need a "real" interlinear text selection.
					sel = RootBox.MakeTextSelInObj(0, rgvsli.Length, rgvsli, 0, null,
						false, false, false, true, true);
				}
				else
				{
					// This is fine for InterlinDocChild, since it treats the area that the sandbox will fill
					// as a picture. Not so good for InterlinPrintChild (see above).
					sel = RootBox.MakeSelInObj(0, rgvsli.Length, rgvsli, 0, false);
				}
			}
			catch (Exception e)
			{
				Debug.WriteLine(e.StackTrace);
				return null;
			}
			return sel;
		}

		/// <summary>
		/// Subclasses can override if they handle the scrolling for OnSizeChanged().
		/// </summary>
		protected override bool ScrollToSelectionOnSizeChanged
		{
			get { return false; }
		}

		/// <summary>
		/// Resizing the window may cause the sandbox to move.  Make sure it does so
		/// correctly.  (See LT-3836.)
		/// </summary>
		/// <param name="e"></param>
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			// (LT-5932) do this in InterlinMaster.OnLayout to help avoid 'random' crash.
			//MoveFocusBoxIntoPlace();
		}

		//

		/// <summary>
		/// Move the sand box to the appropriate place.
		/// Note: if we're already in the process of MoveSandbox, let's not do anything. It may crash
		/// if we try it again (LT-5932).
		/// </summary>
		private bool m_fMovingSandbox = false;
		internal void MoveFocusBoxIntoPlace()
		{
			if (m_fMovingSandbox)
				return;
			try
			{
				m_fMovingSandbox = true;
				IVwSelection sel = MakeSandboxSel();
				// The sequence is important here. Even without doing this scroll, the sandbox is always
				// visible: I think .NET must automatically scroll to make the focused control visible,
				// or maybe we have some other code I've forgotten about that does it. But, if we don't
				// both scroll and update, the position we move the sandbox to may be wrong, after the
				// main window is fully painted, with possible position changes due to expanding lazy stuff.
				// If you change this, be sure to test that in a several-page interlinear text, with the
				// Sandbox near the bottom, you can turn 'show morphology' on and off and the sandbox
				// ends up in the right place.
				this.ScrollSelectionIntoView(sel, VwScrollSelOpts.kssoDefault);
				Update();
				if (sel == null)
				{
					Debug.WriteLine("could not select annotation");
					return;
				}
				Point ptLoc = GetSandboxSelLocation(sel);
				if (ExistingFocusBox != null && FocusBox.Location != ptLoc)
					FocusBox.Location = ptLoc;
			}
			finally
			{
				m_fMovingSandbox = false;
			}
		}

		/// <summary>
		/// Some of the stuff that tries to make sure the focus box is showing either doesn't happen
		/// or doesn't work if it occurs before the windows is made visible. To be sure, check it
		/// really is visible and in the right place when we become visible.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnVisibleChanged(EventArgs e)
		{
			base.OnVisibleChanged(e);
			if (this.Visible && !(this is InterlinPrintChild)) // disable focus box for PrintView
				MoveFocusBoxIntoPlace();
		}

		/// summary>
		/// Get the location of the given selection, presumably that of a Sandbox.
		/// /summary>
		Point GetSandboxSelLocation(IVwSelection sel)
		{
			Debug.Assert(sel != null);
			Rect rcPrimary = GetPrimarySelRect(sel);
			// The location includes margins, so for RTL we need to adjust the
			// Sandbox so it isn't hard up against the next word.
			// Enhance JohnT: ideally we would probably figure this margin
			// to exactly match the margin between words set by the VC.
			int left = rcPrimary.left;
			if (m_vc.RightToLeft)
				left += 8;
			return new Point(left, rcPrimary.top);
		}

		// Get the primary rectangle occupied by a selection (relative to the top left of the client rectangle).
		private Rect GetPrimarySelRect(IVwSelection sel)
		{
			Rect rcPrimary;
			using (new HoldGraphics(this))
			{
				Rectangle rcSrcRoot, rcDstRoot;
				GetCoordRects(out rcSrcRoot, out rcDstRoot);
				Rect rcSec;
				bool fSplit, fEndBeforeAnchor;
				sel.Location(m_graphicsManager.VwGraphics, rcSrcRoot, rcDstRoot, out rcPrimary,
							 out rcSec, out fSplit, out fEndBeforeAnchor);
			}
			return rcPrimary;
		}

		/// summary>
		/// Receives the broadcast message "PropertyChanged"
		/// /summary>
		public override void OnPropertyChanged(string name)
		{
			switch (name)
			{
				case "ShowMorphBundles":
					bool fShowMorphBundles = m_mediator.PropertyTable.GetBoolProperty("ShowMorphBundles", true);
					if (fShowMorphBundles == m_vc.ShowMorphBundles)
						return; // no real change
					m_vc.ShowMorphBundles = fShowMorphBundles;
					ReconstructAndRecreateSandbox(true);
					break;
				default:
					base.OnPropertyChanged(name);
					break;
			}
		}

		/// <summary>
		/// Recreate the Sandbox when something significant has changed, typically the
		/// configuration of lines to show. The caller will usually call UpdateRealFromSandbox()
		/// first, typically before changing whatever requires the Recreate.
		/// </summary>
		internal Sandbox RecreateSandbox(int hvoAnn, int hvoAnalysis)
		{
			TryHideFocusBox();
			if (FocusBox.InterlinWordControl != null)
				FocusBox.InterlinWordControl.Dispose();
			FocusBox.InterlinWordControl = null;
			m_hvoAnnotation = 0;
			// Recreates Sandbox in new form and location.
			TriggerAnnotationSelected(hvoAnn, hvoAnalysis, false);  // abandon saving guess
			return FocusBox.InterlinWordControl;
		}

		// REVIEW: It seems rather odd that we have both PrepareToGoAway() and
		// OnConsideringClosing() methods. Are they doing the same job?

		/// <summary>
		/// This name is magic for an xCoreColleague that is active at the time when an xWindow is being closed.
		/// If some active colleague implements this method, it gets a chance to do something special as the
		/// xWindow closes (and can veto the close, though we aren't really using that here).
		/// </summary>
		/// <returns></returns>
		public bool OnConsideringClosing(object sender, CancelEventArgs arg)
		{
			arg.Cancel = !PrepareToGoAway();
			return arg.Cancel;
		}

		public bool OnPrepareToRefresh(object args)
		{
			// Currently PrepareToGoAway gets called first on a Refresh, so use that instead.
			return false; // other things may wish to prepare too.
		}

		// This method is normally part of IxCoreContentControl but we are not yet claiming to
		// implement that...we just have the method, which InterlinMaster calls to give us
		// a chance to save any last changes.
		public bool PrepareToGoAway()
		{
			// suspend layout, so we won't try to update the display during HideAndDestroyFocusBox
			// (see crash in LT-5932).
			this.SuspendLayout();
			// save sandbox information now, because Refresh will invalidate any dummy object data.
			HideAndDestroyFocusBox(false);
			this.Visible = false;
			return true;
		}

		/// <summary>
		/// Let InterlinMaster.ShowRecord() restore the Sandbox location after a Refresh.
		/// </summary>
		public override void RefreshDisplay()
		{
			base.RefreshDisplay();
		}

		private void HideAndDestroyFocusBox(bool fSaveGuess)
		{
			if (IsFocusBoxInstalled)
				HideFocusBox(fSaveGuess);
			if (ExistingFocusBox != null)
			{
				FocusBox.Dispose();
				FocusBox = null;
			}
		}

		/// <summary>
		/// tell the user that we're in a bad state and need to refresh in hopes to fix things.
		/// </summary>
		internal void MessageBoxMasterRefresh()
		{
			// show message box warning user before we refresh everything.
			MessageBox.Show(ITextStrings.SelectedObjectHasBeenDeleted,
				ITextStrings.DeletedObjectDetected,
				MessageBoxButtons.OK, MessageBoxIcon.Warning);
			HvoAnalysis = 0;
			HvoAnnotation = 0;
			Mediator.BroadcastMessage("MasterRefresh", null);

		}


		/// <summary>
		/// Move the sandbox (see main method), making the default selection.
		/// </summary>
		/// <param name="hvoAnnotation"></param>
		/// <param name="hvoAnalysis"></param>
		/// <param name="fSaveGuess">if true, saves guesses; if false, skips guesses but still saves edits.</param>
		public void TriggerAnnotationSelected(int hvoAnnotation, int hvoAnalysis, bool fSaveGuess)
		{
			TriggerAnnotationSelected(hvoAnnotation, hvoAnalysis, fSaveGuess, true);
		}

		/// <summary>
		/// Move the sandbox to the annotation hvoAnnotation, currently analyzed as hvoAnalysis
		/// (which may be a WfiWordform, WfiAnalysis, or WfiGloss).
		/// </summary>
		/// <param name="hvoAnnotation"></param>
		/// <param name="hvoAnalysis"></param>
		/// <param name="fSaveGuess">if true, saves guesses; if false, skips guesses but still saves edits.</param>
		/// <param name="fMakeDefaultSelection">true to make the default selection within the
		/// new sandbox.</param>
		public virtual void TriggerAnnotationSelected(int hvoAnnotation, int hvoAnalysis,
			bool fSaveGuess, bool fMakeDefaultSelection)
		{
			// This can happen, though it is rare...see LT-8193.
			if (hvoAnnotation == 0)
				return;
			StTxtPara.TwficInfo twficInfo = new StTxtPara.TwficInfo(Cache, hvoAnnotation);
			if (!twficInfo.IsObjectValid())
			{
				this.MessageBoxMasterRefresh();
				return;
			}
			if (Cache.IsDummyObject(hvoAnnotation))
			{
				// convert this to a real object.
				using (SuppressSubTasks supressActionHandler = new SuppressSubTasks(Cache, true))
				{
					int hvoAnnDummy = hvoAnnotation;
					CmBaseAnnotation realAnn = CmObject.ConvertDummyToReal(Cache, hvoAnnotation) as CmBaseAnnotation;
					hvoAnnotation = realAnn.Hvo;
					// re-cache items important to this view, without which some things like guesses may not appear
					// until after a refresh.
					TryCacheRealWordForm(hvoAnnotation);
					TryCacheLowercasedForm(hvoAnnotation);
				}
			}
			//if (hvoAnnotation != m_hvoAnnotation)
			//{
			TryHideFocusBox();
			Sandbox sandbox = ChangeOrCreateSandbox(hvoAnnotation, ref hvoAnalysis, fSaveGuess);
			FocusBox.InterlinWordControl = sandbox;
			if (!Controls.Contains(FocusBox))
				Controls.Add(FocusBox); // Makes it real and may give it a root box.
			SetSandboxSize();
			int hvoOldAnnotation = m_hvoAnnotation;
			HvoAnnotation = hvoAnnotation;
			m_hvoAnalysis = hvoAnalysis;
			SimulateReplaceAnnotation(hvoOldAnnotation);
			SimulateReplaceAnnotation(m_hvoAnnotation);
			MoveFocusBoxIntoPlace();
			// Now it is the right size and place we can show it.
			TryShowFocusBox();
			sandbox.Focus();
			CheckForFreeOrLitAnnotations(MakeSandboxSel());
			if (m_fCanAddFreeTrans)
			{
				// add a free translation for this segment,
				// but don't change the selection from the sandbox.
				AddFreeTrans(false);
			}
			if (fMakeDefaultSelection)
				FocusBox.InterlinWordControl.MakeDefaultSelection();
			//}
			if (AnnnotationSelected != null)
				AnnnotationSelected(this, new AnnotationSelectedArgs(hvoAnnotation, hvoAnalysis));
		}

		/// <summary>
		/// Change root of Sandbox or create it; Lay it out and figure its size;
		/// tell m_vc the size.
		/// </summary>
		/// <param name="hvoAnnotation"></param>
		/// <param name="hvoAnalysis"></param>
		/// <param name="fSaveGuess">if true, saves guesses; if false, skips guesses but still saves edits.</param>
		/// <param name="fTreatAsSentenceInitial"></param>
		/// <returns></returns>
		private Sandbox ChangeOrCreateSandbox(int hvoNewAnnotation, ref int hvoNewAnalysis, bool fSaveGuess)
		{
			Sandbox sandbox = FocusBox.InterlinWordControl;
			if (sandbox == null)
			{
				sandbox = CreateNewSandbox(hvoNewAnnotation, hvoNewAnalysis);
			}
			else
			{
				// if hvoNewAnalysis is the same as m_hvoAnalysis,
				// it may now be invalid if UpdateRealFromSandbox() made it real.
				if (hvoNewAnalysis == m_hvoAnalysis &&
					Cache.IsDummyObject(m_hvoAnalysis))
				{
					Debug.Fail("Any basic wordform in the FocusBox should now already be a real wordform.");
					int hvoAnalysisOld = m_hvoAnalysis;
					int hvoAnalysisOldClassId = Cache.GetClassOfObject(m_hvoAnalysis);
					UpdateRealFromSandbox(fSaveGuess, hvoNewAnnotation, hvoNewAnalysis);
					if (m_hvoAnalysis != hvoAnalysisOld && m_hvoAnalysis >= 0)
					{
						Debug.Assert(hvoAnalysisOldClassId == WfiWordform.kClassId);
						// use the real wordform from new analysis.
						hvoNewAnalysis = GetWordformHvoOfAnalysis(m_hvoAnalysis);
						this.RootBox.Reconstruct();	// sync rootbox selections to new ids.
					}
				}
				else
				{
					UpdateRealFromSandbox(fSaveGuess, hvoNewAnnotation, hvoNewAnalysis);
				}
				ChangeSandboxRoot(sandbox, hvoNewAnnotation, hvoNewAnalysis);
			}
			return sandbox;
		}

		private void ChangeSandboxRoot(Sandbox sandbox, int hvoNewAnnotation, int hvoNewAnalysis)
		{
			sandbox.Visible = false;
			sandbox.SwitchWord(hvoNewAnnotation, true);
		}

		private Sandbox CreateNewSandbox(int hvoNewAnnotation, int hvoNewAnalysis)
		{
			Sandbox sandbox = new Sandbox(this.Cache, this.Mediator, this.StyleSheet,
				m_vc.LineChoices, hvoNewAnnotation, this);
			sandbox.SizeToContent = true; // Layout will ignore size.
			//sandbox.Mediator = Mediator;
			sandbox.ShowMorphBundles = m_vc.ShowMorphBundles;
			sandbox.StyleSheet = this.StyleSheet;
			sandbox.Visible = false;
			return sandbox;
		}

		/// <summary>
		/// indicates whether the focus box exists and is in our controls.
		/// </summary>
		public bool IsFocusBoxInstalled
		{
			get { return ExistingFocusBox != null && this.Controls.Contains(FocusBox); }
		}

		/// <summary>
		/// Return focus box if it exists.
		/// </summary>
		public FocusBoxController ExistingFocusBox
		{
			get { return m_focusBoxController; }
		}

		/// <summary>
		/// returns the focus box for the interlinDoc if it exists or can be created.
		/// </summary>
		internal FocusBoxController FocusBox
		{
			get
			{
				if (ExistingFocusBox == null && ForEditing)
				{
					m_focusBoxController = new FocusBoxController();
					m_focusBoxController.Resize += new EventHandler(m_focusBox_Resize);
				}
				return m_focusBoxController;
			}
			set
			{
				m_focusBoxController = value;
			}
		}


		private int GetWordformHvoOfAnalysis(int hvoAnalysis)
		{
			if (hvoAnalysis == 0)
				return 0;
			switch (Cache.GetClassOfObject(hvoAnalysis))
			{
				case WfiWordform.kclsidWfiWordform:
					return hvoAnalysis;
				case WfiAnalysis.kclsidWfiAnalysis:
					return Cache.GetOwnerOfObject(hvoAnalysis);
				case WfiGloss.kclsidWfiGloss:
					return Cache.GetOwnerOfObject(Cache.GetOwnerOfObject(hvoAnalysis));
				default:
					throw new Exception("Invalid type found in word analysis annotation");
			}
		}

		public override IVwStylesheet StyleSheet
		{

			set
			{
				base.StyleSheet = value;
				if (m_vc != null)
					m_vc.StyleSheet = value;
				if (ExistingFocusBox != null && FocusBox.InterlinWordControl != null)
					FocusBox.InterlinWordControl.StyleSheet = this.StyleSheet;
			}
		}


		/// <summary>
		///  Answer true if the indicated selection is within a single freeform annotation we can delete.
		/// </summary>
		/// <param name="sel"></param>
		/// <returns></returns>
		private bool CanDeleteFF(IVwSelection sel, out int hvoAnnotation)
		{
			hvoAnnotation = 0;
			if (sel == null)
				return false;
			ITsString tss;
			int ichEnd, hvoEnd, tagEnd, wsEnd;
			bool fAssocPrev;
			sel.TextSelInfo(true, out tss, out ichEnd, out fAssocPrev, out hvoEnd, out tagEnd, out wsEnd);
			int ichAnchor, hvoAnchor, tagAnchor, wsAnchor;
			sel.TextSelInfo(false, out tss, out ichAnchor, out fAssocPrev, out hvoAnchor, out tagAnchor, out wsAnchor);
			if (hvoEnd != hvoAnchor || tagEnd != tagAnchor || wsEnd != wsAnchor)
				return false; // must be a one-property selection
			if (tagAnchor != (int)CmAnnotation.CmAnnotationTags.kflidComment)
				return false; // must be a selection in a freeform annotation.
			hvoAnnotation = hvoAnchor;
			return true;
		}
		/// <summary>
		/// Intercepts the 'Delete Record' command otherwise handled by the record clerk (which
		/// offers to delete the whole record). If in a free translation, delete that.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns>true if handled</returns>
		public bool OnDeleteRecord(object commandObject)
		{
			// if we're not in document view or FocusBox is installed, return.
			if (ExistingFocusBox == null || IsFocusBoxInstalled)
				return false; // no special behavior, let clerk handle it.

			using (UndoRedoCommandHelper undoRedoTask = new UndoRedoCommandHelper(Cache, commandObject as Command))
			{
				IVwSelection sel = RootBox.Selection;
				return DeleteFreeform(sel);
			}
		}

		private bool DeleteFreeform(IVwSelection sel)
		{
			int hvoAnnotation;
			if (!CanDeleteFF(sel, out hvoAnnotation))
				return false;
			int hvoSeg, tagFF, ihvoFF, cpropPrevious;
			IVwPropertyStore vps;
			// NOTE: Do not use ihvoFF for updating the cache directly, because the display vector indices
			// does not necessarily correspond to the cache vector indices.
			sel.PropInfo(false, 1, out hvoSeg, out tagFF, out ihvoFF, out cpropPrevious, out vps);
			CmBaseAnnotation seg = new CmBaseAnnotation(Cache, hvoSeg);
			CmIndirectAnnotation ft = new CmIndirectAnnotation(Cache, hvoAnnotation);
			bool wasFt = ft.AnnotationTypeRAHvo == Cache.GetIdFromGuid(LangProject.kguidAnnFreeTranslation);
			StTxtPara para = seg.BeginObjectRA as StTxtPara;
			m_fdoCache.DeleteObject(hvoAnnotation);
			m_fdoCache.PropChanged(null, PropChangeType.kpctNotifyAll, hvoSeg, tagFF, ihvoFF, 0, 1);
			if (wasFt && para != null)
				FreeTransEditMonitor.UpdateMainTransFromSegmented(para, Cache.LangProject.CurAnalysisWssRS.HvoArray);
			return true; // handled
		}

		private bool IsInterlinearMode()
		{
			InterlinMaster ilm = GetMaster();
			if (ilm == null)
				return false;
			else
				return ilm.InterlinearTabPageIsSelected();
		}

		/// <summary>
		/// Enable the "Add Free Translation" command.  Currently only in the 'interlin master'
		/// view used for editing.
		/// </summary>
		public bool OnDisplayAddFreeTrans(object commandObject,
			ref UIItemDisplayProperties display)
		{
			// When LexText is closing, this gets called several times when there is no Parent
			bool fIsInterlinear = IsInterlinearMode();
			display.Visible = fIsInterlinear;
			display.Enabled = fIsInterlinear && m_fCanAddFreeTrans;
			return true;
		}

		/// <summary>
		/// Enable the 'insert word glosses' command
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		public bool OnDisplayAddWordGlossesToFreeTrans(object commandObject, ref UIItemDisplayProperties display)
		{
			int dummy1, dummy2;
			display.Visible = display.Enabled = CanAddWordGlosses(out dummy1, out dummy2);
			return true;
		}

		/// <summary>
		/// Answer whether the AddWordGlossesToFreeTranslation menu option should be enabled.
		/// Also get the FT to which they can be added.
		/// </summary>
		/// <param name="hvoFt"></param>
		/// <returns></returns>
		private bool CanAddWordGlosses(out int hvoFt, out int ws)
		{
			hvoFt = ws = 0; // actually meaningless unless it returns true, but make compiler happy.
			// LT-9389: 2nd part of AND allows this to work in Tagging tab.
			if (!IsInterlinearMode() && (this as InterlinTaggingChild == null))
				return false; // not sure this can happen
			if (RootBox.Selection == null)
				return false;
			if (!Focused)
				return false;
			ITsString tss;
			int ich;
			int tag;
			bool fAssocPrev;
			RootBox.Selection.TextSelInfo(false, out tss, out ich, out fAssocPrev, out hvoFt, out tag, out ws);
			if (tag != SimpleRootSite.kTagUserPrompt)
				return false; // no good if not in a free annotation
			int hvoType = Cache.GetObjProperty(hvoFt, (int)CmAnnotation.CmAnnotationTags.kflidAnnotationType);
			if (hvoType != Cache.GetIdFromGuid(new Guid(LangProject.kguidAnnFreeTranslation))
				&& hvoType != Cache.GetIdFromGuid(new Guid(LangProject.kguidAnnLiteralTranslation)))
				return false; // and must be specifically a Free or Literal translation annotation.
			int dummy;
			if (ws == 0) // a prompt, use ws of first character.
				ws = tss.get_Properties(0).GetIntPropValues((int)FwTextPropType.ktptWs, out dummy);
			return true;
		}

		/// <summary>
		/// Enable the "Add Literal Translation" command.  Currently only in the 'interlin
		/// master' view used for editing.
		/// </summary>
		/// <param name="parameters"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayAddLitTrans(object commandObject,
			ref UIItemDisplayProperties display)
		{
			//Note: Do not assign Visible to Enabled
			bool fIsInterlinear = IsInterlinearMode();
			display.Visible = fIsInterlinear;
			display.Enabled = fIsInterlinear && m_fCanAddLitTrans;
			return true;
		}

		private bool m_fInSelectionChanged; // true while executing SelectionChanged.
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Notifies the site that something about the selection has changed.
		/// </summary>
		/// <param name="prootb"></param>
		/// <param name="vwselNew">Selection</param>
		/// <remarks>When overriding you should call the base class first.</remarks>
		/// -----------------------------------------------------------------------------------
		public override void SelectionChanged(IVwRootBox prootb, IVwSelection vwselNew)
		{
			if (m_fInSelectionChanged)
				return; // don't need to reprocess our own changes.
			m_fInSelectionChanged = true;
			try
			{
				base.SelectionChanged(prootb, vwselNew);
				IVwSelection sel = vwselNew;
				if (!sel.IsValid)
					sel = prootb.Selection;
				if (sel == null)
					return;
				CheckForFreeOrLitAnnotations(sel);

				SelectionHelper helper = SelectionHelper.Create(sel, prootb.Site);
				// Check whether the selection is on the proper line of a multilingual
				// annotation and, if not, fix it.  See LT-9421.
				if (m_cpropPrevForInsert > 0 && !sel.IsRange &&
					(helper.GetNumberOfPreviousProps(SelectionHelper.SelLimitType.Anchor) == 0 ||
					 helper.GetNumberOfPreviousProps(SelectionHelper.SelLimitType.End) == 0))
				{
					try
					{
						helper.SetNumberOfPreviousProps(SelectionHelper.SelLimitType.Anchor, m_cpropPrevForInsert);
						helper.SetNumberOfPreviousProps(SelectionHelper.SelLimitType.End, m_cpropPrevForInsert);
						helper.MakeBest(true);
						m_cpropPrevForInsert = -1;	// we've used this the one time it was needed.
					}
					catch (Exception exc)
					{
						if (exc != null)
							Debug.WriteLine(String.Format(
								"InterlinDocChild.SelectionChanged() trying to display prompt in proper line of annotation: {0}", exc.Message));
					}
				}
				int flid = helper.GetTextPropId(SelectionHelper.SelLimitType.Anchor);

				//Fixes LT-9884 Crash when clicking on the blank space in Text & Words--->Print view area!
				if (helper.LevelInfo.Length == 0)
					return;
				int hvo = helper.LevelInfo[0].hvo;

				// If the selection is in a freeform or literal translation that is empty, display the prompt.
				if (SelIsInEmptyTranslation(helper, flid, hvo) && !m_rootb.IsCompositionInProgress)
				{
					m_vc.SetActiveFreeform(helper.LevelInfo[0].hvo, helper.Ws, helper.NumberOfPreviousProps);
					helper.SetTextPropId(SelectionHelper.SelLimitType.Anchor, SimpleRootSite.kTagUserPrompt);
					helper.SetTextPropId(SelectionHelper.SelLimitType.End, SimpleRootSite.kTagUserPrompt);
					helper.NumberOfPreviousProps = 0; // only ever one occurrence of prompt.
					helper.SetNumberOfPreviousProps(SelectionHelper.SelLimitType.End, 0);
					// Even though the helper method is called MakeRangeSelection, it will initially make
					// an IP, because we haven't set any different offset for the end.
					// Since it's at the start of the prompt, we need it to associate with the prompt,
					// not the preceding (zero width direction-control) character.
					helper.AssocPrev = false;
					try
					{
						sel = helper.MakeRangeSelection(m_rootb, true);
						sel.ExtendToStringBoundaries();
					}
					// Prevent the crash described in LT-9399 by swallowing the exception.
					catch (Exception exc)
					{
						if (exc != null)
							Debug.WriteLine(String.Format(
								"InterlinDocChild.SelectionChanged() trying to display prompt for empty translation: {0}", exc.Message));
					}
				}
				else if (flid != SimpleRootSite.kTagUserPrompt)
				{
					m_vc.SetActiveFreeform(0, 0, 0); // clear any current prompt.
				}
				// do not extend the selection for a user prompt if the user is currently entering an IME composition,
				// since we are about to switch the prompt to a real comment field
				else if (helper.GetTextPropId(SelectionHelper.SelLimitType.End) == SimpleRootSite.kTagUserPrompt
					&& !m_rootb.IsCompositionInProgress)
				{
					// If the selection is entirely in a user prompt then extend the selection to cover the
					// entire prompt. This covers changes within the prompt, like clicking within it or continuing
					// a drag while making it.
					sel.ExtendToStringBoundaries();
					EditingHelper.SetKeyboardForSelection(sel);
				}
			}
			finally
			{
				m_fInSelectionChanged = false;
			}
		}

		private bool SelIsInEmptyTranslation(SelectionHelper helper, int flid, int hvo)
		{
			if (helper.IsRange)
				return false; // range can't be in empty comment.
			if (flid != (int)CmAnnotation.CmAnnotationTags.kflidComment)
				return false; // translation is always a comment.
			if (helper.GetTss(SelectionHelper.SelLimitType.Anchor).Length != 0)
				return false; // translation is non-empty.
			int hvoType = Cache.GetObjProperty(hvo, (int)CmAnnotation.CmAnnotationTags.kflidAnnotationType);
			return (hvoType == m_vc.FtSegmentDefn || hvoType == m_vc.LtSegmentDefn);
		}


		/// <summary>
		/// Visit each segment and determine whether or not to add an initial free translation line.
		/// </summary>
		void PopulateInitialFreeTranslationLines()
		{
			if (!ForEditing)
				return;	// only populate free translations
			// Collect all the segments in the text and convert any dummies to real ones.
			List<int> segmentsInText = CollectAllSegmentsInTextAndEnsureReal();

			// Load all the freeform data for the segments in the text.
			List<int> segsMissingFreeTrans = CollectSegmentsMissingFreeTranslations(segmentsInText);

			// Now create a free translation line for each segment missing one.
			AddFreeTranslations(segsMissingFreeTrans);
		}

		private void AddFreeTranslations(List<int> segsMissingFreeTrans)
		{
			// since this is not connected to a user action, we don't want this to be undoable.
			BaseFreeformAdder bffa = new BaseFreeformAdder(Cache);
			using (new SuppressSubTasks(Cache, true))
			{
				foreach (int hvoSeg in segsMissingFreeTrans)
				{
					bffa.AddFreeformAnnotation(hvoSeg, m_vc.FtSegmentDefn);
				}
			}
		}

		private List<int> CollectSegmentsMissingFreeTranslations(List<int> segmentsInText)
		{
			// Note, since we only really care about FreeTranslations, we just need to load data for analyses wss.
			List<int> segsMissingFreeTrans = new List<int>();
			Set<int> allAnalWsIds = new Set<int>(Cache.LangProject.AnalysisWssRC.HvoArray);
			StTxtPara.LoadSegmentFreeformAnnotationData(Cache, new Set<int>(segmentsInText), allAnalWsIds);
			foreach (int hvoSeg in segmentsInText)
			{
				// assume this segment is missing a FT until we find one.
				int hvoSegMissingFT = hvoSeg;
				foreach (int hvoFF in Cache.GetVectorProperty(hvoSeg, m_vc.ktagSegFF, true))
				{
					int hvoTypeFF = Cache.GetObjProperty(hvoFF,
						(int)CmAnnotation.CmAnnotationTags.kflidAnnotationType);
					if (hvoTypeFF == m_vc.FtSegmentDefn)
					{
						// found a free translation. go to the next segment.
						hvoSegMissingFT = 0;
						break;
					}
				}
				if (hvoSegMissingFT != 0)
					segsMissingFreeTrans.Add(hvoSegMissingFT);
			}
			return segsMissingFreeTrans;
		}

		private List<int> CollectAllSegmentsInTextAndEnsureReal()
		{
			List<int> segmentsInText = new List<int>();
			if (m_hvoRoot == 0)
				return segmentsInText;
			IStText stText = this.RawStText;
			foreach (IStTxtPara para in stText.ParagraphsOS)
			{
				List<int> hvoSegs = para.Segments;
				foreach (int hvoSeg in hvoSegs)
				{
					if (Cache.IsDummyObject(hvoSeg))
					{
						ICmBaseAnnotation cba = CmBaseAnnotation.ConvertBaseAnnotationToReal(Cache, hvoSeg);
						segmentsInText.Add(cba.Hvo);
					}
					else
					{
						segmentsInText.Add(hvoSeg);
					}
				}
			}
			return segmentsInText;
		}

		/// <summary>
		/// Check whether Free Translation and/or Literal Translation annotations already exist
		/// for the segment containing the given selection.
		/// </summary>
		/// <param name="vwsel"></param>
		private void CheckForFreeOrLitAnnotations(IVwSelection sel)
		{
			m_fCanAddFreeTrans = false;
			m_fCanAddLitTrans = false;
			m_fCanAddNote = false;
			if (sel == null || !m_fForEditing)
				return;

			int cvsli = sel.CLevels(false);
			// CLevels includes the string property itself, but AllTextSelInfo doesn't need
			// it.
			cvsli--;
			// Out variables for AllTextSelInfo.
			int ihvoRoot;
			int tagTextProp;
			int cpropPrevious;
			int ichAnchor;
			int ichEnd;
			int ws;
			bool fAssocPrev;
			int ihvoEnd;
			ITsTextProps ttpBogus;
			// Main array of information retrived from sel that made combo.
			SelLevInfo[] rgvsli = SelLevInfo.AllTextSelInfo(sel, cvsli,
				out ihvoRoot, out tagTextProp, out cpropPrevious, out ichAnchor, out ichEnd,
				out ws, out fAssocPrev, out ihvoEnd, out ttpBogus);
			// Identify the segment.
			// This is important because although we are currently displaying just an
			// StTxtPara, eventually it might be part of a higher level structure.  We want
			// this to work no matter how much higher level structure there is.
			int itagSegments = -1;
			for (int i = rgvsli.Length; --i >= 0; )
			{
				if (rgvsli[i].tag == m_vc.ktagParaSegments)
				{
					itagSegments = i;
					break;
				}
			}
			if (itagSegments == -1)
				return;

			// All the above, just to get hvoSeg!
			int hvoSeg = rgvsli[itagSegments].hvo;
			ISilDataAccess sda = Cache.MainCacheAccessor;
			int cFreeForm = sda.get_VecSize(hvoSeg, m_vc.ktagSegFF);
			m_fCanAddFreeTrans = true;
			m_fCanAddLitTrans = true;
			m_fCanAddNote = true;
			for (int i = 0; i < cFreeForm; i++)
			{
				int hvoAnn = sda.get_VecItem(hvoSeg, m_vc.ktagSegFF, i);
				int hvo = sda.get_ObjectProp(hvoAnn,
					(int)CmAnnotation.CmAnnotationTags.kflidAnnotationType);
				// Only one Free Translation annotation and one Literal Translation annotation
				// is allowed for each segment!
				if (hvo == m_vc.FtSegmentDefn)
					m_fCanAddFreeTrans = false;
				if (hvo == m_vc.LtSegmentDefn)
					m_fCanAddLitTrans = false;
			}
		}

		/// <summary>
		/// Enable the "Configure Interlinear" command. Can be done any time this view is a target.
		/// </summary>
		/// <param name="parameters"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayConfigureInterlinear(object commandObject,
			ref UIItemDisplayProperties display)
		{
			display.Visible = true;
			display.Enabled = true;
			return true;
		}

		/// <summary>
		///  Launch the Configure interlinear dialog and deal with the results
		/// </summary>
		/// <param name="argument"></param>
		public bool OnConfigureInterlinear(object argument)
		{
			ConfigureInterlinDialog dlg = new ConfigureInterlinDialog(m_fdoCache,
				m_vc.LineChoices.Clone() as InterlinLineChoices);
			if (dlg.ShowDialog(this) == DialogResult.OK)
			{
				UpdateForNewLineChoices(dlg.Choices);
			}

			return true; // We handled this
		}

		/// <summary>
		/// the line choices configured for this document.
		/// </summary>
		internal InterlinLineChoices LineChoices
		{
			get { return m_vc.LineChoices; }
		}

		/// <summary>
		/// Persist the new line choices and
		/// Reconstruct the document based on the given newChoices for interlinear lines.
		/// </summary>
		/// <param name="newChoices"></param>
		private void UpdateForNewLineChoices(InterlinLineChoices newChoices)
		{
			m_vc.LineChoices = newChoices;
			m_lineChoices = newChoices;
			m_mediator.PropertyTable.SetProperty(ConfigPropName,
				m_vc.LineChoices.Persist(m_fdoCache.LanguageWritingSystemFactoryAccessor),
				PropertyTable.SettingsGroup.LocalSettings);
			UpdateDisplayForNewLineChoices();
		}

		/// <summary>
		/// Do whatever is necessary to display new line choices.
		/// </summary>
		private void UpdateDisplayForNewLineChoices()
		{
			if (m_rootb == null)
				return;
			ReconstructAndRecreateSandbox(true);
		}

		/// <summary>
		/// (EricP) This may be slow and jiggle scrollbar position, but it does help provide a stable
		/// solution to a number of issues with having to significantly update the FocusBox and the
		/// interlinear document.
		///
		/// Examples:
		/// In making and breaking phrases (or undo/redoing those actions, a reconstruct() provides
		/// 1) guesses in new or restored interlindoc words and
		/// 2) makes sure the FocusBox is restored to the right place in the document and doesn't float
		/// somewhere else.
		/// If you choose to rework the areas depending upon this, please test these areas heavily.
		/// </summary>
		/// <param name="fUpdateRealFromSandbox"></param>
		/// <param name="hvoAnn"></param>
		/// <param name="hvoAnalysis"></param>
		private void ReconstructAndRecreateSandbox(bool fUpdateRealFromSandbox, int hvoAnn, int hvoAnalysis)
		{
			if (fUpdateRealFromSandbox)
				UpdateRealFromSandbox();
			m_rootb.Reconstruct();
			if (hvoAnn != 0 && hvoAnalysis != 0)
				RecreateSandbox(hvoAnn, hvoAnalysis);
		}

		internal void ReconstructAndRecreateSandbox(bool fUpdateRealFromSandbox)
		{
			ReconstructAndRecreateSandbox(fUpdateRealFromSandbox, this.HvoAnnotation, this.HvoAnalysis);
		}


		/// <summary>
		/// Enable the "Add Note" command. Currently enabled when Add Free Trans is.
		/// </summary>
		/// <param name="parameters"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayAddNote(object commandObject, ref UIItemDisplayProperties display)
		{
			//Note: Do not assign Visible to Enabled
			bool fIsInterlinear = IsInterlinearMode();
			display.Visible = fIsInterlinear;
			display.Enabled = fIsInterlinear && m_fCanAddNote;
			return true;
		}

		public bool OnDisplayExportInterlinear(object commandObject, ref UIItemDisplayProperties display)
		{
			if (m_hvoRoot != 0)
				display.Enabled = true;
			else
				display.Enabled = false;
			display.Visible = true;
			return true;
		}

		public bool OnExportInterlinear(object argument)
		{
			// If the currently selected text is from Scripture, then we need to give the dialog
			// the list of Scripture texts that have been selected for interlinearization.
			Control parent = this.Parent;
			while (parent != null && !(parent is InterlinMaster))
				parent = parent.Parent;
			InterlinMaster master = parent as InterlinMaster;
			List<int> selectedHvos = null;
			if (master != null)
			{
				InterlinearTextsRecordClerk clerk = master.Clerk as InterlinearTextsRecordClerk;
				if (clerk != null)
					selectedHvos = clerk.GetScriptureIds();
			}
			int oldAnnotation = HvoAnnotation;
			HideAndDestroyFocusBox(true);
			using (InterlinearExportDialog dlg = new InterlinearExportDialog(m_mediator, m_hvoRoot, m_vc, selectedHvos))
			{
				dlg.ShowDialog(this);
			}
			if (oldAnnotation != 0)
			{
				int hvoAnalysis = m_fdoCache.MainCacheAccessor.get_ObjectProp(oldAnnotation, (int)CmAnnotation.CmAnnotationTags.kflidInstanceOf);
				TriggerAnnotationSelected(oldAnnotation, hvoAnalysis, false);
			}

			return true; // we handled this
		}

		/// <summary>
		/// The 1 is added to this so it will NOT be called by reflection. It is now the InterlinMaster
		/// view that implements the command (so it can be enabled even when the interlin view isn't
		/// in focus). That method delegates to this, but if this has the same name, it also gets
		/// called directly when the child DOES have focus, so TWO FTs get added.
		/// It implements the 'Add Free Translation' menu item.
		/// </summary>
		/// <param name="argument"></param>
		public void OnAddFreeTrans1(object argument)
		{
			AddFreeTrans(true);
		}

		public void OnAddWordGlossesToFreeTrans(object argument)
		{
			int hvoFt, ws;
			if (!CanAddWordGlosses(out hvoFt, out ws))
				return;

			int wsText = Cache.LangProject.ActualWs(LangProject.kwsVernInParagraph,
				m_hvoRoot, (int)StText.StTextTags.kflidParagraphs);

			CmIndirectAnnotation ft = CmObject.CreateFromDBObject(Cache, hvoFt) as CmIndirectAnnotation;
			CmBaseAnnotation seg = ft.AppliesToRS[0] as CmBaseAnnotation;
			int ktagSegmentForms = InterlinVc.SegmentFormsTag(Cache);
			int ktagTwficDefault = InterlinVc.TwficDefaultTag(Cache);
			int[] xfics = Cache.GetVectorProperty(seg.Hvo, ktagSegmentForms, true);
			ITsStrBldr bldr = TsStrBldrClass.Create();
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			bool fOpenPunc = false;
			foreach (int hvoXfic in xfics)
			{
				ITsString insert;
				int hvoItem = Cache.GetObjProperty(hvoXfic, (int)CmAnnotation.CmAnnotationTags.kflidInstanceOf);
				CmBaseAnnotation cba = CmObject.CreateFromDBObject(Cache, hvoXfic) as CmBaseAnnotation;
				ITsString tss = cba.TextAnnotated;
				if (hvoItem == 0)
				{
					// Pfic...insert its text.
					// Ensure the punctuation is in the proper analysis writing system.  See LT-9971.
					string sPunc = tss.Text;
					if (sPunc == "\xfffc")
						continue; // footnote probably...should we do something different?
					fOpenPunc = false;
					if (sPunc.Length > 0)
					{
						char ch = sPunc[0];
						System.Globalization.UnicodeCategory ucat = Char.GetUnicodeCategory(ch);
						if (ucat == System.Globalization.UnicodeCategory.InitialQuotePunctuation ||
							ucat == System.Globalization.UnicodeCategory.OpenPunctuation)
						{
							sPunc = sPunc.Insert(0, " ");
							fOpenPunc = true;
						}
					}
					insert = tsf.MakeString(sPunc, ws);
				}
				else
				{
					int clid = Cache.GetClassOfObject(hvoItem);
					if (clid == (int)WfiGloss.kclsidWfiGloss)
					{
						insert = Cache.GetMultiStringAlt(hvoItem, (int)WfiGloss.WfiGlossTags.kflidForm, ws);
					}
					else if (clid == (int)WfiWordform.kclsidWfiWordform &&
						StringUtils.GetWsAtOffset(tss, 0) != wsText)
					{
						insert = tss;
					}
					else
					{
						// check if we have a guess cached with a gloss. (LT-9973)
						int hvoGuess = Cache.GetObjProperty(hvoXfic, ktagTwficDefault);
						// if we don't have a guess or the guess is not a gloss, then skip this one.
						if (hvoGuess == 0 || Cache.GetClassOfObject(hvoGuess) != WfiGloss.kclsidWfiGloss)
							continue;
						insert = Cache.GetMultiStringAlt(hvoGuess, (int)WfiGloss.WfiGlossTags.kflidForm, ws);
					}
					if (bldr.Length > 0 && insert.Length > 0 && !fOpenPunc)
						bldr.Replace(bldr.Length, bldr.Length, " ", null);
					fOpenPunc = false;
				}
				if (insert.Length == 0)
					continue;
				bldr.ReplaceTsString(bldr.Length, bldr.Length, insert);
			}
			// Replacing the string when the new one is empty is useless, and may cause problems,
			// e.g., LT-9416, though I have not been able to reproduce that.
			if (bldr.Length == 0)
				return;
			// if we're trying to replace a user prompt, record which line of a multilingual annotation
			// is being changed.  See LT-9421.
			SetCpropPreviousForInsert();
			RootBox.Selection.ReplaceWithTsString(bldr.GetString());
		}

		private void AddFreeTrans(bool fSetFocusInFreeformAnnotation)
		{
			AddFreeform(m_vc.FtSegmentDefn, InterlinLineChoices.kflidFreeTrans, fSetFocusInFreeformAnnotation);
			m_fCanAddFreeTrans = false;
		}

		/// <summary>
		/// Implements the 'Add Literal Translation' menu item. See comments for OnAddFreeTrans1.
		/// </summary>
		/// <param name="argument"></param>
		public void OnAddLitTrans1(object argument)
		{
			AddFreeform(m_vc.LtSegmentDefn, InterlinLineChoices.kflidLitTrans);
			m_fCanAddLitTrans = false;
		}

		/// <summary>
		/// Implements the 'Add Note' menu item. See comments for OnAddFreeTrans1.
		/// </summary>
		/// <param name="argument"></param>
		public void OnAddNote1(object argument)
		{
			AddFreeform(m_vc.NoteSegmentDefn, InterlinLineChoices.kflidNote);
		}

		/// <summary>
		/// Add a freeform annotation of the specified type to the current segment.
		/// By default, sets the selection in the new Freeform annotation and hides sandbox.
		/// Also, defines the new Freeform annotation as undoable.
		/// </summary>
		/// <param name="hvoType"></param>
		/// <param name="flid"></param>
		internal void AddFreeform(int hvoType, int flid)
		{
			AddFreeform(hvoType, flid, true);
		}

		private void AddFreeform(int hvoType, int flid, bool fMakeSelectionInNewFreeformAnnotation)
		{
			bool fNeedReconstruct = false;
			if (m_vc.LineChoices.IndexOf(flid) < 0)
			{
				m_vc.LineChoices.Add(flid);
				fNeedReconstruct = true;
				// Save the new set of line choices.  See LT-6715.
				m_mediator.PropertyTable.SetProperty(ConfigPropName,
					m_vc.LineChoices.Persist(m_fdoCache.LanguageWritingSystemFactoryAccessor), PropertyTable.SettingsGroup.LocalSettings);
			}
			new FreeformAdder(hvoType, this, fNeedReconstruct, m_vc).Run(fMakeSelectionInNewFreeformAnnotation);
			if (fMakeSelectionInNewFreeformAnnotation)
			{
				DestroyFocusBoxAndSetFocus(true);
				// Focus should do this but doesn't seem to.
				RootBox.Activate(VwSelectionState.vssEnabled);
			}
		}

		/// <summary>
		/// We do NOT want SimpleRootSite.OnLoad to make a spurious selection for us.
		/// </summary>
		public override bool WantInitialSelection
		{
			get { return false; }
		}

		#region implemention of IChangeRootObject

		/// <summary>
		/// Do this to force a change/update of the rootbox, even if the root text object is the same.
		/// This is important to do when we edit the text in one (Edit) tab and switch to the next tab.
		/// </summary>
		internal void InvalidateRootBox()
		{
			m_fRootBoxNeedsUpdate = true;
		}

		public void SetRoot(int hvo)
		{
			EnsureVc();
			if (m_lineChoices != null)
				m_vc.LineChoices = m_lineChoices;
#if DEBUG
			//TimeRecorder.Begin("SetRoot");
#endif
			// since we are rebuilding the display, reset our sandbox mask.
			m_vc.SandboxAnnotation = 0;
			if (m_hvoRoot == hvo)
			{
				if (!m_fRootBoxNeedsUpdate)
					return; // nothing really changed, nothing to do.
			}
			else if (Cache.IsValidObject(m_hvoRoot))
			{
				// Update the database only when we're actually changing the word we're looking
				// at in the sandbox.  Otherwise, an Undo action inside the sandbox can crash by
				// trying to write data invalidated by the undo action which triggered this
				// being called.  (See LT-864.)
				UpdateRealFromSandbox();
			}
			m_hvoAnnotation = 0;
			m_hvoAnalysis = 0;
			if (TryHideFocusBox())
			{
				FocusBox.InterlinWordControl.CloseRootBox();
				FocusBox.Dispose();
				FocusBox = null;
			}
			m_hvoRoot = hvo;
			ChangeOrMakeRoot(m_hvoRoot, m_vc, (int)InterlinVc.kfragStText, m_styleSheet);
			m_vc.RootSite = this;
			m_fRootBoxNeedsUpdate = false;
			PopulateInitialFreeTranslationLines();
#if DEBUG
			//TimeRecorder.End("SetRoot");
			//TimeRecorder.Report();
#endif
		}


		/// <summary>
		/// joins AllowLayout with Suspend/ResumeLayout()
		/// </summary>
		public override bool AllowLayout
		{
			get
			{
				return base.AllowLayout;
			}
			set
			{
				base.AllowLayout = value;
				if (AllowLayout == false)
					SuspendLayout();
				else
					ResumeLayout(true);
			}
		}

		#endregion

		#region message handlers

		protected override void OnPaint(PaintEventArgs e)
		{
			if (!InValidState)
				return;
			base.OnPaint(e);
		}


		/// <summary>
		/// Answer true if we are in the interlinear edit view. This is used as part of enabling
		/// several commands, including show morphology and various bundle navigation commands.
		/// </summary>
		protected bool InFriendlyArea
		{
			get
			{
				string desiredArea = "textsWords";

				// see if it's the right area
				string areaChoice = m_mediator.PropertyTable.GetStringProperty("areaChoice", null);
				return areaChoice != null && areaChoice == desiredArea;
			}
		}

		/// <summary>
		/// This allows the bundle navigation items to work in the Text & Words area.
		/// Tools: Interlinear Edit, Concordance, and Word List Concordance
		/// </summary>
		protected bool InAcceptableTool
		{
			get
			{
				// see if it's the right area
				if (InFriendlyArea)
				{
					// now see if it's the right tool
					string toolChoice = m_mediator.PropertyTable.GetStringProperty("ToolForAreaNamed_textsWords", null);
					if (toolChoice == "interlinearEdit" ||
						toolChoice == "concordance" ||
						toolChoice == "wordListConcordance")
					{
						return true;
					}
				}
				return false; //we are not in an area that wants to see the show morphology command
			}
		}

		/// <summary>
		/// We can navigate from one bundle to another if we're in the right tool and the sandbox is
		/// actually visible.
		/// </summary>
		protected bool CanNavigateBundles
		{
			get
			{
				return InAcceptableTool && ForEditing && IsFocusBoxInstalled && FocusBox.Visible;
			}
		}

		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayMoveFocusBoxRight(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = CanNavigateBundles;
			display.Visible = display.Enabled;
			return true; //we've handled this
		}

		/// <summary>
		/// Move to the next word.
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public bool OnMoveFocusBoxRight(object argument)
		{
			OnMoveFocusBoxRight(true, false);
			return true;
		}

		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayMoveFocusBoxRightNc(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = CanNavigateBundles;
			display.Visible = display.Enabled;
			return true; //we've handled this
		}

		/// <summary>
		/// Move to next bundle with no confirm
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public bool OnMoveFocusBoxRightNc(object argument)
		{
			OnMoveFocusBoxRight(false, false);
			return true;
		}

		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayNextIncompleteBundleNc(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = CanNavigateBundles;
			display.Visible = display.Enabled;
			return true; //we've handled this
		}

		/// <summary>
		/// Move to next bundle needing analysis (and confirm current).
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public bool OnNextIncompleteBundleNc(object argument)
		{
			OnNextBundle(false, true, true, true);
			return true;
		}

		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayNextIncompleteBundle(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = CanNavigateBundles;
			display.Visible = display.Enabled;
			return true; //we've handled this
		}

		/// <summary>
		/// Move to next bundle needing analysis (and confirm current).
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public bool OnNextIncompleteBundle(object argument)
		{
			OnNextBundle(true, true, true, true);
			return true;
		}

		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayPrevIncompleteBundle(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = CanNavigateBundles;
			display.Visible = display.Enabled;
			return true; //we've handled this
		}
		/// <summary>
		/// Move to preivious bundle needing analysis (and confirm current).
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public bool OnPrevIncompleteBundle(object argument)
		{
			OnMoveFocusBoxLeft(true, true);
			return true;
		}

		/// <summary>
		/// Move to next bundle, after approving changes.
		/// </summary>
		/// <param name="fSaveGuess">if true, saves guesses; if false, skips guesses but still saves edits.</param>
		/// <param name="fNeedsAnalysis">true to skip to word needing analysis</param>
		/// <returns>true if there was a next bundle.</returns>
		public bool OnMoveFocusBoxRight(bool fSaveGuess, bool fNeedsAnalysis)
		{
			return OnMoveFocusBoxRight(fSaveGuess, fNeedsAnalysis, true);
		}

		/// <summary>
		/// Move to next bundle, after approving changes.
		/// </summary>
		/// <param name="fSaveGuess">if true, saves guesses; if false, skips guesses but still saves edits.</param>
		/// <param name="fNeedsAnalysis">true to skip to word needing analysis</param>
		/// <param name="fMakeDefaultSelection">true to make the default selection within the
		/// new sandbox.</param>
		/// <returns>true if there was a next bundle.</returns>
		public bool OnMoveFocusBoxRight(bool fSaveGuess, bool fNeedsAnalysis, bool fMakeDefaultSelection)
		{
			// Move in the literal direction (LT-3706)
			return OnNextBundle(fSaveGuess, fNeedsAnalysis, fMakeDefaultSelection, m_vc.RightToLeft ? false : true);
		}

		public bool OnDisplayApproveAndStayPut(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = CanNavigateBundles;
			display.Visible = display.Enabled;
			return true; //we've handled this
		}

		public bool OnApproveAndStayPut(object cmd)
		{
			// don't navigate, just save.
			UpdateRealFromSandbox(true);
			return true;
		}
		public bool OnDisplayApproveAndMoveNext(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = CanNavigateBundles;
			display.Visible = display.Enabled;
			return true; //we've handled this
		}

		public bool OnApproveAndMoveNext(object cmd)
		{
			ApproveGuessOrChangesAndMoveNext();
			return true;
		}

		public bool OnDisplayApproveAndMoveNextSameLine(object commandObject, ref UIItemDisplayProperties display)
		{
			return OnDisplayApproveAndMoveNext(commandObject, ref display);
		}

		public bool OnApproveAndMoveNextSameLine(object cmd)
		{
			OnNextBundle(true, false, false, true);
			return true;
		}

		public bool OnDisplayApproveForWholeTextAndMoveNext(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = CanNavigateBundles;
			display.Visible = display.Enabled;
			return true; //we've handled this
		}

		public bool OnApproveForWholeTextAndMoveNext(object cmd)
		{
			ApproveGuessOrChangesForWholeTextAndMoveNext(cmd as Command);
			return false;
		}

		public bool OnDisplayBrowseMoveNextSameLine(object commandObject, ref UIItemDisplayProperties display)
		{
			return OnDisplayBrowseMoveNext(commandObject, ref display);
		}

		public bool OnBrowseMoveNextSameLine(object cmd)
		{
			OnNextBundle(false, false, false, true);
			return true;
		}

		public bool OnDisplayBrowseMoveNext(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = CanNavigateBundles;
			display.Visible = display.Enabled;
			return true; //we've handled this
		}

		public bool OnBrowseMoveNext(object cmd)
		{
			OnNextBundle(false, false, true, true);
			return true;
		}

		internal bool ShowLinkWordsIcon
		{
			get
			{
				CheckDisposed();
				if (InFriendlyArea && IsFocusBoxInstalled)
				{
					return OnDisplayShowLinkWords(FocusBox.InterlinWordControl.HvoAnnotation,
						FocusBox.InterlinWordControl.Analysis);
				}
				else
				{
					return false;
				}
			}
		}

		public bool OnDisplayJoinWords(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = ShowLinkWordsIcon;
			display.Visible = display.Enabled;
			return true; //we've handled this
		}

		/// <summary>
		/// Note: Assume we are in the OnDisplayShowLinkWords is true context.
		/// </summary>
		public bool OnJoinWords(object cmd)
		{
			// For now, suppress Undo/Redo since we're having difficulty
			// refreshing the Words record lists appropriately.
			// 1) Merge the current annotation with next twfic.
			// Since this action may irreversably overlap with current undo actions, clear the undo stack.
			//ITextStrings.ksUndoLinkWords; ITextStrings.ksRedoLinkWords;
			using (UndoRedoCommandHelper undoRedoHelper = new UndoRedoCommandHelper(Cache, cmd as Command, false, true))
			{
				// any changes in the sandbox should be ignored. we don't want to try to save it.
				using (SegmentFormsUpdateHelper sfuh = new SegmentFormsUpdateHelper(this))
				{
					sfuh.MergeAdjacentSegmentForms();
				}
			}
			return true;
		}

		/// <summary>
		/// helper to properly manage updating the interlinear display for joining/breaking phrases.
		/// </summary>
		public class SegmentFormsUpdateHelper : RecordClerk.ListUpdateHelper
		{
			InterlinDocChild m_idc = null;
			int m_hvoCurrentAnnotation = 0;
			int m_hvoNewAnnotation = 0;
			StTxtPara m_para = null;
			FdoCache m_cache = null;
			public SegmentFormsUpdateHelper(InterlinDocChild idc)
				: base(idc.ActiveClerk)
			{
				// set these flags early so that any dependent ListUpdateHelpers
				// can inherit these settings.
				SkipShowRecord = true;
				TriggerPendingReloadOnDispose = false;

				m_idc = idc;
				m_cache = idc.Cache;

				// any changes in the sandbox should be ignored. we don't want to try to save it.
				m_idc.AbandonChangesInFocusBox();
				m_idc.AddUndoRedoAction(m_idc.HvoAnnotation, 0);
				m_idc.AllowLayout = false;

				CaptureInitialStateInfo(m_idc.HvoAnnotation);
			}

			/// <summary>
			/// Register initial state information that is expected to change during the life of the helper.
			/// </summary>
			/// <param name="hvoParasAffected"></param>
			public void CaptureInitialStateInfo(int hvoCurrentAnnotation)
			{
				m_hvoCurrentAnnotation = hvoCurrentAnnotation;
				ICmBaseAnnotation currentAnnotation = new CmBaseAnnotation(m_cache, hvoCurrentAnnotation);
				m_para = currentAnnotation.BeginObjectRA as StTxtPara;
			}

			/// <summary>
			/// The paragraphs in the current view. These are used to determine whether it's okay to delete
			/// original wordform(s) before joining or breaking phrases.
			/// </summary>
			private int[] ParasInView
			{
				get
				{
					if (m_idc != null)
						return m_idc.RawStText.ParagraphsOS.HvoArray;
					else
						return new int[] { m_para.Hvo };
				}
			}

			public void MergeAdjacentSegmentForms()
			{
				this.HvoNewAnnotation = m_para.MergeAdjacentSegmentForms(m_hvoCurrentAnnotation, ParasInView).Hvo;
			}

			public void BreakPhraseAnnotation()
			{
				List<int> newAnnotations = null;
				using (ParagraphParser phraseParser = new ParagraphParser(m_para))
				{
					newAnnotations = phraseParser.BreakPhraseAnnotation(m_hvoCurrentAnnotation, ParasInView);
					this.HvoNewAnnotation = newAnnotations[0];
				}
			}

			protected override void Dispose(bool disposing)
			{
				if (disposing)
				{
					if (m_idc != null)
					{
						// See LT-10193 -- multiple crash reports with HvoNewAnnotation apparently being zero.
						// This should not be possible, but if an exception gets thrown while trying to merge
						// two adjacent words, it might well have that appearance.  Checking here at least keeps
						// the hypothetical exception from being hidden by a new exception in this method.
						if (HvoNewAnnotation != 0)
							m_idc.AddUndoRedoAction(0, HvoNewAnnotation);
						m_idc.AllowLayout = true;
						if (HvoNewAnnotation != 0)
						{
							ICmBaseAnnotation newAnnotation = CmBaseAnnotation.CreateFromDBObject(m_idc.Cache, HvoNewAnnotation);
							// reconstruction should restore guesses
							m_idc.ReconstructAndRecreateSandbox(false, HvoNewAnnotation, newAnnotation.InstanceOfRAHvo);
						}
					}
				}
				base.Dispose(disposing);

				if (m_idc != null && disposing)
				{
					m_idc.Focus();
				}

				m_hvoNewAnnotation = 0;
				m_idc = null;
				m_para = null;
				m_cache = null;
			}

			/// <summary>
			/// The new current annotation based upon a merge/break phrase action.
			/// </summary>
			public int HvoNewAnnotation
			{
				get { return m_hvoNewAnnotation; }
				set { m_hvoNewAnnotation = value; }
			}
		}

		/// <summary>
		/// Approves the current analysis, including a guess or any changes to the default analysis.
		/// </summary>
		public bool ApproveGuessOrChangesAndMoveNext()
		{
			return OnNextBundle(true, false, true, true);
		}

		/// <summary>
		/// Using the current focus box content, approve it and apply it to all unanalyzed matching
		/// wordforms in the text.  See LT-8833.
		/// </summary>
		/// <returns></returns>
		public void ApproveGuessOrChangesForWholeTextAndMoveNext(Command cmd)
		{
			// We don't want the parser messing around with things until we get done, because it
			// might delete the new analysis before we get things initialized to the point where
			// it can tell the analysis is in use.
			ParserScheduler parser = PauseParserIfRunning();
			//Cache.BeginUndoTask(ITextStrings.ksUndoApproveAnalysis, ITextStrings.ksRedoApproveAnalysis);
			try
			{
				using (new UndoRedoCommandHelper(Cache, cmd))
				{
					// Go through the entire text looking for matching analyses that can be set to the new
					// value.
					if (m_hvoRoot == 0)
						return; // can't do anything.
					ISilDataAccess sda = m_fdoCache.MainCacheAccessor;
					int cpara = sda.get_VecSize(m_hvoRoot, (int)StText.StTextTags.kflidParagraphs);
					if (cpara == 0)
						return;	// newly created, no contents yet.
					int hvoNewAnalysis = FocusBox.InterlinWordControl.GetRealAnalysis(true);
					int hvoOldAnnotation = m_hvoAnnotation;
					int hvoOldAnalysis = m_hvoAnalysis;
					int hvoOldWordform = WfiWordform.GetWfiWordformFromInstanceOf(Cache, hvoOldAnnotation);
					int hvoNewWordform = GetWordformHvoOfAnalysis(hvoNewAnalysis);
					if (hvoNewAnalysis == hvoOldWordform)
					{
						// nothing significant to confirm, so move on
						MoveToAdjacentBundle(false, false, false, true);
						return;
					}
					m_hvoAnnotation = CacheAnalysisForAnnotation(hvoOldAnnotation, hvoNewAnalysis, sda);
					m_hvoAnalysis = hvoNewAnalysis;
					// determine if we confirmed on a sentence initial wfic to its lowercased form
					bool fIsSentenceInitialCaseChange = TrySentenceInitialWficHasCaseLowered(hvoNewWordform);
					if (hvoOldWordform != 0)
					{
						ApplyAnalysisToInstancesOfWordform(sda, hvoNewAnalysis,
							hvoOldWordform, hvoNewWordform, fIsSentenceInitialCaseChange);
					}
					// don't try to clean up the old analysis until we've finished walking through
					// the text and applied all our changes, otherwise we could delete a wordform
					// that is referenced by dummy annotations in the text, and thus cause the display
					// to treat them like pronunciations, and just show an unanalyzable text (LT-9953)
					FinishSettingAnalysis(hvoNewAnalysis, hvoOldAnalysis);
					Set<int> wordforms = new Set<int>();
					wordforms.Add(hvoOldWordform);
					wordforms.Add(hvoNewWordform);
					UpdateGuesses(wordforms);
					// We've done everything but move, so move.
					MoveToAdjacentBundle(false, false, false, true);
				}

			}
			finally
			{
				// If we paused the parser, restart it now.
				if (parser != null)
					parser.Resume();
			}
		}

		/// <summary>
		/// Update any necessary guesses when the specified wordforms change.
		/// </summary>
		/// <param name="wordforms"></param>
		private void UpdateGuesses(Set<int> wordforms)
		{
			ISilDataAccess sda = m_fdoCache.MainCacheAccessor;
			int cpara = sda.get_VecSize(m_hvoRoot, (int)StText.StTextTags.kflidParagraphs);
			// now update the guesses for the paragraphs.
			ParaDataUpdateTracker pdut = new ParaDataUpdateTracker(Cache);
			for (int ipara = 0; ipara < cpara; ++ipara)
			{
				int hvoPara = sda.get_VecItem(m_hvoRoot, (int)StText.StTextTags.kflidParagraphs, ipara);
				if (NeedsGuessesUpdated(hvoPara, wordforms))
					pdut.LoadAnalysisData(hvoPara);
				//pdut.LoadParaData(hvoPara); //This also loads all annotations which never affect guesses and take 3 times the number of queries
			}
			// now update the display with the affected annotations.
			foreach (int hvoAnnotationChanged in pdut.ChangedAnnotations)
				SimulateReplaceAnnotation(hvoAnnotationChanged);
		}

		/// <summary>
		/// Return true if the specified paragraph needs its guesses updated when we've changed something about the analyses
		/// or occurrenes of analyses of one of the specified wordforms.
		/// </summary>
		private bool NeedsGuessesUpdated(int hvoPara, Set<int> wordforms)
		{
			int ktagParaSegments = StTxtPara.SegmentsFlid(Cache);
			int ktagSegmentForms = InterlinVc.SegmentFormsTag(Cache);

			ISilDataAccess sda = m_fdoCache.MainCacheAccessor;
			// If we haven't already figured the segments of a paragraph, we don't need to update it; the guesses will
			// get made when scrolling makes the paragraph visible.
			if (!sda.get_IsPropInCache(hvoPara, ktagParaSegments, (int)CellarModuleDefns.kcptReferenceSequence, 0))
				return false;
			int cseg = sda.get_VecSize(hvoPara, ktagParaSegments);
			for (int iseg = 0; iseg < cseg; iseg++)
			{
				int hvoSeg = sda.get_VecItem(hvoPara, ktagParaSegments, iseg);
				int cxfic = sda.get_VecSize(hvoSeg, ktagSegmentForms);
				for (int ixfic = 0; ixfic < cxfic; ixfic++)
				{
					int hvoWfic = sda.get_VecItem(hvoSeg, ktagSegmentForms, ixfic);
					int hvoInstanceOf = sda.get_ObjectProp(hvoWfic, (int) CmAnnotation.CmAnnotationTags.kflidInstanceOf);
					if (hvoInstanceOf == 0)
						continue; // punctuation, doesn't need guess
					if (Cache.GetClassOfObject(hvoInstanceOf) == WfiGloss.kclsidWfiGloss)
						continue; // fully glossed, no need to update.
					if (wordforms.Contains(WfiWordform.GetWordformFromWag(Cache, hvoInstanceOf)))
						return true; // This paragraph IS linked to one of the interesting wordforms; needs guesses updated
				}
			}
			return false; // no Wfics that might be affected.
		}

		private bool TrySentenceInitialWficHasCaseLowered(int hvoNewWordform)
		{
			bool fIsSentenceInitialCaseChange = false;
			int hvoWfiLower;
			if (TryGetTwficLowercaseSentenceInitialForm(m_hvoAnnotation, out hvoWfiLower) &&
				hvoNewWordform == hvoWfiLower)
			{
				fIsSentenceInitialCaseChange = true;
			}
			return fIsSentenceInitialCaseChange;
		}

		IEnumerable<int> GetNextXfic()
		{
			ISilDataAccess sda = Cache.MainCacheAccessor;
			int cpara = sda.get_VecSize(m_hvoRoot, (int)StText.StTextTags.kflidParagraphs);
			for (int ipara = 0; ipara < cpara; ++ipara)
			{
				int hvoPara = sda.get_VecItem(m_hvoRoot, (int)StText.StTextTags.kflidParagraphs, ipara);
				int cseg = sda.get_VecSize(hvoPara, m_vc.ktagParaSegments);
				for (int iseg = 0; iseg < cseg; iseg++)
				{
					int hvoSeg = sda.get_VecItem(hvoPara, m_vc.ktagParaSegments, iseg);
					int cann = sda.get_VecSize(hvoSeg, m_vc.ktagSegmentForms);
					for (int iann = 0; iann < cann; iann++)
					{
						yield return sda.get_VecItem(hvoSeg, m_vc.ktagSegmentForms, iann);
					}
				}
			}
		}

		IEnumerable<int> GetNextInstanceOf()
		{
			ISilDataAccess sda = Cache.MainCacheAccessor;
			foreach (int hvoAnn in GetNextXfic())
			{
			   yield return sda.get_ObjectProp(hvoAnn,
											   (int)CmAnnotation.CmAnnotationTags.kflidInstanceOf);
			}
		}

		private void ApplyAnalysisToInstancesOfWordform(ISilDataAccess sda, int hvoNewAnalysis, int hvoOldWordform, int hvoNewWordform, bool fIsSentenceInitialCaseChange)
		{
			foreach (int hvoAnn in GetNextXfic())
			{
				if (hvoAnn == m_hvoAnnotation)
					continue; // already processed.
				int hvoInstance = sda.get_ObjectProp(hvoAnn,
													 (int)CmAnnotation.CmAnnotationTags.kflidInstanceOf);
				if (hvoInstance == 0)
					continue; // skip punctuation.
				if (CheckOkToConfirm(fIsSentenceInitialCaseChange,
									 hvoOldWordform, hvoNewWordform, hvoAnn, hvoInstance))
				{
					int hvoAnnNew = CacheAnalysisForAnnotation(hvoAnn, hvoNewAnalysis, sda);
					if (hvoAnnNew != 0)
						SimulateReplaceAnnotation(hvoAnnNew);
				}
			}
		}

		private bool CheckOkToConfirm(bool fIsSentenceInitialCaseChange, int hvoOldWordform, int hvoNewWordform, int hvoAnn, int hvoInstance)
		{
			if (hvoInstance == 0 || Cache.GetClassOfObject(hvoInstance) != WfiWordform.kclsidWfiWordform)
				return false; // only need confirming for instanceOf wordforms.
			bool fOkToConfirm = false;
			if (fIsSentenceInitialCaseChange)
			{
				// the user did confirm-all for lowercase sentence initial analysis, we want to confirm
				// 1) only sentence initial twfics that has same lowercase form as new wordform
				// 2) sentence medial twfics matching the new wordform.
				int hvoWfiLower;
				if (TryGetTwficLowercaseSentenceInitialForm(hvoAnn, out hvoWfiLower))
				{
					// sentence initial twfic
					if (hvoWfiLower == hvoNewWordform)
					{
						fOkToConfirm = true;
					}
				}
				else if (hvoInstance == hvoNewWordform)
				{
					fOkToConfirm = true;
				}
			}
			else if (hvoInstance == hvoOldWordform || hvoInstance == hvoNewWordform)
			{
				int hvoWfiLower;
				if (TryGetTwficLowercaseSentenceInitialForm(hvoAnn, out hvoWfiLower))
				{
					// for now, let sentence initial forms get confirmed seperately, just in case.
				}
				else
				{
					fOkToConfirm = true;
				}
			}
			return fOkToConfirm;
		}

		private bool TryGetTwficLowercaseSentenceInitialForm(int hvoAnnotation, out int hvoWfiLower)
		{
			hvoWfiLower = 0;
			int ktagMatchingLowercaseForm = InterlinVc.MatchingLowercaseWordForm(Cache);
			if (Cache.MainCacheAccessor.get_IsPropInCache(hvoAnnotation, ktagMatchingLowercaseForm, (int)CellarModuleDefns.kcptReferenceAtom, 0))
				hvoWfiLower = Cache.GetObjProperty(hvoAnnotation, ktagMatchingLowercaseForm);
			else
			{
				StTxtPara.TwficInfo twficInfo = new StTxtPara.TwficInfo(m_fdoCache, hvoAnnotation);
				if (twficInfo.IsFirstTwficInSegment)
				{
					ITsString tssWfBaseline = StTxtPara.TssSubstring(twficInfo.Object);
					CpeTracker tracker = new CpeTracker(Cache.LanguageWritingSystemFactoryAccessor, tssWfBaseline);
					ILgCharacterPropertyEngine cpe = tracker.CharPropEngine(0);
					string sLower = cpe.ToLower(tssWfBaseline.Text);
					hvoWfiLower = Cache.LangProject.WordformInventoryOA.GetWordformId(sLower,
						StringUtils.GetWsAtOffset(tssWfBaseline, 0));
				}
			}
			return hvoWfiLower != 0;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="fSaveGuess">if true, saves guesses; if false, skips guesses but still saves edits.</param>
		/// <param name="fNeedsAnalysis"></param>
		/// <param name="fMakeDefaultSelection">if true, tries to make a selection in the first line that needs analysis,
		/// if false, we try to maintain a selection in the same line we were previously in.</param>
		/// <param name="fAdvance">if true, advance to the next annotation in the direction the paragraph.</param>
		/// <returns></returns>
		internal bool OnNextBundle(bool fSaveGuess, bool fNeedsAnalysis, bool fMakeDefaultSelection, bool fAdvance)
		{
			return MoveToAdjacentBundle(fSaveGuess, fNeedsAnalysis, fMakeDefaultSelection, fAdvance);
		}

		private bool MoveToAdjacentBundle(bool fSaveGuess, bool fNeedsAnalysis, bool fMakeDefaultSelection, bool fAdvance)
		{
			AdvanceWordArgs args = GetArgsForAdjacentAnnotation(fNeedsAnalysis, fAdvance);
			if (args.Annotation != 0 && args.Annotation != m_hvoAnnotation)
			{
				int currentLineIndex = -1;
				if (this.ExistingFocusBox != null)
					currentLineIndex = this.FocusBox.InterlinWordControl.GetLineOfCurrentSelection();
				TriggerAnnotationSelected(args.Annotation, args.Analysis, fSaveGuess,
					fMakeDefaultSelection);
				if (!fMakeDefaultSelection && currentLineIndex >= 0)
					this.FocusBox.InterlinWordControl.SelectOnOrBeyondLine(currentLineIndex, 1);
				return true;
			}
			else
			{
				// even though we can't move, we still want to approve changes (and possibly a guess).
				UpdateRealFromSandbox(fSaveGuess);
				return false;
			}
		}

		private AdvanceWordArgs GetArgsForAdjacentAnnotation(bool fNeedsAnalysis, bool fAdvance)
		{
			return GetArgsForAdjacentAnnotation(m_hvoAnnotation, m_hvoAnalysis, fNeedsAnalysis, fAdvance);
		}

		private AdvanceWordArgs GetArgsForAdjacentAnnotation(int hvoAnnotation, int hvoAnalysis, bool fNeedsAnalysis, bool fAdvance)
		{
			AdvanceWordArgs args = new AdvanceWordArgs(hvoAnnotation, hvoAnalysis);
			if (fAdvance)
				AdvanceWord(this, args, fNeedsAnalysis);
			else
				BackWord(this, args, fNeedsAnalysis);
			return args;
		}

		internal bool OnDisplayShowLinkWords(int hvoAnnotation, int hvoAnalysis)
		{
			AdvanceWordArgs argsForNextAnnotation = new AdvanceWordArgs(hvoAnnotation, hvoAnalysis);
			AdvanceWord(this, argsForNextAnnotation, false);
			return CanExtendAnnotation(hvoAnnotation, argsForNextAnnotation.Annotation);
		}

		private bool CanExtendAnnotation(int hvoStartAnnotation, int hvoNextAnnotation)
		{
			if (hvoNextAnnotation == 0)
				return false;
			int hvoCurrentParaSeg;
			int iCurrentParaSeg;
			bool fIsFirstTwfic;
			int iStartSegForm = StTxtPara.TwficSegmentLocation(Cache, hvoStartAnnotation,
				out hvoCurrentParaSeg, out iCurrentParaSeg, out fIsFirstTwfic);
			int hvoNextParaSeg;
			int iNextParaSeg;
			int iNextSegForm = StTxtPara.TwficSegmentLocation(Cache, hvoNextAnnotation,
				out hvoNextParaSeg, out iNextParaSeg, out fIsFirstTwfic);
			// Check that the next annotation is the next form in the same segment.
			if (hvoCurrentParaSeg != hvoNextParaSeg ||
				iCurrentParaSeg != iNextParaSeg ||
				iStartSegForm + 1 != iNextSegForm)
			{
				return false;
			}
			// Make sure that the next wordform has the same writing system.
			int hvoPara = Cache.GetObjProperty(hvoStartAnnotation,
				(int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginObject);
			IStTxtPara para = StTxtPara.CreateFromDBObject(Cache, hvoPara, false) as IStTxtPara;
			int currentBeginOffset = Cache.GetIntProperty(hvoStartAnnotation,
				(int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginOffset);
			int nextBeginOffset = Cache.GetIntProperty(hvoNextAnnotation,
				(int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginOffset);
			int var;
			ITsTextProps ttp = para.Contents.UnderlyingTsString.get_PropertiesAt(currentBeginOffset);
			int currentWs = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out var);
			ttp = para.Contents.UnderlyingTsString.get_PropertiesAt(nextBeginOffset);
			int nextWs = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out var);
			if (currentWs != nextWs)
				return false;
			return true;
		}

		internal void AbandonChangesInFocusBox()
		{
			if (ExistingFocusBox != null && FocusBox.InterlinWordControl != null)
			{
				FocusBox.InterlinWordControl.MarkAsInitialState();
				TryHideFocusBox();
			}
		}

		internal UndoRedoApproveAnalysis AddUndoRedoAction(int hvoCurrentAnnotation, int hvoNewAnnotation)
		{
			if (Cache.ActionHandlerAccessor != null && hvoCurrentAnnotation != hvoNewAnnotation)
			{
				UndoRedoApproveAnalysis undoRedoAction = new UndoRedoApproveAnalysis(this,
					hvoCurrentAnnotation, hvoNewAnnotation);
				Cache.ActionHandlerAccessor.AddAction(undoRedoAction);
				return undoRedoAction;
			}
			return null;
		}


		/// <summary>
		/// whether or not to display the Break phrase icon.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public bool OnDisplayBreakPhrase(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();
			display.Enabled = ShowBreakPhraseIcon;
			display.Visible = display.Enabled;
			return true;
		}

		internal bool ShowBreakPhraseIcon
		{
			get
			{
				return InFriendlyArea &&
					IsFocusBoxInstalled &&
					ParagraphParser.IsPhrase(Cache, FocusBox.InterlinWordControl.RawWordform);
			}
		}

		/// <summary>
		/// (LT-7807) true if this document is in the context/state for adding glossed words to lexicon.
		/// </summary>
		internal bool InModeForAddingGlossedWordsToLexicon
		{
			get
			{
				InterlinMaster master = GetMaster();
				return master != null && master.InModeForAddingGlossedWordsToLexicon;
			}
		}

		/// <summary>
		/// split the current annotation into annotations for each word in the phrase-wordform.
		/// (assume it IsPhrase)
		/// </summary>
		/// <param name="argument"></param>
		public void OnBreakPhrase(object argument)
		{
			// (LT-8069) in some odd circumstances, the break phrase icon lingers on the tool bar menu when it should
			// have disappeared. If we're in that state, just return.
			if (!ShowBreakPhraseIcon)
				return;
			// For now, suppress Undo/Redo since we're having difficulty
			// refreshing the Words record lists appropriately.
			// Since this action may irreversably overlap with current undo actions, clear the undo stack.
			// ITextStrings.ksUndoBreakPhrase; ITextStrings.ksRedoBreakPhrase;
			using (UndoRedoCommandHelper undoRedoHelper = new UndoRedoCommandHelper(Cache, argument as Command, false, true))
			{
				using (SegmentFormsUpdateHelper sfuh = new SegmentFormsUpdateHelper(this))
				{
					sfuh.BreakPhraseAnnotation();
				}
			}
			return;
		}

		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayMoveFocusBoxLeft(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = CanNavigateBundles;
			display.Visible = display.Enabled;
			return true; //we've handled this
		}

		/// <summary>
		/// Move to the previous word (and confirm current).
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public bool OnMoveFocusBoxLeft(object argument)
		{
			OnMoveFocusBoxLeft(true, false);
			return true;
		}

		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayMoveFocusBoxLeftNc(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = CanNavigateBundles;
			display.Visible = display.Enabled;
			return true; //we've handled this
		}
		/// <summary>
		/// Move to the previous word (don't confirm current).
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public bool OnMoveFocusBoxLeftNc(object argument)
		{
			OnMoveFocusBoxLeft(false, false);
			return true;
		}

		/// <summary>
		/// Move to the previous word.
		/// </summary>
		/// <param name="fSaveGuess">if true, saves guesses; if false, skips guesses but still saves edits.</param>
		/// <returns></returns>
		public bool OnMoveFocusBoxLeft(bool fSaveGuess, bool fNeedsAnalysis)
		{
			return OnMoveFocusBoxLeft(fSaveGuess, fNeedsAnalysis, true);
		}
		/// <summary>
		/// Move to the previous word.
		/// </summary>
		/// <param name="fSaveGuess">if true, saves guesses; if false, skips guesses but still saves edits.</param>
		/// <param name="fMakeDefaultSelection">true to make the default selection within the
		/// new sandbox.</param>
		/// <returns></returns>
		public bool OnMoveFocusBoxLeft(bool fSaveGuess, bool fNeedsAnalysis, bool fMakeDefaultSelection)
		{
			return MoveToAdjacentBundle(fSaveGuess, fNeedsAnalysis, fMakeDefaultSelection, m_vc.RightToLeft ? true : false);
		}
		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayBundleUp(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = CanNavigateBundles;
			display.Visible = display.Enabled;
			return true; //we've handled this
		}
		/// <summary>
		/// Move to the next word 'up' (and confirm current).
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public bool OnBundleUp(object argument)
		{
			return OnBundleUp(true);
		}
		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayBundleUpNc(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = CanNavigateBundles;
			display.Visible = display.Enabled;
			return true; //we've handled this
		}
		/// <summary>
		/// Move to the next word 'up' (don't approve guesses).
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public bool OnBundleUpNc(object argument)
		{
			return OnBundleUp(false);
		}
		/// <summary>
		/// Move to the next word 'up'.
		/// </summary>
		/// <param name="fSaveGuess">if true, saves guesses; if false, skips guesses but still saves edits.</param>
		/// <returns></returns>
		public bool OnBundleUp(bool fSaveGuess)
		{
			Point pt = new Point(FocusBox.Left + FocusBox.Width / 2, FocusBox.Top - FocusBox.Height / 2);
			Rectangle rcSrcRoot;
			Rectangle rcDstRoot;
			using (new HoldGraphics(this))
			{
				GetCoordRects(out rcSrcRoot, out rcDstRoot);
				IVwSelection sel = RootBox.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, false);
				while (pt.Y > MinSelectionHeight() && !HandleClickSelection(sel, true, fSaveGuess))
				{
					// Missed...maybe got a free translation...try higher.
					pt.Y -= 20;
					sel = RootBox.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, false);
				}
			}
			if (pt.Y <= MinSelectionHeight())
			{
				// we still want to save changes.
				// even though we can't move, we still want to approve changes (and possibly a guess).
				UpdateRealFromSandbox(fSaveGuess);
			}
			return true;
		}

		private int MinSelectionHeight()
		{
			return -FocusBox.Height * 2;
		}

		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayBundleDown(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = CanNavigateBundles;
			display.Visible = display.Enabled;
			return true; //we've handled this
		}
		/// <summary>
		/// Move to the next word 'down' (and confirm current).
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public bool OnBundleDown(object argument)
		{
			return OnBundleDown(true);
		}

		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayBundleDownNc(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = CanNavigateBundles;
			display.Visible = display.Enabled;
			return true; //we've handled this
		}
		/// <summary>
		/// Move to the next word 'down' (and confirm current).
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public bool OnBundleDownNc(object argument)
		{
			return OnBundleDown(false);
		}

		/// <summary>
		/// Move to the next word 'down'
		/// </summary>
		/// <param name="fSaveGuess">if true, saves guesses; if false, skips guesses but still saves edits.</param>
		/// <returns></returns>
		public bool OnBundleDown(bool fSaveGuess)
		{
			Point pt = new Point(FocusBox.Left + FocusBox.Width / 2, FocusBox.Bottom + FocusBox.Height / 2);
			Rectangle rcSrcRoot;
			Rectangle rcDstRoot;
			using (new HoldGraphics(this))
			{
				GetCoordRects(out rcSrcRoot, out rcDstRoot);
				IVwSelection sel = RootBox.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, false);
				while (pt.Y < MaxSelectionHeight() && !HandleClickSelection(sel, true, fSaveGuess))
				{
					// Missed...maybe got a free translation...try higher.
					pt.Y += 20;
					sel = RootBox.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, false);
				}
			}
			if (pt.Y >= MaxSelectionHeight())
			{
				// we still want to save changes.
				// even though we can't move, we still want to approve changes (and possibly a guess).
				UpdateRealFromSandbox(fSaveGuess);
			}
			return true;
		}

		private int MaxSelectionHeight()
		{
			return this.Height + FocusBox.Height * 2;
		}


		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayLastBundle(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = CanNavigateBundles;
			display.Visible = display.Enabled;
			return true; //we've handled this
		}
		/// <summary>
		/// Move to the last bundle
		/// </summary>
		/// <param name="arg"></param>
		/// <returns></returns>
		public bool OnLastBundle(object arg)
		{
			AdvanceWordArgs args = new AdvanceWordArgs(0, 0);
			BackWord(this, args, false);
			if (args.Annotation != 0)
				TriggerAnnotationSelected(args.Annotation, args.Analysis, true);
			return true;
		}

		/// <summary>
		/// handle the message to see if the menu item should be enabled
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayFirstBundle(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = CanNavigateBundles;
			display.Visible = display.Enabled;
			return true; //we've handled this
		}
		/// <summary>
		/// Move to the first bundle
		/// </summary>
		/// <param name="arg"></param>
		/// <returns></returns>
		public bool OnFirstBundle(object arg)
		{
			AdvanceWordArgs args = new AdvanceWordArgs(0, 0);
			AdvanceWord(this, args, false);
			if (args.Annotation != 0)
				TriggerAnnotationSelected(args.Annotation, args.Analysis, true);
			return true;
		}

		/// <summary>
		/// Enable the "Approve Analysis And" submenu, if we can.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayApproveAnalysisMovementMenu(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = CanNavigateBundles;
			display.Visible = display.Enabled;
			return true;
		}

		/// <summary>
		/// Enable the "Disregard Analysis And" submenu, if we can.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayBrowseMovementMenu(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Enabled = CanNavigateBundles;
			display.Visible = display.Enabled;
			return true;
		}


		#endregion


		/// <summary>
		/// Pull this out into a separate method so InterlinPrintChild can make an InterlinPrintVc.
		/// And so InterlinTaggingChild can make an InterlinTaggingVc.
		/// </summary>
		protected virtual void MakeVc()
		{
			m_vc = new InterlinVc(m_fdoCache);
		}

		private void EnsureVc()
		{
			if (m_vc == null)
				MakeVc();
		}

		#region Overrides of RootSite

		protected override void OnLostFocus(EventArgs e)
		{
			if (!m_fSuppressLoseFocus) // suppresses events while focusing self.
			{
				DisposeFtMonitor();
				m_vc.SetActiveFreeform(0, 0, 0);
			}
			base.OnLostFocus(e);
		}

		private void DisposeFtMonitor()
		{
			if (m_dictFtMonitor.Count > 0)
			{
				if (TopLevelControl is Form)
					(TopLevelControl as Form).FormClosing -= new FormClosingEventHandler(FormClosing);
				List<int> keys = new List<int>();
				keys.AddRange(m_dictFtMonitor.Keys);
				foreach (int ws in keys)
				{
					m_dictFtMonitor[ws].Dispose();
					m_dictFtMonitor[ws] = null;
				}
				m_dictFtMonitor.Clear();
			}
			//if (m_ftMonitor != null)
			//{
			//    if (TopLevelControl is Form)
			//        (TopLevelControl as Form).FormClosing -= new FormClosingEventHandler(FormClosing);
			//    m_ftMonitor.Dispose();
			//    m_ftMonitor = null;
			//}
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make the root box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void MakeRoot()
		{
			if (m_fdoCache == null || DesignMode)
				return;

			// for reference
			//plpi = m_fdoCache.LangProject;
			//qode = m_fdoCache.DatabaseAccessor;

			//			int cannotations = LoadAnnotations();
			//			if (cannotations == 0)
			//			{
			//				DoInitialParse(m_para);
			//				LoadAnnotations();
			//			}

			if (m_lineChoices == null)
				m_lineChoices = SetupLineChoices(null, InterlinLineChoices.InterlinMode.Analyze);

			m_rootb = VwRootBoxClass.Create();
			m_rootb.SetSite(this);
			// Setting this result too low can result in moving a cursor from an editable field
			// to a non-editable field (e.g. with Control-Right and Control-Left cursor
			// commands).  Normally we could set this to only a few (e.g. 4). but in
			// Interlinearizer we may want to jump from one sentence annotation to the next over
			// several read-only paragraphs  contained in a word bundle.  Make sure that
			// procedures that use this limit do not move the cursor from an editable to a
			// non-editable field.
			m_rootb.MaxParasToScan = 2000;

			EnsureVc();

			// We want to get notified when anything changes.
			m_sda = m_fdoCache.MainCacheAccessor;
			m_sda.AddNotification(this);

			m_vc.ShowMorphBundles = m_mediator.PropertyTable.GetBoolProperty("ShowMorphBundles", true);
			m_vc.LineChoices = m_lineChoices;
			m_vc.ShowDefaultSense = true;

			m_rootb.DataAccess = m_fdoCache.MainCacheAccessor;

			m_rootb.SetRootObject(m_hvoRoot, m_vc, (int)InterlinVc.kfragStText, m_styleSheet);

			base.MakeRoot();

			SetSandboxSize(); // in case we already have a current annotation.

			//TODO:
			//ptmw->RegisterRootBox(qrootb);
		}

		/// <summary>
		/// m_lineChoices is for setting m_vc.LineChoices before we even have a valid vc.
		/// </summary>
		InterlinLineChoices m_lineChoices = null;
		internal InterlinLineChoices SetupLineChoices(string configPropName, InterlinLineChoices.InterlinMode mode)
		{
			ConfigPropName = configPropName;
			string persist = m_mediator.PropertyTable.GetStringProperty(ConfigPropName, null, PropertyTable.SettingsGroup.LocalSettings);
			InterlinLineChoices lineChoices;
			if (!TryRestoreLineChoices(out lineChoices))
			{
				if (ForEditing)
				{
					lineChoices = EditableInterlinLineChoices.DefaultChoices(0, m_fdoCache.DefaultAnalWs,
																			 m_fdoCache.LangProject);
					lineChoices.Mode = mode;
					if (mode == InterlinLineChoices.InterlinMode.Gloss ||
						mode == InterlinLineChoices.InterlinMode.GlossAddWordsToLexicon)
						lineChoices.SetStandardGlossState();
					else
						lineChoices.SetStandardState();
				}
				else
				{
					lineChoices = InterlinLineChoices.DefaultChoices(0, m_fdoCache.DefaultAnalWs, m_fdoCache.LangProject);
				}
			}
			else if (ForEditing)
			{
				// just in case this hasn't been set for restored lines
				lineChoices.Mode = mode;
			}
			m_lineChoices = lineChoices;
			return m_lineChoices;
		}

		/// <summary>
		/// Tries to restore the LineChoices saved in the ConfigPropName property in the property table.
		/// </summary>
		/// <param name="lineChoices"></param>
		/// <returns></returns>
		internal bool TryRestoreLineChoices(out InterlinLineChoices lineChoices)
		{
			lineChoices = null;
			string persist = m_mediator.PropertyTable.GetStringProperty(ConfigPropName, null, PropertyTable.SettingsGroup.LocalSettings);
			if (persist != null)
			{
				lineChoices = InterlinLineChoices.Restore(persist, m_fdoCache.LanguageWritingSystemFactoryAccessor,
					0, m_fdoCache.DefaultAnalWs, m_fdoCache.LangProject);
			}
			return persist != null && lineChoices != null;
		}

		public IStText RawStText
		{
			get
			{
				if (m_hvoRoot == 0)
					return null;
				return new StText(Cache, m_hvoRoot);
			}
		}

		/// <summary>
		/// The property table key storing InterlinLineChoices used by our display.
		/// Parent controls (e.g. InterlinMaster) should pass in their own property
		/// to configure for contexts it knows about.
		/// </summary>
		string m_lineChoicesPropName = null;
		internal string ConfigPropName
		{
			get
			{
				if (m_lineChoicesPropName == null)
					m_lineChoicesPropName = "InterlinConfig_" + (ForEditing ? "Edit" : "Doc");
				return m_lineChoicesPropName;
			}
			set { m_lineChoicesPropName = value; }
		}


		protected override void OnGotFocus(EventArgs e)
		{
			if (IsFocusBoxInstalled)
			{
				if (FocusBox.InterlinWordControl != null)
					FocusBox.InterlinWordControl.Focus();
				else
					FocusBox.Focus();
			}
			else
			{
				base.OnGotFocus(e);
				// Supposedly, m_ftMonitor should always be null, since it only gets set in OnGetFocus
				// and it gets cleared in OnLostFocus. However there have been odd cases. If we're already
				// tracking a change we don't want to lose it.
				int cMonitors = m_dictFtMonitor.Count;
				foreach (InterlinLineSpec spec in m_vc.LineChoices)
				{
					if (spec.Flid == InterlinLineChoices.kflidFreeTrans)
					{
						int ws = spec.WritingSystem;
						if (spec.IsMagicWritingSystem)
						{
							// What to do??
							continue;
						}
						FreeTransEditMonitor ftem = null;
						if (m_dictFtMonitor.TryGetValue(ws, out ftem))
							continue;
						m_dictFtMonitor.Add(ws, new FreeTransEditMonitor(Cache, ws));
					}
				}
				if (cMonitors == 0 && m_dictFtMonitor.Count > 0)
				{
					// Unfortunately, when the main window closes, both our Dispose() method and our OnLostFocus() method
					// get called during the Dispose() of the main window, which is AFTER the FdoCache gets disposed.
					// We need to dispose our FreeTransEditMonitor before the cache is disposed, so we can update the
					// CmTranslation if necessary.
					if (TopLevelControl is Form)
						(TopLevelControl as Form).FormClosing += new FormClosingEventHandler(FormClosing);
				}
				//if (m_ftMonitor == null)
				//{
				//    m_ftMonitor = new FreeTransEditMonitor(Cache, Cache.DefaultAnalWs);
				//    // Unfortunately, when the main window closes, both our Dispose() method and our OnLostFocus() method
				//    // get called during the Dispose() of the main window, which is AFTER the FdoCache gets disposed.
				//    // We need to dispose our FreeTransEditMonitor before the cache is disposed, so we can update the
				//    // CmTranslation if necessary.
				//    if (TopLevelControl is Form)
				//        (TopLevelControl as Form).FormClosing += new FormClosingEventHandler(FormClosing);
				//}
			}
		}

		void FormClosing(object sender, FormClosingEventArgs e)
		{
			DisposeFtMonitor();
		}

		/// <summary>
		/// This method is intended to be called directly from a menu option, through the
		/// IxCoreColleague system. Problem: which para should it parse?
		/// </summary>
		//		public void ReParse()
		//		{
		//			ReParse(m_para);
		//		}
		/// <summary>
		/// Parse the StTxtPara, creating annotations and if necessary WfiWordforms for each word.
		/// </summary>
		/// <param name="para"></param>
		//		internal void ReParse(StTxtPara para)
		//		{
		//			ParagraphParser.ParseParagraph(para, false);
		//			m_vc.LoadParaData(m_para.Hvo);
		//			m_rootb.Reconstruct(); //Saves having LoadParaData worry about PropChanged.
		//		}

		/// <summary>
		/// Parse the StTxtPara, creating annotations and if necessary WfiWordforms for each word.
		/// </summary>
		/// <param name="para"></param>
		//		internal void DoInitialParse(StTxtPara para)
		//		{
		//			ParagraphParser.ParseParagraph(para, true);
		//		}
		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				base.OnMouseDown(e);
				return;
			}

			if (m_rootb == null || DataUpdateMonitor.IsUpdateInProgress(DataAccess))
				return;

			EditingHelper.CommitIfWord(new WordEventArgs(EditingHelper.WordEventSource.MouseClick));

			// Convert to box coords and see what selection it produces.
			Point pt;
			Rectangle rcSrcRoot;
			Rectangle rcDstRoot;
			using (new HoldGraphics(this))
			{
				pt = PixelToView(new Point(e.X, e.Y));
				GetCoordRects(out rcSrcRoot, out rcDstRoot);
				IVwSelection sel = RootBox.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, false);
				if (sel == null || !HandleClickSelection(sel, false, false))
					base.OnMouseDown(e);
			}
		}

		public void UpdateRealFromSandbox()
		{
			UpdateRealFromSandbox(false);
		}

		private void UpdateRealFromSandbox(bool fSaveGuess, int hvoNewAnnotation, int hvoNewAnalysis)
		{
			// If we're not InValidState (e.g. we're switching to a new text),
			// there's no point in allowing layout, since what we are displaying will change soon.
			// Note: This prevents us getting a SizeChanged event which eventually causes us
			// to HideAndDestroyFocusBox() before we are through with the sandbox. (LT-4869).
			// JohnT is not exactly sure why the RootBox issues a SizeChanged in the first place.
			// It occurs when converting the old dummy annotation into a new one, or when
			// setting its InstanceOf to the new analysis with SetObjProperty().
			bool fAllowLayout = this.AllowLayout;
			if (!InValidState)
				this.AllowLayout = false;
			UpdateRealFromSandbox(ref m_hvoAnnotation, ref m_hvoAnalysis, fSaveGuess, hvoNewAnnotation, hvoNewAnalysis);
			if (this.AllowLayout != fAllowLayout)
				this.AllowLayout = fAllowLayout;
		}

		private void UpdateRealFromSandbox(bool fSaveGuess)
		{
			UpdateRealFromSandbox(fSaveGuess, m_hvoAnnotation, m_hvoAnalysis);
		}

		// Get a substring of a tsString.
		ITsString SubString(ITsString tss, int ichMin, int ichLim)
		{
			ITsStrBldr bldr = tss.GetBldr();
			int cch = tss.Length;
			if (ichLim < cch)
				bldr.ReplaceTsString(ichLim, cch, null);
			if (ichMin > 0)
				bldr.ReplaceTsString(0, ichMin, null);
			return bldr.GetString();
		}

		private void UpdateRealFromSandbox(ref int hvoOldAnnotation, ref int hvoOldAnalysis)
		{
			UpdateRealFromSandbox(ref hvoOldAnnotation, ref hvoOldAnalysis, false, hvoOldAnnotation, hvoOldAnalysis);
		}

		/// <summary>
		/// Fix the real database to conform to whatever the user has been doing in the Sandbox.
		/// </summary>
		internal void UpdateRealFromSandbox(ref int hvoOldAnnotation, ref int hvoOldAnalysis, bool fSaveGuess, int hvoNextAnnotation, int hvoNextAnalysis)
		{
			if (hvoOldAnnotation == 0)
				return;
			if (FocusBox.InterlinWordControl == null || !FocusBox.InterlinWordControl.ShouldSave(fSaveGuess))
				return;
			if (!Cache.IsValidObject(m_hvoRoot))
				return;

			// Before we start the transaction, we have to make sure we have this high level of
			// isolation. Otherwise, it is very possible for the parse filer to destroy our new
			// analysis before we hook the annotation to it, or even before we finish creating it.
			// This cause all kinds of crashes that are not consistently repeatable. The parse
			// filer is allowed to destroy any analysis that doesn't have a Twfic pointing at
			// it or an evaluation pointing at it. This is obviously true of any newly created
			// annotation, so we must do one of those two things before the end of the transaction.

			// This looks as if it should work, but seems to get things in a funny state where
			// the parser can't connect.
			//DbOps.ExecuteStoredProc(m_fdoCache, "SET TRANSACTION ISOLATION LEVEL REPEATABLE READ", null);

			// We don't want the parser messing around with things until we get done, because it
			// might delete the new analysis before we get things initialized to the point where
			// it can tell the analysis is in use.
			ParserScheduler parser = PauseParserIfRunning();

			int hvoOldWf;
			int hvoTargetWordform;

			try
			{
				using (new UndoRedoTaskHelper(Cache, ITextStrings.ksUndoApproveAnalysis, ITextStrings.ksRedoApproveAnalysis))
				{
					//Cache.BeginUndoTask(ITextStrings.ksUndoApproveAnalysis, ITextStrings.ksRedoApproveAnalysis);
					int hvoNewAnalysis = FocusBox.InterlinWordControl.GetRealAnalysis(fSaveGuess);
					if (hvoNewAnalysis == hvoOldAnalysis)
					{
						//Cache.EndUndoTask(); // typically will be discarded, contains nothing.
						return;		// nothing changed, so nothing else to do.
					}
					AddUndoRedoAction(hvoOldAnnotation, 0);
					int hvoOldAnalysisBackup = hvoOldAnalysis; // just in case something changes it before we check for unattested msas.
					IVwCacheDa cda = m_fdoCache.VwCacheDaAccessor;
					ISilDataAccess sda = m_fdoCache.MainCacheAccessor;

					hvoOldAnnotation = CacheAnalysisForAnnotation(hvoOldAnnotation, hvoNewAnalysis, sda, out hvoOldWf, out hvoTargetWordform);
					FinishSettingAnalysis(hvoNewAnalysis, hvoOldAnalysisBackup);

					if (hvoOldAnnotation != hvoNextAnnotation)
						AddUndoRedoAction(0, hvoNextAnnotation);
					hvoOldAnalysis = hvoNewAnalysis; // in case redisplaying the same place, e.g., turning on morphology.
					//Cache.EndUndoTask();
				}
			}
			finally
			{
				// This is our default isolation level. It would be even cleaner to retrieve the old
				// isolation level before we change it and restore that.
				// Didn't work.
				// DbOps.ExecuteStoredProc(m_fdoCache, "SET TRANSACTION ISOLATION LEVEL READ COMMITTED", null);

				// If we paused the parser, restart it now.
				if (parser != null)
					parser.Resume();
			}
			Set<int> wordforms = new Set<int>();
			wordforms.Add(hvoOldWf);
			wordforms.Add(hvoTargetWordform);
			UpdateGuesses(wordforms);
		}

		private void FinishSettingAnalysis(int hvoNewAnalysis, int hvoOldAnalysisBackup)
		{
			if (hvoNewAnalysis == hvoOldAnalysisBackup)
				return;
			List<int> msaHvoList = new List<int>();
			int wfAnalHvo = FocusBox.InterlinWordControl.GetWfiAnalysisHvoOfAnalysis(hvoOldAnalysisBackup);
			IWfiAnalysis anal = null;
			if (wfAnalHvo != 0)
			{
				anal = new WfiAnalysis(m_fdoCache, wfAnalHvo);
				anal.CollectReferencedMsaHvos(msaHvoList);
			}
			// Collecting for the new analysis is probably overkill, since the MissingEntries combo will only have MSAs
			// that are already referenced outside of the focus box (namely by the Senses). It's unlikely, therefore,
			// that we could configure the Focus Box in such a state as to remove the last link to an MSA in the
			// new analysis.  But just in case it IS possible...
			wfAnalHvo = FocusBox.InterlinWordControl.GetWfiAnalysisHvoOfAnalysis(hvoNewAnalysis);
			if (wfAnalHvo != 0)
			{
				anal = new WfiAnalysis(m_fdoCache, wfAnalHvo);
				anal.CollectReferencedMsaHvos(msaHvoList);
				// Make sure this analysis is marked as user-approved (green check mark)
				m_fdoCache.LangProject.DefaultUserAgent.SetEvaluation(wfAnalHvo, 1, "");
			}

			MoMorphSynAnalysis.DeleteUnusedMsas(m_fdoCache, msaHvoList);
			// if we can't find any instances to the old wordform, try to delete
			// a redundant capitalized form.
			if (Cache.GetClassOfObject(hvoOldAnalysisBackup) == WfiWordform.kclsidWfiWordform)
			{
				bool fInstanceOfFound = false;
				foreach (int hvoInstanceOf in GetNextInstanceOf())
				{
					if (hvoInstanceOf == hvoOldAnalysisBackup)
					{
						fInstanceOfFound = true;
						break;
					}
				}
				if (!fInstanceOfFound)
					WfiWordform.DeleteRedundantCapitalizedWordform(m_fdoCache, hvoOldAnalysisBackup);
			}
			else if (!m_fdoCache.IsDummyObject(hvoOldAnalysisBackup))
			{
				// not a wordform analysis
				DeleteIfUnattested(hvoOldAnalysisBackup);
			}
			// if we're switching to a different annotation, save that information,
			// so that we can move the sandbox back to the annotation being undone.
		}

		private int CacheAnalysisForAnnotation(int hvoAnnotation, int hvoNewAnalysis, ISilDataAccess sda)
		{
			int hvoOldWf;
			int hvoTargetWordform;
			return CacheAnalysisForAnnotation(hvoAnnotation, hvoNewAnalysis, sda, out hvoOldWf, out hvoTargetWordform);
		}
		private int CacheAnalysisForAnnotation(int hvoAnnotation, int hvoNewAnalysis, ISilDataAccess sda, out int hvoOldWf, out int hvoTargetWordform)
		{
			Debug.Assert(hvoAnnotation != 0);
			if (m_fdoCache.IsDummyObject(hvoAnnotation))
			{
				// convert the old annotation to a real one, since it's a dummy.
				CmBaseAnnotation cbaReal = CmObject.ConvertDummyToReal(m_fdoCache, hvoAnnotation) as CmBaseAnnotation;
				hvoAnnotation = cbaReal != null ? cbaReal.Hvo : 0;
			}
			// Record the old wordform before we alter InstanceOf.
			WfiWordform.TryGetWfiWordformFromInstanceOf(m_fdoCache, hvoAnnotation, out hvoOldWf);

			// This is the property that each 'in context' object has that points at one of the WfiX classes as the
			// analysis of the word.
			m_fdoCache.SetObjProperty(hvoAnnotation, (int)CmBaseAnnotation.CmAnnotationTags.kflidInstanceOf,
									  hvoNewAnalysis);

			// In case the wordform we point at has a form that doesn't match, we may need to set up an overidden form for the annotation.
			if (WfiWordform.TryGetWfiWordformFromInstanceOf(m_fdoCache, hvoAnnotation, out hvoTargetWordform))
			{
				TryCacheRealWordForm(hvoAnnotation);
			}
			if (hvoTargetWordform != hvoOldWf)
			{
				// We've changed wordform, typically because what happened in the sandbox involved picking a
				// different case form. In principle it seems we should adjust both occurrences lists.
				// In practice, any time we display a concordance we rebuild the lists, so adding is not
				// necessary. But we must do the deletion, because we need an accurate count in case we
				// can delete an unused capitalized wordform.
				int kflidOcurrences = WfiWordform.OccurrencesFlid(m_fdoCache);
				if (hvoOldWf != 0)
				{
					int ihvo = m_fdoCache.GetObjIndex(hvoOldWf, kflidOcurrences, hvoAnnotation);
					if (ihvo >= 0)
					{
						CacheReplaceOneUndoAction.SetItUp(Cache, hvoOldWf, kflidOcurrences, ihvo, ihvo + 1, new int[0]);
					}
				}
			}

			// If there is a cached default, clear it out...otherwise if the analysis we just chose was a
			// WfiAnalysis, it (or one of its glosses) may still show as a default.
			CacheObjPropUndoAction.SetItUp(Cache, hvoAnnotation, m_vc.ktagTwficDefault, 0);
			return hvoAnnotation;
		}

		private bool BaselineFormDiffersFromAnalysisWord(int hvoAnnotation, out ITsString baselineCbaForm)
		{
			ICmBaseAnnotation cbaRealAnnotation = CmBaseAnnotation.CreateFromDBObject(m_fdoCache, hvoAnnotation);
			baselineCbaForm = StTxtPara.TssSubstring(cbaRealAnnotation);
			int wsBaselineCbaForm = StringUtils.GetWsAtOffset(baselineCbaForm, 0);
			// We've updated the annotation to have InstanceOf set to the NEW analysis, so what we now derive from
			// that is the NEW wordform.
			int hvoNewWf = WfiWordform.GetWfiWordformFromInstanceOf(Cache, hvoAnnotation);
			WfiWordform wfNew = new WfiWordform(Cache, hvoNewWf);
			ITsString tssWfNew = wfNew.Form.GetAlternativeTss(wsBaselineCbaForm);
			return !baselineCbaForm.Equals(tssWfNew);
		}

		private void TryCacheRealWordForm(int hvoAnnotation)
		{
			ITsString tssBaselineCbaForm;
			if (BaselineFormDiffersFromAnalysisWord(hvoAnnotation, out tssBaselineCbaForm))
			{
				m_fdoCache.VwCacheDaAccessor.CacheStringProp(hvoAnnotation,
															 InterlinVc.TwficRealFormTag(m_fdoCache),
															 tssBaselineCbaForm);
			}
		}

		private bool TryCacheLowercasedForm(int hvoAnnotation)
		{
			int hvoWfiLower;
			if (TryGetTwficLowercaseSentenceInitialForm(hvoAnnotation, out hvoWfiLower))
			{
				m_fdoCache.VwCacheDaAccessor.CacheObjProp(hvoAnnotation,
														  InterlinVc.MatchingLowercaseWordForm(m_fdoCache),
														  hvoWfiLower);
				return true;
			}
			return false;
		}

		private ParserScheduler PauseParserIfRunning()
		{
			ParserScheduler parser = null;
			if (ParserFactory.HasParser(m_fdoCache.ServerName, m_fdoCache.DatabaseName,
				m_fdoCache.LangProject.Name.AnalysisDefaultWritingSystem))
			{
				// Getting the parser can fail with an internal error message something like
				//	Object '/8b9d17e1_bb1e_4fb3_b84a_1ac50b02c4ed/gm6vzmwmfhwbcnsyu085vinz_105.rem' has been disconnected or does not exist at the server
				// See LT-8704
				try
				{
					parser = ParserFactory.GetDefaultParser(m_fdoCache.ServerName, m_fdoCache.DatabaseName,
						m_fdoCache.LangProject.Name.AnalysisDefaultWritingSystem);
					if (parser.IsPaused)
						parser = null; // nothing to do when closed
					else
						if (!parser.AttemptToPause())
							Debug.Fail("Could not pause parser.");
				}
				catch
				{
					parser = null;
					Debug.WriteLine("UpdateRealFromSandbox(): ParserFactory.GetDefaultParser() threw an error?!");
				}
			}
			return parser;
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			// detect whether the user is doing a range selection with the keyboard within
			// a freeform annotation, and try to keep the selection within the bounds of the editable selection. (LT-2910)
			if (RootBox != null && (e.Modifiers & Keys.Shift) == Keys.Shift)
			{
				TextSelInfo tsi = new TextSelInfo(RootBox);
				int hvoAnchor = tsi.HvoAnchor;
				if (tsi.TagAnchor == (int)CmAnnotation.CmAnnotationTags.kflidComment
					&& Cache.GetClassOfObject(hvoAnchor) == CmIndirectAnnotation.kclsidCmIndirectAnnotation)
				{
					// we are in the comment field of an indirect annotation (assume freeform annotation).
					// so, extend the selection to the limit of the comment.
					if (e.KeyCode == Keys.Home)
					{
						// extend the selection to the beginning of the comment.
						SelectionHelper selHelper = SelectionHelper.GetSelectionInfo(tsi.Selection, this);
						selHelper.IchEnd = 0;
						selHelper.MakeRangeSelection(RootBox, true);
						return;
					}
				}
			}
			// LT-9570 for the Tree Translation line Susanna wanted Enter to copy Word Glosses
			// into the Free Translation line. Note: DotNetBar is not handling shortcut="Enter"
			// for the XML <command id="CmdAddWordGlossesToFreeTrans"...
			if (RootBox != null && e.KeyCode == Keys.Enter)
				OnAddWordGlossesToFreeTrans(e);
			base.OnKeyDown(e);
		}

		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			// if we're trying to replace a user prompt, record which line of a multilingual annotation
			// is being changed.  See LT-9421.
			SetCpropPreviousForInsert();
			base.OnKeyPress(e);
		}

		public override void PrePasteProcessing()
		{
			CheckDisposed();
			// if we're trying to replace a user prompt, record which line of a multilingual annotation
			// is being changed.  See LT-9421.
			SetCpropPreviousForInsert();
		}

		/// <summary>
		/// This computes and saves the information needed to ensure the insertion point is
		/// placed on the correct line of a multilingual annotation when replacing a user
		/// prompt.  See LT-9421.
		/// </summary>
		private void SetCpropPreviousForInsert()
		{
			m_cpropPrevForInsert = -1;
			if (RootBox != null)
			{
				TextSelInfo tsi = new TextSelInfo(RootBox);
				int hvoAnchor = tsi.HvoAnchor;
				if (tsi.TagAnchor == SimpleRootSite.kTagUserPrompt
					&& Cache.GetClassOfObject(hvoAnchor) == CmIndirectAnnotation.kclsidCmIndirectAnnotation)
				{
					SelectionHelper helper = SelectionHelper.GetSelectionInfo(tsi.Selection, this);
					int wsField = 0;
					if (tsi.TssAnchor != null && tsi.TssAnchor.Length > 0)
						wsField = StringUtils.GetWsAtOffset(tsi.TssAnchor, 0);
					else
						return;
					int hvoField = tsi.HvoAnchor;
					SelLevInfo[] rgsli = helper.GetLevelInfo(SelectionHelper.SelLimitType.Anchor);
					int itagSegments = -1;
					for (int i = rgsli.Length; --i >= 0; )
					{
						if (rgsli[i].tag == m_vc.ktagParaSegments)
						{
							itagSegments = i;
							break;
						}
					}
					if (itagSegments >= 0)
					{
						int hvoSeg = rgsli[itagSegments].hvo;
						int hvoType = Cache.GetObjProperty(hvoField, (int)CmAnnotation.CmAnnotationTags.kflidAnnotationType);
						int idx = 0;
						InterlinLineChoices choices = m_vc.LineChoices;
						for (int i = choices.FirstFreeformIndex; i < choices.Count; )
						{
							int hvoAnnType = m_vc.SegDefnFromFfFlid(choices[i].Flid);
							if (hvoAnnType == hvoType)
							{
								idx = i;
								break; // And that's where we want our selection!!
							}
							// Adjacent WSS of the same annotation count as only ONE object in the display.
							// So we advance i over as many items in m_choices as there are adjacent Wss
							// of the same flid.
							i += choices.AdjacentWssAtIndex(i).Length;
						}
						int[] rgws = choices.AdjacentWssAtIndex(idx);
						for (int i = 0; i < rgws.Length; ++i)
						{
							if (rgws[i] == wsField)
							{
								m_cpropPrevForInsert = i;
								break;
							}
						}
					}
				}
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (AllowLayout == false || !InValidState)
				return;
			base.OnMouseMove(e);
		}

		public bool OnUndo(object args)
		{
			if (Cache.CanUndo)
			{
				using (UndoRedoSyncTextAndSandbox syncHelper = new UndoRedoSyncTextAndSandbox(this))
				{
					UndoResult ures;
					Cache.Undo(out ures);
					// Apparently Undo/Redo for making/breaking phrases depends upon being able to survive with kuresRefresh.
					//Debug.Assert(ures != UndoResult.kuresRefresh,
					//             "Undo/Redo indicates that the display needs to refresh, so the display and the cache is out of date. " +
					//             "In general, we want to write Undo/Redo tasks that are able to undo/redo without refreshing. " +
					//             "A common cause for this is calling DeleteObjects and inadvertently setting fRequiresFullRefreshOfViewInUndoRedo to true. " +
					//             "Please investigate further.");
				}
				return true;
			}
			return false;
		}
		/// <summary>
		/// Resync the Focus Box according to Undo/Redo (cf. LT-5330).
		/// </summary>
		/// <param name="undoRedoText"></param>
		private void ResyncSandboxToDatabase()
		{
			if (HvoAnnotation == 0)
				return;
			int hvoStTextOfCurrentText = GetStTextIdOfCurrentAnnotation();
			if (hvoStTextOfCurrentText == m_hvoRoot)
				ReconstructAndRecreateSandbox(false);
		}

		protected virtual void UpdateDisplayAfterUndo()
		{
			ReconstructAndRecreateSandbox(false);
		}

		private StText GetStTextOfCurrentAnnotation()
		{
			StTxtPara.TwficInfo twficInfo = new StTxtPara.TwficInfo(Cache, HvoAnnotation);
			StTxtPara para = twficInfo.Object.BeginObjectRA as StTxtPara;
			StText text = para.Owner as StText;
			return text;
		}

		private class UndoRedoSyncTextAndSandbox : RecordClerk.ListUpdateHelper
		{
			InterlinDocChild m_idc;
			int m_hvoOriginalAnnotation = 0;

			internal UndoRedoSyncTextAndSandbox(InterlinDocChild idc) :
				base(idc.ActiveClerk)
			{
				SkipShowRecord = true;
				TriggerPendingReloadOnDispose = false; // suspend reloading clerk.
				m_idc = idc;
				m_hvoOriginalAnnotation = m_idc.HvoAnnotation;
				// we may not have a sandbox due to a freeform annotation selection.
				if (m_hvoOriginalAnnotation != 0)
					m_idc.AbandonChangesInFocusBox();

				m_idc.AllowLayout = false;
				// may be overkill, but i don't think we always get PropChanges for
				// restored or deleted objects.
				WordformInventory.OnChangedWordformsOC();
			}

			protected override void Dispose(bool disposing)
			{
				if (disposing == true)
				{
					// let's make sure the active clerk doesn't have any invalid items as a result from the
					// the Undo/Redo.
					RecordClerk activeClerk = m_idc.ActiveClerk;
					if (activeClerk != null)
						activeClerk.RemoveInvalidItems();
					// let's make sure the clerk driving the master doesn't have any invalid items.
					InterlinMaster master = m_idc.GetMaster();
					if (master != null && master.Clerk != activeClerk)
						master.Clerk.RemoveInvalidItems();

					m_idc.AllowLayout = true;
					if (m_hvoOriginalAnnotation != 0)
					{
						if (m_idc.CurrentParaIsValid())
							m_idc.ResyncSandboxToDatabase();
						else
							m_idc.MessageBoxMasterRefresh();
					}
					else
					{
						// redisply the new state of things
						// possibly with or without a freeform annotation.
						m_idc.UpdateDisplayAfterUndo();
					}
				}
				base.Dispose(disposing);
				m_idc = null;
			}
		}

		internal bool CurrentParaIsValid()
		{
			if (ForEditing == false)
				return true;	// not tracking a current annotation.
			if (HvoAnnotation == 0)
				return false;
			StTxtPara.TwficInfo currentTwficInfo = new StTxtPara.TwficInfo(Cache, HvoAnnotation);
			if (!currentTwficInfo.IsObjectValid())
				return false;
			// go through the current paragraph and validate its segments and twfics.
			StTxtPara para = currentTwficInfo.Object.BeginObjectRA as StTxtPara;

			IVwVirtualHandler vh;
			if (Cache.TryGetVirtualHandler(StTxtPara.SegmentsFlid(Cache), out vh))
			{
				BaseVirtualHandler bvh = vh as BaseVirtualHandler;
				if (!bvh.IsPropInCache(Cache.MainCacheAccessor, para.Hvo, 0))
					return false;
			}
			int twficType = CmAnnotationDefn.Twfic(Cache).Hvo;
			foreach (int hvoSegment in para.Segments)
			{
				if (!Cache.IsValidObject(hvoSegment))
					return false;
				foreach (int hvoSegform in para.SegmentForms(hvoSegment))
				{
					// kflidAnnotationType
					int hvoAnnType = Cache.GetObjProperty(hvoSegform, (int)CmBaseAnnotation.CmAnnotationTags.kflidAnnotationType);
					if (hvoAnnType == twficType)
					{
						// do extra checking on twfics.
						StTxtPara.TwficInfo twficInfo = new StTxtPara.TwficInfo(Cache, hvoSegform);
						if (!twficInfo.IsObjectValid())
							return false;
					}
					else if (!Cache.IsValidObject(hvoSegform))
					{
						return false;
					}
				}
			}
			return true;
		}

		internal bool CurrentAnnotationIsValid()
		{
			if (ForEditing == false)
				return true;	// not tracking a current annotation.
			if (HvoAnnotation == 0)
				return false;
			StTxtPara.TwficInfo twficInfo = new StTxtPara.TwficInfo(Cache, HvoAnnotation);
			return twficInfo.IsObjectValid();
		}

		internal bool OnRedo(object args)
		{
			if (Cache.CanRedo)
			{
				using (UndoRedoSyncTextAndSandbox syncHelper = new UndoRedoSyncTextAndSandbox(this))
				{
					UndoResult ures;
					Cache.Redo(out ures);
				}
				return true;
			}
			return false;
		}


		private int GetStTextIdOfCurrentAnnotation()
		{
			if (HvoAnnotation == 0)
				return 0;
			StText text = GetStTextOfCurrentAnnotation();
			return text.Hvo;
		}

		/// <summary>
		/// If hvoOldAnalysis is 'unattested', get rid of it.
		/// It is 'unattested' if
		///		- No CmBaseAnnotation of type 'Wordform in context' has it as its InstanceOf
		///		- It is not the target of a CmAgentEvaluation owned by the parser with accepted = true.
		///		- It's owner is not parser-approved (this is relevant for WfiGlosses)
		///		- It doesn't own any WfiGlosses (relevant for WfiAnalyses)
		///		- It doesn't own any WfiAnalyses (relevant for WfiWordform)
		/// </summary>
		/// <param name="hvoOldAnalyis"></param>
		void DeleteIfUnattested(int hvoOldAnalysis)
		{
			// (LT-7457) Make sure this data wasn't deleted in another screen. If it
			// was, trying to delete it here will crash.
			if (m_fdoCache.IsValidObject(hvoOldAnalysis))
			{
				int hvoParserAgent = m_fdoCache.LangProject.DefaultParserAgent.Hvo;
				int hvoAnnDefnTwfic = CmAnnotationDefn.Twfic(m_fdoCache).Hvo;
				int hvoOwner = m_fdoCache.GetOwnerOfObject(hvoOldAnalysis);
				// This query counts the number of times any of the things that make it 'attested' occur.
				string sql = string.Format("select " +
				   "	(select count(*) from CmBaseAnnotation_ cba where cba.AnnotationType={0} and cba.InstanceOf={1})" +
				   " + (select count(*) from CmAgentEvaluation_ where owner$={2} and target={1} and Accepted=1)" +
				   " + (select count(*) from CmAgentEvaluation_ where owner$={2} and target={3} and Accepted=1)" +
				   " + (select count(*) from WfiGloss_ where owner$={1})" +
				   " + (select count(*) from WfiAnalysis_ where owner$={1})",
				   hvoAnnDefnTwfic, hvoOldAnalysis, hvoParserAgent, hvoOwner);
				int cAttestation;
				DbOps.ReadOneIntFromCommand(m_fdoCache, sql, null, out cAttestation);
				if (cAttestation != 0)
					return; // attested!
				ICmObject obj = CmObject.CreateFromDBObject(m_fdoCache, hvoOldAnalysis, false);
				bool fShouldTryOwner = obj is WfiAnalysis || obj is WfiGloss;
				obj.DeleteUnderlyingObject();
				// If we deleted this object, we may have made its owner unattested, unless the thing
				// we're deleting is s wordform...the lexical database is always attested!
				if (fShouldTryOwner)
					DeleteIfUnattested(hvoOwner);
			}
		}

		private bool m_fSuppressLoseFocus;
		private void DestroyFocusBoxAndSetFocus(bool fSaveGuess)
		{
			// Editing a freeform annotation...no special behavior, except hide the Sandbox.
			HideAndDestroyFocusBox(fSaveGuess);
			// See LT-1688.  Without this, a new selection created in an empty annotation
			// field is not visible.  Although typing is possible, pasting is not!
			//base.OnGotFocus(new EventArgs());

			// Somehow while executing Focus() or subsequently we LOSE focus!! Maybe we are sometimes changing from this to this?
			// I tried only calling Focus() if this.Focused is false, that didn't help, but this seems to.
			// Anyway, ignore any subsequent Losefocus until idle.
			m_fSuppressLoseFocus = true;
			Application.Idle += new EventHandler(EndSuppressLoseFocus);
			Focus();
		}

		void EndSuppressLoseFocus(object sender, EventArgs e)
		{
			m_fSuppressLoseFocus = false;
			Application.Idle -= new EventHandler(EndSuppressLoseFocus);
		}

		/// <summary>
		/// Hide the sandbox, typically because selection moving to a freefrom annotation.
		/// </summary>
		/// <param name="fSaveGuess">if true, saves guesses; if false, skips guesses but still saves edits.</param>
		void HideFocusBox(bool fSaveGuess)
		{
			if (!TryHideFocusBox())
				return;
			int oldAnnotation = m_hvoAnnotation;
			UpdateRealFromSandbox(fSaveGuess);
			HvoAnnotation = 0;
			m_hvoAnalysis = 0;
			SimulateReplaceAnnotation(oldAnnotation);
		}

		/// <summary>
		/// Hides the sandbox and removes it from the controls.
		/// </summary>
		/// <returns>true, if it could hide the sandbox. false, if it was not installed.</returns>
		internal bool TryHideFocusBox()
		{
			if (!IsFocusBoxInstalled)
				return false;
			m_vc.SandboxAnnotation = 0;
			FocusBox.Uninstall();
			return true;
		}

		/// <summary>
		/// Adds the sandbox to the control and makes it visible.
		/// </summary>
		/// <returns>true, if we made the sandbox visible, false, if we couldn't.</returns>
		bool TryShowFocusBox()
		{
			Debug.Assert(FocusBox.InterlinWordControl != null, "make sure sandbox is setup before trying to show it.");
			if (FocusBox.InterlinWordControl == null)
				return false;
			if (!Controls.Contains(FocusBox))
				Controls.Add(FocusBox); // Makes it real and gives it a root box.
			FocusBox.Visible = true;
			// Refresh seems to prevent the sandbox from blanking out (LT-9922)
			FocusBox.Refresh();
			return true;
		}

		// Is hvoAnn, currently analyzed as hvoAnalysis, fully analyzed?
		// This means:
		//  -- it isn't a default (property InterlinVc.m_vc.ktagTwficDefault isn't cached)
		//  -- It's a WfiGloss, with non-empty form.
		//  -- Owner is a WfiAnalysis with non-empty Category.
		//  -- Owner has at least one WfiMorphBundle.
		//  -- For each WfiMorphBundle, Form, Msa, and Sense are all filled in.
		// Alternatively, if hvoAnalysis is zero, the annotation is punctuation, which we don't analyze further;
		// so return true to indicate that it needs no further attention.
		internal bool FullyAnalyzed(FdoCache fdoCache, WsListManager listman, int hvoAnn, int hvoAnalysis)
		{
			int ktagTwficDefault = StTxtPara.TwficDefaultFlid(fdoCache);
			if (hvoAnalysis == 0)
				return true; // punctuation, treat as fully analyzed.
			ISilDataAccess sda = fdoCache.MainCacheAccessor;
			// Check for default. If the analysis we're showing is a default it needs at least confirmation.
			if (sda.get_IsPropInCache(hvoAnn, ktagTwficDefault, (int)CellarModuleDefns.kcptReferenceAtom, 0)
				&& sda.get_ObjectProp(hvoAnn, ktagTwficDefault) != 0)
				return false;

			int analysisClass = fdoCache.GetClassOfObject(hvoAnalysis);
			if (analysisClass != (int)WfiGloss.kclsidWfiGloss && analysisClass != (int)WfiAnalysis.kclsidWfiAnalysis)
				return false; // Has to BE an analysis...unless pathologically everything is off? Too bad if so...
			int hvoWfiAnalysis = fdoCache.GetOwnerOfObject(hvoAnalysis);
			int hvoWordform;
			if (analysisClass == (int)WfiAnalysis.kclsidWfiAnalysis)
			{
				hvoWordform = hvoWfiAnalysis;
				hvoWfiAnalysis = hvoAnalysis;
			}
			else
			{
				hvoWordform = fdoCache.GetOwnerOfObject(hvoWfiAnalysis);
			}

			foreach (InterlinLineSpec spec in m_vc.LineChoices)
			{
				// see if the information required for this linespec is present.
				switch (spec.Flid)
				{
					case InterlinLineChoices.kflidWord:
						int ws = m_vc.GetRealWs(hvoWordform, spec);
						if (sda.get_MultiStringAlt(hvoWordform, (int)WfiWordform.WfiWordformTags.kflidForm, ws).Length == 0)
							return false;
						break;
					case InterlinLineChoices.kflidLexEntries:
						if (!CheckPropSetForAllMorphs(sda, hvoWfiAnalysis, (int)WfiMorphBundle.WfiMorphBundleTags.kflidMorph))
							return false;
						break;
					case InterlinLineChoices.kflidMorphemes:
						if (!CheckPropSetForAllMorphs(sda, hvoWfiAnalysis, (int)WfiMorphBundle.WfiMorphBundleTags.kflidMorph))
							return false;
						break;
					case InterlinLineChoices.kflidLexGloss:
						if (!CheckPropSetForAllMorphs(sda, hvoWfiAnalysis, (int)WfiMorphBundle.WfiMorphBundleTags.kflidSense))
							return false;
						break;
					case InterlinLineChoices.kflidLexPos:
						if (!CheckPropSetForAllMorphs(sda, hvoWfiAnalysis, (int)WfiMorphBundle.WfiMorphBundleTags.kflidMsa))
							return false;
						break;
					case InterlinLineChoices.kflidWordGloss:
						// If it isn't a WfiGloss the user needs a chance to supply a word gloss.
						if (analysisClass != WfiGloss.kclsidWfiGloss)
							return false;
						// If it is empty for the (possibly magic) ws specified here, it needs filling in.
						int ws1 = m_vc.GetRealWs(hvoAnalysis, spec);
						if (sda.get_MultiStringAlt(hvoAnalysis, (int)WfiGloss.WfiGlossTags.kflidForm, ws1).Length == 0)
							return false;
						break;
					case InterlinLineChoices.kflidWordPos:
						if (sda.get_ObjectProp(hvoWfiAnalysis, (int)WfiAnalysis.WfiAnalysisTags.kflidCategory) == 0)
							return false;
						break;
					case InterlinLineChoices.kflidFreeTrans:
					case InterlinLineChoices.kflidLitTrans:
					case InterlinLineChoices.kflidNote:
					default:
						// unrecognized or non-word-level annotation, nothing required.
						break;
				}
			}

			return true; // If we can't find anything to complain about, it's fully analyzed.
		}

		// Check that the specified WfiAnalysis includes at least one morpheme bundle, and that all morpheme
		// bundles have the specified property set. Return true if all is well.
		private static bool CheckPropSetForAllMorphs(ISilDataAccess sda, int hvoWfiAnalysis, int flid)
		{
			int cbundle = sda.get_VecSize(hvoWfiAnalysis, (int)WfiAnalysis.WfiAnalysisTags.kflidMorphBundles);
			if (cbundle == 0)
				return false;
			for (int ibundle = 0; ibundle < cbundle; ibundle++)
			{
				int hvoBundle = sda.get_VecItem(hvoWfiAnalysis, (int)WfiAnalysis.WfiAnalysisTags.kflidMorphBundles, ibundle);
				if (sda.get_ObjectProp(hvoBundle, flid) == 0)
					return false;
			}
			return true;
		}

		/// <summary>
		/// Get the array of SelLevInfo corresponding to one end point of a selection.
		/// </summary>
		/// <param name="vwselNew"></param>
		/// <param name="fEndPoint">True if we want the end of the selection. False if we want the anchor.</param>
		/// <returns></returns>
		protected static SelLevInfo[] GetOneEndPointOfSelection(IVwSelection vwselNew, bool fEndPoint)
		{
			// Get the info about the other end of the selection.
			int ihvoRoot, tagTextProp, cpropPrevious, ich, ws;
			bool fAssocPrev;
			ITsTextProps ttpSelProps;
			int cvsli = vwselNew.CLevels(fEndPoint) - 1;
			SelLevInfo[] rgvsliEnd;
			using (ArrayPtr prgvsli = MarshalEx.ArrayToNative(cvsli, typeof(SelLevInfo)))
			{
				vwselNew.AllSelEndInfo(fEndPoint, out ihvoRoot, cvsli, prgvsli,
					out tagTextProp, out cpropPrevious, out ich,
					out ws, out fAssocPrev, out ttpSelProps);
				rgvsliEnd = (SelLevInfo[])MarshalEx.NativeToArray(prgvsli, cvsli,
						typeof(SelLevInfo));
			}
			return rgvsliEnd;
		}

		protected static int GetWfiAnalysisIndexInSelLevInfoArray(SelLevInfo[] rgvsli)
		{
			// Identify the twfic, and the position in rgvsli of the property holding it.
			// It is also possible that the twfic is the root object.
			// This is important because although we are currently displaying just an StTxtPara,
			// eventually it might be part of a higher level structure. We want to be able to
			// reproduce everything that gets us down to the twfic.
			int result = -1;
			for (int i = rgvsli.Length; --i >= 0; )
			{
				if (rgvsli[i].tag == TagAnalysis)
				{
					result = i;
					break;
				}
			}
			return result;
		}

		/// <summary>
		/// Handles a view selection produced by a click. Return true to suppress normal
		/// mouse down handling, indicating that an interlinear bundle has been clicked and the Sandbox
		/// moved.
		/// </summary>
		/// <param name="vwselNew"></param>
		/// <param name="fBundleOnly"></param>
		/// <param name="fSaveGuess">if true, saves guesses; if false, skips guesses but still saves edits.</param>
		/// <returns></returns>
		protected virtual bool HandleClickSelection(IVwSelection vwselNew, bool fBundleOnly, bool fSaveGuess)
		{
			if (vwselNew == null)
				return false; // couldn't select a bundle!
			// The basic idea is to find the level at which we are displaying the TagAnalysis property.
			int cvsli = vwselNew.CLevels(false);
			cvsli--; // CLevels includes the string property itself, but AllTextSelInfo doesn't need it.

			// Out variables for AllTextSelInfo.
			int ihvoRoot;
			int tagTextProp;
			int cpropPrevious;
			int ichAnchor;
			int ichEnd;
			int ws;
			bool fAssocPrev;
			int ihvoEnd;
			ITsTextProps ttpBogus;
			// Main array of information retrived from sel that made combo.
			SelLevInfo[] rgvsli = SelLevInfo.AllTextSelInfo(vwselNew, cvsli,
				out ihvoRoot, out tagTextProp, out cpropPrevious, out ichAnchor, out ichEnd,
				out ws, out fAssocPrev, out ihvoEnd, out ttpBogus);

			if (tagTextProp == (int)CmAnnotation.CmAnnotationTags.kflidComment)
			{
				bool fWasFocusBoxInstalled = IsFocusBoxInstalled;
				Rect oldSelLoc = GetPrimarySelRect(vwselNew);
				if (!fBundleOnly)
					DestroyFocusBoxAndSetFocus(fSaveGuess);

				// If the selection resulting from the click is still valid, and we just closed the focus box, go ahead and install it;
				// continuing to process the click may not produce the intended result, because
				// removing the focus box can re-arrange things substantially (LT-9220).
				// (However, if we didn't change anything it is necesary to process it normally, otherwise, dragging
				// and shift-clicking in the free translation don't work.)
				if (!vwselNew.IsValid || !fWasFocusBoxInstalled)
					return false;
				// We have destroyed a focus box...but we may not have moved the free translation we clicked enough
				// to cause problems. If not, we'd rather do a normal click, because installing a selection that
				// the root box doesn't think is from mouse down does not allow dragging.
				Rect selLoc = GetPrimarySelRect(vwselNew);
				if (selLoc.top == oldSelLoc.top)
					return false;
				vwselNew.Install();
				return true;
			}

			// Identify the twfic, and the position in m_rgvsli of the property holding it.
			// It is also possible that the twfic is the root object.
			// This is important because although we are currently displaying just an StTxtPara,
			// eventually it might be part of a higher level structure. We want to be able to
			// reproduce everything that gets us down to the twfic.
			int itagAnalysis = -1;
			for (int i = rgvsli.Length; --i >= 0; )
			{
				if (rgvsli[i].tag == TagAnalysis)
				{
					itagAnalysis = i;
					break;
				}
			}
			if (itagAnalysis < 0)
			{
				// Go ahead and hide the focus box, since it could try to still the selection back (cf. LT-7968)
				if (!fBundleOnly)
					DestroyFocusBoxAndSetFocus(fSaveGuess);
				return false; // Selection is somewhere we can't handle.
			}
			int hvoAnalysis = rgvsli[itagAnalysis].hvo; // The current analyis object.
			Debug.Assert(itagAnalysis < rgvsli.Length - 1); // Need different approach if the twfic is the root.
			int hvoAnnotation = rgvsli[itagAnalysis + 1].hvo;

			// Launch a combo on the base line.
			//			if (tagTextProp == (int)WfiWordform.WfiWordformTags.kflidForm)
			//			{
			//				// First line: display the in-place combo.
			//				analysisHandler = new ChooseAnalysisHandler(m_fdoCache, hvoSrc, hvoAnalysis);
			//				analysisHandler.AnalysisChosen += analysisChosenDelegate;
			//				analysisHandler.SetupCombo();
			//				analysisHandler.Show(m_site, m_vwselNew);
			//				return;
			//			}

			// Enhance JohnT: if click was on word gloss line, put cursor there instead of default.
			TriggerAnnotationSelected(hvoAnnotation, hvoAnalysis, fSaveGuess);
			//			new HandleSelectionChangeMethod(vwselNew, this)
			//				.Run(ref m_analysisHandler, m_vc.ListManager,
			//					new EventHandler(m_analysisHandler_AnalysisChosen), new AdvanceWordEventHandler(scope_AdvanceWord));
			return true;
		}

		internal InterlinMaster GetMaster()
		{
			for (Control parentControl = this.Parent; parentControl != null; parentControl = parentControl.Parent)
			{
				if (parentControl is InterlinMaster)
					return parentControl as InterlinMaster;
			}
			return null;
		}

		// Return whether this view is in a valid state. If it is out of sync with its master
		// (e.g., while being prematurely made visible by the blanky-blank tab control),
		// various things are just not safe to do.
		private bool InValidState
		{
			get
			{
				InterlinMaster master = this.GetMaster();
				if (master == null)
					return m_hvoRoot != 0; //&& CurrentAnnotationIsValid();
				else
					return m_hvoRoot == master.RootHvo; // && CurrentAnnotationIsValid();
			}
		}


		protected override void OnLayout(System.Windows.Forms.LayoutEventArgs levent)
		{
			if (!InValidState)
				return;
			base.OnLayout(levent);
		}

		// Called in the middle of OnLayout, this allows us to move our Sandbox to its proper position.
		protected override void MoveChildWindows()
		{
			// (LT-5932) do this in InterlinMaster.OnLayout to help avoid 'random' crash.
			//MoveFocusBoxIntoPlace();
		}

		#endregion

		private void m_analysisHandler_AnalysisChosen(object sender, EventArgs e)
		{
			ChooseAnalysisHandler handler = (ChooseAnalysisHandler)sender;
			m_fdoCache.SetObjProperty(handler.Source, TagAnalysis, handler.Analysis);
		}

		/// <summary>
		/// Requirement is to figure the next annotaion after e.Annotation and store it
		/// along with its current annotation in e.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// <param name="fNeedsAnalysis">True to advance to a word that needs analysis;
		/// false to just advance to the immediately next word.</param>
		public void AdvanceWord(object sender, AdvanceWordArgs e, bool fNeedsAnalysis)
		{
			int hvoCurrent = e.Annotation;
			ISilDataAccess sda = m_fdoCache.MainCacheAccessor;
			int hvoPara;
			if (CmObject.IsValidObject(m_fdoCache, hvoCurrent))
			{
				int twficType = CmAnnotationDefn.Twfic(Cache).Hvo;
				// We should assert that hvoCurrent is Twfic
				int annoType = sda.get_ObjectProp(hvoCurrent,
					(int)CmBaseAnnotation.CmAnnotationTags.kflidAnnotationType);
				Debug.Assert(annoType == twficType, "Given annotation type should be twfic("
					+ twficType + ") but was " + annoType + ".");
				hvoPara = sda.get_ObjectProp(hvoCurrent,
					(int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginObject);
			}
			else
			{
				// Start with first para of text
				if (m_hvoRoot == 0)
					return; // can't do anything.
				int hvoStText = m_hvoRoot;
				if (sda.get_VecSize(hvoStText, (int)StText.StTextTags.kflidParagraphs) == 0)
					return;	// newly created, no contents yet.
				hvoPara = sda.get_VecItem(hvoStText, (int)StText.StTextTags.kflidParagraphs, 0);
			}
			if (hvoPara != 0)
			{
				int cseg = sda.get_VecSize(hvoPara, m_vc.ktagParaSegments);
				// This flag is set if we have found the word we're advancing from...or
				// immediately, if hvoCurrent is zero, which means we're 'advancing' from the
				// start of the text.
				bool prevMatch = (hvoCurrent == 0);
				for (int iseg = 0; iseg < cseg; iseg++)
				{
					int hvoSeg = sda.get_VecItem(hvoPara, m_vc.ktagParaSegments, iseg);
					int cann = sda.get_VecSize(hvoSeg, m_vc.ktagSegmentForms);
					for (int iann = 0; iann < cann; iann++)
					{
						int hvoAnn = sda.get_VecItem(hvoSeg, m_vc.ktagSegmentForms,
							iann);
						if (prevMatch)
						{
							int hvoAnalysis = sda.get_ObjectProp(hvoAnn,
								(int)CmAnnotation.CmAnnotationTags.kflidInstanceOf);
							if (hvoAnalysis == 0 || !m_vc.CanBeAnalyzed(hvoAnn))
								continue; // skip punctuation.
							if (fNeedsAnalysis && FullyAnalyzed(m_fdoCache,
								m_vc.ListManager, hvoAnn, hvoAnalysis))
								continue;
							// Some previous annotation was a match, and this one isn't fully
							// analyzed, so this is the one we want.
							e.Annotation = hvoAnn;
							e.Analysis = hvoAnalysis;
							// if, for some reason, the next annotation is invalid
							// reset these to 0.
							if (!CmObject.IsValidObject(m_fdoCache, hvoAnn))
							{
								e.Annotation = 0;
								e.Analysis = 0;
							}
							return;
						}
						prevMatch = (hvoAnn == hvoCurrent);
					}
				}
				// Didn't find another one in the expected paragraph.
				// If prevmatch is false, something is wrong.
				// if prevMatch is true, hvoCurrent is the last word in the paragraph, and we
				// should move to the next.  But it's complicated: it might be a paragraph we
				// haven't expanded from laziness yet, and which therefore doesn't have all its
				// segment info cached.  For now we'll skip doing that.
				// Sorry, we can't be that lazy about handling laziness!  So here goes ...
				Debug.Assert(prevMatch);
				Debug.Assert(m_rootb != null);

				// In the loop above, we searched the paragraph for the relevant word.  Now we
				// need to search the text for the relevant paragraph, and return the first word
				// in the next paragraph.
				// Start with first para of text
				if (m_hvoRoot == 0)
					return; // can't do anything.
				int hvoStText = m_hvoRoot;
				if (hvoStText == 0)
					return; // newly created text, has no Contents yet.
				int cpara = sda.get_VecSize(hvoStText, (int)StText.StTextTags.kflidParagraphs);
				if (cpara == 0)
					return;

				SelLevInfo[] rgvsli = new SelLevInfo[1];
				rgvsli[0].tag = (int)StText.StTextTags.kflidParagraphs;
				rgvsli[0].cpropPrevious = 0;
				rgvsli[0].ihvo = 0;
				rgvsli[0].hvo = 0;
				rgvsli[0].ws = 0;
				rgvsli[0].ich = 0;
				for (int ipara = 0; ipara < cpara; ++ipara)
				{
					int hvoPara1 = sda.get_VecItem(hvoStText,
						(int)StText.StTextTags.kflidParagraphs, ipara);
					if (hvoPara1 == hvoPara && (ipara + 1) < cpara)
					{
						rgvsli[0].ihvo = ipara + 1;
						hvoPara = sda.get_VecItem(hvoStText,
							(int)StText.StTextTags.kflidParagraphs, ipara + 1);
						if (GetWordInNextPara(hvoPara, rgvsli, e, fNeedsAnalysis))
							return;
					}
				}
			}
		}

		/// <summary>
		///
		///
		/// </summary>
		/// <param name="hvoPara"></param>
		/// <param name="rgvsli"></param>
		/// <param name="e"></param>
		/// <param name="fNeedsAnalysis"></param>
		private bool GetWordInNextPara(int hvoPara, SelLevInfo[] rgvsli, AdvanceWordArgs e,
			bool fNeedsAnalysis)
		{
			// This may seem a bit of a hack, but should minimize the amount of code exercised
			// by loading the data for a lazy paragraph: we make an object selection of the next
			// paragraph, which automatically loads things into the cache if needed, but does
			// minimal work if it's already there.
			IVwSelection selNew = m_rootb.MakeTextSelInObj(
				/*[in] int ihvoRoot */ 0,
				/*[in] int cvsli */ rgvsli.Length,
				/*[in, size_is(cvsli)] VwSelLevInfo * */ rgvsli,
				/*[in] int cvsliEnd */ 0,
				/*[in, size_is(cvsliEnd)] VwSelLevInfo * rgvsliEnd */ null,
				/*[in] ComBool fInitial */ false,
				/*[in] ComBool fEdit */ false,
				/*[in] ComBool fRange */ false,
				/*[in] ComBool fWholeObj */ true,
				/*[in] ComBool fInstall */ false
				);
			if (selNew == null)
			{
				// If we can't even make a selection, assume there's nothing there.
				return false;
			}
			ISilDataAccess sda = m_fdoCache.MainCacheAccessor;
			int cseg = sda.get_VecSize(hvoPara, m_vc.ktagParaSegments);
			for (int iseg = 0; iseg < cseg; iseg++)
			{
				int hvoSeg = sda.get_VecItem(hvoPara, m_vc.ktagParaSegments, iseg);
				int cann = sda.get_VecSize(hvoSeg, m_vc.ktagSegmentForms);
				for (int iann = 0; iann < cann; iann++)
				{
					int hvoAnn = sda.get_VecItem(hvoSeg, m_vc.ktagSegmentForms,
						iann);
					int hvoAnalysis = sda.get_ObjectProp(hvoAnn,
						(int)CmAnnotation.CmAnnotationTags.kflidInstanceOf);
					if (hvoAnalysis == 0)
						continue;	// skip punctuation.
					if (fNeedsAnalysis && FullyAnalyzed(m_fdoCache,
						m_vc.ListManager, hvoAnn, hvoAnalysis))
						continue;	// conditionally skip analyzed words.
					// This is the one we want.
					e.Annotation = hvoAnn;
					e.Analysis = hvoAnalysis;
					return true;
				}
			}
			// Nothing valid in this paragraph: either no words or no unanalyzed words.
			return false;
		}

		/// <summary>
		/// Requirement is to figure the annotaion before e.Annotation and store it
		/// along with its current annotation in e.
		/// If e.Annotation is 0, go to the very last word of the document.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void BackWord(object sender, AdvanceWordArgs e, bool fNeedsAnalysis)
		{
			int[] indexes;
			int[] hvos;
			int[] tags = new int[] { (int)StText.StTextTags.kflidParagraphs,
									   m_vc.ktagParaSegments,
									   m_vc.ktagSegmentForms };
			ISilDataAccess sda = m_rootb.DataAccess;
			if (e.Annotation == 0)
			{
				// Very last word.
				// We set indexes as if the current word was at the very start of the non-existent
				// paragraph after the last one.
				hvos = new int[tags.Length];
				hvos[0] = m_hvoRoot;
				indexes = new int[tags.Length]; // full of zeros.
				indexes[0] = m_fdoCache.MainCacheAccessor.get_VecSize(hvos[0], (int)StText.StTextTags.kflidParagraphs);
			}
			else
			{
				if (IndexOf(m_hvoRoot, tags, e.Annotation, out indexes, out hvos) < 0)
				{
					return; // can't find it at all.
				}
			}

			// This is used for ensuring that paragraphs are loaded.
			SelLevInfo[] rgvsli = new SelLevInfo[1];
			rgvsli[0].tag = (int)StText.StTextTags.kflidParagraphs;
			rgvsli[0].cpropPrevious = 0;
			rgvsli[0].ihvo = 0;
			rgvsli[0].hvo = 0;
			rgvsli[0].ws = 0;
			rgvsli[0].ich = 0;

			// Loop until we can't go back further or find a valid previous word.
			for (; ; )
			{
				int ilev;
				// Work back till we find a non-zero index we can decrement.
				for (ilev = indexes.Length - 1; ilev >= 0 && indexes[ilev] == 0; ilev--)
					;
				if (ilev < 0)
					return; // at start.
				indexes[ilev]--;
				if (ilev == 0)
				{
					// Ensure that the paragraph we're moving back to is not lazy.
					// This may seem a bit of a hack, but should minimize the amount of code
					// exercised by loading the data for a lazy paragraph: we make an object
					// selection of the previous paragraph, which automatically loads things
					// into the cache if needed, but does minimal work if it's already there.
					// Note that indexes[0] is already set to the index of the previous
					// paragraph.
					rgvsli[0].ihvo = indexes[0];
					IVwSelection selNew = m_rootb.MakeTextSelInObj(
						/*[in] int ihvoRoot */ 0,
						/*[in] int cvsli */ rgvsli.Length,
						/*[in, size_is(cvsli)] VwSelLevInfo * */ rgvsli,
						/*[in] int cvsliEnd */ 0,
						/*[in, size_is(cvsliEnd)] VwSelLevInfo * rgvsliEnd */ null,
						/*[in] ComBool fInitial */ false,
						/*[in] ComBool fEdit */ false,
						/*[in] ComBool fRange */ false,
						/*[in] ComBool fWholeObj */ true,
						/*[in] ComBool fInstall */ false
						);
				}
				ilev++;

				// work forward choosing the last item in any higher-level list.
				for (; ilev < indexes.Length; ilev++)
				{
					int chvo = 0;
					// iterate backward thru until we find a list that isn't empty, for example
					// we iterate thru the paragraphs in a text starting from the currently set index and
					// move backwards until we find a paragraph that is not empty.
					for (int offset = 0; offset <= indexes[ilev - 1]; offset++)
					{
						int hvo = sda.get_VecItem(hvos[ilev - 1], tags[ilev - 1], indexes[ilev - 1] - offset);
						hvos[ilev] = hvo;
						chvo = sda.get_VecSize(hvo, tags[ilev]);
						if (chvo > 0)
							// found non-empty list
							break;
					}
					if (chvo == 0)
						return; // something empty; give up
					indexes[ilev] = chvo - 1; // last one
				}
				// Our target is indicated by the last item in the wordforms of the last
				// segment.
				int hvoAnn = sda.get_VecItem(hvos[2], m_vc.ktagSegmentForms,
					indexes[2]);
				int hvoAnalysis = sda.get_ObjectProp(hvoAnn,
					(int)CmAnnotation.CmAnnotationTags.kflidInstanceOf);
				if (hvoAnalysis == 0 || !m_vc.CanBeAnalyzed(hvoAnn))
					continue; // punctuation.
				// If we're looking for a word that needs analysis, skip this one if it doesn't.
				if (fNeedsAnalysis &&
					FullyAnalyzed(m_fdoCache, m_vc.ListManager, hvoAnalysis, hvoAnalysis))
					continue;
				// Otherwise this is it.
				e.Annotation = hvoAnn;
				e.Analysis = hvoAnalysis;
				return;
			}
		}

		// The Sandbox is resizing. If it really changed and is visible, adjust the underlying
		// text.
		private void m_focusBox_Resize(object sender, EventArgs e)
		{
			if (!Controls.Contains(FocusBox))
				return;
			if (SetSandboxSizeForVc())
			{
				SimulateReplaceAnnotation(m_hvoAnnotation);
				MoveFocusBoxIntoPlace();
			}
		}

		/// <summary>
		/// We got a right click event. Bring up the appropriate menu if any.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void InterlinDocChild_RightMouseClickedEvent(SimpleRootSite sender, FwRightMouseClickEventArgs e)
		{
			e.EventHandled = true; // for the moment we always claim to have handled it.
			int hvoAnnotation;
			if (!CanDeleteFF(e.Selection, out hvoAnnotation))
				return;
			e.Selection.Install();
			ContextMenuStrip menu = new ContextMenuStrip();

			// Add spelling items if any (i.e., if we clicked a squiggle word).
			Rectangle rcSrcRoot, rcDstRoot;
			GetCoordRects(out rcSrcRoot, out rcDstRoot);
			EditingHelper.MakeSpellCheckMenuOptions(e.MouseLocation, m_rootb, rcSrcRoot, rcDstRoot, menu);
			if (menu.Items.Count > 0)
			{
				menu.Items.Add(new ToolStripSeparator());
			}

			// Add the delete item.
			// We need to choose the proper menu id for the selected annotation.
			string sMenuText = GetTextForDeleteFreeform(hvoAnnotation);
			ToolStripMenuItem item = new ToolStripMenuItem(sMenuText);
			item.Click += new EventHandler(OnDeleteFreeform);
			menu.Items.Add(item);

			menu.Show(this, e.MouseLocation);

			//// We need to choose the proper menu id for the selected annotation.
			//string sMenuId = "mnuIText-Note";
			//ISilDataAccess sda = Cache.MainCacheAccessor;
			//int hvo = sda.get_ObjectProp(hvoAnnotation,
			//    (int)CmAnnotation.CmAnnotationTags.kflidAnnotationType);
			//// Only one Free Translation annotation and one Literal Translation annotation
			//// is allowed for each segment!
			//if (hvo == m_vc.FtSegmentDefn)
			//    sMenuId = "mnuIText-FreeTrans";
			//if (hvo == m_vc.LtSegmentDefn)
			//    sMenuId = "mnuIText-LitTrans";
			//XCore.XWindow window = (XCore.XWindow)m_mediator.PropertyTable.GetValue("window");
			//Point pt = new Point(e.MouseLocation.X, e.MouseLocation.Y);
			//ClientToScreen(m_rootb, ref pt);
			//window.ShowContextMenu(sMenuId,
			//    pt,
			//    null, // No temporary XCore colleague.
			//    null);
			//    // Using the sequencer here now causes problems.
			//    // If a safe blocking mechanism can be found for the context menu, we can restore the original behavior
			//    // which will have this code do the setup and teardown work.
			//    //(this as IReceiveSequentialMessages).Sequencer);
		}

		private string GetTextForDeleteFreeform(int hvoAnnotation)
		{
			string sMenuText = ITextStrings.ksDeleteNote;
			ISilDataAccess sda = Cache.MainCacheAccessor;
			int hvo = sda.get_ObjectProp(hvoAnnotation,
				(int)CmAnnotation.CmAnnotationTags.kflidAnnotationType);
			// Only one Free Translation annotation and one Literal Translation annotation
			// is allowed for each segment!
			if (hvo == m_vc.FtSegmentDefn)
				sMenuText = ITextStrings.ksDeleteFreeTrans;
			if (hvo == m_vc.LtSegmentDefn)
				sMenuText = ITextStrings.ksDeleteLitTrans;
			return sMenuText;
		}

		void OnDeleteFreeform(object sender, EventArgs e)
		{
			ToolStripMenuItem item = sender as ToolStripMenuItem;
			using (new UndoRedoTaskHelper(Cache, string.Format(ITextStrings.ksUndoCommand, item.Text),
			string.Format(ITextStrings.ksRedoCommand, item.Text)))
			{
				DeleteFreeform(RootBox.Selection);
			}
		}

		//public bool OnDeleteFreeform(object commandObject)
		//{
		//    using (UndoRedoCommandHelper undoRedoTask = new UndoRedoCommandHelper(Cache, commandObject as Command))
		//    {
		//        return DeleteFreeform(RootBox.Selection);
		//    }
		//}

		/// <summary>
		/// This saves the location of the sandbox temporarily while setting the
		/// scroll position.
		/// </summary>
		private Point m_sbLoc;
		private void SaveSandboxLocation()
		{
			if (ExistingFocusBox != null)
			{
				m_sbLoc = FocusBox.Location;
			}
		}
		private void RestoreSandboxLocation()
		{
			if (!InValidState)
			{
				HideAndDestroyFocusBox(false);
				return;
			}
			if (ExistingFocusBox != null && FocusBox.InterlinWordControl != null && FocusBox.Visible)
			{
				IVwSelection sel = MakeSandboxSel();
				if (sel != null)
				{
					Point ptLoc = GetSandboxSelLocation(sel);
					if (FocusBox.Location != ptLoc)
						FocusBox.Location = ptLoc;
				}
				else if (FocusBox.Location != m_sbLoc)
				{
					FocusBox.Location = m_sbLoc;
				}
			}
		}
		/// <summary>
		/// Setting the scroll position can cause the Focus Box (sandbox) to move
		/// to a seemingly random spot.  Saving the sandbox location and then
		/// restoring it seems to work.  See LT-3836 for more bug details, including
		/// pretty pictures.
		/// </summary>
		public override Point ScrollPosition
		{
			set
			{
				SaveSandboxLocation();
				base.ScrollPosition = value;
				RestoreSandboxLocation();
			}
		}
		#region IVwNotifyChange Members

		internal protected RecordClerk ActiveClerk
		{
			get
			{
				return Mediator.PropertyTable.GetValue("ActiveClerk") as RecordClerk;
			}
		}

		/// <summary>
		/// When any of these properties changes in the cache, we discard any saved collection of annotations.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ivMin"></param>
		/// <param name="cvIns"></param>
		/// <param name="cvDel"></param>
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (tag == (int)CmAnnotation.CmAnnotationTags.kflidInstanceOf
				|| tag == (int)CmAgentEvaluation.CmAgentEvaluationTags.kflidTarget
				|| tag == (int)CmAgentEvaluation.CmAgentEvaluationTags.kflidAccepted
				|| tag == (int)CmAgent.CmAgentTags.kflidEvaluations)
			{
				m_vc.ResetAnalysisCache();
			}
			else if (tag == (int)WfiWordform.WfiWordformTags.kflidForm
				|| tag == (int)WordformInventory.WordformInventoryTags.kflidWordforms)
			{
				WordformInventory.OnChangedWordformsOC();
			}
			else if (ivMin == 0 && cvDel > 0 &&
				Cache != null && Cache.LangProject != null && hvo == Cache.LangProject.Hvo &&
				Mediator != null && Mediator.PropertyTable != null)
			{
				RecordClerk clerk = this.ActiveClerk;
				if (clerk != null && clerk.ListSize == 0 && tag == clerk.VirtualFlid)
				{
					// We've deleted the one and only text, so the old annotation is now invalid.
					// Setting the hvo to zero keeps the code from blowing up elsewhere.
					m_hvoAnnotation = 0;
				}
			}
		}

		#endregion


		/// <summary>
		/// This class allows smarter UndoRedo for ApproveAnalysis, so that the FocusBox can move appropriately.
		/// </summary>
		internal class UndoRedoApproveAnalysis : UndoActionBase
		{
			FdoCache m_cache = null;
			InterlinDocChild m_interlinDoc = null;
			StTxtPara.TwficInfo m_oldTwficInfo = null;
			StTxtPara.TwficInfo m_newTwficInfo = null;

			internal UndoRedoApproveAnalysis(InterlinDocChild interlinDoc, int hvoOldAnnotation,
				int hvoNewAnnotation)
			{
				m_interlinDoc = interlinDoc;
				m_cache = interlinDoc.Cache;
				if (hvoOldAnnotation != 0)
				{
					m_oldTwficInfo = new StTxtPara.TwficInfo(m_cache, hvoOldAnnotation);
					m_oldTwficInfo.CaptureObjectInfo();
				}
				if (hvoNewAnnotation != 0)
					this.NewAnnotation = hvoNewAnnotation;
			}

			#region Overrides of UndoActionBase

			private bool IsUndoable()
			{
				return TryToRestoreValidTwficInfo(m_oldTwficInfo);
			}

			private bool TryToRestoreValidTwficInfo(StTxtPara.TwficInfo twficInfo)
			{
				if (twficInfo != null)
				{
					int hvoIdenticalTwfic = twficInfo.FindIdenticalTwfic();
					if (hvoIdenticalTwfic != 0)
					{
						if (hvoIdenticalTwfic != twficInfo.Object.Hvo)
						{
							// regenerate the twficInfo according to the identical twfic;
							twficInfo.ReloadInfo(hvoIdenticalTwfic);
						}
						return true;
					}
				}
				return false;
			}

			internal int NewAnnotation
			{
				set
				{
					m_newTwficInfo = new StTxtPara.TwficInfo(m_cache, value);
					m_newTwficInfo.CaptureObjectInfo();
				}
			}

			public override bool Redo(bool fRefreshPending)
			{
				if (m_newTwficInfo != null)
				{
					if (TryToRestoreValidTwficInfo(m_newTwficInfo))
					{
						m_interlinDoc.HvoAnnotation = m_newTwficInfo.Object.Hvo;
						m_interlinDoc.HvoAnalysis = m_newTwficInfo.Object.InstanceOfRAHvo;
					}
					else
					{
						m_interlinDoc.HvoAnnotation = 0;
						m_interlinDoc.HvoAnalysis = 0;
					}
				}
				else
				{
					// nothing to redo in this action.
				}
				return true;
			}

			public override bool Undo(bool fRefreshPending)
			{
				if (m_oldTwficInfo != null)
				{
					if (IsUndoable())
					{
						m_interlinDoc.HvoAnnotation = m_oldTwficInfo.Object.Hvo;
						m_interlinDoc.HvoAnalysis = m_oldTwficInfo.Object.InstanceOfRAHvo;
					}
					else
					{
						m_interlinDoc.HvoAnnotation = 0;
						m_interlinDoc.HvoAnalysis = 0;
					}
				}
				else
				{
					// nothing to undo in this action.
				}
				return true;
			}

			#endregion
		}

		///// <summary>
		///// The ITsString has each piece of information separated by a newline.  We want
		///// a tabbed format to make it easier for user to deal with.  See LT-9475.
		///// </summary>
		//public override ITsString GetTsStringForClipboard(IVwSelection vwsel)
		//{
		//    InterlinClipboardHelper helper = new InterlinClipboardHelper(vwsel, this, m_fdoCache);
		//    return helper.Run();
		//}

		/// <summary>
		/// This helper class is used to obtain a tab-delimited clipboard ITsString for the given
		/// selection.
		/// </summary>
		internal class InterlinClipboardHelper
		{
			/// <summary>These chars are used in multiple places for dealing with free annotation lines.</summary>
			char[] m_rgchDirMkrs = new char[] { '\x200E', '\x200F' };
			InterlinLineChoices m_choices;
			ITsString m_tss;
			List<ITsString> m_rgtssPieces;
			ITsStrBldr[] m_rgtsbLines;
			FdoCache m_cache;
			ITsStrFactory m_tsf = TsStrFactoryClass.Create();
			StringBuilder m_bldrLineTabs = new StringBuilder();

			bool m_fHaveMorphemeLevel;
			bool m_fHaveTagging;
			int m_idxChoiceFirstMorpheme;
			int m_idxChoiceLastMorpheme;
			int m_cMorphLines = 0;
			int m_idxChoiceFirstFreeform;
			/// <summary>
			/// Number of different lines that may be displayed, including both interlinear and
			/// freeform lines.
			/// </summary>
			int m_cChoices;
			string[] m_rgsLabel;
			/// <summary>
			/// This is the index into m_choices that the tagging line would have, if only it
			/// were listed in m_choices.
			/// </summary>
			int m_idxChoiceTaggingLine;
			/// <summary>
			/// This is the number of interlinear lines (word or morpheme), which is the same as
			/// the number of labels shown (apart from those embedded in the freeform lines).
			/// </summary>
			int m_cInterlinearLines;

			internal InterlinClipboardHelper(IVwSelection vwsel, InterlinDocChild site, FdoCache cache)
			{
				// Get the string and split it into its individual lines.
				vwsel.GetSelectionString(out m_tss, "\t");
				m_rgtssPieces = StringUtils.Split(m_tss, new string[] { Environment.NewLine }, StringSplitOptions.None);
				m_choices = site.LineChoices;
				m_fHaveMorphemeLevel = m_choices.HaveMorphemeLevel;
				if (m_fHaveMorphemeLevel)
					m_cMorphLines = m_idxChoiceLastMorpheme - m_idxChoiceFirstMorpheme + 1;
				m_idxChoiceFirstMorpheme = m_choices.FirstMorphemeIndex;
				m_idxChoiceLastMorpheme = m_choices.LastMorphemeIndex;
				m_idxChoiceFirstFreeform = m_choices.FirstFreeformIndex;
				m_cChoices = m_choices.Count;
				m_cInterlinearLines = m_choices.FirstFreeformIndex;
				// Get the labels for the lines in m_choices.
				m_rgsLabel = new string[m_cInterlinearLines];
				for (int i = 0; i < m_rgsLabel.Length; ++i)
					m_rgsLabel[i] = m_choices.LabelFor(m_choices[i].Flid);
				bool fTaggingView = site is InterlinTaggingChild;
				if (site is InterlinTaggingChild)
				{
					m_fHaveTagging = true;
					m_idxChoiceTaggingLine = m_idxChoiceFirstFreeform;
					++m_idxChoiceFirstFreeform;
					++m_cChoices;
				}
				else
				{
					m_fHaveTagging = false;
					m_idxChoiceTaggingLine = -1;
				}
				m_cache = cache;
			}

			internal ITsString Run()
			{
				//// We can't (yet) handle processing if the first line is morpheme level.
				//if (m_idxChoiceFirstMorpheme == 0)
				//    return null;
				m_rgtsbLines = new ITsStrBldr[m_cChoices];
				for (int i = 0; i < m_cChoices; ++i)
					m_rgtsbLines[i] = TsStrBldrClass.Create();
				ITsIncStrBldr tisbClipBoard;
				if (m_fHaveMorphemeLevel)
					tisbClipBoard = GetMorphemeLevelInterlinearClipboardData();
				else
					tisbClipBoard = GetWordLevelInterlinearClipboardData();
				if (tisbClipBoard == null)
					return m_tss;
				else
					return tisbClipBoard.GetString();
			}

			/// <summary>
			/// Process the data for an interlinear text with morpheme-level annotations.
			/// </summary>
			private ITsIncStrBldr GetMorphemeLevelInterlinearClipboardData()
			{
				ITsIncStrBldr tisbClipBoard = TsIncStrBldrClass.Create();
				// Find the first available line that marks the beginning of an entire word
				// bundle.
				int idxFirstLine = FindFirstDataLineWithMorphLevelData();
				if (idxFirstLine < 0)
					return null;	// we'll get garbage out, but...
				int idxChoice = 0;				// index in m_choices for the current piece.
				int cMorphs = 0;				// number of morphemes in the current word.
				string sWordTabs = null;
				for (int i = idxFirstLine; i < m_rgtssPieces.Count; ++i)
				{
					ITsString tssPiece = m_rgtssPieces[i];
					string sPiece = TextOf(tssPiece);
					// Freeform lines have direction markers embedded in them.
					if (idxChoice >= m_idxChoiceFirstFreeform && sPiece.IndexOfAny(m_rgchDirMkrs) < 0)
						idxChoice = 0;
					if (idxChoice == 0 && idxChoice < m_idxChoiceFirstMorpheme)
					{
						if (i + m_idxChoiceFirstMorpheme < m_rgtssPieces.Count)
						{
							string s = TextOf(m_rgtssPieces[i + m_idxChoiceFirstMorpheme]);
							if (s.StartsWith("\t"))
								sWordTabs = s;
							else
								sWordTabs = "\t";
						}
						m_bldrLineTabs.Append(sWordTabs);
					}
					if (sPiece.StartsWith("\t"))
					{
						if (idxChoice == m_idxChoiceFirstMorpheme)
						{
							cMorphs = sPiece.Length;
							if (idxChoice == 0)
							{
								sWordTabs = sPiece;
								m_bldrLineTabs.Append(sWordTabs);
							}
						}
						continue;
					}
					if (idxChoice < m_idxChoiceFirstFreeform && sPiece.IndexOfAny(m_rgchDirMkrs) >= 0)
					{
						FillInEmptyLinesForClipboard(idxChoice);
						if (m_fHaveTagging)
							idxChoice = m_idxChoiceTaggingLine;
						else
							idxChoice = m_idxChoiceFirstFreeform;
					}
					if (idxChoice < m_idxChoiceFirstFreeform)
					{
						if (idxChoice < m_idxChoiceFirstMorpheme || idxChoice > m_idxChoiceLastMorpheme)
						{
							StoreWordAnnotationForClipboard(m_rgtsbLines[idxChoice], tssPiece, sWordTabs);
						}
						else
						{
							StoreMorphAnnotationsForClipboard(m_rgtsbLines[idxChoice], cMorphs, i);
							if (cMorphs > 0 && idxChoice == m_idxChoiceLastMorpheme)
								i += (cMorphs - 1) * m_cMorphLines;
						}
					}
					else if (idxChoice < m_cChoices)
					{
						StoreFreeAnnotationForClipboard(m_rgtsbLines[idxChoice], tssPiece);
					}
					++idxChoice;
					int idxChoiceOrig = idxChoice;
					bool fSegEnd = HandlePunctuationAndCheckSegmentEnd(ref idxChoice, i, sPiece);
					if (idxChoice >= m_cChoices)
					{
						idxChoice = 0;
						if (m_idxChoiceFirstFreeform < m_cChoices || fSegEnd)
						{
							StoreClipboardSegment(tisbClipBoard);
							m_bldrLineTabs.Remove(0, m_bldrLineTabs.Length);
							i += m_cInterlinearLines;	// skip the labels for the m_choices (tagging line doesn't have a label).
							i += 2;		// skip the line full of tabs for the next segment's objects and the segment number.
						}
					}
				}
				StoreRemainingClipboardData(tisbClipBoard);
				return tisbClipBoard;
			}

			/// <summary>
			/// Find the first line of data by looking for the tabs that mark the beginning of
			/// the morpheme annotation section.
			/// </summary>
			private int FindFirstDataLineWithMorphLevelData()
			{
				int idxFirstLine = -1;
				for (int i = 0; i < m_rgtssPieces.Count; ++i)
				{
					if (TextOf(m_rgtssPieces[i]).StartsWith("\t"))
					{
						idxFirstLine = i - m_idxChoiceFirstMorpheme;
						if (idxFirstLine >= 0)
							break;
					}
				}
				int idx2 = FindFirstDataLineWithWordLevelData();
				if (idx2 >= 0 && (idx2 < idxFirstLine || idxFirstLine < 0))
					return idx2;
				else
					return idxFirstLine;
			}

			private bool HandlePunctuationAndCheckSegmentEnd(ref int idxChoice, int i, string sPiece)
			{
				// TODO: assumes word line is index 0, which may not be true for print view (at least)
				bool fSegEnd = false;
				if (idxChoice == 1 && sPiece.Length > 0 &&
					sPiece != ITextStrings.ksStars &&
					Icu.IsPunct(WordMaker.FullCharAt(sPiece, 0)))
				{
					FillInEmptyLinesForClipboard(idxChoice);
					if (m_fHaveTagging)
						idxChoice = m_idxChoiceTaggingLine;
					else
						idxChoice = 0;
					string sNextPiece = GetNextPiece(i);
					if (sNextPiece == null)
					{
						fSegEnd = true;
					}
					else if (sNextPiece.StartsWith("\t"))
					{
						if (m_idxChoiceFirstMorpheme == 0 && !BundleLabelsAreComingUp(i + 3))
							idxChoice = 0;
						else
							fSegEnd = true;
					}
					else if (sNextPiece.IndexOfAny(m_rgchDirMkrs) >= 0)
					{
						idxChoice = m_idxChoiceFirstFreeform;
					}
				}
				else if (idxChoice >= m_idxChoiceFirstFreeform && idxChoice < m_cChoices)
				{
					string sNextPiece = GetNextPiece(i);
					if (sNextPiece == null)
					{
						fSegEnd = true;
					}
					else if (sNextPiece.StartsWith("\t"))
					{
						if (m_idxChoiceFirstMorpheme == 0 && !BundleLabelsAreComingUp(i + 3))
							idxChoice = 0;
						else
							fSegEnd = true;
					}
				}
				if (fSegEnd)
					idxChoice = m_cChoices;
				return fSegEnd;
			}

			private string GetNextPiece(int i)
			{
				if (i + 1 < m_rgtssPieces.Count)
					return TextOf(m_rgtssPieces[i + 1]);
				else
					return null;
			}

			private bool BundleLabelsAreComingUp(int i)
			{
				for (int j = 0; j < m_rgsLabel.Length && i < m_rgtssPieces.Count; ++j)
				{
					string sPiece = TextOf(m_rgtssPieces[i]);
					if (sPiece != m_rgsLabel[j])
						return false;
					++i;
				}
				return true;
			}

			private void StoreRemainingClipboardData(ITsIncStrBldr tisbClipBoard)
			{
				// Check whether we actually have any more data remaining, so that
				// we don't add a gratuitous extra newline at the end.
				bool fMoreData = false;
				for (int i = 0; i < m_rgtsbLines.Length; ++i)
				{
					if (!String.IsNullOrEmpty(m_rgtsbLines[i].Text))
					{
						fMoreData = true;
						break;
					}
				}
				if (fMoreData)
					StoreClipboardSegment(tisbClipBoard);
			}

			/// <summary>
			/// Store a word-level piece in the given builder.
			/// </summary>
			private void StoreWordAnnotationForClipboard(ITsStrBldr tsbLine, ITsString tssPiece, string sWordTabs)
			{
				tsbLine.ReplaceTsString(tsbLine.Length, tsbLine.Length, tssPiece);
				tsbLine.Replace(tsbLine.Length, tsbLine.Length, sWordTabs, null);
			}

			/// <summary>
			/// Store a set of morph-level pieces in the given builder.
			/// </summary>
			private void StoreMorphAnnotationsForClipboard(ITsStrBldr tsbLine, int cMorphs, int idxPiece)
			{
				for (int iMorph = 0; iMorph < cMorphs; ++iMorph)
				{
					int i = idxPiece + iMorph * m_cMorphLines;
					if (i < m_rgtssPieces.Count)
					{
						tsbLine.ReplaceTsString(tsbLine.Length, tsbLine.Length, m_rgtssPieces[i]);
						tsbLine.Replace(tsbLine.Length, tsbLine.Length, "\t", null);
					}
				}
			}

			/// <summary>
			/// Store a free-form annotation piece in the given builder, first stripping out
			/// the label and direction markers if possible.
			/// </summary>
			private void StoreFreeAnnotationForClipboard(ITsStrBldr tsbLine, ITsString tssPiece)
			{
				ITsStrBldr tsb = tssPiece.GetBldr();
				int ich;
				if (tsb.Text != null)
				{
					ich = tsb.Text.IndexOfAny(m_rgchDirMkrs);
					while (ich >= 0)
					{
						tsb.Replace(ich, ich + 1, null, null);
						if (tsb.Text == null)
							break;
						ich = tsb.Text.IndexOfAny(m_rgchDirMkrs);
					}
				}
				string sPiece = tsb.Text;
				if (sPiece != null)
				{
					string sLabel = ITextStrings.ksFree_.Trim();
					ich = sPiece.IndexOf(sLabel);
					if (ich < 0)
					{
						sLabel = ITextStrings.ksNote_.Trim();
						ich = sPiece.IndexOf(sLabel);
					}
					if (ich < 0)
					{
						sLabel = ITextStrings.ksLit_.Trim();
						ich = sPiece.IndexOf(sLabel);
					}
					if (ich >= 0)
					{
						int cch = sLabel.Length;
						if (ich == 0 || ich + cch == sPiece.Length)
							tsb.Replace(ich, ich + cch, null, null);
					}
				}
				if (tsb.Text != null && tsb.Text.IndexOf(' ') == 0)
					tsb.Replace(0, 1, null, null);
				if (tsb.Text != null && tsb.Text.LastIndexOf(' ') == tsb.Length - 1)
					tsb.Replace(tsb.Length - 1, tsb.Length, null, null);

				tsbLine.ReplaceTsString(tsbLine.Length, tsbLine.Length, tsb.GetString());
				tsbLine.Replace(tsbLine.Length, tsbLine.Length, m_bldrLineTabs.ToString(), null);
			}

			/// <summary>
			/// Store this segment's worth of data in the overall builder.
			/// </summary>
			private void StoreClipboardSegment(ITsIncStrBldr tisbClipBoard)
			{
				for (int i = 0; i < m_rgtsbLines.Length; ++i)
				{
					// Trim the final trailing tab on the line since it's not needed.
					int cch = m_rgtsbLines[i].Length;
					if (cch > 0 && m_rgtsbLines[i].Text.LastIndexOf('\t') == cch - 1)
						m_rgtsbLines[i].ReplaceTsString(cch - 1, cch, null);
					tisbClipBoard.AppendTsString(m_rgtsbLines[i].GetString());
					m_rgtsbLines[i].Clear();
					tisbClipBoard.Append(Environment.NewLine);
				}
				tisbClipBoard.Append(Environment.NewLine);
			}

			/// <summary>
			/// Extract the text string from the ITsString, but use String.Empty instead of null.
			/// </summary>
			private string TextOf(ITsString tss)
			{
				string s = tss.Text;
				if (s == null)
					return String.Empty;
				else
					return s;
			}

			/// <summary>
			/// Fill in a single tab character for all the lines from idxChoice to the first
			/// freeform annotation (if any).
			/// </summary>
			private void FillInEmptyLinesForClipboard(int idxChoice)
			{
				for (int j = idxChoice; j < m_cInterlinearLines; ++j)
				{
					int ws = m_choices[j].WritingSystem;
					if (m_choices[j].IsMagicWritingSystem)
						ws = (m_cache.LangProject as LangProject).DefaultWsForMagicWs(ws);
					int cch = m_rgtsbLines[j].Length;
					m_rgtsbLines[j].ReplaceTsString(cch, cch, m_tsf.MakeString("\t", ws));
				}
			}

			/// <summary>
			/// Process the data for an interlinear text with only word-level annotations.
			/// </summary>
			private ITsIncStrBldr GetWordLevelInterlinearClipboardData()
			{
				ITsIncStrBldr tisbClipBoard = TsIncStrBldrClass.Create();
				int idxFirstLine = FindFirstDataLineWithWordLevelData();
				if (idxFirstLine < 0)
					return null;	// we'll get garbage out, but...
				int idxChoice = 0;				// index in m_choices for the current piece.
				for (int i = idxFirstLine; i < m_rgtssPieces.Count; ++i)
				{
					ITsString tssPiece = m_rgtssPieces[i];
					string sPiece = TextOf(tssPiece);
					if (sPiece.StartsWith("\t"))
						continue;
					// If we have free annotations, reset the choice index if the next choice is a
					// free annotation, but the data looks like a word instead.
					if (idxChoice >= m_idxChoiceFirstFreeform && sPiece.IndexOfAny(m_rgchDirMkrs) < 0)
						idxChoice = 0;
					if (idxChoice == 0)
						m_bldrLineTabs.Append("\t");
					if (idxChoice < m_idxChoiceFirstFreeform && sPiece.IndexOfAny(m_rgchDirMkrs) >= 0)
					{
						FillInEmptyLinesForClipboard(idxChoice);
						//if (m_idxChoiceTaggingLine >= 0)
						//    idxChoice = m_idxChoiceTaggingLine;
						//else
							idxChoice = m_idxChoiceFirstFreeform;
					}
					if (idxChoice < m_idxChoiceFirstFreeform)
						StoreWordAnnotationForClipboard(m_rgtsbLines[idxChoice], tssPiece, "\t");
					else if (idxChoice < m_cChoices)
						StoreFreeAnnotationForClipboard(m_rgtsbLines[idxChoice], tssPiece);
					++idxChoice;
					bool fSegEnd = HandlePunctuationAndCheckSegmentEnd(ref idxChoice, i, sPiece);
					if (idxChoice >= m_cChoices)
					{
						idxChoice = 0;
						if (m_idxChoiceFirstFreeform < m_cChoices || fSegEnd)
						{
							StoreClipboardSegment(tisbClipBoard);
							m_bldrLineTabs.Remove(0, m_bldrLineTabs.Length);
							i += m_idxChoiceFirstFreeform;	// skip the labels for the m_choices.
							i += 2;		// skip the line full of tabs for the next segment's objects and the segment number.
						}
					}
				}
				StoreRemainingClipboardData(tisbClipBoard);
				return tisbClipBoard;
			}

			/// <summary>
			/// Find the first line of data by looking for a clump of labels, and either backtracking
			/// or skipping forward.
			/// </summary>
			private int FindFirstDataLineWithWordLevelData()
			{
				int idxFirstLine = -1;
				for (int i = 0; i < m_rgtssPieces.Count; ++i)
				{
					string sPiece = TextOf(m_rgtssPieces[i]);
					if (sPiece == m_rgsLabel[0])
					{
						bool fLabels = true;
						for (int j = 1; j < m_rgsLabel.Length; ++j)
						{
							if (m_rgtssPieces[i + j].Text != m_rgsLabel[j])
							{
								fLabels = false;
								break;
							}
						}
						if (fLabels)
						{
							int idxGuess = i - 2;	// one for number, one for tabs representing words.
							idxGuess -= (m_cChoices - m_idxChoiceFirstFreeform);
							idxFirstLine = idxGuess;
							while (idxFirstLine - m_idxChoiceFirstFreeform >= 0)
								idxFirstLine -= m_idxChoiceFirstFreeform;
							if (idxFirstLine == idxGuess)
							{
								// Don't want to consider effect of a tagging line here, since
								// it doesn't show a label.
								idxFirstLine = i + m_cInterlinearLines;
							}
							return idxFirstLine;
						}
					}
				}
				return idxFirstLine;
			}
		}
	}
}

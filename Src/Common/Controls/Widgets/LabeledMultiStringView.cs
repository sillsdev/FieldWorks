using System;
using System.Xml;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;		// controls and etc...
using System.Windows.Forms.VisualStyles;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using XCore;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Resources;

namespace SIL.FieldWorks.Common.Widgets
{
	/// <summary>
	/// LabeledMultiStringView displays one or more writing system alternatives of a string property.
	/// It simply edits that property.
	/// </summary>
	public class LabeledMultiStringView : RootSiteControl
	{
		bool m_forceIncludeEnglish;
		bool m_editable;
		int m_hvoObj;
		int m_flid;
		int m_wsMagic;
		ILgWritingSystem[] m_rgws;
		ILgWritingSystem[] m_rgwsToDisplay;
		LabeledMultiStringVc m_vc = null;

		/// <summary>
		/// This event is triggered at the start of the Display() method of the VC.
		/// It provides an opportunity to set overall properties (such as read-only) in the view.
		/// </summary>
		public event VwEnvEventHandler Display;

		/// <summary>
		/// Make one.
		/// </summary>
		/// <param name="hvo">The object to be edited</param>
		/// <param name="flid">The multistring property to be edited</param>
		/// <param name="wsMagic">The magic writing system (like LangProject.kwsAnals)
		/// indicating which writing systems to display.</param>
		/// <param name="forceIncludeEnglish">True, if English is to be included along with others.</param>
		/// <param name="editable">false if we don't want to allow editing of the strings.</param>
		public LabeledMultiStringView(int hvo, int flid, int wsMagic, bool forceIncludeEnglish, bool editable)
			: this(hvo, flid, wsMagic, forceIncludeEnglish, editable, true)
		{
		}
		/// <summary>
		/// Make one.
		/// </summary>
		/// <param name="hvo">The object to be edited</param>
		/// <param name="flid">The multistring property to be edited</param>
		/// <param name="wsMagic">The magic writing system (like LangProject.kwsAnals)
		/// indicating which writing systems to display.</param>
		/// <param name="forceIncludeEnglish">True, if English is to be included along with others.</param>
		/// <param name="editable">false if we don't want to allow editing of the strings.</param>
		/// <param name="spellCheck">true if you want the view spell-checked.</param>
		public LabeledMultiStringView(int hvo, int flid, int wsMagic, bool forceIncludeEnglish, bool editable, bool spellCheck)
		{
			m_hvoObj = hvo;
			m_flid = flid;
			m_wsMagic = wsMagic;
			m_forceIncludeEnglish = forceIncludeEnglish;
			m_editable = editable;
			if (editable && RootSiteEditingHelper != null)
				RootSiteEditingHelper.PasteFixTssEvent += new FwPasteFixTssEventHandler(OnPasteFixTssEvent);
			DoSpellCheck = spellCheck;
		}

		/// <summary>
		/// If the text for pasting is too long, truncate it and warn the user.
		/// </summary>
		void OnPasteFixTssEvent(EditingHelper sender, FwPasteFixTssEventArgs e)
		{
			TruncatePasteIfNecessary(e, m_flid);
		}

		/// <summary>
		/// If the view's root object is valid, then call the base method.  Otherwise do nothing.
		/// (See LT-8656 and LT-9119.)
		/// </summary>
		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			if (m_fdoCache.VerifyValidObject(m_hvoObj))
				base.OnKeyPress(e);
			else
				e.Handled = true;
		}

		static bool s_fProcessingSelectionChanged = false;
		/// <summary>
		/// Try to keep the selection from including any of the characters in a writing system label.
		/// See LT-8396.
		/// </summary>
		public override void SelectionChanged(IVwRootBox prootb, IVwSelection vwselNew)
		{
			base.SelectionChanged(prootb, vwselNew);
			// 1) We don't want to recurse into here.
			// 2) If the selection is invalid we can't use it.
			// 3) If the selection is entirely editable ("CanFormatChar"), we don't need to do
			//    anything.
			if (s_fProcessingSelectionChanged || !vwselNew.IsValid || vwselNew.CanFormatChar)
				return;
			try
			{
				s_fProcessingSelectionChanged = true;

				SelectionHelper hlpr = SelectionHelper.Create(vwselNew, this);
				bool fRange = hlpr.IsRange;
				bool fChangeRange = false;
				if (fRange)
				{
					bool fAnchorEditable = vwselNew.IsEditable;
					int ichAnchor = hlpr.GetIch(SelectionHelper.SelLimitType.Anchor);
					int tagAnchor = hlpr.GetTextPropId(SelectionHelper.SelLimitType.Anchor);
					int ichEnd = hlpr.GetIch(SelectionHelper.SelLimitType.End);
					int tagEnd = hlpr.GetTextPropId(SelectionHelper.SelLimitType.End);
					bool fEndBeforeAnchor = vwselNew.EndBeforeAnchor;
					if (fEndBeforeAnchor)
					{
						if (fAnchorEditable && tagAnchor > 0 && tagEnd < 0)
						{
							hlpr.SetTextPropId(SelectionHelper.SelLimitType.End, tagAnchor);
							hlpr.SetIch(SelectionHelper.SelLimitType.End, 0);
							fChangeRange = true;
						}
					}
					else
					{
						if (!fAnchorEditable && tagAnchor < 0 && tagEnd > 0)
						{
							hlpr.SetTextPropId(SelectionHelper.SelLimitType.Anchor, tagEnd);
							hlpr.SetIch(SelectionHelper.SelLimitType.Anchor, 0);
							fChangeRange = true;
						}
					}
				}
				if (fChangeRange)
					hlpr.SetSelection(true);
			}
			finally
			{
				s_fProcessingSelectionChanged = false;
			}
		}
		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			base.Dispose(disposing);

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_vc != null)
					m_vc.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_rgws = null;
			m_vc = null;
		}

		#endregion IDisposable override

		/// <summary>
		/// Return an array of writing systems given an array of their HVOs.
		/// </summary>
		/// <returns></returns>
		public static ILgWritingSystem[] WssFromHvos(int[] hvos, FdoCache cache)
		{
			ILgWritingSystem [] result = new ILgWritingSystem[hvos.Length];
			for (int i = 0; i < hvos.Length; i++)
			{
				result[i] = LgWritingSystem.CreateFromDBObject(cache, hvos[i]);
			}
			return result;
		}


		/// <summary>
		/// gets a list of ws hvos, starting with the current wss, followed by remaining (non-current) active ones
		/// </summary>
		/// <param name="currentWss"></param>
		/// <param name="activeWss"></param>
		/// <param name="fAddOnlyCurrent">if true, only add the current wss, ignoring remaining active wss.</param>
		/// <returns></returns>
		internal static List<int> GetCurrentThenRemainingActiveWss(FdoReferenceSequence<ILgWritingSystem> currentWss,
			FdoReferenceCollection<ILgWritingSystem> activeWss, bool fAddOnlyCurrent)
		{
			List<int> hvoWss = new List<int>();
			// Add ordered (checked) writing system names to the list.
			foreach (ILgWritingSystem ws in currentWss)
				hvoWss.Add(ws.Hvo);
			if (fAddOnlyCurrent)
				return hvoWss;	// finished adding current wss, so return;
			// Now add the unchecked (or not current) writing systems to the list.
			foreach (ILgWritingSystem ws in activeWss)
			{
				if (!hvoWss.Contains(ws.Hvo))
					hvoWss.Add(ws.Hvo);
			}
			return hvoWss;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static ILgWritingSystem[] AnalysisWss(FdoCache cache)
		{
			return AnalysisWss(cache, false);
		}
		private static ILgWritingSystem[] AnalysisWss(FdoCache cache, bool fIncludeUncheckedActiveWss)
		{
			List<int> hvoWss = GetCurrentThenRemainingActiveWss(cache.LangProject.CurAnalysisWssRS,
					cache.LangProject.AnalysisWssRC, !fIncludeUncheckedActiveWss);
			return WssFromHvos(hvoWss.ToArray(), cache);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static ILgWritingSystem[] VernWss(FdoCache cache)
		{
			return VernWss(cache, false);
		}

		private static ILgWritingSystem[] VernWss(FdoCache cache, bool fIncludeUncheckedActiveWss)
		{
			List<int> hvoWss = GetCurrentThenRemainingActiveWss(cache.LangProject.CurVernWssRS,
				cache.LangProject.VernWssRC, !fIncludeUncheckedActiveWss);
			return WssFromHvos(hvoWss.ToArray(), cache);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets PronunciationWritingSystems.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static ILgWritingSystem[] PronunciationWritingSystems(FdoCache cache)
		{
			// Ensure list is not empty.
			Set<NamedWritingSystem> wssPronunciation = cache.LangProject.GetPronunciationWritingSystems();
			List<int> hvosWsPronunciation = new List<int>();
			// add the "default" pronunciation at the top of our ws choice list.
			if (cache.LangProject.CurPronunWssRS.Count > 0)
				hvosWsPronunciation.Add(cache.LangProject.CurPronunWssRS[0].Hvo);
			foreach (NamedWritingSystem nws in wssPronunciation)
			{
				// add all other pronunciation wss that are not the "default"
				if (hvosWsPronunciation.Count > 0 && hvosWsPronunciation[0] != nws.Hvo)
					hvosWsPronunciation.Add(nws.Hvo);
			}
			return WssFromHvos(hvosWsPronunciation.ToArray(), cache);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reversals the index writing systems.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="hvoReversalIndexEntry">The hvo reversal index entry.</param>
		/// <param name="forceIncludeEnglish">if set to <c>true</c> [force include english].</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static ILgWritingSystem[] ReversalIndexWritingSystems(FdoCache cache,
			int hvoReversalIndexEntry, bool forceIncludeEnglish)
		{
			return WssFromHvos(
				cache.LangProject.GetReversalIndexWritingSystems(hvoReversalIndexEntry,
				forceIncludeEnglish),
				cache);
		}

		/// <summary>
		/// Make an array which contains all the members of first() plus those of second() that
		/// are not included in first.
		/// </summary>
		/// <param name="first"></param>
		/// <param name="second"></param>
		/// <returns></returns>
		public static int[] MergeTwoArrays(int[] first, int[] second)
		{
			Set<int> set = new Set<int>(first);
			set.AddRange(second);
			return set.ToArray();
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static ILgWritingSystem[] VernacularAnalysisWss(FdoCache cache)
		{
			return VernacularAnalysisWss(cache, false);
		}
		private static ILgWritingSystem[] VernacularAnalysisWss(FdoCache cache, bool fIncludeUncheckedActiveWss)
		{
			List<int> hvosVern = GetCurrentThenRemainingActiveWss(cache.LangProject.CurVernWssRS,
				cache.LangProject.VernWssRC, !fIncludeUncheckedActiveWss);
			List<int> hvosAnalysis = GetCurrentThenRemainingActiveWss(cache.LangProject.CurAnalysisWssRS,
				cache.LangProject.AnalysisWssRC, !fIncludeUncheckedActiveWss);
			return WssFromHvos(
				MergeTwoArrays(hvosVern.ToArray(), hvosAnalysis.ToArray()),
				cache);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static ILgWritingSystem[] AnalysisVernacularWss(FdoCache cache)
		{
			return AnalysisVernacularWss(cache, false);
		}

		private static ILgWritingSystem[] AnalysisVernacularWss(FdoCache cache, bool fIncludeUncheckedActiveWss)
		{
			List<int> hvosVern = GetCurrentThenRemainingActiveWss(cache.LangProject.CurVernWssRS,
				cache.LangProject.VernWssRC, !fIncludeUncheckedActiveWss);
			List<int> hvosAnalysis = GetCurrentThenRemainingActiveWss(cache.LangProject.CurAnalysisWssRS,
				cache.LangProject.AnalysisWssRC, !fIncludeUncheckedActiveWss);
			return WssFromHvos(
				MergeTwoArrays(hvosAnalysis.ToArray(), hvosVern.ToArray()),
				cache);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void MakeRoot()
		{
			CheckDisposed();

			m_rootb = null;
			base.MakeRoot();

			if (m_fdoCache == null || DesignMode)
				return;

			m_rgws = GetWritingSystemOptions(true);

			// A crude way of making sure the property we want is loaded into the cache.
			CmObject.CreateFromDBObject(m_fdoCache, m_hvoObj);

			int wsUser = m_fdoCache.LanguageWritingSystemFactoryAccessor.UserWs;
			m_vc = new LabeledMultiStringViewVc(m_flid, m_rgws, wsUser, m_editable, this);

			// Review JohnT: why doesn't the base class do this??
			m_rootb = VwRootBoxClass.Create();
			m_rootb.SetSite(this);

			// And maybe this too, at least by default?
			m_rootb.DataAccess = m_fdoCache.MainCacheAccessor;

			// arg3 is a meaningless initial fragment, since this VC only displays one thing.
			// arg4 could be used to supply a stylesheet.
			m_rootb.SetRootObject(m_hvoObj, m_vc, 1, m_styleSheet);
		}

		/// <summary>
		/// returns a list of the writing systems available to display for this view
		/// </summary>
		/// <param name="fIncludeUncheckedActiveWss">if false, include only current wss,
		/// if true, includes unchecked active wss.</param>
		/// <returns></returns>
		public ILgWritingSystem[] GetWritingSystemOptions(bool fIncludeUncheckedActiveWss)
		{
			return GetWritingSystemList(m_fdoCache, m_wsMagic, m_hvoObj, m_forceIncludeEnglish, fIncludeUncheckedActiveWss);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the writing system list.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="wsMagic">The ws magic.</param>
		/// <param name="hvoObj">The hvo obj.</param>
		/// <param name="forceIncludeEnglish">if set to <c>true</c> [force include english].</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static ILgWritingSystem[] GetWritingSystemList(FdoCache cache, int wsMagic,
			int hvoObj, bool forceIncludeEnglish)
		{
			// add only current writing systems (not all active), by default
			return GetWritingSystemList(cache, wsMagic, hvoObj, forceIncludeEnglish, false);
		}

		/// <summary>
		/// Gets the writing system list.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="wsMagic">The ws magic.</param>
		/// <param name="hvoObj">The hvo obj.</param>
		/// <param name="forceIncludeEnglish">if set to <c>true</c> [force include english].</param>
		/// <param name="fIncludeUncheckedActiveWss">if true, add appropriate non-current but active writing systems.</param>
		/// <returns></returns>
		public static ILgWritingSystem[] GetWritingSystemList(FdoCache cache, int wsMagic,
			int hvoObj, bool forceIncludeEnglish, bool fIncludeUncheckedActiveWss)
		{
			switch(wsMagic)
			{
				case LangProject.kwsAnals:
					return AnalysisWss(cache, fIncludeUncheckedActiveWss);
				case LangProject.kwsVerns:
					return VernWss(cache, fIncludeUncheckedActiveWss);
				case LangProject.kwsAnalVerns:
					return AnalysisVernacularWss(cache, fIncludeUncheckedActiveWss);
				case LangProject.kwsVernAnals:
					return VernacularAnalysisWss(cache, fIncludeUncheckedActiveWss);
				case LangProject.kwsPronunciations:
					return PronunciationWritingSystems(cache);
				case LangProject.kwsAllReversalIndex:
					return ReversalIndexWritingSystems(cache, hvoObj, forceIncludeEnglish);
				default: // for now some sort of default.
					return AnalysisWss(cache, fIncludeUncheckedActiveWss);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the writing system list.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="wsMagic">The ws magic.</param>
		/// <param name="forceIncludeEnglish">if set to <c>true</c> [force include english].</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static ILgWritingSystem[] GetWritingSystemList(FdoCache cache, int wsMagic,
			bool forceIncludeEnglish)
		{
			return GetWritingSystemList(cache, wsMagic, 0, forceIncludeEnglish);
		}

		/// <summary>
		/// This is the list of writing systems that can be enabled for this control.
		/// </summary>
		public ILgWritingSystem[] WritingSystemOptions
		{
			get
			{
				CheckDisposed();
				return m_rgws;
			}
		}

		/// <summary>
		/// if non-null, we'll use this list to determine which writing systems to display.
		/// if null, we'll display every writing system option.
		/// </summary>
		public ILgWritingSystem[] WritingSystemsToDisplay
		{
			get { return m_rgwsToDisplay; }
			set
			{
				m_rgwsToDisplay = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Triggers the display.
		/// </summary>
		/// <param name="vwenv">The vwenv.</param>
		/// ------------------------------------------------------------------------------------
		internal void TriggerDisplay(IVwEnv vwenv)
		{
			CheckDisposed();

			if (Display != null)
				Display(this, new VwEnvEventArgs(vwenv));
		}

		/// <summary>
		/// Make a selection in the specified writing system at the specified character offset.
		/// Note: selecting other than the first writing system is not yet implemented.
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="ich"></param>
		public void SelectAt(int ws, int ich)
		{
			CheckDisposed();

			Debug.Assert(ws == m_rgws[0].Hvo);
			try
			{
				RootBox.MakeTextSelection(0, 0, null, m_flid, 0, ich, ich, ws, true, -1, null, true);
			}
			catch (Exception)
			{
				Debug.Assert(false, "Unexpected failure to make selection in LabeledMultiStringView");
			}
		}
	}

	/// <summary>
	/// LabeledMultiStringControl (used in InsertEntryDlg)
	/// has an FdoCache, but it is used only to figure out the writing systems to use; the control
	/// works with a dummy cache, object, and flid, and the resulting text must be read back.
	/// </summary>
	public class LabeledMultiStringControl : UserControl, IVwNotifyChange, IFWDisposable
	{
		InnerLabeledMultiStringControl m_innerControl;
		bool m_isHot = false;
		bool m_hasBorder;
		Padding m_textPadding;

		/// <summary>
		/// Initializes a new instance of the <see cref="LabeledMultiStringControl"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="wsMagic">The ws magic.</param>
		/// <param name="vss">The VSS.</param>
		/// ------------------------------------------------------------------------------------
		/// ------------------------------------------------------------------------------------
		public LabeledMultiStringControl(FdoCache cache, int wsMagic, IVwStylesheet vss)
		{
			m_innerControl = new InnerLabeledMultiStringControl(cache, wsMagic);
			if (vss != null)
				m_innerControl.StyleSheet = vss;
			m_innerControl.Dock = DockStyle.Fill;
			this.Controls.Add(m_innerControl);
			m_innerControl.MakeRoot();

			m_innerControl.RootBox.DataAccess.AddNotification(this);
			m_innerControl.MouseEnter += new EventHandler(m_innerControl_MouseEnter);
			m_innerControl.MouseLeave += new EventHandler(m_innerControl_MouseLeave);
			m_innerControl.GotFocus += new EventHandler(m_innerControl_GotFocus);
			m_innerControl.LostFocus += new EventHandler(m_innerControl_LostFocus);

			HasBorder = true;
			Height = PreferredHeight;
		}

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			// m_sda COM object block removed due to crash in Finializer thread LT-6124

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_innerControl != null)
				{
					if (m_innerControl.RootBox != null && m_innerControl.RootBox.DataAccess != null)
						m_innerControl.RootBox.DataAccess.RemoveNotification(this);

					m_innerControl.MouseEnter -= new EventHandler(m_innerControl_MouseEnter);
					m_innerControl.MouseLeave -= new EventHandler(m_innerControl_MouseLeave);
					m_innerControl.GotFocus -= new EventHandler(m_innerControl_GotFocus);
					m_innerControl.LostFocus -= new EventHandler(m_innerControl_LostFocus);
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_innerControl = null;

			base.Dispose(disposing);
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		#endregion IDisposable override

		/// <summary>
		/// Gets the preferred height.
		/// </summary>
		/// <value>The preferred height.</value>
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int PreferredHeight
		{
			get
			{
				CheckDisposed();
				int borderHeight = 0;
				switch (BorderStyle)
				{
					case BorderStyle.Fixed3D:
						borderHeight = SystemInformation.Border3DSize.Height * 2;
						break;

					case BorderStyle.FixedSingle:
						borderHeight = SystemInformation.BorderSize.Height * 2;
						break;
				}
				int height = 0;
				if (m_innerControl.RootBox != null && m_innerControl.RootBox.Height > 0)
					height = Math.Min(m_innerControl.RootBox.Height + 8, 66);
				else
					height = 46;	// barely enough to make a scroll bar workable
				return height + base.Padding.Vertical + borderHeight;
			}
		}

		Rectangle ContentRectangle
		{
			get
			{
				if (!Application.RenderWithVisualStyles || !m_hasBorder)
					return ClientRectangle;

				using (Graphics g = CreateGraphics())
				{
					VisualStyleRenderer renderer = new VisualStyleRenderer(VisualStyleElement.TextBox.TextEdit.Normal);
					return renderer.GetBackgroundContentRectangle(g, ClientRectangle);
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the text box has a border.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance has a border, otherwise <c>false</c>.
		/// </value>
		public bool HasBorder
		{
			get
			{
				CheckDisposed();
				return m_hasBorder;
			}

			set
			{
				CheckDisposed();
				m_hasBorder = value;
				if (Application.RenderWithVisualStyles)
					SetPadding();
				else
					BorderStyle = m_hasBorder ? BorderStyle.Fixed3D : BorderStyle.None;
			}
		}

		/// <summary>
		/// Gets or sets the border style of the tree view control.
		/// </summary>
		/// <value></value>
		/// <returns>
		/// One of the <see cref="T:System.Windows.Forms.BorderStyle"/> values. The default is <see cref="F:System.Windows.Forms.BorderStyle.Fixed3D"/>.
		/// </returns>
		/// <exception cref="T:System.ComponentModel.InvalidEnumArgumentException">
		/// The assigned value is not one of the <see cref="T:System.Windows.Forms.BorderStyle"/> values.
		/// </exception>
		/// <PermissionSet>
		/// 	<IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
		/// </PermissionSet>
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new BorderStyle BorderStyle
		{
			get
			{
				return base.BorderStyle;
			}

			set
			{
				if (!Application.RenderWithVisualStyles)
				{
					base.BorderStyle = value;
					m_hasBorder = value != BorderStyle.None;
				}
			}
		}

		/// <summary>
		/// Gets or sets padding within the control. This adjusts the padding around the text.
		/// </summary>
		/// <value></value>
		/// <returns>
		/// A <see cref="T:System.Windows.Forms.Padding"/> representing the control's internal spacing characteristics.
		/// </returns>
		/// <PermissionSet>
		/// 	<IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Unrestricted="true"/>
		/// </PermissionSet>
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public new Padding Padding
		{
			get
			{
				CheckDisposed();
				return m_textPadding;
			}

			set
			{
				CheckDisposed();
				m_textPadding = value;
				SetPadding();
			}
		}

		/// <summary>
		/// Gets a value indicating whether the control has input focus.
		/// </summary>
		/// <value></value>
		/// <returns>true if the control has focus; otherwise, false.
		/// </returns>
		public override bool Focused
		{
			get
			{
				CheckDisposed();
				return m_innerControl.Focused;
			}
		}

		/// <summary>
		/// Gets the root box.
		/// </summary>
		/// <value>The root box.</value>
		[BrowsableAttribute(false),
			DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public IVwRootBox RootBox
		{
			get
			{
				CheckDisposed();
				return m_innerControl.RootBox;
			}
		}

		TextBoxState State
		{
			get
			{
				if (Enabled)
					return m_isHot ? TextBoxState.Hot : TextBoxState.Normal;
				else
					return TextBoxState.Disabled;
			}
		}

		void SetPadding()
		{
			Rectangle rect = ContentRectangle;
			base.Padding = new Padding((rect.Left - ClientRectangle.Left) + m_textPadding.Left,
				(rect.Top - ClientRectangle.Top) + m_textPadding.Top, (ClientRectangle.Right - rect.Right) + m_textPadding.Right,
				(ClientRectangle.Bottom - rect.Bottom) + m_textPadding.Bottom);
		}

		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Control.Paint"/> event.
		/// </summary>
		/// <param name="e">A <see cref="T:System.Windows.Forms.PaintEventArgs"/> that contains the event data.</param>
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			VisualStyleRenderer renderer = FwTextBox.CreateRenderer(State, ContainsFocus, true);
			if (renderer != null)
				renderer.DrawBackground(e.Graphics, ClientRectangle, e.ClipRectangle);
		}

		void m_innerControl_MouseLeave(object sender, EventArgs e)
		{
			m_isHot = false;
			Invalidate();
		}

		void m_innerControl_MouseEnter(object sender, EventArgs e)
		{
			m_isHot = true;
			Invalidate();
		}

		void m_innerControl_LostFocus(object sender, EventArgs e)
		{
			Invalidate();
		}

		void m_innerControl_GotFocus(object sender, EventArgs e)
		{
			Invalidate();
		}

		/// <summary>
		/// Get one of the resulting strings.
		/// Enhance JohnT: make a setter, too.
		/// </summary>
		/// <param name="ws"></param>
		/// <returns></returns>
		public ITsString Value(int ws)
		{
			CheckDisposed();

			return m_innerControl.RootBox.DataAccess.get_MultiStringAlt(InnerLabeledMultiStringControl.khvoRoot,
				InnerLabeledMultiStringControl.kflid, ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="tss"></param>
		/// ------------------------------------------------------------------------------------
		public void SetValue(int ws, ITsString tss)
		{
			CheckDisposed();

			m_innerControl.RootBox.DataAccess.SetMultiStringAlt(InnerLabeledMultiStringControl.khvoRoot,
				InnerLabeledMultiStringControl.kflid, ws, tss);
			m_innerControl.RootBox.DataAccess.PropChanged(null, (int)PropChangeType.kpctNotifyAll, InnerLabeledMultiStringControl.khvoRoot,
				InnerLabeledMultiStringControl.kflid, ws, 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="txt"></param>
		/// ------------------------------------------------------------------------------------
		public void SetValue(int ws, string txt)
		{
			CheckDisposed();

			ITsStrFactory tsf = TsStrFactoryClass.Create();
			SetValue(ws, tsf.MakeString(txt, ws));
		}

		/// <summary>
		/// Get the number of writing systems being displayed.
		/// </summary>
		public int NumberOfWritingSystems
		{
			get
			{
				CheckDisposed();
				return m_innerControl.WritingSystems.Length;
			}
		}

		/// <summary>
		/// Get the nth string and writing system.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="ws"></param>
		/// <returns></returns>
		public ITsString ValueAndWs(int index, out int ws)
		{
			CheckDisposed();

			ws = m_innerControl.WritingSystems[index].Hvo;
			return m_innerControl.RootBox.DataAccess.get_MultiStringAlt(InnerLabeledMultiStringControl.khvoRoot,
				InnerLabeledMultiStringControl.kflid, ws);
		}

		/// <summary>
		/// Selects a range of text based on the specified writing system.
		/// </summary>
		/// <param name="ws">The writing system.</param>
		/// <param name="start">The position of the first character in the current text selection within the text box.</param>
		/// <param name="length">The number of characters to select.</param>
		/// <remarks>
		/// If you want to set the start position to the first character in the control's text, set the <i>start</i> parameter to 0.
		/// You can use this method to select a substring of text, such as when searching through the text of the control and replacing information.
		/// <b>Note:</b> You can programmatically move the caret within the text box by setting the <i>start</i> parameter to the position within
		/// the text box where you want the caret to move to and set the <i>length</i> parameter to a value of zero (0).
		/// The text box must have focus in order for the caret to be moved.
		/// </remarks>
		/// <exception cref="ArgumentException">
		/// The value assigned to either the <i>start</i> parameter or the <i>length</i> parameter is less than zero.
		/// </exception>
		public void Select(int ws, int start, int length)
		{
			CheckDisposed();

			if (start < 0)
				throw new ArgumentException("Starting position is less than zero.", "start");
			if (length < 0)
				throw new ArgumentException("Length is less than zero.", "length");

			IVwSelection sel = m_innerControl.RootBox.Selection;
			if (sel != null)
			{
				// See if the desired thing is already selected. If so do nothing. This can prevent stack overflow!
				ITsString tssDummy;
				int ichAnchor, ichEnd, hvo, tag, wsDummy;
				bool fAssocPrev;
				sel.TextSelInfo(true, out tssDummy, out ichEnd, out fAssocPrev, out hvo, out tag, out wsDummy);
				sel.TextSelInfo(false, out tssDummy, out ichAnchor, out fAssocPrev, out hvo, out tag, out wsDummy);
				if (Math.Min(ichAnchor, ichEnd) == start && Math.Max(ichAnchor, ichEnd) == start + length)
					return;
			}
			try
			{
				m_innerControl.RootBox.MakeTextSelection(0, 0, null, InnerLabeledMultiStringControl.kflid, 0, start, start + length,
					ws, false, -1, null, true);
			}
			catch
			{
			}
		}

		#region IVwNotifyChange Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ivMin"></param>
		/// <param name="cvIns"></param>
		/// <param name="cvDel"></param>
		/// ------------------------------------------------------------------------------------
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			CheckDisposed();

			OnTextChanged(new EventArgs());
		}

		#endregion
	}

	internal class InnerLabeledMultiStringControl : SimpleRootSite
	{
		LabeledMultiStringVc m_vc;
		FdoCache m_realCache; // real one we get writing system info from
		ISilDataAccess m_sda; // one actually used in the view.
		ILgWritingSystem[] m_rgws;
		int m_wsMagic;

		internal const int khvoRoot = -3045; // arbitrary but recognizeable numbers for debugging.
		internal const int kflid = 4554;

		public InnerLabeledMultiStringControl(FdoCache cache, int wsMagic)
		{
			m_realCache = cache;
			m_wsMagic = wsMagic;
			m_sda = VwCacheDaClass.Create();
			m_sda.WritingSystemFactory = cache.LanguageWritingSystemFactoryAccessor;
			m_rgws = LabeledMultiStringView.GetWritingSystemList(m_realCache, m_wsMagic, 0, false);

			this.AutoScroll = true;
			this.IsTextBox = true;	// range selection not shown when not in focus
		}

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			// m_sda COM object block removed due to crash in Finializer thread LT-6124

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_vc != null)
					m_vc.Dispose();

				if (m_sda != null)
					(m_sda as IVwCacheDa).ClearAllData();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_sda = null;
			m_realCache = null;
			m_rgws = null;
			m_vc = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// <summary>
		/// Get the number of writing systems being displayed.
		/// </summary>
		public ILgWritingSystem[] WritingSystems
		{
			get
			{
				CheckDisposed();
				return m_rgws;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void MakeRoot()
		{
			CheckDisposed();

			if (DesignMode)
				return;

			m_rootb = VwRootBoxClass.Create();
			m_rootb.SetSite(this);
			m_rootb.DataAccess = m_sda;

			int wsUser = m_realCache.LanguageWritingSystemFactoryAccessor.UserWs;
			int wsEn = m_realCache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("en");
			m_vc = new LabeledMultiStringVc(kflid, m_rgws, wsUser, true, wsEn);

			// arg3 is a meaningless initial fragment, since this VC only displays one thing.
			m_rootb.SetRootObject(khvoRoot, m_vc, 1, m_styleSheet);
			m_dxdLayoutWidth = kForceLayout; // Don't try to draw until we get OnSize and do layout.
			// The simple root site won't lay out properly until this is done.
			// It needs to be done before base.MakeRoot or it won't lay out at all ever!
			WritingSystemFactory = m_realCache.LanguageWritingSystemFactoryAccessor;
			base.MakeRoot();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// User pressed a key.
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (!m_editingHelper.HandleOnKeyDown(e))
				base.OnKeyDown(e);
		}
	}

	/// <summary>
	/// View constructor for LabeledMultiStringView.
	/// </summary>
	internal class LabeledMultiStringVc: VwBaseVc
	{
		internal int m_flid;
		internal ILgWritingSystem [] m_rgws; // writing systems to display
		ITsTextProps m_ttpLabel; // Props to use for ws name labels
		int m_wsUI;		// ws to use to display UI stuff, such as WS name labels.
		bool m_editable = true;
		int m_wsEn;

		public LabeledMultiStringVc(int flid, ILgWritingSystem[] rgws, int wsUser, bool editable, int wsEn)
		{
			m_flid = flid;
			m_rgws = rgws;
			m_wsUI = wsUser;
			m_ttpLabel = LgWritingSystem.AbbreviationTextProperties;
			m_editable = editable;
			m_wsEn = wsEn == 0 ? wsUser : wsEn;
			// Here's the C++ code which does the same thing using styles.
			//				StrUni stuLangCodeStyle(L"Language Code");
			//				ITsPropsFactoryPtr qtpf;
			//				qtpf.CreateInstance(CLSID_TsPropsFactory);
			//				StrUni stu;
			//				ITsStringPtr qtss;
			//				ITsStrFactoryPtr qtsf;
			//				qtsf.CreateInstance(CLSID_TsStrFactory);
			//				// Get the properties of the "Language Code" style for the writing system
			//				// which corresponds to the user's environment.
			//				qtpf->MakeProps(stuLangCodeStyle.Bstr(), ???->UserWs(), 0, &qttp);
		}

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			base.Dispose(disposing);

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_rgws = null;
			if (m_ttpLabel != null)
			{
				Marshal.ReleaseComObject(m_ttpLabel);
				m_ttpLabel = null;
			}
		}

		#endregion IDisposable override

		private ITsString NameOfWs(ITsStrFactory tsf, int i)
		{
			// Don't use this, it uses the analysis default writing system which is not wanted per LT-4610.
			//tsf.MakeString(m_rgws[i].Abbreviation, m_wsUI);
			ILgWritingSystem ws = m_rgws[i];
			// Display in English if possible for now (August 2008).  See LT-8631 and LT-8574.
			ITsString result = ws.Cache.MainCacheAccessor.get_MultiStringAlt(ws.Hvo,
				(int)LgWritingSystem.LgWritingSystemTags.kflidAbbr, m_wsEn/*m_wsUI*/);

			if (result == null || result.Length == 0)
				return tsf.MakeString(m_rgws[i].Abbreviation, m_wsEn/*m_wsUI*/);
			return result;
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			CheckDisposed();

			TriggerDisplay(vwenv);
			//if (m_rgws.Length == 1)
			//{
			//    // Single option...don't bother with labels.
			//    if (m_rgws[0].RightToLeft)
			//    {
			//        vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft,
			//            (int)FwTextPropVar.ktpvEnum,
			//            (int)FwTextToggleVal.kttvForceOn);
			//        vwenv.set_IntProperty((int)FwTextPropType.ktptAlign,
			//            (int)FwTextPropVar.ktpvEnum,
			//            (int)FwTextAlign.ktalTrailing);
			//    }
			//    vwenv.set_IntProperty((int)FwTextPropType.ktptPadTop, (int)FwTextPropVar.ktpvMilliPoint, 2000);
			//    vwenv.AddStringAltMember(m_flid, m_rgws[0].Hvo, this);
			//    return;
			//}
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			// We use a table to display
			// encodings in column one and the strings in column two.
			// The table uses 100% of the available width.
			VwLength vlTable;
			vlTable.nVal = 10000;
			vlTable.unit = VwUnit.kunPercent100;

			// The width of the writing system column is determined from the width of the
			// longest one which will be displayed.
			int dxs;	// Width of displayed string.
			int dys;	// Height of displayed string (not used here).
			int dxsMax = 0;	// Max width required.
			for (int i = 0; i < m_rgws.Length; ++i)
			{
				// Set qtss to a string representing the writing system.
				vwenv.get_StringWidth(NameOfWs(tsf, i),
					m_ttpLabel, out dxs, out dys);
				dxsMax = Math.Max(dxsMax, dxs);
			}
			VwLength vlColWs; // 5-pt space plus max label width.
			vlColWs.nVal = dxsMax + 5000;
			vlColWs.unit = VwUnit.kunPoint1000;

			// Enhance JohnT: possibly allow for right-to-left UI by reversing columns?

			// The Main column is relative and uses the rest of the space.
			VwLength vlColMain;
			vlColMain.nVal = 1;
			vlColMain.unit = VwUnit.kunRelative;

			vwenv.OpenTable(2, // Two columns.
				vlTable, // Table uses 100% of available width.
				0, // Border thickness.
				VwAlignment.kvaLeft, // Default alignment.
				VwFramePosition.kvfpVoid, // No border.
				VwRule.kvrlNone, // No rules between cells.
				0, // No forced space between cells.
				0, // No padding inside cells.
				false);
			// Specify column widths. The first argument is the number of columns,
			// not a column index. The writing system column only occurs at all if its
			// width is non-zero.
			vwenv.MakeColumns(1, vlColWs);
			vwenv.MakeColumns(1, vlColMain);

			vwenv.OpenTableBody();
			Set<ILgWritingSystem> visibleWss = new Set<ILgWritingSystem>();
			// if we passed in a view and have WritingSystemsToDisplay
			// then we'll load that list in order to filter our larger m_rgws list.
			AddViewWritingSystems(visibleWss);
			for (int i = 0; i < m_rgws.Length; ++i)
			{
				if (SkipEmptyWritingSystem(visibleWss, i, hvo))
					continue;
				vwenv.OpenTableRow();

				// First cell has writing system abbreviation displayed using m_ttpLabel.
				vwenv.Props = m_ttpLabel;
				vwenv.OpenTableCell(1,1);
				vwenv.AddString(NameOfWs(tsf, i));
				vwenv.CloseTableCell();

				// Second cell has the string contents for the alternative.
				// DN version has some property setting, including trailing margin and
				// RTL.
				if (m_rgws[i].RightToLeft)
				{
					vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft,
						(int)FwTextPropVar.ktpvEnum,
						(int)FwTextToggleVal.kttvForceOn);
					vwenv.set_IntProperty((int)FwTextPropType.ktptAlign,
						(int)FwTextPropVar.ktpvEnum,
						(int)FwTextAlign.ktalTrailing);
				}
				if (!m_editable)
				{
					vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum,
						(int)TptEditable.ktptNotEditable);
				}
				vwenv.set_IntProperty((int)FwTextPropType.ktptPadTop, (int)FwTextPropVar.ktpvMilliPoint, 2000);
				vwenv.OpenTableCell(1,1);
				vwenv.AddStringAltMember(m_flid, m_rgws[i].Hvo, this);
				vwenv.CloseTableCell();

				vwenv.CloseTableRow();
			}
			vwenv.CloseTableBody();

			vwenv.CloseTable();
		}

		/// <summary>
		/// Subclass with LabelledMultiStringView tests for empty alternatives and returns true to skip them.
		/// </summary>
		internal virtual bool SkipEmptyWritingSystem(Set<ILgWritingSystem> visibleWss, int i, int hvo)
		{
			return false;
		}

		/// <summary>
		/// Subclass with LabelledMultiStringView gets extra WSS to display from it.
		/// </summary>
		/// <param name="visibleWss"></param>
		internal virtual void AddViewWritingSystems(Set<ILgWritingSystem> visibleWss)
		{
		}

		/// <summary>
		/// Subclass with LabelledMultiStringView calls TriggerView
		/// </summary>
		internal virtual void TriggerDisplay(IVwEnv vwenv)
		{

		}
	}

	/// <summary>
	/// Subclass suitable for LabeledMultistringView.
	/// </summary>
	internal class LabeledMultiStringViewVc : LabeledMultiStringVc
	{
		LabeledMultiStringView m_view;

		public LabeledMultiStringViewVc(int flid, ILgWritingSystem[] rgws, int wsUser, bool editable,
			LabeledMultiStringView view)
			: base(flid, rgws, wsUser, editable, view.WritingSystemFactory.GetWsFromStr("en"))
		{
			m_view = view;
			Debug.Assert(m_view != null);
		}
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_view = null;
		}

		internal override void TriggerDisplay(IVwEnv vwenv)
		{
			base.TriggerDisplay(vwenv);
			m_view.TriggerDisplay(vwenv);
		}

		internal override void AddViewWritingSystems(Set<ILgWritingSystem> visibleWss)
		{
			if (m_view.WritingSystemsToDisplay != null)
			{
				visibleWss.AddRange(m_view.WritingSystemsToDisplay);
			}
		}
		internal override bool SkipEmptyWritingSystem(Set<ILgWritingSystem> visibleWss, int i, int hvo)
		{
			// if we have defined writing systems to display, we want to
			// show those, plus other options that have data.
			// otherwise, we'll assume we want to display the given ws fields.
			// (this effectively means that setting WritingSystemsToDisplay to 'null'
			// will display all the ws options in m_rgws. That is also what happens in the base class.)
			if (m_view.WritingSystemsToDisplay != null)
			{
				// if we haven't configured to display this writing system
				// we still want to show it if it has data.
				if (!visibleWss.Contains(m_rgws[i]))
				{
					ITsString result = m_view.Cache.MainCacheAccessor.get_MultiStringAlt(hvo, m_flid, m_rgws[i].Hvo);
					if (result == null || result.Length == 0)
						return true;
				}
			}
			return false;
		}
	}

	/// <summary>
	/// Delegate defn for an event handler that passes an IVwEnv.
	/// </summary>
	public delegate void VwEnvEventHandler (object sender, VwEnvEventArgs e);

	/// <summary>
	/// Event Args for an event that passes a VwEnv.
	/// </summary>
	public class VwEnvEventArgs : EventArgs
	{
		IVwEnv m_env;
		/// <summary>
		/// Make one.
		/// </summary>
		/// <param name="env"></param>
		public VwEnvEventArgs(IVwEnv env)
		{
			m_env = env;
		}

		/// <summary>
		/// Get the environment.
		/// </summary>
		public IVwEnv Environment
		{
			get { return m_env; }
		}
	}
}

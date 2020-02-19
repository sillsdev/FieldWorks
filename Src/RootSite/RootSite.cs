// Copyright (c) 2002-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// <remarks>
// Implementation of RootSite (formerly AfVwRootSite and AfVwScrollWndBase).
// This class does most of the interesting work relating to hosting a FieldWorks View
// represented as an IVwRootBox. It provides an implementation of IVwRootSite,
// passes interesting events to the root box, and handles various common menu
// commands and toolbar functions.
//
// The original RootSite class contained most of the code of this file, but this was later
// refactored to enable a distinction between a SimpleRootSite that does not know what cache
// is being used for the view, and RootSite which has an LcmCache member variable.
// </remarks>

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.SpellChecking;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using Rect = SIL.FieldWorks.Common.ViewsInterfaces.Rect;

// How to debug COM reference counts:
// a) create a global variable that contains a file handle:
// 		HANDLE hLogFile = CreateFile(_T("c:\\log.txt"), GENERIC_WRITE,
//			FILE_SHARE_WRITE | FILE_SHARE_READ, NULL, CREATE_ALWAYS,
//			FILE_ATTRIBUTE_NORMAL | FILE_FLAG_WRITE_THROUGH, NULL);
// b) in your c'tor, call:
//		_CrtSetReportMode(_CRT_WARN, _CRTDBG_MODE_FILE);
//		_CrtSetReportFile(_CRT_WARN, hLogFile);
// c) any output to _CrtDebugReport goes now to c:\log.txt, e.g.
//		_CrtDbgReport(_CRT_WARN, NULL, 0, NULL, "VwGraphics::VwGraphics\n");
// d) use Cygwin's "tail.exe" to display the debug messages while the app is running:
//		tail -f log.txt

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// RootSite is the most commonly used implementation of IVwRootSite for applications that
	/// don't want to use the FieldWorks Framework classes (if using them is OK, FwRootSite
	/// adds considerable functionality, such as stylesheets and find/replace). It requires
	/// initialization with an LcmCache.
	/// </summary>
	public class RootSite : SimpleRootSite, IHeightEstimator, IRootSiteSlave
	{
		#region Member variables
		/// <summary>The height in points of an average paragraph when the rootsite is
		/// <c>kdxBaselineRootsiteWidth</c> wide</summary>
		public const int kdypBaselineParagraphHeight = 12;
		/// <summary>The baseline width in pixels of a rootsite, used for storing height
		/// estimates that are independent of the actual width of the rootsite.
		/// </summary>
		public const int kdxBaselineRootsiteWidth = 1500;

		/// <summary>The LCM cache</summary>
		protected LcmCache m_cache;
		/// <summary>Set to true while we are in the SelectionChanged method. We don't want
		/// to process any other selection changes in any other view while we're not done
		/// with the first one.</summary>
		protected static bool s_fInSelectionChanged;
		/// <summary>The real average paragraph height in points, based on the available width
		/// of the client rectangle. To speed things up we store it here, but it needs to be
		/// updated if the zoom, width or horizontal margin changes.</summary>
		protected int m_ParaHeightInPoints;
		/// <summary>Used to keep from updating the selection on every keystroke.</summary>
		private Rect m_prevParaRectangle;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private Container components = null;
		#endregion

		/// <summary />
		public RootSite()
		{
			base.AutoScroll = true;

			InitializeComponent();
			// RootSite shouldn't handle tabs like a control
			AcceptsTab = true;
			AcceptsReturn = true;
		}

		/// <summary>
		/// Initialize one. It doesn't have a scroll bar because the containing Group is
		/// meant to handle scrolling.
		/// </summary>
		public RootSite(LcmCache cache) : this()
		{
			Cache = cache; // make sure to set the property, not setting m_cache directly
		}

		private void InitializeComponent()
		{
			Name = "RootSite";
			AccessibleName = "RootSite";
		}

		/// <summary />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (RootBox != null)
			{
				CloseRootBox();
				RootBox = null;
			}
			if (disposing)
			{
				PropertyTable?.GetValue<IIdleQueueProvider>(FwUtils.FwUtils.window)?.IdleQueue?.Remove(SpellCheckOnIdle);
			}

			base.Dispose(disposing);

			if (disposing)
			{
				components?.Dispose();
			}
			m_cache = null;
		}

		/// <summary>
		/// This MUST be called by the MakeRoot method or something similar AFTER the
		/// root box is created but BEFORE the view is laid out. Even after it is called,
		/// MakeRoot must not do anything that would cause layout; that should not happen
		/// until all roots are synchronized.
		/// </summary>
		protected void Synchronize(IVwRootBox rootb)
		{
		}

		/// <summary>
		/// Calculates the average paragraph height in points, based on the available width.
		/// </summary>
		protected void CalculateAvgParaHeightInPoints()
		{
			var width = GetAvailWidth(null);
			if (width <= 0)
			{
				width = 1;
			}
			m_ParaHeightInPoints = kdxBaselineRootsiteWidth * AverageParaHeight / width;
		}

		#region Properties
		/// <summary>
		/// Set to true if this view should be spell-checked. For now this just displays the red
		/// squiggle if our spelling checker thinks a word is mis-spelled.
		/// </summary>
		public bool DoSpellCheck { get; set; } = false;

		/// <summary>
		/// Get the editing helper for RootSite.
		/// </summary>
		public RootSiteEditingHelper RootSiteEditingHelper => EditingHelper as RootSiteEditingHelper;

		/// <summary>
		/// Gets a value indicating whether a selection is currently being changed.
		/// </summary>
		public override bool InSelectionChanged
		{
			get { return s_fInSelectionChanged; }
			set { s_fInSelectionChanged = value; }
		}

		/// <summary>
		/// With access to the cache, we can limit this to writing sytems the user might plausibly want for this project.
		/// </summary>
		protected override CoreWritingSystemDefinition[] PlausibleWritingSystems => m_cache.ServiceLocator.WritingSystems.AllWritingSystems.ToArray();

		/// <summary>
		/// Creates a new RootSiteEditingHelper used for processing editing requests.
		/// </summary>
		protected override EditingHelper CreateEditingHelper()
		{
			return new RootSiteEditingHelper(m_cache, this);
		}

		/// <summary>
		/// Called when the editing helper is created.
		/// </summary>
		protected override void OnEditingHelperCreated()
		{
			m_editingHelper.VwSelectionChanged += HandleSelectionChange;
		}

		/// <summary>
		/// Gets the average paragraph height in points.
		/// </summary>
		public virtual int AverageParaHeight => kdypBaselineParagraphHeight;

		/// <summary>
		/// Override the getter to obtain a WSF from the LcmCache, if we don't have
		/// one set independently, as is usually the case for this class.
		/// </summary>
		public override ILgWritingSystemFactory WritingSystemFactory => m_wsf == null && m_cache != null ? m_cache.WritingSystemFactory : m_wsf;

		/// <summary>
		/// This tests whether the class has a cache (in this RootSite subclass) or
		/// (in the SimpleRootSite base class) whether it has a ws. This is often used to determine whether
		/// we are sufficiently initialized to go ahead with some operation that may get called
		/// prematurely by something in the .NET framework.
		/// </summary>
		public override bool GotCacheOrWs => m_cache != null;

		/// <summary>
		/// Gets a string representation of the object suitable to put on the clipboard.
		/// </summary>
		public override string get_TextRepOfObj(ref Guid guid)
		{
			return RootSiteEditingHelper.TextRepOfObj(m_cache, guid);
		}

		/// <summary>
		/// Show the writing system choices?
		/// </summary>
		public override bool IsSelectionFormattable
		{
			get
			{
				if (DesignMode || RootBox == null)
				{
					return false;
				}
				if (EditingHelper?.CurrentSelection == null || !EditingHelper.Editable)
				{
					return false;
				}
				var sel = EditingHelper.CurrentSelection.Selection;
				if (sel != null && !sel.IsEditable)
				{
					return false;
				}
				//todo: in some complex selection, we will want to just "say no". For now, we just
				//look at the start (anchor) of the selection.
				var flid = EditingHelper.CurrentSelection.GetTextPropId(SelLimitType.Anchor);
				if (flid == 0) // can happen for e.g. icons
				{
					return false;
				}
				// Don't use LcmCache here, it doesn't know about decorators.
				var mdc = RootBox.DataAccess.GetManagedMetaDataCache() ?? Cache.GetManagedMetaDataCache();
				if (!mdc.FieldExists(flid))
				{
					return false; // some sort of special field; if it ought to be formattable, make a decorator MDC that recognizes it.
				}
				var type = (CellarPropertyType)mdc.GetFieldType((int)flid);
				return !(type == CellarPropertyType.Unicode || type == CellarPropertyType.MultiUnicode);
			}
		}

		/// <summary>
		/// Get the best style name that suits the selection and put it into the proepty table.
		/// </summary>
		public override string Style_Changed(BaseStyleInfo newValue)
		{
			if (DesignMode || RootBox == null || EditingHelper == null)
			{
				// In these cases, don't try to update the "BestStyleName" property.
				return string.Empty;
			}
			var bestStyle = BestSelectionStyle;
			if (newValue.Name == bestStyle)
			{
				return newValue.Name;
			}
			EditingHelper.SuppressNextBestStyleNameChanged = true;
			return bestStyle; // Changed the style, so caller will know of the change.
		}

		#region Overrides of SimpleRootSite

		/// <summary>
		/// Get the best style name that suits the selection.
		/// </summary>
		public override string BestSelectionStyle
		{
			get
			{
				string bestStyle;
				if (EditingHelper.CurrentSelection == null || EditingHelper.Editable == false)
				{
					bestStyle = string.Empty;
				}
				else
				{
					var sel = EditingHelper.CurrentSelection.Selection;
					if (sel != null && !sel.IsEditable)
					{
						bestStyle = string.Empty;
					}
					else
					{
						var flidAnchor = EditingHelper.CurrentSelection.GetTextPropId(SelLimitType.Anchor);
						if (flidAnchor == 0) // can happen for e.g. icons
						{
							bestStyle = string.Empty;
						}
						else
						{
							var flidEnd = EditingHelper.CurrentSelection.GetTextPropId(SelLimitType.End);
							if (flidEnd != flidAnchor)
							{
								bestStyle = string.Empty;
							}
							else
							{
								var mdc = RootBox.DataAccess.GetManagedMetaDataCache();
								if (!mdc.FieldExists(flidAnchor))
								{
									bestStyle = string.Empty;
								}
								else
								{
									var type = (CellarPropertyType)RootBox.DataAccess.MetaDataCache.GetFieldType(flidAnchor);
									if (type != CellarPropertyType.String && type != CellarPropertyType.MultiString)
									{
										bestStyle = string.Empty;
									}
									else
									{
										var paraStyleName = EditingHelper.GetParaStyleNameFromSelection();
										var charStyleName = EditingHelper.GetCharStyleNameFromSelection();
										if (string.IsNullOrEmpty(charStyleName) && flidAnchor == StTxtParaTags.kflidContents)
										{
											bestStyle = paraStyleName;
										}
										else if (charStyleName == string.Empty)
										{
											bestStyle = StyleUtils.DefaultParaCharsStyleName;
										}
										else if (charStyleName == null)
										{
											bestStyle = string.Empty;
										}
										else
										{
											bestStyle = charStyleName;
										}
									}
								}
							}
						}
					}
				}
				return bestStyle;
			}
		}

		#endregion

		/// <summary>
		/// Show paragraph styles?
		/// </summary>
		public override bool IsSelectionInParagraph
		{
			get
			{
				if (DesignMode || RootBox == null)
				{
					return false;
				}
				if (EditingHelper?.CurrentSelection == null || EditingHelper.Editable == false)
				{
					return false;
				}
				var sel = EditingHelper.CurrentSelection.Selection;
				if (sel != null && !sel.IsEditable)
				{
					return false;
				}
				var flidAnchor = EditingHelper.CurrentSelection.GetTextPropId(SelLimitType.Anchor);
				if (flidAnchor == 0) // can happen for e.g. icons
				{
					return false;
				}
				var flidEnd = EditingHelper.CurrentSelection.GetTextPropId(SelLimitType.End);
				if (flidEnd != flidAnchor)
				{
					return false;
				}
				return flidAnchor == StTxtParaTags.kflidContents;
			}
		}

		/// <summary>
		/// Gets or sets the LCM cache
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual LcmCache Cache
		{
			get
			{
				return m_cache;
			}
			set
			{
				m_cache = value;
				if (m_cache != null)
				{
					if (m_editingHelper is RootSiteEditingHelper)
					{
						RootSiteEditingHelper.Cache = m_cache;
					}
				}
			}
		}

		/// <summary />
		public override float Zoom
		{
			get
			{
				return base.Zoom;
			}
			set
			{
				base.Zoom = value;
				CalculateAvgParaHeightInPoints();
			}
		}

		/// <summary>
		/// Gets or sets the horizontal margin
		/// </summary>
		protected override int HorizMargin
		{
			get { return base.HorizMargin; }
			set
			{
				base.HorizMargin = value;
				CalculateAvgParaHeightInPoints();
			}
		}

		/// <summary>
		/// It sometimes helps to use the max of the sizes of all sites. I (JT) think that
		/// while adjusting the scroll range of one pane because of expanded lazy boxes,
		/// the size of the slaves is set to the appropriate root box. This can lead to
		/// an adjust scroll range for the other pane at a time when its range is much
		/// less, perhaps because it hasn't been synchronized yet.
		/// </summary>
		protected override Size AdjustedScrollRange => ScrollRange;
		#endregion

		#region Overridden Methods

		/// <summary>
		/// Overridden to kick off spell-checking
		/// </summary>
		protected override void OnTimer(object sender, EventArgs e)
		{
			base.OnTimer(sender, e);
			StartSpellingIfNeeded();
		}

		/// <summary>
		/// Restart the spell-checking process (e.g. when dictionary changed)
		/// </summary>
		public void RestartSpellChecking()
		{
			if (DoSpellCheck && RootBox != null)
			{
				RootBox.RestartSpellChecking();
				StartSpellingIfNeeded();
			}
		}

		/// <summary>
		/// Call this whenever we might have changed the state of the view, so that respelling
		/// is needed. This can happen quite often, since scrolling (for example) can expose
		/// new material to check. Currently we check after every paint.
		/// </summary>
		private void StartSpellingIfNeeded()
		{
			if (DoSpellCheck && RootBox != null)
			{
				RootBox.SetSpellingRepository(SpellingHelper.GetCheckerInstance);
				if (!RootBox.IsSpellCheckComplete())
				{
					PropertyTable?.GetValue<IIdleQueueProvider>(FwUtils.FwUtils.window).IdleQueue.Add(IdleQueuePriority.Low, SpellCheckOnIdle);
				}
			}
		}

		/// <summary>
		/// Call Draw() which does all the real painting
		/// </summary>
		protected override void OnPaint(PaintEventArgs e)
		{
			// This check is important especially because we must NOT clear m_fInPaint unless we set it true
			// ourselves. Otherwise, the first recursive call clears the flag, and the second one goes
			// ahead without the necessary suppression.
			if (CheckForRecursivePaint())
			{
				return;
			}

			base.OnPaint(e);

			PaintInProgress = true;
			try
			{
				StartSpellingIfNeeded();
			}
			finally
			{
				PaintInProgress = false;
			}
		}

		/// <summary>
		/// Override this to provide a context menu for some subclass.
		/// </summary>
		protected override bool DoContextMenu(IVwSelection invSel, Point pt, Rectangle rcSrcRoot, Rectangle rcDstRoot)
		{
			if (DoSpellCheck)
			{
				// Currently the only case in which we make a right-click menu by default.
				if (RootSiteEditingHelper.DoSpellCheckContextMenu(pt, this))
				{
					return true;
				}
			}
			return base.DoContextMenu(invSel, pt, rcSrcRoot, rcDstRoot);
		}

		/// <summary>
		/// This hook is installed when OnTimer detects that there is spell-checking to do.
		/// It removes itself when the view indicates it is completely checked, in order to
		/// reduce background work.
		/// </summary>
		private bool SpellCheckOnIdle(object parameter)
		{
			if (IsDisposed)
			{
				throw new InvalidOperationException("Thou shalt not call methods after I am disposed!");
			}
			return RootBox == null || RootBox.DoSpellCheckStep();
		}

		/// <summary>
		/// If we need to make a selection, but we can't because edits haven't been updated in the
		/// view, this method requests creation of a selection after the unit of work is complete.
		/// </summary>
		public override void RequestSelectionAtEndOfUow(IVwRootBox rootb, int ihvoRoot, int cvlsi,
			SelLevInfo[] rgvsli, int tagTextProp, int cpropPrevious, int ich, int wsAlt, bool fAssocPrev, ITsTextProps selProps)
		{
			// Creating one hooks it up; it will free itself when invoked.
			new RequestSelectionHelper((IActionHandlerExtensions)m_cache.ActionHandlerAccessor,
				rootb, ihvoRoot, rgvsli, tagTextProp, cpropPrevious, ich, wsAlt, fAssocPrev,
				selProps);

			// We don't want to continue using the old, out-of-date selection.
			rootb.DestroySelection();
		}

		/// <summary>
		/// If we need to make a selection, but we can't because edits haven't been updated in
		/// the view, this method requests creation of a selection after the unit of work is
		/// complete. It will also scroll the selection into view.
		/// Derived classes should implement this if they have any hope of supporting multi-
		/// paragraph editing.
		/// </summary>
		public override void RequestVisibleSelectionAtEndOfUow(SelectionHelper helper)
		{
			new RequestSelectionByHelper((IActionHandlerExtensions)m_cache.ActionHandlerAccessor, helper);

			// We don't want to continue using the old, out-of-date selection.
			RootBox.DestroySelection();
		}

		/// <summary>
		/// Overridden to fix TE-4146
		/// </summary>
		protected override void OnLayout(LayoutEventArgs levent)
		{
			CallBaseLayout(levent);
		}

		/// <summary>
		/// Lets people call the base implementation of OnLayout()
		/// </summary>
		private void CallBaseLayout(LayoutEventArgs levent)
		{
			base.OnLayout(levent);
		}

		/// <summary>
		/// Make a selection in all of the views that are in a snynced group. This fixes
		/// problems where the user changes the selection in one of the slaves, but the master
		/// is not updated. Thus the view is not scrolled as the groups scroll position only
		/// scrolls the master's selection into view. (TE-3380)
		/// </summary>
		private void HandleSelectionChange(object sender, VwSelectionArgs args)
		{
			var rootb = args.RootBox;
			var vwselNew = args.Selection;
			Debug.Assert(vwselNew != null);
			HandleSelectionChange(rootb, vwselNew);
		}

		/// <summary>
		/// Once we have a cache we can return a sensible list.
		/// </summary>
		protected override int[] GetPossibleWritingSystemsToSelectByInputLanguage(ILgWritingSystemFactory wsf)
		{
			var writingSystems = Cache.ServiceLocator.WritingSystems;
			return writingSystems.CurrentAnalysisWritingSystems
				.Union(writingSystems.CurrentVernacularWritingSystems)
				.Union(writingSystems.CurrentPronunciationWritingSystems)
				.Select(ws => ws.Handle).ToArray();
		}

		/// <summary>
		/// Base method to be extended by subclasses.
		/// </summary>
		protected virtual void HandleSelectionChange(IVwRootBox rootb, IVwSelection vwselNew)
		{
			// To fix FWR-2395, the code to make selections in the slave sites of a group
			// was removed. The original problem that was being fixed by this doesn't seem to
			// apply any longer and the extra selection was causing incorrect updates to the
			// Goto Reference control and the Information Bar in TE. Maybe other things as well.
		}

		/// <summary>
		/// Create a new object, given a text representation (e.g., from the clipboard).
		/// </summary>
		/// <param name="bstrText">Text representation of object</param>
		/// <param name="selDst">Provided for information in case it's needed to generate
		/// the new object (E.g., footnotes might need it to generate the proper sequence
		/// letter)</param>
		/// <param name="kodt">The object data type to use for embedding the new object
		/// </param>
		public override Guid get_MakeObjFromText(string bstrText, IVwSelection selDst, out int kodt)
		{
			return RootSiteEditingHelper.MakeObjFromText(m_cache, bstrText, selDst, out kodt);
		}

		/// <summary>
		/// When the client size changed we have to recalculate the average paragraph height
		/// </summary>
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			if (Visible)
			{
				CalculateAvgParaHeightInPoints();
			}
		}

		/// <summary>
		/// Gets the writing system for the HVO. This could either be the vernacular or
		/// analysis writing system.
		/// </summary>
		public override int GetWritingSystemForHvo(int hvo)
		{
			return m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle;
		}
		#endregion

		#region IHeightEstimator implementation

		/// <summary>
		/// This routine is used to estimate the height of an item in points. The item will be
		/// one of those you have added to the environment using AddLazyItems. The arguments
		/// are as for Display, that is, you are being asked to estimate how much vertical space
		/// is needed to display this item in the available width.
		/// </summary>
		/// <param name="hvo">Item whose height is to be estimated</param>
		/// <param name="frag">Basically indicates what kind of thing the HVO represents (or
		/// else we're in trouble)</param>
		/// <param name="availableWidth"></param>
		/// <returns>The estimated height in points for the specified object</returns>
		public virtual int EstimateHeight(int hvo, int frag, int availableWidth)
		{
			return (int)(10 * Zoom);
		}
		#endregion
	}
}

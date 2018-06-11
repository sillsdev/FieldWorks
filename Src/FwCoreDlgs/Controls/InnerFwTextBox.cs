// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Utils;
using SIL.Reporting;

namespace SIL.FieldWorks.FwCoreDlgs.Controls
{
	/// <summary>
	/// InnerFwTextBox implements the main body of an FwTextBox. Has to be public so combo box
	/// can return its text box.
	/// </summary>
	public class InnerFwTextBox : SimpleRootSite, IVwNotifyChange
	{
		#region Data members

		// This 'view' displays the string m_tssData by pretending it is property ktagText of
		// object khvoRoot.
		internal const int ktagText = 9001; // completely arbitrary, but recognizable.
		const int kfragRoot = 8002; // likewise.
		const int khvoRoot = 7003; // likewise.
		internal const string LineBreak = "\u2028";

		// Neither of these caches are used by LcmCache.
		// They are only used here.
		internal ISilDataAccess m_DataAccess; // Another interface on m_CacheDa.
		TextBoxVc m_vc;

		internal int m_WritingSystem; // Writing system to use when Text is set.
		internal bool m_fUsingTempWsFactory;
		// This stores a value analogous to AutoScrollPosition,
		// but unlike that, it isn't disabled by AutoScroll false.
		Point m_ScrollPosition = new Point(0, 0); // our own scroll position
		internal string m_controlID;
		// true to adjust font height to fix box. When set false, client will normally
		// call PreferredHeight and adjust control size to suit.
		internal bool NotificationsDisabled { get; private set; }

		// Maximum characters allowed.
		// true while we are in Dispose(bool) method
		private bool m_fIsDisposing;
		internal int m_mpEditHeight;

		#endregion Data members

		#region Constructor/destructor
		/// <summary>
		/// Default constructor
		/// </summary>
		public InnerFwTextBox()
		{
			m_DataAccess = new TextBoxDataAccess();
			// Check for the availability of the FwKernel COM DLL.  Too bad we have to catch an
			// exception to make this check...
			if (LicenseManager.UsageMode != LicenseUsageMode.Designtime)
			{
				m_vc = new TextBoxVc(this);
				// So many things blow up so badly if we don't have one of these that I finally decided to just
				// make one, even though it won't always, perhaps not often, be the one we want.
				CreateTempWritingSystemFactory();
				m_DataAccess.WritingSystemFactory = WritingSystemFactory;
			}
			IsTextBox = true;   // range selection not shown when not in focus
		}

		internal bool Rtl => m_vc.m_rtl;

		/// <summary>
		/// Make a writing system factory that is based on the Languages folder (ICU-based).
		/// This is only used in Designer, tests, and momentarily (during construction) in
		/// production, until the client sets supplies a real one.
		/// </summary>
		private void CreateTempWritingSystemFactory()
		{
			m_wsf = FwUtils.CreateWritingSystemManager();
			m_fUsingTempWsFactory = true;
		}

		/// <summary>
		/// The maximum length of text allowed in this context.
		/// </summary>
		public int MaxLength { get; set; } = int.MaxValue;

		/// <summary>
		/// True if this text box prevents the Enter key from inserting a newline.
		/// (This is not normally needed in dialogs since Enter is handled before individual controls see it.)
		/// </summary>
		public bool SuppressEnter { get; set; }

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
				m_fIsDisposing = true;

				// This disposes m_wsf
				ShutDownTempWsFactory();
			}
			// Must happen before call to base.

			base.Dispose(disposing);

			if (disposing)
			{
				m_DataAccess.RemoveNotification(this);
				m_editingHelper?.Dispose();
				m_rootb?.Close();
				m_fIsDisposing = false;
			}
			m_rootb = null;
			m_editingHelper = null;
			m_vc = null;
			m_controlID = null;
			m_DataAccess = null;
			m_wsf = null;
		}

		/// <summary>
		/// true to adjust font height to fix box. When set false, client will normally
		/// call PreferredHeight and adjust control size to suit.
		/// </summary>
		internal bool AdjustStringHeight { get; set; } = true;

		/// <summary>
		/// Shut down the writing system factory and release it explicitly.
		/// </summary>
		private void ShutDownTempWsFactory()
		{
			if (m_fUsingTempWsFactory)
			{
				SingletonsContainer.Get<RenderEngineFactory>().ClearRenderEngines(m_wsf);
				var disposable = m_wsf as IDisposable;
				disposable?.Dispose();
				m_wsf = null;
				m_fUsingTempWsFactory = false;
			}
		}
		#endregion

		#region Properties

		/// <summary>
		/// The desired text alignment. The default depends on the Vc's basic text direction, but the method is
		/// here rather than on the VC to allow it to be overriden in ComboTextBox.
		/// </summary>
		internal virtual FwTextAlign Alignment => Rtl ? FwTextAlign.ktalRight : FwTextAlign.ktalLeft;

		/// <summary>
		/// Indicates whether a text box control automatically wraps words to the beginning of the next line when necessary.
		/// </summary>
		public bool WordWrap { get; set; }

		/// <summary>
		/// For this class, if we haven't been given a WSF we create a default one (based on
		/// the registry). (Note this is kind of overkill, since the constructor does this too.
		/// But I left it here in case we change our minds about the constructor.)
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override ILgWritingSystemFactory WritingSystemFactory
		{
			get
			{
				if (base.WritingSystemFactory == null)
				{
					CreateTempWritingSystemFactory();
				}
				return base.WritingSystemFactory;
			}
			set
			{
				if (base.WritingSystemFactory != value)
				{
					ShutDownTempWsFactory();
					// when the writing system factory changes, delete any string that was there
					// and reconstruct the root box.
					base.WritingSystemFactory = value;
					m_vc.SetWsfAndWs(value, WritingSystemCode);
					// Enhance JohnT: Base class should probably do this.
					if (m_DataAccess != null)
					{
						m_DataAccess.WritingSystemFactory = value;
						m_rootb?.Reconstruct();
					}

				}
			}
		}

		/// <summary>
		/// The writing system that should be used to construct a TsString out of a string in Text.set.
		/// If one has not been supplied use the User interface writing system from the factory.
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int WritingSystemCode
		{
			get
			{
				if (m_WritingSystem == 0)
				{
					m_WritingSystem = WritingSystemFactory.UserWs;
				}
				return m_WritingSystem;
			}
			set
			{
				m_WritingSystem = value;
				m_vc.SetWsfAndWs(WritingSystemFactory, value);
				// If the contents is currently empty, make sure inital typing will occur in this WS.
				// (Unless it is zero, which is not a valid WS...hope it gets changed again if so.)
				if (Tss.Length == 0 && value != 0)
				{
					Tss = TsStringUtils.MakeString("", value);
				}
			}
		}

		/// <summary>
		/// The stylesheet used for the data being displayed.
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override IVwStylesheet StyleSheet
		{
			get
			{
				return base.StyleSheet;
			}
			set
			{
				if (base.StyleSheet != value)
				{
					base.StyleSheet = value;
					m_rootb?.SetRootObject(khvoRoot, m_vc, kfragRoot, value);
				}
			}
		}

		/// <summary>
		/// The real string we are displaying.
		/// </summary>
		/// <value>The TSS.</value>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ITsString Tss
		{
			get
			{
				if (m_DataAccess == null || InDesigner)
				{
					return null;
				}
				if (m_wsf == null)
				{
					m_DataAccess.WritingSystemFactory = WritingSystemFactory;
				}
				var tss = m_DataAccess.get_StringProp(khvoRoot, ktagText);
				// We need to return the TsString without font size because it won't set properly.
				tss = TsStringUtils.RemoveIntProp(tss, (int)FwTextPropType.ktptFontSize);
				// replace line breaks with new lines
				return ReplaceAll(tss, LineBreak, Environment.NewLine);
			}
			set
			{
				// replace new lines with line breaks
				var tss = ReplaceAll(value, Environment.NewLine, LineBreak);
				// Reduce the font size of any run in the new string as necessary to keep the text
				// from being clipped by the height of the box.
				if (tss != null && DoAdjustHeight)
				{
					m_DataAccess.SetString(khvoRoot, ktagText, FontHeightAdjuster.GetAdjustedTsString(tss, m_mpEditHeight, StyleSheet, WritingSystemFactory));
				}
				else
				{
					m_DataAccess.SetString(khvoRoot, ktagText, tss);
				}

				if (m_rootb != null && tss != null)
				{
					var sel = m_rootb.MakeSimpleSel(true, true, true, true);
					ScrollSelectionIntoView(sel, VwScrollSelOpts.kssoDefault);
				}
			}
		}

		private static ITsString ReplaceAll(ITsString tss, string oldSubStr, string newSubStr)
		{
			if (tss == null || tss.Length == 0)
			{
				return tss;
			}

			var tisb = TsStringUtils.MakeIncStrBldr();
			var lines = TsStringUtils.Split(tss, new[] { oldSubStr }, StringSplitOptions.None);
			for (var i = 0; i < lines.Count; i++)
			{
				tisb.AppendTsString(lines[i]);
				if (i < lines.Count - 1 || tss.Text.EndsWith(oldSubStr))
				{
					tisb.Append(newSubStr);
				}
			}
			return tisb.GetString();
		}

		/// <summary>
		/// Because we turn AutoScroll off to suppress the scroll bars, we need our own
		/// private representation of the actual scroll position.
		/// The setter has to behave in the same bizarre way as AutoScrollPosition,
		/// that setting it to (x,y) results in the new value being (-x, -y).
		/// </summary>
		public override Point ScrollPosition
		{
			get
			{
				return AutoScroll ? base.ScrollPosition : m_ScrollPosition;
			}
			set
			{
				if (AutoScroll)
				{
					base.ScrollPosition = value;
				}

				var newPos = new Point(-value.X, -value.Y);
				if (newPos != m_ScrollPosition)
				{
					// Achieve the scroll by just invalidating. For a small window this is fine.
					m_ScrollPosition = newPos;
					Invalidate();
				}
			}
		}

		/// <summary>
		/// The text box in a combo should never autoscroll. Doing so produces LT-11073 among other problems.
		/// </summary>
		protected override bool ScrollToSelectionOnSizeChanged => false;

		/// <summary>
		/// Allows the control to function like an ordinary text box, setting and reading its text.
		/// Generally it is preferred to use the Tss property, giving access to the full
		/// styled string.
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		public override string Text
		{
			get
			{
				if (m_fIsDisposing)
				{
					return string.Empty;
				}

				var tss = Tss;
				if (tss == null)
				{
					return string.Empty;
				}

				return tss.Text ?? string.Empty;
			}
			set
			{
				Tss = TsStringUtils.MakeString(value, WritingSystemCode);
			}
		}

		/// <summary>
		/// Accessor for data access object
		/// </summary>
		internal new ISilDataAccess DataAccess => m_DataAccess;

		/// <summary>
		/// Gets or sets the starting point of text selected in the text box.
		/// JohnT, 8 Aug 2006: contrary to previous behavior, this is now the logically first
		/// character selected, which I think is consistent with TextBox, not necessarily the
		/// anchor.
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int SelectionStart
		{
			get
			{
				if (RootBox == null || InDesigner)
				{
					return 0;
				}
				var sel = RootBox.Selection;
				if (sel == null)
				{
					return 0;
				}

				ITsString tss;
				int ichAnchor;
				bool fAssocPrev;
				int hvoObj;
				int tag;
				int ws;
				int ichEnd;
				sel.TextSelInfo(true, out tss, out ichEnd, out fAssocPrev, out hvoObj, out tag, out ws);
				sel.TextSelInfo(false, out tss, out ichAnchor, out fAssocPrev, out hvoObj, out tag, out ws);
				return IntToExtIndex(Math.Min(ichAnchor, ichEnd));
			}
			set
			{
				if (value < 0)
				{
					throw new ArgumentException("The value cannot be less than zero.");
				}
				Select(value, SelectionLength);
			}
		}

		/// <summary>
		/// Gets or sets the number of characters selected in the text box.
		/// (JohnT, 8 Aug 2006: contrary to the previous implementation, this is now always a
		/// positive number...or zero...never negative, irrespective of the relative positions
		/// of the anchor and endpoint.)
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public virtual int SelectionLength
		{
			get
			{
				if (RootBox == null || InDesigner)
				{
					return 0;
				}
				var sel = RootBox.Selection;
				if (sel == null)
				{
					return 0;
				}

				ITsString tss;
				int ichEnd;
				bool fAssocPrev;
				int hvoObj;
				int tag;
				int ws;
				int ichAnchor;
				sel.TextSelInfo(true, out tss, out ichEnd, out fAssocPrev, out hvoObj, out tag, out ws);
				sel.TextSelInfo(false, out tss, out ichAnchor, out fAssocPrev, out hvoObj, out tag, out ws);
				return Math.Abs(IntToExtIndex(ichAnchor) - IntToExtIndex(ichEnd));
			}
			set
			{
				if (value < 0)
				{
					throw new ArgumentException("The value cannot be less than zero.");
				}
				Select(SelectionStart, value);
			}
		}

		/// <summary>
		/// Because InnerFwTextBox is an embedded control <code>InDesigner</code> does not return usable information.
		/// </summary>
		protected internal bool InDesigner { get; set; }

		/// <summary>
		/// Gets or sets the selected text.
		/// </summary>
		/// <value>The selected text.</value>
		[Browsable(false)]
		public string SelectedText
		{
			get
			{
				return SelectedTss?.Text ?? string.Empty;
			}

			set
			{
				SelectedTss = TsStringUtils.MakeString(value, WritingSystemCode);
			}
		}

		/// <summary>
		/// Gets or sets the selected TSS.
		/// </summary>
		[Browsable(false)]
		public ITsString SelectedTss
		{
			get
			{
				return Tss?.Substring(SelectionStart, SelectionLength);
			}

			set
			{
				var selStart = SelectionStart;
				Tss = Tss.Replace(selStart, SelectionLength, value);
				Select(selStart + value.Length, 0);
			}
		}

		#endregion

		#region Overridden rootsite methods

		/// <summary>
		/// Creates a special extended editing helper for this text box.
		/// </summary>
		protected override EditingHelper CreateEditingHelper()
		{
			return new TextBoxEditingHelper(this);
		}

		/// <summary>
		/// Simulate infinite width if needed.
		/// </summary>
		public override int GetAvailWidth(IVwRootBox prootb)
		{
			if (WordWrap)
			{
				return base.GetAvailWidth(prootb);
			}

			// Displaying Right-To-Left Graphite behaves badly if available width gets up to
			// one billion (10**9) or so.  See LT-6077.  One million (10**6) should be ample
			// for simulating infinite width.
			return 1000000;
		}

		/// <summary>
		/// Create the root box and initialize it. We want this one to work even in design mode, and since
		/// we supply the cache and data ourselves, that's possible.
		/// </summary>
		public override void MakeRoot()
		{
			if (InDesigner)
			{
				return;
			}

			base.MakeRoot();

			m_rootb.DataAccess = m_DataAccess;
			m_rootb.SetRootObject(khvoRoot, m_vc, kfragRoot, StyleSheet);
			m_dxdLayoutWidth = kForceLayout; // Don't try to draw until we get OnSize and do layout.
			m_DataAccess.AddNotification(this);
		}

		/// <summary>
		/// Overridden property to indicate that this control will handle horizontal scrolling
		/// </summary>
		protected override bool DoAutoHScroll => true;

		/// <summary>
		/// Returns the selection that should be made visible if null is passed to
		/// MakeSelectionVisible. FwTextBox overrides to pass a selection that is the whole
		/// range, if nothing is selected.
		/// </summary>
		protected override IVwSelection SelectionToMakeVisible
		{
			get
			{
				if (m_rootb == null)
				{
					return null;
				}
				return m_rootb.Selection ?? m_rootb.MakeSimpleSel(true, false, true, false);
			}
		}

		internal void BaseMakeSelectionVisible(IVwSelection sel)
		{
			base.MakeSelectionVisible(sel, true);
		}

		/// <summary>
		/// First try to make everything visible, if possible. This is especially helpful with
		/// RTL text.
		/// </summary>
		protected override bool MakeSelectionVisible(IVwSelection sel, bool fWantOneLineSpace)
		{
			// Select everything (but don't install it).
			var wholeSel = m_rootb.MakeSimpleSel(true, false, true, false);
			if (wholeSel != null)
			{
				Rectangle rcPrimary;
				using (new HoldGraphics(this))
				{
					bool fEndBeforeAnchor;
					SelectionRectangle(wholeSel, out rcPrimary, out fEndBeforeAnchor);
				}

				if (rcPrimary.Width < ClientRectangle.Width - HorizMargin * 2)
				{
					return base.MakeSelectionVisible(wholeSel, false);
				}
			}
			else
			{
				if (sel == null || !sel.IsValid)
				{
					Logger.WriteError(new InvalidOperationException("MakeSelectionVisible called when no valid selection could be made."));
					Logger.WriteEvent("m_rootb.Height = " + m_rootb.Height);
					Logger.WriteEvent("m_rootb.Width = " + m_rootb.Width);
					int hvoRoot, frag;
					IVwViewConstructor vc;
					IVwStylesheet stylesheet;
					m_rootb.GetRootObject(out hvoRoot, out vc, out frag, out stylesheet);
					Logger.WriteEvent("Root object = " + hvoRoot);
					Logger.WriteEvent("Root fragment = " + frag);
				}
				Debug.Fail("Unable to make a simple selection in rootbox.");
			}
			// And, in case it really is longer than the box (or was null!), make sure we really can see the actual selection;
			// but ONLY if we had one, otherwise, it tries again to make everything visible
			if (m_rootb.Selection != null && m_rootb.Selection.IsValid)
			{
				return base.MakeSelectionVisible(sel, fWantOneLineSpace);
			}
			return false;
		}

		#endregion

		#region Other methods

		/// <summary>
		/// Converts the internal string index to the external string index. They might be different because a line break character
		/// is used internally to represent a new line and CRLF is used externally on Windows to represent a new line.
		/// </summary>
		/// <param name="intIndex">The internal string index.</param>
		/// <returns>The external string index.</returns>
		protected int IntToExtIndex(int intIndex)
		{
			if (Environment.NewLine.Length == LineBreak.Length)
			{
				return intIndex;
			}
			var tss = m_DataAccess.get_StringProp(khvoRoot, ktagText);
			var hardBreakCount = SubstringCount(tss == null || tss.Length == 0 ? string.Empty : tss.Text.Substring(0, intIndex), LineBreak);
			return intIndex + (hardBreakCount * (Environment.NewLine.Length - LineBreak.Length));
		}

		/// <summary>
		/// Converts the external string index to the internal string index. They might be different because a line break character
		/// is used internally to represent a new line and CRLF is used externally on Windows to represent a new line.
		/// </summary>
		/// <param name="extIndex">The external string index.</param>
		/// <returns>The internal string index.</returns>
		protected int ExtToIntIndex(int extIndex)
		{
			if (Environment.NewLine.Length == LineBreak.Length)
			{
				return extIndex;
			}
			var tss = Tss;
			if (tss == null || tss.Length == 0)
			{
				return extIndex; // empty string, no newlines to adjust for
			}
			// Count the newlines before extIndex.
			var content = tss.Text;
			if (extIndex < content.Length) // allow for the possibility, which should be unlikely, that index is past the end.
			{
				content = content.Substring(extIndex);
			}
			var newLineCount = SubstringCount(content, Environment.NewLine);

			return extIndex + (newLineCount * (LineBreak.Length - Environment.NewLine.Length));
		}

		private static int SubstringCount(string str, string substr)
		{
			var num = 0;
			var pos = -1;
			while ((pos = str.IndexOf(substr, pos + 1)) > 0)
			{
				num++;
			}
			return num;
		}

		/// <summary>
		/// Gets or sets the background color for the control.
		/// </summary>
		public override Color BackColor
		{
			get
			{
				return base.BackColor;
			}
			set
			{
				base.BackColor = value;
				m_rootb?.Reconstruct();
			}
		}

		/// <summary>
		/// Gets or sets the foreground color of the control.
		/// </summary>
		public override Color ForeColor
		{
			get
			{
				return base.ForeColor;
			}
			set
			{
				base.ForeColor = value;
				m_rootb?.Reconstruct();
			}
		}

		/// <summary>
		/// Gets a rectangle that encloses the text.
		/// </summary>
		/// <remarks>
		/// The width is likely to be bogus for a multiline textbox, but the height should be
		/// okay regardless.
		/// </remarks>
		public Rectangle TextRect
		{
			get
			{
				var rect = new Rectangle();
				var sel = RootBox.MakeSimpleSel(true, false, false, false);
				using (new HoldGraphics(this))
				{
					Rectangle rcSrcRoot, rcDstRoot;
					Rect rcSec, rcPrimary;
					bool fSplit, fEndBeforeAnchor;
					GetCoordRects(out rcSrcRoot, out rcDstRoot);
					sel.Location(m_graphicsManager.VwGraphics, rcSrcRoot, rcDstRoot, out rcPrimary, out rcSec, out fSplit, out fEndBeforeAnchor);

					rect.X = rcPrimary.left;
					rect.Y = rcPrimary.top;

					sel = RootBox.MakeSimpleSel(false, false, false, false);
					sel.Location(m_graphicsManager.VwGraphics, rcSrcRoot, rcDstRoot, out rcPrimary, out rcSec, out fSplit, out fEndBeforeAnchor);

					rect.Width = rcPrimary.right - rect.X;
					rect.Height = RootBox.Height;
				}
				return rect;
			}
		}

		/// <summary>
		/// Selects a range of text in the text box.
		/// </summary>
		/// <param name="start">The position of the first character in the current text selection within the text box.</param>
		/// <param name="length">The number of characters to select. </param>
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
		public void Select(int start, int length)
		{
			if (start < 0)
			{
				throw new ArgumentException("Starting position is less than zero.", nameof(start));
			}

			if (length < 0)
			{
				throw new ArgumentException("Length is less than zero.", nameof(length));
			}
			if (m_rootb == null)
			{
				return;
			}

			var intStart = ExtToIntIndex(start);
			var intEnd = ExtToIntIndex(start + length);
			var sel = m_rootb.Selection;
			if (sel != null)
			{
				// See if the desired thing is already selected. If so do nothing. This can prevent stack overflow!
				ITsString tssDummy;
				int ichAnchor, ichEnd, hvo, tag, ws;
				bool fAssocPrev;
				sel.TextSelInfo(true, out tssDummy, out ichEnd, out fAssocPrev, out hvo, out tag, out ws);
				sel.TextSelInfo(false, out tssDummy, out ichAnchor, out fAssocPrev, out hvo, out tag, out ws);
				if (Math.Min(ichAnchor, ichEnd) == intStart && Math.Max(ichAnchor, ichEnd) == intEnd)
				{
					return;
				}
			}
			try
			{
				m_rootb.MakeTextSelection(0, 0, null, ktagText, 0, intStart, intEnd, -1, false, -1, null, true);
			}
			catch
			{
			}
		}

		/// <summary>
		/// The height the box would like to be to neatly display its current data.
		/// </summary>
		/// <remarks>
		/// TextHeight doesn't always return the exact height (font height is specified in points, which
		/// when converted to pixels can often get rounded down), so we add one extra pixel
		/// to be sure there is enough room to fit the text properly so that even if AdjustHeight is
		/// set to true, it will not have to adjust the font size to fit.
		/// Note that if WordWrap is true, multiple lines are expected, and if it is false,
		/// only one line is expected (which is what TextHeight assumes).
		/// </remarks>
		public int PreferredHeight => WordWrap ? TextRect.Height + 1 : TextHeight + 1;

		/// <summary>
		/// Gets the internal height of the text.
		/// </summary>
		public int TextHeight
		{
			get
			{
				if (m_wsf == null)
				{
					throw new Exception("A text box is being asked for its height, but its writing system factory has not been set.");
				}

				try
				{
					IVwCacheDa cda = VwCacheDaClass.Create();
					cda.TsStrFactory = TsStringUtils.TsStrFactory;
					var sda = (ISilDataAccess)cda;
					sda.WritingSystemFactory = WritingSystemFactory;
					sda.SetString(khvoRoot, ktagText, FontHeightAdjuster.GetUnadjustedTsString(Tss));
					IVwRootBox rootb = VwRootBoxClass.Create();
					rootb.RenderEngineFactory = SingletonsContainer.Get<RenderEngineFactory>();
					rootb.TsStrFactory = TsStringUtils.TsStrFactory;
					rootb.SetSite(this);
					rootb.DataAccess = sda;
					rootb.SetRootObject(khvoRoot, m_vc, kfragRoot, StyleSheet);
					using (new HoldGraphics(this))
					{
						rootb.Layout(m_graphicsManager.VwGraphics, GetAvailWidth(rootb));
					}
					var dy = rootb.Height;
					rootb.Close(); // MUST close root boxes (see LT-5345)!
					return dy;
				}
				catch (Exception e)
				{
					throw new Exception("Failed to compute the height of an FwTextBox, though it has a writing system factory", e);
				}
			}
		}

		/// <summary>
		/// Return the simple width (plus a small fudge factor) of the current string in screen units.
		/// </summary>
		public int PreferredWidth
		{
			get
			{
				if (WordWrap && RootBox != null)
				{
					return GetAvailWidth(RootBox) + 4;
				}
				var fOldSaveSize = m_vc.SaveSize;
				try
				{
					m_vc.SaveSize = true;
					IVwCacheDa cda = VwCacheDaClass.Create();
					cda.TsStrFactory = TsStringUtils.TsStrFactory;
					var sda = (ISilDataAccess)cda;
					sda.WritingSystemFactory = WritingSystemFactory;
					sda.SetString(khvoRoot, ktagText, FontHeightAdjuster.GetUnadjustedTsString(Tss));
					IVwRootBox rootb = VwRootBoxClass.Create();
					rootb.RenderEngineFactory = SingletonsContainer.Get<RenderEngineFactory>();
					rootb.TsStrFactory = TsStringUtils.TsStrFactory;
					rootb.SetSite(this);
					rootb.DataAccess = sda;
					rootb.SetRootObject(khvoRoot, m_vc, kfragRoot, StyleSheet);
					int dx;
					using (new HoldGraphics(this))
					{
						rootb.Layout(m_graphicsManager.VwGraphics, GetAvailWidth(rootb));
						var dpx = m_graphicsManager.VwGraphics.XUnitsPerInch;
						dx = (m_vc.PreferredWidth * dpx) / 72000;
					}
					rootb.Close();
					return dx + 8;
				}
				finally
				{
					m_vc.SaveSize = fOldSaveSize;
				}
			}
		}

		/// <summary>
		/// show default cursor for read-only text boxes.
		/// </summary>
		protected override void OnMouseMoveSetCursor(Point mousePos)
		{
			if (ReadOnlyView)
			{
				Cursor = Cursors.Default;
			}
			else
			{
				base.OnMouseMoveSetCursor(mousePos);
			}
		}

		/// <summary>
		/// Gets a value indicating whether to attempt to adjust the height of the string
		/// in the textbox to fit the height of the textbox.
		/// </summary>
		protected bool DoAdjustHeight => LicenseManager.UsageMode != LicenseUsageMode.Designtime && AdjustStringHeight && WritingSystemFactory != null && !AutoScroll;

		/// <summary>
		/// Adjusts text height after a style change.
		/// </summary>
		internal bool AdjustHeight()
		{
			if (!DoAdjustHeight || m_DataAccess == null || Tss == null)
			{
				return false;
			}

			// Reduce the font size of any run in the new string as necessary to keep the text
			// from being clipped by the height of the box.
			// ENHANCE: Consider having GetAdjustedTsString return a value to tell whether any
			// adjustments were made. If not, we don't need to call SetString.
			RemoveNonRootNotifications(); // want to suppress any side effects and just update the view.
			try
			{
				m_DataAccess.SetString(khvoRoot, ktagText, FontHeightAdjuster.GetAdjustedTsString(FontHeightAdjuster.GetUnadjustedTsString(Tss), m_mpEditHeight, StyleSheet, WritingSystemFactory));

			}
			finally
			{
				RestoreNonRootNotifications();
			}
			return true;
		}

		internal virtual void RestoreNonRootNotifications()
		{
			if (m_rootb != null)
			{
				DataAccess.AddNotification(this);
			}
			NotificationsDisabled = false;
		}

		internal virtual void RemoveNonRootNotifications()
		{
			DataAccess.RemoveNotification(this);
			NotificationsDisabled = true;
		}
		#endregion

		#region Event-handling methods

		/// <summary>
		/// When the edit box gets resized, recalc the maximum line height (when setting a Tss
		/// string, applying styles, or setting the WS, we need to reduce the font size if
		/// necessary to keep the text from being clipped by the height of the box).
		/// </summary>
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			if (m_DataAccess == null)
			{
				return;
			}

			m_mpEditHeight = FwTextBox.GetDympMaxHeight(this);
			if (AdjustHeight())
			{
				m_rootb.Reconstruct();
			}
			// Don't bother making selection visible until our writing system is set, or the
			// string has something in it.  See LT-9472.
			// Also don't try if we have no selection; this can produce undesirable scrolling when the
			// window is just too narrow. LT-11073
			if (m_rootb == null || m_rootb.Selection == null)
			{
				return;
			}
			var tss = Tss;
			if (m_WritingSystem != 0 || (tss != null && tss.Text != null))
			{
				MakeSelectionVisible(null);
			}
		}

		/// <summary>
		/// watch for keys to do the cut/copy/paste operations
		/// </summary>
		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (Parent is FwTextBox)
			{
				((FwTextBox)Parent).HandleKeyDown(e);
				if (e.Handled)
				{
					return;
				}
			}
			if (!EditingHelper.HandleOnKeyDown(e))
			{
				base.OnKeyDown(e);
			}
		}

		/// <summary>
		/// Override to suppress Enter key if it is not allowed.
		/// </summary>
		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			if (SuppressEnter && e.KeyChar == '\r')
			{
				return;
			}
			base.OnKeyPress(e);
		}
		#endregion

		#region Methods for applying styles and writing systems

		/// <summary>
		/// Applies the specified writing system to the current selection
		/// </summary>
		public void ApplyWS(int hvoWs)
		{
			EditingHelper.ApplyWritingSystem(hvoWs);

			AdjustHeight();
		}
		#endregion

		#region IVwNotifyChange Members

		/// <summary>
		/// Any change to this private data access must be a change to our string, so check its length.
		/// </summary>
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			var tssValue = Tss;
			if (tssValue != null && tssValue.Length > MaxLength)
			{
				MessageBox.Show(this, string.Format(FwCoreDlgs.ksStringTooLong, MaxLength), FwCoreDlgs.ksWarning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				var bldr = tssValue.GetBldr();
				bldr.ReplaceTsString(MaxLength, bldr.Length, null);
				Tss = bldr.GetString();
			}
		}

		#endregion

		/// <summary />
		private sealed class TextBoxEditingHelper : EditingHelper
		{
			private InnerFwTextBox m_innerFwTextBox;

			/// <summary>
			/// Initializes a new instance of the <see cref="TextBoxEditingHelper"/> class.
			/// </summary>
			public TextBoxEditingHelper(InnerFwTextBox innerFwTextBox) :
				base(innerFwTextBox)
			{
				m_innerFwTextBox = innerFwTextBox;
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
				Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");
				// Must not be run more than once.
				if (IsDisposed)
				{
					return;
				}

				if (disposing)
				{
					// Dispose managed resources here.
				}

				// Dispose unmanaged resources here, whether disposing is true or false.
				m_innerFwTextBox = null;

				base.Dispose(disposing);
			}

			#endregion IDisposable override

			/// <summary>
			/// Applies the specified style to the current selection
			/// </summary>
			public override void ApplyStyle(string sStyle)
			{
				base.ApplyStyle(sStyle);

				// Give text box a chance to adjust height of text to fit box.
				m_innerFwTextBox.AdjustHeight();
			}

			protected override void OnCharAux(string stuInput, VwShiftStatus ss, Keys modifiers)
			{
				if ((modifiers & Keys.Alt) != Keys.Alt && stuInput == "\r")
				{
					stuInput = InnerFwTextBox.LineBreak;
				}
				base.OnCharAux(stuInput, ss, modifiers);
			}

			/// <summary>
			/// Text boxes should only ever use the destination writing system.
			/// </summary>
			/// <param name="wsf">writing system factory containing the writing systems in the
			/// pasted ITsString</param>
			/// <param name="destWs">[out] The destination writing system.</param>
			/// <returns>an indication of how the paste should be handled.</returns>
			public override PasteStatus DeterminePasteWs(ILgWritingSystemFactory wsf, out int destWs)
			{
				destWs = m_innerFwTextBox.WritingSystemCode;
				return PasteStatus.UseDestWs;
			}
		}

		private sealed class TextBoxVc : FwBaseVc
		{
			#region Data members
			internal bool m_rtl;
			private int m_dxWidth;
#pragma warning disable 414
			private int m_dyHeight;
#pragma warning restore 414
			private readonly InnerFwTextBox m_innerTextBox;
			#endregion

			/// <summary>
			/// Construct one. Must be part of an InnerFwTextBox.
			/// </summary>
			internal TextBoxVc(InnerFwTextBox innerTextBox)
			{
				m_innerTextBox = innerTextBox;
			}

			/// <summary>
			/// Gets or sets a value indicating whether to save the size.
			/// </summary>
			internal bool SaveSize { get; set; }

			/// <summary>
			/// Return the simple width of the current string in millipoints.
			/// </summary>
			internal int PreferredWidth => m_dxWidth;

			public override ITsTextProps UpdateRootBoxTextProps(ITsTextProps ttp)
			{
				var propsBldr = ttp.GetBldr();
				propsBldr.SetIntPropValues((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault, (int)RGB(m_innerTextBox.BackColor));

				using (var graphics = m_innerTextBox.CreateGraphics())
				{
					propsBldr.SetIntPropValues((int)FwTextPropType.ktptPadTop, (int)FwTextPropVar.ktpvMilliPoint, m_innerTextBox.Padding.Top * 72000 / (int)graphics.DpiY);
					propsBldr.SetIntPropValues((int)FwTextPropType.ktptPadBottom, (int)FwTextPropVar.ktpvMilliPoint, m_innerTextBox.Padding.Bottom * 72000 / (int)graphics.DpiY);
					propsBldr.SetIntPropValues((int)FwTextPropType.ktptPadLeading, (int)FwTextPropVar.ktpvMilliPoint, (m_rtl ? m_innerTextBox.Padding.Right : m_innerTextBox.Padding.Left) * 72000 / (int)graphics.DpiX);
					propsBldr.SetIntPropValues((int)FwTextPropType.ktptPadTrailing, (int)FwTextPropVar.ktpvMilliPoint, (m_rtl ? m_innerTextBox.Padding.Left : m_innerTextBox.Padding.Right) * 72000 / (int)graphics.DpiX);
				}

				return propsBldr.GetTextProps();
			}

			/// <summary>
			/// The main method just displays the text with the appropriate properties.
			/// </summary>
			public override void Display(IVwEnv vwenv, int hvo, int frag)
			{
				vwenv.set_IntProperty((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, (int)RGB(m_innerTextBox.ForeColor));

				if (m_rtl)
				{
					vwenv.set_IntProperty((int)FwTextPropType.ktptRightToLeft, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
				}
				vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, (int)m_innerTextBox.Alignment);

				vwenv.OpenParagraph();
				vwenv.AddStringProp(ktagText, this);
				if (SaveSize)
				{
					var tss = vwenv.DataAccess.get_StringProp(hvo, ktagText);
					vwenv.get_StringWidth(tss, null, out m_dxWidth, out m_dyHeight);
				}
				vwenv.CloseParagraph();
			}

			/// <summary>
			/// Sets the writing system factory and the writing system hvo.
			/// </summary>
			public void SetWsfAndWs(ILgWritingSystemFactory wsf, int ws)
			{
				Debug.Assert(wsf != null, "Set the WritingSystemFactory first!");
				var wsObj = wsf.get_EngineOrNull(ws);
				m_rtl = (wsObj != null && wsObj.RightToLeftScript);
			}

			/// <summary>
			///  Convert a .NET color to the type understood by Views code and other Win32 stuff.
			/// </summary>
			public static uint RGB(Color c)
			{
				return c == Color.Transparent ? 0xC0000000 : RGB(c.R, c.G, c.B);
			}

			/// <summary>
			/// Make a standard Win32 color from three components.
			/// </summary>
			public static uint RGB(int r, int g, int b)
			{
				return (uint)((byte)r | ((byte)g << 8) | ((byte)b << 16));
			}
		}
	}
}
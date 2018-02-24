// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace SIL.FieldWorks.Common.Widgets
{
	/// <summary>
	/// InnerFwListBox implements the main body of an FwListBox.
	/// </summary>
	internal class InnerFwListBox : SimpleRootSite
	{
		// This 'view' displays the strings representing the list items by representing
		// each string as property ktagText of one of the objects of ktagItems of
		// object khvoRoot. In addition, for each item we display a long string of blanks,
		// so we can make the selection highlight go the full width of the window.
		protected internal const int ktagText = 9001; // completely arbitrary, but recognizable.
		protected internal const int ktagItems = 9002;
		protected internal const int kfragRoot = 8002; // likewise.
		protected internal const int kfragItems = 8003;
		protected internal const int khvoRoot = 7003; // likewise.
		protected internal const int kclsItem = 5007;
		// Our own cache, so we need to get rid of it.
		protected IVwCacheDa m_cacheDa; // Main cache object

		protected int m_writingSystem; // Writing system to use when Text is set.

		// Set this false to (usually temporarily) disable changing the background color
		// for the selected item. This allows us to get an accurate figure for the overall
		// width of the view.
		bool m_fShowHighlight = true;

		/// <summary>
		/// Constructor
		/// </summary>
		internal InnerFwListBox(FwListBox owner)
		{
			Owner = owner;
			m_cacheDa = VwCacheDaClass.Create();
			m_cacheDa.TsStrFactory = TsStringUtils.TsStrFactory;
			DataAccess = (ISilDataAccess)m_cacheDa;
			// So many things blow up so badly if we don't have one of these that I finally decided to just
			// make one, even though it won't always, perhaps not often, be the one we want.
			m_wsf = FwUtils.FwUtils.CreateWritingSystemManager();
			DataAccess.WritingSystemFactory = WritingSystemFactory;
			VScroll = true;
			AutoScroll = true;
		}

		protected internal new ISilDataAccess DataAccess { get; protected set; }

		/// <summary>
		/// The writing system that should be used to construct a TsString out of a string in Text.set.
		/// If one has not been supplied use the User interface writing system from the factory.
		/// </summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int WritingSystemCode
		{
			get
			{
				if (m_writingSystem == 0)
				{
					m_writingSystem = WritingSystemFactory.UserWs;
				}
				return m_writingSystem;
			}
			set
			{
				m_writingSystem = value;
			}
		}

		/// <summary>
		/// Gets or sets the view constructor.
		/// </summary>
		private ListBoxVc ViewConstructor { get; set; }

		public bool ShowHighlight
		{
			get
			{
				return m_fShowHighlight;
			}
			set
			{
				if (value == m_fShowHighlight)
				{
					return;
				}
				m_fShowHighlight = value;
				RootBox?.Reconstruct();
			}
		}


		public override int GetAvailWidth(IVwRootBox prootb)
		{
			// Simulate infinite width. I (JohnT) think the / 2 is a good idea to prevent overflow
			// if the view code at some point adds a little bit to it.
			// return Int32.MaxValue / 2;
			// Displaying Right-To-Left Graphite behaves badly if available width gets up to
			// one billion (10**9) or so.  See LT-6077.  One million (10**6) should be ample
			// for simulating infinite width.
			return 1000000;
		}

		/// <summary>
		/// For this class, if we haven't been given a WSF we create a default one (based on
		/// the registry). (Note this is kind of overkill, since the constructor does this too.
		/// But I left it here in case we change our minds about the constructor.)
		/// </summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override ILgWritingSystemFactory WritingSystemFactory
		{
			get
			{
				if (m_wsf == null)
				{
					m_wsf = FwUtils.FwUtils.CreateWritingSystemManager();
				}
				return base.WritingSystemFactory;
			}
			set
			{
				if (m_wsf != value)
				{
					base.WritingSystemFactory = value;
					// Enhance JohnT: this should probably be done by the base class.
					DataAccess.WritingSystemFactory = value;
					m_writingSystem = 0; // gets reloaded if used.
					m_rootb?.Reconstruct();
				}
			}
		}

		protected internal FwListBox Owner { get; protected set; }

		/// <summary>
		/// Create the root box and initialize it.
		/// </summary>
		public override void MakeRoot()
		{
			if (DesignMode)
			{
				return;
			}

			base.MakeRoot();

			m_rootb.DataAccess = DataAccess;
			if (ViewConstructor == null)
			{
				ViewConstructor = new ListBoxVc(this);
			}
			m_rootb.SetRootObject(khvoRoot, ViewConstructor, kfragRoot, m_styleSheet);
			m_dxdLayoutWidth = kForceLayout; // Don't try to draw until we get OnSize and do layout.
			EditingHelper.DefaultCursor = Cursors.Arrow;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
				// Cleanup managed stuff here.
				m_cacheDa?.ClearAllData();
			}
			// Cleanup unmanaged stuff here.
			DataAccess = null;
			if (m_cacheDa != null)
			{
				if (Marshal.IsComObject(m_cacheDa))
				{
					Marshal.ReleaseComObject(m_cacheDa);
				}
				m_cacheDa = null;
			}
			Owner = null; // It will get disposed on its own, if it hasn't been already.
			ViewConstructor = null;

			base.Dispose(disposing);
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			if (Visible && e.Button == MouseButtons.Left)
			{
				base.OnMouseUp (e);
				if (Owner.SelectedIndex == Owner.HighlightedIndex)
				{
					Owner.RaiseSameItemSelected();
				}
				else
				{
					Owner.SelectedIndex = Owner.HighlightedIndex;
				}
			}
		}

		protected void HighlightFromMouse(Point pt, Rectangle rcSrcRoot, Rectangle rcDstRoot)
		{
			// If we don't have any items, we certainly can't highlight them!
			if (Owner.Items.Count == 0)
			{
				return;
			}
			var sel = m_rootb.MakeSelAt(pt.X, pt.Y, new Rect(rcSrcRoot.Left, rcSrcRoot.Top, rcSrcRoot.Right, rcSrcRoot.Bottom),
				new Rect(rcDstRoot.Left, rcDstRoot.Top, rcDstRoot.Right, rcDstRoot.Bottom), false);
			if (sel == null)
			{
				return; // or set selected index to -1?
			}
			int index;
			int dummyHvo, dummyTag, dummyPrev;
			IVwPropertyStore vpsDummy;
			// Level 0 would give info about ktagText and the hvo of the dummy line object.
			// Level 1 gives info about which line object it is in the root.
			sel.PropInfo(false, 1, out dummyHvo, out dummyTag, out index, out dummyPrev, out vpsDummy);
			Debug.Assert(index < Owner.Items.Count && index >= 0);
			// We are getting an out-of-bounds crash in setting HighlightedIndex at times,
			// for no apparent reason (after fixing the display bug of FWNX-803).
			if (index >= 0 && index < Owner.Items.Count)
			{
				Owner.HighlightedIndex = index;
			}
		}

		/// <summary>
		/// While tracking, we move the highlight as the mouse moves.
		/// </summary>
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove (e);
			if (!Owner.Tracking)
			{
				return;
			}
			using(new HoldGraphics(this))
			{
				Rectangle rcSrcRoot;
				Rectangle rcDstRoot;
				GetCoordRects(out rcSrcRoot, out rcDstRoot);
				var pt = new Point(e.X, e.Y);
				HighlightFromMouse(PixelToView(pt), rcSrcRoot, rcDstRoot);
			}
		}

		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			if (!char.IsControl(e.KeyChar))
			{
				if (Owner is ComboListBox)
				{
					// Highlight list item based upon first character
					((ComboListBox)Owner).HighlightItemStartingWith(e.KeyChar.ToString()); // closes the box
					e.Handled = true;
				}
			}
			else if (e.KeyChar == '\r' || e.KeyChar == '\t')
			{
				// If we're in a ComboBox, we must handle the ENTER key here, otherwise
				// SimpleRootSite may handle it inadvertently forcing the parent dialog to close (cf. LT-2280).
				HandleListItemSelect();

				if (e.KeyChar == '\r')
				{
					e.Handled = true;
				}
			}

			base.OnKeyPress(e);
		}

		private void HandleListItemSelect()
		{
			if (Owner.HighlightedIndex >= 0)
			{
				if (Owner.SelectedIndex == Owner.HighlightedIndex)
				{
					Owner.RaiseSameItemSelected();
				}
				else
				{
					Owner.SelectedIndex = Owner.HighlightedIndex;
				}
			}
			// if the user didn't highlight an item, treat this as we would selecting
			// the same item we did before.
			Owner.RaiseSameItemSelected();
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown (e);
			switch (e.KeyCode)
			{
				case Keys.Right:
				case Keys.Down:
				{
					// Handle Alt-Down
					if (e.Alt && e.KeyCode == Keys.Down && Owner is ComboListBox)
					{
						HandleListItemSelect();
						e.Handled = true;
					}
					else
					{
						// If we don't have any items, we certainly can't highlight them!
						if (Owner.Items.Count == 0)
						{
							return;
						}
						// don't increment if already at the end
						if (Owner.HighlightedIndex < Owner.Items.Count - 1)
						{
							Owner.HighlightedIndex += 1;
						}
					}
					break;
				}
				case Keys.Left:
				case Keys.Up:
				{
					// Handle Alt-Up
					if (e.Alt && e.KeyCode == Keys.Up && Owner is ComboListBox)
					{
						HandleListItemSelect();
						e.Handled = true;
					}
					else
					{
						// If we don't have any items, we certainly can't highlight them!
						if (Owner.Items.Count == 0)
						{
							return;
						}

						// don't scroll up past first item
						if (Owner.HighlightedIndex > 0)
						{
							Owner.HighlightedIndex -= 1;
						}
						else if (Owner.HighlightedIndex < 0)
						{
							Owner.HighlightedIndex = 0;	// reset to first item.
						}
					}
					break;
				}
			}
		}

		public bool IsHighlighted(int index)
		{
			return Owner.IsHighlighted(index);
		}


		private sealed class ListBoxVc : FwBaseVc
		{
			private InnerFwListBox m_listbox;

			/// <summary>
			/// Construct one. Must be part of an InnerFwListBox.
			/// </summary>
			internal ListBoxVc(InnerFwListBox listbox)
			{
				m_listbox = listbox;
			}

			/// <summary>
			/// The main method just displays the text with the appropriate properties.
			/// </summary>
			/// <param name="vwenv">The view environment</param>
			/// <param name="hvo">The HVo of the object to display</param>
			/// <param name="frag">The fragment to lay out</param>
			public override void Display(IVwEnv vwenv, int hvo, int frag)
			{
				switch (frag)
				{
					case kfragRoot:
						var f = m_listbox.Font;
						if (m_listbox.StyleSheet == null)
						{
							// Try to get items a reasonable size based on whatever font has been set for the
							// combo as a whole. We don't want to do this if a stylesheet has been set, because
							// it will override the sizes specified in the stylesheet.
							// Enhance JohnT: there are several more properties we could readily copy over
							// from the font, but this is a good start.
							vwenv.set_IntProperty((int)FwTextPropType.ktptFontSize, (int)FwTextPropVar.ktpvMilliPoint, (int)(f.SizeInPoints * 1000));
						}
						vwenv.set_IntProperty((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(m_listbox.ForeColor));
						DisplayList(vwenv);
						break;
					case kfragItems:
						int index, hvoDummy, tagDummy;
						var clev = vwenv.EmbeddingLevel;
						vwenv.GetOuterObject(clev - 1, out hvoDummy, out tagDummy, out index);
						var fHighlighted = m_listbox.IsHighlighted(index);
						if (fHighlighted && m_listbox.ShowHighlight)
						{
							vwenv.set_IntProperty((int)FwTextPropType.ktptForeColor, (int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(Color.FromKnownColor(KnownColor.HighlightText)));
							vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(Color.FromKnownColor(KnownColor.Highlight)));
						}
						vwenv.OpenParagraph();
						var tss = vwenv.DataAccess.get_StringProp(hvo, ktagText);
						if (fHighlighted && m_listbox.ShowHighlight)
						{
							// Insert a string that has the foreground color not set, so the foreground color set above can take effect.
							var bldr = tss.GetBldr();
							bldr.SetIntPropValues(0, bldr.Length, (int)FwTextPropType.ktptForeColor, -1, -1);
							vwenv.AddString(bldr.GetString());
						}
						else
						{
							// Use the same Add method on both branches of the if.  Otherwise, wierd
							// results can happen on the display.  (See FWNX-803, which also affects
							// the Windows build, not just the Linux build!)
							vwenv.AddString(tss);
						}
						// REVIEW (DamienD): Why do we add blanks here? I commented this out.
						//vwenv.AddString(m_tssBlanks);
						vwenv.CloseParagraph();
						break;
				}
			}

			/// <summary>
			/// Displays the list of items in the list box.
			/// </summary>
			private void DisplayList(IVwEnv vwenv)
			{
				vwenv.OpenDiv();
				vwenv.AddObjVecItems(ktagItems, this, kfragItems);
				vwenv.CloseDiv();
			}
		}
	}
}
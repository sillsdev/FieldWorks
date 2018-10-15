// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Utils;
using SIL.PlatformUtilities;

namespace SIL.FieldWorks.FwCoreDlgs.Controls
{
	/// <summary>
	/// A "Combo" list box is one that can launch itself as a stand-alone, yet modal window, like the drop-down
	/// list in a Combo. It can also be used for other drop-down lists, for example, from an icon button.
	/// It is displayed using the Launch method. It is automatically hidden if the user clicks outside it
	/// (and the click is absorbed in the process). The item the user is hovering over is highlighted.
	/// </summary>
	public class ComboListBox : FwListBox, IComboList, IDropDownBox
	{
		#region Data members

		// This is a Form to contain the ListBox. I tried just making the list box visible,
		// but it seems only a Form can show up as a top-level window.
		// We track the form that was active when we launched, in hopes of working around
		// a peculiar bug that brings another window to the front when we close on some
		// systems (LT-2962).
		Form m_previousForm;
		// This filter captures clicks outside the list box while it is displayed.
		FwComboMessageFilter m_comboMessageFilter;
		// This flag determines whether we close the Dropdown List during a selection.

		// Button control that can contain the button that is used to bring up the list
		// This could be null / empty if not used.
		ComboBoxState m_state = ComboBoxState.Normal;

		private bool m_activateOnShow;

		#endregion Data members

		#region Properties

		/// <summary>
		/// Gets the state.
		/// </summary>
		public ComboBoxState State
		{
			get
			{
				return m_state;
			}

			set
			{
				m_state = value;
				LaunchButton.Invalidate();
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the combo box has a border.
		/// </summary>
		public bool HasBorder
		{
			get
			{
				return BorderStyle != BorderStyle.None;
			}

			set
			{
				BorderStyle = value ? BorderStyle.FixedSingle : BorderStyle.None;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the combo box will use the visual style background.
		/// </summary>
		public bool UseVisualStyleBackColor
		{
			get
			{
				return false;
			}

			set
			{
				// do nothing;
			}
		}

		/// <summary>
		/// This property exposes the button that is used to launch the list.
		/// </summary>
		public Button LaunchButton { get; set; }

		/// <summary>
		/// Giving ComboListBox this property is a convenience for clisnts that wish to use
		/// it somewhat interchangeably with FwComboBox. The style is always DropDownList.
		/// </summary>
		public ComboBoxStyle DropDownStyle
		{
			get
			{
				return ComboBoxStyle.DropDownList;
			}
			set
			{
				// required interface method does nothing at all.
			}
		}

		/// <summary />
		public bool ActivateOnShow
		{
			get
			{
				return m_activateOnShow;
			}

			set
			{
				m_activateOnShow = value;
				Form.TopMost = value;
			}
		}

		/// <summary>
		/// Find the width that will display the full width of all items.
		/// Note that if the height is set to less than the natural height,
		/// some additional space may be wanted for a scroll bar.
		/// </summary>
		public int NaturalWidth
		{
			get
			{
				InnerListBox.ShowHighlight = false;
				EnsureRoot();
				var result = InnerListBox.RootBox.Width;
				InnerListBox.ShowHighlight = true;
				return result + 5; // allows something for borders, etc.
			}
		}

		/// <summary>
		/// Find the height that will display the full height of all items.
		/// </summary>
		public int NaturalHeight
		{
			get
			{
				EnsureRoot();
				// The extra pixels are designed to be enough so that a scroll bar is not shown.
				// It allows for borders and so forth around the actual root box.
				return InnerListBox.RootBox.Height + 10;
			}
		}

		/// <summary>
		/// Allows the control to be more like an FwComboBox for classes wishing to initialize both the
		/// same way. Value is always derived from the selected index. Setting has no effect unless
		/// the text matches a selected item.
		/// </summary>
		[Browsable(true), DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		public override string Text
		{
			get
			{
				if (SelectedIndex < 0 || Items == null || SelectedIndex >= Items.Count || Items[SelectedIndex] == null)
				{
					return string.Empty;
				}
				return Items[SelectedIndex].ToString();
			}
			set
			{
				SelectedIndex = FindStringExact(value);
			}
		}

		/// <summary />
		public Form Form { get; private set; }

		/// <summary>
		///  Gets or sets the form that the ComboListBox is launched from.
		/// </summary>
		public Form LaunchingForm { get; set; }
		#endregion Properties

		#region Construction and disposal


		/// <summary>
		/// Make one.
		/// </summary>
		public ComboListBox()
		{
			m_activateOnShow = true;
			HasBorder = true;
			// It fills the list form.
			Dock = DockStyle.Fill;
			// Create a form to hold the list.
			Form = new Form { Size = Size, FormBorderStyle = FormBorderStyle.None, StartPosition = FormStartPosition.Manual, TopMost = true };
			Form.Controls.Add(this);
			Form.Deactivate += m_ListForm_Deactivate;
			Tracking = true;

			// Make sure this isn't null, allow launch to update its value
			m_previousForm = Form.ActiveForm;
		}

		#region IDisposable & Co. implementation

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****************** Missing Dispose() call for " + GetType().Name + " ******************");
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
				// Don't call Controls.Clear() - we need to dispose the controls it contains.
				// This will be done in base class.
				if (Form != null)
				{
					Form.Deactivate -= m_ListForm_Deactivate;
					Form.Controls.Remove(this);
					if (Form.Visible)
					{
						Form.Close();
					}
					Form.Dispose();
				}
				if (m_comboMessageFilter != null)
				{
					Application.RemoveMessageFilter(m_comboMessageFilter);
					m_comboMessageFilter.Dispose();
				}
			}
			Form = null;
			LaunchButton = null;
			m_comboMessageFilter = null;

			base.Dispose(disposing);
		}
		#endregion IDisposable & Co. implementation

		#endregion Construction and disposal

		#region Other methods

		/// <summary>
		/// Adjust the size of the list box, so it is just large enough to show all items,
		/// or at most the size specified by the arguments (or at least 100x10).
		/// </summary>
		public void AdjustSize(int maxWidth, int maxHeight)
		{
			var height = NaturalHeight;
			// Give ourselves a small margin of width, plus extra if we need a scroll bar.
			var width = NaturalWidth + (height > maxHeight ? 25 : 10);

			Form.Width = Math.Max(100, Math.Min(width, maxWidth));
			Form.Height = Math.Max(10, Math.Min(height, maxHeight));
		}

		/// <summary>
		/// Launch the ComboListBox.
		/// Typical usage, where 'this' is a control that the list should appear below:
		/// 		m_listBox.Launch(Parent.RectangleToScreen(Bounds), Screen.GetWorkingArea(this));
		/// Or, where rect is a rectangle in the client area of control 'this':
		///			m_listBox.Launch(RectangleToScreen(rect), Screen.GetWorkingArea(this);
		///	(Be sure to set the height and width of the ComboListBox first.)
		/// </summary>
		/// <param name="launcherBounds">A rectangle in 'screen' coordinates indicating where to display the list. Typically, as shown
		/// above, the location of something the user clicked to make the list display. It's significance is that
		/// the list will usually be shown with its top left just to the right of the bottom left of the rectangle, and
		/// (if the list width has not already been set explicitly) its width will match the rectangle. If there is not
		/// room to displya the list below this rectangle, it will be displayed above instead.</param>
		/// <param name="screenBounds">A rectangle in 'screen' coordinates indicating the location of the actual screen
		/// that the list is to appear on.</param>
		public void Launch(Rectangle launcherBounds, Rectangle screenBounds)
		{
			m_previousForm = Form.ActiveForm;
			if (Platform.IsMono)
			{
				// FWNX-908: Crash closing combobox.
				// Somehow on Mono, Form.ActiveForm can sometimes return m_listForm at this point.
				if (m_previousForm == null || m_previousForm == Form)
				{
					m_previousForm = LaunchingForm;
				}
			}
			// this is mainly to prevent it showing in the task bar.
			Form.ShowInTaskbar = false;
			//Figure where to put it. First try right below the main combo box.
			// Pathologically the list box may be bigger than the available height. If so shrink it.
			var maxListHeight = Math.Max(launcherBounds.Top - screenBounds.Top, screenBounds.Bottom - launcherBounds.Bottom);
			if (Form.Height > maxListHeight)
			{
				Form.Height = maxListHeight;
			}
			// This is the default position right below the combo.
			var popupBounds = new Rectangle(launcherBounds.Left, launcherBounds.Bottom, Form.Width, Form.Height);
			if (screenBounds.Bottom < popupBounds.Bottom)
			{
				// extends below the bottom of the screen. Use a rectangle above instead.
				// We already made sure it will fit in one place or the other.
				popupBounds = new Rectangle(launcherBounds.Left, launcherBounds.Top - Form.Height, Form.Width, Form.Height);
			}
			if (screenBounds.Right < popupBounds.Right)
			{
				// Extends too far to the right; adjust (amount is negative to move left).
				popupBounds.Offset(screenBounds.Right - popupBounds.Right, 0);
			}
			if (screenBounds.Left > popupBounds.Left)
			{
				// Extends too far to the left; adjust (amount is positive to move right).
				popupBounds.Offset(screenBounds.Left - popupBounds.Left, 0);
			}
			Form.Location = new Point(popupBounds.Left, popupBounds.Top);

			if (m_activateOnShow)
			{
				Form.Show(m_previousForm);
			}
			else
			{
				ShowInactiveTopmost(m_previousForm, Form);
			}

			if (m_comboMessageFilter != null)
			{
				// losing our ref to m_comboMessageFilter, while it's still added to the Application Message Filter,
				// would mean it would never get removed. (Which would be very bad.)
				Application.RemoveMessageFilter(m_comboMessageFilter);
				m_comboMessageFilter.Dispose();
			}

			m_comboMessageFilter = new FwComboMessageFilter(this);
			Application.AddMessageFilter(m_comboMessageFilter);
			if (m_activateOnShow)
			{
				FocusAndCapture();
			}
		}

		private const int SW_SHOWNOACTIVATE = 4;
		private const int HWND_TOPMOST = -1;
		private const uint SWP_NOACTIVATE = 0x0010;

		[DllImport("user32.dll", EntryPoint = "SetWindowPos")]
		private static extern bool SetWindowPos(
			int hWnd,           // window handle
			int hWndInsertAfter,    // placement-order handle
			int X,          // horizontal position
			int Y,          // vertical position
			int cx,         // width
			int cy,         // height
			uint uFlags);       // window positioning flags

		[DllImport("user32.dll")]
		private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		private const int GWL_HWNDPARENT = -8;

		[DllImport("user32.dll")]
		private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

		private static void ShowInactiveTopmost(Form owner, Form frm)
		{
			if (Platform.IsMono)
			{
				// TODO:  Implement something comparable on Linux/Mono if possible.
				return;
			}
			if (owner != null)
			{
				SetWindowLong(frm.Handle, GWL_HWNDPARENT, owner.Handle.ToInt32());
			}
			ShowWindow(frm.Handle, SW_SHOWNOACTIVATE);
			SetWindowPos(frm.Handle.ToInt32(), HWND_TOPMOST, frm.Left, frm.Top, frm.Width, frm.Height, SWP_NOACTIVATE);
		}

		/// <summary>
		/// Hide the containing form (and thus the list box as a whole).
		/// </summary>
		public void HideForm()
		{
			// The order of the following two lines is very important!  On some
			// machines the LT-2962 issue will show itself if this is changed.
			// The summary statement is that making the form not visible causes
			// the system to activate another application, and with out telling
			// it before hand what to activate it would get confused and cycle
			// through the applications that were currently running.  By
			// activating the main form first and then hiding the little form
			// the bug is not seen (and maybe not present...
			// but that's much like the forest and a falling tree debate.)
			//
			// ** Dont change the order of the following two lines **
			m_previousForm?.Activate();
			if (Form != null)
			{
				Form.Visible = false;
			}
			// reset HighlightedItem to current selected.
			HighlightedIndex = SelectedIndex;
			if (m_comboMessageFilter != null)
			{
				Application.RemoveMessageFilter(m_comboMessageFilter);
				m_comboMessageFilter.Dispose();
				m_comboMessageFilter = null;
			}
		}

		/// <summary>
		/// Cycle the selection to the next item that begins with the specified text.
		/// (Case insensitive. Trims whitespace in list items.)
		/// </summary>
		/// <param name="start">the search key</param>
		public void HighlightItemStartingWith(string start)
		{
			var iPreviousHighlighted = -1;
			int iStarting;
			var itemStr = string.Empty;
			if (HighlightedItem != null)
			{
				itemStr = TextOfItem(HighlightedItem).Text.Trim();
			}
			// If the new start key matches the start key for the current selection,
			// we'll start our search from there.
			if (HighlightedItem != null && itemStr.ToLower().StartsWith(start.ToLower()))
			{
				// save the current position as our previous index
				iPreviousHighlighted = HighlightedIndex;
				// set our search index to one after the currently selected item
				iStarting = iPreviousHighlighted + 1;
			}
			else
			{
				// start our search from the beginning
				iStarting = 0;
			}

			var iEnding = Items.Count - 1;
			var fFound = FindAndHighlightItemStartingWith(iStarting, iEnding, start);
			if (!fFound && iStarting != 0)
			{
				// Cycle from the beginning and see if we find a match before our
				// previous (i.e. current) selection.
				iStarting = 0;
				iEnding = iPreviousHighlighted - 1;
				FindAndHighlightItemStartingWith(iStarting, iEnding, start);
				// if we don't find a match, return quietly. Nothing else to select.
			}
		}

		/// <summary>
		/// Highlight the item whose text starts with the given startKey string (if item exists).
		/// </summary>
		private bool FindAndHighlightItemStartingWith(int iStarting, int iEnding, string startKey)
		{
			var fFound = false;
			for (var i = iStarting; i <= iEnding; ++i)
			{
				var itemStr = TextOfItem(Items[i]).Text.Trim();
				if (itemStr.ToLower().StartsWith(startKey.ToLower()))
				{
					// Highlight this item
					HighlightedItem = Items[i];
					ScrollHighlightIntoView();
					fFound = true;
					break;
				}
			}
			return fFound;
		}

		/// <summary>
		/// Use this property to avoid closing the dropdown list during a selection.
		/// Client is required to reset the flag after selection is completed.
		/// (Default is false.)
		/// </summary>
		public bool KeepDropDownListDuringSelection { get; set; }
		#endregion Other methods

		#region Event handlers

		private void m_ListForm_Deactivate(object sender, EventArgs e)
		{
			HideForm();
		}

		/// <summary>
		/// If a combo list box loses focus, hide it.
		/// </summary>
		protected override void OnLostFocus(EventArgs e)
		{
			base.OnLostFocus(e);
			HideForm();
		}

		/// <summary>
		/// This is called when the embedded InnerFwListBox detects a click outside its bounds,
		/// which happens only when it has been told to capture the mouse. ComboListBox overrides
		/// this to close the list box.
		/// </summary>
		public void OnCapturedClick()
		{
			HideForm();
		}

		#endregion Event handlers

		/// <summary>Message filter for detecting events that may hide the compbo </summary>
		private sealed class FwComboMessageFilter : IMessageFilter, IDisposable
		{
			private ComboListBox m_comboListbox;
			private bool m_fGotMouseDown; // true after a mouse down occurs anywhere at all.

			/// <summary>Constructor for filter object</summary>
			public FwComboMessageFilter(ComboListBox comboListbox)
			{
				m_comboListbox = comboListbox;
			}

			#region IDisposable & Co. implementation
			/// <summary>
			/// See if the object has been disposed.
			/// </summary>
			private bool IsDisposed { get; set; }

			/// <summary>
			/// Finalizer, in case client doesn't dispose it.
			/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
			/// </summary>
			/// <remarks>
			/// In case some clients forget to dispose it directly.
			/// </remarks>
			~FwComboMessageFilter()
			{
				Dispose(false);
				// The base class finalizer is called automatically.
			}

			/// <summary />
			public void Dispose()
			{
				Dispose(true);
				// This object will be cleaned up by the Dispose method.
				// Therefore, you should call GC.SupressFinalize to
				// take this object off the finalization queue
				// and prevent finalization code for this object
				// from executing a second time.
				GC.SuppressFinalize(this);
			}

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
			private void Dispose(bool disposing)
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
				m_comboListbox = null; // It is disposed of elsewhere.

				IsDisposed = true;
			}

			#endregion IDisposable & Co. implementation

			/// <summary />
			public bool PreFilterMessage(ref Message m)
			{
				switch ((Win32.WinMsgs)m.Msg)
				{
					case Win32.WinMsgs.WM_CHAR:
						{
							// Handle the Escape key by removing list if present with out selecting
							var wparam = m.WParam.ToInt32();
							var vkey = (Win32.VirtualKeycodes)(wparam & 0xffff);
							if (vkey == Win32.VirtualKeycodes.VK_ESCAPE)
							{
								m_comboListbox.OnCapturedClick();
								return true;
							}
							return false;
						}
					case Win32.WinMsgs.WM_LBUTTONDOWN:
					case Win32.WinMsgs.WM_LBUTTONUP:
						{
							// Handle any mouse left button activity.
							// This is tricky. A mouse DOWN anywhere outside our control closes the window without
							// making a choice. This is done by calling OnCapturedClick, and happens if we get
							// to the end of this case WITHOUT returning false.
							// A mouse UP usually does the same thing (this would correspond to clicking an item,
							// then changing your mind and moving the mouse away from the control).
							// But a mouse UP must only do it AFTER there has been a mouse DOWN.
							// Otherwise, the control disappears if the user drags slightly outside the launch button
							// (or didn't hit it exactly) in the process of the original click that launched it.
							// Non-client areas include the title bar, menu bar, window borders,
							// and scroll bars. But the only one in our combo is the scroll bar.
							if ((Win32.WinMsgs)m.Msg == Win32.WinMsgs.WM_LBUTTONDOWN)
							{
								m_fGotMouseDown = true; // and go on to check whether it's inside
							}
							else
							{
								if (!m_fGotMouseDown)
								{
									return false; // ignore mouse up until we get mouse down
								}
							}
							var c = Control.FromHandle(m.HWnd);
							// Clicking anywhere in an FwListBox, including it's scroll bar,
							// behaves normally.
							if ((c == m_comboListbox.InnerListBox || c == m_comboListbox.LaunchButton) && c.Visible)
							{
								var xPos = MiscUtils.LoWord(m.LParam);  // LOWORD(m.LParam);
								var yPos = MiscUtils.HiWord(m.LParam);  // HIWORD(m.LParam);

								if (xPos > 0x8fff || yPos < 0 ||    // x or y is negitive
									xPos > c.ClientSize.Width ||    // x is to big
									yPos > c.ClientSize.Height)     // y is to big
								{
									// this is our exit case - event outside of client space...
								}
								else
								{
									return false;
								}
							}
							// On Mono clicking on the Scrollbar causes return from Control.FromHandle
							// to be a ImplicitScrollBar which is a child of the ComboTextBox InnerListBox.
							if (c is ScrollBar && c.Parent == m_comboListbox.InnerListBox)
							{
								return false;
							}
							if (c.GetType().ToString() == "LanguageExplorer.Areas.TextsAndWords.Interlinear.Sandbox")
							{
								//Size lbSize = m_listbox.Bounds;
								var x1Pos = MiscUtils.LoWord(m.LParam); // LOWORD(m.LParam);
								var y1Pos = MiscUtils.HiWord(m.LParam); // HIWORD(m.LParam);

								// convert from one client to another client
								Win32.POINT screenPos;
								screenPos.x = x1Pos;
								screenPos.y = y1Pos;
								Win32.ClientToScreen(c.Handle, ref screenPos);
								Win32.ScreenToClient(m_comboListbox.Handle, ref screenPos);

								// Test the regular window, if fails then add 21 to the pos and
								// try it again (allow for the button up on the icon that
								// started this combolist).
								if (m_comboListbox.Bounds.Contains(screenPos.x, screenPos.y) || m_comboListbox.Bounds.Contains(screenPos.x, screenPos.y + 21))
								{
									return false;
								}
							}

							// Any other click is captured and turned into a message that causes the list box to go away.
							m_comboListbox.OnCapturedClick();
							// We want to consume mouse down, but not mouse up, because consuming mouse up
							// somehow suppresses the next click.
							return (Win32.WinMsgs)m.Msg == Win32.WinMsgs.WM_LBUTTONDOWN;
						}
					default:
						return false;
				}
			}
		}
	}
}
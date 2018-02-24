// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FwCoreDlgs.Controls;

namespace LanguageExplorer.Controls
{
	/// <summary>
	/// FwComboBox is a simulation of a regular Windows.Forms.ComboBox. It has much the same interface, though not all
	/// events and properties are yet supported. There are two main differences:
	/// (1) It is implemented using FieldWorks Views, and hence can render Graphite fonts properly.
	/// (2) Item labels can be TsStrings, in which case, formatting of items can vary based on the properties of string runs.
	///
	/// To get this behavior, you can
	///		(a) Let the items actually be ITsStrings.
	///		(b) Let the items implement the SIL.FieldWorks.LCM.ITssValue interface, which has just one property, public ITsString AsTss {get;}
	///
	///	You must also pass your writing system factory to the FwComboBox (set the WritingSystemFactory property).
	///	Otherwise, the combo box will not be able to interpret the writing systems of any TsStrings it is asked to display.
	///	It will improve performance to do this even if you are not using TsString data.
	/// </summary>
	public class FwComboBox : FwComboBoxBase, IComboList
	{
		#region Events

		/// <summary />
		public event EventHandler SelectedIndexChanged;

		#endregion Events

		#region Data members

		// The previous combo box text string
		private ITsString m_tssPrevious;

		#endregion Data members

		#region Construction and disposal

		/// <summary>
		/// Construct one.
		/// </summary>
		public FwComboBox()
		{
			TextBox.KeyPress += m_comboTextBox_KeyPress;
			m_button.KeyPress += m_button_KeyPress;
		}

		/// <summary>
		/// Creates the drop down box.
		/// </summary>
		protected override IDropDownBox CreateDropDownBox()
		{
			// Create the list.
			var comboListBox = new ComboListBox { LaunchButton = m_button };
			comboListBox.SelectedIndexChanged += m_listBox_SelectedIndexChanged;
			comboListBox.SameItemSelected += m_listBox_SameItemSelected;
			comboListBox.TabStopControl = TextBox;
			return comboListBox;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
				if (TextBox != null)
				{
					TextBox.KeyPress -= m_comboTextBox_KeyPress;
				}
				if (m_button != null)
				{
					m_button.KeyPress -= m_button_KeyPress;
				}
				if (ListBox != null)
				{
					ListBox.SelectedIndexChanged -= m_listBox_SelectedIndexChanged;
					ListBox.SameItemSelected -= m_listBox_SameItemSelected;
				}
			}
			m_tssPrevious = null;

			base.Dispose(disposing);
		}

		#endregion Construction and disposal

		#region Properties

		/// <summary>
		/// Make the list box accessible so its height can be adjusted.
		/// </summary>
		[BrowsableAttribute(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ComboListBox ListBox => m_dropDownBox as ComboListBox;

		/// <summary>
		/// The index of the item currently selected.
		/// Review JohnT: what value should be returned if the user has edited the text box
		/// to a value not in the list? Probably -1...do we need to do more to ensure this?
		/// </summary>
		public int SelectedIndex
		{
			get
			{
				return ListBox.SelectedIndex;
			}
			set
			{
				ListBox.SelectedIndex = value;
			}
		}

		/// <summary>
		/// Gets or sets the style sheet.
		/// </summary>
		[BrowsableAttribute(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override IVwStylesheet StyleSheet
		{
			get
			{
				return base.StyleSheet;
			}
			set
			{
				base.StyleSheet = value;
				ListBox.StyleSheet = value;
			}
		}

		/// <summary>
		/// Retrieve the list of items in the menu. Changes may be made to this to affect
		/// the visible menu contents.
		/// </summary>
		public ObjectCollection Items => ListBox.Items;

		/// <summary>
		/// Get/Set the selected item from the list; null if none is selected.
		/// </summary>
		[BrowsableAttribute(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override object SelectedItem
		{
			get
			{
				return ListBox.SelectedItem;
			}
			set
			{
				ListBox.SelectedItem = value;
				if (value != null)
				{
					TextBox.Tss = ListBox.TextOfItem(value);
				}
			}
		}

		/// <summary>
		/// Allows the control to function like an ordinary text box, setting and reading its text.
		/// Generally it is preferred to use the Tss property, giving access to the full
		/// styled string.
		/// </summary>
		[BrowsableAttribute(true), DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Visible)]
		public override string Text
		{
			get
			{
				return base.Text;
			}
			set
			{
				base.Text = value;
				ListBox.SelectedIndex = ListBox.FindStringExact(value);
			}
		}

		/// <summary>
		/// The real string of the embedded control.
		/// </summary>
		[BrowsableAttribute(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override ITsString Tss
		{
			get
			{
				return base.Tss;
			}
			set
			{
				base.Tss = value;
				// Don't just set the SelectedTss, as that throws an exception if not found.
				ListBox.SelectedIndex = ListBox.FindIndexOfTss(value);
			}
		}

		/// <summary>
		/// This is used (e.g., in filter bar) when the text we want to show in the combo
		/// is something different from the text of the selected item.
		/// </summary>
		public void SetTssWithoutChangingSelectedIndex(ITsString tss)
		{
			base.Tss = tss;
		}

		/// <summary />
		[BrowsableAttribute(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override int WritingSystemCode
		{
			get
			{
				return base.WritingSystemCode;
			}
			set
			{
				base.WritingSystemCode = value;
				ListBox.WritingSystemCode = value;
			}
		}

		/// <summary>
		/// The real WSF of the embedded control.
		/// </summary>
		[BrowsableAttribute(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override ILgWritingSystemFactory WritingSystemFactory
		{
			get
			{
				return base.WritingSystemFactory;
			}
			set
			{
				base.WritingSystemFactory = value;
				ListBox.WritingSystemFactory = value;
			}
		}

		/// <summary>
		/// When setting the font for this control we need it also set for the
		/// TextBox and ListBox
		/// </summary>
		public override Font Font
		{
			get
			{
				return base.Font;
			}
			set
			{
				base.Font = value;
				ListBox.Font = value;
			}
		}

		/// <summary>
		/// This property contains the previous text box text string
		/// </summary>
		[BrowsableAttribute(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public ITsString PreviousTextBoxText
		{
			get
			{
				return m_tssPrevious;
			}
			set
			{
				m_tssPrevious = value;
			}
		}

		#endregion Properties

		#region Other methods

		/// <summary>
		/// Add items to the FWComboBox but adjust the string so it
		/// matches the Font size.
		/// </summary>
		public void AddItem(ITsString tss)
		{
			//first calculate things to we adjust the font to the correct size.
			Items.Add(FontHeightAdjuster.GetAdjustedTsString(tss, FwTextBox.GetDympMaxHeight(TextBox), StyleSheet, WritingSystemFactory));
		}


		/// <summary>
		/// Find the index where exactly this string occurs in the list, or -1 if it does not.
		/// </summary>
		public int FindStringExact(string str)
		{
			return ListBox.FindStringExact(str);
		}

		/// <summary>
		/// Find the index where exactly this string occurs in the list, or -1 if it does not.
		/// </summary>
		public int FindStringExact(ITsString tss)
		{
			return ListBox.FindIndexOfTss(tss);
		}

		/// <summary>
		/// Fire the SelectedIndexChanged event.
		/// </summary>
		protected void RaiseSelectedIndexChanged()
		{
			SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
		}

		#endregion Other methods

		#region IVwNotifyChange implementation

		/// <summary />
		public override void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			// Currently the only property that can change is the string,
			// but verify it in case we later make a mechanism to work with a shared
			// cache. If it is the right property, report TextChanged.
			if (tag != InnerFwTextBox.ktagText)
			{
				return;
			}
			ListBox.IgnoreSelectedIndexChange = true;
			try
			{
				ListBox.SelectedIndex = ListBox.FindIndexOfTss(TextBox.Tss);
			}
			finally
			{
				ListBox.IgnoreSelectedIndexChange = false;
			}

			base.PropChanged(hvo, tag, ivMin, cvIns, cvDel);
		}

		#endregion IVwNotifyChange implementation

		#region Event handlers

		private void m_listBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			// If the selection came from choosing in the list box, we need to hide the list
			// box and focus on the text box.
			var fNeedFocus = false;
			if (ListBox.Form != null && ListBox.Form.Visible && !ListBox.KeepDropDownListDuringSelection)
			{
				HideDropDownBox();
				fNeedFocus = true;
			}
			if (ListBox.SelectedIndex == -1)
			{
				if (DropDownStyle != ComboBoxStyle.DropDown)
				{
					// Clear out the text box, since there isn't a selection now.
					// (Don't do this if typing is allowed, because we get a selection change to -1
					// when the user types something not in the list, and we don't want to erase what
					// the user typed.)
					PreviousTextBoxText = TextBox.Tss;	// save for future use (if needed)
					var bldr = TextBox.Tss.GetBldr();
					var props = bldr.get_Properties(0);
					var str = bldr.GetString().Text;
					var cStr = str?.Length ?? 0;
					bldr.Replace(0, cStr, string.Empty, props);
					// Can't do this since string might be null.
					// bldr.Replace(0, bldr.GetString().Text.Length, String.Empty, props);
					TextBox.Tss = bldr.GetString();
				}
			}
			else
			{
				// It's a real selection, so copy to the text box.
				PreviousTextBoxText = TextBox.Tss;	// save for future use (if needed)
				TextBox.Tss = ListBox.SelectedTss;
			}
			if (fNeedFocus)
			{
				TextBox.Focus();
			}
			// Finally notify our own delegates.
			RaiseSelectedIndexChanged();
		}

		private void m_listBox_SameItemSelected(object sender, EventArgs e)
		{
			if (!ListBox.KeepDropDownListDuringSelection)
			{
				HideDropDownBox();
				TextBox.Focus();
			}
		}

		/// <summary>
		/// Handle a key press in the combo box. If it is a dropdown list
		/// use typing to try to make a selection (cf. LT-2190).
		/// </summary>
		private void m_comboTextBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			// These have important meanings we don't want to suppress by saying we handled it.
			if (e.KeyChar == '\t' || e.KeyChar == '\r' || e.KeyChar == (char)Win32.VirtualKeycodes.VK_ESCAPE)
			{
				return;
			}
			if (DropDownStyle == ComboBoxStyle.DropDownList)
			{
				if (!char.IsControl(e.KeyChar))
				{
					// We should drop the list down first, so that
					// ScrollHighlightIntoView() will execute.
					DroppedDown = true;
					ListBox.HighlightItemStartingWith(e.KeyChar.ToString());
				}
				e.Handled = true;
			}
		}

		/// <summary>
		/// Handle a key press in the combo box. If it is a dropdown list
		/// use typing to try to make a selection (cf. LT-2190).
		/// </summary>
		private void m_button_KeyPress(object sender, KeyPressEventArgs e)
		{
			// These have important meanings we don't want to suppress by saying we handled it.
			if (e.KeyChar == '\t' || e.KeyChar == '\r' || e.KeyChar == (char)Win32.VirtualKeycodes.VK_ESCAPE)
			{
				return;
			}
			if (DropDownStyle == ComboBoxStyle.DropDownList)
			{
				if (!char.IsControl(e.KeyChar))
				{
					// We should drop the list down first, so that
					// ScrollHighlightIntoView() will execute.
					DroppedDown = true;
					ListBox.HighlightItemStartingWith(e.KeyChar.ToString());
				}
				e.Handled = true;
			}
		}

		#endregion Event handlers
	}
}

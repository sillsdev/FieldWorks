// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
// </copyright>
#endregion
//
// File: BarAdapterBase.cs
// Authorship History: Randy Regnier
// Last reviewed:
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Collections;
using System.Diagnostics;

using SIL.Utils; // for ImageCollection

namespace XCore
{
	/// <summary>
	/// Base class for all adapters that are dockable bars, such as menus or toolbars.
	/// </summary>
	public abstract class BarAdapterBase : AdapterBase
	{
		#region Properties

		/// <summary>
		/// Override to get (and perhaps create) a Bar.
		/// </summary>
		protected override Control MyControl
		{
			get { return MyMenuStrip; }
		}

		/// <summary>
		/// Gets the bar for the adapter. May be null.
		/// </summary>
		protected virtual MenuStrip MyMenuStrip
		{
			get
			{
				return (MenuStrip)m_control;
			}
		}

		#endregion Properties

		#region Construction

		/// <summary>
		/// Constructor.
		/// </summary>
		public BarAdapterBase()
		{
		}

		#endregion Construction

		#region Other methods
		protected ToolStripComboBox CreateComboBox(ChoiceGroup choice, bool wantsSeparatorBefore)
		{
			UIItemDisplayProperties display = choice.GetDisplayProperties();
			string label = display.Text;
			choice.PopulateNow();


			if (label == null)
				label = AdapterStrings.ErrorGeneratingLabel;

			if (!display.Visible)
				return null;


			label = label.Replace("_", "&");

			ToolStripComboBox combo = new ToolStripComboBox();

			combo.AccessibilityObject.Name = choice.Id;
			combo.Text = label;


			//foreach(ChoiceBase s in choice)
			//{
			//    item.Items.Add(s.Label);
			//}
			//item.Tag = choice;
			choice.ReferenceWidget = combo;
			combo.Tag = choice;
			FillCombo(choice);
			combo.SelectedIndexChanged +=  OnSelectedIndexChanged;

			combo.Enabled = display.Enabled;
			combo.Visible = display.Visible;



			return combo;
		}

		/// <summary>
		/// populate a combo box on the toolbar
		/// </summary>
		/// <param name="choice">The group that is the basis for this combo box.</param>
		private void FillCombo(ChoiceGroup choice)
		{
			UIItemDisplayProperties groupDisplay = choice.GetDisplayProperties();

			ToolStripComboBox combo = choice.ReferenceWidget as ToolStripComboBox;
			if (combo.Focused)
				return;//don't mess while we're in the combo

			// Disable if needed, but still show what's current, as for unicode fields where you can't change it, but you want to see what it is set to.
			combo.Enabled = groupDisplay.Enabled;

			ArrayList newItems = new ArrayList();
			bool fDifferent = false;
			var selectedItem = (ChoiceBase) null;
			foreach (ChoiceRelatedClass item in choice)
			{
				if (item is SeparatorChoice)
				{
					//TODO
				}
				else if (item is ChoiceBase)
				{
					newItems.Add(item);
					//if (groupDisplay.Checked)
					   // selectedItem = (ChoiceBase) item;

				  if (choice.SinglePropertyValue == (item as ListPropertyChoice).Value)
						selectedItem = (ChoiceBase) item;
					if (combo.Items.Count < newItems.Count || combo.Items[newItems.Count - 1] != item)
							fDifferent = true;

				}

			}

			// let it take the default
			// combo.AccessibleName = choice.Label;
			if (fDifferent || selectedItem != (combo.SelectedItem))
			{
				//combo.Click -= new EventHandler(OnComboClick);	//don't generate clicks (which end up being onpropertychanged() calls)
				combo.Items.Clear();
				combo.Items.AddRange(newItems.ToArray());

				combo.DropDownStyle = ComboBoxStyle.DropDownList;

				combo.SelectedItem = selectedItem;

				//combo.SuspendLayout = false;
			}

			////Set the ComboWidth of the combo box so that is is wide enough to show
			////the text of all items in the list.
			//int maxStringLength = 0;
			//for (int i = 0; i < combo.Items.Count; i++)
			//{
			//    if (combo.Items[i].ToString().Length > maxStringLength)
			//    {
			//        maxStringLength = combo.Items[i].ToString().Length;
			//    }
			//}
			//int factor = 6;
			//if (maxStringLength > 0 && combo.ComboWidth < maxStringLength * factor)
			//    combo.ComboWidth = maxStringLength * factor;
			//combo.Tooltip = combo.ToString();

		}

		bool IsAcceptableShortcut(Keys shortcut)
		{
			// According to
			// https://connect.microsoft.com/VisualStudio/feedback/details/91616/toolstripmenuitem-shortcutkeys-keys-d0?wa=wsignin1.0#tabs)
			// Something can be entered as the shortcut of a menu item only if it includes the Alt or Control modifier,
			// or if it includes one of the function keys F1-F24, or if it is the Insert or Delete key.
			// We use a number of other shortcuts, such as Return and arrow keys in combination with various modifiers,
			// but must implement the shortcut fuction other than automatically by the menu item.
			if ((shortcut & (Keys.Alt | Keys.Control)) != 0)
				return true;
			var shortcutMain = (Keys) (shortcut & (~Keys.Modifiers)); // main key without any modifiers.
			if (shortcutMain == Keys.Insert || shortcutMain == Keys.Delete)
				return true;
			return (shortcutMain >= Keys.F1 && shortcutMain <= Keys.F24);
		}

		/// <summary>
		/// Create a button item for use on a menu or a toolbar.
		/// </summary>
		/// <param name="choice">The details for the new item.</param>
		/// <param name="reallyVisible">true if the item will be visible eventually
		/// (it never is right away, because the parent control isn't yet)</param>
		/// <exception cref="ConfigurationException"></exception>
		/// <returns>The new item.</returns>
		protected ToolStripItem CreateButtonItem(ChoiceBase choice, out bool reallyVisible)
		{
			UIItemDisplayProperties display = choice.GetDisplayProperties();
			reallyVisible = display.Visible;

			string label = display.Text;
			if (label == null)
				label = AdapterStrings.ErrorGeneratingLabel;

			label = label.Replace("_", "&");

			ToolStripButton  item = new ToolStripButton(label);

			item.AccessibilityObject.Name = choice.Id;
			item.Tag = choice.Id;


			if(choice is CommandChoice)
			{
				item.Click += OnClick;
			}
			else
			{
				item.Click += choice.OnClick;
			}

			Image image = null;
			if (display.ImageLabel!= "default")
				image = m_smallImages.GetImage(display.ImageLabel);
			item.Image = image;
			item.Checked = display.Checked;

			Debug.Assert(item != null);
			item.Tag = choice;
			item.Enabled = display.Enabled;
			item.Visible = display.Visible;

			object helper = m_mediator.PropertyTable.GetValue("ContextHelper");

			if (helper != null)
			{
				String s = ((IContextHelper)helper).GetToolTip(choice.HelpId);
				if (choice.Shortcut != Keys.None)
				{

					KeysConverter kc = new KeysConverter();
					s += '(' + kc.ConvertToString(choice.Shortcut) + ')';
					item.ToolTipText = s;
				}
			}
			else
				item.ToolTipText = item.Text.Replace("&",""); //useful for buttons.


			choice.ReferenceWidget = item;
			return item;
		}
		protected ToolStripItem CreateMenuItem(ChoiceBase choice, out bool reallyVisible)
		{
			UIItemDisplayProperties display = choice.GetDisplayProperties();
			reallyVisible = display.Visible;

			string label = display.Text;
			if (label == null)
				label = AdapterStrings.ErrorGeneratingLabel;

			label = label.Replace("_", "&");

			ToolStripMenuItem item = new ToolStripMenuItem(label);

			item.AccessibilityObject.Name = choice.Id;
			item.Tag = choice.Id;


			if (choice is CommandChoice)
			{
				item.Click += OnClick;
			}
			else
			{
				item.Click += choice.OnClick;
			}

			Image image = null;
			if (display.ImageLabel != "default")
				image = m_smallImages.GetImage(display.ImageLabel);
			item.Image = image;
			item.Checked = display.Checked;

			if(choice.Shortcut != Keys.None)
			{
				KeysConverter sc = new KeysConverter();

				try
				{
					if (IsAcceptableShortcut(choice.Shortcut))
						item.ShortcutKeys = choice.Shortcut;
					// otherwise some other code must implement the shortcut, the built-in menu item code won't do it.
				}
				catch(Exception ex)
				{
					if (!(ex is InvalidEnumArgumentException))
						throw new ConfigurationException(
							"Software couldn't understand or doesn't support this shortcut: ("
							 + choice.Shortcut + ") for " + choice.Label, ex);
				}
			}

			Debug.Assert(item != null);
			item.Tag = choice;
			item.Enabled = display.Enabled;
			item.Visible = display.Visible;

			object helper = m_mediator.PropertyTable.GetValue("ContextHelper");

			if (helper != null)
			{
				String s = ((IContextHelper)helper).GetToolTip(choice.HelpId);
				item.ToolTipText = s;
				if (choice.Shortcut != Keys.None)
				{

					KeysConverter kc = new KeysConverter();
					item.ToolTipText += '(' + kc.ConvertToString(choice.Shortcut) + ')';
				}

			}
			else
				item.ToolTipText = item.Text.Replace("&", ""); //useful for buttons.


			choice.ReferenceWidget = item;
			return item;
		}
		#endregion Other methods

		#region Event handlers

		/// <summary>
		/// Handles the button item click event by passing it on to the ChoiceBase.
		/// </summary>
		/// <param name="something">The ButtonItem that was clicked.</param>
		/// <param name="args">Event arguments, which are not currently used.</param>
		protected virtual void OnClick(object something, EventArgs args)
		{
			ToolStripItem item = (ToolStripItem)something;
			ChoiceBase control = (ChoiceBase)item.Tag;
			if (control == null)
			{
				// Debug.Assert(control != null);
				// LT-2884 : this crash is infrequent, so for now just removing the assert
				MessageBox.Show(AdapterStrings.ErrorProcessingThatClick,
					AdapterStrings.ProcessingError, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}
			control.OnClick(item, null);
		}
		protected virtual void OnSelectedIndexChanged(object something, EventArgs args)
		{
			ToolStripComboBox item = (ToolStripComboBox)something;
			ChoiceGroup control = (ChoiceGroup)item.Tag;
			if (control == null)
			{
				// Debug.Assert(control != null);
				// LT-2884 : this crash is infrequent, so for now just removing the assert
				MessageBox.Show(AdapterStrings.ErrorProcessingThatClick,
					AdapterStrings.ProcessingError, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return;
			}


			var tgt =  (ListPropertyChoice) ( item.SelectedItem);
			control.HandleItemClick(tgt);


		}


		#endregion Event handlers
	}
}

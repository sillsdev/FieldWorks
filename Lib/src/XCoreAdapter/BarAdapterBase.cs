// --------------------------------------------------------------------------------------------
// Copyright (c) 2004-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: BarAdapterBase.cs
// Authorship History: Randy Regnier
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections;
using System.Diagnostics;

using DevComponents.DotNetBar;

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
			get { return MyBar; }
		}

		/// <summary>
		/// Gets the bar for the adapter. May be null.
		/// </summary>
		protected virtual Bar MyBar
		{
			get
			{
				return (Bar)m_control;
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

		/// <summary>
		/// Create a button item for use on a menu or a toolbar.
		/// </summary>
		/// <param name="choice">The details for the new item.</param>
		/// <param name="wantsSeparatorBefore">True, if it should have a separator before it,
		/// otherwise false.</param>
		/// <returns>The new item.</returns>
		protected ButtonItem CreateButtonItem(ChoiceBase choice, bool wantsSeparatorBefore)
		{
			UIItemDisplayProperties display = choice.GetDisplayProperties();

			string label = display.Text;
			if (label ==null)
				label = AdapterStrings.ErrorGeneratingLabel;

			label = label.Replace("_", "&");
			ButtonItem item = new ButtonItem(choice.Id, label);
//			if(choice is CommandChoice)
//			{
//				Image image = null;
//				if (display.ImageLabel!= "default")
//					image = m_smallImages.GetImage(display.ImageLabel);
//				item.Image = image;
//				item.Click += new EventHandler(OnClick);
//
//				// this is a bit odd to have checks on commands, but in at least one case
//				// (field visibility) the programmer expected this to work.  So let's make it work.
//				item.Checked = display.Checked;
//			}
//			else
//			{
//				item.Click += new EventHandler(choice.OnClick);
//				item.Checked = display.Checked;
//			}
			if(choice is CommandChoice)
			{
				item.Click += new EventHandler(OnClick);
			}
			else
			{
				item.Click += new EventHandler(choice.OnClick);
			}

			Image image = null;
			if (display.ImageLabel!= "default")
				image = m_smallImages.GetImage(display.ImageLabel);
			item.Image = image;
			item.Checked = display.Checked;

			if(choice.Shortcut != System.Windows.Forms.Keys.None)
			{
				try
				{
					//try just casting in the shortcut
					item.Shortcuts.Add((eShortcut) choice.Shortcut);
				}
				catch
				{
					throw new ConfigurationException("DotNetBar Adapter couldn't understand or doesn't support this shortcut: ("+choice.Shortcut.ToString()+") for "+choice.Label);
				}
//					string s = choice.Shortcut.ToString().Replace("+","");//remove any +
//					//				System.Windows.Forms.Shortcut sc = (System.Windows.Forms.Shortcut) Enum.Parse(typeof(System.Windows.Forms.Shortcut), s);
//					//DevComponents.DotNetBar.e
//					if(s.IndexOf(", Shift")> -1)
//					{
//						s= "Shift"+s.Replace(", Shift","");
//					}
//					if(s.IndexOf(", Control")> -1)
//					{
//						s= "Ctrl"+s.Replace(", Control","");
//					}
//					if(s.IndexOf(", Alt")> -1)
//					{
//						s= "Alt"+s.Replace(", Alt","");
//					}
//					try
//					{
//						eShortcut e = (eShortcut) Enum.Parse(typeof(eShortcut), s);
//						item.Shortcuts.Add(e);
//						catch
//						{
//							item.Shortcuts.Add((eShortcut)choice.Shortcut);
//							//throw new ConfigurationException("DotNetBar Adapter couldn't understand or doesn't support this shortcut: "+s+" ("+choice.Shortcut.ToString()+") for "+choice.Label);
//						}
//					}
//					catch
//					{
//						item.Shortcuts.Add((eShortcut)choice.Shortcut);
//						//throw new ConfigurationException("DotNetBar Adapter couldn't understand or doesn't support this shortcut: "+s+" ("+choice.Shortcut.ToString()+") for "+choice.Label);
//					}
//				}
			}

			Debug.Assert(item != null);
			item.BeginGroup = wantsSeparatorBefore;
			item.Tag = choice;
			item.Enabled = display.Enabled;
			item.Visible = display.Visible;
			object helper = m_mediator.PropertyTable.GetValue("ContextHelper");
			if (helper != null)
				item.Tooltip =((IContextHelper)helper).GetToolTip(choice.HelpId);
			else
				item.Tooltip = item.Text.Replace("&",""); //useful for buttons.

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
		protected virtual void OnClick(object something, System.EventArgs args)
		{
			ButtonItem item = (ButtonItem)something;

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

		#endregion Event handlers
	}
}

// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Windows.Forms;
using LanguageExplorer.Controls.DetailControls;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Controls.PaneBar
{
	internal class PanelButton : PanelExtension
	{
		private bool _mouseOverControl;
		private readonly IPropertyTable _propertyTable;
		private readonly Image _image;
		private readonly string _property;
		private readonly string _checkedLabel;
		private readonly string _uncheckedLabel;
		private bool _isChecked;


		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="propertyTable">The property table, which we use to reset the relevant "ShowHiddenFields" property.</param>
		/// <param name="image">Optional Image to display.</param>
		/// <param name="property">The name of the property in the table that is being monitored.</param>
		/// <param name="checkedLabel">Label to display, when check box is checked</param>
		/// <param name="uncheckedLabel">Label to display, when check box is not checked.</param>
		public PanelButton(IPropertyTable propertyTable, Image image, string property, string checkedLabel, string uncheckedLabel)
		{
			_propertyTable = propertyTable;
			_image = image;
			_property = property;
			_isChecked = propertyTable.GetValue(_property, false);
			_checkedLabel = checkedLabel;
			_uncheckedLabel = uncheckedLabel;

			Dock = DockStyle.Right;
			Font = new Font("Tahoma", 13F, FontStyle.Regular, GraphicsUnit.Point, 0);
			Location = new Point(576, 2);
			Name = "panelButton";
			Anchor = AnchorStyles.None;
			Size = new Size(120, 20);

			MouseEnter += panelButton_MouseEnter;
			MouseLeave += panelButton_MouseLeave;
			MouseDown += panelButton_MouseDown;
			Click += PanelButton_CheckBox_Clicked;
			TabIndex = 0;

			SetLabel();
		}

		/// <summary>
		/// Set the DataTree.
		/// </summary>
		public DataTree DatTree { get; set; }

		private void SetLabel()
		{
			const int checkBoxWidth = 17;
			Text = _isChecked ? _checkedLabel : _uncheckedLabel;

			using (var g = CreateGraphics())
			{
				var labelWidth = (int)(g.MeasureString(Text + "_", Font).Width);
				Width = labelWidth;
			}

			// Simulate a mouse enter or leave event to get the correct highlighting
			if (_mouseOverControl)
			{
				panelButton_MouseEnter(null, null);
			}
			else
			{
				panelButton_MouseLeave(null, null);
			}

			// Unwire event handlers
			foreach (Control control in Controls)
			{
				if (control is CheckBox)
				{
					var controlAsCheckBox = (CheckBox)control;
					controlAsCheckBox.Click -= PanelButton_CheckBox_Clicked;
					controlAsCheckBox.MouseEnter -= panelButton_MouseEnter;
					controlAsCheckBox.MouseLeave -= panelButton_MouseLeave;
					controlAsCheckBox.MouseDown -= panelButton_MouseDown;
				}
				else if (control is PanelExtension)
				{
					var controlAsPanelExtension = (PanelExtension)control;
					controlAsPanelExtension.Click -= PanelButton_Image_Clicked;
					controlAsPanelExtension.MouseEnter -= panelButton_MouseEnter;
					controlAsPanelExtension.MouseLeave -= panelButton_MouseLeave;
					controlAsPanelExtension.MouseDown -= panelButton_MouseDown;
				}
				control.Dispose();
			}
			Controls.Clear(); // Clear out any previous checkboxes and images

			// Add in a checkbox that reflects the "checked" status of the button
			var checkBox = new CheckBox
			{
				Name = "CheckBox",
				Checked = _isChecked,
				Location = new Point(0, 0),
				Anchor = AnchorStyles.Left,
				Dock = DockStyle.Left,
				Width = checkBoxWidth,
				BackColor = Color.Transparent
			};
			checkBox.Click += PanelButton_CheckBox_Clicked;
			checkBox.MouseEnter += panelButton_MouseEnter;
			checkBox.MouseLeave += panelButton_MouseLeave;
			checkBox.MouseDown += panelButton_MouseDown;
			Controls.Add(checkBox);

			Width += checkBox.Width;

			if (_image != null)
			{
#if RANDYTODO
				// TODO: Are there any users of this? If so what do they do?
#endif
				var p = new PanelExtension
				{
					Name = "PanelExtension",
					BackgroundImage = _image,
					BackgroundImageLayout = ImageLayout.Center,
					Location = new Point(checkBox.Width, 0),
					Anchor = AnchorStyles.Left,
					Dock = DockStyle.None,
					Size = new Size(17, Height)
				};
				Width += p.Size.Width;
				p.Click += PanelButton_Image_Clicked;
				p.MouseEnter += panelButton_MouseEnter;
				p.MouseLeave += panelButton_MouseLeave;
				p.MouseDown += panelButton_MouseDown;
				Controls.Add(p);
			}
			Refresh();
		}

		/// <summary />
		public void UpdateDisplay()
		{
			SetLabel();
		}

		private void PanelButton_CheckBox_Clicked(object sender, EventArgs e)
		{
			using (new WaitCursor(Form.ActiveForm))
			{
				var cb = (CheckBox)Controls.Find("CheckBox", false)[0];
				_isChecked = cb.Checked;
				_propertyTable.SetProperty(_property, _isChecked, SettingsGroup.LocalSettings, true, false);
				DatTree.ShowHiddenFields(_isChecked);
			}
		}

		private void PanelButton_Image_Clicked(object sender, EventArgs e)
		{
			using (new WaitCursor(Form.ActiveForm))
			{
				_isChecked = !_isChecked;
				_propertyTable.SetProperty(_property, _isChecked, SettingsGroup.LocalSettings, true, false);
				DatTree.ShowHiddenFields(_isChecked);
			}
		}

		private void panelButton_MouseEnter(object sender, EventArgs e)
		{
			_mouseOverControl = true;

			Refresh();
		}

		private void panelButton_MouseDown(object sender, MouseEventArgs e)
		{
			Refresh();
		}

		private void panelButton_MouseLeave(object sender, EventArgs e)
		{
			_mouseOverControl = false;

			Refresh();
		}
	}
}

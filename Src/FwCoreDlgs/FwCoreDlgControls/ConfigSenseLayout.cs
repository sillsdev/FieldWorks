// Copyright (c) 2011-2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ConfigSenseLayout.cs
// Responsibility: mcconnel

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Text;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.FwCoreDlgControls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Group together all the controls used for specifying the layout specific to Senses.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class ConfigSenseLayout : UserControl
	{
		///<summary>delegate for passing on the clicked event</summary>
		public delegate void SensesBtnClickedHandler(object sender, EventArgs e);
		///<summary>
		/// Event handler for clicking the "Senses" button.
		///</summary>
		public event SensesBtnClickedHandler SensesBtnClicked;

		/// <summary>
		/// delegate for signaling that the "Display each sense in a paragraph" checkbox has been selected.
		/// This was necessary to allow the XmlDocConfigureDlg to respond to this check by disabling
		/// the "Surrounding Context" option. -gjm 7/2011
		/// (I may have messed up naylor's earlier Sense/Subentries handling.)
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public delegate void DisplaySenseInParaCheckedHandler(object sender, EventArgs e);

		/// <summary>
		/// event handler for the checking of the "Display each sense in a paragraph" checkbox.
		/// </summary>
		public event DisplaySenseInParaCheckedHandler DisplaySenseInParaChecked;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ConfigSenseLayout"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ConfigSenseLayout()
		{
			InitializeComponent();
			m_cbSenseParaStyle.Enabled = false;
			m_btnMoreStyles.Enabled = false;
			m_btnMoreStyles.Click += m_btnMoreStyles_Click;
			m_chkSenseParagraphStyle.CheckedChanged +=new EventHandler(m_chkSenseParagraphStyle_CheckedChanged);
		}

		/// <summary>
		/// Finish initializing those controls that need it.
		/// </summary>
		public void Initialize()
		{
			FillNumberStyleComboList();
			FillNumberFontComboList();
		}

		/// <summary>
		/// Convert a distance that is right at 96 dpi to the current screen dpi
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		int From96dpiY(int input)
		{
			using (var g = CreateGraphics())
			{
				return (int)Math.Round(input * g.DpiY / 96.0);
			}
		}

		///// <summary>
		///// Adjust the two controls. Redundant at 96 dpi, but essential at other resolutions,
		///// after Windows.Forms tries to autoscale them.
		///// </summary>
		//protected override void OnLayout(LayoutEventArgs e)
		//{
		//    base.OnLayout(e);
		//    m_grpSensePara.Top = m_grpSenseNumber.Bottom - From96dpiY(10);
		//    m_grpSensePara.Width = m_grpSenseNumber.Width = Math.Max(m_grpSensePara.Width, m_grpSenseNumber.Width);
		//    MinimumSize = new Size(0, m_grpSensePara.Bottom);
		//}

		/// <summary>
		/// Fill the combobox list which gives the possibilities for numbering a recursive
		/// sequence.  The  first argument of the combo item is the displayed string.  The
		/// second element is the marker inserted into the string for displaying numbers as
		/// given.  The marker must be interpreted by code in
		/// XmlVc.DisplayVec(IVwEnv vwenv, int hvo, int flid, int frag) (XMLViews/XmlVc.cs).
		/// </summary>
		private void FillNumberStyleComboList()
		{
			m_cbNumberStyle.Items.Add(new NumberingStyleComboItem(FwCoreDlgControls.ksNone, ""));
			m_cbNumberStyle.Items.Add(new NumberingStyleComboItem("1  1.2  1.2.3", "%O"));
			m_cbNumberStyle.Items.Add(new NumberingStyleComboItem("1  b  iii", "%z"));
		}

		/// <summary>
		/// Fill the combobox list which gives the possible fonts for displaying the numbers
		/// of a numbered recursive sequence.
		/// </summary>
		private void FillNumberFontComboList()
		{
			m_cbNumberFont.Items.Add(FwCoreDlgControls.kstidUnspecified);
			using (var installedFontCollection = new InstalledFontCollection())
			{
				var fontFamilies = installedFontCollection.Families;
				var count = fontFamilies.Length;
				for (var i = 0; i < count; ++i)
				{
					// The .NET framework is unforgiving of using a font that doesn't support the
					// "regular" style.  So we won't allow the user to even see them...
					if (fontFamilies[i].IsStyleAvailable(FontStyle.Regular))
					{
						var familyName = fontFamilies[i].Name;
						m_cbNumberFont.Items.Add(familyName);
					}
					fontFamilies[i].Dispose();
				}
			}
		}

		/// <summary>
		/// Access the value of the "Display sense in paragraph" checkbox.
		/// </summary>
		public bool DisplaySenseInPara
		{
			get { return m_chkSenseParagraphStyle.Checked; }
			set { m_chkSenseParagraphStyle.Checked = value; }
		}

		/// <summary>
		/// Get the number style combobox.
		/// </summary>
		public ComboBox NumberStyleCombo
		{
			get { return m_cbNumberStyle; }
		}

		/// <summary>
		/// Get the number font combobox.
		/// </summary>
		public ComboBox NumberFontCombo
		{
			get { return m_cbNumberFont; }
		}

		/// <summary>
		/// Get the sense (paragraph) style combobox.
		/// </summary>
		public ComboBox SenseStyleCombo
		{
			get { return m_cbSenseParaStyle; }
		}

		/// <summary>
		/// Initialize the list of styles in the styles combo boxes.
		/// </summary>
		public void FillStylesCombo(List<StyleComboItem> styles)
		{
			m_cbSenseParaStyle.Items.Clear();
			m_cbSenseParaStyle.Items.AddRange(styles.ToArray());
		}

		/// <summary>
		/// Get/set the name of the currently selected sense paragraph style.
		/// </summary>
		public string SenseParaStyle
		{
			get { return GetStyleName(m_cbSenseParaStyle); }
			set { SetStyleName(m_cbSenseParaStyle, value); }
		}

		private static void SetStyleName(ComboBox combo, string value)
		{
			for (var i = 0; i < combo.Items.Count; ++i)
			{
				var sci = combo.Items[i] as StyleComboItem;
				if (sci == null || sci.Style == null || sci.Style.Name != value)
					continue;
				combo.SelectedIndex = i;
				break;
			}
		}

		private static string GetStyleName(ComboBox combo)
		{
			var sci = (StyleComboItem)combo.SelectedItem;
			return sci != null && sci.Style != null ? sci.Style.Name : null;
		}

		/// <summary>
		/// Access the value of the "number single sense" checkbox.
		/// </summary>
		public bool NumberSingleSense
		{
			get { return m_chkNumberSingleSense.Checked; }
			set { m_chkNumberSingleSense.Checked = value; }
		}

		/// <summary>
		/// Access the value of the text after the sense number.
		/// </summary>
		public string AfterNumber
		{
			get { return m_tbAfterNumber.Text; }
			set { m_tbAfterNumber.Text = value; }
		}

		/// <summary>
		/// Access the value of the text before the sense number.
		/// </summary>
		public string BeforeNumber
		{
			get { return m_tbBeforeNumber.Text; }
			set { m_tbBeforeNumber.Text = value; }
		}

		/// <summary>
		/// Access the check state for setting bold sense numbers.
		/// </summary>
		public CheckState BoldSenseNumber
		{
			get { return m_chkSenseBoldNumber.CheckState; }
			set { m_chkSenseBoldNumber.CheckState = value; }
		}

		/// <summary>
		/// Access the check state for setting italic sense numbers.
		/// </summary>
		public CheckState ItalicSenseNumber
		{
			get { return m_chkSenseItalicNumber.CheckState; }
			set { m_chkSenseItalicNumber.CheckState = value; }
		}

		/// <summary>
		/// Hide the whole group of Sense Number Configuration controls
		/// (currently for Subsense nodes).
		/// </summary>
		public void HideSenseNumberConfiguration()
		{
			m_grpSenseNumber.Visible = false;
		}

		/// <summary>
		/// Show the whole group of Sense Number Configuration controls.
		/// </summary>
		public void ShowSenseNumberConfiguration()
		{
			m_grpSenseNumber.Visible = true;
		}

		#region Event Handlers
		// ReSharper disable InconsistentNaming

		private void m_chkSenseParagraphStyle_CheckedChanged(object sender, EventArgs e)
		{
			if (m_chkSenseParagraphStyle.Checked)
			{
				// Sense number configuration is now independent of paragraph style or not
				// LT-11598 -- GJM

				m_cbSenseParaStyle.Enabled = true;
				m_btnMoreStyles.Enabled = true;
				if (DisplaySenseInParaChecked != null)
					DisplaySenseInParaChecked(this, null);
			}
			else // unchecked
			{
				// Sense number configuration is now independent of paragraph style or not
				// LT-11598 -- GJM

				m_cbSenseParaStyle.Enabled = false;
				m_btnMoreStyles.Enabled = false;
				if (DisplaySenseInParaChecked != null)
					DisplaySenseInParaChecked(this, null);
			}
		}

		private void m_btnMoreStyles_Click(object sender, EventArgs e)
		{
			if (SensesBtnClicked != null)
				SensesBtnClicked(sender, e);
		}

		private void m_cbNumberStyle_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (m_cbNumberStyle.SelectedIndex == 0)
			{
				m_chkNumberSingleSense.Enabled = false;
				m_tbAfterNumber.Enabled = false;
				m_tbBeforeNumber.Enabled = false;
				m_chkSenseBoldNumber.Enabled = false;
				m_chkSenseItalicNumber.Enabled = false;
				m_cbNumberFont.Enabled = false;
			}
			else
			{
				m_chkNumberSingleSense.Enabled = true;
				m_tbAfterNumber.Enabled = true;
				m_tbBeforeNumber.Enabled = true;
				m_chkSenseBoldNumber.Enabled = true;
				m_chkSenseItalicNumber.Enabled = true;
				m_cbNumberFont.Enabled = true;
			}
		}

		// ReSharper restore InconsistentNaming
		#endregion
	}
}

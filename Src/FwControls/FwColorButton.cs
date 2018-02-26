// Copyright (c) 2002-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// Summary description for FwColorButton.
	/// </summary>
	public class FwColorButton : FwButton
	{
		private Color m_clrColorValue;
		private Color m_clrColorSquareBorderColor;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		/// <summary>
		///
		/// </summary>
		public FwColorButton()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitForm call
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			//
			// FwColorButton
			//
			this.Name = "FwColorButton";
			this.Size = new System.Drawing.Size(15, 15);
			this.ButtonStyle = ButtonStyles.Popup;
			this.SunkenAppearance = SunkenAppearances.Deep;
			this.ButtonToggles = true;
		}
		#endregion

		//**********************************************************************************************
		//****
		//**** Properties
		//****
		//**********************************************************************************************
		/*
				public bool ShouldSerializeColorValue()
				{
					return (m_clrColorValue != System.Drawing.Color.White);
				}

				public override void ResetColorValue()
				{
					ColorValue = System.Drawing.Color.White;
				}
		*/
		/// <summary>
		///
		/// </summary>
		public Color ColorValue
		{
			get
			{
				return m_clrColorValue;
			}
			set
			{
				m_clrColorValue = value;
				this.Invalidate();
			}
		}

		//**********************************************************************************************
		//**********************************************************************************************
		/*
				public bool ShouldSerializeColorSquareBorderColor()
				{
					return (m_clrColorSquareBorderColor != SystemColors.ControlText);
				}

				public override void ResetColorSquareBorderColor()
				{
					ColorSquareBorderColor = SystemColors.ControlText;
				}
		*/
		/// <summary>
		///
		/// </summary>
		public Color ColorSquareBorderColor
		{
			get
			{
				return m_clrColorSquareBorderColor;
			}
			set
			{
				m_clrColorSquareBorderColor = value;
				this.Invalidate();
			}
		}

		//**********************************************************************************************
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		//**********************************************************************************************
		protected override void OnPaint(PaintEventArgs e)
		{
			System.Console.WriteLine("ColorButton Paint");
			base.OnPaint(e);
			Rectangle rect = new Rectangle(2, 2, this.Width - 3, this.Height - 3);
			e.Graphics.FillRectangle(new SolidBrush(m_clrColorValue), rect);
			e.Graphics.DrawRectangle(new Pen(m_clrColorSquareBorderColor, 1), rect);
		}
	}
}

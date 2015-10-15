// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.Widgets
{
	/// <summary>
	/// Control for entering passwords. Contains a password reveal eye that can be clicked and held to reveal the entered password.
	/// </summary>
	public partial class PasswordBox : TextBox
	{
		private PictureBox m_eye;
		private char m_PasswordChar = '\u2022';

		/// <summary/>
		public PasswordBox()
		{
			InitializeComponent();
			m_eye = new PictureBox();

			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PasswordBox));
			m_eye = new System.Windows.Forms.PictureBox();
			((System.ComponentModel.ISupportInitialize)(m_eye)).BeginInit();
			this.SuspendLayout();

			m_eye.Image = ((System.Drawing.Image)(resources.GetObject("password-reveal-eye-16x16")));
			m_eye.Name = "eyePicture";
			m_eye.Size = new System.Drawing.Size(16, 16);
			m_eye.TabIndex = 0;
			m_eye.TabStop = false;
			m_eye.Cursor = Cursors.Arrow;

			this.PasswordChar = m_PasswordChar;

			m_eye.MouseDown += RevealPassword;
			m_eye.MouseUp += HidePassword;

			this.Controls.Add(m_eye);
		}

		private void RevealPassword(object sender, MouseEventArgs mouseEventArgs)
		{
			this.PasswordChar = '\0';
		}

		private void HidePassword(object sender, MouseEventArgs e)
		{
			this.PasswordChar = m_PasswordChar;
		}

		/// <summary/>
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			var textBoxWidth = this.Size.Width;
			var fudge = 8;
			m_eye.Location = new Point(textBoxWidth - m_eye.Width - fudge, 0);
		}
	}
}

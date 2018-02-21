// Copyright (c) 2014-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Windows.Forms;

namespace LanguageExplorer.Impls
{
	/// <summary>
	/// Control for entering passwords. Contains a password reveal eye that can be clicked and held to reveal the entered password.
	/// </summary>
	public partial class PasswordBox : TextBox
	{
		private PictureBox m_eye;
		private char m_PasswordChar = '\u2022';

		/// <summary />
		public PasswordBox()
		{
			InitializeComponent();
			m_eye = new PictureBox();
			m_eye = new PictureBox();
			((System.ComponentModel.ISupportInitialize)(m_eye)).BeginInit();
			SuspendLayout();

			m_eye.Image = LanguageExplorerResources.password_reveal_eye_16x16;
			m_eye.Name = "eyePicture";
			m_eye.Size = new Size(16, 16);
			m_eye.TabIndex = 0;
			m_eye.TabStop = false;
			m_eye.Cursor = Cursors.Arrow;

			PasswordChar = m_PasswordChar;

			m_eye.MouseDown += RevealPassword;
			m_eye.MouseUp += HidePassword;

			Controls.Add(m_eye);
		}

		private void RevealPassword(object sender, MouseEventArgs mouseEventArgs)
		{
			PasswordChar = '\0';
		}

		private void HidePassword(object sender, MouseEventArgs e)
		{
			PasswordChar = m_PasswordChar;
		}

		/// <summary/>
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			var textBoxWidth = Size.Width;
			const int fudge = 8;
			m_eye.Location = new Point(textBoxWidth - m_eye.Width - fudge, 0);
		}
	}
}

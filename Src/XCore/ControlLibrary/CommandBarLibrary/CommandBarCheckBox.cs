// ---------------------------------------------------------
// Windows Forms CommandBar Control
// Copyright (C) 2001-2003 Lutz Roeder. All rights reserved.
// http://www.aisto.com/roeder
// roeder@aisto.com
// ---------------------------------------------------------
namespace Reflector.UserInterface
{
	using System;
	using System.ComponentModel;
	using System.Drawing;
	using System.Windows.Forms;

	public class CommandBarCheckBox : CommandBarButtonBase
	{
		private bool isChecked = false;

		public CommandBarCheckBox(string text) : base(text)
		{
		}

		public CommandBarCheckBox(Image image, string text) : base(image, text)
		{
		}

		public bool IsChecked
		{
			set
			{
				if (value != this.isChecked)
				{
					this.isChecked = value;
					this.OnPropertyChanged(new PropertyChangedEventArgs("IsChecked"));
				}
			}

			get { return this.isChecked; }
		}

		protected override void OnClick(EventArgs e)
		{
			this.IsChecked = !this.IsChecked;
			base.OnClick(e);
		}

		public override string ToString()
		{
			return "CheckBox(" + this.Text + "," + this.IsChecked + ")";
		}
	}
}

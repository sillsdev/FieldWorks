// ---------------------------------------------------------
// Windows Forms CommandBar Control
// Copyright (C) 2001-2003 Lutz Roeder. All rights reserved.
// http://www.aisto.com/roeder
// roeder@aisto.com
// ---------------------------------------------------------
namespace Reflector.UserInterface
{
	using System;
	using System.Drawing;
	using System.Collections;
	using System.ComponentModel;
	using System.Windows.Forms;

	public class CommandBarItem : Component
	{
		public event PropertyChangedEventHandler PropertyChanged;

		private Image image = null;
		private string text = null;
		private bool isEnabled = true;
		private bool isVisible = true;
		private object tag;

		private CommandBarItem()
		{
		}

		public CommandBarItem(string text)
		{
			this.text = text;
		}

		public CommandBarItem(Image image)
		{
			this.image = image;
		}

		public CommandBarItem(Image image, string text)
		{
			this.text = text;
			this.image = image;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				this.image = null;
				this.text = null;
			}

			base.Dispose(disposing);
		}

		public virtual object Tag
		{
			set
			{
				this.tag = value;
			}

			get
			{
				return this.tag;
			}
		}

		public virtual bool IsVisible
		{
			set
			{
				if (value != this.isVisible)
				{
					this.isVisible = value;
					this.OnPropertyChanged(new PropertyChangedEventArgs("IsVisible"));
				}
			}

			get
			{
				return this.isVisible;
			}
		}

		public virtual bool IsEnabled
		{
			set
			{
				if (this.isEnabled != value)
				{
				  this.isEnabled = value;
				  this.OnPropertyChanged(new PropertyChangedEventArgs("IsEnabled"));
				}
			}

			get
			{
				return this.isEnabled;
			}
		}

		public Image Image
		{
			set
			{
				if (value != this.image)
				{
					this.image = value;
					this.OnPropertyChanged(new PropertyChangedEventArgs("Image"));
				}
			}

			get
			{
				return this.image;
			}
		}

		public string Text
		{
			set
			{
				if (value != this.text)
				{
					this.text = value;
					this.OnPropertyChanged(new PropertyChangedEventArgs("Text"));
				}
			}

			get { return this.text; }
		}

		protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			if (this.PropertyChanged != null)
			{
				this.PropertyChanged(this, e);
			}
		}
	}
}

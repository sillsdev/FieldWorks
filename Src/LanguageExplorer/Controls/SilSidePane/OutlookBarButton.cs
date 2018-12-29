// SilSidePane, Copyright 2009-2019 SIL International. All rights reserved.
// SilSidePane is licensed under the Code Project Open License (CPOL), <http://www.codeproject.com/info/cpol10.aspx>.
// Derived from OutlookBar v2 2005 <http://www.codeproject.com/KB/vb/OutlookBar.aspx>, Copyright 2007 by Star Vega.
// Changed in 2008 and 2009 by SIL International to convert to C# and add more functionality.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;

namespace LanguageExplorer.Controls.SilSidePane
{
	/// <summary />
	[DesignTimeVisible(false), DefaultProperty("Text")]
	internal class OutlookBarButton : IDisposable
	{
		private OutlookBar _owner;
		private bool _disposeOwner;
		private bool _visible = true;
		private bool _allowed = true;
		private Image _image;
		private bool _disposeImage;
		private bool _selected;

		/// <summary />
		public OutlookBarButton()
		{
			_owner = new OutlookBar();
			_disposeOwner = true;
		}

		/// <summary />
		public OutlookBarButton(string text, Image image) : this()
		{
			Text = text;
			Image = image;
		}

		/// <summary />
		internal OutlookBarButton(OutlookBar owner)
		{
			_owner = owner;
		}

		~OutlookBarButton()
		{
			Dispose(false);
		}

		//The ButtonClass is not inheriting from Control, so I need this destructor...

		/// <summary />
		private bool IsDisposed { get; set; }

		// IDisposable
		/// <summary></summary>
		protected virtual void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ****** ");
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
				// free managed and unmanaged resources when explicitly called
				if (_disposeOwner)
				{
					_owner?.Dispose();
				}

				if (_disposeImage)
				{
					_image?.Dispose();
				}
			}
			// free unmanaged resources
			_owner = null;
			_image = null;

			IsDisposed = true;
		}

		/// <summary />
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary />
		internal bool IsLarge { get; set; }

		/// <summary />
		internal Rectangle Rectangle { get; set; }

		/// <summary />
		internal ButtonState State { get; } = ButtonState.Passive;

		//This field lets us react with the parent control.
		/// <summary />
		internal OutlookBar Owner
		{
			get { return _owner; }
			set
			{
				if (_disposeOwner)
				{
					_owner?.Dispose();
				}
				_disposeOwner = false;
				_owner = value;
			}
		}

		/// <summary />
		public string Text { get; set; }

		/// <summary />
		[DefaultValue(typeof(bool), "True")]
		public bool Visible
		{
			get { return _visible; }
			set
			{
				_visible = value;
				if (!value)
				{
					Rectangle = Rectangle.Empty;
				}
			}
		}

		/// <summary />
		[DefaultValue(typeof(bool), "False"), Browsable(false)]
		public bool Selected
		{
			get { return _selected; }
			set
			{
				_selected = value;
				switch (value)
				{
					case true:
						Owner.SelectedButton = this;
						break;
					case false:
						Owner.SelectedButton = null;
						break;
				}
				Owner.SetSelectionChanged(this);
			}
		}

		/// <summary />
		[DefaultValue(typeof(bool), "True")]
		public bool Allowed
		{
			get { return _allowed; }
			set
			{
				_allowed = value;
				if (!value)
				{
					Visible = false;
				}
			}
		}

		/// <summary />
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
		public Image Image
		{
			get
			{
				if (_image == null)
				{
					_image = LanguageExplorerResources.DefaultIcon.ToBitmap();
					_disposeImage = true;
				}
				return _image;
			}
			set
			{
				if (_image != null && _disposeImage)
				{
					_image.Dispose();
					_disposeImage = false;
				}
				_image = value;
			}
		}

		/// <summary />
		[DefaultValue(typeof(bool), "True")]
		public bool Enabled { get; set; }

		/// <summary>
		/// A place where clients can store arbitrary data associated with this item.
		/// </summary>
		public object Tag { get; set; }

		/// <summary />
		public string Name { get; set; }

		/// <summary />
		public override string ToString()
		{
			return Text;
		}
	}
}
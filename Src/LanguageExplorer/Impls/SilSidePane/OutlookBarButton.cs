// Copyright (c) 2016 SIL International
// SilOutlookBar is licensed under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;

namespace LanguageExplorer.Impls.SilSidePane
{
	/// <summary />
	[DesignTimeVisible(false), DefaultProperty("Text")]
	internal sealed class OutlookBarButton : IDisposable
	{
		private OutlookBar _owner;
		private bool _disposeOwner;
		private bool _visible = true;
		private bool _allowed = true;
		private Image _image;
		private bool _disposeImage;
		private bool _selected;

		/// <summary />
		internal OutlookBarButton()
		{
			_owner = new OutlookBar();
			_disposeOwner = true;
		}

		/// <summary />
		internal OutlookBarButton(string text, Image image) : this()
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
		private void Dispose(bool disposing)
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
			get => _owner;
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
		internal string Text { get; set; }

		/// <summary />
		[DefaultValue(typeof(bool), "True")]
		internal bool Visible
		{
			get => _visible;
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
		internal bool Selected
		{
			get => _selected;
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
		internal bool Allowed
		{
			get => _allowed;
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
		internal Image Image
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
		internal bool Enabled { get; set; }

		/// <summary>
		/// A place where clients can store arbitrary data associated with this item.
		/// </summary>
		internal object Tag { get; set; }

		/// <summary />
		internal string Name { get; set; }

		/// <summary />
		public override string ToString()
		{
			return Text;
		}
	}
}
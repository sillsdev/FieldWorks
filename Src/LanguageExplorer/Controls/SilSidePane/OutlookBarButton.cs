// SilSidePane, Copyright 2009-2018 SIL International. All rights reserved.
// SilSidePane is licensed under the Code Project Open License (CPOL), <http://www.codeproject.com/info/cpol10.aspx>.
// Derived from OutlookBar v2 2005 <http://www.codeproject.com/KB/vb/OutlookBar.aspx>, Copyright 2007 by Star Vega.
// Changed in 2008 and 2009 by SIL International to convert to C# and add more functionality.

using System;
using System.ComponentModel;
using System.Drawing;

namespace LanguageExplorer.Controls.SilSidePane
{
	/// <summary />
	[DesignTimeVisible(false), DefaultProperty("Text")]
	internal class OutlookBarButton : IDisposable
	{
		private OutlookBar _owner;
		private bool _disposeOwner;
		/// <summary />
		internal ButtonState State = ButtonState.Passive;

		private bool _Visible = true;
		private bool _Allowed = true;
		private Image _Image;
		private bool _disposeImage;
		/// <summary />
		internal Rectangle Rectangle;
		/// <summary />
		internal bool isLarge; // If tab is expanded with text and not collapsed as an icon to the bottom
		private bool _Selected;

		#region " Constructors "

		//Includes a constructor without parameters so the control can be configured during design-time.

		/// <summary />
		public OutlookBarButton()
		{
			_owner = new OutlookBar();
			_disposeOwner = true;
		}

		/// <summary />
		public OutlookBarButton(string text, Image image): this()
		{
			Text = text;
			Image = image;
		}

		/// <summary />
		internal OutlookBarButton(OutlookBar owner)
		{
			_owner = owner;
		}

		#endregion

		#region " Destructor "
#if DEBUG
		~OutlookBarButton()
		{
			Dispose(false);
		}
#endif

		//The ButtonClass is not inheriting from Control, so I need this destructor...

		/// <summary />
		public bool IsDisposed { get; private set; }

		// IDisposable
		/// <summary></summary>
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ****** ");
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
					_Image?.Dispose();
				}
			}

			// free unmanaged resources
			_owner = null;
			_Image = null;

			IsDisposed = true;
		}

		#region " IDisposable Support "
		/// <summary />
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		#endregion

		#endregion

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
			get { return _Visible; }
			set
			{
				_Visible = value;
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
			get { return _Selected; }
			set
			{
				_Selected = value;
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
			get { return _Allowed; }
			set
			{
				_Allowed = value;
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
				if (_Image == null)
				{
					_Image = LanguageExplorerResources.DefaultIcon.ToBitmap();
					_disposeImage = true;
				}
				return _Image;
				}
			set
			{
				if (_Image != null && _disposeImage)
				{
					_Image.Dispose();
					_disposeImage = false;
				}
				_Image = value;
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

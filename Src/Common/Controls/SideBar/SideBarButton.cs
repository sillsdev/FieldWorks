// -------------------------------------------------------------------------------------------
// <copyright from='2002' to='2002' company='SIL International'>
//    Copyright (c) 2002, SIL International. All Rights Reserved.
// </copyright>
//
// File: SideBarButton.cs
// Responsibility: EberhardB
// Last reviewed:
//
// <remarks>
// Implementation of SideBarButton. This is the button that is used for displaying on the
// SideBar.
// </remarks>
// -------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;


namespace SIL.FieldWorks.Common.Controls
{
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// The SideBarButton is a specialized version of the FwButton.
	/// </summary>
	/// <remarks>We set the PropertyTab attribute, so that the Events tab shows in the PropertyGrid
	/// </remarks>
	/// ------------------------------------------------------------------------------------
	[ToolboxItem(false)]
	[Designer("SIL.FieldWorks.Common.Controls.Design.SideBarButtonDesigner")]
	[PropertyTab(typeof(System.Windows.Forms.Design.EventsTab), PropertyTabScope.Component)]
	[DefaultProperty("Text")]
	[DefaultEvent("Click")]
	public class SideBarButton : FwButton
	{
		#region Constructor, Dispose
		private System.ComponentModel.IContainer components = null;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the SideBarButton class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SideBarButton()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();

			// Set new default values
			Anchor = Anchor | AnchorStyles.Right;

			BorderDarkColor = SystemColors.ControlDarkDark;
			BorderLightestColor = SystemColors.ControlLight;
			ForeColor = SystemColors.ControlLightLight;

			TextInButton = false;
			ButtonFillsControl = false;
			ButtonToggles = true;
			m_behaveLikeOptionButton = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#endregion

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
		}
		#endregion

		#region Member variables
		//************************************************************************************
		private ImageList m_imageListLarge = null;
		private ImageList m_imageListSmall = null;
		private int m_heightLarge = SideBar.kDefaultHeightLarge;
		private int m_heightSmall = SideBar.kDefaultHeightSmall;

		/// <summary>
		/// Default height of the button if showing large icons
		/// </summary>
		const int kDefaultButtonHeightLarge = 38;
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the ImageList that is used for large buttons (large bitmap with
		/// text below)
		/// </summary>
		/// <remarks>Image should be 32x32 pixels</remarks>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		public ImageList ImageListLarge
		{
			get
			{
				CheckDisposed();
				return m_imageListLarge;
			}
			set
			{
				CheckDisposed();
				m_imageListLarge = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the ImageList that is used for small buttons (small bitmap with
		/// text to the right)
		/// </summary>
		/// <remarks>Image should be 16x16 pixels</remarks>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		public ImageList ImageListSmall
		{
			get
			{
				CheckDisposed();
				return m_imageListSmall;
			}
			set
			{
				CheckDisposed();
				m_imageListSmall = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the height of the button if displayed with large icon
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DefaultValue(SideBar.kDefaultHeightLarge)]
		public int HeightLarge
		{
			get
			{
				CheckDisposed();
				return m_heightLarge;
			}
			set
			{
				CheckDisposed();
				m_heightLarge = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the height of the button if displayed with small icon
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DefaultValue(SideBar.kDefaultHeightSmall)]
		public int HeightSmall
		{
			get
			{
				CheckDisposed();
				return m_heightSmall;
			}
			set
			{
				CheckDisposed();
				m_heightSmall = value;
			}
		}

		#endregion

		#region Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows large or small icons
		/// </summary>
		/// <param name="fLarge"><c>true</c> to show large icons.</param>
		/// ------------------------------------------------------------------------------------
		public void ShowLargeIcons(bool fLarge)
		{
			CheckDisposed();

			if (fLarge)
			{
				// Show large icons
				ImageList = m_imageListLarge;
				TextPosition = TextLocation.Below;
				TextAlign = ContentAlignment.MiddleCenter;
				if (ImageList != null)
				{
					// we use Padding.Left for width and height so that we get a square.
					ButtonSize = new Size(ImageList.ImageSize.Width + 2 * Padding.Left, ImageList.ImageSize.Height + 2 * Padding.Left);
				}
				else
					ButtonSize = new Size(kDefaultButtonHeightLarge, kDefaultButtonHeightLarge);
				Height = HeightLarge;
			}
			else
			{
				// Show small icons
				ImageList = m_imageListSmall;
				TextPosition = TextLocation.Right;
				TextAlign = ContentAlignment.MiddleLeft;
				if (ImageList != null)
				{
					// we use Padding.Left for width and height so that we get a square.
					ButtonSize = new Size(ImageList.ImageSize.Width + 2 * Padding.Left, ImageList.ImageSize.Height + 2 * Padding.Left);
				}
				else
					ButtonSize = new Size(SideBar.kDefaultHeightSmall, SideBar.kDefaultHeightSmall);
				Height = HeightSmall;
			}
			Invalidate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Do additional width adjustment
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnResize(EventArgs e)
		{
			if (Parent != null)
			{
				// adjust the width. This is necessary because if the tab was collapsed when
				// the size was changed, our width is suddenly way to large after expanding.
				// Seems to be a bug in .NET framework.
				Width = Parent.Width - 2 * Padding.Left;
			}
			base.OnResize(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulate a click on the button, even if the button is not visible.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new void PerformClick()
		{
			CheckDisposed();

			if (Visible)
				base.PerformClick();
			else
				OnClick(EventArgs.Empty);
		}

		#endregion
	}
}

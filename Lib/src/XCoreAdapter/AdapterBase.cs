// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
// </copyright>
#endregion
//
// File: AdapterBase.cs
// Authorship History: Randy Regnier
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections;
using System.Diagnostics;
using System.ComponentModel;

using DevComponents.DotNetBar;

using SIL.Utils; // for ImageCollection

namespace XCore
{
	/// <summary>
	/// Base class for all adapters for the DotNetBar library.
	/// </summary>
	public abstract class AdapterBase : IUIAdapter
	{
		#region Data members

		/// <summary>
		/// The XCore mediator.
		/// </summary>
		protected Mediator m_mediator;
		/// <summary>
		/// The main XWindow form.
		/// </summary>
		protected Form m_window;
		/// <summary>
		/// Collection of small images.
		/// </summary>
		protected ImageCollection m_smallImages;
		/// <summary>
		/// Collection of large images.
		/// </summary>
		protected ImageCollection m_largeImages;
		/// <summary>
		/// The subclass specific main control that is given back to the adapter library client.
		/// </summary>
		protected System.Windows.Forms.Control m_control;

		#endregion Data members

		/// <summary>
		/// store the location/settings of various widgets so that we can restore them next time
		/// </summary>
		public virtual void PersistLayout()
		{
		}

		protected string SettingsPath()
		{
			string path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
			path = System.IO.Path.Combine(path,System.Windows.Forms.Application.CompanyName+"\\"+ System.Windows.Forms.Application.ProductName);
			System.IO.Directory.CreateDirectory(path);
			return path;
		}

		#region Properties

		/// <summary>
		/// Gets the main control that is given to the adapter library client.
		/// </summary>
		protected virtual Control MyControl
		{
			get { return m_control; }
		}

		/// <summary>
		/// The manager for whatever bars can be docked on any of the four main window edges.
		/// </summary>
		protected DotNetBarManager Manager
		{
			get
			{
				Debug.Assert(m_mediator != null);
				Debug.Assert(m_window != null);

				DotNetBarManager manager = (DotNetBarManager)m_mediator.PropertyTable.GetValue("DotNetBarManager");
				if (manager == null)
				{
					manager = new DotNetBarManager();
					m_mediator.PropertyTable.SetProperty("DotNetBarManager", manager);
					m_mediator.PropertyTable.SetPropertyPersistence("DotNetBarManager", false);

					// we only create a top dock site, since we don't want to allow docking of menus and toolbars to right,
					// left, and bottom right now

					m_window.SuspendLayout();
					manager.AlphaBlendShadow = true;
					manager.AlwaysShowFullMenus = false;
					manager.BarStream = new DotNetBarStreamer(manager);
					//manager.BottomDockSite = new DevComponents.DotNetBar.DockSite();
					manager.Images = m_smallImages.ImageList;
					manager.ImagesLarge = m_largeImages.ImageList;
					manager.ImagesMedium = null;
					//manager.LeftDockSite = new DevComponents.DotNetBar.DockSite();
					manager.MenuDropShadow = DevComponents.DotNetBar.eMenuDropShadow.SystemDefault;
					manager.ParentForm = m_window;
					manager.PopupAnimation = DevComponents.DotNetBar.ePopupAnimation.SystemDefault;
					//manager.RightDockSite = new DevComponents.DotNetBar.DockSite();
					manager.ShowCustomizeContextMenu = false;
					manager.ShowFullMenusOnHover = true;
					manager.ShowResetButton = false;
					manager.ShowShortcutKeysInToolTips = true;
					manager.ShowToolTips = true;

					if(	m_mediator.PropertyTable.GetBoolProperty("UseOffice2003Style", false))
					{
						manager.ThemeAware = false;
						manager.Style = eDotNetBarStyle.Office2003;
					}
					else
					{
						manager.ThemeAware = true;
						manager.Style = eDotNetBarStyle.OfficeXP;
					}
					manager.UseHook = false;
					manager.TopDockSite = new DevComponents.DotNetBar.DockSite();
					Size clientSize = m_window.ClientSize;
					////
					//// barLeftDockSite
					////
					//DockSite ds = manager.LeftDockSite;
					//ds.Dock = System.Windows.Forms.DockStyle.Left;
					//ds.Name = "barLeftDockSite";
					//ds.Size = new System.Drawing.Size(0, clientSize.Height);
					//ds.TabIndex = 0;
					//ds.TabStop = false;
					////
					//// barRightDockSite
					////
					//ds = manager.RightDockSite;
					//ds.Dock = System.Windows.Forms.DockStyle.Right;
					//ds.Location = new System.Drawing.Point(clientSize.Width, 0);
					//ds.Name = "barRightDockSite";
					//ds.Size = new System.Drawing.Size(0, clientSize.Height);
					//ds.TabIndex = 1;
					//ds.TabStop = false;
					//
					// barTopDockSite
					//
					DockSite ds = manager.TopDockSite;
					ds.Dock = System.Windows.Forms.DockStyle.Top;
					ds.Name = "barTopDockSite";
					ds.Size = new System.Drawing.Size(clientSize.Width, 0);
					ds.TabIndex = 2;
					ds.TabStop = false;
					////
					//// barBottomDockSite
					////
					//ds = manager.BottomDockSite;
					//ds.Dock = System.Windows.Forms.DockStyle.Bottom;
					//ds.Location = new System.Drawing.Point(0, clientSize.Height);
					//ds.Name = "barBottomDockSite";
					//ds.Size = new System.Drawing.Size(clientSize.Width, 0);
					//ds.TabIndex = 3;
					//ds.TabStop = false;
					//
					// Main form
					//
					m_window.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
					m_window.Controls.Add(manager.TopDockSite);
					//m_window.Controls.AddRange(new System.Windows.Forms.Control[] {
					//															  manager.LeftDockSite,
					//															  manager.RightDockSite,
					//															  manager.TopDockSite,
					//															  manager.BottomDockSite});

					m_window.ResumeLayout(false);
				}
				return manager;
			}
		}


		#endregion Properties

		#region Construction

		/// <summary>
		/// Constructor.
		/// </summary>
		public AdapterBase()
		{
		}

		#endregion Construction

		#region IUIAdapter implementation

		/// <summary>
		/// Initializes the adapter.
		/// </summary>
		/// <param name="window">The main form.</param>
		/// <param name="smallImages">Collection of small images.</param>
		/// <param name="largeImages">Collection of large images.</param>
		/// <param name="mediator">XCore Mediator.</param>
		/// <returns>A Control for use by client.</returns>
		public virtual System.Windows.Forms.Control Init(System.Windows.Forms.Form window,
			ImageCollection smallImages, ImageCollection largeImages, Mediator mediator)
		{
			m_window = window;
			m_smallImages = smallImages;
			m_largeImages = largeImages;
			m_mediator = mediator;
			if(this is IxCoreColleague)
				((IxCoreColleague)this).Init(mediator, null/*I suppose we could get these to the adapter if someone needs that someday*/);
			return MyControl;
		}

		/// <summary>
		/// Implement do-nothing method to keep the compiler happy.
		/// Subclasses override this method to do useful things.
		/// </summary>
		/// <param name="groupCollection">Collection of choices.</param>
		public virtual void CreateUIForChoiceGroupCollection(ChoiceGroupCollection groupCollection)
		{
		}


		/// <summary>
		/// Implement a do-nothing method to keep the compiler happy.
		/// </summary>
		/// <param name="group">The group that is the basis for this menu</param>
		public virtual void CreateUIForChoiceGroup(ChoiceGroup group)
		{
		}

		/// <summary>
		/// Implement a do-nothing method to keep the compiler happy.
		/// </summary>
		public virtual void OnIdle()
		{
		}

		/// <summary>
		/// Implement a do-nothing method to keep the compiler happy.
		/// </summary>
		public virtual void FinishInit()
		{
		}

		#endregion IUIAdapter implementation
	}
}

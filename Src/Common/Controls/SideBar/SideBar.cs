// --------------------------------------------------------------------------------------------
// <copyright from='2002' to='2002' company='SIL International'>
//    Copyright (c) 2002, SIL International. All Rights Reserved.
// </copyright>
//
// File: SideBar.cs
// Responsibility: EberhardB
// Last reviewed:
//
// <remarks>
// Implementation of SideBar.
// Documentation for it can be found in \fw\Doc\FW.Net\CommonControls.mht.
// </remarks>
// --------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Diagnostics;

using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// This class implements a SideBar where you e.g. can switch between different views.
	/// Documentation for it can be found in <see href="\fw\Doc\FW.Net\CommonControls.mht"/>
	/// </summary>
	[ToolboxBitmap(typeof(SideBar), "resources.SideBar.bmp")]
	[Designer("SIL.FieldWorks.Common.Controls.Design.SideBarDesigner")]
	[DefaultProperty("Tabs")]
	public class SideBar : UserControl, IFWDisposable, ISettings
	{
		#region Member variables
		/// <summary>
		/// Currently active tab
		/// </summary>
		protected int m_iActive = 0;

		private SIL.FieldWorks.Common.Controls.Persistence m_persistence;
		private SIL.FieldWorks.Common.Drawing.BorderDrawing m_borderDrawing;
		private System.ComponentModel.IContainer components;

		private bool m_ActiveTabIsExclusive = true;

		#endregion

		#region Constructor, Dispose and designer generated stuff
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new SideBar object
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SideBar()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitForm call
			this.Dock = DockStyle.Left;

			if (m_fPersist)
				InitializePersistence();
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
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
				if( components != null )
					components.Dispose();
			}
			base.Dispose( disposing );
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the persistence object. We can't do it in InitializeComponent
		/// because we have the <see cref="Persist"/> property that allows not
		/// to persist.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializePersistence()
		{
			m_persistence = new SIL.FieldWorks.Common.Controls.Persistence(components);
			m_persistence.Parent = this;
			m_persistence.EnableSaveWindowSettings = false;
			m_persistence.SaveSettings += new SIL.FieldWorks.Common.Controls.Persistence.Settings(SaveSettings);
			m_persistence.LoadSettings += new SIL.FieldWorks.Common.Controls.Persistence.Settings(LoadSettings);
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SideBar));
			this.m_borderDrawing = new SIL.FieldWorks.Common.Drawing.BorderDrawing(this.components);
			this.SuspendLayout();
			//
			// m_borderDrawing
			//
			this.m_borderDrawing.BorderDarkColor = System.Drawing.SystemColors.ControlDarkDark;
			this.m_borderDrawing.BorderDarkestColor = System.Drawing.SystemColors.ControlDark;
			this.m_borderDrawing.BorderLightColor = System.Drawing.SystemColors.ControlLightLight;
			this.m_borderDrawing.BorderLightestColor = System.Drawing.SystemColors.ControlLight;
			this.m_borderDrawing.Graphics = null;
			//
			// SideBar
			//
			this.BackColor = System.Drawing.SystemColors.ControlDark;
			this.Name = "SideBar";
			resources.ApplyResources(this, "$this");
			this.ResumeLayout(false);

		}
		#endregion
		#endregion

		#region Overriden functions and event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Paint event
		/// </summary>
		/// <param name="e">A PaintEventArgs that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			Rectangle rect = new Rectangle(0, 0, Width, Height);
			m_borderDrawing.Draw(e.Graphics, rect, SIL.FieldWorks.Common.Drawing.BorderTypes.DoubleSunken);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If layout of tabs was delayed because of SuspendLayout(), we have to do it now.
		/// Also we have to set the width of the tabs to our width.
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnLayout(LayoutEventArgs e)
		{
			base.OnLayout(e);

			PositionTabs();
			Invalidate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The tab is about to be inserted to the task bar tab control. Insert it also to the
		/// controls collection..
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void OnTabInserting(int index, object value)
		{
			SideBarTab tab = value as SideBarTab;

			if (tab != null)
			{
				tab.CalculateDispRect();
				tab.Left = SystemInformation.Border3DSize.Width;
				tab.Width = this.Width - 2 * SystemInformation.Border3DSize.Width;
				tab.Activate += new SideBarTab.ActivateEventHandler(ActivateTab);
				tab.Persist = m_fPersist;
				tab.Collapse();

				Controls.Add(tab);
				Controls.SetChildIndex(tab, Tabs.Count);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the active tab.
		/// </summary>
		///
		/// <param name='key'>The Registry Key.</param>
		/// ------------------------------------------------------------------------------------
		protected void LoadSettings(RegistryKey key)
		{
			IRegistryKeyNameModifier modifier = Parent as IRegistryKeyNameModifier;
			if (modifier != null)
				key = modifier.ModifyKey(key, false);
			ActivateTab((int)key.GetValue(Name + "ActiveTab", m_iActive));
			Width = (int)key.GetValue(Name + "Width", Width);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save the active tab.
		/// </summary>
		///
		/// <param name='key'>The Registry Key.</param>
		/// ------------------------------------------------------------------------------------
		protected void SaveSettings(RegistryKey key)
		{
			IRegistryKeyNameModifier modifier = Parent as IRegistryKeyNameModifier;
			if (modifier != null)
				key = modifier.ModifyKey(key, true);
			key.SetValue(Name + "ActiveTab", m_iActive);
			key.SetValue(Name + "Width", Width);
		}
		#endregion

		#region Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjust the position (and height) of each tab
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void PositionTabs()
		{
			if (Tabs.Count < 1)
				return;

			if (Tabs.Count <= m_iActive)
				m_iActive = 0;

			// Height of deactivated (collapsed) tab
			int nMinHeight = Tabs[0].MinHeight;

			// Calculate the space that the active tab can have:
			// height of parent control - No of inactive tabs * inactive tabs height
			int nHeight = (Height - 2 * SystemInformation.Border3DSize.Height) - (Tabs.Count-1) * nMinHeight;

			// adjust height and position of previous tabs, and set width to our width
			for (int i = 0; i < Tabs.Count; i++)
			{
				Tabs[i].Left = SystemInformation.Border3DSize.Width;
				Tabs[i].Width = Width - 2 * SystemInformation.Border3DSize.Width;

				if (i < m_iActive)
					Tabs[i].Top = i * nMinHeight + SystemInformation.Border3DSize.Height;
				else if (i == m_iActive)
				{
					Tabs[i].Top = i * nMinHeight + SystemInformation.Border3DSize.Height;
					Tabs[i].Expand(nHeight);
				}
				else // i > m_iActive
				{
					// because nHeight includes nMinHeight for Tabs[m_iActive], we have to use i-1
					Tabs[i].Top = nHeight + (i - 1) * nMinHeight + SystemInformation.Border3DSize.Height;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Activates the given SideBarTab. This method is an event handler for a SideBarTab's
		/// Activate event (a custom event).
		/// </summary>
		/// <param name="tab">The new active SideBarTab</param>
		/// ------------------------------------------------------------------------------------
		public void ActivateTab(SideBarTab tab)
		{
			CheckDisposed();

			int nTab = Tabs.IndexOf(tab);
			ActivateTab(nTab);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Activates the given SideBarTab. This involves deactivating (collapsing) the old
		/// active one and expanding the new one.
		/// </summary>
		/// <param name="iTab">Index of tab to activate</param>
		/// ------------------------------------------------------------------------------------
		public void ActivateTab(int iTab)
		{
			CheckDisposed();

			if (m_iActive != iTab)
			{
				// Collapse the previous tab and expand the new tab.
				int iActivePrevious = m_iActive;
				m_iActive = iTab;

				SuspendLayout();
				Tabs[iActivePrevious].Collapse();
				PositionTabs();
				ResumeLayout(true);

				// In the ActiveTabIsExclusive mode, simple activation of a tab also involves
				//  clicking on the button that was previously pressed (but not visible until now).
				if (ActiveTabIsExclusive)
					Tabs[m_iActive].ClickPressedButton();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Activates the given SideBarButton and perhaps the SideBarTab, if appropriate. This
		/// is called when the View menu item is clicked.
		/// </summary>
		/// <param name="tab">The SideBarTab to make active, if appropriate</param>
		/// <param name="button">The SideBarButton to make active</param>
		/// ------------------------------------------------------------------------------------
		public void ActivateTabAndButton(SideBarTab tab, SideBarButton button)
		{
			CheckDisposed();

			int iTab = Tabs.IndexOf(tab);

			// if we're in ActiveTabIsExclusive mode...
			if (ActiveTabIsExclusive)
			{
				if (m_iActive != iTab)
				{
					// Collapse the previous tab and expand the new tab.
					int iActivePrevious = m_iActive;
					m_iActive = iTab;

					SuspendLayout();
					Tabs[iActivePrevious].Collapse();
					PositionTabs();
					ResumeLayout(true);
				}

			}

			// Press the specified button in this tab and clear all others.
			// REVIEW: What should this do in multiSelect mode when we
			//  want to preserve the state of all the buttons? We would
			//  want to press only the specified button.
			foreach(SideBarButton btn in Tabs[m_iActive].Buttons)
				btn.PressButton(btn == button);

			button.PerformClick();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save the persisted settings now.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SaveSettingsNow()
		{
			CheckDisposed();

			if (m_persistence != null)
				m_persistence.SaveSettingsNow(this);
		}

		#endregion

		#region Variables used for Properties
		private SideBarTabCollection m_tabs = null;
		private ImageList m_imageListLarge = null;
		private ImageList m_imageListSmall = null;
		private int m_heightLarge = kDefaultHeightLarge;
		private int m_heightSmall = kDefaultHeightSmall;
		private bool m_fPersist = true;

		/// <summary>
		/// Default height of the control if showing large icons
		/// </summary>
		public const int kDefaultHeightLarge = 60;
		/// <summary>
		/// Default height of the control if showing small icons
		/// </summary>
		public const int kDefaultHeightSmall = 22;
		#endregion

		#region Properties & Events
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the menu extender for the side bar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public MenuExtender MenuExtender
		{
			set
			{
				CheckDisposed();

				foreach (SideBarTab tab in Tabs)
					tab.MenuExtender = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets whether only one tab may be operational at once.
		/// If true, the active tab excludes the operation of all buttons in the other tabs.
		/// For example, in TE the tabs are sub-groupings of user views, and only one
		/// user view can be active at one time.
		/// If false, the active button(s) in every tab are operational. For example, in
		/// Data Notebook, the choices for View, Filter and Sort are all simultaneously
		/// active.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ActiveTabIsExclusive
		{
			get
			{
				CheckDisposed();
				return m_ActiveTabIsExclusive;
			}
			set
			{
				CheckDisposed();
				m_ActiveTabIsExclusive = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the active tab.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int ActiveTabIndex
		{
			get
			{
				CheckDisposed();
				return m_iActive;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the active tab.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SideBarTab ActiveTab
		{
			get
			{
				CheckDisposed();
				return m_tabs[m_iActive];
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the access registry key for the SideBar
		/// </summary>
		/// <value>The registry key for the SideBar</value>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		public RegistryKey SettingsKey
		{
			get
			{
				CheckDisposed();
				return (Parent is ISettings ? ((ISettings)Parent).SettingsKey : null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a window creation option.
		/// </summary>
		/// <value>By default, returns false</value>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		public bool KeepWindowSizePos
		{
			get
			{
				CheckDisposed();
				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the collection with the tabs that will appear in the task bar.
		/// </summary>
		/// <value>The collection of tabs.</value>
		/// ------------------------------------------------------------------------------------
		[Category("Appearance")]
		[Description("The tabs that will appear in the task bar")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public SideBarTabCollection Tabs
		{
			get
			{
				CheckDisposed();

				if (m_tabs == null)
				{
					m_tabs = new SideBarTabCollection();
					m_tabs.BeforeInsert +=
						new SideBarTabCollection.CollectionChange(OnTabInserting);
				}

				return m_tabs;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the <c>ImageList</c> that is used for large buttons (large bitmap with
		/// text below)
		/// </summary>
		/// <value>The <c>ImageList</c> used for large buttons.</value>
		/// <remarks>Images should be 32x32 pixels</remarks>
		/// ------------------------------------------------------------------------------------
		[Category("Appearance")]
		[Description("The image list that will be used to display buttons with large icons.")]
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
		/// Gets or sets the <c>ImageList</c> that is used for small buttons (small bitmap with
		/// text to the right)
		/// </summary>
		/// <value>The <c>ImageList</c> used for small buttons.</value>
		/// <remarks>Image should be 16x16 pixels</remarks>
		/// ------------------------------------------------------------------------------------
		[Category("Appearance")]
		[Description("The image list that will be used to display buttons with small icons.")]
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
		/// Gets or sets the height of the buttons if displayed with large icon
		/// </summary>
		/// <value>Height of a large button</value>
		/// ------------------------------------------------------------------------------------
		[DefaultValue(kDefaultHeightLarge)]
		[Category("Layout")]
		[Description("The height of a button displayed with a large icon")]
		public int ButtonHeightLarge
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
		/// Gets or sets the height of the buttons if displayed with small icon
		/// </summary>
		/// <value>Height of a small button</value>
		/// ------------------------------------------------------------------------------------
		[DefaultValue(kDefaultHeightSmall)]
		[Category("Layout")]
		[Description("The height of a button displayed with a small icon")]
		public int ButtonHeightSmall
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the flag if the side bar should persist its settings to the registry.
		/// </summary>
		/// <value>Flag for persisting settings.</value>
		/// ------------------------------------------------------------------------------------
		[DefaultValue(true)]
		[Category("Behavior")]
		[Description("Determines if the control persists its settings to the registry.")]
		public bool Persist
		{
			get
			{
				CheckDisposed();
				return m_fPersist;
			}
			set
			{
				CheckDisposed();

				m_fPersist = value;
				if (!m_fPersist && m_persistence != null)
				{
					m_persistence.Dispose();
					m_persistence = null;
				}
				else if (m_fPersist && m_persistence == null)
					InitializePersistence();

				// set the persistence flag on all tabs
				foreach(SideBarTab tab in m_tabs)
					tab.Persist = value;
			}
		}

		#endregion

		#region ShouldSerialize/Reset methods (methods related to properties)
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if Tabs property should be serialized. This method has purely
		/// cosmetic affects. If the Buttons collection is empty, the property is not
		/// shown in bold in design mode.
		/// </summary>
		/// <returns>Returns true if collection contains any elements.</returns>
		/// ------------------------------------------------------------------------------------
		private bool ShouldSerializeTabs()
		{
			return m_tabs.Count > 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes all tabs
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ResetTabs()
		{
			m_tabs.Clear();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the ImageListLarge property should be serialized.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool ShouldSerializeImageListLarge()
		{
			return m_imageListLarge != null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resets the image list
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ResetImageListLarge()
		{
			m_imageListLarge = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if the ImageListSmall property should be serialized.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool ShouldSerializeImageListSmall()
		{
			return m_imageListSmall != null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resets the image list
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void ResetImageListSmall()
		{
			m_imageListSmall = null;
		}

		#endregion
	}
}

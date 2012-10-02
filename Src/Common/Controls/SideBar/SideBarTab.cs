// --------------------------------------------------------------------------------------------
// <copyright from='2002' to='2002' company='SIL International'>
//    Copyright (c) 2002, SIL International. All Rights Reserved.
// </copyright>
//
// File: SideBarTab.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// Implementation of SideBarTab
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	// TODO EberhardB: Find workaround for bugs below.
	// This code has (at least) two flaws at design-time:
	// 1. After adding a button to the collection and changing to code view, the following
	// error message is displayed: "Code generation for property 'Controls'
	// failed.  Error was: 'Object reference not set to an instance of an object.'" However,
	// all code is generated as expected. I couldn't figure out which Controls causes the
	// problems.
	// 2. When copying the control to the clipboard and pasting it, a second SideBarTab
	// object is inserted, but the buttons are all the same. This leads to problems: no moving
	// or resizing... of the new object. I suppose that this problem is related to the first one.
	// If the buttons are not added to the Controls collection, all works fine.
	//
	// Answer: Probably we have to use a different collection during initialization. Look at
	// Burkey's sample, and search on Internet for that.

	/// <summary>
	/// Implements one tab of the SideBar.
	/// </summary>
	/// <remarks>We set the PropertyTab attribute, so that the Events tab shows in the PropertyGrid
	/// </remarks>
	[ToolboxItem(false)]
	[Designer("SIL.FieldWorks.Common.Controls.Design.SideBarTabDesigner")]
	[PropertyTab(typeof(System.Windows.Forms.Design.EventsTab), PropertyTabScope.Component)]
	[DefaultProperty("Buttons")]
	[DefaultEvent("Configure")]
	public class SideBarTab : UserControl, IFWDisposable, ISettings
	{
		private SideBarButton m_pressedBtn;

		#region Designer added variables
		private FwButton btnTitle;
		private System.Windows.Forms.Button btnUp;
		private System.Windows.Forms.Button btnDown;
		private SideBarTabPanel scrollTab;
		private MenuItem mnuSmallIcons;
		private MenuItem mnuLargeIcons;
		private MenuItem mnuHideSideBar;
		private System.Windows.Forms.ContextMenu contextMenuSideBarTab;
		private MenuItem mnuSeparator2;
		private MenuItem mnuConfigure;
		private SIL.FieldWorks.Common.Controls.Persistence m_persistence;
		private SIL.FieldWorks.Common.Controls.MenuExtender m_menuExtender;
		private System.Windows.Forms.ImageList imgArrows;
		private System.Windows.Forms.MenuItem mnuSepr;
		private System.ComponentModel.IContainer components;
		#endregion

		#region Constructor, Dispose
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the SideBarTab class
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SideBarTab()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			if (m_fPersist)
				InitializePersistence();

			btnUp.Click += new System.EventHandler(this.scrollTab.OnScrollUp);
			btnDown.Click += new System.EventHandler(this.scrollTab.OnScrollDown);

			BackColor = SystemColors.Control;

			scrollTab.SetScrollButtons(btnUp, btnDown);
			scrollTab.Padding = Padding;
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
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#endregion

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SideBarTab));
			this.btnTitle = new SIL.FieldWorks.Common.Controls.FwButton();
			this.btnUp = new System.Windows.Forms.Button();
			this.scrollTab = new SIL.FieldWorks.Common.Controls.SideBarTabPanel();
			this.imgArrows = new System.Windows.Forms.ImageList(this.components);
			this.btnDown = new System.Windows.Forms.Button();
			this.contextMenuSideBarTab = new System.Windows.Forms.ContextMenu();
			this.mnuLargeIcons = new System.Windows.Forms.MenuItem();
			this.mnuSmallIcons = new System.Windows.Forms.MenuItem();
			this.mnuSepr = new System.Windows.Forms.MenuItem();
			this.mnuConfigure = new System.Windows.Forms.MenuItem();
			this.mnuSeparator2 = new System.Windows.Forms.MenuItem();
			this.mnuHideSideBar = new System.Windows.Forms.MenuItem();
			this.m_menuExtender = new SIL.FieldWorks.Common.Controls.MenuExtender(this.components);
			((System.ComponentModel.ISupportInitialize)(this.m_menuExtender)).BeginInit();
			this.SuspendLayout();
			//
			// btnTitle
			//
			resources.ApplyResources(this.btnTitle, "btnTitle");
			this.btnTitle.ButtonStyle = SIL.FieldWorks.Common.Controls.ButtonStyles.Raised;
			this.btnTitle.Dock = System.Windows.Forms.DockStyle.Top;
			this.btnTitle.Name = "btnTitle";
			this.btnTitle.SunkenAppearance = SIL.FieldWorks.Common.Controls.SunkenAppearances.Sunken;
			this.btnTitle.Click += new System.EventHandler(this.btnTitle_Click);
			//
			// btnUp
			//
			resources.ApplyResources(this.btnUp, "btnUp");
			this.btnUp.Dock = System.Windows.Forms.DockStyle.Top;
			this.btnUp.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
			this.btnUp.ImageIndex = 0;
			this.btnUp.ImageList = this.imgArrows;
			this.btnUp.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.btnUp.Name = "btnUp";
			//
			// scrollTab
			//
			resources.ApplyResources(this.scrollTab, "scrollTab");
			this.scrollTab.AutoScroll = true;
			this.scrollTab.AutoScrollMargin = new System.Drawing.Size(0, 10);
			this.scrollTab.BackColor = System.Drawing.SystemColors.ControlDark;
			this.scrollTab.Dock = System.Windows.Forms.DockStyle.Fill;
			this.scrollTab.Name = "scrollTab";
			//
			// imgArrows
			//
			this.imgArrows.ImageSize = new System.Drawing.Size(16, 16);
			this.imgArrows.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imgArrows.ImageStream")));
			this.imgArrows.TransparentColor = System.Drawing.Color.Magenta;
			//
			// btnDown
			//
			resources.ApplyResources(this.btnDown, "btnDown");
			this.btnDown.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.btnDown.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
			this.btnDown.ImageIndex = 1;
			this.btnDown.ImageList = this.imgArrows;
			this.btnDown.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.btnDown.Name = "btnDown";
			//
			// contextMenuSideBarTab
			//
			resources.ApplyResources(this.contextMenuSideBarTab, "contextMenuSideBarTab");
			this.contextMenuSideBarTab.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
																								  this.mnuLargeIcons,
																								  this.mnuSmallIcons,
																								  this.mnuSepr,
																								  this.mnuConfigure,
																								  this.mnuSeparator2,
																								  this.mnuHideSideBar});
			//
			// mnuLargeIcons
			//
			resources.ApplyResources(this.mnuLargeIcons, "mnuLargeIcons");
			this.mnuLargeIcons.Checked = true;
			this.m_menuExtender.SetImageIndex(this.mnuLargeIcons, -1);
			this.mnuLargeIcons.Click += new System.EventHandler(this.OnLargeIcons);
			//
			// mnuSmallIcons
			//
			resources.ApplyResources(this.mnuSmallIcons, "mnuSmallIcons");
			this.m_menuExtender.SetImageIndex(this.mnuSmallIcons, -1);
			this.mnuSmallIcons.Click += new System.EventHandler(this.OnSmallIcons);
			//
			// mnuSepr
			//
			resources.ApplyResources(this.mnuSepr, "mnuSepr");
			this.m_menuExtender.SetCommandId(this.mnuSepr, "SideBarConfigure");
			this.m_menuExtender.SetImageIndex(this.mnuSepr, -1);
			//
			// mnuConfigure
			//
			resources.ApplyResources(this.mnuConfigure, "mnuConfigure");
			this.m_menuExtender.SetCommandId(this.mnuConfigure, "SideBarConfigure");
			this.m_menuExtender.SetImageIndex(this.mnuConfigure, 2);
			this.m_menuExtender.SetImageList(this.mnuConfigure, this.imgArrows);
			this.mnuConfigure.Click += new System.EventHandler(this.OnConfigure);
			//
			// mnuSeparator2
			//
			resources.ApplyResources(this.mnuSeparator2, "mnuSeparator2");
			this.m_menuExtender.SetImageIndex(this.mnuSeparator2, -1);
			//
			// mnuHideSideBar
			//
			resources.ApplyResources(this.mnuHideSideBar, "mnuHideSideBar");
			this.m_menuExtender.SetImageIndex(this.mnuHideSideBar, -1);
			this.mnuHideSideBar.Click += new System.EventHandler(this.OnHideSideBar);
			//
			// m_menuExtender
			//
			this.m_menuExtender.DefaultHelpText = null;
			this.m_menuExtender.MenuStyle = SIL.FieldWorks.Common.Controls.MenuStyles.OfficeXP;
			this.m_menuExtender.Parent = null;
			//
			// SideBarTab
			//
			resources.ApplyResources(this, "$this");
			this.ContextMenu = this.contextMenuSideBarTab;
			this.Controls.Add(this.scrollTab);
			this.Controls.Add(this.btnDown);
			this.Controls.Add(this.btnUp);
			this.Controls.Add(this.btnTitle);
			this.Name = "SideBarTab";
			((System.ComponentModel.ISupportInitialize)(this.m_menuExtender)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion

		#region Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the persistence object
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializePersistence()
		{
			m_persistence = new SIL.FieldWorks.Common.Controls.Persistence(components);
			m_persistence.Parent = this;
			m_persistence.EnableSaveWindowSettings = false;
			m_persistence.SaveSettings += new Persistence.Settings(SaveSettings);
			m_persistence.LoadSettings += new Persistence.Settings(LoadSettings);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save the persisted settings now.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SaveSettingsNow()
		{
			CheckDisposed();

			if (m_fPersist)
				m_persistence.SaveSettingsNow(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Center all buttons on the control
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void CenterButtons()
		{
			foreach(SideBarButton btn in Buttons)
			{
				// adjust button width
				btn.Width = this.Width - Padding.Horizontal;
				btn.Left = Padding.Left;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show the SideBar tab
		/// </summary>
		/// <param name="height"></param>
		/// ------------------------------------------------------------------------------------
		public void Expand(int height)
		{
			CheckDisposed();

			SuspendLayout();
			Size = new Size(Width, height);
			scrollTab.Show();
			//scrollTab.ShowButtons();
			ResumeLayout();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Collapse the SideBar tab, so that only the title shows
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Collapse()
		{
			CheckDisposed();

			SuspendLayout();
			Size = new Size(Width, MinHeight);
			btnDown.Hide();
			btnUp.Hide();
			scrollTab.Hide();
			ResumeLayout();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calculate the display rectangle
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CalculateDispRect()
		{
			CheckDisposed();

			scrollTab.CalculateDispRect();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reposition the button according to its index and height
		/// </summary>
		/// <param name="index"></param>
		/// <param name="btn"></param>
		/// ------------------------------------------------------------------------------------
		protected void RepositionButton(int index, Button btn)
		{
			//we assume each button has the same height
			btn.Top = index * (btn.Height + Padding.Vertical) + Padding.Top;
		}
		#endregion

		#region Suspend/Resume Layout, which also handles Button States and Activation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Temporarily suspends the layout logic for the control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new void SuspendLayout()
		{
			CheckDisposed();

			m_nSuspendedLayout++;
			base.SuspendLayout();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resumes normal layout logic. Optionally forces an immediate layout of pending
		/// layout requests.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new void ResumeLayout()
		{
			CheckDisposed();

			ResumeLayout(true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resumes normal layout logic for this sidebar tab. Optionally forces an immediate
		/// layout of pending layout requests.
		/// This will execute every time this sidebar tab is collapsed, expanded, or activated.
		/// At startup, we also press/click the tab's buttons after the buttons are actually created.
		/// </summary>
		/// <param name="fPerformLayout"><b>true</b> to execute pending layout requests;
		/// otherwise, <b>false</b>.</param>
		/// ------------------------------------------------------------------------------------
		public new void ResumeLayout(bool fPerformLayout)
		{
			CheckDisposed();

			base.ResumeLayout(fPerformLayout);

			m_nSuspendedLayout--;
			if (m_nSuspendedLayout <= 0)
			{
				// process each button (if they've been created)
				if (Buttons.Count > 0)
				{
					int i = 0;
					foreach(SideBarButton btn in Buttons)
					{
						// reset button size, because the size of the image may have changed!
						btn.ShowLargeIcons(LargeIconsShowing);

						// if we have startup button states to initialize
						if (m_nPressedButtonsStartup != -1)
						{
							// ENHANCE (TomB/EberhardB): Buttons should know and handle
							// themselves if they are pressed.
							// Visually press or unpress this button.
							bool fPress = (m_nPressedButtonsStartup & (1 << i)) != 0;
							btn.PressButton(fPress);

							// Also click a pressed button if appropriate
							if (fPress && OkToClickAButtonInThisTab)
								btn.PerformClick();
						}
						i++;
					}

					// Clear the startup button states, now that we have used them
					m_nPressedButtonsStartup = -1;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine if it's okay to click a button in this sidebar tab.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool OkToClickAButtonInThisTab
		{
			get
			{
				// if all tabs can activate their pressed buttons (i.e. the sidebar is not in
				//  ActiveTabIsExclusive mode), it's okay
				if (!((SideBar)this.Parent).ActiveTabIsExclusive)
					return true;

				// if this tab is the active tab, it's okay
				if (IsActiveTab)
					return true;

				// otherwise, no
				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Click the currently pressed button(s) on this tab.
		/// This is needed in some conditions, such as when the ActiveTabIsExclusive option is
		/// in effect, and a different tab was just activated. In this case, the tab button that was
		/// visually pressed now needs to become fully operational.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ClickPressedButton()
		{
			CheckDisposed();

			int i = 0;
			foreach(SideBarButton btn in Buttons)
			{
				// ENHANCE (TomB/EberhardB): Buttons should know and handle themselves.
				// If this button was pressed, click it now
				if (btn.Pressed)
					btn.PerformClick();

				i++;
			}
		}
		#endregion

		#region Program internal event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void btnTitle_Click(object sender, System.EventArgs e)
		{
			if (Activate != null)
			{
				Activate(this);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calculate if we should show scroll buttons
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			CenterButtons();

			// Give Panel its new maximum size
			scrollTab.MaxHeight = Height - btnTitle.Height;

			// Set new width for scroll buttons
			btnUp.Width = Width;
			btnDown.Width = Width;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The button is about to be inserted to the button control. Insert it also to the
		/// panel, and adjust the location of the new button.
		/// </summary>
		/// <param name="index">index for button in tab</param>
		/// <param name="value">the button</param>
		/// ------------------------------------------------------------------------------------
		private void OnButtonInserting(int index, object value)
		{
			SideBarButton btn = value as SideBarButton;
			SideBar parent = Parent as SideBar;
			if (btn != null)
			{
				btn.SuspendLayout();

				btn.Click += new EventHandler(OnButtonClicked);

				if (m_buttonClickEvent != null)
					btn.Click += m_buttonClickEvent;

				if (parent != null)
				{
					btn.ImageListLarge = parent.ImageListLarge;
					btn.ImageListSmall = parent.ImageListSmall;
					btn.HeightLarge = parent.ButtonHeightLarge;
					btn.HeightSmall = parent.ButtonHeightSmall;
				}

				if (LargeIconsShowing)
				{
					btn.ImageList = btn.ImageListLarge;
					btn.Height = btn.HeightLarge;
				}
				else
				{
					btn.ImageList = btn.ImageListSmall;
					btn.Height = btn.HeightSmall;
				}

				btn.Width = Width;
				btn.Padding = Padding;

				RepositionButton(index, btn);

				btn.ShowLargeIcons(LargeIconsShowing);

				if (scrollTab.Controls.Count == 0)
					btn.PressButton(true);

				btn.ResumeLayout();
				scrollTab.AddButton(btn);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Button is about to be removed. Remove it also from the panel.
		/// </summary>
		/// <param name="index">The zero-based index at which <paramref name="value"/>
		/// can be found.</param>
		/// <param name="value">The value of the element to remove from
		/// <paramref name="index"/>.</param>
		/// ------------------------------------------------------------------------------------
		private void OnButtonRemoval(int index, object value)
		{
			SideBarButton btn = value as SideBarButton;

			if (btn != null)
				scrollTab.RemoveButton(btn);

			int i = 0;
			foreach (SideBarButton btnTmp in m_buttons)
			{
				RepositionButton(i, btnTmp);
				i++;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the active tab's settings.
		/// </summary>
		///
		/// <param name='key'>The Registry Key.</param>
		/// ------------------------------------------------------------------------------------
		private void LoadSettings(Microsoft.Win32.RegistryKey key)
		{
			SuspendLayout();

			try
			{
				IRegistryKeyNameModifier modifier = Parent.Parent as IRegistryKeyNameModifier;
				key = modifier.ModifyKey(key, true);
			}
			catch
			{}
			m_nPressedButtonsStartup = (int)key.GetValue(Name + "State", -1);

			if (m_nPressedButtonsStartup != -1 && Buttons.Count > 0)
			{
				// note: In TE, there are no buttons created yet when settings are loaded.
				// So this section of code is probably never used.
				// Instead, the ResumeLayout() method takes care of the initial pressing of
				//buttons later, when the tab is redrawn after the buttons are created.
				for (int i = 0; i < Buttons.Count; i++)
				{
					bool fPress = (m_nPressedButtonsStartup & (1 << i)) != 0;
					Buttons[i].PressButton(fPress);
					// Also visually click only if not suspended (we are suspended at least once, because we
					// call SuspendLayout() above!)
					if (fPress && m_nSuspendedLayout < 2)
					{
						// Also click a pressed button if appropriate
						if (fPress && OkToClickAButtonInThisTab)
							Buttons[i].PerformClick();
					}
				}

				// Clear the startup button states, now that we have used them
				m_nPressedButtonsStartup = -1;
			}

			int nDefault = LargeIconsShowing ? 1 : 0;
			LargeIconsShowing = ((int)key.GetValue(Name + "LargeIcons", nDefault) == 1)
				? true : false;

			ResumeLayout();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save the active tab.
		/// </summary>
		///
		/// <param name='key'>The Registry Key.</param>
		/// ------------------------------------------------------------------------------------
		private void SaveSettings(Microsoft.Win32.RegistryKey key)
		{
			try
			{
				IRegistryKeyNameModifier modifier = Parent.Parent as IRegistryKeyNameModifier;
				key = modifier.ModifyKey(key, true);
			}
			catch
			{}
			key.SetValue(Name + "LargeIcons", LargeIconsShowing ? 1 : 0);
			key.SetValue(Name + "State", GetButtonStates());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a bit field of the current states of the buttons on this tab.
		/// </summary>
		/// <returns>bit field of the current states of the buttons on this tab</returns>
		/// ------------------------------------------------------------------------------------
		private int GetButtonStates()
		{
			int nPressedButtons = 0;
			int i = 0;
			foreach(SideBarButton btn in Buttons)
			{
				if (btn.Pressed)
					nPressedButtons |= (1 << i);

				i++;
			}
			return nPressedButtons;
		}
		#endregion

		#region Event handlers in response to user interaction
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// User has clicked on a button. Release all other buttons that might be pressed.
		/// </summary>
		/// <param name="sender">The button that was clicked</param>
		/// <param name="e">Event arguments</param>
		/// ------------------------------------------------------------------------------------
		protected void OnButtonClicked(object sender, EventArgs e)
		{
			SideBarButton pressedBtn = sender as SideBarButton;

			if (pressedBtn == null)
				return;


//			// If the button is already pushed in, then don't bother doing anything.
//			if (pressedBtn.ButtonToggleState == FwButton.ButtonToggleStates.Pushed)
//				return;

			bool isFirstButtonPressed = (Buttons.IndexOf(pressedBtn) == 0);
			int nPressedButtons = 0;
			pressedBtn.PressButton(true);
			if (!MultipleSelections)
			{
				m_pressedBtn = pressedBtn;
				for (int i = 0; i < Buttons.Count; i++)
				{
					// This will make sure all the buttons that weren't clicked
					// are not pushed in.
					if (Buttons[i] != pressedBtn)
						Buttons[i].PressButton(false);
				}
			}
			else
			{
				int i = 0;
				if (FirstButtonExclusive)
				{
					i = 1;
					if (isFirstButtonPressed)
					{
						// In multiple selection and FirstButtonExclusive mode,
						// pressing the first button should unpress all other buttons.
						Buttons[0].PressButton(true);
						nPressedButtons++;
					}
					else
					{
						// We have to unpress the first button, because it can't stay
						// pressed when other buttons are selected.
						Buttons[0].PressButton(false);
					}
				}
				for (; i < Buttons.Count; i++)
				{
					if (Buttons[i].Pressed)
						nPressedButtons++;
				}
			}

			// If no other button is selected we should select the first one
			if (MultipleSelections && FirstButtonExclusive && nPressedButtons == 0 &&
				Buttons[0].ButtonToggleState != FwButton.ButtonToggleStates.Pushed)
			{
				Buttons[0].PressButton(true);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Users wants to see small icons with text to the right of the icon
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void OnSmallIcons(object sender, System.EventArgs e)
		{
			LargeIconsShowing = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// User wants to see large icons with text below the icon
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void OnLargeIcons(object sender, System.EventArgs e)
		{
			LargeIconsShowing = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// User wants to configure the tab. Delegate it to whomever.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void OnConfigure(object sender, System.EventArgs e)
		{
			if (Configure != null)
				Configure(sender, e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// User wants to hide the SideBar
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void OnHideSideBar(object sender, System.EventArgs e)
		{
			Parent.Hide();
		}
		#endregion

		#region Variables for properties
		//*************************************************************************************
		/// <summary>
		/// Counts the number SuspendedLayout was called without ResumeLayout
		/// </summary>
		private int m_nSuspendedLayout;

		/// <summary>
		/// m_nPressedButtonsStartup is a bit field that holds the pressed state of the buttons
		/// as loaded from the registry, for initializing them.
		/// Used for run-time added buttons. Null state is -1.
		/// </summary>
		private int m_nPressedButtonsStartup = -1;

		private SideBarButtonCollection m_buttons = null;
		private bool m_fInitialShowLargeIcons = true;
		private bool m_fMultipleSelections = false;
		private bool m_fFirstButtonExclusive = true;

		// Eventhandler that will be set for all buttons
		private EventHandler m_buttonClickEvent;

		private bool m_fPersist = true;
		#endregion

		#region Event declarations
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Represents the method that will handle the <see cref="Activate"/> event.
		/// </summary>
		/// <param name="newActive">The newly activated tab</param>
		/// ------------------------------------------------------------------------------------
		public delegate void ActivateEventHandler(SideBarTab newActive);

		/// <summary>User clicked on tab to activate it</summary>
		[Category("Action")]
		[Description("Occurs when the tab is activated")]
		public event ActivateEventHandler Activate;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// User clicked on the configure menu item
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Category("Action")]
		[Description("Occurs when the Configure menu item is clicked")]
		public event EventHandler Configure;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Event that will be set for each button
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Category("Action")]
		[Description("Will be set for the click event for each button")]
		public event EventHandler ButtonClickEvent
		{
			// Override Event registration. See Jeffrey Richter: Applied Microsoft .NET
			// Framwork Programming, p. 237 (Chapter 11, Events) for details
			remove
			{
				CheckDisposed();

				if (m_buttonClickEvent != null)
				{
					// remove the handler from all the buttons
					foreach (SideBarButton btn in Buttons)
						btn.Click -= m_buttonClickEvent;
				}
				m_buttonClickEvent = null;
			}
			add
			{
				CheckDisposed();

				if (m_buttonClickEvent != null)
				{
					// first remove the old handler from all the buttons
					foreach (SideBarButton btn in Buttons)
						btn.Click -= m_buttonClickEvent;
				}

				m_buttonClickEvent = value;

				// than add the new handler to all the buttons
				foreach (SideBarButton btn in Buttons)
					btn.Click += m_buttonClickEvent;
			}
		}

		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or Sets the menu extender for the tab's context menu. This is the context
		/// menu displayed when the user right-clicks on a sidebar button. This property is
		/// set by a main window (e.g. TeMainWnd) so the main window's menu extender is used
		/// rather than the tab's own menu extender.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public MenuExtender MenuExtender
		{
			get
			{
				CheckDisposed();
				return m_menuExtender;
			}
			set
			{
				CheckDisposed();

				if (value == null || value == m_menuExtender)
					return;

				// When setting the menu extender for this tab, we need to make sure the
				// information for each menu item that's been stored in the local menu extender
				// (i.e. the one instantiated in InitializeComponents) is copied to the new one
				// before setting the local menu extender to the new one.
				foreach (MenuItem menuItem in contextMenuSideBarTab.MenuItems)
				{
					value.AddMenuItem(menuItem);
					value.SetImageList(menuItem, m_menuExtender.GetImageList(menuItem));
					value.SetImageIndex(menuItem, m_menuExtender.GetImageIndex(menuItem));

					string commandId = m_menuExtender.GetCommandId(menuItem);

					// If this item is the configure menu item or it's separator, then
					// associate this tab with the menu item by setting the extender's tag.
					if (commandId == ConfigureMenuCommandId && commandId != null)
						value.SetTag(menuItem, this);

					if (commandId != null)
						value.SetCommandId(menuItem, commandId);
				}

				value.AddContextMenu(contextMenuSideBarTab);
				m_menuExtender = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the state of the tab: true if active and false otherwise.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsActiveTab
		{
			get
			{
				CheckDisposed();
				return this == ((SideBar)Parent).ActiveTab;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the text that is displayed as the title for the tab.
		/// </summary>
		/// <value>The text to display as the title for the tab.</value>
		/// ------------------------------------------------------------------------------------
		[Localizable(true)]
		[Category("Appearance")]
		[Description("The text to display as the title for the tab.")]
		public string Title
		{
			get
			{
				CheckDisposed();
				return btnTitle.Text;
			}
			set
			{
				CheckDisposed();
				btnTitle.Text = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the collection that contains the buttons that belong to this tab.
		/// </summary>
		/// <value>The buttons that will be displayed in this tab.</value>
		/// ------------------------------------------------------------------------------------
		[Category("Appearance")]
		[Description("The buttons that will be displayed in this tab.")]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
		public SideBarButtonCollection Buttons
		{
			get
			{
				CheckDisposed();

				if (m_buttons == null)
				{
					m_buttons = new SideBarButtonCollection();
					m_buttons.BeforeInsert +=
						new SideBarButtonCollection.CollectionChange(OnButtonInserting);
					m_buttons.AfterRemove +=
						new SideBarButtonCollection.CollectionChange(OnButtonRemoval);
					m_buttons.BeforeClear +=
						new SideBarButtonCollection.CollectionClear(scrollTab.ClearButtons);
				}

				return m_buttons;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the height of the control in deactivated (collapsed) state
		/// </summary>
		/// <value>The minimal height of the tab</value>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int MinHeight
		{
			get
			{
				CheckDisposed();
				return btnTitle.Height;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the horizontal and vertical padding between the border of the tab and the
		/// buttons
		/// </summary>
		/// <value>The horizontal and vertical padding between the border of the tab and the buttons.</value>
		/// ------------------------------------------------------------------------------------
		[Category("Layout")]
		[Description("The horizontal and vertical padding between the border of the tab and the buttons.")]
		public new Padding Padding
		{
			get
			{
				CheckDisposed();
				return base.Padding;
			}
			set
			{
				CheckDisposed();

				base.Padding = value;

				// Refresh padding for all buttons
				foreach (SideBarButton btn in Buttons)
					btn.Padding = value;

				scrollTab.Padding = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a flag wether to show large icons at startup
		/// </summary>
		/// <value><c>true</c> to start with large icons, otherwise <c>false</c>.
		/// The default is <c>true</c>.</value>
		/// ------------------------------------------------------------------------------------
		[DefaultValue(true)]
		[Category("Appearance")]
		[Description("Determines if large icons should be shown initially")]
		public bool InitialLargeIcons
		{
			get
			{
				CheckDisposed();
				return m_fInitialShowLargeIcons;
			}
			set
			{
				CheckDisposed();

				m_fInitialShowLargeIcons = value;
				LargeIconsShowing = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a flag wether to show large icons
		/// </summary>
		/// <value><c>true</c> if large icons are displayed, otherwise <c>false</c>.
		/// The default is <c>true</c>.</value>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public bool LargeIconsShowing
		{
			get
			{
				CheckDisposed();
				return mnuLargeIcons.Checked;
			}
			set
			{
				CheckDisposed();

				mnuLargeIcons.Checked = value;
				mnuSmallIcons.Checked = !value;

				foreach(SideBarButton btn in Buttons)
					btn.ShowLargeIcons(value);

				// adjust scroll buttons
				if (scrollTab.Visible)
					scrollTab.ShowButtons();

				int index = 0;
				foreach(SideBarButton btn in Buttons)
				{
					RepositionButton(index, btn);
					index++;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a flag wether to allow the selection of multiple buttons
		/// </summary>
		/// <value><c>true</c> if multiple buttons can be selected, otherwise <c>false</c>.
		/// The default is <c>false</c>.</value>
		/// ------------------------------------------------------------------------------------
		[DefaultValue(false)]
		[Category("Behavior")]
		[Description("Determines if multiple buttons can be selected (pressed)")]
		public bool MultipleSelections
		{
			get
			{
				CheckDisposed();
				return m_fMultipleSelections;
			}
			set
			{
				CheckDisposed();
				m_fMultipleSelections = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a flag wether the first button works as "Off" button, i.e. deselects
		/// all other selected buttons. This property is ignored if MultipleSelections is not
		/// set.
		/// </summary>
		/// <value><c>true</c> if the first button works as 'off' button, i.e. deselects all
		/// other selected buttons. Otherwise <c>false</c>. The default is <c>true</c>.</value>
		/// <remarks>This property is ignored if <see cref="MultipleSelections"/> is
		/// <c>false</c>.</remarks>
		/// ------------------------------------------------------------------------------------
		[DefaultValue(true)]
		[Category("Behavior")]
		[Description("Determines if the first button works as 'Off' button, i.e. deselects " +
			 "all other selected buttons. This property is ignored if MultipleSelections is " +
			 "not activated.")]
		public bool FirstButtonExclusive
		{
			get
			{
				CheckDisposed();
				return m_fFirstButtonExclusive;
			}
			set
			{
				CheckDisposed();
				m_fFirstButtonExclusive = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the access Registry key for the SideBarTab
		/// </summary>
		/// <value>The registry key where the <see cref="SideBarTab"/> persists its values.
		/// </value>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		virtual public Microsoft.Win32.RegistryKey SettingsKey
		{
			get
			{
				CheckDisposed();

				if (Parent is ISettings)
					return ((ISettings)Parent).SettingsKey;
				return null;
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
		/// Gets or sets the selected buttons.
		/// </summary>
		/// <value>A <see cref="SideBarButtonCollection"/> with the selected (depressed)
		/// buttons.</value>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public SideBarButtonCollection Selection
		{
			get
			{
				CheckDisposed();

				SideBarButtonCollection collection = new SideBarButtonCollection();

				foreach(SideBarButton btn in Buttons)
				{
					if (btn.Pressed)
						collection.Add(btn);
				}

				return collection;
			}
			set
			{
				foreach(SideBarButton btn in value)
				{
					if (!Buttons.Contains(btn))
						Buttons.Add(btn);
					btn.PerformClick();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the selected buttons. The values in the array represent
		/// the index numbers of the buttons.
		/// </summary>
		/// <value>A <see cref="int"/> array with the indexes of the selected
		/// (pressed) buttons.</value>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int[] IntSelection
		{
			get
			{
				CheckDisposed();

				int[] nTmp = new int[Buttons.Count];

				int iSelectedItems = 0;
				for (int i = 0; i < Buttons.Count; i++)
				{
					if (Buttons[i].Pressed)
					{
						nTmp[iSelectedItems] = i;
						iSelectedItems++;
					}
				}

				int[] nSelectedItems = new int[iSelectedItems];
				for (int i = 0; i < iSelectedItems; i++)
					nSelectedItems[i] = nTmp[i];
				// we can call Length to get the number of items, so no need to store -1
				// nSelectedItems[iSelectedItems] = -1;
				return nSelectedItems;
			}
			set
			{
				if (value != null)
				{
					Debug.Assert(m_fMultipleSelections || value.Length <= 1);
					for (int i = 0; i < value.Length; i++)
					{
						FwButton btn = Buttons[value[i]];
						btn.PressButton(true);
						btn.PerformClick();
					}

				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the flag if the side bar should persist its settings to the registry.
		/// </summary>
		/// <value><c>true</c> to persist settings to the registry, otherwise <c>false</c>.
		/// The default value is <c>true</c>.</value>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
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
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the text for the configure menu item on the tab's context menu.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		public string ConfigureMenuText
		{
			get
			{
				CheckDisposed();
				return mnuConfigure.Text;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the command id for the configure menu item on the tab's context menu.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		public string ConfigureMenuCommandId
		{
			get
			{
				CheckDisposed();
				return m_menuExtender.GetCommandId(mnuConfigure);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the image list for the configure menu item on the tab's context menu.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		public ImageList ConfigureMenuImageList
		{
			get
			{
				CheckDisposed();
				return m_menuExtender.GetImageList(mnuConfigure);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the image index for the configure menu item on the tab's context menu.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Browsable(false)]
		public int ConfigureMenuImageIndex
		{
			get
			{
				CheckDisposed();
				return m_menuExtender.GetImageIndex(mnuConfigure);
			}
		}
		#endregion

		#region ShouldSerialize/Reset Methods (Methods related to properties)
		//************************************************************************************
		/// <summary>
		/// Determine if Buttons property should be serialized. This method has purely
		/// cosmetic affects. If the Buttons collection is empty, the property is not
		/// shown in bold in design mode.
		/// </summary>
		/// <returns>Returns true if collection contains any elements.</returns>
		private bool ShouldSerializeButtons()
		{
			return m_buttons.Count > 0;
		}

// Doing it this way doesn't delete the objects in the form!
//		/// <summary>
//		/// Deletes all buttons
//		/// </summary>
//		private void ResetButtons()
//		{
//			m_buttons.Clear();
//		}
		#endregion
	}
}

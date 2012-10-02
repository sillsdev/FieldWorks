//-------------------------------------------------------------------------------------------------
// <copyright file="PropertyPage.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
// Contains the PropertyPage class.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.ComponentModel;
	using System.Drawing;
	using System.Runtime.InteropServices;
	using System.Windows.Forms;
	using Microsoft.VisualStudio.Designer.Interfaces;
	using Microsoft.VisualStudio.OLE.Interop;

	/// <summary>
	/// Abstract base class for a property page in the Property Page dialog.
	/// </summary>
	/// <remarks>Note to subclasses: Make sure to define a unique GUID.</remarks>
	[ComVisible(true)]
	[Guid("97656668-E50F-4179-AFF5-A7897E7557A6")]
	public abstract class PropertyPage : IPropertyPage
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(PropertyPage);

		private string name;
		private IPropertyPageSite pageSite;
		private Project project;
		private Size requiredSize = new Size(450, 300);

		// These make up the contents of the property page.
		private Panel panel;
		private IVSMDPropertyGrid propertyGrid;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="PropertyPage"/> class.
		/// </summary>
		/// <param name="name">The localized name of the property page.</param>
		protected PropertyPage(string name)
		{
			Tracer.VerifyStringArgument(name, "name");
			this.name = name;
		}
		#endregion

		#region Enums
		//==========================================================================================
		// Enums
		//==========================================================================================

		/// <summary>
		/// Native interop enumeration for the property page status.
		/// </summary>
		[Flags]
		private enum PropertyPageStatus
		{
			Dirty = 0x1,
			Validate = 0x2,
			Clean = 0x4
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets the name of the property page that will be displayed in the left hand
		/// navigation bar on the VS property page dialog.
		/// </summary>
		public string Name
		{
			get { return this.name; }
		}

		/// <summary>
		/// Gets the object that is bound to this property page.
		/// </summary>
		protected abstract PropertyPageSettings BoundObject { get; }

		/// <summary>
		/// Gets or sets a value indicating whether this property page has changed its state since
		/// the last call to <see cref="ApplyChanges"/>. The property sheet uses this information
		/// to enable or disable the Apply button in the dialog box.
		/// </summary>
		protected bool IsDirty
		{
			get
			{
				if (this.BoundObject == null)
				{
					return false;
				}
				return this.BoundObject.IsDirty;
			}
		}

		/// <summary>
		/// Gets the project that owns the property page.
		/// </summary>
		internal protected Project Project
		{
			get { return this.project; }
		}

		/// <summary>
		/// Gets the main panel container.
		/// </summary>
		protected Panel Panel
		{
			get { return this.panel; }
		}

		/// <summary>
		/// Gets the property grid control.
		/// </summary>
		protected IVSMDPropertyGrid PropertyGrid
		{
			get { return this.propertyGrid; }
		}

		/// <summary>
		/// Gets or sets the required size of the property page.
		/// </summary>
		protected virtual Size RequiredSize
		{
			get { return this.requiredSize; }
			set { this.requiredSize = value; }
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		#region IPropertyPage Members

		/// <summary>
		/// Called when the environment wants us to create our property page.
		/// </summary>
		/// <param name="hWndParent">The HWND of the parent window.</param>
		/// <param name="pRect">The bounds of the area that we should fill.</param>
		/// <param name="bModal">Indicates whether the dialog box is shown modally or not.</param>
		void IPropertyPage.Activate(IntPtr hWndParent, RECT[] pRect, int bModal)
		{
			// Create the main panel container
			if (this.panel == null)
			{
				this.panel = new Panel();
				this.panel.Visible = false;
				this.ResizePropertyGrid(pRect[0]);

				// We have to force the creation of the control so the handle will be valid
				this.panel.CreateControl();

				// Make the panel a child of the parent
				NativeMethods.SetParent(this.panel.Handle, hWndParent);
			}

			// Let the subclasses have a chance to create their own specific property page
			this.CreatePageControls();

			this.Refresh();
		}

		/// <summary>
		/// Applies the changes made on the property page to the bound objects.
		/// </summary>
		/// <returns>
		/// <b>S_OK</b> if the changes were successfully applied and the property page is current with the bound objects;
		/// <b>S_FALSE</b> if the changes were applied, but the property page cannot determine if its state is current with the objects.
		/// </returns>
		int IPropertyPage.Apply()
		{
			if (this.IsDirty)
			{
				bool applied = this.ApplyChanges();
				this.BoundObject.ClearDirty();
				this.Refresh();
				return (applied ? NativeMethods.S_OK : NativeMethods.S_FALSE);
			}

			return NativeMethods.S_OK;
		}

		/// <summary>
		/// The environment calls this to notify us that we should clean up our resources.
		/// </summary>
		void IPropertyPage.Deactivate()
		{
			this.panel.Dispose();
			this.panel = null;
		}

		/// <summary>
		/// The environment calls this to get the parameters to describe the property page.
		/// </summary>
		/// <param name="pPageInfo">The parameters are returned in this one-sized array.</param>
		void IPropertyPage.GetPageInfo(PROPPAGEINFO[] pPageInfo)
		{
			Tracer.VerifyNonNullArgument(pPageInfo, "pPageInfo");

			PROPPAGEINFO info = new PROPPAGEINFO();

			info.cb = (uint)Marshal.SizeOf(typeof(PROPPAGEINFO));
			info.dwHelpContext = 0;
			info.pszDocString = null;
			info.pszHelpFile = null;
			info.pszTitle = this.Name;
			info.SIZE.cx = this.RequiredSize.Width;
			info.SIZE.cy = this.RequiredSize.Height;
			pPageInfo[0] = info;
		}

		/// <summary>
		/// Invokes the property page help in response to an end-user request.
		/// </summary>
		/// <param name="pszHelpDir">
		/// String under the HelpDir key in the property page's CLSID information in the registry.
		/// If HelpDir does not exist, this will be the path found in the InProcServer32 entry
		/// minus the server file name. (Note that LocalServer32 is not checked in the current
		/// implementation, since local property pages are not currently supported).
		/// </param>
		void IPropertyPage.Help(string pszHelpDir)
		{
		}

		/// <summary>
		/// Indicates whether this property page has changed its state since the last call to
		/// <see cref="IPropertyPage.Apply"/>. The property sheet uses this information to enable
		/// or disable the Apply button in the dialog box.
		/// </summary>
		/// <returns>
		/// <b>S_OK</b> if the value state of the property page is dirty, that is, it has changed
		/// and is different from the state of the bound objects;
		/// <b>S_FALSE</b> if the value state of the page has not changed and is current with that
		/// of the bound objects.
		/// </returns>
		int IPropertyPage.IsPageDirty()
		{
			return (this.IsDirty ? NativeMethods.S_OK : NativeMethods.S_FALSE);
		}

		/// <summary>
		/// Repositions and resizes the property page dialog box according to the contents of
		/// <paramref name="pRect"/>. The rectangle specified by <paramref name="pRect"/> is
		/// treated identically to that passed to <see cref="IPropertyPage.Activate"/>.
		/// </summary>
		/// <param name="pRect">The bounds of the area that we should fill.</param>
		void IPropertyPage.Move(RECT[] pRect)
		{
			this.ResizePropertyGrid(pRect[0]);
		}

		/// <summary>
		/// The environment calls this to set the currently selected objects that the property page should show.
		/// </summary>
		/// <param name="cObjects">The count of elements in <paramref name="ppunk"/>.</param>
		/// <param name="ppunk">An array of <b>IUnknown</b> objects to show in the property page.</param>
		/// <remarks>
		/// We are supposed to cache these objects until we get another call with <paramref name="cObjects"/> = 0.
		/// Also, the environment is supposed to call this before calling <see cref="IPropertyPage.Activate"/>,
		/// but like all things when interacting with Visual Studio, don't trust that and code defensively.
		/// </remarks>
		void IPropertyPage.SetObjects(uint cObjects, object[] ppunk)
		{
			if (ppunk == null || ppunk.Length == 0 || cObjects == 0)
			{
				this.project = null;
				this.UnbindObject();
				return;
			}

			// Check the incoming parameters
			Tracer.WriteLineIf(classType, "IPropertyPage.SetObjects", Tracer.Level.Verbose, cObjects != ppunk.Length, "Visual Studio passed us a ppunk array of size {0} but the count is {1}.", ppunk.Length, cObjects);

			// We get called when the user selects "Properties" from the context menu on the project node
			if (ppunk[0] is NodeProperties)
			{
				if (this.project == null)
				{
					this.project = ((NodeProperties)ppunk[0]).Node.Hierarchy.AttachedProject;
				}
			}

			// Refresh the page with the new settings
			if (this.Project != null)
			{
				this.BindObject();
			}
		}

		/// <summary>
		/// Initializes a property page and provides the property page object with the
		/// <see cref="IPropertyPageSite"/> interface through which the property page communicates
		/// with the property frame.
		/// </summary>
		/// <param name="pPageSite">
		/// The <see cref="IPropertyPageSite"/> that manages and provides services to this property
		/// page within the entire property sheet.
		/// </param>
		void IPropertyPage.SetPageSite(IPropertyPageSite pPageSite)
		{
			// pPageSite can be null (on deactivation)
			this.pageSite = pPageSite;
		}

		/// <summary>
		/// Makes the property page dialog box visible or invisible according to the <paramref name="nCmdShow"/>
		/// parameter. If the page is made visible, the page should set the focus to itself, specifically to the
		/// first property on the page.
		/// </summary>
		/// <param name="nCmdShow">
		/// Command describing whether to become visible (SW_SHOW or SW_SHOWNORMAL) or hidden (SW_HIDE). No other values are valid for this parameter.
		/// </param>
		void IPropertyPage.Show(uint nCmdShow)
		{
			if (this.Panel != null)
			{
				if (nCmdShow == NativeMethods.SW_HIDE)
				{
					this.Panel.Hide();
				}
				else
				{
					Tracer.WriteLineIf(classType, "IPropertyPage.Show", Tracer.Level.Verbose, nCmdShow == NativeMethods.SW_SHOW || nCmdShow == NativeMethods.SW_SHOWNORMAL, "VS passed us an invalid nCmdShow: {0}. Defaulting to SW_SHOW.", nCmdShow);
					this.Panel.Show();
				}
			}
		}

		/// <summary>
		/// Instructs the property page to process the keystroke described in <paramref name="pMsg"/>.
		/// </summary>
		/// <param name="pMsg">Describes the keystroke to process.</param>
		/// <returns>
		/// <list type="table">
		/// <item><term>S_OK</term><description>The property page handles the accelerator.</description></item>
		/// <item><term>S_FALSE</term><description>The property page handles accelerators, but this one was not useful to it.</description></item>
		/// <item><term>E_NOTIMPL</term><description>The proeprty page does not handle accelerators.</description></item>
		/// </list>
		/// </returns>
		int IPropertyPage.TranslateAccelerator(MSG[] pMsg)
		{
			Tracer.VerifyNonNullArgument(pMsg, "pMsg");

			MSG msg = pMsg[0];
			int hr = NativeMethods.S_FALSE;

			// If the message is a keyboard or mouse message, then call the Win32 function that determines
			// if the panel can handle the message and handles it if it can.
			if ((msg.message >= NativeMethods.WM_KEYFIRST && msg.message <= NativeMethods.WM_KEYLAST) ||
				(msg.message >= NativeMethods.WM_MOUSEFIRST && msg.message <= NativeMethods.WM_MOUSELAST))
			{
				if (this.Panel != null && NativeMethods.IsDialogMessageA(this.Panel.Handle, ref msg))
				{
					hr = NativeMethods.S_OK;
				}
			}

			return hr;
		}
		#endregion

		/// <summary>
		/// Applies the changes made on the property page to the bound objects.
		/// </summary>
		/// <returns>
		/// true if the changes were successfully applied and the property page is current with the bound objects;
		/// false if the changes were applied, but the property page cannot determine if its state is current with the objects.
		/// </returns>
		protected virtual bool ApplyChanges()
		{
			return true;
		}

		/// <summary>
		/// Binds the settings object to this page.
		/// </summary>
		protected void BindObject()
		{
			if (this.BoundObject != null)
			{
				// Attach ourself to the settings
				this.BoundObject.AttachToPropertyPage(this);

				// Listen to the bound object's events
				this.BoundObject.DirtyStateChanged += new EventHandler(this.HandleDirtyStateChanged);
				this.BoundObject.PropertyChanged += new PropertyChangedEventHandler(this.HandlePropertyChanged);
			}
		}

		/// <summary>
		/// Creates the controls that constitute the property page. The default implementation
		/// creates a property grid. This should be safe to re-entrancy.
		/// </summary>
		protected virtual void CreatePageControls()
		{
			if (this.propertyGrid == null && this.Project != null)
			{
				IVSMDPropertyBrowser pb = this.Project.ServiceProvider.GetService(typeof(IVSMDPropertyBrowser)) as IVSMDPropertyBrowser;
				this.propertyGrid = pb.CreatePropertyGrid();

				// Set the property grid properties
				this.propertyGrid.GridSort = _PROPERTYGRIDSORT.PGSORT_ALPHABETICAL | _PROPERTYGRIDSORT.PGSORT_CATEGORIZED;
				this.propertyGrid.SetOption(_PROPERTYGRIDOPTION.PGOPT_TOOLBAR, false);

				// Set some of the control properties
				Control gridControl = Control.FromHandle(new IntPtr(this.propertyGrid.Handle));
				gridControl.Dock = DockStyle.Fill;
				gridControl.Visible = true;

				this.Panel.Controls.Add(gridControl);
			}
		}

		/// <summary>
		/// Notifies the page site that the dirty state has changed.
		/// </summary>
		protected void NotifyPageSiteOnDirtyStateChange()
		{
			if (this.pageSite != null && this.BoundObject != null)
			{
				this.pageSite.OnStatusChange((uint)(this.BoundObject.IsDirty ? PropertyPageStatus.Dirty : PropertyPageStatus.Clean));
			}
		}

		/// <summary>
		/// Refreshes the property page from the bound object. The default implementation sets the
		/// bound object on the property grid (if it has been created).
		/// </summary>
		protected virtual void Refresh()
		{
			if (this.PropertyGrid != null)
			{
				IntPtr punkBoundObject = Marshal.GetIUnknownForObject(this.BoundObject);
				IntPtr ppunk = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(IntPtr)));

				try
				{
					Marshal.WriteIntPtr(ppunk, punkBoundObject);

					// Select the bound object into the property grid
					this.propertyGrid.SetSelectedObjects(1, ppunk.ToInt32());
					this.propertyGrid.Refresh();
				}
				finally
				{
					Marshal.FreeCoTaskMem(ppunk);
					Marshal.Release(punkBoundObject);
				}
			}
		}

		/// <summary>
		/// Unbinds the object from this page.
		/// </summary>
		protected void UnbindObject()
		{
			// Remove ourself as a listener from the old bound object's events
			if (this.BoundObject != null)
			{
				this.BoundObject.DirtyStateChanged -= new EventHandler(this.HandleDirtyStateChanged);
				this.BoundObject.PropertyChanged -= new PropertyChangedEventHandler(this.HandlePropertyChanged);
				this.BoundObject.DetachPropertyPage();
			}
		}

		/// <summary>
		/// Event handler for the bound object's <see cref="IDirtyable.DirtyStateChanged">DirtyStateChanged</see> event.
		/// </summary>
		/// <param name="sender">The bound object.</param>
		/// <param name="e">The <see cref="EventArgs"/> object that contains the event data.</param>
		private void HandleDirtyStateChanged(object sender, EventArgs e)
		{
			Tracer.Assert(sender as PropertyPageSettings == this.BoundObject, "The sender should be the cached bound object.");
			this.NotifyPageSiteOnDirtyStateChange();
		}

		/// <summary>
		/// Event handler for the bound object's <see cref="PropertyPageSettings.PropertyChanged">PropertyChanged</see> event.
		/// </summary>
		/// <param name="sender">The bound object.</param>
		/// <param name="e">The <see cref="PropertyChangedEventArgs"/> object that contains the event data.</param>
		private void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			Tracer.Assert(sender as PropertyPageSettings == this.BoundObject, "The sender should be the cached bound object.");
			this.NotifyPageSiteOnDirtyStateChange();
			this.Refresh();
		}

		/// <summary>
		/// Resizes the property grid to the specified bounds.
		/// </summary>
		/// <param name="newBounds">The total area of the property page.</param>
		private void ResizePropertyGrid(RECT newBounds)
		{
			if (this.Panel != null)
			{
				this.Panel.Bounds = new Rectangle(newBounds.left, newBounds.top, newBounds.right - newBounds.left, newBounds.bottom - newBounds.top);

				// Leave a 6 pixel padding on the bottom so that the property grid doesn't come in contact with the dividing line on the dialog
				this.Panel.Height -= 6;
			}
		}
		#endregion
	}
}

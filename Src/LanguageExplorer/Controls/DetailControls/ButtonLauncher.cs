// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Diagnostics;
using SIL.LCModel;
using LanguageExplorer.Controls.DetailControls.Resources;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Controls.DetailControls
{
	internal class ButtonLauncher : UserControl, IFlexComponent, INotifyControlInCurrentSlice
	{
		#region Data Members

		protected LcmCache m_cache;
		protected ICmObject m_obj;
		protected int m_flid;
		protected string m_fieldName;
		protected Control m_mainControl;
		protected Panel m_panel;
		protected Button m_btnLauncher;
		protected IPersistenceProvider m_persistProvider;
		protected string m_displayNameProperty;
		protected string m_displayWs;
		// The following variables control features of the chooser dialog.
		protected XElement m_configurationNode = null;

		#endregion // Data Members
		private ImageList imageList1;
		private IContainer components;

		#region Properties
		protected Slice Slice
		{
			get
			{
				var parent = Parent;
				while (!(parent is Slice))
				{
					parent = parent.Parent;
				}

				Debug.Assert(parent is Slice);

				return parent as Slice;
			}
		}

		/// <summary>
		/// Get or set the main control.
		/// </summary>
		/// <remarks>
		/// It can only be set one time.
		/// </remarks>
		public virtual Control MainControl
		{
			set
			{
				Debug.Assert(value != null);
				Debug.Assert(m_mainControl == null);
				m_mainControl = value;
				m_mainControl.TabIndex = 0;
			}
			get { return m_mainControl; }
		}

		/// <summary>
		/// Get the launcher button.
		/// </summary>
		public Button LauncherButton => m_btnLauncher;

		/// <summary>
		/// Store or retrieve the XML configuration node associated with the parent slice.
		/// </summary>
		public virtual XElement ConfigurationNode
		{
			get { return m_configurationNode; }
			set { m_configurationNode = value; }
		}

		#endregion // Properties

		#region Construction, Initialization, and Disposing
		/// <summary>
		/// Initializes a new instance of the <see cref="ReferenceLauncher"/> class.
		/// </summary>
		public ButtonLauncher()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
			if (Application.RenderWithVisualStyles)
			{
				return;
			}
			m_btnLauncher.ImageIndex = 2;
			m_btnLauncher.BackColor = System.Drawing.SystemColors.Control;
		}

		/// <summary>
		/// Initialize the launcher.
		/// </summary>
		public virtual void Initialize(LcmCache cache, ICmObject obj, int flid, string fieldName, IPersistenceProvider persistProvider, string displayNameProperty, string displayWs)
		{
			Debug.Assert(cache != null);
			Debug.Assert(flid != 0);
			Debug.Assert(!string.IsNullOrEmpty(fieldName));

			m_displayNameProperty = displayNameProperty;
			m_displayWs = displayWs;
			m_persistProvider = persistProvider;
			m_cache = cache;
			m_obj = obj;
			m_flid = flid;
			m_fieldName = fieldName;
		}

		#region Implementation of IFlexComponent

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public virtual void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			FlexComponentCheckingService.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;
		}

		#endregion

		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; private set; }

		#endregion

		#region Implementation of IPublisherProvider

		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		public IPublisher Publisher { get; private set; }

		#endregion

		#region Implementation of ISubscriberProvider

		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		public ISubscriber Subscriber { get; private set; }

		#endregion

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
				components?.Dispose();
				if (m_mainControl != null && m_mainControl.Parent == null)
				{
					m_mainControl.Dispose();
				}
			}
			m_fieldName = null;
			m_cache = null;
			m_obj = null;
			m_persistProvider = null;
			m_configurationNode = null;
			m_mainControl = null;
			m_panel = null;
			m_btnLauncher = null;
			m_displayNameProperty = null;
			PropertyTable = null;
			Publisher = null;
			Subscriber = null;

			base.Dispose(disposing);
		}

		#endregion // Construction, Initialization, and Disposing

		/// <summary>
		/// Set this to create a target object if necessary when the user clicks the chooser button.
		/// May be left null if m_obj is never null.
		/// </summary>
		public Func<ICmObject> ObjectCreator { get; set; }

		/// <summary>
		/// Handle launching of the standard chooser.
		/// </summary>
		/// <remarks>
		/// Subclasses should override this method.
		/// </remarks>
		protected virtual void HandleChooser()
		{
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ButtonLauncher));
			this.m_panel = new System.Windows.Forms.Panel();
			this.m_btnLauncher = new System.Windows.Forms.Button();
			this.imageList1 = new System.Windows.Forms.ImageList(this.components);
			this.m_panel.SuspendLayout();
			this.SuspendLayout();
			//
			// m_panel
			//
			this.m_panel.Controls.Add(this.m_btnLauncher);
			resources.ApplyResources(this.m_panel, "m_panel");
			this.m_panel.Name = "m_panel";
			//
			// m_btnLauncher
			//
			resources.ApplyResources(this.m_btnLauncher, "m_btnLauncher");
			this.m_btnLauncher.ImageList = this.imageList1;
			this.m_btnLauncher.Name = "m_btnLauncher";
			this.m_btnLauncher.UseVisualStyleBackColor = true;
			this.m_btnLauncher.Click += new System.EventHandler(this.OnClick);
			//
			// imageList1
			//
			this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
			this.imageList1.TransparentColor = System.Drawing.Color.Fuchsia;
			this.imageList1.Images.SetKeyName(0, "ellipsis-vs.bmp");
			this.imageList1.Images.SetKeyName(1, "LeftArrow.bmp");
			this.imageList1.Images.SetKeyName(2, "ellipsis.bmp");
			//
			// ButtonLauncher
			//
			this.Controls.Add(this.m_panel);
			this.Name = "ButtonLauncher";
			resources.ApplyResources(this, "$this");
			this.Leave += new System.EventHandler(this.ReferenceLauncher_Leave);
			this.Enter += new System.EventHandler(this.ReferenceLauncher_Enter);
			this.m_panel.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		#region Event handlers

		/// <summary>
		/// Notifies that m_obj has been created.
		/// </summary>
		protected virtual void OnObjectCreated()
		{
		}

		/// <summary>
		/// Handle the click event for the launcher button.
		/// </summary>
		protected virtual void OnClick(object sender, EventArgs arguments)
		{
			bool fValid;
			if (m_obj == null && ObjectCreator != null)
			{
				m_obj = ObjectCreator();
				OnObjectCreated();
			}
			if (m_obj == null)
			{
				MessageBox.Show(DetailControlsStrings.ksNotInitialized);
				fValid = false;
			}
			else
			{
				fValid = m_obj.IsValidObject;
			}

			if (fValid)
			{
				HandleChooser();
			}
		}

		protected void ReferenceLauncher_Enter(object sender, EventArgs e)
		{
			m_mainControl?.Focus();
			if (!(Parent is IContainerControl))
			{
				return;
			}
			var uc = (IContainerControl)Parent;
			uc.ActiveControl = this;
		}

		protected void ReferenceLauncher_Leave(object sender, System.EventArgs e)
		{
		}

		#endregion Event handlers

		#region INotifyControlInCurrentSlice Members

		/// <summary>
		/// Adjust controls based on whether the slice is the current slice.
		/// </summary>
		public virtual bool SliceIsCurrent
		{
			set
			{
				// The panel (with the button) is visible only when the slice is current
				if (value)
				{
					m_panel.Show();
				}
				else
				{
					m_panel.Hide();
				}
			}
		}

		#endregion
	}
}
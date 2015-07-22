// Copyright (c) 2005-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ButtonLauncher.cs
// Responsibility: RandyR
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using SIL.FieldWorks.Common.Framework.DetailControls.Resources;
using XCore;
using System.Diagnostics.CodeAnalysis;
using SIL.CoreImpl;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	public class ButtonLauncher : UserControl, IFWDisposable, INotifyControlInCurrentSlice
	{
		#region event handler declarations

		//public event EventHandler ChoicesMade;

		#endregion event handler declarations

		#region Data Members

		protected FdoCache m_cache;
		protected ICmObject m_obj;
		protected int m_flid;
		protected string m_fieldName;
		protected Control m_mainControl;
		protected System.Windows.Forms.Panel m_panel;
		protected System.Windows.Forms.Button m_btnLauncher;
		protected IPersistenceProvider m_persistProvider;
		protected Mediator m_mediator;
		protected PropertyTable m_propertyTable;
		protected string m_displayNameProperty;
		protected string m_displayWs;
		// The following variables control features of the chooser dialog.
		protected System.Xml.XmlNode m_configurationNode = null;

		#endregion // Data Members
		private ImageList imageList1;
		private IContainer components;

		#region Properties

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "parent is a reference")]
		protected Slice Slice
		{
			get
			{
				// Depending on compile switch for SLICE_IS_SPLITCONTAINER,
				// grandParent will be both a Slice and a SplitContainer
				// (Slice is a subclass of SplitContainer),
				// or just a SplitContainer (SplitContainer is the only child Control of a Slice).
				// If grandParent is not a Slice, then we have to move up to the great-grandparent
				// to find the Slice.
				Control parent = Parent;
				while (!(parent is Slice))
					parent = parent.Parent;

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
				CheckDisposed();
				Debug.Assert(value != null);
				Debug.Assert(m_mainControl == null);
				m_mainControl = value;
				m_mainControl.TabIndex = 0;
			}
			get { CheckDisposed(); return m_mainControl; }
		}

		/// <summary>
		/// Get the launcher button.
		/// </summary>
		public Button LauncherButton
		{
			get { CheckDisposed(); return m_btnLauncher; }
		}

		/// <summary>
		/// Store or retrieve the XML configuration node associated with the parent slice.
		/// </summary>
		public virtual XmlNode ConfigurationNode
		{
			get { CheckDisposed(); return m_configurationNode; }
			set
			{
				CheckDisposed();
				m_configurationNode = value;
			}
		}

		#endregion // Properties

		#region Construction, Initialization, and Disposing
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ReferenceLauncher"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public ButtonLauncher()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
			if (!Application.RenderWithVisualStyles)
			{
				m_btnLauncher.ImageIndex = 2;
				m_btnLauncher.BackColor = System.Drawing.SystemColors.Control;
			}
		}

		/// <summary>
		/// Initialize the launcher.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="obj"></param>
		/// <param name="flid"></param>
		/// <param name="fieldName"></param>
		/// <param name="persistProvider"></param>
		/// <param name="mediator"></param>
		/// <param name="propertyTable"></param>
		/// <param name="displayNameProperty"></param>
		/// <param name="displayWs"></param>
		public virtual void Initialize(FdoCache cache, ICmObject obj, int flid, string fieldName,
			IPersistenceProvider persistProvider, Mediator mediator, PropertyTable propertyTable, string displayNameProperty, string displayWs)
		{
			Debug.Assert(cache != null);
			Debug.Assert(flid != 0);
			Debug.Assert(!string.IsNullOrEmpty(fieldName));

			m_displayNameProperty = displayNameProperty;
			m_displayWs = displayWs;
			m_persistProvider = persistProvider;
			m_mediator = mediator;
			m_propertyTable = propertyTable;

			m_cache = cache;
			m_obj = obj;
			m_flid = flid;
			m_fieldName = fieldName;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if(components != null)
				{
					components.Dispose();
				}
				if (m_mainControl != null && m_mainControl.Parent == null)
					m_mainControl.Dispose();
			}
			m_fieldName = null;
			m_cache = null;
			m_obj = null;
			m_persistProvider = null;
			m_mediator = null;
			m_propertyTable = null;
			m_configurationNode = null;
			m_mainControl = null;
			m_panel = null;
			m_btnLauncher = null;
			m_displayNameProperty = null;

			base.Dispose(disposing);
		}

		#endregion // Construction, Initialization, and Disposing

		#region IFWDisposable
		//// use the Control IsDisposed method
		///// <summary>
		///// See if the object has been disposed.
		///// </summary>
		//public bool IsDisposed
		//{
		//    get { return m_isDisposed; }
		//}

		/// <summary>
		/// Throw if the IsDisposed property is true
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException("ButtonLauncher", "This object is being used after it has been disposed: this is an Error.");
		}

		#endregion // IFWDisposable

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

		/// <summary>
		/// Get the mediator. A subclass may override this property.
		/// </summary>
		protected virtual Mediator Mediator
		{
			get { return m_mediator; }
		}

		#region Component Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
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
		/// <param name="sender"></param>
		/// <param name="arguments"></param>
		protected virtual void OnClick(Object sender, EventArgs arguments)
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
				HandleChooser();
		}

		protected void ReferenceLauncher_Enter(object sender, EventArgs e)
		{
			if (m_mainControl != null)
			{
				m_mainControl.Focus();
			}
			if (Parent is IContainerControl)
			{
				IContainerControl uc = (IContainerControl)Parent;
				uc.ActiveControl = this;
			}
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
				CheckDisposed();
				// The panel (with the button) is visible only when the slice is current
				if (value)
					m_panel.Show();
				else
					m_panel.Hide();
			}
		}

		#endregion
	}

	/// <summary>
	/// A control within a slice may implement this in order to receive notification when the slice
	/// becomes active.
	/// It's crazy to define this over in LexTextControls, but then, it's crazy for ButtonLauncher
	/// and most of its subclasses to be here, either. It's a historical artifact resulting from
	/// the fact that LexTextControls doesn't reference DetailControls; rather, DetailControls
	/// references LexTextControls. We need references both ways, but can't achieve it.
	/// </summary>
	public interface INotifyControlInCurrentSlice
	{
		bool SliceIsCurrent { set; }
	}
}

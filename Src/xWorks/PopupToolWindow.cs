using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;
using SIL.Utils;
using System;
using System.Windows.Forms;
using System.Xml;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	public class PopupToolWindow : Form, IxCoreColleague
	{
		protected Control m_mainContentControl; // Dispose manually, if it has no parent control.

		public Control MainControl
		{
			get { return m_mainContentControl; }
		}

		private PropertyTable m_propertyTable;
		private RecordClerk m_recordClerk;
		protected ActiveViewHelper m_viewHelper;

		public PopupToolWindow()
		{
			InitializeComponent();
		}

		private void InitializeComponent()
		{
			this.SuspendLayout();
			//
			// PopupToolWindow
			//
			this.AccessibleDescription = "a popup tool window";
			this.AccessibleName = "A Popup Tool Window";
			this.AccessibleRole = System.Windows.Forms.AccessibleRole.Window;
			this.AutoScaleMode = AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(700, 700);
			this.KeyPreview = true;
			this.Name = "PopupToolWindow";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
			this.Text = "Popup Tool Window";
			this.WindowState = System.Windows.Forms.FormWindowState.Normal;
			this.KeyPreview = true;
			this.KeyDown += new KeyEventHandler(PopupToolWindow_KeyDown);
			this.Deactivate += new System.EventHandler(PopupToolWindow_Deactivate);
			this.Activated += new System.EventHandler(PopupToolWindow_Activated);
			this.ResumeLayout(false);
		}

		private void PopupToolWindow_Deactivate(object sender, EventArgs e)
		{
			var oldActiveClerk = m_propertyTable.GetValue<RecordClerk>("OldActiveClerk");
			var oldUseRecordTreeBar = m_propertyTable.GetValue<Boolean>("OldUseRecordTreeBar");
			var oldOldUpdateStatusBar = m_propertyTable.GetValue<Boolean>("OldUpdateStatusBar");
			if (oldActiveClerk != null)
			{
				oldActiveClerk.ActivateUI(oldUseRecordTreeBar, oldOldUpdateStatusBar);
			}
		}

		private void PopupToolWindow_Activated(object sender, EventArgs e)
		{
			if (m_recordClerk != null)
			{
				m_recordClerk.ActivateUI(false);
			}
		}

		private void PopupToolWindow_KeyDown(object sender, KeyEventArgs e)
		{
			FwXWindow fwXWindow = Owner as FwXWindow;
			if (e.KeyCode == Keys.A && e.Control)
			{
				fwXWindow?.OnEditSelectAll(m_viewHelper);
			}
			if (e.KeyCode == Keys.C && e.Control)
			{
				fwXWindow?.OnEditCopy(m_viewHelper);
			}
			else if (e.KeyCode == Keys.V && e.Control)
			{
				fwXWindow?.OnEditPaste(m_viewHelper);
			}
			else if (e.KeyCode == Keys.X && e.Control)
			{
				fwXWindow?.OnEditCut(m_viewHelper);
			}
			else
			{
				// e.g. Control-Z.
				fwXWindow?.HandleKeyDown(e);
			}
		}

		private IxCoreColleague MainContentControlAsIxCoreColleague
		{
			get { return m_mainContentControl as IxCoreColleague; }
		}

		public bool ShouldNotCall
		{
			get { return IsDisposed; }
		}

		public int Priority
		{
			get { return (int)ColleaguePriority.Medium; }
		}

		public virtual IxCoreColleague[] GetMessageTargets()
		{
			return new IxCoreColleague[] { this };
		}

		public void Init(Mediator mediator, PropertyTable propertyTable, XmlNode toolNode)
		{
			m_propertyTable = propertyTable;
			XmlNode contentClassNode = toolNode.SelectSingleNode("control");
			XmlNode dynLoaderNode = contentClassNode.SelectSingleNode("dynamicloaderinfo");
			string contentAssemblyPath = XmlUtils.GetMandatoryAttributeValue(dynLoaderNode, "assemblyPath");
			string contentClass = XmlUtils.GetMandatoryAttributeValue(dynLoaderNode, "class");
			Control mainControl = (Control)DynamicLoader.CreateObject(contentAssemblyPath, contentClass);
			mainControl.SuspendLayout();
			m_mainContentControl = mainControl;
			m_mainContentControl.Dock = DockStyle.Fill;
			m_mainContentControl.AccessibleDescription = contentClass;
			m_mainContentControl.AccessibleName = contentClass;
			m_mainContentControl.TabStop = true;
			m_mainContentControl.TabIndex = 1;
			this.Controls.Add(mainControl);
			XmlNode parameters = null;
			if (contentClassNode != null)
				parameters = contentClassNode.SelectSingleNode("parameters");
			MainContentControlAsIxCoreColleague.Init(mediator, propertyTable, parameters);
			// We don't want it or any part of it drawn until we're done laying out.
			// Also, layout tends not to actually happen until we make it visible, which further helps avoid duplication,
			// and makes sure the user doesn't see any intermediate state.
			m_mainContentControl.Visible = false;
			mainControl.ResumeLayout(false);
			m_mainContentControl.BringToFront();
			m_mainContentControl.Visible = true;
			m_mainContentControl.Select();
			if (toolNode.Attributes["label"] != null)
			{
				this.Text = toolNode.Attributes["label"].Value;
			}
			m_viewHelper = new ActiveViewHelper(this);

			this.Show();
			this.BringToFront();
			this.Activate();
			m_recordClerk = m_propertyTable.GetValue<RecordClerk>("ActiveClerk");
		}
	}
}

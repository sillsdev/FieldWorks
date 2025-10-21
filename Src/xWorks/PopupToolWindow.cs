using SIL.Utils;
using System.Drawing;
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
			this.ClientSize = new System.Drawing.Size(400, 400);
			this.KeyPreview = true;
			this.Name = "PopupToolWindow";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
			this.Text = "Popup Tool Window";
			this.WindowState = System.Windows.Forms.FormWindowState.Normal;
			// this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.XWindow_KeyDown);
			// this.Resize += new System.EventHandler(this.XWindow_Resize);
			// this.Closing += new System.ComponentModel.CancelEventHandler(this.XWindow_Closing);
			// this.Move += new System.EventHandler(this.XWindow_Move);
			// this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.XWindow_KeyUp);
			// this.Activated += new System.EventHandler(this.XWindow_Activated);
			this.ResumeLayout(false);
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

		public void Init(Mediator mediator, PropertyTable propertyTable, XmlNode contentClassNode)
		{
			XmlNode dynLoaderNode = contentClassNode.SelectSingleNode("dynamicloaderinfo");
			string contentAssemblyPath = XmlUtils.GetMandatoryAttributeValue(dynLoaderNode, "assemblyPath");
			string contentClass = XmlUtils.GetMandatoryAttributeValue(dynLoaderNode, "class");
			Control mainControl = (Control)DynamicLoader.CreateObject(contentAssemblyPath, contentClass);
			mainControl.SuspendLayout();
			m_mainContentControl = mainControl;
			m_mainContentControl.Dock = DockStyle.Fill;
			m_mainContentControl.AccessibleDescription = "XXXXXXXXXXXX";
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
			this.Show();
			this.BringToFront();
			this.Activate();
		}
	}
}

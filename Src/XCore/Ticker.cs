// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: Ticker.cs
// Authorship History: John Hatton
// Last reviewed:
//
// <remarks>
// </remarks>
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using SIL.CoreImpl;

namespace XCore
{
	/// <summary>
	///
	/// </summary>
	/// <remarks>
	/// IxCoreContentControl includes IxCoreColleague now,
	/// so only IxCoreContentControl needs to be declared here.
	/// </remarks>
	public class Ticker : XCoreUserControl, IxCoreContentControl
	{
		private RichTextBox m_textBox;
		private System.ComponentModel.IContainer components=null;
		private Button button1;
		protected Mediator m_mediator;
		protected PropertyTable m_propertyTable;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="UserControl1"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public Ticker()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitForm call

			base.AccNameDefault = "Ticker";	// default accessibility name
		}

		#region IxCoreContentControl implementation

		public bool PrepareToGoAway()
		{
			CheckDisposed();

			return true;
		}

		public string AreaName
		{
			get
			{
				CheckDisposed();

				return "test";
			}
		}

		#endregion // IxCoreContentControl implementation

		#region IxCoreCtrlTabProvider implementation

		public Control PopulateCtrlTabTargetCandidateList(List<Control> targetCandidates)
		{
			if (targetCandidates == null)
				throw new ArgumentNullException("'targetCandidates' is null.");

			targetCandidates.Add(this);

			return ContainsFocus ? this : null;
		}

		#endregion  IxCoreCtrlTabProvider implementation

		#region IxCoreColleague implementation

		/// <summary>
		/// Initialize this has an IxCoreColleague
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="propertyTable"></param>
		/// <param name="configurationParameters"></param>
		public void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configurationParameters)
		{
			CheckDisposed();

			m_mediator = mediator;
			m_configurationParameters = configurationParameters;
			mediator.AddColleague(this);
		}

		/// <summary>
		/// return an array of all of the objects which should
		/// 1) be queried when looking for someone to deliver a message to
		/// 2) be potential recipients of a broadcast
		/// </summary>
		/// <returns></returns>
		public IxCoreColleague[] GetMessageTargets()
		{
			CheckDisposed();

			return new IxCoreColleague[]{this};
		}

		/// <summary>
		/// Should not be called if disposed.
		/// </summary>
		public bool ShouldNotCall
		{
			get { return IsDisposed; }
		}

		/// <summary>
		/// Mediator message handling Priority
		/// </summary>
		public int Priority
		{
			get { return (int)ColleaguePriority.Low; }
		}

		#endregion // IxCoreColleague implementation

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// -----------------------------------------------------------------------------------
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
				if (m_mediator != null)
					m_mediator.RemoveColleague(this);
			}
			m_mediator = null;
			m_propertyTable = null;

			base.Dispose( disposing );
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
			this.m_textBox = new System.Windows.Forms.RichTextBox();
			this.button1 = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// m_textBox
			//
			this.m_textBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
				| System.Windows.Forms.AnchorStyles.Left)
				| System.Windows.Forms.AnchorStyles.Right)));
			this.m_textBox.Location = new System.Drawing.Point(8, 8);
			this.m_textBox.Name = "m_textBox";
			this.m_textBox.Size = new System.Drawing.Size(496, 488);
			this.m_textBox.TabIndex = 0;
			this.m_textBox.Text = "";
			this.m_textBox.MouseDown += new System.Windows.Forms.MouseEventHandler(this.OnMouseDown);
			//
			// button1
			//
			this.button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.button1.ForeColor = System.Drawing.SystemColors.Control;
			this.button1.Location = new System.Drawing.Point(16, 8);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(32, 32);
			this.button1.TabIndex = 1;
			//
			// Ticker
			//
			this.BackColor = System.Drawing.Color.FromArgb(((System.Byte)(255)), ((System.Byte)(224)), ((System.Byte)(192)));
			this.Controls.Add(this.m_textBox);
			this.Name = "Ticker";
			this.Size = new System.Drawing.Size(512, 504);
			this.ResumeLayout(false);

		}
		#endregion

		#region Message Handlers
		/// <summary>
		/// Receives the broadcast message "PropertyChanged"
		/// </summary>
		public void OnPropertyChanged(string name)
		{
			CheckDisposed();

			WriteLine ("property '" + name + "' changed.");
		}

		/// <summary>
		/// Receives the  message "ListPropertiesTable"
		/// </summary>
		public bool OnListPropertiesTable(object argument)
		{
			CheckDisposed();

			WriteLine ("--------Properties table");
			WriteLine(m_propertyTable.GetPropertiesDumpString());
			return true;//we handled this, no need to ask anyone else.
		}
		/// <summary>
		/// Receives the  message "ListPropertiesTable"
		/// </summary>
		public bool OnListColleagues(object argument)
		{
			CheckDisposed();

			WriteLine ("--------Colleagues");
			WriteLine(m_mediator.GetColleaguesDumpString());
			return true;//we handled this, no need to ask anyone else.
		}

		/// <summary>
		/// enable this command because we are listening for it.
		/// </summary>
		public bool OnDisplayCmdListPropertiesTable(object parameters, ref  UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Enabled = true;
			return true;//we handled this, no need to ask anyone else.
		}
		#endregion

		protected void WriteLine(string s)
		{
			m_textBox.Text += s + Environment.NewLine;
		}

		private void OnMouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				XWindow window = (XWindow)FindForm();
//				window.ShowContextMenu("TestContextMenu",this, e.X,e.Y);
				window.ShowContextMenu("TestContextMenu", new Point(e.X, e.Y), null, null);
//				ContextMenu menu =window.ShowContextMenu("TestContextMenu");
//				Point p = new Point(e.X,e.Y);
////				//this.Invoke(menu.Popup, new object[]{e});//
//				menu.Show(m_textBox, p);
			}
		 }
	}
}

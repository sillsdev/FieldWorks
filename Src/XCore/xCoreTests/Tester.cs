// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: Tester.cs
// Authorship History: John Hatton
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml;
using SIL.CoreImpl;
using SIL.Utils;

namespace XCore
{
	/// <summary>
	/// the Tester class does various things in support of the unit tests.
	/// </summary>
	public class Tester : UserControl, IFWDisposable, IxCoreContentControl
	{
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		public System.Windows.Forms.TextBox letters;
		public System.Windows.Forms.TextBox numbers;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		public System.Windows.Forms.TextBox output;
		public System.Windows.Forms.CheckBox cbEnableTest;
		public System.Windows.Forms.CheckBox cbModifyVowelList;

		protected Mediator m_mediator;
		protected PropertyTable m_propertyTable;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="Tester"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public Tester()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			// TODO: Add any initialization after the InitForm call

		}
		#region IxCoreColleague stuff

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
			m_propertyTable = propertyTable;
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

		public int Priority
		{
			get { return (int)ColleaguePriority.Low; }
		}

		#endregion

		#region IXCoreUserControl implementation

		public string AccName
		{
			get { return "Tester"; }
		}

		#endregion IXCoreUserControl implementation

		#region IxCoreContentControl implementation

		public bool PrepareToGoAway()
		{
			return true;
		}

		public string AreaName
		{
			get { return "Tester"; }
		}

		#endregion IxCoreContentControl implementation

		#region IxCoreCtrlTabProvider implementation

		public Control PopulateCtrlTabTargetCandidateList(List<Control> targetCandidates)
		{
			if (targetCandidates == null)
				throw new ArgumentNullException("'targetCandidates' is null.");

			targetCandidates.Add(this);

			return ContainsFocus ? this : null;
		}

		#endregion  IxCoreCtrlTabProvider implementation

		/// <summary>
		/// Receives the  message "TypeLetter"
		/// </summary>
		public bool OnTypeLetter(object argument)
		{
			CheckDisposed();

			//string s ="";
			Command command = (Command) argument;

			letters.Text+=command.GetParameter("text", "?");
			return true;
		}

		/// <summary>
		/// Receives the broadcast message "PropertyChanged"
		/// </summary>
		public void OnPropertyChanged(string name)
		{
			CheckDisposed();

			WriteOutputLine ("property '"+name+"' changed.");
		}

		/// <summary>
		/// Receives the  message "ClearFields"
		/// </summary>
		public bool OnClearFields(object argument)
		{
			CheckDisposed();

			letters.Text = "";
			numbers.  Text = "";
			output.Text="";
			return true;
		}
		/// <summary>
		/// Receives the  message "ListPropertiesTable"
		/// </summary>
		public bool OnListPropertiesTable(object argument)
		{
			CheckDisposed();

			WriteOutputLine ("--------Properties table");
			WriteOutputLine(m_propertyTable.GetPropertiesDumpString());
			return true;//we handled this, no need to ask anyone else.
		}
		/// <summary>
		/// enable this command because we are listening for it.
		/// </summary>
		public bool OnDisplayCmdListPropertiesTable(object parameters, ref  UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.  Enabled = true;
			return true;//we handled this, no need to ask anyone else.
		}

		/// <summary>
		/// enable this command iff the corresponding check box is checked.
		/// </summary>
		public bool OnDisplayCmdEnableTest(object parameters, ref  UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.  Enabled = cbEnableTest.Checked;
			return true;//we handled this, no need to ask anyone else.
		}

		/// <summary>
		/// enable this list of items iff the corresponding check box is checked.
		/// </summary>
		public bool OnDisplayListVowels(object parameters, ref  UIListDisplayProperties display)
		{
			CheckDisposed();

			if (cbModifyVowelList.Checked)
			{
				display.List.Add("AA", "AA", "", null);
				display.List.Add("OO", "OO", "", null);
			}
			return true;//we handled this, no need to ask anyone else.
		}

		protected void WriteOutputLine(string s)
		{
			output.Text += s + Environment.NewLine;
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
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.letters = new System.Windows.Forms.TextBox();
			this.numbers = new System.Windows.Forms.TextBox();
			this.output = new System.Windows.Forms.TextBox();
			this.cbEnableTest = new System.Windows.Forms.CheckBox();
			this.cbModifyVowelList = new System.Windows.Forms.CheckBox();
			this.SuspendLayout();
			//
			// label1
			//
			this.label1.Location = new System.Drawing.Point(8, 8);
			this.label1.Name = "label1";
			this.label1.TabIndex = 0;
			this.label1.Text = "letters";
			//
			// label2
			//
			this.label2.Location = new System.Drawing.Point(8, 56);
			this.label2.Name = "label2";
			this.label2.TabIndex = 1;
			this.label2.Text = "numbers";
			//
			// label3
			//
			this.label3.Location = new System.Drawing.Point(8, 96);
			this.label3.Name = "label3";
			this.label3.TabIndex = 2;
			this.label3.Text = "output";
			//
			// letters
			//
			this.letters.Location = new System.Drawing.Point(136, 8);
			this.letters.Name = "letters";
			this.letters.Size = new System.Drawing.Size(256, 20);
			this.letters.TabIndex = 3;
			this.letters.Text = "";
			//
			// numbers
			//
			this.numbers.Location = new System.Drawing.Point(136, 48);
			this.numbers.Name = "numbers";
			this.numbers.Size = new System.Drawing.Size(256, 20);
			this.numbers.TabIndex = 4;
			this.numbers.Text = "";
			//
			// output
			//
			this.output.Location = new System.Drawing.Point(136, 88);
			this.output.Multiline = true;
			this.output.Name = "output";
			this.output.Size = new System.Drawing.Size(256, 168);
			this.output.TabIndex = 5;
			this.output.Text = "";
			//
			// cbEnableTest
			//
			this.cbEnableTest.Location = new System.Drawing.Point(24, 280);
			this.cbEnableTest.Name = "cbEnableTest";
			this.cbEnableTest.Size = new System.Drawing.Size(128, 24);
			this.cbEnableTest.TabIndex = 6;
			this.cbEnableTest.Text = "Enable \'EnableTest\'";
			//
			// cbModifyVowelList
			//
			this.cbModifyVowelList.Location = new System.Drawing.Point(24, 304);
			this.cbModifyVowelList.Name = "cbModifyVowelList";
			this.cbModifyVowelList.Size = new System.Drawing.Size(128, 24);
			this.cbModifyVowelList.TabIndex = 7;
			this.cbModifyVowelList.Text = "Enable \'Vowels\'";
			//
			// Tester
			//
			this.Controls.AddRange(new System.Windows.Forms.Control[] {
																		  this.cbModifyVowelList,
																		  this.cbEnableTest,
																		  this.output,
																		  this.numbers,
																		  this.letters,
																		  this.label3,
																		  this.label2,
																		  this.label1});
			this.Name = "Tester";
			this.Size = new System.Drawing.Size(424, 344);
			this.ResumeLayout(false);

		}
		#endregion
	}
}

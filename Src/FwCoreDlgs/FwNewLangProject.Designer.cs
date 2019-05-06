using System;
using System.Windows.Forms;

namespace SIL.FieldWorks.FwCoreDlgs
{
	public partial class FwNewLangProject
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;
		private HelpProvider helpProvider1;
		private TableLayoutPanel _tableLayoutPanel;
		private FlowLayoutPanel _buttonPannel;
		private Button btnHelp;
		private Button btnOK;
		private Button _next;
		private Button _previous;
		private Panel _mainContentPanel;


		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.Windows.Forms.Button btnCancel;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FwNewLangProject));
			this.helpProvider1 = new System.Windows.Forms.HelpProvider();
			this.btnHelp = new System.Windows.Forms.Button();
			this.btnOK = new System.Windows.Forms.Button();
			this._tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this._buttonPannel = new System.Windows.Forms.FlowLayoutPanel();
			this._next = new System.Windows.Forms.Button();
			this._previous = new System.Windows.Forms.Button();
			this._mainContentPanel = new System.Windows.Forms.Panel();
			this._stepsPanel = new System.Windows.Forms.TableLayoutPanel();
			btnCancel = new System.Windows.Forms.Button();
			this._tableLayoutPanel.SuspendLayout();
			this._buttonPannel.SuspendLayout();
			this.SuspendLayout();
			// 
			// btnCancel
			// 
			btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.helpProvider1.SetHelpString(btnCancel, resources.GetString("btnCancel.HelpString"));
			resources.ApplyResources(btnCancel, "btnCancel");
			btnCancel.Name = "btnCancel";
			this.helpProvider1.SetShowHelp(btnCancel, ((bool)(resources.GetObject("btnCancel.ShowHelp"))));
			// 
			// btnHelp
			// 
			this.helpProvider1.SetHelpString(this.btnHelp, resources.GetString("btnHelp.HelpString"));
			resources.ApplyResources(this.btnHelp, "btnHelp");
			this.btnHelp.Name = "btnHelp";
			this.helpProvider1.SetShowHelp(this.btnHelp, ((bool)(resources.GetObject("btnHelp.ShowHelp"))));
			// 
			// btnOK
			// 
			this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			resources.ApplyResources(this.btnOK, "btnOK");
			this.helpProvider1.SetHelpString(this.btnOK, resources.GetString("btnOK.HelpString"));
			this.btnOK.Name = "btnOK";
			this.helpProvider1.SetShowHelp(this.btnOK, ((bool)(resources.GetObject("btnOK.ShowHelp"))));
			this.btnOK.Click += new System.EventHandler(this.OnFinishClick);
			// 
			// _tableLayoutPanel
			// 
			resources.ApplyResources(this._tableLayoutPanel, "_tableLayoutPanel");
			this._tableLayoutPanel.Controls.Add(this._buttonPannel, 0, 2);
			this._tableLayoutPanel.Controls.Add(this._mainContentPanel, 0, 1);
			this._tableLayoutPanel.Controls.Add(this._stepsPanel, 0, 0);
			this._tableLayoutPanel.Name = "_tableLayoutPanel";
			// 
			// _buttonPannel
			// 
			this._buttonPannel.Controls.Add(this.btnHelp);
			this._buttonPannel.Controls.Add(btnCancel);
			this._buttonPannel.Controls.Add(this.btnOK);
			this._buttonPannel.Controls.Add(this._next);
			this._buttonPannel.Controls.Add(this._previous);
			resources.ApplyResources(this._buttonPannel, "_buttonPannel");
			this._buttonPannel.Name = "_buttonPannel";
			// 
			// _next
			// 
			resources.ApplyResources(this._next, "_next");
			this._next.Name = "_next";
			this._next.UseVisualStyleBackColor = true;
			this._next.Click += new System.EventHandler(this.OnNextClick);
			// 
			// _previous
			// 
			resources.ApplyResources(this._previous, "_previous");
			this._previous.Name = "_previous";
			this._previous.UseVisualStyleBackColor = true;
			this._previous.Click += new System.EventHandler(this.OnPreviousClick);
			// 
			// _mainContentPanel
			// 
			resources.ApplyResources(this._mainContentPanel, "_mainContentPanel");
			this._mainContentPanel.Name = "_mainContentPanel";
			this._mainContentPanel.TabStop = true;
			// 
			// _stepsPanel
			// 
			resources.ApplyResources(this._stepsPanel, "_stepsPanel");
			this._stepsPanel.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.AddColumns;
			this._stepsPanel.Name = "_stepsPanel";
			// 
			// FwNewLangProject
			// 
			this.AcceptButton = this.btnOK;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = btnCancel;
			this.Controls.Add(this._tableLayoutPanel);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FwNewLangProject";
			this.helpProvider1.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			this._tableLayoutPanel.ResumeLayout(false);
			this._buttonPannel.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion // Windows Form Designer generated code

		/// <summary/>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "********* Missing Dispose() call for " + GetType().Name + ". *******");
			if (disposing && !IsDisposed)
			{
				var disposable = m_wsManager as IDisposable;
				if (disposable != null)
					disposable.Dispose();
			}
			base.Dispose(disposing);
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

		private TableLayoutPanel _stepsPanel;
	}
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	public class MorphTypeChooser : SimpleListChooser
	{
		private System.Windows.Forms.CheckBox cbShowAllTypes;
		private ICmObject m_obj;
		private string m_displayNameProperty;
		private int m_flid;

		//private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// constructor for use with adding a new value
		/// </summary>
		/// <param name="labels"></param>
		/// <param name="fieldName">the user-readable name of the field that is being edited
		/// </param>
		public MorphTypeChooser(IPersistenceProvider persistProvider,
			ObjectLabelCollection labels, string fieldName) :
			base(persistProvider, labels, fieldName)
		{
			InitMorphTypeForm(null);
		}

		private void InitMorphTypeForm(string sShowAllTypes)
		{
			InitializeComponent();
			if (sShowAllTypes == null || sShowAllTypes.Length == 0)
				sShowAllTypes = "&Show all types";
			else
				sShowAllTypes = sShowAllTypes.Replace("_", "&");
			cbShowAllTypes.Text = sShowAllTypes;
			int nVerticalFix = cbShowAllTypes.Height + 4;
			m_labelsTreeView.Height -= nVerticalFix - 2;
			int nLeft = m_labelsTreeView.Left;
			int nTop = btnOK.Top - nVerticalFix;
			cbShowAllTypes.Location = new System.Drawing.Point(nLeft, nTop);
			cbShowAllTypes.Visible = true;
		}
		/// <summary>
		/// constructor for use with adding a new value
		/// </summary>
		/// <param name="labels"></param>
		/// <param name="fieldName">the user-readable name of the field that is being edited
		/// </param>
		public MorphTypeChooser(IPersistenceProvider persistProvider,
			ObjectLabelCollection labels, string fieldName, ICmObject obj, string displayNameProperty,
			int flid, string sShowAllTypes) :
			base(persistProvider, labels, fieldName)
		{
			m_obj = obj;
			m_displayNameProperty = displayNameProperty;
			m_flid = flid;
			InitMorphTypeForm(sShowAllTypes);
		}
		/// <summary>
		/// Get/set visibility of show all types check box
		/// </summary>
		public bool ShowAllTypesCheckBoxVisible
		{
			get
			{
				CheckDisposed();

				return cbShowAllTypes.Visible;
			}
			set
			{
				CheckDisposed();

				cbShowAllTypes.Visible = value;
			}
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MorphTypeChooser));
			this.cbShowAllTypes = new System.Windows.Forms.CheckBox();
			((System.ComponentModel.ISupportInitialize)(this.m_picboxLink2)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_picboxLink1)).BeginInit();
			this.SuspendLayout();
			//
			// cbShowAllTypes
			//
			resources.ApplyResources(this.cbShowAllTypes, "cbShowAllTypes");
			this.cbShowAllTypes.Name = "cbShowAllTypes";
			this.cbShowAllTypes.CheckedChanged += new System.EventHandler(this.cbShowAllTypes_CheckedChanged);
			//
			// MorphTypeChooser
			//
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.cbShowAllTypes);
			this.Name = "MorphTypeChooser";
			this.Controls.SetChildIndex(this.cbShowAllTypes, 0);
			this.Controls.SetChildIndex(this.btnOK, 0);
			this.Controls.SetChildIndex(this.btnCancel, 0);
			this.Controls.SetChildIndex(this.m_labelsTreeView, 0);
			this.Controls.SetChildIndex(this.m_lblLink2, 0);
			this.Controls.SetChildIndex(this.m_picboxLink2, 0);
			this.Controls.SetChildIndex(this.m_lblLink1, 0);
			this.Controls.SetChildIndex(this.m_picboxLink1, 0);
			this.Controls.SetChildIndex(this.m_lblExplanation, 0);
			((System.ComponentModel.ISupportInitialize)(this.m_picboxLink2)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_picboxLink1)).EndInit();
			this.ResumeLayout(false);
		}
		#endregion

		private void cbShowAllTypes_CheckedChanged(object sender, System.EventArgs e)
		{
			// If a node is selected, try selecting again when we get through.
			int hvoSelected = SelectedHvo;
			Set<int> candidates = null;
			string displayWs = "best analorvern";
			if (cbShowAllTypes.Checked)
			{
				MoForm form = m_obj as MoForm;
				candidates = form.GetAllMorphTypeReferenceTargetCandidates();
			}
			else
			{
				candidates = m_obj.ReferenceTargetCandidates(m_flid);
			}
			ObjectLabelCollection labels = new ObjectLabelCollection(m_cache, candidates,
				m_displayNameProperty, displayWs);
			LoadTree(labels, 0, false);
			MakeSelection(hvoSelected);
		}
	}
}

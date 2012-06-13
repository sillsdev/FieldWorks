// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2003' to='2004' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ParserParametersDlg.cs
// Responsibility: Andy Black
// Last reviewed:
//
// <remarks>
// Implementation of:
//		ParserParametersDlg - Dialog for editing XML representation of parser parameters
//                            (MoMorphData : ParserParameters)
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Xml;
using System.Data;
using System.Diagnostics;

using SIL.FieldWorks.Common.Framework;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Summary description for ParserParametersDlg.
	/// </summary>
	public class ParserParametersDlg : Form, IFWDisposable
	{
		#region Data members

		/// <summary>
		/// member strings
		/// </summary>
#pragma warning disable 0414
		private string m_sXmlErrorText = "The XML is not well-formed; you need to correct it and try again.";
		private string m_sXmlErrorCaption = "Ill-formed XML";
		private string m_sXmlParameters;
#pragma warning restore 0414

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private Container components = null;

		private IHelpTopicProvider m_helpTopicProvider;
		private Label label1;
		private Label label2;
		private Button btnOK;
		private Button btnCancel;
		private Button btnHelp;
		private DataGrid dataGrid1;

		private const string s_helpTopic = "khtpParserParamters";
		private HelpProvider helpProvider;
		private DataGrid dataGrid2;
		private Label label3;

		private DataSet m_dsParserParameters = null;

		#endregion Data members

		private ParserParametersDlg()
		{
			InitializeComponent();
			AccessibleName = GetType().Name;
		}

		public ParserParametersDlg(IHelpTopicProvider helpTopicProvider) :
			this(ParserUIStrings.ksXmlNotWellFormed, ParserUIStrings.ksIllFormedXml, helpTopicProvider)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// To allow one to pass in mediated string table values for the XML error message
		/// </summary>
		/// <param name="sXmlErrorText">The s XML error text.</param>
		/// <param name="sXmlErrorCaption">The s XML error caption.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		public ParserParametersDlg(string sXmlErrorText, string sXmlErrorCaption,
			IHelpTopicProvider helpTopicProvider) : this()
		{
			m_sXmlErrorText = sXmlErrorText;
			m_sXmlErrorCaption = sXmlErrorCaption;
			m_helpTopicProvider = helpTopicProvider;

			helpProvider = new HelpProvider();
			helpProvider.HelpNamespace = m_helpTopicProvider.HelpFile;
			helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
			helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(s_helpTopic));
			helpProvider.SetShowHelp(this, true);
		}

		/// <summary>
		///Get or set the parser parameters XML text
		///</summary>
		public string XMLRep
		{
			get
			{
				CheckDisposed();

				return m_sXmlParameters;
			}
			set
			{
				CheckDisposed();

				m_sXmlParameters = value;
			}
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

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
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

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ParserParametersDlg));
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.btnOK = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.dataGrid1 = new System.Windows.Forms.DataGrid();
			this.btnHelp = new System.Windows.Forms.Button();
			this.dataGrid2 = new System.Windows.Forms.DataGrid();
			this.label3 = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.dataGrid1)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.dataGrid2)).BeginInit();
			this.SuspendLayout();
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			//
			// btnOK
			//
			this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			resources.ApplyResources(this.btnOK, "btnOK");
			this.btnOK.Name = "btnOK";
			this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
			//
			// btnCancel
			//
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.btnCancel, "btnCancel");
			this.btnCancel.Name = "btnCancel";
			//
			// dataGrid1
			//
			resources.ApplyResources(this.dataGrid1, "dataGrid1");
			this.dataGrid1.DataMember = global::SIL.FieldWorks.LexText.Controls.ParserUIStrings.ksIdle_;
			this.dataGrid1.HeaderForeColor = System.Drawing.SystemColors.ControlText;
			this.dataGrid1.Name = "dataGrid1";
			//
			// btnHelp
			//
			resources.ApplyResources(this.btnHelp, "btnHelp");
			this.btnHelp.Name = "btnHelp";
			this.btnHelp.UseVisualStyleBackColor = true;
			this.btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			//
			// dataGrid2
			//
			resources.ApplyResources(this.dataGrid2, "dataGrid2");
			this.dataGrid2.DataMember = global::SIL.FieldWorks.LexText.Controls.ParserUIStrings.ksIdle_;
			this.dataGrid2.HeaderForeColor = System.Drawing.SystemColors.ControlText;
			this.dataGrid2.Name = "dataGrid2";
			//
			// label3
			//
			resources.ApplyResources(this.label3, "label3");
			this.label3.Name = "label3";
			//
			// ParserParametersDlg
			//
			this.AcceptButton = this.btnOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.btnCancel;
			this.Controls.Add(this.label3);
			this.Controls.Add(this.dataGrid2);
			this.Controls.Add(this.btnHelp);
			this.Controls.Add(this.dataGrid1);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "ParserParametersDlg";
			((System.ComponentModel.ISupportInitialize)(this.dataGrid1)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.dataGrid2)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		void btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, s_helpTopic);
		}

		private void btnOK_Click(object sender, System.EventArgs e)
		{
			XmlDocument newDoc = new XmlDocument();
			newDoc.LoadXml(m_dsParserParameters.GetXml());
			XmlDocument oldDoc = new XmlDocument();
			oldDoc.LoadXml(XMLRep);
			XmlNode oldParserNode = oldDoc.SelectSingleNode("/ParserParameters/ActiveParser");
			if (oldParserNode != null)
			{
				XmlNode paramsNode = newDoc.SelectSingleNode("/ParserParameters");
				XmlNode newParserNode = newDoc.CreateElement("ActiveParser");
				paramsNode.AppendChild(newParserNode);
				newParserNode.InnerText = oldParserNode.InnerText;
			}
			XMLRep = newDoc.OuterXml;
			ValidateValues(newDoc);
		}

		private void ValidateValues(XmlDocument doc)
		{
			const string ksXAmpleXPath = "/ParserParameters/XAmple/";
			EnforceValidValue(doc, ksXAmpleXPath, "MaxNulls", 0, 10);
			EnforceValidValue(doc, ksXAmpleXPath, "MaxPrefixes", 0, 25);
			EnforceValidValue(doc, ksXAmpleXPath, "MaxSuffixes", 0, 25);
			EnforceValidValue(doc, ksXAmpleXPath, "MaxInfixes", 0, 7);
			EnforceValidValue(doc, ksXAmpleXPath, "MaxInterfixes", 0, 7);
			EnforceValidValue(doc, ksXAmpleXPath, "MaxRoots", 0, 10);
			EnforceValidValue(doc, ksXAmpleXPath, "MaxAnalysesToReturn", -1, 10000, true);

			const string ksHCXPath = "/ParserParameters/HC/";
			EnforceValidValue(doc, ksHCXPath, "DelReapps", 0, 10);
		}

		private void EnforceValidValue(XmlDocument doc, string sXPath, string sItem, int iMin, int iMax)
		{
			EnforceValidValue(doc, sXPath, sItem, iMin, iMax, false);
		}

		private void EnforceValidValue(XmlDocument doc, string sXPath, string sItem, int iMin, int iMax, bool fUseMinIfZero)
		{
			XmlNode node = doc.SelectSingleNode(sXPath + sItem);
			if (node != null)
			{
				int val = Convert.ToInt32(node.InnerText);
				if (val < iMin || (fUseMinIfZero && val == 0))
				{
					node.InnerText = iMin.ToString();
					XMLRep = doc.OuterXml;
					ReportChangeOfValue(sItem, val, iMin, iMin, iMax);
				}
				else if (val > iMax)
				{
					node.InnerText = iMax.ToString();
					XMLRep = doc.OuterXml;
					ReportChangeOfValue(sItem, val, iMax, iMin, iMax);
				}
			}
		}

		private void ReportChangeOfValue(string sItem, int value, int iNew, int iMin, int iMax)
		{
			string sMessage = String.Format(ParserUIStrings.ksChangedValueReport, sItem, value, iNew, iMin, iMax);
			MessageBox.Show(sMessage, ParserUIStrings.ksChangeValueDialogTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}
		/// <summary>
		/// Set up the dlg in preparation to showing it.
		/// </summary>
		/// <param name="wp">Strings used for various items in this dialog.</param>
		public void SetDlgInfo(string sTitle, string sOKButton, string parserParameters)
		{
			CheckDisposed();

			XMLRep = parserParameters;
			Text = sTitle;
			btnOK.Text = sOKButton;

			m_dsParserParameters = new DataSet();
			m_dsParserParameters.DataSetName = "ParserParameters";

			DataTable tblXAmple = CreateXAmpleDataTable();
			m_dsParserParameters.Tables.Add(tblXAmple);
			DataTable tblHC = CreateHCDataTable();
			m_dsParserParameters.Tables.Add(tblHC);

			LoadParserData(m_dsParserParameters);

			PopulateDataGrid(dataGrid1, "XAmple");
			PopulateDataGrid(dataGrid2, "HC");
			if (m_dsParserParameters.Tables["HC"].Rows[0][ParserUIStrings.ksMaxDelReapps] == DBNull.Value)
				m_dsParserParameters.Tables["HC"].Rows[0][ParserUIStrings.ksMaxDelReapps] = 0;
		}

		private void LoadParserData(DataSet dsParserParameters)
		{
			using (System.IO.StringReader xmlSR = new System.IO.StringReader(XMLRep))
				dsParserParameters.ReadXml(xmlSR, XmlReadMode.IgnoreSchema);
		}

		private void PopulateDataGrid(DataGrid dataGrid, string parser)
		{
			if (m_dsParserParameters.Tables[parser].Rows.Count == 0)
			{
				DataRow row = m_dsParserParameters.Tables[parser].NewRow();
				m_dsParserParameters.Tables[parser].Rows.Add(row);
			}

			dataGrid.SetDataBinding(m_dsParserParameters, parser);

			DataView view = CreateDataView(m_dsParserParameters.Tables[parser]);
			dataGrid.DataSource = view;
		}

		private DataView CreateDataView(DataTable table)
		{
			DataView view = new DataView(table);
			view.AllowNew = false;
			return view;
		}

		private DataTable CreateXAmpleDataTable()
		{
			DataTable tblXAmple = new DataTable("XAmple");
			tblXAmple.Columns.Add(ParserUIStrings.ksMaxNulls, typeof(int));
			tblXAmple.Columns.Add(ParserUIStrings.ksMaxPrefixes, typeof(int));
			tblXAmple.Columns.Add(ParserUIStrings.ksMaxInfixes, typeof(int));
			tblXAmple.Columns.Add(ParserUIStrings.ksMaxRoots, typeof(int));
			tblXAmple.Columns.Add(ParserUIStrings.ksMaxSuffixes, typeof(int));
			tblXAmple.Columns.Add(ParserUIStrings.ksMaxInterfixes, typeof(int));
			tblXAmple.Columns.Add(ParserUIStrings.ksMaxAnalysesToReturn, typeof(int));
			return tblXAmple;
		}

		private DataTable CreateHCDataTable()
		{
			DataTable tblHC = new DataTable("HC");
			tblHC.Columns.Add(ParserUIStrings.ksMaxDelReapps, typeof(int));
			tblHC.Columns.Add(ParserUIStrings.ksRulesDoNotApplyOnClitics, typeof(bool));
			return tblHC;
		}
	}
}

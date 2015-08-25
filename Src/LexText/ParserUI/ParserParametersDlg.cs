// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
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

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using System.Data;
using System.Xml.Linq;
using SIL.CoreImpl;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Summary description for ParserParametersDlg.
	/// </summary>
	public class ParserParametersDlg : Form, IFWDisposable
	{
		private const string HelpTopic = "khtpParserParamters";

		private const string HC = "HC";
		private const string DelReapps = "DelReapps";
		private const string NotOnClitics = "NotOnClitics";
		private const string NoDefaultCompounding = "NoDefaultCompounding";

		private const string XAmple = "XAmple";
		private const string MaxNulls = "MaxNulls";
		private const string MaxPrefixes = "MaxPrefixes";
		private const string MaxSuffixes = "MaxSuffixes";
		private const string MaxInfixes = "MaxInfixes";
		private const string MaxInterfixes = "MaxInterfixes";
		private const string MaxRoots = "MaxRoots";
		private const string MaxAnalysesToReturn = "MaxAnalysesToReturn";

		#region Data members

		/// <summary>
		/// member strings
		/// </summary>
		private string m_sXmlParameters;

		private readonly IHelpTopicProvider m_helpTopicProvider;
		private Label m_label1;
		private Label m_label2;
		private Button m_btnOk;
		private Button m_btnCancel;
		private Button m_btnHelp;
		private DataGrid m_dataGrid1;

		private DataGrid m_dataGrid2;
		private Label m_label3;

		private DataSet m_dsParserParameters;

		#endregion Data members

		private ParserParametersDlg()
		{
			InitializeComponent();
			AccessibleName = GetType().Name;
		}

		public ParserParametersDlg(IHelpTopicProvider helpTopicProvider) : this()
		{
			m_helpTopicProvider = helpTopicProvider;
		}

		/// <summary>
		///Get or set the parser parameters XML text
		///</summary>
		public string XmlRep
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
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (m_dsParserParameters != null)
				{
					m_dsParserParameters.Dispose();
					m_dsParserParameters = null;
				}
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ParserParametersDlg));
			this.m_label1 = new System.Windows.Forms.Label();
			this.m_label2 = new System.Windows.Forms.Label();
			this.m_btnOk = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_dataGrid1 = new System.Windows.Forms.DataGrid();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.m_dataGrid2 = new System.Windows.Forms.DataGrid();
			this.m_label3 = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.m_dataGrid1)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_dataGrid2)).BeginInit();
			this.SuspendLayout();
			//
			// label1
			//
			resources.ApplyResources(this.m_label1, "m_label1");
			this.m_label1.Name = "m_label1";
			//
			// label2
			//
			resources.ApplyResources(this.m_label2, "m_label2");
			this.m_label2.Name = "m_label2";
			//
			// btnOK
			//
			this.m_btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			resources.ApplyResources(this.m_btnOk, "m_btnOk");
			this.m_btnOk.Name = "m_btnOk";
			this.m_btnOk.Click += new System.EventHandler(this.btnOK_Click);
			//
			// btnCancel
			//
			this.m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			this.m_btnCancel.Name = "m_btnCancel";
			//
			// dataGrid1
			//
			resources.ApplyResources(this.m_dataGrid1, "m_dataGrid1");
			this.m_dataGrid1.DataMember = global::SIL.FieldWorks.LexText.Controls.ParserUIStrings.ksIdle_;
			this.m_dataGrid1.HeaderForeColor = System.Drawing.SystemColors.ControlText;
			this.m_dataGrid1.Name = "m_dataGrid1";
			//
			// btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.UseVisualStyleBackColor = true;
			this.m_btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			//
			// dataGrid2
			//
			resources.ApplyResources(this.m_dataGrid2, "m_dataGrid2");
			this.m_dataGrid2.DataMember = global::SIL.FieldWorks.LexText.Controls.ParserUIStrings.ksIdle_;
			this.m_dataGrid2.HeaderForeColor = System.Drawing.SystemColors.ControlText;
			this.m_dataGrid2.Name = "m_dataGrid2";
			//
			// label3
			//
			resources.ApplyResources(this.m_label3, "m_label3");
			this.m_label3.Name = "m_label3";
			//
			// ParserParametersDlg
			//
			this.AcceptButton = this.m_btnOk;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.m_btnCancel;
			this.Controls.Add(this.m_label3);
			this.Controls.Add(this.m_dataGrid2);
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_dataGrid1);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnOk);
			this.Controls.Add(this.m_label2);
			this.Controls.Add(this.m_label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "ParserParametersDlg";
			((System.ComponentModel.ISupportInitialize)(this.m_dataGrid1)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_dataGrid2)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		private void btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, HelpTopic);
		}

		private void btnOK_Click(object sender, EventArgs e)
		{
			XElement newParserParamsElem = XElement.Parse(m_dsParserParameters.GetXml());
			XElement oldParserParamsElem = XElement.Parse(XmlRep);
			newParserParamsElem.Add(oldParserParamsElem.Element("ActiveParser"));
			XmlRep = newParserParamsElem.ToString();
			ValidateValues(newParserParamsElem);
		}

		private void ValidateValues(XElement elem)
		{
			EnforceValidValue(elem, XAmple, MaxNulls, 0, 10, false);
			EnforceValidValue(elem, XAmple, MaxPrefixes, 0, 25, false);
			EnforceValidValue(elem, XAmple, MaxSuffixes, 0, 25, false);
			EnforceValidValue(elem, XAmple, MaxInfixes, 0, 7, false);
			EnforceValidValue(elem, XAmple, MaxInterfixes, 0, 7, false);
			EnforceValidValue(elem, XAmple, MaxRoots, 0, 10, false);
			EnforceValidValue(elem, XAmple, MaxAnalysesToReturn, -1, 10000, true);

			EnforceValidValue(elem, HC, DelReapps, 0, 10, false);
		}

		private void EnforceValidValue(XElement elem, string parser, string item, int min, int max, bool useMinIfZero)
		{
			XElement valueElem = elem.Elements(parser).Elements(item).FirstOrDefault();
			if (valueElem != null)
			{
				var val = (int) valueElem;
				if (val < min || (useMinIfZero && val == 0))
				{
					valueElem.SetValue(min);
					XmlRep = elem.ToString();
					ReportChangeOfValue(item, val, min, min, max);
				}
				else if (val > max)
				{
					valueElem.SetValue(max);
					XmlRep = elem.ToString();
					ReportChangeOfValue(item, val, max, min, max);
				}
			}
		}

		private void ReportChangeOfValue(string item, int value, int newValue, int min, int max)
		{
			string sMessage = String.Format(ParserUIStrings.ksChangedValueReport, item, value, newValue, min, max);
			MessageBox.Show(sMessage, ParserUIStrings.ksChangeValueDialogTitle, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
		}

		/// <summary>
		/// Set up the dlg in preparation to showing it.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="m_dsParserParameters gets disposed in Dispose()")]
		public void SetDlgInfo(string title, string parserParameters)
		{
			CheckDisposed();

			XmlRep = parserParameters;
			Text = title;

			m_dsParserParameters = new DataSet { DataSetName = "ParserParameters" };

			DataTable tblXAmple = CreateXAmpleDataTable();
			m_dsParserParameters.Tables.Add(tblXAmple);
			DataTable tblHC = CreateHCDataTable();
			m_dsParserParameters.Tables.Add(tblHC);

			LoadParserData(m_dsParserParameters);

			PopulateDataGrid(m_dataGrid1, XAmple);
			PopulateDataGrid(m_dataGrid2, HC);
			m_dataGrid2.TableStyles[0].GridColumnStyles[2].Width = 130;
		}

		private void LoadParserData(DataSet dsParserParameters)
		{
			var parserParamsElem = XElement.Parse(XmlRep);
			// set default values for HC
			XElement hcElem = parserParamsElem.Element(HC);
			if (hcElem == null)
			{
				hcElem = new XElement(HC);
				parserParamsElem.Add(hcElem);
			}
			if (hcElem.Element(DelReapps) == null)
				hcElem.Add(new XElement(DelReapps, 0));
			if (hcElem.Element(NoDefaultCompounding) == null)
				hcElem.Add(new XElement(NoDefaultCompounding, false));
			if (hcElem.Element(NotOnClitics) == null)
				hcElem.Add(new XElement(NotOnClitics, true));

			using (XmlReader reader = parserParamsElem.CreateReader())
				dsParserParameters.ReadXml(reader, XmlReadMode.IgnoreSchema);
		}

		private void PopulateDataGrid(DataGrid dataGrid, string parser)
		{
			dataGrid.SetDataBinding(m_dsParserParameters, parser);

			DataView view = CreateDataView(m_dsParserParameters.Tables[parser]);
			dataGrid.DataSource = view;
			dataGrid.TableStyles.Add(new DataGridTableStyle { MappingName = parser, RowHeadersVisible = false, AllowSorting = false });
			foreach (DataGridBoolColumn col in dataGrid.TableStyles[0].GridColumnStyles.OfType<DataGridBoolColumn>())
				col.AllowNull = false;
		}

		private DataView CreateDataView(DataTable table)
		{
			return new DataView(table) { AllowNew = false };
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "DataColumns get added to collection and disposed there")]
		private DataTable CreateXAmpleDataTable()
		{
			var tblXAmple = new DataTable(XAmple);
			tblXAmple.Columns.Add(MaxNulls, typeof(int));
			tblXAmple.Columns.Add(MaxPrefixes, typeof(int));
			tblXAmple.Columns.Add(MaxInfixes, typeof(int));
			tblXAmple.Columns.Add(MaxRoots, typeof(int));
			tblXAmple.Columns.Add(MaxSuffixes, typeof(int));
			tblXAmple.Columns.Add(MaxInterfixes, typeof(int));
			tblXAmple.Columns.Add(MaxAnalysesToReturn, typeof(int));
			return tblXAmple;
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "DataColumns get added to collection and disposed there")]
		private DataTable CreateHCDataTable()
		{
			var tblHC = new DataTable(HC);
			tblHC.Columns.Add(DelReapps, typeof(int));
			tblHC.Columns.Add(NotOnClitics, typeof(bool));
			tblHC.Columns.Add(NoDefaultCompounding, typeof(bool));
			return tblHC;
		}
	}
}

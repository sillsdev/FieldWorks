// Copyright (c) 2025 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using SIL.LCModel;
using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using DataGrid = System.Windows.Forms.DataGrid;
using TextBox = System.Windows.Forms.TextBox;

namespace SIL.FieldWorks.LexText.Controls
{
	public partial class HCMaxCompoundRulesDlg : ParserParametersBase
	{
		private const string m_Name = "Name";
		private const string m_Description = "Description";
		private const string m_MaxApps = "MaxApps";
		private const string m_Guid = "Guid";
		private const string m_CompoundRules = "CompoundRules";
		private XElement m_ParserParametersElem;

		private DataGrid m_dataGrid;
		private DataSet m_dsCompoundRules;

		public HCMaxCompoundRulesDlg()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Set up the dlg in preparation to showing it.
		/// </summary>
		public void SetDlgInfo(string title, string parserParameters, ILcmOwningSequence<IMoCompoundRule> compoundRules)
		{
			CheckDisposed();

			XmlRep = parserParameters;
			m_ParserParametersElem = XElement.Parse(parserParameters);
			Text = title;

			m_dsCompoundRules = new DataSet { DataSetName = m_CompoundRules };

			DataTable tblCompoundRules = new DataTable();
			tblCompoundRules.TableName = m_CompoundRules;
			tblCompoundRules.Columns.Add(m_Name, typeof(string));
			tblCompoundRules.Columns.Add(m_Description, typeof(string));
			tblCompoundRules.Columns.Add(m_MaxApps, typeof(int));
			tblCompoundRules.Columns.Add(m_Guid, typeof(string));
			tblCompoundRules.Columns[0].ReadOnly = true;
			tblCompoundRules.Columns[1].ReadOnly = true;
			tblCompoundRules.Columns[3].ReadOnly = true;
			m_dsCompoundRules.Tables.Add(tblCompoundRules);

			XElement cRules = new XElement(m_CompoundRules);
			foreach (IMoCompoundRule rule in compoundRules)
			{
				var name = new XElement(m_Name, rule.Name.BestAnalysisAlternative.Text);
				var description = new XElement(m_Description, rule.Description.BestAnalysisAlternative.Text);
				var sGuid = rule.Guid.ToString();
				string sMaxApps = getMaxAppsFromParameters(sGuid);
				var maxApps = new XElement(m_MaxApps, sMaxApps);
				var guid = new XElement(m_Guid, sGuid);
				cRules.Add(name, description, maxApps, guid);
				using (XmlReader reader = cRules.CreateReader())
					m_dsCompoundRules.ReadXml(reader, XmlReadMode.IgnoreSchema);
				cRules.RemoveAll();
			}

			m_dataGrid.SetDataBinding(m_dsCompoundRules, m_CompoundRules);
			DataView view = CreateDataView(m_dsCompoundRules.Tables[m_CompoundRules]);
			m_dataGrid.DataSource = view;
			m_dataGrid.TableStyles.Add(new DataGridTableStyle { MappingName = m_CompoundRules, RowHeadersVisible = false, AllowSorting = false });
			foreach (DataGridTextBoxColumn col in m_dataGrid.TableStyles[0].GridColumnStyles.OfType<DataGridTextBoxColumn>())
			{
				TextBox textBox1 = col.TextBox;
				textBox1.Multiline = true;
				textBox1.ScrollBars = ScrollBars.Vertical;
				textBox1.WordWrap = true;
				m_dataGrid.TableStyles[0].PreferredRowHeight = 25;
			}

			m_dataGrid.TableStyles[0].GridColumnStyles[0].Width = 200;
			m_dataGrid.TableStyles[0].GridColumnStyles[1].Width = 250;
			m_dataGrid.TableStyles[0].GridColumnStyles[3].Width = 0;
		}

		private string getMaxAppsFromParameters(string sGuid)
		{
			var sMaxApps = "1";
			var ruleElem = m_ParserParametersElem.XPathSelectElement("//CompoundRule[@guid='" + sGuid + "']");
			if (ruleElem != null)
			{
				var aMaxApps = ruleElem.Attribute("maxApps");
				if (aMaxApps != null)
				{
					sMaxApps = aMaxApps.Value;
				}
			}
			return sMaxApps;
		}

		private DataView CreateDataView(DataTable table)
		{
			return new DataView(table) { AllowNew = false };
		}

		private void btnOK_Click(object sender, EventArgs e)
		{
			XElement compoundRulesTopElem = XElement.Parse(m_dsCompoundRules.GetXml());
			ValidateValues(compoundRulesTopElem);
			XElement oldParserParamsElem = XElement.Parse(XmlRep);
			oldParserParamsElem.Element(m_CompoundRules)?.Remove();
			// Rework compound rules to show just GUID and max apps
			var cRulesElem = CreateCompoundRulesElementToStore(compoundRulesTopElem);
			oldParserParamsElem.Add(cRulesElem);
			XmlRep = oldParserParamsElem.ToString();
		}

		private void ValidateValues(XElement elem)
		{
			var rulesElem = elem.XPathSelectElements(m_CompoundRules);
			foreach (var rule in rulesElem)
			{
				EnforceValidValue(rule, m_MaxApps, 1, 9, true);
			}
		}

		private XElement CreateCompoundRulesElementToStore(XElement elem)
		{
			XElement cRulesElem = new XElement(m_CompoundRules);
			var rulesElem = elem.XPathSelectElements(m_CompoundRules);
			foreach (var rule in rulesElem)
			{
				var sMaxApps = rule.Element(m_MaxApps).Value;
				if (sMaxApps != "1")
				{
					var sGuid = rule.Element(m_Guid).Value;
					XElement cRule = new XElement("CompoundRule");
					XAttribute guid = new XAttribute("guid", sGuid);
					XAttribute maxApps = new XAttribute("maxApps", sMaxApps);
					cRule.Add(guid, maxApps);
					cRulesElem.Add(cRule);
				}
			}
			return cRulesElem;
		}

		private void btnHelp_Click(object sender, EventArgs e)
		{
			MessageBox.Show("Help clicked");
		}

		protected void EnforceValidValue(XElement elem, string item, int min, int max, bool useMinIfZero)
		{
			XElement valueElem = elem.Elements(item).FirstOrDefault();
			if (valueElem != null)
			{
				var val = (int)valueElem;
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

	}
}

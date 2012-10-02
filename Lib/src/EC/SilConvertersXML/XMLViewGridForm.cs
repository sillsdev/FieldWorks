using System;
using System.Collections;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;
using ECInterfaces;
using SilEncConverters31;
using System.IO;
using System.Runtime.Serialization;                 // for SerializationException
using System.Runtime.Serialization.Formatters.Soap; // for soap formatter

namespace SilConvertersXML
{
	// this file contains the code for the grid pane
	public partial class XMLViewForm : Form
	{
		protected Hashtable m_mapEncConverters = new Hashtable();
		protected DirectableEncConverter m_aEcDefault = null;
		protected DirectableEncConverter m_aECLast = null;
		protected const string cstrClickMsg = "Select a converter";
		protected const string cstrDots = "...";
		protected const string cstrMappingExtn = "xcm";
		protected const string cstrFileOpenFilter = "XML Converter mapping files (*.xcm)|*.xcm|All files (*.*)|*.*";
		protected const string cstrFixedStringConverterPrefix = "Fixed value: ";

		protected const int cnXmlPathsColumn = 0;
		protected const int cnExampleDataColumn = 1;
		protected const int cnEncConverterColumn = 2;
		protected const int cnExampleOutputColumn = 3;

		protected int cnMaxConverterName = 30;

		internal void AddRow(string strXmlPath, XPathNodeIterator xpIterator)
		{
			int nIndex = dataGridViewConverterMapping.Rows.Add();
			DataGridViewRow theRow = dataGridViewConverterMapping.Rows[nIndex];
			theRow.Cells[cnXmlPathsColumn].Value = strXmlPath;

			if (DefaultConverterDefined)
				DefineConverter(strXmlPath, m_aEcDefault);

			UpdateConverterCellValue(theRow.Cells[cnEncConverterColumn], (DefaultConverterDefined) ? m_aEcDefault : null);
			theRow.Tag = xpIterator;
			UpdateSampleValue(xpIterator, theRow);
		}

		protected void UpdateSampleValue(XPathNodeIterator xpIterator, DataGridViewRow theRow)
		{
			if (xpIterator.MoveNext())
			{
				XPathNavigator nav = xpIterator.Current;
				UpdateExampleDataColumns(theRow, nav.Value);
			}
		}

		protected void UpdateExampleDataColumns(DataGridViewRow theRow, string strSampleValue)
		{
			theRow.Cells[cnExampleDataColumn].Value = strSampleValue;
			string strXmlPath = (string)theRow.Cells[cnXmlPathsColumn].Value;
			if (!String.IsNullOrEmpty(strSampleValue) && IsConverterDefined(strXmlPath))
			{
				DirectableEncConverter aEC = (DirectableEncConverter)m_mapEncConverters[strXmlPath];
				strSampleValue = CallSafeConvert(aEC, strSampleValue);
			}
			theRow.Cells[cnExampleOutputColumn].Value = strSampleValue;
		}

		public bool DefaultConverterDefined
		{
			get { return (m_aEcDefault != null); }
		}

		public bool IsConverterDefined(string strXmlPath)
		{
			return m_mapEncConverters.ContainsKey(strXmlPath);
		}

		public void DefineConverter(string strXmlPath, DirectableEncConverter aEC)
		{
			if (m_mapEncConverters.ContainsKey(strXmlPath))
				throw new ApplicationException("You already have a converter defined for this XPath expression! (try again after you've processed this one)");
			m_mapEncConverters.Add(strXmlPath, aEC);
		}

		protected void UpdateConverterCellValue(DataGridViewCell theCell, DirectableEncConverter aEC)
		{
			if (aEC == null)
			{
				theCell.Value = (m_mapEncConverters.Count > 0) ? cstrDots : cstrClickMsg;
				theCell.ToolTipText = null;
			}
			else
			{
				string strName = aEC.Name;
				if (strName.Length > cnMaxConverterName)
					strName = strName.Substring(0, cnMaxConverterName);
				theCell.Value = strName;
				theCell.ToolTipText = aEC.ToString();
			}
		}

		private void dataGridViewConverterMapping_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
		{
			int nColumnIndex = e.ColumnIndex;
			// if the user clicks on the header... that doesn't work
			if (((e.RowIndex < 0) || (e.RowIndex > dataGridViewConverterMapping.Rows.Count))
				|| ((nColumnIndex < cnXmlPathsColumn) || (nColumnIndex > cnEncConverterColumn)))
				return;

			DataGridViewRow theRow = this.dataGridViewConverterMapping.Rows[e.RowIndex];
			string strXmlPath = (string)theRow.Cells[cnXmlPathsColumn].Value;
			DataGridViewCell theCell = theRow.Cells[e.ColumnIndex];
			DirectableEncConverter aEC = null;
			XPathNodeIterator xpIterator = null;
			switch (nColumnIndex)
			{
				case cnXmlPathsColumn:
					if (e.Button == MouseButtons.Right)
					{
						XPathFilterForm dlg = new XPathFilterForm(strXmlPath);
						if ((dlg.ShowDialog() == DialogResult.OK) && (strXmlPath != dlg.FilterExpression))
						{
							strXmlPath = dlg.FilterExpression;
							UpdateConverterCellValue(theRow.Cells[cnEncConverterColumn],
								(IsConverterDefined(strXmlPath)) ? (DirectableEncConverter)m_mapEncConverters[strXmlPath]
									: (DefaultConverterDefined) ? m_aEcDefault : null);

							theCell.Value = strXmlPath;
							Program.AddRecentXPathExpression(strXmlPath);
							try
							{
								xpIterator = GetIterator(ref strXmlPath, false);
								theRow.Tag = xpIterator;
								UpdateSampleValue(xpIterator, theRow);
							}
							catch (ApplicationException ex)
							{
								// we throw this to cancel
								if (ex.Message != cstrCaption)
									MessageBox.Show(ex.Message, cstrCaption);
							}
							catch (Exception ex)
							{
								MessageBox.Show(ex.Message, cstrCaption);
							}
						}
					}
					break;

				case cnExampleDataColumn:
					xpIterator = (XPathNodeIterator)theRow.Tag;
					UpdateSampleValue(xpIterator, theRow);
					break;

				case cnExampleOutputColumn:
					break;

				case cnEncConverterColumn:
					string strExampleData = (string)theRow.Cells[cnExampleDataColumn].Value;

					if (e.Button == MouseButtons.Right)
					{
						aEC = m_aECLast;
						if ((m_aECLast == null) && IsConverterDefined(strXmlPath))
							m_mapEncConverters.Remove(strXmlPath);
					}
					else
					{
						EncConverters aECs = GetEncConverters;
						if (aECs != null)
						{
							IEncConverter aIEC = aECs.AutoSelectWithData(strExampleData, null, ConvType.Unknown, "Choose Converter");
							if (aIEC != null)
								aEC = new DirectableEncConverter(aIEC);
							else
							{
								CheckForFixedValue(theRow);
								return;
							}
						}
					}

					if (aEC != null)
					{
						if (IsConverterDefined(strXmlPath))
							m_mapEncConverters.Remove(strXmlPath);
						DefineConverter(strXmlPath, aEC);
					}

					UpdateExampleDataColumns(theRow, strExampleData);
					UpdateConverterCellValue(theCell, aEC);
					m_aECLast = aEC;
					break;
			}
		}

		internal static EncConverters GetEncConverters
		{
			get
			{
				try
				{
					return DirectableEncConverter.EncConverters;
				}
				catch (Exception ex)
				{
					MessageBox.Show("Can't access the repository because: " + ex.Message, cstrCaption);
				}
				return null;
			}
		}

		protected void CheckForFixedValue(DataGridViewRow theRow)
		{
			DialogResult res = MessageBox.Show("Do you want to specify a fixed value to be applied to all matching records (i.e. rather than using a converter from the system repository)?", cstrCaption, MessageBoxButtons.YesNoCancel);
			if (res == DialogResult.Yes)
			{
				string strSampleValue = (string)theRow.Cells[cnExampleDataColumn].Value;
				QueryFixedValueForm dlg = new QueryFixedValueForm(strSampleValue);
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					string strXmlPath = (string)theRow.Cells[cnXmlPathsColumn].Value;
					DirectableEncConverter aEC = GetTempFixedValueConverter(dlg.FixedValue);
					if (aEC != null)
					{
						if (IsConverterDefined(strXmlPath))
							m_mapEncConverters.Remove(strXmlPath);
						DefineConverter(strXmlPath, aEC);
						UpdateConverterCellValue(theRow.Cells[cnEncConverterColumn], aEC);
						UpdateExampleDataColumns(theRow, strSampleValue);
						m_aECLast = aEC;
					}
				}
			}
		}

		protected DirectableEncConverter GetTempFixedValueConverter(string strFixedValue)
		{
			string strName = String.Format("{0}{1}", cstrFixedStringConverterPrefix, strFixedValue);
			EncConverters aECs = GetEncConverters;
			if (aECs != null)
			{
				IEncConverter aIEC = null;
				if (!aECs.ContainsKey(strName))
				{
					aIEC = aECs.NewEncConverterByImplementationType(EncConverters.strTypeSILicuRegex);
					ConvType eConvType = ConvType.Unicode_to_Unicode;
					string strDummy = null;
					int nProcType = (int)ProcessTypeFlags.ICURegularExpression;
					aIEC.Initialize(strName, String.Format(".+->{0}", strFixedValue), ref strDummy, ref strDummy,
						ref eConvType, ref nProcType, 0, 0, true);
					aECs.Add(strName, aIEC);
				}
				else
					aIEC = aECs[strName];

				return new DirectableEncConverter(aIEC);
			}
			return null;
		}

		private void setDefaultConverterToolStripMenuItem_Click(object sender, EventArgs e)
		{
			m_aEcDefault = null;
			EncConverters aECs = GetEncConverters;
			if (aECs != null)
			{
				IEncConverter aIEC = aECs.AutoSelectWithTitle(ConvType.Unknown, "Choose Default Converter");
				if (aIEC != null)
				{
					m_aEcDefault = new DirectableEncConverter(aIEC);

					foreach (DataGridViewRow aRow in dataGridViewConverterMapping.Rows)
					{
						string strXmlPath = (string)aRow.Cells[cnXmlPathsColumn].Value;
						if (!IsConverterDefined(strXmlPath))
						{
							DataGridViewCell cellConverter = aRow.Cells[cnEncConverterColumn];
							cellConverter.Value = m_aEcDefault.Name;
							cellConverter.ToolTipText = m_aEcDefault.ToString();
							string strInput = (string)aRow.Cells[cnExampleDataColumn].Value;
							aRow.Cells[cnExampleOutputColumn].Value = CallSafeConvert(m_aEcDefault, strInput);
							DefineConverter(strXmlPath, m_aEcDefault);
						}
					}

					m_aECLast = null;
				}
			}
		}

		protected string CallSafeConvert(DirectableEncConverter aEC, string strInput)
		{
			try
			{
				return aEC.Convert(strInput);
			}
			catch (Exception ex)
			{
				MessageBox.Show(String.Format("Conversion failed! Reason: {0}", ex.Message), cstrCaption);
			}

			return null;
		}

		private void newToolStripMenuItem_Click(object sender, EventArgs e)
		{
			m_mapEncConverters.Clear();
			UpdateConverterNames();
		}

		private void UpdateConverterNames()
		{
			foreach (DataGridViewRow aRow in dataGridViewConverterMapping.Rows)
			{
				string strXmlPath = (string)aRow.Cells[cnXmlPathsColumn].Value;
				string strConverterName, strTooltip;
				InitConverterDetails(strXmlPath, out strConverterName, out strTooltip);
				DataGridViewCell theEcNameCell = aRow.Cells[cnEncConverterColumn];
				theEcNameCell.Value = strConverterName;
				theEcNameCell.ToolTipText = strTooltip;
				UpdateExampleDataColumns(aRow, (string)aRow.Cells[cnExampleDataColumn].Value);
			}
		}

		protected void InitConverterDetails(string strXmlPath, out string strConverterName, out string strTooltip)
		{
			strConverterName = (m_mapEncConverters.Count > 0) ? cstrDots : cstrClickMsg;
			strTooltip = null;

			if (IsConverterDefined(strXmlPath))
			{
				DirectableEncConverter aEC = (DirectableEncConverter)m_mapEncConverters[strXmlPath];
				strConverterName = aEC.Name;
				strTooltip = aEC.ToString();
			}
		}

		private void loadToolStripMenuItem_Click(object sender, EventArgs e)
		{
			OpenFileDialog dlgSettings = new OpenFileDialog();
			dlgSettings.DefaultExt = cstrMappingExtn;
			dlgSettings.InitialDirectory = Application.UserAppDataPath;
			dlgSettings.Filter = cstrFileOpenFilter;
			dlgSettings.RestoreDirectory = true;

			if (dlgSettings.ShowDialog() == DialogResult.OK)
			{
				LoadConverterMappingFile(dlgSettings.FileName);
			}
		}

		public void LoadConverterMappingFile(string strFileSpec)
		{
			FileStream fs = new FileStream(strFileSpec, FileMode.Open);

			// Construct a SoapFormatter and use it
			// to serialize the data to the stream.
			try
			{
				SoapFormatter formatter = new SoapFormatter();
				Hashtable map = (Hashtable)formatter.Deserialize(fs);
				foreach (string strXPath in map.Keys)
				{
					DirectableEncConverter aFileEc = (DirectableEncConverter)map[strXPath];

					// see if this one is even valid anymore
					if (aFileEc.GetEncConverter == null)
					{
						// there's one case we probably ought to handle: the user sets a temporary converter of the
						//  fixed value flavor. (since we created it, we should recreated it).
						if (aFileEc.Name.IndexOf(cstrFixedStringConverterPrefix) != -1)
						{
							aFileEc = GetTempFixedValueConverter(aFileEc.Name.Substring(cstrFixedStringConverterPrefix.Length));
						}
						else
						{
							MessageBox.Show(String.Format("The converter '{0}' is no longer available on this system... skipping", aFileEc.Name), XMLViewForm.cstrCaption);
							continue;
						}
					}

					if (IsConverterDefined(strXPath))
					{
						DirectableEncConverter aExistingEc = (DirectableEncConverter)m_mapEncConverters[strXPath];
						if (aExistingEc.Name != aFileEc.Name)
						{
							DialogResult res = MessageBox.Show(String.Format("The XPath expression {0}{0}{1}{0}{0} is already defined to use the '{2}' converter. Would you like to replace this mapping with the one from the file (i.e. '{3}')?",
								Environment.NewLine, strXPath, aExistingEc.Name, aFileEc.Name), XMLViewForm.cstrCaption, MessageBoxButtons.YesNo);
							if (res == DialogResult.Yes)
							{
								m_mapEncConverters.Remove(strXPath);
								DefineConverter(strXPath, aFileEc);
							}
						}
					}
					else
					{
						DefineConverter(strXPath, (DirectableEncConverter)map[strXPath]);
					}
				}

				ArrayList arLstXPaths = (ArrayList)formatter.Deserialize(fs);

				foreach (string strXPath in arLstXPaths)
				{
					string strXmlPath = strXPath;
					XPathNodeIterator xpIterator = GetIterator(ref strXmlPath, false);
					AddRow(strXPath, xpIterator);
				}

				UpdateConverterNames();
			}
			catch (SerializationException ex)
			{
				MessageBox.Show("Failed to open mapping file. Reason: " + ex.Message);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, cstrCaption);
			}
			finally
			{
				fs.Close();
			}
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			SaveFileDialog dlgSettings = new SaveFileDialog();
			dlgSettings.DefaultExt = cstrMappingExtn;
			dlgSettings.FileName = "XML Converter mapping1.xcm";
			dlgSettings.InitialDirectory = Application.UserAppDataPath;
			dlgSettings.Filter = cstrFileOpenFilter;
			dlgSettings.RestoreDirectory = true;
			if (dlgSettings.ShowDialog() == DialogResult.OK)
			{
				// Construct a SoapFormatter and use it
				// to serialize the data to the stream.
				FileStream fs = new FileStream(dlgSettings.FileName, FileMode.Create);
				SoapFormatter formatter = new SoapFormatter();
				try
				{
					// when we save, save the xPath and the converter name... in order
					ArrayList arLstXPaths = new ArrayList(dataGridViewConverterMapping.Rows.Count);
					foreach (DataGridViewRow aRow in dataGridViewConverterMapping.Rows)
						arLstXPaths.Add((string)aRow.Cells[cnXmlPathsColumn].Value);

					formatter.Serialize(fs, m_mapEncConverters);
					formatter.Serialize(fs, arLstXPaths);
				}
				catch (SerializationException ex)
				{
					MessageBox.Show("Failed to save! Reason: " + ex.Message);
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message, cstrCaption);
				}
				finally
				{
					fs.Close();
				}
			}
		}
	}
}

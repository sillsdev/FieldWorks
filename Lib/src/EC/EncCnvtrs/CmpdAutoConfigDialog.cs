using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ECInterfaces;                     // for IEncConverter

namespace SilEncConverters31
{
	public partial class CmpdAutoConfigDialog : SilEncConverters31.MetaCmpdAutoConfigDialog
	{
		protected const int cnStepNameColumn = 0;
		protected const int cnDirectionReverseColumn = 1;
		protected const int cnNormalizationColumn = 2;
		protected const int cnMaxConverterName = 30;

		protected const string cstrClickMeMessage = "Click to Add a Step";
		protected const string cstrNormalizationFullyComposed = "Fully Composed";
		protected const string cstrNormalizationFullyDecomposed = "Fully Decomposed";

		protected bool m_bAllStepsAreBidirectional = true;

		public CmpdAutoConfigDialog
			(
			IEncConverters aECs,
			string strDisplayName,
			string strFriendlyName,
			string strConverterIdentifier,
			ConvType eConversionType,
			string strLhsEncodingId,
			string strRhsEncodingId,
			int lProcessTypeFlags,
			bool bIsInRepository
			)
		{
			System.Diagnostics.Debug.Assert(aECs != null);
			if (aECs.Count == 0)
			{
				MessageBox.Show("No existing converters in the repository! You must add some before attempting to chain them together.");
				return;
			}

			InitializeComponent();

			base.Initialize
			(
			aECs,
			CmpdEncConverter.strHtmlFilename,
			strDisplayName,
			strFriendlyName,
			strConverterIdentifier,
			eConversionType,
			strLhsEncodingId,
			strRhsEncodingId,
			lProcessTypeFlags,
			bIsInRepository
			);

			// the NewRow should have (only) the "Click me" label
			dataGridViewSteps.Rows[0].Cells[cnStepNameColumn].Value = cstrClickMeMessage;

			// if we're editing ...
			if (m_bEditMode)
			{
				System.Diagnostics.Debug.Assert(!String.IsNullOrEmpty(ConverterIdentifier));

				labelCompoundConverterName.Text = FriendlyName; // FriendlyName was initialized during base.Initialize since we're editing

				System.Diagnostics.Debug.Assert(m_aEC != null); // m_aEC was initialized during base.Initialize since we have the friendly name ...
				if (m_aEC != null)  // ... but make sure anyway
				{
					QueryStepData();    // get the data about the steps

					for (int i = 0; i < m_astrStepFriendlyNames.Length; i++)
					{
						IEncConverter aEC = m_aECs[m_astrStepFriendlyNames[i]];
						if (aEC == null)
						{
							// this really can't happen, because if the converter isn't there when we subsequently open it,
							//  the call to QueryStepData (which calls m_aEC.ConverterNameEnum) will fail to return the
							//  now deleted step. But just in case...
							MessageBox.Show(String.Format("The converter '{0}' no longer exists. Removing from the compound converter", m_astrStepFriendlyNames[i]), EncConverters.cstrCaption);
							dataGridViewSteps.Rows.Insert(i, new object[] { cstrClickMeMessage, false, "None" });
						}
						else
						{
							// insert a row in the grid for this step
							dataGridViewSteps.Rows.Insert(i, 1);
							DataGridViewRow theRow = dataGridViewSteps.Rows[i];
							UpdateConverterCellValue(theRow, aEC);
							UpdateDirectionReverseCellValue(theRow, !m_abDirectionForwards[i], aEC);
							UpdateNormalizationCellValue(theRow, m_aeNormalizeFlags[i]);
							theRow.Tag = aEC;
						}
					}
				}

				IsModified = false;
				m_bAllStepsAreBidirectional = !EncConverters.IsUnidirectional(m_aEC.ConversionType);
			}
		}

		public CmpdAutoConfigDialog
			(
			IEncConverters aECs,
			string strFriendlyName,
			string strConverterIdentifier,
			ConvType eConversionType,
			string strTestData
			)
		{
			InitializeComponent();

			base.Initialize
			(
			aECs,
			strFriendlyName,
			strConverterIdentifier,
			eConversionType,
			strTestData
			);
		}

		// this method is called either when the user clicks the "Apply" or "OK" buttons *OR* if she
		//  tries to switch to the Test or Advanced tab. This is the dialog's one opportunity
		//  to make sure that the user has correctly configured a legitimate converter.
		protected override bool OnApply()
		{
			// for compound converter, there must be at least one step
			int nRowCount = dataGridViewSteps.Rows.Count - 1;
			if (nRowCount > 0)
			{
				m_astrStepFriendlyNames = new string[nRowCount];
				m_abDirectionForwards = new bool[nRowCount];
				m_aeNormalizeFlags = new NormalizeFlags[nRowCount];
				for (int i = 0; i < nRowCount; i++)
				{
					DataGridViewRow theRow = dataGridViewSteps.Rows[i];
					IEncConverter aEC = (IEncConverter)theRow.Tag;
					// check to see if the user actually configured a converter at this row (might have just checked the reverse box)
					if (aEC == null)
					{
						MessageBox.Show(this, String.Format(@"No Converter selected for step {0}. Click where it says '{1}' to choose a converter for this step", i + 1, cstrClickMeMessage), EncConverters.cstrCaption);
						return false;
					}

					m_astrStepFriendlyNames[i] = aEC.Name;  // don't use the cell value as that may have been truncated
					m_abDirectionForwards[i] = !(bool)theRow.Cells[cnDirectionReverseColumn].Value;
					string strNormalizeValue = (string)theRow.Cells[cnNormalizationColumn].Value;
					NormalizeFlags eNormalizeFlag = NormalizeFlags.None;
					if (strNormalizeValue == cstrNormalizationFullyComposed)
						eNormalizeFlag = NormalizeFlags.FullyComposed;
					else if (strNormalizeValue == cstrNormalizationFullyDecomposed)
						eNormalizeFlag = NormalizeFlags.FullyDecomposed;
					m_aeNormalizeFlags[i] = eNormalizeFlag;

					// grab the beginning and ending encoding ids as well (in case we're creating a default friendly name)
					if (i == 0) // first step
						LhsEncodingId = (m_abDirectionForwards[i]) ? aEC.LeftEncodingID : aEC.RightEncodingID;

					if (i == (nRowCount - 1))   // last step
						RhsEncodingId = (m_abDirectionForwards[i]) ? aEC.RightEncodingID : aEC.LeftEncodingID;

					// also adjust the flag saying whether all steps are bidirectional
					m_bAllStepsAreBidirectional &= !EncConverters.IsUnidirectional(aEC.ConversionType);
				}
			}
			else
				return false;

			if (tabControl.SelectedTab == tabPageSetup)
			{
				// only do these message boxes if we're on the Setup tab itself, because if this OnApply
				//  is being called as a result of the user switching to the Test tab, that code will
				//  already put up an error message and we don't need two error messages.
				if (dataGridViewSteps.Rows.Count < 1)
				{
					MessageBox.Show(this, "You must add at least one step!", EncConverters.cstrCaption);
					return false;
				}
				else
				{
					for (int i = 0; i < nRowCount; i++)
					{
						if (cstrClickMeMessage == m_astrStepFriendlyNames[i])
						{
							MessageBox.Show(this, String.Format("The step in row '{0}' has not been configured. Delete that row first or choose a converter for it", i + 1), EncConverters.cstrCaption);
							return false;
						}
					}
				}
			}

			try
			{
				return base.OnApply();
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, String.Format("Failed to add compound converter! {0}{0}{1}", Environment.NewLine, ex.Message), EncConverters.cstrCaption);
			}

			return false;
		}

		protected override string ProgID
		{
			get { return typeof(CmpdEncConverter).FullName; }
		}

		protected override string ImplType
		{
			get { return EncConverters.strTypeSILcomp; }
		}

		protected override string DefaultFriendlyName
		{
			get
			{
				// first shot goes when we have both encoding IDs (because lhsEncID<>rhsEncID is the best default)
				string strFriendlyName = null;
				if ((m_astrStepFriendlyNames != null) && (m_astrStepFriendlyNames.Length > 0))
				{
					strFriendlyName = String.Format("{0} {1} {2} (compound)",
						String.IsNullOrEmpty(LhsEncodingId) ? m_astrStepFriendlyNames[0] : LhsEncodingId,
						(m_bAllStepsAreBidirectional) ? "<>" : ">",
						String.IsNullOrEmpty(RhsEncodingId) ? m_astrStepFriendlyNames[m_astrStepFriendlyNames.Length - 1] : RhsEncodingId);
				}
				else if (!String.IsNullOrEmpty(m_strOriginalFriendlyName))
					strFriendlyName = m_strOriginalFriendlyName;

				return strFriendlyName;
			}
		}

		protected override void AddConverterMappingSub()
		{
			System.Diagnostics.Debug.Assert(m_astrStepFriendlyNames != null);
			int nStepCount = m_astrStepFriendlyNames.Length;
			System.Diagnostics.Debug.Assert(nStepCount > 0);

			for (int i = 0; i < nStepCount; i++)
			{
				m_aECs.AddCompoundConverterStep
						(
							FriendlyName,
							m_astrStepFriendlyNames[i],
							m_abDirectionForwards[i],
							m_aeNormalizeFlags[i]
						);
			}

			m_aEC = null;   // so it get's requeried (since, for example, the ConvType might have changed if we changed the direction)
		}

		protected void InitNewRow(ref DataGridViewRow theRow, ref DataGridViewCell theCell)
		{
			int nRowIndex = dataGridViewSteps.Rows.Count - 1;
			System.Diagnostics.Debug.Assert(nRowIndex >= 0);
			dataGridViewSteps.Rows.Insert(nRowIndex, 1);
			theRow = dataGridViewSteps.Rows[nRowIndex];
			theCell = theRow.Cells[cnStepNameColumn];
		}

		private void dataGridViewSteps_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
		{
			// if the user clicks on the header... that doesn't work
			if (((e.RowIndex < 0) || (e.RowIndex > dataGridViewSteps.Rows.Count))
				|| ((e.ColumnIndex < cnStepNameColumn) || e.ColumnIndex > cnNormalizationColumn))
				return;

			DataGridViewRow theRow = dataGridViewSteps.Rows[e.RowIndex];
			DataGridViewCell theCell = theRow.Cells[e.ColumnIndex];

			switch (e.ColumnIndex)
			{
				case cnStepNameColumn:
					if (theRow.IsNewRow)
						InitNewRow(ref theRow, ref theCell);

					IEncConverter aEC = m_aECs.AutoSelectWithTitle(ConvType.Unknown, "Choose Converter Step");
					if (aEC != null)
					{
						UpdateConverterCellValue(theRow, aEC);
						UpdateDirectionReverseCellValue(theRow, !aEC.DirectionForward, aEC);
						UpdateNormalizationCellValue(theRow, aEC.NormalizeOutput);
						theRow.Tag = aEC;
					}
					else
					{
						theCell.Value = cstrClickMeMessage;    // New Row "Click me" label
					}

					IsModified = true;
					break;

				default:
					if (!theRow.IsNewRow)   // but don't allow the new row to be edited
					{
						IsModified |= dataGridViewSteps.BeginEdit(false);
					}
					break;
			}
		}

		protected void UpdateConverterCellValue(DataGridViewRow theRow, IEncConverter aEC)
		{
			string strStepName = aEC.Name;
			if (strStepName.Length > cnMaxConverterName)
				strStepName = strStepName.Substring(0, cnMaxConverterName);

			DataGridViewCell theCell = theRow.Cells[cnStepNameColumn];
			theCell.Value = strStepName;
			theCell.ToolTipText = aEC.ToString();
		}

		protected void UpdateDirectionReverseCellValue(DataGridViewRow theRow, bool bDirectionReverse, IEncConverter aEC)
		{
			DataGridViewCell theCell = theRow.Cells[cnDirectionReverseColumn];
			if (EncConverters.IsUnidirectional(aEC.ConversionType))
				theCell.ReadOnly = true;
			theCell.Value = bDirectionReverse;
			theCell.ToolTipText = String.Format("For this step, the converter will be run in the {0} direction", (bDirectionReverse) ? "reverse" : "forward");
		}

		protected void UpdateNormalizationCellValue(DataGridViewRow theRow, NormalizeFlags eNormalizeFlag)
		{
			string strNormalizeValue;
			switch (eNormalizeFlag)
			{
				case NormalizeFlags.FullyComposed:
					strNormalizeValue = "Fully Composed";
					break;
				case NormalizeFlags.FullyDecomposed:
					strNormalizeValue = "Fully Decomposed";
					break;
				default:
					strNormalizeValue = "None";
					break;
			}
			theRow.Cells[cnNormalizationColumn].Value = strNormalizeValue;
		}

		// make it so that the row/cells never look like they're selected (it's annoying)
		private void dataGridViewSteps_SelectionChanged(object sender, EventArgs e)
		{
			if (dataGridViewSteps.SelectedRows.Count > 0)
				dataGridViewSteps.SelectedRows[0].Selected = false;
		}

		protected override void UpdateCompoundConverterNameLabel(string strFriendlyName)
		{
			labelCompoundConverterName.Text = strFriendlyName;
		}
	}
}

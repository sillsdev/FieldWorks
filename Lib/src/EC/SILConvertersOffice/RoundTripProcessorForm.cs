using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ECInterfaces;
using SilEncConverters31;

namespace SILConvertersOffice
{
	internal partial class RoundTripProcessorForm : SILConvertersOffice.SILConverterProcessorForm
	{
		public RoundTripProcessorForm()
		{
			InitializeComponent();

			// a the round-trip row
			this.tableLayoutPanel.SuspendLayout();

			// add our additional row to the table layout panel
			tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
			tableLayoutPanel.Controls.Add(labelRoundTrip, 0, 2);
			tableLayoutPanel.Controls.Add(textBoxRoundTrip, 1, 2);
			tableLayoutPanel.Controls.Add(labelRoundTripCodePoints, 2, 2);

			// make it bigger to accomodate the extra row's worth of controls
			ClientSize = new System.Drawing.Size(648, 222);
			tableLayoutPanel.Size = new System.Drawing.Size(619, 126);

			// for the Designer, it's nicer if these aren't 'fill', but we need them to be that at run-time.
			textBoxRoundTrip.Dock = DockStyle.Fill;
			labelRoundTripCodePoints.Dock = DockStyle.Fill;

			// adjust some base-class Form properties
			labelInputString.Text = "&Input:";
			labelForwardConversion.Text = "&Forward:";

			toolTip.SetToolTip(buttonReplaceOnce, "Click here to replace the text in the document with the text in the 'Round-trip' box");
			toolTip.SetToolTip(buttonReplaceAll, "Click here to replace all the words in the document with the results of the round-trip conversion");

			this.tableLayoutPanel.ResumeLayout(false);
			this.tableLayoutPanel.PerformLayout();
		}

		public string RoundTripString
		{
			get { return this.textBoxRoundTrip.Text; }
			set { this.textBoxRoundTrip.Text = value; }
		}

		public virtual FormButtons Show
			(
			FontConverter aFontPlusEC,
			string strInput,
			string strOutput,
			string strRoundtrip
			)
		{
			m_aFontPlusEC = aFontPlusEC;
			RoundTripString = strRoundtrip;

			if (m_aFontPlusEC.Font != null)
				this.textBoxRoundTrip.Font = m_aFontPlusEC.Font;

			UpdateLhsUniCodes(RoundTripString, this.labelRoundTripCodePoints);

			return base.Show(aFontPlusEC, strInput, strOutput);
		}

		private void textBoxRoundTrip_TextChanged(object sender, EventArgs e)
		{
			UpdateLhsUniCodes(RoundTripString, this.labelRoundTripCodePoints);
		}

		// override theses to add the Round-trip fields
		protected override void RefreshTextBoxes(DirectableEncConverter aEC)
		{
			base.RefreshTextBoxes(aEC);
			RoundTripString = aEC.ConvertDirectionOpposite(ForwardString);
		}
	}

	internal class RoundTripCheckWordProcessor : OfficeDocumentProcessor
	{
		public RoundTripCheckWordProcessor(FontConverters aFCs)
			: base(aFCs, new RoundTripProcessorForm())
		{
		}

		protected override FormButtons ConvertProcessing(OfficeRange aWordRange, FontConverter aThisFC, string strInput, ref int nCharIndex, ref string strReplace)
		{
			// here's the meat of the RoundTripChecker engine: process the word both in the
			// forward and reverse directions and compare the 2nd output with the input
			string strOutput = aThisFC.DirectableEncConverter.Convert(strInput);
			string strRoundtrip = aThisFC.DirectableEncConverter.ConvertDirectionOpposite(strOutput);

			// our 'form' is really a RoundTripProcessorForm (which has special methods/properties we need to call)
			RoundTripProcessorForm form = (RoundTripProcessorForm)Form;
			FormButtons res = FormButtons.None;
			if (!form.SkipIdenticalValues || (strInput != strRoundtrip))
			{
				if (ReplaceAll)
				{
					strReplace = strRoundtrip;
					res = FormButtons.ReplaceAll;
				}
				else
				{
					res = form.Show(aThisFC, strInput, strOutput, strRoundtrip);

					// just in case it's Replace or ReplaceAll, our replacement string is the 'RoundTripString'
					strReplace = form.RoundTripString;
				}
			}

			return res;
		}

		public override void SetRangeFont(OfficeRange aWordRange, string strFontName)
		{
			// this sub-class doesn't ever change the font
			// base.SetRangeFont(aWordRange, strFontName);
		}

		protected override FontConverter QueryForFontConvert(string strFontName)
		{
			return new FontConverter(strFontName);
		}
	}
}

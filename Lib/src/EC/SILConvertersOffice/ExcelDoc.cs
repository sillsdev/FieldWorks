using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;                     // for DialogResult
using Excel = Microsoft.Office.Interop.Excel;

namespace SILConvertersOffice
{
	internal class ExcelRange : OfficeRange
	{
		protected Excel.Range RangeBasedOn
		{
			get { return (Excel.Range)base.m_aRangeBasedOn; }
		}

		public ExcelRange(Excel.Range basedOnRange)
		{
			m_aRangeBasedOn = basedOnRange;
			m_typeRange = basedOnRange.GetType();   // so the properties in the base class work
		}

		public override int Start
		{
			get { return RangeBasedOn.Row; }
		}

		public override int End
		{
			get { return RangeBasedOn.Column; }
		}

		public override string Text
		{
			// can't write to Text, so call "Value" instead
			get { return (string)RangeBasedOn.Value2; }
			set { RangeBasedOn.Value2 = value; }
		}

		public override string FontName
		{
			get { return (string)RangeBasedOn.Font.Name; }
		}

		public override void Select()
		{
			// RangeBasedOn.Activate();
			// RangeBasedOn.Select();
		}
	}

	internal class ExcelDocument : OfficeDocument
	{
		public ExcelDocument(object doc)
			: base(doc)
		{
		}

		public Excel.Range Document
		{
			get { return (Excel.Range)m_baseDocument; }
		}

		public override int WordCount
		{
			get
			{
				return Document.Count;
			}
		}

		public override bool ProcessWordByWord(OfficeDocumentProcessor aWordProcessor)
		{
			if (aWordProcessor.AreLeftOvers)
			{
				DialogResult res = MessageBox.Show("Click 'Yes' to restart where you left off, 'No' to start over at the top, and 'Cancel' to quit", OfficeApp.cstrCaption, MessageBoxButtons.YesNoCancel);
				if (res == DialogResult.No)
					aWordProcessor.LeftOvers = null;
				else if (res == DialogResult.Cancel)
					return true;
			}

			int nCharIndex = 0; // don't care
			foreach (Excel.Range aArea in Document.Areas)
			{
				foreach (Excel.Range aColumn in aArea.Columns)
				{
					if (aWordProcessor.AreLeftOvers && (aWordProcessor.LeftOvers.End > aColumn.Column))
						continue;

					foreach (Excel.Range aRow in aColumn.Rows)
					{
						if(     (aWordProcessor.AreLeftOvers && (aWordProcessor.LeftOvers.Start > aRow.Row))
							||  (String.IsNullOrEmpty((string)aRow.Text)))
							continue;

						ExcelRange aCellRange = new ExcelRange(aRow);
						aWordProcessor.LeftOvers = aCellRange;

						if (!aWordProcessor.Process(aCellRange, ref nCharIndex))
							return false;
					}

					aWordProcessor.LeftOvers = null;
				}
			}

			return true;
		}
	}
}

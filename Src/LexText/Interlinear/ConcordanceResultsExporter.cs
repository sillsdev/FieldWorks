using System.Globalization;
using System.IO;
using System.Xml;
using CsvHelper;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.Utils;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// Write out a csv file with the ConcordanceResults (used by both ConcordanceTool and ComplexConcordanceTool)
	/// </summary>
	class ConcordanceResultsExporter : CollectorEnv
	{
		private TextWriter Writer { get; }
		private CsvHelper.CsvWriter csv { get; }
		private XmlBrowseViewBaseVc VC { get; }
		// CsvWriter expects empty cells to be written. Keep track of when a cell is
		// written. If a cell is being closed and nothing was written to it, then write a empty string.
		private bool HasCellBeenWritten { get; set; }

		public ConcordanceResultsExporter(TextWriter writer, XmlBrowseViewBaseVc vc, ISilDataAccess sda, int hvoRoot) : base(null, sda, hvoRoot)
		{
			Writer = writer;
			csv = new CsvWriter(Writer, CultureInfo.InvariantCulture);
			VC = vc;
			HasCellBeenWritten = false;
			WriteHeader();
		}

		private void WriteHeader()
		{
			// TODO: This doesn't completely work since it lists all the columns, not just the ones displayed.
			/*
			foreach (XmlNode node in VC.ComputePossibleColumns())
			{
				string label = XmlUtils.GetLocalizedAttributeValue(node, "label", null) ??
							   XmlUtils.GetMandatoryAttributeValue(node, "label");
				csv.WriteField(label);
			}
			csv.NextRecord();
			*/
		}

		public void Export()
		{
			VC.Display(this, m_hvoCurr, 100000);
			csv.Flush();
		}

		public override void CloseTableCell()
		{
			// A empty cell might not result in a property value being written.  If we get
			// in here without writing a value then write an empty string to keep the columns aligned.
			if (!HasCellBeenWritten)
				csv.WriteField("");
			HasCellBeenWritten = false;
		}

		public override void AddStringProp(int tag, IVwViewConstructor _vwvc)
		{
			base.AddStringProp(tag, _vwvc);
		}

		public override void AddString(ITsString tss)
		{
			csv.WriteField(tss.Text);
			HasCellBeenWritten = true;
		}

		public override void AddObj(int hvoItem, IVwViewConstructor vc, int frag)
		{
			base.AddObj(hvoItem, vc, frag);
		}

		public override void AddTsString(ITsString tss)
		{
			csv.WriteField(tss.Text);
			HasCellBeenWritten = true;
		}

		public override void CloseTableRow()
		{
			csv.NextRecord();
		}
	}
}

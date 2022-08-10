// Copyright (c) 2022 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Globalization;
using System.IO;
using CsvHelper;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// Write out a csv file with the ConcordanceResults (used by both ConcordanceTool and ComplexConcordanceTool)
	/// </summary>
	class ConcordanceResultsExporter : CollectorEnv
	{
		private TextWriter Writer { get; }
		private CsvWriter Csv { get; }
		private XmlBrowseViewBaseVc VC { get; }
		/// <summary>
		/// CsvWriter expects empty cells to be written. Keep track of when a cell is written.
		/// If a cell is being closed and nothing was written to it, then write a empty string.
		/// </summary>
		private bool HasCellBeenWritten { get; set; }

		public ConcordanceResultsExporter(TextWriter writer, XmlBrowseViewBaseVc vc, ISilDataAccess sda, int hvoRoot) : base(null, sda, hvoRoot)
		{
			Writer = writer;
			Csv = new CsvWriter(Writer, CultureInfo.InvariantCulture);
			VC = vc;
			HasCellBeenWritten = false;
			WriteHeader();
		}

		private void WriteHeader()
		{
			foreach (var columnLabel in XmlBrowseViewBaseVc.GetHeaderLabels(VC))
			{
				Csv.WriteField(columnLabel);
			}
			Csv.NextRecord();
		}

		public void Export()
		{
			VC.Display(this, m_hvoCurr, 100000);
			Csv.Flush();
		}

		public override void CloseTableCell()
		{
			// A empty cell might not result in a property value being written.  If we get
			// in here without writing a value then write an empty string to keep the columns aligned.
			if (!HasCellBeenWritten)
				Csv.WriteField("");
			HasCellBeenWritten = false;
		}

		public override void AddString(ITsString tss)
		{
			Csv.WriteField(tss.Text);
			HasCellBeenWritten = true;
		}

		public override void AddTsString(ITsString tss)
		{
			Csv.WriteField(tss.Text);
			HasCellBeenWritten = true;
		}

		public override void CloseTableRow()
		{
			Csv.NextRecord();
		}
	}
}

using System.IO;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// Write out a csv file with the ConcordanceResults (used by both ConcordanceTool and ComplexConcordanceTool)
	/// </summary>
	class ConcordanceResultsExporter : CollectorEnv
	{
		private TextWriter Writer { get; }
		private XmlBrowseViewBaseVc VC { get; }
		public ConcordanceResultsExporter(TextWriter writer, XmlBrowseViewBaseVc vc, ISilDataAccess sda, int hvoRoot) : base(null, sda, hvoRoot)
		{
			Writer = writer;
			VC = vc;
		}

		public void Export()
		{
			VC.Display(this, m_hvoCurr, 100000);
		}

		public override void CloseTableCell()
		{
			Writer.Write(',');
		}

		public override void AddStringProp(int tag, IVwViewConstructor _vwvc)
		{
			base.AddStringProp(tag, _vwvc);
		}

		public override void AddString(ITsString tss)
		{
			Writer.Write(tss.Text);
		}

		public override void AddObj(int hvoItem, IVwViewConstructor vc, int frag)
		{
			base.AddObj(hvoItem, vc, frag);
		}

		public override void AddTsString(ITsString tss)
		{
			Writer.Write(tss.Text);
		}

		public override void CloseTableRow()
		{
			Writer.WriteLine();
		}
	}
}

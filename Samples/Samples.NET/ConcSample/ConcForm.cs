using System;
using System.Diagnostics;
using System.Configuration;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using SIL.FieldWorks.Common.Framework.TreeForms;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using System.Xml;
using SIL.FieldWorks.FDO.Ling.Generated;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Cellar.Generated;

namespace SIL.FieldWorks.Samples
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class ConcForm : System.Windows.Forms.Form
	{
		StText m_txt;
		//Map from string (words) to arrayLists of MultiLevelConc.ContextInfo.
		Hashtable m_hmStrList = new Hashtable();
		FDO.FdoCache m_cache;
		protected MultiLevelConc mainControl;
		/// <summary>
		///  An arbitrary StText in TestLangProj with several
		///  paragraphs, beginning "Les divers passages où Paul"
		/// </summary>
		int m_hvoText = 7120;
		// each item is a MultiLevelConc.SummaryInfo for one word
		// in the concordance.
		ArrayList m_alSummaries = new ArrayList();
		ArrayList m_alCharSummaries = new ArrayList();
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public ConcForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			Init();
		}
		bool IsLeadSurrogate(char ch)
		{
			const char minLeadSurrogate = '\xD800';
			const char maxLeadSurrogate = '\xDBFF';
			return ch >= minLeadSurrogate && ch <= maxLeadSurrogate;
		}
		/// <summary>
		/// Increment an index into a string, allowing for surrogates.
		/// Refactor JohnT: there should be some more shareable place to put this...
		/// a member function of string would be ideal...
		/// </summary>
		/// <param name="st"></param>
		/// <param name="ich"></param>
		/// <returns></returns>
		int NextChar(string st, int ich)
		{
			//const char minTrailSurrogate = Convert.ToChar(0xDC00);
			//const char maxTrailSurrogate = Convert.ToChar(0xDFFF);
			if (IsLeadSurrogate(st[ich]))
				return ich + 2;
			return ich + 1;
		}
		/// <summary>
		/// Return a full 32-bit character value from the surrogate pair.
		/// </summary>
		/// <param name="ch1"></param>
		/// <param name="ch2"></param>
		/// <returns></returns>
		int Int32FromSurrogates(char ch1, char ch2)
		{
			Debug.Assert(IsLeadSurrogate(ch1));
			return ((ch1 - 0xD800) << 10) + ch2 + 0x2400;
		}
		/// <summary>
		/// Return the full 32-bit character starting at position ich in st
		/// </summary>
		/// <param name="st"></param>
		/// <param name="ich"></param>
		/// <returns></returns>
		int FullCharAt(string st, int ich)
		{
			if (IsLeadSurrogate(st[ich]))
				return Int32FromSurrogates(st[ich], st[ich + 1]);
			else return Convert.ToInt32(st[ich]);
		}
		ITsString GetSubString(ITsString tss, int ichMin, int ichLim)
		{
			ITsStrBldr tsb = tss.GetBldr();
			int len = tss.get_Length();
			if (ichLim < len)
				tsb.Replace(ichLim, tss.get_Length(), null, null);
			if (ichMin > 0)
				tsb.Replace(0, ichMin, null, null);
			return tsb.GetString();
		}
		void BuildWord(StTxtPara stp, string sContents, int ichMin, int ichLim)
		{
			string stWord = sContents.Substring(ichMin, ichLim - ichMin);
			MultiLevelConc.ContextInfo ci =
				new MultiLevelConc.ContextInfo(
					stp.Hvo,
					(int)BaseStTxtPara.StTxtParaTags.kflidContents,
					ichMin,
					stWord.Length,
					false); // no editing for now.
			ArrayList alOccurrences = (ArrayList)m_hmStrList[stWord];
			if (alOccurrences == null)
			{
				alOccurrences = new ArrayList();
				m_hmStrList[stWord] = alOccurrences;
				MultiLevelConc.SummaryInfo si = new MultiLevelConc.SummaryInfo(
					GetSubString(stp.Contents.UnderlyingTsString, ichMin, ichLim),
					alOccurrences);
				m_alSummaries.Add(si);
			}
			alOccurrences.Add(ci);
		}
		void AnalyzePara(StTxtPara stp, ILgCharacterPropertyEngine cpe)
		{
			ITsString tss = stp.Contents.UnderlyingTsString;
			string sContents = tss.get_Text();

			bool fPrevWordForming = false;
			int ichStartWord = -1;
			for (int ich = 0; ich < sContents.Length; ich = NextChar(sContents, ich))
			{
				bool fThisWordForming = cpe.get_IsWordForming(FullCharAt(sContents, ich));
				if (fThisWordForming && !fPrevWordForming)
				{
					// Start of word.
					ichStartWord = ich;
				}
				else if (fPrevWordForming && !fThisWordForming)
				{
					// End of word
					Debug.Assert(ichStartWord >= 0);
					BuildWord(stp, sContents, ichStartWord, ich);
				}
				fPrevWordForming = fThisWordForming;
			}
			if (fPrevWordForming)
				BuildWord(stp, sContents, ichStartWord, sContents.Length);
		}
		public void BuildConcData()
		{
			// Get a character property engine that can distinguish word-forming and
			// other characters.
			ILgWritingSystemFactory encf = m_cache.LanguageWritingSystemFactoryAccessor;
			ILgCharacterPropertyEngine cpe = encf.get_UnicodeCharProps();
			Debug.Assert(cpe != null, "encf.get_UnicodeCharProps() returned null");

			foreach (StTxtPara stp in m_txt.ParagraphsOS)
			{
				AnalyzePara(stp, cpe);
			}
			m_alSummaries.Sort(new SimpleComparer());
			if (m_alSummaries.Count == 0)
				return;
			ITsStrFactory tsf = (ITsStrFactory)new FwKernelLib.TsStrFactoryClass();
			ArrayList alSublist = new ArrayList(); // list for a given letter of alphabet.
			string sKeyCurrent = ((MultiLevelConc.SummaryInfo)m_alSummaries[0]).Key.Substring(0, 1);
			foreach (MultiLevelConc.SummaryInfo si in m_alSummaries)
			{
				string sNewKey = si.Key.Substring(0, 1);
				if (string.Compare(sKeyCurrent, sNewKey, true) != 0)
				{
					AddCharSummary(tsf, sKeyCurrent, alSublist, encf.get_UserWs());
					alSublist = new ArrayList();
					sKeyCurrent = sNewKey;
				}
				alSublist.Add(si);
			}
			AddCharSummary(tsf, sKeyCurrent, alSublist, encf.get_UserWs());
		}
		void AddCharSummary(ITsStrFactory tsf, string sNewKey, ArrayList alSublist, int ws)
		{
			ITsString tssChar = tsf.MakeString(sNewKey, ws);
			MultiLevelConc.SummaryInfo siChar = new MultiLevelConc.SummaryInfo(
				tssChar,alSublist);
			m_alCharSummaries.Add(siChar);
		}

		/// <summary>
		/// Analyze the StText indicated by hvoText. Return an array of ITsStrings
		/// representing the words of the text. Add to gni the information it
		/// needs about the occurrences of each word.
		/// </summary>
		public void Init()
		{
			// Get a data cache
			m_cache = FDO.FdoCache.Create("TestLangProj");
			m_txt = new StText(m_cache, m_hvoText);
			BuildConcData();

			this.mainControl = new MultiLevelConc(m_cache, m_alCharSummaries);

			this.SuspendLayout();
			//
			//  mainControl
			//
			this.mainControl.BackColor = System.Drawing.SystemColors.Window;
			this.mainControl.ForeColor = System.Drawing.SystemColors.WindowText;
			this.mainControl.Location = new System.Drawing.Point(16, 32); // Review JohnT: compute from container info?
			this.mainControl.Name = "ConcSampleSub";
			this.mainControl.Dock = DockStyle.Fill;
			//this.mainControl.Size = new System.Drawing.Size(232, 192); // Review JohnT: compute from container info?
			this.mainControl.TabIndex = 0;

			//
			// Main window (after contained view, following model of HellowView; maybe so
			// we get an appropriate OnSize for the contained view when sizing the main one?
			//
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(400, 400);
			this.Controls.Add(this.mainControl);
			this.Name = "ConcSample"; // Revie JohnT: what's the use of each of these?
			this.Text = "ConcSample";

			this.ResumeLayout(false);
		}


		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null)
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
			//
			// ConcForm
			//
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(292, 266);
			this.Name = "ConcForm";
			this.Text = "ConcForm";

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.Run(new ConcForm());
		}
	}
}

//		/// <summary>
//		/// Obtain an implementation of NodeInfo for the specified object at the specified
//		/// position in the overall list of top-level objects.
//		/// </summary>
//		public TwoLevelConc.NodeInfo InfoFor(int ihvoRoot, int hvoRoot)
//		{
//			// Todo JohnT: figure arguments here...
//			string stWord = ((ITsString)m_alStrings[ihvoRoot]).get_Text();
//			ArrayList alOccurrences = (ArrayList) m_hmStrList[stWord];
//			int[] startOffsets = new Int[alOccurrences.Count];
//			for (int i = 0; i < alOccurrences.Count; i++)
//				startOffsets[i] = ((WordOccurrence) alOccurrences[i]).ich;
//			// Todo JohnT: we have to make a dummy property in the cache
//			// which is the references to this word...also an HVO for the word...
//			// where do we note the dummy HVO?
//			int flidList = 0;
//			TwoLevelConc.ParaNodeInfo pni = new TwoLevelConc.ParaNodeInfo(flidList, startOffsets, stWord.Length);
//			return pni;
//		}

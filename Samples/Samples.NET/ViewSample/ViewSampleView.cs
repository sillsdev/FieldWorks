/// --------------------------------------------------------------------------------------------
#region /// Copyright (c) 2002, SIL International. All Rights Reserved.
/// <copyright from='2002' to='2002' company='SIL International'>
///		Copyright (c) 2002, SIL International. All Rights Reserved.
///
///		Distributable under the terms of either the Common Public License or the
///		GNU Lesser General Public License, as specified in the LICENSING.txt file.
/// </copyright>
#endregion
///
/// File: ViewSampleView.cs
/// Responsibility: John Thomson
/// Last reviewed:
///
/// <remarks>
/// Implementation of the main view of the ViewSample sample in .NET
/// </remarks>
/// --------------------------------------------------------------------------------------------

using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Xml;
using System.Diagnostics;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.Samples.ViewSample
{
	/// <summary>
	/// Summary description for HelloView.
	/// </summary>
	public class ViewSampleView : SIL.FieldWorks.Common.RootSites.SimpleRootSite
	{
		public const int khvoBook = 1; // The whole book always has this hvo.
		public int m_hvoNextSection = 1000; // next unused section ID.
		public int m_hvoNextPara = 100000; // next unused paragraph ID.

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		private ViewSampleVc m_vc;
		ISilDataAccess m_sda;

		#region Constructor, Dispose and Component Designer generated code
		public ViewSampleView()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
			m_wsf.Shutdown(); // Not normally in View Dispose, but after closing ALL views.
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
		}
		#endregion
		#endregion

		public override void MakeRoot()
		{
			m_rootb = (IVwRootBox)new FwViews.VwRootBoxClass();
			m_rootb.SetSite(this);

			m_sda = (ISilDataAccess) new FwViews.VwCacheDaClass();
			// Usually not here, but in some application global passed to each view.
			m_wsf = (ILgWritingSystemFactory) new FwLanguage.LgWritingSystemFactoryClass();
			m_sda.set_WritingSystemFactory(m_wsf);
			m_rootb.set_DataAccess(m_sda);

			m_vc = new ViewSampleVc(); // Before LoadData, which sets some of its properties.

			LoadData("data.xml");

			m_rootb.SetRootObject(khvoBook, m_vc, ViewSampleVc.kfrBook, new SimpleStyleSheet());
			m_fRootboxMade = true;
			m_dxdLayoutWidth = -50000; // Don't try to draw until we get OnSize and do layout.
		}

		/// <summary>
		/// This is a demonstration of one way to handle special tricks as the user types.
		/// This is an oversimplified way of forcing numbers to be treated as verse numbers...
		/// for example, it will cause '5000' in the 'feeding of the 5000' to be treated as a verse number.
		/// </summary>
		/// <param name="vwselNew"></param>
		protected override void HandleSelectionChange(IVwSelection vwselNew)
		{
			base.HandleSelectionChange (vwselNew);
			if (vwselNew == null)
				return; // Not sure whether this happens, but best to be sure.

			int ichSel, hvoObj, tag, ws;
			bool fAssocPrev;
			ITsString tss;
			vwselNew.TextSelInfo(false, out tss, out ichSel, out fAssocPrev, out hvoObj, out tag, out ws);

			string text = tss.get_Text();
			if (text == null)
				return; // empty string.
			ITsStrBldr tsb = null;
			for (int ich = 0; ich < text.Length; ++ich)
			{
				if (Char.IsDigit(text[ich]))
				{
					ITsTextProps ttp = tss.get_PropertiesAt(ich);
					string styleName = ttp.GetStrPropValue((int)FwKernelLib.FwTextPropType.ktptNamedStyle);
					if (styleName != "verseNumber")
					{
						// We'll change just this one character. We could make this more efficient for dealing with
						// long strings of digits, but it's unlikely we'll ever have to deal with more than one.
						if (tsb == null)
							tsb = tss.GetBldr();
						tsb.SetStrPropValue(ich, ich + 1, (int)FwKernelLib.FwTextPropType.ktptNamedStyle, "verseNumber");
					}
				}
			}
			if (tsb != null)
			{
				ISilDataAccess sda = m_rootb.get_DataAccess();
				// In this sample the only string is a multistring. If in doubt, we could test for ws == 0 to
				// see whether it is a simple string.
				sda.SetMultiStringAlt(hvoObj, tag, ws, tsb.GetString());
				sda.PropChanged(null, (int)FwViews.PropChangeType.kpctNotifyAll, hvoObj, tag, 0, tss.get_Length(), tss.get_Length());
			}
		}

		protected override bool OnRightMouseDown(Point pt, Rectangle rcSrcRoot, Rectangle rcDstRoot)
		{
			IVwSelection vwsel = m_rootb.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, false);
			if (vwsel != null)
			{
				int clevels = vwsel.CLevels(false);
				// Figure which paragraph we clicked. The last level (clevels-1) is our top-level
				// list: hvoObj would be khvoBook. (clevels-2) is next: hvoObj is a section.
				// (clevels-3) gives us a paragraph object.
				int hvoObj, tag, ihvo, cpropPrevious;
				IVwPropertyStore vps;
				vwsel.PropInfo(false, clevels - 3, out hvoObj, out tag, out ihvo, out cpropPrevious, out vps);

				Parse(hvoObj);
			}

			return true;
		}

		/// <summary>
		/// Parse the text in hvoPara.Contents[vc.DestWs] and make words
		/// </summary>
		/// <param name="hvoPara"></param>
		public void Parse(int hvoPara)
		{
			ITsString tssSrc = m_sda.get_MultiStringAlt(hvoPara, ViewSampleVc.ktagParaContents, m_vc.DestWs);
			WordMaker wm = new WordMaker(tssSrc, m_wsf);
			int ichMin, ichLim;
			int cbundle = m_sda.get_VecSize(hvoPara, ViewSampleVc.ktagParaBundles);
			// Clean it out. This wouldn't normally be appropriate for an owning property, but we can get away
			// with it for a non-database cache.
			if (cbundle != 0)
				m_sda.Replace(hvoPara, ViewSampleVc.ktagParaBundles, 0, cbundle, new int[0], 0);
			int ibundle = 0;
			ITsPropsFactory tpf = (ITsPropsFactory) new FwKernelLib.TsPropsFactoryClass();
			ITsTextProps ttp = tpf.MakeProps(null, m_vc.SourceWs, 0);
			for (ITsString tssWord = wm.NextWord(out ichMin, out ichLim); tssWord != null;
				tssWord = wm.NextWord(out ichMin, out ichLim))
			{
				// 4 is an arbitrary classid; this kind of cache does nothing with it.
				int hvoBundle = m_sda.MakeNewObject(4, hvoPara, ViewSampleVc.ktagParaBundles, ibundle);
				ibundle++;
				m_sda.SetString(hvoBundle, ViewSampleVc.ktagBundleBase, tssWord);
				ITsStrBldr tsb = tssWord.GetBldr();
				tsb.Replace(0, 0, "idiom(", ttp);
				tsb.Replace(tsb.get_Length(), tsb.get_Length(), ")", ttp);
				m_sda.SetString(hvoBundle, ViewSampleVc.ktagBundleIdiom, tsb.GetString());

				tsb = tssWord.GetBldr();
				tsb.Replace(0, 0, "ling(", ttp);
				tsb.Replace(tsb.get_Length(), tsb.get_Length(), ")", ttp);
				m_sda.SetString(hvoBundle, ViewSampleVc.ktagBundleLing, tsb.GetString());
			}
			m_sda.PropChanged(null, (int)FwViews.PropChangeType.kpctNotifyAll, hvoPara, ViewSampleVc.ktagParaBundles, 0, ibundle, cbundle);
		}



		public static string GetAttrVal(XmlNode node, string attrName)
		{
			XmlAttribute xa = node.Attributes[attrName];
			if (xa == null)
				return null;
			return xa.Value;
		}

		public static int GetIntVal(XmlNode node, string attr, int defVal)
		{
			XmlAttribute xa = node.Attributes[attr];
			if (xa == null)
				return defVal;

			return Convert.ToInt32(xa.Value,10);
		}

		public void MakeStringProp(int hvo, XmlNode node, string attrName, int tag, int ws, ITsStrFactory tsf,
			IVwCacheDa cda)
		{
			string val = GetAttrVal(node, attrName);
			if (val == null)
				return;
			cda.CacheStringProp(hvo, tag, tsf.MakeString(val, ws));
		}

		/// <summary>
		/// Load data into the VwCacheDa.
		/// </summary>
		public void LoadData(string filename)
		{
			ITsStrFactory tsf = (ITsStrFactory)new FwKernelLib.TsStrFactoryClass();
			IVwCacheDa cda = (IVwCacheDa) m_sda;

			XmlDocument xmlDoc = new XmlDocument();
			xmlDoc.Load(filename);

			XmlNode book = xmlDoc.SelectSingleNode("book");
			int wsAnalysis = m_wsf.get_Engine("en").get_WritingSystem(); // In real life try to get a meaningful one.
			int wsVern = m_wsf.get_Engine("de").get_WritingSystem(); // Vernacular ws.
			m_vc.SourceWs = wsAnalysis;
			m_vc.DestWs = wsVern;
			MakeStringProp(khvoBook, book, "name", ViewSampleVc.ktagBookName, wsAnalysis, tsf, cda);

			ITsPropsBldr tpb = (ITsPropsBldr) new FwKernelLib.TsPropsBldrClass();
			tpb.SetIntPropValues((int)FwKernelLib.FwTextPropType.ktptWs,
				(int)FwKernelLib.FwTextPropVar.ktpvDefault, wsAnalysis);
			tpb.SetStrPropValue((int)FwKernelLib.FwTextPropType.ktptNamedStyle, "verseNumber");
			ITsTextProps ttpVStyle = tpb.GetTextProps();

			int [] sectionIds = new int[book.ChildNodes.Count];
			int isection = 0;
			foreach (XmlNode section in book.ChildNodes)
			{
				int sectionId = m_hvoNextSection++;
				sectionIds[isection] = sectionId;
				isection++;
				MakeStringProp(sectionId, section, "refs", ViewSampleVc.ktagSectionRefs, wsAnalysis, tsf, cda);
				MakeStringProp(sectionId, section, "title", ViewSampleVc.ktagSectionTitle, wsAnalysis, tsf, cda);

				int[] paraIds = new int[section.ChildNodes.Count];
				int ipara = 0;
				foreach(XmlNode para in section.ChildNodes)
				{
					int paraId = m_hvoNextPara++;
					paraIds[ipara] = paraId;
					ipara++;
					// Construct paragraph contents string
					ITsStrBldr tsb = (ITsStrBldr) new FwKernelLib.TsStrBldrClass();
					foreach(XmlNode item in para.ChildNodes)
					{
						int ichLim = tsb.get_Length();
						if (item.Name == "v")
						{
							string num = GetAttrVal(item, "n");
							tsb.Replace(ichLim, ichLim, num, ttpVStyle);
						}
						else if (item.Name == "s")
						{
							string wsName = GetAttrVal(item, "ws");
							int ws = wsName == null ? wsAnalysis : m_wsf.get_Engine(wsName).get_WritingSystem();
							tsb.ReplaceTsString(ichLim, ichLim, tsf.MakeString(item.InnerText, ws));
						}
					}
					// Review: should we assume the default ws analysis? Represent a multistring in the file?
					// Remember the last ws we encountered in the string? This is just a sample...
					cda.CacheStringAlt(paraId, ViewSampleVc.ktagParaContents, wsAnalysis, tsb.GetString());
				}
				cda.CacheVecProp(sectionId, ViewSampleVc.ktagSectionParas, paraIds, paraIds.Length);
			}
			cda.CacheVecProp(khvoBook, ViewSampleVc.ktagBookSections, sectionIds, sectionIds.Length);
		}
	}
}

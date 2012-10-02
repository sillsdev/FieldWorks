using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// The modification of the main class suitable for this view.
	/// </summary>
	public partial class InterlinPrintChild : InterlinDocRootSiteBase
	{
		public InterlinPrintChild()
		{
			InitializeComponent();
		}

		/// <summary>
		/// Pull this out into a separate method so InterlinPrintChild can make an InterlinPrintVc.
		/// </summary>
		protected override void MakeVc()
		{
			m_vc = new InterlinPrintVc(m_fdoCache);
		}

		/// <summary>
		/// Activate() is disabled by default in ReadOnlyViews, but PrintView does want to show selections.
		/// </summary>
		protected override bool AllowDisplaySelection
		{
			get { return true; }
		}

	}

	/// <summary>
	/// Modifications of InterlinVc for printing.
	/// </summary>
	public class InterlinPrintVc : InterlinVc
	{
		internal const int kfragTextDescription = 200027;
		internal int vtagStTextTitle = 0;
		internal int vtagStTextSource = 0;

		public InterlinPrintVc(FdoCache cache) : base(cache)
		{

		}

		protected override int LabelRGBFor(InterlinLineSpec spec)
		{
			// In the print view these colors are plain black.
			return 0;
		}

		protected override void GetSegmentLevelTags(FdoCache cache)
		{
			// for PrintView
			vtagStTextTitle = cache.MetaDataCacheAccessor.GetFieldId("StText", "Title", false);
			vtagStTextSource = cache.MetaDataCacheAccessor.GetFieldId("StText", "Source", false);
			base.GetSegmentLevelTags(cache);
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			ITsStrFactory tsf = null;
			switch (frag)
			{
				case kfragStText: // The whole text, root object for the InterlinDocChild.
					if (hvo == 0)
						return;		// What if the user deleted all the texts?  See LT-6727.
					IStText stText = m_coRepository.GetObject(hvo) as IStText;
					vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
						(int)FwTextPropVar.ktpvDefault,
						(int)TptEditable.ktptNotEditable);
					vwenv.OpenDiv();
					vwenv.set_IntProperty((int)FwTextPropType.ktptMarginBottom,
						(int)FwTextPropVar.ktpvMilliPoint, 6000);
					vwenv.set_IntProperty((int)FwTextPropType.ktptFontSize,
						(int)FwTextPropVar.ktpvMilliPoint, 24000);
					// Add both vernacular and analysis if we have them (LT-5561).
					bool fAddedVernacular = false;
					int wsVernTitle = 0;
					//
					if (stText.Title.TryWs(WritingSystemServices.kwsFirstVern, out wsVernTitle))
					{
						vwenv.OpenParagraph();
						vwenv.AddStringAltMember(vtagStTextTitle, wsVernTitle, this);
						vwenv.CloseParagraph();
						fAddedVernacular = true;
					}
					int wsAnalysisTitle = 0;
					vwenv.set_IntProperty((int)FwTextPropType.ktptMarginBottom,
						(int)FwTextPropVar.ktpvMilliPoint, 10000);
					vwenv.OpenParagraph();
					ITsString tssAnal;
					if (stText.Title.TryWs(WritingSystemServices.kwsFirstAnal, out wsAnalysisTitle, out tssAnal) &&
						!tssAnal.Equals(stText.Title.BestVernacularAlternative))
					{
						if (fAddedVernacular)
						{
							// display analysis title at smaller font size.
							vwenv.set_IntProperty((int)FwTextPropType.ktptFontSize,
								(int)FwTextPropVar.ktpvMilliPoint, 12000);
						}
						vwenv.AddStringAltMember(vtagStTextTitle, wsAnalysisTitle, this);
					}
					else
					{
						// just add a blank title.
						tsf = TsStrFactoryClass.Create();
						ITsString blankTitle = tsf.MakeString("", m_wsAnalysis);
						vwenv.AddString(blankTitle);
					}
					vwenv.CloseParagraph();
					vwenv.set_IntProperty((int)FwTextPropType.ktptMarginBottom,
						(int)FwTextPropVar.ktpvMilliPoint, 10000);
					int wsSource = 0;
					if (stText.Source.TryWs(WritingSystemServices.kwsFirstVernOrAnal, out wsSource))
					{
						vwenv.OpenParagraph();
						vwenv.set_IntProperty((int)FwTextPropType.ktptFontSize,
							(int)FwTextPropVar.ktpvMilliPoint, 12000);
						vwenv.AddStringAltMember(vtagStTextSource, wsSource, this);
						vwenv.CloseParagraph();
					}
					else
					{
						// just add a blank source.
						tsf = TsStrFactoryClass.Create();
						ITsString tssBlank = tsf.MakeString("", m_wsAnalysis);
						vwenv.AddString(tssBlank);
					}
					vwenv.set_IntProperty((int)FwTextPropType.ktptMarginBottom,
						(int)FwTextPropVar.ktpvMilliPoint, 10000);
					vwenv.OpenParagraph();
					if (stText.OwningFlid == TextTags.kflidContents)
					{
						vwenv.AddObjProp((int)CmObjectFields.kflidCmObject_Owner, this, kfragTextDescription);
					}
					vwenv.CloseParagraph();
					base.Display(vwenv, hvo, frag);
					vwenv.CloseDiv();
					break;
				case kfragTextDescription:
					vwenv.AddStringAltMember(CmMajorObjectTags.kflidDescription, m_wsAnalysis, this);
					break;
				default:
					base.Display(vwenv, hvo, frag);
					break;
			}
		}

	}
}

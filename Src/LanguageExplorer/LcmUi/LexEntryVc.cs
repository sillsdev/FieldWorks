// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using LanguageExplorer.Controls;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainImpl;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// Override to support kfragHeadword with a properly live display of the headword.
	/// Also, the default of displaying the vernacular writing system can be overridden.
	/// </summary>
	public class LexEntryVc : CmVernObjectVc
	{
		/// <summary>
		/// arbitrary.
		/// </summary>
		private const int kfragFormForm = 9543;
		/// <summary>
		/// use with WfiMorphBundle to display the headword with variant info appended.
		/// </summary>
		public const int kfragEntryAndVariant = 9544;
		/// <summary>
		/// use with EntryRef to display the variant type info
		/// </summary>
		public const int kfragVariantTypes = 9545;
		private int m_wsActual;

		/// <summary />
		public LexEntryVc(LcmCache cache)
			: base(cache)
		{
			WritingSystemCode = cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle;
		}

		public int WritingSystemCode { get; set; }

		/// <summary>
		/// Display a view of the LexEntry (or fragment thereof).
		/// </summary>
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			switch (frag)
			{
				case (int)VcFrags.kfragHeadWord:
					// This case should stay in sync with
					// LexEntry.LexemeFormMorphTypeAndHomographStatic
					vwenv.OpenParagraph();
					AddHeadwordWithHomograph(vwenv, hvo);
					vwenv.CloseParagraph();
					break;
				case kfragEntryAndVariant:
					var wfb = m_cache.ServiceLocator.GetInstance<IWfiMorphBundleRepository>().GetObject(hvo);
					//int hvoMf = wfb.MorphRA.Hvo;
					//int hvoLexEntry = m_cache.GetOwnerOfObject(hvoMf);
					// if morphbundle morph (entry) is in a variant relationship to the morph bundle sense
					// display its entry headword and variant type information (LT-4053)
					ILexEntryRef ler;
					var variant = wfb.MorphRA.Owner as ILexEntry;
					if (variant.IsVariantOfSenseOrOwnerEntry(wfb.SenseRA, out ler))
					{
						// build Headword from sense's entry
						vwenv.OpenParagraph();
						vwenv.OpenInnerPile();
						vwenv.AddObj(wfb.SenseRA.EntryID, this, (int)VcFrags.kfragHeadWord);
						vwenv.CloseInnerPile();
						vwenv.OpenInnerPile();
						// now add variant type info
						vwenv.AddObj(ler.Hvo, this, kfragVariantTypes);
						vwenv.CloseInnerPile();
						vwenv.CloseParagraph();
						break;
					}
					// build Headword even though we aren't in a variant relationship.
					vwenv.AddObj(variant.Hvo, this, (int)VcFrags.kfragHeadWord);
					break;
				case kfragVariantTypes:
					ler = m_cache.ServiceLocator.GetInstance<ILexEntryRefRepository>().GetObject(hvo);
					var fNeedInitialPlus = true;
					vwenv.OpenParagraph();
					foreach (var let in ler.VariantEntryTypesRS.Where(let => let.ClassID == LexEntryTypeTags.kClassId))
					{
						// just concatenate them together separated by comma.
						var tssVariantTypeRevAbbr = let.ReverseAbbr.BestAnalysisAlternative;
						if (tssVariantTypeRevAbbr != null && tssVariantTypeRevAbbr.Length > 0)
						{
							vwenv.AddString(fNeedInitialPlus ? TsStringUtils.MakeString("+", m_cache.DefaultUserWs) : TsStringUtils.MakeString(",", m_cache.DefaultUserWs));
							vwenv.AddString(tssVariantTypeRevAbbr);
							fNeedInitialPlus = false;
						}
					}
					vwenv.CloseParagraph();
					break;
				case kfragFormForm: // form of MoForm
					vwenv.AddStringAltMember(MoFormTags.kflidForm, m_wsActual, this);
					break;
				default:
					base.Display(vwenv, hvo, frag);
					break;
			}
		}

		private void AddHeadwordWithHomograph(IVwEnv vwenv, int hvo)
		{
			var sda = vwenv.DataAccess;
			var hvoLf = sda.get_ObjectProp(hvo, LexEntryTags.kflidLexemeForm);
			var hvoType = 0;
			if (hvoLf != 0)
			{
				hvoType = sda.get_ObjectProp(hvoLf, MoFormTags.kflidMorphType);
			}
			// If we have a type of morpheme, show the appropriate prefix that indicates it.
			// We want vernacular so it will match the point size of any aligned vernacular text.
			// (The danger is that the vernacular font doesn't have these characters...not sure what
			// we can do about that, but most do, and it looks awful in analysis if that is a
			// much different size from vernacular.)
			string sPrefix = null;
			if (hvoType != 0)
			{
				sPrefix = sda.get_UnicodeProp(hvoType, MoMorphTypeTags.kflidPrefix);
			}
			// Show homograph number if non-zero.
			var defUserWs = m_cache.WritingSystemFactory.UserWs;
			var nHomograph = sda.get_IntProp(hvo, LexEntryTags.kflidHomographNumber);
			var hc = m_cache.ServiceLocator.GetInstance<HomographConfiguration>();
			//Insert HomographNumber when position is Before
			if (hc.HomographNumberBefore)
			{
				InsertHomographNumber(vwenv, hc, nHomograph, defUserWs);
			}
			// LexEntry.ShortName1; basically tries for form of the lexeme form, then the citation form.
			var fGotLabel = false;
			var wsActual = 0;
			if (hvoLf != 0)
			{
				// if we have a lexeme form and its label is non-empty, use it.
				if (TryMultiStringAlt(hvoLf, MoFormTags.kflidForm, out wsActual))
				{
					m_wsActual = wsActual;
					fGotLabel = true;
					if (sPrefix != null)
					{
						vwenv.AddString(TsStringUtils.MakeString(sPrefix, wsActual));
					}
					vwenv.AddObjProp(LexEntryTags.kflidLexemeForm, this, kfragFormForm);
				}
			}
			if (!fGotLabel)
			{
				// If we didn't get a useful form from the lexeme form try the citation form.
				if (TryMultiStringAlt(hvo, LexEntryTags.kflidCitationForm, out wsActual))
				{
					m_wsActual = wsActual;
					if (sPrefix != null)
					{
						vwenv.AddString(TsStringUtils.MakeString(sPrefix, wsActual));
					}
					vwenv.AddStringAltMember(LexEntryTags.kflidCitationForm, wsActual, this);
					fGotLabel = true;
				}
			}
			if (!fGotLabel)
			{
				// If that fails just show two questions marks.
				if (sPrefix != null)
				{
					vwenv.AddString(TsStringUtils.MakeString(sPrefix, wsActual));
				}
				vwenv.AddString(TsStringUtils.MakeString(LcmUiStrings.ksQuestions, defUserWs)); // was "??", not "???"
			}
			// If we have a lexeme form type show the appropriate postfix.
			if (hvoType != 0)
			{
				vwenv.AddString(TsStringUtils.MakeString(sda.get_UnicodeProp(hvoType, MoMorphTypeTags.kflidPostfix), wsActual));
			}
			vwenv.NoteDependency(new[] { hvo }, new[] { LexEntryTags.kflidHomographNumber }, 1);
			//Insert HomographNumber when position is After
			if (!hc.HomographNumberBefore)
			{
				InsertHomographNumber(vwenv, hc, nHomograph, defUserWs);
			}
		}

		/// <summary>
		/// Method to insert the homograph number with settings into the Text
		/// </summary>
		private void InsertHomographNumber(IVwEnv vwenv, HomographConfiguration hc, int nHomograph, int defUserWs)
		{
			if (nHomograph <= 0)
			{
				return;
			}
			// Use a string builder to embed the properties in with the TsString.
			// this allows our TsStringCollectorEnv to properly encode the superscript.
			// ideally, TsStringCollectorEnv could be made smarter to handle SetIntPropValues
			// since AppendTss treats the given Tss as atomic.
			var tsBldr = TsStringUtils.MakeIncStrBldr();
			tsBldr.SetIntPropValues((int)FwTextPropType.ktptSuperscript, (int)FwTextPropVar.ktpvEnum, (int)FwSuperscriptVal.kssvSub);
			tsBldr.SetIntPropValues((int)FwTextPropType.ktptBold, (int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
			tsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, defUserWs);
			StringServices.InsertHomographNumber(tsBldr, nHomograph, hc, HomographConfiguration.HeadwordVariant.Main, m_cache);
			vwenv.AddString(tsBldr.GetString());
		}

		/// <summary />
		public static ITsString GetLexEntryTss(LcmCache cache, int hvoEntryToDisplay, int wsVern, ILexEntryRef ler)
		{
			var vcEntry = new LexEntryVc(cache) { WritingSystemCode = wsVern };
			var collector = new TsStringCollectorEnv(null, cache.MainCacheAccessor, hvoEntryToDisplay)
			{
				RequestAppendSpaceForFirstWordInNewParagraph = false
			};
			vcEntry.Display(collector, hvoEntryToDisplay, (int)VcFrags.kfragHeadWord);
			if (ler != null)
			{
				vcEntry.Display(collector, ler.Hvo, kfragVariantTypes);
			}
			return collector.Result;
		}

		private bool TryMultiStringAlt(int hvo, int flid, out int wsActual)
		{
			return WritingSystemServices.GetMagicStringAlt(m_cache, WritingSystemCode, hvo, flid, true, out wsActual) != null;
		}
	}
}
// Copyright (c) 2004-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Drawing;
using LanguageExplorer.Controls;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	internal class RawTextVc : StVc
	{
		public const int kTagUserPrompt = 1000009879; // very large number prevents auto-load.
		IVwRootBox m_rootb;

		public RawTextVc(IVwRootBox rootb, LcmCache cache, int wsFirstPara) : base("Normal", wsFirstPara)
		{
			m_rootb = rootb;
			Cache = cache;
			// This is normally done in the Cache setter, but not if the default WS is already set.
			// I'm not sure why not, but rather than mess with a shared base class, we'll just
			// fix it here.
			SetupVernWsForText(m_wsDefault);
			Lazy = true;
		}

		internal void SetupVernWsForText(int wsVern)
		{
			m_wsDefault = wsVern;
			var defWs = Cache.ServiceLocator.WritingSystemManager.Get(wsVern);
			RightToLeft = defWs.RightToLeftScript;
		}

		// This evaluates a paragraph to find out whether to display a user prompt, and if so,
		// inserts one.
		protected override bool InsertParaContentsUserPrompt(IVwEnv vwenv, int paraHvo)
		{
			// The only easy solution for LT-1437 "Pasting in a text produces unequal results"
			// is to not have the user prompt!
			return false;
		}

		/// <summary>
		/// Gets a value indicating whether to set the base WS and direction according to the
		/// first run in the paragraph contents.
		/// </summary>
		public override bool BaseDirectionOnParaContents => true;

		/// <summary>
		/// Set the BaseWs and RightToLeft properties for the paragraph that is being laid out.
		/// These are computed (if possible) from the current paragraph; otherwise, use the
		/// default as set on the view constructor for the whole text. This override also sets
		/// the alignment (which presumably overrides the alignment set in the stylesheet?).
		/// </summary>
		protected override void SetupWsAndDirectionForPara(IVwEnv vwenv, int paraHvo)
		{
			base.SetupWsAndDirectionForPara(vwenv, paraHvo);

			vwenv.set_IntProperty((int)FwTextPropType.ktptAlign, (int)FwTextPropVar.ktpvEnum, RightToLeft ? (int)FwTextAlign.ktalRight : (int)FwTextAlign.ktalLeft);
		}

		/// <summary />
		public override ITsString UpdateProp(IVwSelection vwsel, int hvo, int tag, int frag, ITsString tssVal)
		{
			Debug.Assert(tag == kTagUserPrompt, "Got an unexpected tag");

			// Get information about current selection
			var cvsli = vwsel.CLevels(false);
			cvsli--; // CLevels includes the string property itself, but AllTextSelInfo doesn't need it.
			int ihvoRoot;
			int tagTextProp;
			int cpropPrevious;
			int ichAnchor;
			int ichEnd;
			int ihvoEnd;
			bool fAssocPrev;
			int ws;
			ITsTextProps ttp;
			var rgvsli = SelLevInfo.AllTextSelInfo(vwsel, cvsli, out ihvoRoot, out tagTextProp, out cpropPrevious, out ichAnchor, out ichEnd, out ws, out fAssocPrev, out ihvoEnd, out ttp);
			// get para info
			var para = Cache.ServiceLocator.GetInstance<IStTxtParaRepository>().GetObject(hvo);
			// Add the text the user just typed to the paragraph - this destroys the selection
			// because we replace the user prompt.
			para.Contents = tssVal;
			// now restore the selection
			m_rootb.MakeTextSelection(ihvoRoot, cvsli, rgvsli, StTxtParaTags.kflidContents, cpropPrevious, ichAnchor, ichEnd, Cache.DefaultVernWs, fAssocPrev, ihvoEnd, null, true);

			return tssVal;
		}

		/// <summary>
		/// We only use this to generate our empty text prompt.
		/// </summary>
		public override ITsString DisplayVariant(IVwEnv vwenv, int tag, int frag)
		{
			var userPrompt = ITextStrings.ksEnterOrPasteHere;
			var ttpBldr = TsStringUtils.MakePropsBldr();
			ttpBldr.SetIntPropValues((int)FwTextPropType.ktptBackColor, (int)FwTextPropVar.ktpvDefault, Color.LightGray.ToArgb());
			ttpBldr.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, Cache.DefaultUserWs);
			var bldr = TsStringUtils.MakeStrBldr();
			bldr.Replace(0, 0, userPrompt, ttpBldr.GetTextProps());
			// Begin the prompt with a zero-width space in the vernacular writing system (with
			// no funny colors).  This ensures anything the user types (or pastes from a non-FW
			// clipboard) is put in that WS.
			// 200B == zero-width space.
			var ttpBldr2 = TsStringUtils.MakePropsBldr();
			ttpBldr2.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, Cache.DefaultVernWs);
			bldr.Replace(0, 0, "\u200B", ttpBldr2.GetTextProps());
			return bldr.GetString();
		}

		public override ITsTextProps CaptionProps
		{
			get
			{
				var bldr = TsStringUtils.MakePropsBldr();
				bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Dictionary-Pictures");
				bldr.SetIntPropValues((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
				return bldr.GetTextProps();
			}
		}
	}
}
// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace LanguageExplorer.Controls.DetailControls
{
	internal class LiteralLabelVc : FwBaseVc
	{
		readonly ITsString m_text;

		public LiteralLabelVc(string text, int ws)
		{
			m_text = MakeUiElementString(text, ws, null);
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			vwenv.set_IntProperty((int)FwTextPropType.ktptForeColor,
				(int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.FromKnownColor(KnownColor.ControlDarkDark)));
			// By default, the paragraph that is created automatically by AddString will automatically inherit
			// the background color of the whole view (typically white). A paragraph with a background color
			// of white rather than transparent is automatically as wide as it is allowed to be (so as to display
			// the background color over the whole area the user things of as being that paragraph).
			// However, we want LiteralLabelView to adjust its size so it is just big enough to show the label,
			// so we can use the rest of the space for the command menu items. So we need to make the paragraph
			// transparent background, which allows it to be just as wide as the text content.
			vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor,
				(int)FwTextPropVar.ktpvDefault,
				(int)FwTextColor.kclrTransparent);
			vwenv.set_IntProperty((int)FwTextPropType.ktptBold,
				(int)FwTextPropVar.ktpvEnum, (int)FwTextToggleVal.kttvForceOn);
			vwenv.AddString(m_text);
		}
	}
}
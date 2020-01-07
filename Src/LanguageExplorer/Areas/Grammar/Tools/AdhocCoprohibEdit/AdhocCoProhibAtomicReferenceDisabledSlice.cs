// Copyright (c) 2012-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer.Controls.DetailControls;

namespace LanguageExplorer.Areas.Grammar.Tools.AdhocCoprohibEdit
{
	internal class AdhocCoProhibAtomicReferenceDisabledSlice : AdhocCoProhibAtomicReferenceSlice
	{
		public override void FinishInit()
		{
			base.FinishInit();
			var arl = (AtomicReferenceLauncher)Control;
			var view = (AtomicReferenceView)arl.MainControl;
			view.FinishInit(ConfigurationNode);
		}
	}
}
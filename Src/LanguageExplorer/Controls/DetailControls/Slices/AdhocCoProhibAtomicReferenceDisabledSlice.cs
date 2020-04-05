// Copyright (c) 2012-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls.DetailControls.Slices
{
	internal sealed class AdhocCoProhibAtomicReferenceDisabledSlice : AdhocCoProhibAtomicReferenceSlice
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
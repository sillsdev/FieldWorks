// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer.Controls.DetailControls;

namespace LanguageExplorer.Areas.Grammar.Tools.AdhocCoprohibEdit
{
	internal class AdhocCoProhibVectorReferenceDisabledSlice : AdhocCoProhibVectorReferenceSlice
	{
		public AdhocCoProhibVectorReferenceDisabledSlice()
			: base()
		{
		}
		public override void FinishInit()
		{
			CheckDisposed();
			base.FinishInit();
			var arl = (VectorReferenceLauncher)Control;
			var view = (VectorReferenceView)arl.MainControl;
			view.FinishInit(ConfigurationNode);
		}
	}
}
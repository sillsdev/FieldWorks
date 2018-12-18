// Copyright (c) 2005-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Drawing;
using LanguageExplorer.Controls.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Areas.Grammar.Tools.AdhocCoprohibEdit
{
	/// <summary />
	internal class AdhocCoProhibAtomicReferenceSlice : CustomAtomicReferenceSlice
	{
		/// <summary />
		public AdhocCoProhibAtomicReferenceSlice()
			: base(new AdhocCoProhibAtomicLauncher())
		{
		}

		/// <summary>
		/// Override method to add suitable control.
		/// </summary>
		public override void FinishInit()
		{
			Debug.Assert(Cache != null);

			base.FinishInit();

			//We need to set the Font so the height of this slice will be
			//set appropriately to fit the text.
			IVwStylesheet stylesheet = FwUtils.StyleSheetFromPropertyTable(PropertyTable);
			var fontHeight = FontHeightAdjuster.GetFontHeightForStyle("Normal", stylesheet, Cache.DefaultVernWs, Cache.LanguageWritingSystemFactoryAccessor);
			Font = new Font(Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.DefaultFontName, fontHeight / 1000f);
		}
	}
}
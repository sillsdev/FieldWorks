// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Drawing;
using LanguageExplorer.Controls.DetailControls;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Areas.Grammar.Tools.AdhocCoprohibEdit
{
	/// <summary>
	/// Summary description for AdhocCoProhibAtomicReferenceSlice.
	/// </summary>
	internal class AdhocCoProhibAtomicReferenceSlice : CustomAtomicReferenceSlice
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AdhocCoProhibAtomicReferenceSlice"/> class.
		/// </summary>
		public AdhocCoProhibAtomicReferenceSlice()
			: base(new AdhocCoProhibAtomicLauncher())
		{
		}

		/// <summary>
		/// Override method to add suitable control.
		/// </summary>
		public override void FinishInit()
		{
			CheckDisposed();
			Debug.Assert(m_cache != null);

			base.FinishInit();

			//We need to set the Font so the height of this slice will be
			//set appropriately to fit the text.
			IVwStylesheet stylesheet = FontHeightAdjuster.StyleSheetFromPropertyTable(PropertyTable);
			var fontHeight = FontHeightAdjuster.GetFontHeightForStyle("Normal", stylesheet, m_cache.DefaultVernWs, m_cache.LanguageWritingSystemFactoryAccessor);
			Font = new Font(m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.DefaultFontName, fontHeight / 1000f);
		}
	}
}
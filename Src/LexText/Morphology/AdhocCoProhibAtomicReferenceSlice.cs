// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Drawing;

using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Common.COMInterfaces;
using System.Diagnostics.CodeAnalysis;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// Summary description for AdhocCoProhibAtomicReferenceSlice.
	/// </summary>
	public class AdhocCoProhibAtomicReferenceSlice : CustomAtomicReferenceSlice
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AdhocCoProhibAtomicReferenceSlice"/> class.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "AdhocCoProhibAtomicLauncher gets added to panel's Controls collection and disposed there")]
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
			IVwStylesheet stylesheet = FontHeightAdjuster.StyleSheetFromMediator(Mediator);
			int fontHeight = FontHeightAdjuster.GetFontHeightForStyle(
					"Normal", stylesheet,
					m_cache.DefaultVernWs, m_cache.LanguageWritingSystemFactoryAccessor);
			Font = new Font(m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.DefaultFontName,
				fontHeight / 1000f);
		}
	}

	public class AdhocCoProhibAtomicReferenceDisabledSlice : AdhocCoProhibAtomicReferenceSlice
	{
		public AdhocCoProhibAtomicReferenceDisabledSlice()
			: base()
		{
		}
		public override void FinishInit()
		{
			CheckDisposed();
			base.FinishInit();
			var arl = (AtomicReferenceLauncher)Control;
			var view = (AtomicReferenceView)arl.MainControl;
			view.FinishInit(ConfigurationNode);
		}
	}

}

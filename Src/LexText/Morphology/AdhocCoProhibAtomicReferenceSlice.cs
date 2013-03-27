using System.Diagnostics;
using System.Drawing;

using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Common.COMInterfaces;

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

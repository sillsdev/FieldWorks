using SIL.FieldWorks.Common.Framework.DetailControls;
using System.Diagnostics.CodeAnalysis;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// Summary description for AdhocCoProhibVectorReferenceSlice.
	/// </summary>
	public class AdhocCoProhibVectorReferenceSlice : CustomReferenceVectorSlice
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AdhocCoProhibVectorReferenceSlice"/> class.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "AdhocCoProhibVectorLauncher gets added to panel's Controls collection and disposed there")]
		public AdhocCoProhibVectorReferenceSlice()
			: base(new AdhocCoProhibVectorLauncher())
		{
		}
	}
	public class AdhocCoProhibVectorReferenceDisabledSlice : AdhocCoProhibVectorReferenceSlice
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

// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.Framework.DetailControls;
using System.Diagnostics.CodeAnalysis;

namespace LanguageExplorer.Areas.Grammar.Tools.AdhocCoprohibEdit
{
#if RANDYTODO
	// TODO: 1. Split out AdhocCoProhibAtomicReferenceDisabledSlice into its own file after move.
#endif
	/// <summary>
	/// Summary description for AdhocCoProhibVectorReferenceSlice.
	/// </summary>
	internal class AdhocCoProhibVectorReferenceSlice : CustomReferenceVectorSlice
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

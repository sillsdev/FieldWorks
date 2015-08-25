using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Framework.DetailControls;
using System.Diagnostics.CodeAnalysis;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// </summary>
	public class LexReferenceTreeBranchesSlice : CustomReferenceVectorSlice, ILexReferenceSlice
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LexReferenceTreeBranchesSlice"/> class.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "LexReferenceTreeBranchesLauncher gets added to panel's Controls collection and disposed there")]
		public LexReferenceTreeBranchesSlice()
			: base(new LexReferenceTreeBranchesLauncher())
		{
		}

		#region ILexReferenceSlice Members

#if RANDYTODO
		public override bool HandleDeleteCommand(Command cmd)
		{
			CheckDisposed();
			((LexReferenceMultiSlice)m_parentSlice).DeleteReference(GetObjectForMenusToOperateOn() as ILexReference);
			return true; // delete was done
		}
#endif

		public override void HandleLaunchChooser()
		{
			CheckDisposed();
			((LexReferenceTreeBranchesLauncher)Control).LaunchChooser();
		}

		public override void HandleEditCommand()
		{
			CheckDisposed();
			((LexReferenceMultiSlice)m_parentSlice).EditReferenceDetails(GetObjectForMenusToOperateOn() as ILexReference);
		}
		#endregion
	}
}

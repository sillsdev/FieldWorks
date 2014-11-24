using SIL.FieldWorks.Common.Framework.DetailControls;
using System.Diagnostics.CodeAnalysis;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	///
	/// </summary>
	public class RecordReferenceVectorSlice : CustomReferenceVectorSlice
	{
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "RecordReferenceVectorLauncher gets added to panel's Controls collection and disposed there")]
		public RecordReferenceVectorSlice()
			: base(new RecordReferenceVectorLauncher())
		{
		}
	}
}

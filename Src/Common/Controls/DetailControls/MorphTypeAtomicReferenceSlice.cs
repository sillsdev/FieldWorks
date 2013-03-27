using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Summary description for MorphTypeAtomicReferenceSlice.
	/// </summary>
	public class MorphTypeAtomicReferenceSlice : PossibilityAtomicReferenceSlice
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MorphTypeAtomicReferenceSlice"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="obj">The obj.</param>
		/// <param name="flid">The flid.</param>
		public MorphTypeAtomicReferenceSlice(FdoCache cache, ICmObject obj, int flid)
			: base(new MorphTypeAtomicLauncher(), cache, obj, flid)
		{
		}
	}
}

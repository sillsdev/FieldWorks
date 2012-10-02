using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Summary description for MorphTypeAtomicReferenceSlice.
	/// </summary>
	public class MorphTypeAtomicReferenceSlice : AtomicReferenceSlice
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

		/// <summary>
		/// Override method to add suitable control.
		/// </summary>
		public override void FinishInit()
		{
			CheckDisposed();
			// REVIEW (DamienD): do we need to do this?
			SetFieldFromConfig();
			base.FinishInit();
		}
	}
}

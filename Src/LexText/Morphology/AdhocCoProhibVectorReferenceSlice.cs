using SIL.FieldWorks.Common.Framework.DetailControls;

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
		public AdhocCoProhibVectorReferenceSlice()
			: base(new AdhocCoProhibVectorLauncher())
		{
		}
	}
}

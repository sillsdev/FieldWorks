using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.FDO;
using XCore;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// LexReferenceTreeRootSlice is used to support selecting
	/// of a Sense or Entry tree.
	/// </summary>
	public class LexReferenceTreeRootSlice : CustomAtomicReferenceSlice, ILexReferenceSlice
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LexReferenceTreeRootSlice"/> class.
		/// </summary>
		public LexReferenceTreeRootSlice()
			: base(new LexReferenceTreeRootLauncher())
		{
		}

		#region ILexReferenceSlice Members

		public override bool HandleDeleteCommand(Command cmd)
		{
			CheckDisposed();
			((LexReferenceMultiSlice)m_parentSlice).DeleteFromReference(GetObjectForMenusToOperateOn() as ILexReference);
			return true; // delete was done
		}

		public override void HandleLaunchChooser()
		{
			CheckDisposed();
			((LexReferenceTreeRootLauncher)Control).LaunchChooser();
		}

		public override void HandleEditCommand()
		{
			CheckDisposed();
			((LexReferenceMultiSlice)m_parentSlice).EditReferenceDetails(GetObjectForMenusToOperateOn() as ILexReference);
		}

		#endregion
	}
}

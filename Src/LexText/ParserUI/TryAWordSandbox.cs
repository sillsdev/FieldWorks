using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.IText;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// The 'TryAWordSandbox' is an IText Sandbox that is used within the Try A word dialog.
	/// </summary>
	public class TryAWordSandbox : SandboxBase
	{
		#region Data members

		#endregion Data members

		#region Construction and initialization

		/// <summary>
		/// Create a new one.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="propertyTable"></param>
		/// <param name="ss"></param>
		/// <param name="choices"></param>
		/// <param name="analysis"></param>
		/// <param name="mediator"></param>
		public TryAWordSandbox(FdoCache cache, Mediator mediator, IPropertyTable propertyTable, IVwStylesheet ss, InterlinLineChoices choices,
			IAnalysis analysis)
			: base(cache, mediator, propertyTable, ss, choices)
		{
			SizeToContent = true;
			LoadForWordBundleAnalysis(analysis.Hvo);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			base.Dispose( disposing );

			if (disposing)
			{
			}

		}

		#endregion Construction and initialization

	}
}

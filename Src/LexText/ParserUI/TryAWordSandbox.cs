using System;
using System.ComponentModel;
using System.Collections;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
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
		/// Default Constructor.
		/// </summary>
		public TryAWordSandbox()
		{
		}

		/// <summary>
		/// Create a new one.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="ss"></param>
		/// <param name="choices"></param>
		/// <param name="hvoWordform"></param>
		/// <param name="mediator"></param>
		public TryAWordSandbox(FdoCache cache, Mediator mediator, IVwStylesheet ss, InterlinLineChoices choices,
			int hvoWordform)
			: base(cache, mediator, ss, choices)
		{
			SizeToContent = true;
			//RawWordform = word;
			base.LoadForWordBundleAnalysis(hvoWordform);
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

using System;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;
using SIL.FieldWorks.Common.Framework.DetailControls;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// A tree control item where the embedded form is a View (specifically
	/// SIL.FieldWorks.Common.Framework.RootSite).
	/// </summary>
	public class InterlinearSlice : ViewSlice
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public InterlinearSlice()
		{
		}

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// <summary>
		/// Therefore this method, called once we have a cache and object, is our first chance to
		/// actually create the embedded control.
		/// </summary>
		public override void FinishInit()
		{
			CheckDisposed();
			AnalysisInterlinearRS ctrl = new AnalysisInterlinearRS(m_cache, Object.Hvo, ConfigurationNode, StringTbl);
			ctrl.Mediator = Mediator;
			Control = ctrl;
			//if (ctrl.RootBox == null)
			//    ctrl.MakeRoot();
		}

		/// <summary>
		/// Override to give the height that the AnalysisInterlinearRS wants to be.
		/// </summary>
		/// <param name="rs"></param>
		/// <returns></returns>
		protected override int DesiredHeight(RootSite rs)
		{
			return (rs as AnalysisInterlinearRS).DesiredSize.Height;
		}

		protected override void SetWidthForDataTreeLayout(int width)
		{
			int minWidth = (Control as AnalysisInterlinearRS).DesiredSize.Width + SplitCont.SplitterDistance + SplitCont.SplitterWidth;
			if (width < minWidth)
				width = minWidth;
			base.SetWidthForDataTreeLayout(width);
		}
	}
}

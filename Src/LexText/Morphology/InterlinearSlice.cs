using System.Diagnostics;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// A tree control item where the embedded form is a View (specifically
	/// SIL.FieldWorks.Common.Framework.RootSite).
	/// </summary>
	public class InterlinearSlice : ViewSlice
	{
		public InterlinearSlice()
		{
			Debug.WriteLine("Created.");
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
			var ctrl = new AnalysisInterlinearRs(m_cache, (IWfiAnalysis) Object, ConfigurationNode);
			ctrl.InitializeFlexComponent(PropertyTable, Publisher, Subscriber);
			Control = ctrl;
		}

		/// <summary>
		/// Override to give the height that the AnalysisInterlinearRs wants to be.
		/// </summary>
		/// <param name="rs"></param>
		/// <returns></returns>
		protected override int DesiredHeight(RootSite rs)
		{
			return ((AnalysisInterlinearRs)rs).DesiredSize.Height;
		}

		protected override void SetWidthForDataTreeLayout(int width)
		{
			var minWidth = ((AnalysisInterlinearRs)Control).DesiredSize.Width + SplitCont.SplitterDistance + SplitCont.SplitterWidth;
			if (width < minWidth)
				width = minWidth;
			base.SetWidthForDataTreeLayout(width);
		}
	}
}

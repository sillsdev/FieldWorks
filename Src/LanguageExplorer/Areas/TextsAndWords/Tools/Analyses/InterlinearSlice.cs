// Copyright (c) 2006-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using LanguageExplorer.Controls.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;

namespace LanguageExplorer.Areas.TextsAndWords.Tools.Analyses
{
	/// <summary>
	/// A tree control item where the embedded form is a View (specifically
	/// SIL.FieldWorks.Common.Framework.RootSite).
	/// </summary>
	internal sealed class InterlinearSlice : ViewSlice
	{
		private ISharedEventHandlers _sharedEventHandlers;

		public InterlinearSlice(ISharedEventHandlers sharedEventHandlers)
		{
			_sharedEventHandlers = sharedEventHandlers;
			Debug.WriteLine("Created.");
		}

		#region IDisposable override

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

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
			var ctrl = new AnalysisInterlinearRs(_sharedEventHandlers, Cache, (IWfiAnalysis)MyCmObject, ConfigurationNode);
			ctrl.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
			Control = ctrl;
		}

		/// <summary>
		/// Override to give the height that the AnalysisInterlinearRs wants to be.
		/// </summary>
		protected override int DesiredHeight(RootSite rs)
		{
			return ((AnalysisInterlinearRs)rs).DesiredSize.Height;
		}

		protected internal override void SetWidthForDataTreeLayout(int width)
		{
			var minWidth = ((AnalysisInterlinearRs)Control).DesiredSize.Width + SplitCont.SplitterDistance + SplitCont.SplitterWidth;
			if (width < minWidth)
			{
				width = minWidth;
			}
			base.SetWidthForDataTreeLayout(width);
		}
	}
}
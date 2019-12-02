// Copyright (c) 2007-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Areas.TextsAndWords
{
	/// <summary>
	/// The 'TryAWordSandbox' is an IText Sandbox that is used within the Try A word dialog.
	/// </summary>
	internal sealed class TryAWordSandbox : SandboxBase
	{
		#region Construction and initialization

		/// <summary>
		/// Create a new one.
		/// </summary>
		public TryAWordSandbox(LcmCache cache, IVwStylesheet ss, InterlinLineChoices choices, IAnalysis analysis)
			: base(cache, ss, choices)
		{
			SizeToContent = true;
			LoadForWordBundleAnalysis(analysis.Hvo);
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			base.Dispose(disposing);

			if (disposing)
			{
			}

		}

		#endregion Construction and initialization
	}
}
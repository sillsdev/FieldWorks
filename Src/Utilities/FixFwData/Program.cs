// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Program.cs
// Responsibility: FLEx team
// ---------------------------------------------------------------------------------------------
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using Palaso.Reporting;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FixData;
using Palaso.UI.WindowsForms.HotSpot;

namespace FixFwData
{
	class Program
	{
		[SuppressMessage("Gendarme.Rules.Portability", "ExitCodeIsLimitedOnUnixRule",
			Justification = "Appears to be a bug in Gendarme...not recognizing that 0 and 1 are in correct range (0..255)")]
		private static int Main(string[] args)
		{
			SetUpErrorHandling();
			var pathname = args[0];
			using (var prog = new NullProgress())
			{
				var data = new FwDataFixer(pathname, prog, logger);
				data.FixErrorsAndSave();
			}
			if (errorsOccurred)
				return 1;
			return 0;
		}

		private static bool errorsOccurred;

		private static void logger(string guid, string date, string description)
		{
			Console.WriteLine(description);
			errorsOccurred = true;
		}

		private static void SetUpErrorHandling()
		{
			using (new HotSpotProvider())
			{
				ErrorReport.EmailAddress = "flex_errors@sil.org";
				ErrorReport.AddStandardProperties();
				ExceptionHandler.Init();
			}
		}

		private sealed class NullProgress : IProgress, IDisposable
		{
			public event CancelEventHandler Canceling;

			public void Step(int amount)
			{
				if (Canceling != null)
				{
					// don't do anything -- this just shuts up the compiler about the
					// event handler never being used.
				}
			}

			public string Title { get; set; }

			public string Message
			{
				get { return null; }
				set { Console.Out.WriteLine(value); }
			}

			public int Position { get; set; }
			public int StepSize { get; set; }
			public int Minimum { get; set; }
			public int Maximum { get; set; }
			public ISynchronizeInvoke SynchronizeInvoke { get; private set; }
			public Form Form { get; private set; }
			public bool IsIndeterminate
			{
				get { return false; }
				set { }
			}

			public bool AllowCancel
			{
				get { return false; }
				set { }
			}
			#region Gendarme required cruft
#if DEBUG
			/// <summary/>
			~NullProgress()
			{
				Dispose(false);
			}
#endif

			/// <summary/>
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			/// <summary/>
			private void Dispose(bool fDisposing)
			{
				System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + ". *******");

				if (Form != null && !Form.IsDisposed)
				{
					Form.Dispose();
				}
			}
			#endregion
		}
	}
}

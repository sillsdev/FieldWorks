// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using SIL.LCModel.FixData;
using SIL.LCModel.Utils;
using SIL.Reporting;
using SIL.Windows.Forms.HotSpot;
using SIL.Windows.Forms.Reporting;

namespace FixFwData
{
	internal static class Program
	{
		private static int Main(string[] args)
		{
			SetUpErrorHandling();
			var data = new FwDataFixer(args[0], new NullProgress(), Logger, Counter);
			data.FixErrorsAndSave();
			return s_errorsOccurred ? 1 : 0;
		}

		private static bool s_errorsOccurred;
		private static int s_errorCount;

		private static void Logger(string description, bool errorFixed)
		{
			Console.WriteLine(description);
			s_errorsOccurred = true;
			if (errorFixed)
			{
				++s_errorCount;
			}
		}

		private static int Counter()
		{
			return s_errorCount;
		}

		private static void SetUpErrorHandling()
		{
			using (new HotSpotProvider())
			{
				ErrorReport.EmailAddress = "flex_errors@sil.org";
				ErrorReport.AddStandardProperties();
				ExceptionHandler.Init(new WinFormsExceptionHandler());
			}
		}

		private sealed class NullProgress : IProgress
		{
			public event CancelEventHandler Canceling;

			void IProgress.Step(int amount)
			{
				if (Canceling != null)
				{
					// don't do anything -- this just shuts up the compiler about the
					// event handler never being used.
				}
			}

			string IProgress.Title { get; set; }

			string IProgress.Message
			{
				get => null;
				set => Console.Out.WriteLine(value);
			}

			int IProgress.Position { get; set; }
			int IProgress.StepSize { get; set; }
			int IProgress.Minimum { get; set; }
			int IProgress.Maximum { get; set; }
			ISynchronizeInvoke IProgress.SynchronizeInvoke { get; }

			bool IProgress.IsIndeterminate
			{
				get => false;
				set { }
			}

			bool IProgress.AllowCancel
			{
				get => false;
				set { }
			}
		}
	}
}

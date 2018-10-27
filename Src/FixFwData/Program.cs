// Copyright (c) 2011-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using SIL.LCModel.FixData;
using SIL.LCModel.Utils;
using SIL.Reporting;
using SIL.Windows.Forms.HotSpot;

namespace FixFwData
{
	internal class Program
	{
		private static int Main(string[] args)
		{
			SetUpErrorHandling();
			var data = new FwDataFixer(args[0], new NullProgress(), logger, counter);
			data.FixErrorsAndSave();
			return errorsOccurred ? 1 : 0;
		}

		private static bool errorsOccurred;
		private static int errorCount;

		private static void logger(string description, bool errorFixed)
		{
			Console.WriteLine(description);
			errorsOccurred = true;
			if (errorFixed)
			{
				++errorCount;
			}
		}

		private static int counter()
		{
			return errorCount;
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

		private sealed class NullProgress : IProgress
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
		}
	}
}

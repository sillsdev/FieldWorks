// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using NUnit.Framework;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Covers the upload log's behavior when the log file itself cannot be written.
	/// </summary>
	[TestFixture]
	public class WebonaryUploadLogTests
	{
		[Test]
		public void WaitForLogEntriesDoesNotThrowWhenTheLogFileIsLocked()
		{
			var directory = Path.Combine(Path.GetTempPath(), "webonary-log-tests", Path.GetRandomFileName());
			Directory.CreateDirectory(directory);
			var logFilePath = Path.Combine(directory, "last-upload.log");
			try
			{
				// An exclusive handle makes the log's append fail, faulting the write task.
				using (File.Open(logFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					var log = new WebonaryUploadLog(logFilePath);
					log.AddEntry(WebonaryStatusCondition.Error, "an entry that cannot be written");
					//SUT
					Assert.DoesNotThrow(() => log.WaitForLogEntries());
					Assert.DoesNotThrow(() => log.WaitForLogEntries(),
						"A faulted write should not resurface on a later flush.");
				}
			}
			finally
			{
				try
				{
					Directory.Delete(directory, true);
				}
				catch (IOException)
				{
				}
			}
		}
	}
}

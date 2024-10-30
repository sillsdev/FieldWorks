// // Copyright (c) $year$ SIL International
// // This software is licensed under the LGPL, version 2.1 or later
// // (http://www.gnu.org/licenses/lgpl-2.1.html)

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SIL.FieldWorks.XWorks
{
	public class WebonaryUploadLog
	{
		private List<Task> logTasks = new List<Task>();
		private readonly string logFilePath;
		private readonly object lockObj = new object();

		public class UploadLogEntry
		{
			public DateTime Timestamp { get; }
			public WebonaryStatusCondition Status { get; }
			public string Message { get; }

			public UploadLogEntry(WebonaryStatusCondition status, string message)
			{
				Timestamp = DateTime.UtcNow;
				Status = status;
				Message = message;
			}
		}

		public WebonaryUploadLog(string logFilePath)
		{
			if (string.IsNullOrEmpty(logFilePath))
				throw new ArgumentException(nameof(logFilePath));
			Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));
			this.logFilePath = logFilePath;
		}

		public void AddEntry(WebonaryStatusCondition uploadStatus, string statusString)
		{
			logTasks.Add(LogAsync(uploadStatus, statusString));
		}

		public Task LogAsync(WebonaryStatusCondition level, string message)
		{
			var logEntry = new UploadLogEntry(level,  message);
			string jsonLogEntry;
			using (var sw = new StringWriter())
			{
				JsonSerializer.Create().Serialize(sw, logEntry);
				jsonLogEntry = sw.ToString();
			}

			// Append log asynchronously to file without blocking the main thread
			var logTask = Task.Run(() =>
			{
				lock (lockObj) // Ensure that only one thread writes to the file at a time
				{
					// Append to the log file
					using (var writer = new StreamWriter(logFilePath, append: true))
					{
						writer.WriteLine(jsonLogEntry); // Write the serialized log entry
					}
				}
			});

			// Add the log task to the task list for tracking
			return logTask;
		}

		public void WaitForLogEntries()
		{
			Task.WaitAll(logTasks.ToArray());
		}
	}
}
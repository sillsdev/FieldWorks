// Copyright (c) 2012-2021 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using SIL.Progress;
using SIL.Reporting;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// This class downloads files. It is adapted from Paratext's RESTClient.
	/// REVIEW (Hasso) 2021.07: is there any reason to inherit System.Net.WebClient? WebonaryClient does but RESTClient does not
	/// </summary>
	public class DownloadClient
	{
		public IProgress Progress { get; }

		private bool m_CancelRequested;

		private bool CancelRequested => m_CancelRequested || Progress.CancelRequested;

		private int m_SequenceCount;

		public  int MaxRetryAttempts { get; set; } = 25;

		public DownloadClient(IProgress progress = null)
		{
			Progress = progress ?? new NullProgress();
		}

		public void RequestCancel()
		{
			m_CancelRequested = true;
		}

		public bool DownloadFile(string url, string outputFile)
		{
			var outputDir = Path.GetDirectoryName(outputFile);
			if (!Directory.Exists(outputDir))
			{
				// ReSharper disable once AssignNullToNotNullAttribute
				Directory.CreateDirectory(outputDir);
			}
			var outputStream = new FileStream(outputFile, FileMode.Append);
			try
			{
				return GetStreamingInternalWrapped(new Uri(url), outputStream);
			}
			finally
			{
				outputStream.Flush();
				outputStream.Close();
			}
		}

		private bool GetStreamingInternalWrapped(Uri url, Stream outputStream)
		{
			var offset = outputStream.Position;
			const int chunkSize = 20 * 1024;

			var sequenceCount = m_SequenceCount++;
			var sw = Stopwatch.StartNew();
			Logger.WriteEvent($"Downloading {url}");

			Progress.ProgressIndicator.PercentCompleted = 0;
			var curDownloadSize = 0L;

			// Repeat until all is received
			var remainingRetries = MaxRetryAttempts;
			Exception lastException = null;
			do
			{
				//cancel requested - end download
				if (CancelRequested)
				{
					Cancel();
					return false;
				}

				var req = (HttpWebRequest)WebRequest.Create(url);
				req.Method = "GET";
				req.KeepAlive = true;

				// If partially downloaded, add range
				if (outputStream.Position > 0)
				{
					req.AddRange(offset, int.MaxValue);
					Logger.WriteMinorEvent("GetStreaming recall   [{0}] ({1} ms): offset = {2}, length = {3}",
						sequenceCount, sw.ElapsedMilliseconds, offset, outputStream.Position);
				}
				try
				{
					using (var res = req.GetResponse())
					using (var s = res.GetResponseStream())
					{
						// Create data array
						if (res.ContentLength == 0)
							return true;

						if (curDownloadSize == 0)
						{
							curDownloadSize = offset + res.ContentLength;
							Progress.ProgressIndicator.PercentCompleted = (int)(offset * 100L / curDownloadSize);
						}

						int read; // Bytes read in current read
						long lastLogEntryOffset = 0;
						var data = new byte[chunkSize];
						do
						{
							Progress.WriteStatus("{0}/{1} K", offset / 1024, curDownloadSize / 1024);
							// TODO (Hasso) 2021.07: is it possible that the request has no ContentLength but has content?
							read = s.Read(data, 0, (int)Math.Min(curDownloadSize - offset, chunkSize));
							outputStream.Write(data, 0, read);
							offset += read;
							if (lastLogEntryOffset == 0 || (offset - lastLogEntryOffset) > 200000)
							{
								Logger.WriteMinorEvent("GetStreaming progress [{0}] ({1} ms): offset = {2}",
									sequenceCount, sw.ElapsedMilliseconds, offset);
								lastLogEntryOffset = offset;
							}

							// Continue until end of stream or cancelled
							Progress.ProgressIndicator.PercentCompleted = (int)(offset * 100L / curDownloadSize);
						}
						while (read > 0 && offset < curDownloadSize && !CancelRequested);

						//if the user cancelled the download return false
						if (CancelRequested)
						{
							Cancel();
							return false;
						}

						// If all read
						if (offset == curDownloadSize)
						{
							Progress.ProgressIndicator.PercentCompleted = 100;
							Logger.WriteMinorEvent("GetStreaming result   [{0}] ({1} ms): length = {2}", sequenceCount, sw.ElapsedMilliseconds, offset);
							return true;
						}
					}
				}
				catch (WebException wex)
				{
					lastException = wex;
					if (wex.Status == WebExceptionStatus.ProtocolError)
					{
						Logger.WriteError("DownloadClient.GetStreamingInternal exception", wex);
						throw;
					}
					Logger.WriteError("GetStreaming failed with {0}", wex);
				}
				catch (IOException ioe)
				{
					lastException = ioe;
					Logger.WriteError("GetStreaming failed with {0}", ioe);
				}

				// decided to put a limit on retries in case this is a permanent server error
				if (--remainingRetries < 0)
					throw new ApplicationException("Too many retry attempts during get", lastException);

				// TODO (Hasso) 2021.07: localize
				Progress.WriteMessage("Connection to server lost. Retrying...");

				// Delay slightly before retrying
				Thread.Sleep(2000);
			} while (true);
		}

		private void Cancel()
		{
			Progress.ProgressIndicator.Finish();
			Progress.ProgressIndicator.PercentCompleted = 0;
			Progress.CancelRequested = m_CancelRequested = false;
		}
	}
}
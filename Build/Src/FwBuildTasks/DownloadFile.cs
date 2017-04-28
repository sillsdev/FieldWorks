using System;
using System.IO;
using System.Net;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace FwBuildTasks
{
	/// <summary>
	/// Downloads a file from a web address. Params specify the web address, the local path for the file,
	/// and optionally a user and password. The user/password feature has not been tested.
	/// If using an important password, make sure the address is https, since I think otherwise the password
	/// may be sent in clear.
	/// Adapted from http://stackoverflow.com/questions/1089452/how-can-i-use-msbuild-to-download-a-file
	/// </summary>
	public class DownloadFile : Task
	{
		/// <summary>times to retry failed downloads</summary>
		protected const int Retries = 3;

		/// <summary>time to wait before retrying failed downloads (one minute = 60,000ms)</summary>
		protected const int RetryWaitTime = 60000;

		/// <summary>HTTP address to download from</summary>
		[Required]
		public string Address
		{ get; set; }

		/// <summary>Full path to the local file to which the downloaded file will be saved</summary>
		[Required]
		public string LocalFilename
		{ get; set; }

		/// <summary>Credential for HTTP authentication</summary>
		public string Username
		{ get; set; }

		/// <summary>Credential for HTTP authentication</summary>
		public string Password
		{ get; set; }

		public override bool Execute()
		{
			return ProcessDownloadFile(Address, LocalFilename);
		}

		public static string GetFilenameFromUrl(string url)
		{
			var fileName = Path.GetFileName(url) ?? string.Empty;
			var iq = fileName.IndexOf('?');
			if (iq > 0)
				fileName = fileName.Substring(0, iq); // trim any trailing query
			return fileName;
		}

		/// <summary>
		/// Downloads the file from remoteUrl to localPathname, using the member-property username and password, if any.
		/// On failure, tries again <c>Retries</c> times.
		/// Returns true if the file was downloaded successfully or already existed.
		/// </summary>
		public bool ProcessDownloadFile(string remoteUrl, string localPathname)
		{
			for (var retries = Retries; retries >= 0; --retries)
			{
				// This doesn't seem to work reliably..can return true even when only network cable is unplugged.
				// Left in in case it works in some cases. But the main way of dealing with disconnect is the
				// same algorithm in the WebException handler.
				if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
				{
					if (File.Exists(localPathname))
					{
						Log.LogWarning("Could not retrieve latest {0}. No network connection. Keeping existing file.", remoteUrl);
						return true; // don't stop or delay the build
					}
					if (retries > 0)
					{
						Log.LogMessage(MessageImportance.High, "Could not retrieve latest {0}. No network connection. Trying {1} more times.", remoteUrl, retries);
						Thread.Sleep(RetryWaitTime); // wait a moment before trying again
					}
					continue;
				}

				bool success;
				var read = DoDownloadFile(remoteUrl, localPathname, Username, Password, out success);

				if (success)
				{
					if (read == 0)
						Log.LogMessage(MessageImportance.Normal, "The local file {0} is up-to-date with {1}.", localPathname, remoteUrl);
					else
						Log.LogMessage(MessageImportance.Low, "{0} bytes written to {1} from {2}", read, localPathname, remoteUrl);
					return true;
				}

				if (File.Exists(localPathname))
				{
					Log.LogWarning("Could not download {0}; using local file.", remoteUrl);
					return true; // don't stop or delay the build
				}
				if (retries > 0)
				{
					Log.LogMessage(MessageImportance.High, "Could not download {0}. Trying {1} more times.", remoteUrl, retries);
					Thread.Sleep(RetryWaitTime); // wait a moment before trying again
				}
			}

			Log.LogError("Could not retrieve latest {0}. Exceeded retry count.", remoteUrl);
			return false; // The build can't continue without the local file
		}

		/// <summary>
		/// Downloads the file from remoteFilename to localFilename, using the specified username and password, if any.
		/// Does not throw or log errors (only warns). Retrying or marking an utter failure is the client's responsibility.
		/// Returns the number of bytes processed (0 if the file was up-to-date).
		/// </summary>
		public int DoDownloadFile(string remoteFilename, string localFilename, string httpUsername, string httpPassword, out bool success)
		{
			// Function will return the number of bytes processed to the caller. Initialize to 0 here.
			var bytesProcessed = 0;
			success = false; // The build can't continue until the file has been downloaded

			// Assign values to these objects here so that they can
			// be referenced in the finally block
			Stream remoteStream = null;
			Stream localStream = null;
			HttpWebResponse response = null;
			var lastModified = DateTime.Now;

			// Use a try/catch/finally block as both the WebRequest and Stream classes throw exceptions upon error
			try
			{
				// Create a request for the specified remote file name
				var request = WebRequest.Create(remoteFilename);
				// If a username or password have been given, use them
				if (!string.IsNullOrEmpty(httpUsername) || !string.IsNullOrEmpty(httpPassword))
				{
					var username = httpUsername;
					var password = httpPassword;
					request.Credentials = new NetworkCredential(username, password);
				}

				// Prevent caching of requests so that we always download latest
				request.Headers[HttpRequestHeader.CacheControl] = "no-cache";

				// Send the request to the server and retrieve the WebResponse object
				response = (HttpWebResponse) request.GetResponse();
				if (response.StatusCode == HttpStatusCode.OK)
				{
					lastModified = response.LastModified;
					if (File.Exists(localFilename) && lastModified == File.GetLastWriteTime(localFilename))
					{
						success = true;
						return 0;
					}

					// Once the WebResponse object has been retrieved, get the stream object associated with the response's data
					remoteStream = response.GetResponseStream();
					if (remoteStream == null)
						return 0;

					// Create the local file
					localStream = File.Create(localFilename);

					// Allocate a 1k buffer
					var buffer = new byte[1024];
					int bytesRead;
					// Simple do/while loop to read from stream until no bytes are returned
					do
					{
						// Read data (up to 1k) from the stream
						bytesRead = remoteStream.Read(buffer, 0, buffer.Length);

						// Write the data to the local file
						localStream.Write(buffer, 0, bytesRead);

						// Increment total bytes processed
						bytesProcessed += bytesRead;
					} while (bytesRead > 0);
					if (bytesProcessed == 0)
						Log.LogWarning("No data received from {0}", remoteFilename);
					else
						success = true; // The file has been downloaded; the build can continue
				}
				else
				{
					Log.LogWarning("Unexpected Server Response[{0}] when downloading {1}", response.StatusCode, Path.GetFileName(localFilename));
				}
			}
			catch (WebException wex)
			{
				if (wex.Status == WebExceptionStatus.ConnectFailure || wex.Status == WebExceptionStatus.NameResolutionFailure)
				{
					// We probably don't have a network connection (despite the check in the caller).
					Log.LogWarning("Could not retrieve latest {0}. No network connection.", remoteFilename);
					return 0;
				}
				string html = null;
				if (wex.Response != null)
					using (var errStream = wex.Response.GetResponseStream())
						if(errStream != null)
							using (var sr = new StreamReader(errStream))
								html = sr.ReadToEnd();
				if (html != null)
					Log.LogWarning("Could not download from {0}. Server responds {1}", remoteFilename, html);
				else
					Log.LogWarning("Could not download from {0}. Exception {1}. Status {2}", remoteFilename, wex.Message, wex.Status);
				return 0;
			}
			catch (Exception e)
			{
				Log.LogError(e.Message);
				Log.LogMessage(MessageImportance.Normal, e.StackTrace);
			}
			finally
			{
				// Close the response and streams objects here to make sure they're closed even if an exception is thrown at some point
				if (response != null) response.Close();
				if (remoteStream != null) remoteStream.Close();
				if (localStream != null)
				{
					localStream.Close();
					// ReSharper disable once ObjectCreationAsStatement
					// Justification: all we need to do with the new FileInfo is set LastWriteTime, which is saved immediately
					new FileInfo(localFilename) { LastWriteTime = lastModified };
				}
			}

			// Return total bytes processed to caller.
			return bytesProcessed;
		}
	}
}

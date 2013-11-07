using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace SIL.FieldWorks.Build.Tasks
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
		[Required]
		public String Address // HTTP address to download from
		{ get; set; }

		[Required]
		public String LocalFilename // Local file to which the downloaded file will be saved
		{ get; set; }

		public String Username // Credential for HTTP authentication
		{ get; set; }

		public String Password // Credential for HTTP authentication
		{ get; set; }

		public override bool Execute()
		{
			// This doesn't seem to work reliably..can return true even when only network cable is unplugged.
			// Left in in case it works in some cases. But the main way of dealing with disconnect is the
			// same algorithm in the WebException handler.
			if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
			{
				if (File.Exists(LocalFilename))
				{
					Log.LogWarning("Could not retrieve latest {0}. No network connection. Keeping existing file.", LocalFilename);
					return true; // don't stop the build
				}
				else
				{
					Log.LogError("Could not retrieve latest {0}. No network connection.", Address);
					return false; // Presumably can't continue
				}
			}

			bool success;
			var read = DoDownloadFile(Address, LocalFilename, Username, Password, out success);

			if (success)
				Log.LogMessage(MessageImportance.Low, "{0} bytes written", read);
			else
			{
				if (File.Exists(LocalFilename))
				{
					Log.LogWarning("Could not download {0} using local file.", Address);
					success = true;
				}
				else
				{
					Log.LogError("Could not download {0}", Address);
				}
			}

			return success;
		}

		public int DoDownloadFile(String remoteFilename, String localFilename, String httpUsername, String httpPassword, out bool success)
		{
			// Function will return the number of bytes processed
			// to the caller. Initialize to 0 here.
			int bytesProcessed = 0;
			success = true;

			// Assign values to these objects here so that they can
			// be referenced in the finally block
			Stream remoteStream = null;
			Stream localStream = null;
			WebResponse response = null;

			// Use a try/catch/finally block as both the WebRequest and Stream
			// classes throw exceptions upon error
			try
			{
				// Create a request for the specified remote file name
				WebRequest request = WebRequest.Create(remoteFilename);
				// If a username or password have been given, use them
				if (!string.IsNullOrEmpty(httpUsername) || !string.IsNullOrEmpty(httpPassword))
				{
					string username = httpUsername;
					string password = httpPassword;
					request.Credentials = new NetworkCredential(username, password);
				}

				// Prevent caching of requests so that we always download latest
				var cacheControl = request.Headers[HttpRequestHeader.CacheControl];
				request.Headers[HttpRequestHeader.CacheControl] = "no-cache";

				// Send the request to the server and retrieve the
				// WebResponse object
				response = request.GetResponse();
				// Once the WebResponse object has been retrieved,
				// get the stream object associated with the response's data
				remoteStream = response.GetResponseStream();

				// Create the local file
				localStream = File.Create(localFilename);

				// Allocate a 1k buffer
				var buffer = new byte[1024];
				int bytesRead;

				// Simple do/while loop to read from stream until
				// no bytes are returned
				do
				{
					// Read data (up to 1k) from the stream
					bytesRead = remoteStream.Read(buffer, 0, buffer.Length);

					// Write the data to the local file
					localStream.Write(buffer, 0, bytesRead);

					// Increment total bytes processed
					bytesProcessed += bytesRead;
				} while (bytesRead > 0);
			}
			catch (WebException wex)
			{
				if (wex.Status == WebExceptionStatus.ConnectFailure || wex.Status == WebExceptionStatus.NameResolutionFailure)
				{
					// We probably don't have a network connection (despite the check in the caller).
					if (File.Exists(localFilename))
					{
						Log.LogMessage("Could not retrieve latest {0}. No network connection. Keeping existing file.", localFilename);
					}
					else
					{
						Log.LogError("Could not retrieve latest {0}. No network connection.", remoteFilename);
						success = false; // Presumably can't continue
					}
					return 0;
				}
				string html = "";
				if (wex.Response != null)
				{
					using (var sr = new StreamReader(wex.Response.GetResponseStream()))
						html = sr.ReadToEnd();
					Log.LogMessage("Could not download from {0}. Server responds {1}", remoteFilename, html);
				}
				else
				{
					Log.LogMessage("Could not download from {0}. no server response. Exception {1}. Status {2}",
						remoteFilename, wex.Message, wex.Status);
				}
				success = false;
				return 0;
			}
			catch (Exception e)
			{
				Log.LogError(e.Message);
				Log.LogMessage(MessageImportance.Normal, e.StackTrace);
				success = false;
			}
			finally
			{
				// Close the response and streams objects here
				// to make sure they're closed even if an exception
				// is thrown at some point
				if (response != null) response.Close();
				if (remoteStream != null) remoteStream.Close();
				if (localStream != null) localStream.Close();
			}

			// Return total bytes processed to caller.
			return bytesProcessed;
		}
	}
}

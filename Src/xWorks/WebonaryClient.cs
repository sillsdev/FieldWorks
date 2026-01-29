// Copyright (c) 2016-2021 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System;
using System.IO;
using System.Net;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// This class extends WebClient to provide an infinite timeout on the upload to webonary and to disable AutoRedirect so that an incorrect site
	/// name returns an error instead of sending us a webpage as a response.
	/// </summary>
	public class WebonaryClient : WebClient, IWebonaryClient
	{
		public HttpStatusCode ResponseStatusCode { get; set; }

		protected override WebRequest GetWebRequest(Uri address)
		{
			var request = base.GetWebRequest(address);

			if (request is HttpWebRequest httpRequest)
			{
				httpRequest.Timeout = -1;
				httpRequest.AllowAutoRedirect = false;
			}

			return request;
		}

		/// <summary>
		/// Wraps the UploadFile from WebClient to provide status accessor and allow mocking returns for the unit tests.
		/// </summary>
		/// <exception cref="WebonaryException"></exception>
		public byte[] UploadFileToWebonary(string address, string fileName, string method = null)
		{
			try
			{
				return UploadFile(address, method, fileName);
			}
			catch (WebException ex)
			{
				if(ex.Response == null)
					throw new WebonaryException("WebException with null response stream.", ex);
				using (var stream = ex.Response.GetResponseStream())
				using (var reader = new StreamReader(stream))
				{
					var response = reader.ReadToEnd();
					throw new WebonaryException(response, ex);
				}
			}
		}

		public string PostDictionaryMetadata(string address, string postBody)
		{
			return PostToWebonaryApi(address, postBody);
		}

		public string PostEntry(string address, string postBody, bool isReversal)
		{
			if (isReversal)
			{
				// reversals use the same api as entry but with one extra parameter to indicate the type
				return PostToWebonaryApi(address, postBody, "&entryType=reversalindexentry");
			}
			return PostToWebonaryApi(address, postBody);
		}

		public byte[] DeleteContent(string targetURI)
		{
			try
			{
				return UploadData(targetURI, "DELETE", Encoding.GetBytes(""));
			}
			catch (WebException ex)
			{
				if (ex.Response == null)
					throw new WebonaryException("WebException with null response stream.", ex);
				using (var stream = ex.Response.GetResponseStream())
				using (var reader = new StreamReader(stream))
				{
					var response = reader.ReadToEnd();
					throw new WebonaryException(response, ex);
				}
			}
		}

		public string GetSignedUrl(string address, string filePath)
		{
			dynamic urlBody = new JObject();
			urlBody.objectId = filePath;
			urlBody.action = "putObject";
			return PostToWebonaryApi(address, urlBody.ToString());
		}

		private string PostToWebonaryApi(string address, string postBody, string extraArgs = "")
		{
			const int maxRetries = 4;
			var retryDelay = 500; // Start with half second

			for (var attempt = 1; attempt <= maxRetries; attempt++)
				try
				{
					return UploadString(address + extraArgs, postBody);
				}
				catch (WebException ex)
				{
					if (ex.Response == null)
						throw new WebonaryException("WebException with null response stream.",
							ex);

					var statusCode = ((HttpWebResponse)ex.Response).StatusCode;

					// Only retry on 504 Gateway Timeout or 503 Service Unavailable or 502 Bad Gateway (common for server overload)
					if ((statusCode == HttpStatusCode.GatewayTimeout ||
						 statusCode == HttpStatusCode.ServiceUnavailable ||
						 statusCode == HttpStatusCode.BadGateway) && attempt < maxRetries)
					{
						Thread.Sleep(retryDelay);
						retryDelay *= 2; // Exponential backoff
						continue; // Retry
					}

					// For other errors or final attempt, throw WebonaryException with details
					using (var stream = ex.Response.GetResponseStream())
					using (var reader = new StreamReader(stream))
					{
						var response = reader.ReadToEnd();
						var webonaryEx = new WebonaryException(response, ex);
						// Add retry information to the exception message for better diagnostics
						if (attempt > 1)
							webonaryEx.FullResponse = string.Format("[Retried {0} times] {1}",
								attempt - 1, response);
						throw webonaryEx;
					}
				}

			// Unreachable code, but required to satisfy the compiler
			throw new WebonaryException("Max retries exceeded", null);
		}

		protected override WebResponse GetWebResponse(WebRequest request)
		{
			var response = base.GetWebResponse(request);
			return SetStatusAndReturn((HttpWebResponse)response);
		}


		protected override WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
		{
			var response = base.GetWebResponse(request, result);
			return SetStatusAndReturn((HttpWebResponse)response);
		}

		private WebResponse SetStatusAndReturn(HttpWebResponse response)
		{
			ResponseStatusCode = response.StatusCode;
			return response;
		}

		public class WebonaryException : Exception
		{
			public WebException WebException { get; private set; }
			public HttpStatusCode StatusCode { get; internal set; }
			/// <summary>
			/// The full response returned by the server. Useful for debugging connection issues.
			/// </summary>
			public string FullResponse { get; set; }

			public WebonaryException(WebException webException) : this(null, webException)
			{
			}

			internal WebonaryException(string fullResponse, WebException webException)
			{
				FullResponse = fullResponse;
				WebException = webException;
				if (webException.Response != null)
				{
					StatusCode = ((HttpWebResponse)webException.Response).StatusCode;
				}
			}
		}
	}
}
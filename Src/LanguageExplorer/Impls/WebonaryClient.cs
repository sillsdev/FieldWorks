// Copyright (c) 2016-2022 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;

namespace LanguageExplorer.Impls
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
		public byte[] UploadFileToWebonary(string address, string fileName, string method = null)
		{
			try
			{
				return UploadFile(address, method, fileName);
			}
			catch (WebException ex)
			{
				if (ex.Response == null)
				{
					throw new WebonaryException("WebException with null response stream.", ex);
				}
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
				{
					throw new WebonaryException("WebException with null response stream.", ex);
				}
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
			try
			{
				return UploadString(address + extraArgs, postBody);
			}
			catch (WebException ex)
			{
				if (ex.Response == null)
				{
					throw new WebonaryException("WebException with null response stream.", ex);
				}
				using (var stream = ex.Response.GetResponseStream())
				using (var reader = new StreamReader(stream))
				{
					var response = reader.ReadToEnd();
					throw new WebonaryException(response, ex);
				}
			}
		}

		protected override WebResponse GetWebResponse(WebRequest request)
		{
			return SetStatusAndReturn((HttpWebResponse)base.GetWebResponse(request));
		}


		protected override WebResponse GetWebResponse(WebRequest request, IAsyncResult result)
		{
			return SetStatusAndReturn((HttpWebResponse)base.GetWebResponse(request, result));
		}

		private WebResponse SetStatusAndReturn(HttpWebResponse response)
		{
			ResponseStatusCode = response.StatusCode;
			return response;
		}
	}
}

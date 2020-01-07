// Copyright (c) 2016-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Net;

namespace LanguageExplorer.Impls
{
	public class WebonaryException : Exception
	{
		public WebException WebException { get; }

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
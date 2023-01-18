// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System;
using System.Net;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// This interface allows us to mock the client connection for the purposes of unit testing
	/// </summary>
	public interface IWebonaryClient : IDisposable
	{
		WebHeaderCollection Headers { get; }
		byte[] UploadFileToWebonary(string address, string fileName, string method = null);
		HttpStatusCode ResponseStatusCode { get; }

		#region WebonaryApiV1
		string PostDictionaryMetadata(string address, string postBody);
		string PostEntry(string address, string postBody, bool isReversal);
		byte[] DeleteContent(string targetURI);
		/// <summary>
		/// This method returns a temporary url that can be used to upload a file to an AWS S3 bucket.
		/// </summary>
		/// <param name="address">The url for the request</param>
		/// <param name="filePath">The relative file path for the upload</param>
		/// <returns></returns>
		string GetSignedUrl(string address, string filePath);
		#endregion

	}
}
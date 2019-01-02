// Copyright (c) 2016-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Net;

namespace LanguageExplorer.Impls
{
	/// <summary>
	/// This interface allows us to mock the client connection for the purposes of unit testing
	/// </summary>
	public interface IWebonaryClient : IDisposable
	{
		WebHeaderCollection Headers { get; }
		byte[] UploadFileToWebonary(string address, string fileName);
		HttpStatusCode ResponseStatusCode { get; }
	}
}
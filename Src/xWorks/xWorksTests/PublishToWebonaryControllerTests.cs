// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Net;
using System.Text;
using NUnit.Framework;

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	class PublishToWebonaryControllerTests
	{
		#region Test connection to local Webonary instance
		[Test]
		[Category("ByHand")]
		public void CanConnectAtAll()
		{
			using (var client = new WebClient())
			{
				var responseText = ConnectAndUpload(client);
				Assert.That(responseText, Contains.Substring("returnedData"));
			}
		}

		[Test]
		[Category("ByHand")]
		public void CanAuthenticate()
		{
			using (var client = new WebClient())
			{
				client.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(new UTF8Encoding().GetBytes("username:password")));
				var responseText = ConnectAndUpload(client);
				Assert.That(responseText, Is.Not.StringContaining("You are not logged in."));
			}
		}

		[Test]
		[Category("ByHand")]
		public void ZipFileExtracts()
		{
			using (var client = new WebClient())
			{
				client.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(new UTF8Encoding().GetBytes("username:password")));
				var responseText = ConnectAndUpload(client);
				Assert.That(responseText, Is.StringContaining("extracted successfully"));
			}
		}

		/// <summary>
		/// Helper
		/// </summary>
		static string ConnectAndUpload(WebClient client)
		{
			var targetURI = "http://192.168.33.10/test/wp-json/webonary/import";
			var inputFile = "../../Src/xWorks/xWorksTests/lubwisi-d-new.zip";
			var response = client.UploadFile(targetURI, inputFile);
			var responseText = System.Text.Encoding.ASCII.GetString(response);
			return responseText;
		}
		#endregion
	}
}

// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.DisambiguateInFLExDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIL.DisambiguateInFLExDBTest
{
	[TestFixture]
	class GuidConverterTest
	{
		[Test, Ignore("Ignoring this test for timing purposes")]
		public void ConvertGuidsTest()
		{
			string result =
				"e2e4949d-9af0-4142-9d4f-f2d9afdcb646\nb3e8623e-5679-4261-acd5-d62ed71d1d2b\n9be2d38f-bc3a-4e96-acb5-64d2b3e53d95\n0dee3420-0d8e-4506-8737-c5a78b85188a\n1ea23f59-f6d9-406d-89f6-792318a04efe\n479aca02-ca6a-4c2a-862a-d980fbcc9a37\n04f021dc-a0dd-44fc-8b0a-9e6741743dd8\n4172bab0-cbf7-4e8f-96b5-1ac409c99276=91a77900-98fe-45f1-ae54-7886db43d64e=\n";
			var guids = GuidConverter.CreateListFromString(result);
			Assert.AreEqual(8, guids.Count);
			var guid = guids.ElementAt(0);
			Assert.AreEqual("e2e4949d-9af0-4142-9d4f-f2d9afdcb646", guid.ToString());
			guid = guids.ElementAt(2);
			Assert.AreEqual("9be2d38f-bc3a-4e96-acb5-64d2b3e53d95", guid.ToString());
			guid = guids.ElementAt(7);
			Assert.AreEqual("4172bab0-cbf7-4e8f-96b5-1ac409c99276", guid.ToString());
			result = null;
			guids = GuidConverter.CreateListFromString(result);
			Assert.AreEqual(0, guids.Count);
			result = "";
			guids = GuidConverter.CreateListFromString(result);
			Assert.AreEqual(0, guids.Count);
		}
	}
}

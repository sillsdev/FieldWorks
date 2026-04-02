// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.DisambiguateInFLExDB;
using SIL.DisambiguateInFLExDBTests;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SIL.DisambiguateInFLExDBTest
{
	[TestFixture]
	class AndFileLoaderTest : DisambiguateTests
	{
		string AndFile { get; set; }

		[Test]
		public void LoadAndFileGuidsTest()
		{
			AndFile = Path.Combine(TestDataDir, "Text4LoadTest.and");
			var result = AndFileLoader.GetGuidsFromAndFile(AndFile);
			Assert.AreEqual(17, result.Length);
			var guid = result[0];
			Assert.AreEqual(
				"d3f40ba4-6fc3-4696-a648-cc6d51f6fe1e\n399dd27b-5ff1-4719-87ac-58b2f0178463\n7df77c51-3b54-44a5-9a2c-fac71c31a197\n4172bab0-cbf7-4e8f-96b5-1ac409c99276=91a77900-98fe-45f1-ae54-7886db43d64e\n",
				guid.ToString()
			);
			guid = result[1];
			Assert.AreEqual("", guid.ToString());
			guid = result[3];
			Assert.AreEqual(
				"7e6c1be9-ad21-454e-91b4-dfc126514da7\n39994213-303e-4e59-9e47-f10e74d7331d\n1ea23f59-f6d9-406d-89f6-792318a04efe\n",
				guid.ToString()
			);
			guid = result[4];
			Assert.AreEqual(
				"e2e4949d-9af0-4142-9d4f-f2d9afdcb646\nb3e8623e-5679-4261-acd5-d62ed71d1d2b\n9be2d38f-bc3a-4e96-acb5-64d2b3e53d95\n0dee3420-0d8e-4506-8737-c5a78b85188a\n1ea23f59-f6d9-406d-89f6-792318a04efe\n479aca02-ca6a-4c2a-862a-d980fbcc9a37\n04f021dc-a0dd-44fc-8b0a-9e6741743dd8\n07fbf262-bbe7-415b-af3f-8317a2cb4521\n",
				guid.ToString()
			);
			guid = result[6];
			Assert.AreEqual("", guid.ToString());
			guid = result[16];
			Assert.AreEqual(
				"e2e4949d-9af0-4142-9d4f-f2d9afdcb646\n216db198-8a9e-43e6-ba00-f10db3d51465\n9be2d38f-bc3a-4e96-acb5-64d2b3e53d95\n0053c955-c19a-480b-bb45-9a1b27b3d5eb\n933d7fb3-b038-4913-b5b8-576f80df2fba\n7841d0ff-57f0-4a2c-a689-6d109efca66e\nb8c624a0-9fb3-4e39-867d-802292933ed5\n",
				guid.ToString()
			);
		}
	}
}

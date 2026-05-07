// Copyright (c) 2022-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.AlloGenService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SIL.AlloGenService.FLExCustomFieldsObtainer;

namespace SIL.AlloGenServiceTests
{
	[TestFixture]
	class FLExCustomFieldsObtainerTest : FwTestBase
	{
		[Test]
		public void GetListOfCustomFieldsTest()
		{
			Assert.NotNull(myCache);
			FLExCustomFieldsObtainer obtainer = new FLExCustomFieldsObtainer(myCache);
			List<FDWrapper> customFields = obtainer.CustomFields;
			Assert.NotNull(customFields);
			//foreach (FDWrapper fdw in customFields)
			//{
			//    Console.WriteLine("fd=" + fdw.Fd.Name);
			//}
			Assert.AreEqual(2, customFields.Count());
			var fd = customFields.FirstOrDefault(fdw => fdw.Fd.Name == "lx");
			Assert.NotNull(fd);
			fd = customFields.FirstOrDefault(fdw => fdw.Fd.Name == "SFMs");
			Assert.NotNull(fd);
		}
	}
}

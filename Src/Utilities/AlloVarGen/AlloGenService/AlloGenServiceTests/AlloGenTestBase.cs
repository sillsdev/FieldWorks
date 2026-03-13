// Copyright (c) 2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.AlloGenService;
using SIL.FieldWorks.Common.FwUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SIL.AlloGenServiceTest
{
	[TestFixture]
	public class AlloGenTestBase
	{
		protected XmlBackEndProvider provider = new XmlBackEndProvider();
		protected string TestDataDir { get; set; }
		protected string AlloGenFile { get; set; }
		protected string AlloGenExpected { get; set; }
		protected string ExpectedFileName = "AlloGenExpected.xml";

		[SetUp]
		public void Setup()
		{
			TestDataDir = Path.Combine(FwDirectoryFinder.SourceDirectory, "Utilities", "AlloVarGen", "AlloGenService", "AlloGenServiceTests", "TestData");
			AlloGenExpected = Path.Combine(TestDataDir, ExpectedFileName);
		}
	}
}

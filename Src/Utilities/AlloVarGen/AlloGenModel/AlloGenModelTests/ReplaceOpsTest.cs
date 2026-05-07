// Copyright (c) 2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.AlloGenModel;
using SIL.AlloGenService;
using SIL.FieldWorks.Common.FwUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AlloGenModelTests
{
	class ReplaceOpsTest
	{
		XmlBackEndProvider provider = new XmlBackEndProvider();
		string TestDataDir { get; set; }
		string AlloGenFile { get; set; }
		string AlloGenExpected { get; set; }

		[SetUp]
		public void Setup()
		{
			// nothing to do
		}

		private void InitDataFile()
		{
			TestDataDir = Path.Combine(FwDirectoryFinder.SourceDirectory, "Utilities", "AlloVarGen", "AlloGenModel", "AlloGenModelTests", "TestData");
			AlloGenExpected = Path.Combine(TestDataDir, AlloGenFile);
		}

		[Test]
		public void FindReplaceOpsTests()
		{
			AlloGenFile = "AlloGenExpected.xml";
			InitDataFile();
			provider.LoadDataFromFile(AlloGenExpected);
			AllomorphGenerators allomorphGenerators = provider.AlloGens;
			Assert.NotNull(allomorphGenerators);
			List<Replace> masterReplaceOps = allomorphGenerators.ReplaceOperations;
			Assert.NotNull(masterReplaceOps);
			Assert.AreEqual(4, masterReplaceOps.Count);
			Replace replace = allomorphGenerators.FindReplaceOp(
				"5e8b9b79-0269-4dee-bfb0-be8ed4f4dc5d"
			);
			Assert.NotNull(replace);
			Assert.AreEqual("*", replace.From);
			Assert.AreEqual("", replace.To);
			Assert.AreEqual(5, replace.WritingSystemRefs.Count);
			foreach (string name in replace.WritingSystemRefs)
			{
				Console.WriteLine("name='" + name + "'");
			}
			Assert.AreEqual("qvm-ach", replace.WritingSystemRefs[0]);
			Assert.AreEqual("qvm-acl", replace.WritingSystemRefs[1]);
			Assert.AreEqual("qvm-akh", replace.WritingSystemRefs[2]);
			Assert.AreEqual("qvm-akl", replace.WritingSystemRefs[3]);
			Assert.AreEqual("qvm-ame", replace.WritingSystemRefs[4]);
			Assert.AreEqual(false, replace.Mode);
			replace = allomorphGenerators.FindReplaceOp("34e77406-d2fe-4526-9bf9-3bc8fa653190");
			Assert.NotNull(replace);
			Assert.AreEqual("+", replace.From);
			Assert.AreEqual("", replace.To);
			Assert.AreEqual(5, replace.WritingSystemRefs.Count);
			Assert.AreEqual("qvm-ach", replace.WritingSystemRefs[0]);
			Assert.AreEqual("qvm-acl", replace.WritingSystemRefs[1]);
			Assert.AreEqual("qvm-akh", replace.WritingSystemRefs[2]);
			Assert.AreEqual("qvm-akl", replace.WritingSystemRefs[3]);
			Assert.AreEqual("qvm-ame", replace.WritingSystemRefs[4]);
			Assert.AreEqual(false, replace.Mode);
			replace = allomorphGenerators.FindReplaceOp("0f853476-407d-40e9-a8f3-803792f4f83e");
			Assert.NotNull(replace);
			Assert.AreEqual(":", replace.From);
			Assert.AreEqual("", replace.To);
			Assert.AreEqual(4, replace.WritingSystemRefs.Count);
			Assert.AreEqual("qvm-ach", replace.WritingSystemRefs[0]);
			Assert.AreEqual("qvm-acl", replace.WritingSystemRefs[1]);
			Assert.AreEqual("qvm-akh", replace.WritingSystemRefs[2]);
			Assert.AreEqual("qvm-akl", replace.WritingSystemRefs[3]);
			Assert.AreEqual(false, replace.Mode);
			replace = allomorphGenerators.FindReplaceOp("4c3f43c6-f130-4767-9a5a-f2a93b1c6222");
			Assert.NotNull(replace);
			Assert.AreEqual(":", replace.From);
			Assert.AreEqual("a", replace.To);
			Assert.AreEqual(1, replace.WritingSystemRefs.Count);
			Assert.AreEqual("qvm-ame", replace.WritingSystemRefs[0]);
			Assert.AreEqual(false, replace.Mode);
			// missing replace op
			replace = allomorphGenerators.FindReplaceOp("34e7XYZ7406-d2fe-4526-9bf9-3bc8fa653190");
			Assert.Null(replace);
		}

		[Test]
		public void DeleteReplaceOpsTests()
		{
			AlloGenFile = "DeleteReplaceOps.xml";
			InitDataFile();
			provider.LoadDataFromFile(AlloGenExpected);
			AllomorphGenerators allomorphGenerators = provider.AlloGens;
			Assert.NotNull(allomorphGenerators);
			List<Replace> masterReplaceOps = allomorphGenerators.ReplaceOperations;
			Assert.NotNull(masterReplaceOps);
			Assert.AreEqual(12, masterReplaceOps.Count);

			// delete a replace op that is not used
			Replace replace = allomorphGenerators.FindReplaceOp(
				"2dabfec9-2247-4aa1-876e-48614e59e339"
			);
			Assert.NotNull(replace);
			List<Operation> operations = allomorphGenerators.FindOperationsUsedByReplaceOp(replace);
			Assert.AreEqual(0, operations.Count);
			allomorphGenerators.DeleteReplaceOp(replace);
			Assert.AreEqual(false, allomorphGenerators.ReplaceOperations.Contains(replace));

			// delete a replace op used three times
			replace = allomorphGenerators.FindReplaceOp("331e03b6-d7b5-4761-b013-e4086a94e478");
			Assert.NotNull(replace);
			operations = allomorphGenerators.FindOperationsUsedByReplaceOp(replace);
			Assert.AreEqual(3, operations.Count);
			Operation op = allomorphGenerators.Operations.Find(o => o == operations[0]);
			SIL.AlloGenModel.Action action1 = op.Action;
			List<string> opRefs = action1.ReplaceOpRefs.FindAll(oRef => oRef == replace.Guid);
			Assert.AreEqual(1, opRefs.Count);
			op = allomorphGenerators.Operations.Find(o => o == operations[1]);
			SIL.AlloGenModel.Action action2 = op.Action;
			opRefs = action2.ReplaceOpRefs.FindAll(oRef => oRef == replace.Guid);
			Assert.AreEqual(1, opRefs.Count);
			op = allomorphGenerators.Operations.Find(o => o == operations[2]);
			SIL.AlloGenModel.Action action3 = op.Action;
			opRefs = action3.ReplaceOpRefs.FindAll(oRef => oRef == replace.Guid);
			Assert.AreEqual(2, opRefs.Count);
			allomorphGenerators.DeleteReplaceOp(replace);
			Assert.AreEqual(false, allomorphGenerators.ReplaceOperations.Contains(replace));
			Assert.AreEqual(false, action1.ReplaceOpRefs.Contains(replace.Guid));
			Assert.AreEqual(false, action2.ReplaceOpRefs.Contains(replace.Guid));
			Assert.AreEqual(false, action3.ReplaceOpRefs.Contains(replace.Guid));
		}

		[Test]
		public void ReplaceOpIsEmptyTests()
		{
			Replace rep = new Replace();
			Assert.AreEqual(true, rep.IsEmpty());
			rep.From = "something";
			Assert.AreEqual(false, rep.IsEmpty());
			rep.From = "";
			rep.Name = "something";
			Assert.AreEqual(false, rep.IsEmpty());
			rep.Name = "";
			rep.Description = "something";
			Assert.AreEqual(false, rep.IsEmpty());
		}
	}
}

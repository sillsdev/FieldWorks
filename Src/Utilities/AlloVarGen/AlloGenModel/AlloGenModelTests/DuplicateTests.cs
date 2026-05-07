// Copyright (c) 2022-2023 SIL International
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
using Action = SIL.AlloGenModel.Action;

namespace AlloGenModelTests
{
	class DuplicateTests
	{
		XmlBackEndProvider provider = new XmlBackEndProvider();
		string TestDataDir { get; set; }
		string AlloGenFile { get; set; }
		string AlloGenExpected { get; set; }

		[SetUp]
		public void Setup()
		{
			TestDataDir = Path.Combine(FwDirectoryFinder.SourceDirectory, "Utilities", "AlloVarGen", "AlloGenModel", "AlloGenModelTests", "TestData");
			AlloGenExpected = Path.Combine(TestDataDir, "AlloGenExpected.xml");
		}

		[Test]
		public void DuplicateTest()
		{
			provider.LoadDataFromFile(AlloGenExpected);
			AllomorphGenerators allomorphGenerators = provider.AlloGens;
			Assert.NotNull(allomorphGenerators);
			Operation operation = allomorphGenerators.Operations[0];
			Operation opCopy = operation.Duplicate();
			Assert.AreEqual(true, operation.Equals(opCopy));
			opCopy.Description = "";
			Assert.AreEqual(false, operation.Equals(opCopy));
			Pattern pattern = operation.Pattern;
			Pattern patCopy = opCopy.Pattern;
			Assert.AreEqual(true, pattern.Equals(patCopy));
			patCopy.Matcher.MatchCase = !pattern.Matcher.MatchCase;
			Assert.AreEqual(false, pattern.Equals(patCopy));
			Action action = operation.Action;
			Action actCopy = opCopy.Action;
			Assert.AreEqual(true, action.Equals(actCopy));

			string guid = action.ReplaceOpRefs[0];
			Replace replace = allomorphGenerators.FindReplaceOp(guid);
			Assert.NotNull(replace);
			Replace replaceCopy = replace.Duplicate();
			Assert.AreEqual(replace.Name, replaceCopy.Name);
			Assert.AreEqual(replace.Description, replaceCopy.Description);
			Assert.AreEqual(replace.From, replaceCopy.From);
			Assert.AreEqual(replace.To, replaceCopy.To);
			Assert.AreEqual(replace.Mode, replaceCopy.Mode);
			Assert.AreEqual(replace.WritingSystemRefs.Count, replaceCopy.WritingSystemRefs.Count);
			for (int i = 0; i < replace.WritingSystemRefs.Count; i++)
			{
				Assert.AreEqual(replace.WritingSystemRefs[i], replaceCopy.WritingSystemRefs[i]);
			}
			Assert.AreEqual(replace.Active, replaceCopy.Active);

			Action actionCopy = action.Duplicate();
			Assert.NotNull(actionCopy);
			Assert.AreEqual(action.Environments, actionCopy.Environments);
			Assert.AreEqual(action.PublishEntryInItems, actionCopy.PublishEntryInItems);
			Assert.AreEqual(action.ShowMinorEntry, actionCopy.ShowMinorEntry);
			Assert.AreEqual(action.StemName, actionCopy.StemName);
			Assert.AreEqual(action.VariantTypes, actionCopy.VariantTypes);
			Assert.AreEqual(action.ReplaceOpRefs, actionCopy.ReplaceOpRefs);
		}
	}
}

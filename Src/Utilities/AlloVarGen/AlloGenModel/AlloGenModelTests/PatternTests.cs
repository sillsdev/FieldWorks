// Copyright (c) 2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.AlloGenModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlloGenModelTests
{
	class PatternTests
	{
		[SetUp]
		public void Setup()
		{
			// nothing to do
		}

		[Test]
		public void CreationTest()
		{
			Pattern pattern = new Pattern();
			pattern.SetDefaultMorphTypes();
			var mts = pattern.MorphTypes;
			Assert.AreEqual(4, mts.Count);
			var mt = mts[0];
			Assert.AreEqual("bound root", mt.Name);
			Assert.AreEqual("d7f713e4-e8cf-11d3-9764-00c04f186933", mt.Guid);
			mt = mts[1];
			Assert.AreEqual("bound stem", mt.Name);
			Assert.AreEqual("d7f713e7-e8cf-11d3-9764-00c04f186933", mt.Guid);
			mt = mts[2];
			Assert.AreEqual("root", mt.Name);
			Assert.AreEqual("d7f713e5-e8cf-11d3-9764-00c04f186933", mt.Guid);
			mt = mts[3];
			Assert.AreEqual("stem", mt.Name);
			Assert.AreEqual("d7f713e8-e8cf-11d3-9764-00c04f186933", mt.Guid);
		}
	}
}

// Copyright (c) 2017-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using LanguageExplorer;
using NUnit.Framework;
using SIL.Extensions;

namespace LanguageExplorerTests.Repository
{
	internal class AreaTestBase : MefTestBase
	{
		protected IArea _myArea;
		protected IReadOnlyDictionary<string, ITool> _myOrderedTools;
		protected string _areaMachineName;

		/// <summary>
		/// Set up test fixture.
		/// </summary>
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			if (string.IsNullOrWhiteSpace(_areaMachineName))
			{
				return;
			}
			_myArea = _areaRepository.GetArea(_areaMachineName);
			_myOrderedTools = _myArea.AllToolsInOrder;
		}

		/// <summary>
		/// Tear down the test fixture.
		/// </summary>
		public override void FixtureTeardown()
		{
			_myOrderedTools = null;
			_myArea = null;
			_areaMachineName = null;

			base.FixtureTeardown();
		}

		protected void DoTests(string uiName, int idx, string expectedMachineName)
		{
			var tool = _myOrderedTools[uiName];
			Assert.AreEqual(idx, _myOrderedTools.IndexOf(new KeyValuePair<string, ITool>(uiName, tool)));
			Assert.AreEqual(expectedMachineName, tool.MachineName);
			Assert.IsTrue(ReferenceEquals(_myArea, tool.Area));
		}
	}
}
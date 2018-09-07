// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.IO;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using NUnit.Framework;
using Paratext.Data;
using PtxUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.FwUtils;

namespace Paratext8Plugin
{
	/// <summary>
	/// Stub class to simulate the Scripture Provider class.
	/// </summary>
	public class MockScriptureProvider
	{
		[Import]
		private ScriptureProvider.IScriptureProvider _potentialScriptureProvider;

		internal static ScriptureProvider.IScriptureProvider _provider;

		static MockScriptureProvider()
		{
			MockScriptureProvider provider = new MockScriptureProvider();
			AggregateCatalog catalog = new AggregateCatalog();
			var exePath = Path.Combine(Path.GetDirectoryName(FwDirectoryFinder.FlexExe));
			catalog.Catalogs.Add(new DirectoryCatalog(exePath, "Paratext8Plugin.dll"));
			using (CompositionContainer container = new CompositionContainer(catalog))
			{
				container.SatisfyImportsOnce(provider);
			}
			_provider = provider._potentialScriptureProvider;
		}

		public static void Initialize()
		{
			_provider.Initialize();
		}

		public static bool IsInstalled { get { return _provider != null && _provider.IsInstalled; } }
	}

	class ConsoleAlert : Alert
	{
		protected override void ShowLaterInternal(string text, string caption, AlertLevel alertLevel)
		{
			Console.WriteLine(text);
		}

		protected override AlertResult ShowInternal(IComponent owner, string text, string caption, AlertButtons alertButtons,
			AlertLevel alertLevel, AlertDefaultButton defaultButton, bool showInTaskbar)
		{
			Console.WriteLine(text);
			return AlertResult.Negative;
		}
	}

	/// <summary>
	/// Tests to determine that the ParatextData dll is functioning as expected.
	/// </summary>
	[TestFixture]
	public class ParatextDataIntegrationTests
	{

		[TestFixtureSetUp]
		public void FixtureSetUp()
		{
			Alert.Implementation = new ConsoleAlert();
		}

		[SetUp]
		public void Setup()
		{
			if (!ParatextInfo.IsParatextInstalled)
				Assert.Ignore("Paratext is not installed");
		}

		[Test]
		public void ParatextCanInitialize()
		{
			try
			{
				MockScriptureProvider.Initialize();
			}
			catch (Exception e)
			{
				// A TypeInitializationException may also be thrown if ParaText 8 is not installed.
				Assert.False(MockScriptureProvider.IsInstalled);
				// A FileLoadException may indicate that ParatextData dependency (i.e. icu.net) has been undated to a new version.
				Assert.False(e.GetType().Name.Contains(typeof(FileLoadException).Name));
			}
		}
	}
}

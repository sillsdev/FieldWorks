// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Every command, menu and hotlinks reference in the shipped configuration and layouts
	/// names something the configuration defines, so no menu throws when it is built.
	/// </summary>
	[TestFixture]
	[Apartment(System.Threading.ApartmentState.STA)]
	public class WindowConfigurationIntegrityTests : XWorksAppTestBase
	{
		private XmlNode m_configuration;

		protected override void Init()
		{
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
		}

		[SetUp]
		public void LoadConfiguration()
		{
			m_window = new MockFwXWindow(m_application, m_configFilePath);
			((MockFwXWindow)m_window).Init(Cache);
			m_window.PropTable.RemoveLocalAndGlobalSettings();
			m_window.LoadUI(m_configFilePath);
			m_configuration = m_window.PropTable.GetValue<XmlNode>("WindowConfiguration");
			Assert.That(m_configuration, Is.Not.Null);
		}

		[TearDown]
		public void DisposeWindow()
		{
			m_configuration = null;
			if (m_window != null && !m_window.IsDisposed)
			{
				m_window.PropTable.RemoveLocalAndGlobalSettings();
				m_window.Dispose();
			}
			m_window = null;
		}

		[Test]
		public void EveryCommandReference_NamesADefinedCommand()
		{
			var defined = Ids(m_configuration.SelectNodes("//commands/command[@id]"));
			var missing = m_configuration.SelectNodes("//*[@command]").Cast<XmlNode>()
				.Select(n => n.Attributes["command"].Value).Where(id => !defined.Contains(id))
				.Distinct().ToList();

			Assert.That(missing, Is.Empty, "menu, toolbar or context-menu items naming an undefined command");
		}

		[Test]
		public void EveryLayoutMenuReference_NamesADefinedMenu()
		{
			var defined = Ids(m_configuration.SelectNodes("//menu[@id]"));
			var missing = new List<string>();
			var files = ShippedLayoutFiles().ToList();
			Assert.That(files, Is.Not.Empty, "the shipped layout and part files");
			foreach (var path in files)
			{
				var doc = new XmlDocument();
				doc.Load(path);
				foreach (XmlNode node in doc.SelectNodes("//*[@menu or @hotlinks]"))
				{
					foreach (var attribute in new[] { "menu", "hotlinks" })
					{
						var id = node.Attributes[attribute]?.Value;
						if (!string.IsNullOrEmpty(id) && !defined.Contains(id))
							missing.Add(Path.GetFileName(path) + ": " + attribute + "=" + id);
					}
				}
			}

			Assert.That(missing, Is.Empty, "layout parts naming a context menu the window configuration lacks");
		}

		// The layout and part files DataTree loads: Language Explorer's and the shared ones.
		private static IEnumerable<string> ShippedLayoutFiles()
		{
			var folders = new[]
			{
				Path.Combine(FwDirectoryFinder.FlexFolder, "Configuration", "Parts"),
				Path.Combine(FwDirectoryFinder.CodeDirectory, "Parts")
			};
			return folders.Where(Directory.Exists).SelectMany(folder =>
				Directory.GetFiles(folder, "*.fwlayout").Concat(Directory.GetFiles(folder, "*Parts.xml")));
		}

		private static HashSet<string> Ids(XmlNodeList nodes)
		{
			return new HashSet<string>(nodes.Cast<XmlNode>().Select(n => n.Attributes["id"].Value),
				StringComparer.Ordinal);
		}
	}
}

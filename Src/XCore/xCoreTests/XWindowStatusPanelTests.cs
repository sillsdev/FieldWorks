// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Threading;
using System.Windows.Forms;
using System.Xml;
using NUnit.Framework;

namespace XCore
{
	/// <summary>
	/// The status bar panel text contract of <see cref="XWindow"/>.
	/// </summary>
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class XWindowStatusPanelTests
	{
		/// <summary>
		/// A window whose status bar is built from a statusbar element alone, with no other UI.
		/// </summary>
		private sealed class StatusBarOnlyWindow : XWindow
		{
			public void LoadStatusBar(string windowXml)
			{
				var doc = new XmlDocument();
				doc.LoadXml(windowXml);
				CreateStatusBar(doc.DocumentElement);
			}
		}

		private const string TwoPanels =
			"<window><statusbar>"
			+ "<panel id=\"Message\"/><panel id=\"RecordNumber\" width=\"Contents\"/>"
			+ "</statusbar></window>";

		private static string PanelText(XWindow window, string panelId)
		{
			return window.PropTable.GetValue<StatusBarPanel>(panelId).Text;
		}

		[Test]
		public void SetStatusPanelText_ShowsTheTextInThePanelWithThatId()
		{
			using (var window = new StatusBarOnlyWindow())
			{
				window.LoadStatusBar(TwoPanels);

				window.SetStatusPanelText("RecordNumber", "3/12");

				Assert.That(PanelText(window, "RecordNumber"), Is.EqualTo("3/12"));
				Assert.That(PanelText(window, "Message"), Is.Empty);
			}
		}

		[Test]
		public void SetStatusPanelText_IgnoresAnIdNoPanelHas()
		{
			using (var window = new StatusBarOnlyWindow())
			{
				window.LoadStatusBar(TwoPanels);

				Assert.That(() => window.SetStatusPanelText("Progress", "parsing"), Throws.Nothing);
				Assert.That(() => window.SetStatusPanelText(null, "parsing"), Throws.Nothing);

				Assert.That(PanelText(window, "Message"), Is.Empty);
				Assert.That(PanelText(window, "RecordNumber"), Is.Empty);
			}
		}

		[Test]
		public void SetStatusPanelText_IgnoresAWindowWithNoStatusBar()
		{
			using (var window = new StatusBarOnlyWindow())
				Assert.That(() => window.SetStatusPanelText("Message", "hello"), Throws.Nothing);
		}
	}
}

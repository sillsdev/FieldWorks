// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Automation;
using NUnit.Framework;

namespace FwAvaloniaPreviewHostTests
{
	/// <summary>
	/// Native desktop automation (UIA2/System.Windows.Automation) smoke tests for the net48 preview
	/// host. These validate the real Windows accessibility tree, not Avalonia.Headless internals.
	/// They require an interactive Windows desktop/session and therefore are categorized separately.
	/// </summary>
	[TestFixture]
	[Category("UIA")]
	[NonParallelizable]
	[Apartment(ApartmentState.STA)]
	public class PreviewHostUiaTests
	{
		private Process m_process;

		[TearDown]
		public void TearDown()
		{
			if (m_process == null)
				return;

			try
			{
				if (!m_process.HasExited)
				{
					m_process.CloseMainWindow();
					if (!m_process.WaitForExit(2000))
					{
						m_process.Kill();
						m_process.WaitForExit(2000);
					}
				}
			}
			catch
			{
			}
			finally
			{
				m_process.Dispose();
				m_process = null;
			}
		}

		[Test]
		public void PreviewHost_MainWindowAndCoreControls_ExposeStableAutomationIds()
		{
			EnsureInteractiveDesktop();
			var window = StartPreviewHostAndWaitForWindow();

			Assert.That(window.Current.Name, Is.EqualTo("Lexical Edit POC (Preview)"));
			Assert.That(FindByAutomationId(window, "LexemeFormEditor.seh"), Is.Not.Null);
			Assert.That(FindByAutomationId(window, "LexemeFormEditor.en"), Is.Not.Null);
			Assert.That(FindByAutomationId(window, "SenseGlossEditor.en"), Is.Not.Null);
			Assert.That(FindByAutomationId(window, "MorphTypeChooser.Button"), Is.Not.Null);
		}

		[Test]
		public void PreviewHost_MorphTypeButton_Invoke_ShowsPopupList()
		{
			EnsureInteractiveDesktop();
			var window = StartPreviewHostAndWaitForWindow();
			var button = FindByAutomationId(window, "MorphTypeChooser.Button");
			Assert.That(button, Is.Not.Null, "Morph type button should be reachable in the automation tree.");

			var invoke = button.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
			Assert.That(invoke, Is.Not.Null, "Morph type button should support InvokePattern.");
			invoke.Invoke();

			var list = WaitForElement(
				() => AutomationElement.RootElement.FindFirst(
					TreeScope.Subtree,
					new PropertyCondition(AutomationElement.AutomationIdProperty, "MorphTypeChooser.List")));

			Assert.That(list, Is.Not.Null, "Invoking the button should show the popup list.");
			var suffix = list.FindFirst(
				TreeScope.Descendants,
				new PropertyCondition(AutomationElement.NameProperty, "suffix"));
			Assert.That(suffix, Is.Not.Null, "Popup should expose the 'suffix' option through UIA.");
		}

		private static void EnsureInteractiveDesktop()
		{
			if (!Environment.UserInteractive)
			{
				Assert.Ignore("UIA2 preview-host tests require an interactive Windows desktop/session.");
			}
		}

		private AutomationElement StartPreviewHostAndWaitForWindow()
		{
			var exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FwAvaloniaPreviewHost.exe");
			Assert.That(File.Exists(exePath), Is.True,
				"Preview host executable must be built before running UIA2 tests. Expected: " + exePath);

			m_process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = exePath,
					Arguments = "--module lexical-edit-poc --data sample",
					WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
					UseShellExecute = false
				}
			};

			Assert.That(m_process.Start(), Is.True, "Preview host process should start.");

			return WaitForElement(() =>
			{
				m_process.Refresh();
				if (m_process.HasExited || m_process.MainWindowHandle == IntPtr.Zero)
					return null;
				return AutomationElement.FromHandle(m_process.MainWindowHandle);
			});
		}

		private static AutomationElement FindByAutomationId(AutomationElement root, string automationId)
			=> root.FindFirst(
				TreeScope.Descendants,
				new PropertyCondition(AutomationElement.AutomationIdProperty, automationId));

		private static AutomationElement WaitForElement(Func<AutomationElement> finder)
		{
			var deadline = DateTime.UtcNow.AddSeconds(10);
			while (DateTime.UtcNow < deadline)
			{
				var element = finder();
				if (element != null)
					return element;
				Thread.Sleep(100);
			}

			return null;
		}
	}
}

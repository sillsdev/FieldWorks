using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Automation;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	[Category("UIA")]
	[Category("DesktopRequired")]
	[NonParallelizable]
	[Apartment(ApartmentState.STA)]
	public class MorphTypeLauncherUiaSmokeTests : XWorksAppTestBase
	{
		private List<ICmObject> m_createdObjects;
		private ILexEntry m_entry;

		protected override void Init()
		{
			m_application = new MockFwXApp(
				new MockFwManager { Cache = Cache },
				null,
				null);
			m_configFilePath = Path.Combine(
				FwDirectoryFinder.CodeDirectory,
				m_application.DefaultConfigurationPathname);
		}

		[SetUp]
		public void SetUpLauncher()
		{
			WinFormsUiaTestHelpers.EnsureInteractiveDesktop();

			m_window = new MockFwXWindow(m_application, m_configFilePath);
			((MockFwXWindow)m_window).Init(Cache);
			m_createdObjects = new List<ICmObject>();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, CreateLexiconTestData);
		}

		[TearDown]
		public void TearDownLauncher()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, DestroyLexiconTestData);
			m_createdObjects = null;
			m_entry = null;

			if (m_window != null && !m_window.IsDisposed)
			{
				m_window.Dispose();
				m_window = null;
			}
		}

		[Test]
		public void MorphTypeLauncher_RealizedWindow_ExposesInvokePatternThroughUIA()
		{
			using (var host = new MorphTypeLauncherHost(Cache, m_entry.LexemeFormOA))
			{
				host.WaitUntilReady();

				var button = AutomationElement.FromHandle(host.LauncherButtonHandle);
				Assert.That(button, Is.Not.Null);
				Assert.That(button.Current.ControlType, Is.EqualTo(ControlType.Button));
				Assert.That(
					button.GetCurrentPattern(InvokePattern.Pattern),
					Is.Not.Null,
					"Launcher button should expose InvokePattern through UIA.");
			}
		}

		[Test]
		public void MorphTypeChooser_RealizedWindow_ExposesCoreTreeAndCancelButtonThroughUIA()
		{
			using (var host = new MorphTypeChooserHost(Cache, m_entry.LexemeFormOA))
			{
				host.WaitUntilReady();

				var chooser = AutomationElement.FromHandle(host.WindowHandle);
				Assert.That(chooser, Is.Not.Null);
				Assert.That(
					WinFormsUiaTestHelpers.FindByAutomationId(chooser, "m_labelsTreeView"),
					Is.Not.Null,
					"Chooser tree should be reachable through UIA.");
				WinFormsUiaTestHelpers.AssertButtonSupportsInvokePattern(
					chooser,
					"btnCancel");
			}
		}

		private void CreateLexiconTestData()
		{
			var stemMorphType = GetMorphTypeOrCreateOne("stem");
			var nounPartOfSpeech = GetGrammaticalCategoryOrCreateOne(
				"noun",
				Cache.LangProject.PartsOfSpeechOA);
			m_entry = AddLexeme(
				m_createdObjects,
				"uia-entry",
				stemMorphType,
				"uia gloss",
				nounPartOfSpeech);
		}

		private void DestroyLexiconTestData()
		{
			if (m_createdObjects == null)
				return;

			foreach (var obj in m_createdObjects)
			{
				if (!obj.IsValidObject)
					continue;
				if (obj is ILexEntry)
					obj.Delete();
			}
		}

		private sealed class MorphTypeLauncherHost : IDisposable
		{
			private readonly LcmCache m_cache;
			private readonly ICmObject m_obj;
			private readonly ManualResetEventSlim m_ready = new ManualResetEventSlim();
			private readonly Thread m_thread;
			private Exception m_startupException;
			private Form m_hostForm;
			private Mediator m_mediator;
			private PropertyTable m_propertyTable;
			private MorphTypeAtomicLauncher m_launcher;

			public MorphTypeLauncherHost(LcmCache cache, ICmObject obj)
			{
				m_cache = cache;
				m_obj = obj;
				m_thread = new Thread(ThreadMain) { IsBackground = true };
				m_thread.SetApartmentState(ApartmentState.STA);
				m_thread.Start();
			}

			public IntPtr LauncherButtonHandle { get; private set; }

			public void WaitUntilReady()
			{
				Assert.That(
					m_ready.Wait(TimeSpan.FromSeconds(10)),
					Is.True,
					"Timed out waiting for the launcher host window.");
				if (m_startupException != null)
				{
					Assert.Fail(
						"Morph type launcher host failed to start: " + m_startupException);
				}
			}

			public void Dispose()
			{
				try
				{
					if (m_hostForm != null && !m_hostForm.IsDisposed && m_hostForm.IsHandleCreated)
					{
						m_hostForm.BeginInvoke(new MethodInvoker(() => m_hostForm.Close()));
					}
				}
				catch
				{
				}

				if (!m_thread.Join(TimeSpan.FromSeconds(5)))
					m_thread.Abort();

				m_propertyTable?.Dispose();
				m_propertyTable = null;
				m_mediator?.Dispose();
				m_mediator = null;
				m_ready.Dispose();
			}

			private void ThreadMain()
			{
				try
				{
					m_mediator = new Mediator();
					m_propertyTable = new PropertyTable(m_mediator);
					m_hostForm = new Form
					{
						Text = "MorphTypeLauncherUiaHost",
						ShowInTaskbar = false,
						StartPosition = FormStartPosition.Manual,
						Left = 80,
						Top = 80,
						Width = 320,
						Height = 120
					};
					m_propertyTable.SetProperty("window", m_hostForm, false);
					m_propertyTable.SetPropertyPersistence("window", false);

					m_launcher = new MorphTypeAtomicLauncher { Dock = DockStyle.Fill };
					m_launcher.Initialize(
						m_cache,
						m_obj,
						MoFormTags.kflidMorphType,
						"MorphTypeRA",
						null,
						m_mediator,
						m_propertyTable,
						string.Empty,
						"analysis");
					m_hostForm.Controls.Add(m_launcher);
					m_hostForm.Show();
					m_hostForm.Activate();
					Application.DoEvents();

					LauncherButtonHandle = m_launcher.LauncherButton.Handle;
					m_ready.Set();
					Application.Run(m_hostForm);
				}
				catch (Exception ex)
				{
					m_startupException = ex;
					m_ready.Set();
				}
			}
		}

		private sealed class MorphTypeChooserHost : IDisposable
		{
			private readonly LcmCache m_cache;
			private readonly ICmObject m_obj;
			private readonly ManualResetEventSlim m_ready = new ManualResetEventSlim();
			private readonly Thread m_thread;
			private Exception m_startupException;
			private MorphTypeChooser m_chooser;

			public MorphTypeChooserHost(LcmCache cache, ICmObject obj)
			{
				m_cache = cache;
				m_obj = obj;
				m_thread = new Thread(ThreadMain) { IsBackground = true };
				m_thread.SetApartmentState(ApartmentState.STA);
				m_thread.Start();
			}

			public IntPtr WindowHandle { get; private set; }

			public void WaitUntilReady()
			{
				Assert.That(
					m_ready.Wait(TimeSpan.FromSeconds(10)),
					Is.True,
					"Timed out waiting for the chooser host window.");
				if (m_startupException != null)
				{
					Assert.Fail("Morph type chooser host failed to start: " + m_startupException);
				}
			}

			public void Dispose()
			{
				try
				{
					if (m_chooser != null && !m_chooser.IsDisposed && m_chooser.IsHandleCreated)
					{
						m_chooser.BeginInvoke(new MethodInvoker(() => m_chooser.Close()));
					}
				}
				catch
				{
				}

				if (!m_thread.Join(TimeSpan.FromSeconds(5)))
					m_thread.Abort();

				m_ready.Dispose();
			}

			private void ThreadMain()
			{
				try
				{
					var labels = ObjectLabel.CreateObjectLabels(
						m_cache,
						m_obj.ReferenceTargetCandidates(MoFormTags.kflidMorphType),
						string.Empty,
						"analysis vernacular");
					m_chooser = new MorphTypeChooser(null, labels, "MorphTypeRA", null);
					m_chooser.Show();
					m_chooser.Activate();
					Application.DoEvents();
					WindowHandle = m_chooser.Handle;
					m_ready.Set();
					Application.Run(m_chooser);
				}
				catch (Exception ex)
				{
					m_startupException = ex;
					m_ready.Set();
				}
			}
		}
	}

	[TestFixture]
	[Category("UIA")]
	[Category("DesktopRequired")]
	[NonParallelizable]
	[Apartment(ApartmentState.STA)]
	public class BulkEditBarUiaSmokeTests : BulkEditBarTestsBase
	{
		[Test]
		public void FilterBar_RealizedWindow_ExposesTargetCombosThroughUIA()
		{
			WinFormsUiaTestHelpers.EnsureInteractiveDesktop();

			m_window.Show();
			m_window.Activate();
			Application.DoEvents();

			var window = AutomationElement.FromHandle(m_window.Handle);
			Assert.That(window, Is.Not.Null);

			var filterBar = WinFormsUiaTestHelpers.FindByAutomationId(window, "FilterBar");
			Assert.That(filterBar, Is.Not.Null, "FilterBar should be reachable through UIA.");

			var lexemeCombo = WinFormsUiaTestHelpers.FindByAutomationId(
				window,
				"FilterCombo.LexemeFormForEntry");
			Assert.That(
				lexemeCombo,
				Is.Not.Null,
				"Lexeme Form filter combo should be reachable through UIA.");

			var morphTypeCombo = WinFormsUiaTestHelpers.FindByAutomationId(
				window,
				"FilterCombo.MorphTypeForEntry");
			Assert.That(
				morphTypeCombo,
				Is.Not.Null,
				"Morph Type filter combo should be reachable through UIA.");

			Assert.DoesNotThrow(
				() => lexemeCombo.SetFocus(),
				"Lexeme Form filter combo should be focusable through UIA.");
			Assert.DoesNotThrow(
				() => morphTypeCombo.SetFocus(),
				"Morph Type filter combo should be focusable through UIA.");
		}
	}

	internal static class WinFormsUiaTestHelpers
	{
		internal static void EnsureInteractiveDesktop()
		{
			if (!Environment.UserInteractive)
			{
				Assert.Ignore(
					"UIA2 WinForms smoke tests require an interactive Windows desktop/session.");
			}
		}

		internal static AutomationElement FindByAutomationId(
			AutomationElement root,
			string automationId)
		{
			return root.FindFirst(
				TreeScope.Descendants,
				new PropertyCondition(AutomationElement.AutomationIdProperty, automationId));
		}

		internal static void AssertButtonSupportsInvokePattern(
			AutomationElement dialog,
			string buttonAutomationId)
		{
			var button = FindByAutomationId(dialog, buttonAutomationId);
			Assert.That(button, Is.Not.Null, "Expected to find button '{0}'.", buttonAutomationId);
			var invoke = button.GetCurrentPattern(InvokePattern.Pattern) as InvokePattern;
			Assert.That(invoke, Is.Not.Null, "Button '{0}' should support InvokePattern.", buttonAutomationId);
		}
	}
}
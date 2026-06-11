// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Task 3.10 audit: when the Avalonia surface is active, <c>RecordEditView</c> must not instantiate or
	/// drive the hidden legacy <c>DataTree</c>. This proves the active-host contract on the real product
	/// host by loading the lexicon edit tool fresh in the New UI mode and asserting the legacy surface was
	/// never initialized, while the Avalonia surface was.
	/// </summary>
	[TestFixture]
	[Apartment(System.Threading.ApartmentState.STA)]
	public class RecordEditViewActiveHostContractTests : XWorksAppTestBase
	{
		private PropertyTable m_propertyTable;
		private List<ICmObject> m_createdObjects;

		protected override void Init()
		{
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			m_configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory, m_application.DefaultConfigurationPathname);
		}

		[SetUp]
		public void SetUpWindow()
		{
			m_window = new MockFwXWindow(m_application, m_configFilePath);
			((MockFwXWindow)m_window).Init(Cache);
			m_propertyTable = m_window.PropTable;
			m_propertyTable.RemoveLocalAndGlobalSettings();
			m_window.LoadUI(m_configFilePath);
			m_createdObjects = new List<ICmObject>();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, CreateLexiconTestData);
		}

		[TearDown]
		public void TearDownWindow()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, DestroyLexiconTestData);
			m_createdObjects = null;
			m_propertyTable?.RemoveLocalAndGlobalSettings();
			m_propertyTable = null;
			if (m_window != null && !m_window.IsDisposed)
			{
				m_window.Dispose();
				m_window = null;
			}
		}

		[Test]
		public void AvaloniaActive_DoesNotInitializeOrDriveLegacyDataTree()
		{
			m_propertyTable.SetProperty("UIMode", "New", true);
			m_propertyTable.SetPropertyPersistence("UIMode", false);

			LoadRecordEditView("lexiconEdit");
			DrainMediatorAndIdleQueues();

			var control = m_propertyTable.GetValue<object>("currentContentControlObject", null) as RecordEditView;
			Assert.That(control, Is.Not.Null);
			EnsureCurrentRecord(control);

			Assert.That(GetPrivateFieldValue(control, "m_lexicalEditSurface"), Is.EqualTo(LexicalEditSurface.Avalonia),
				"Precondition: the lexicon edit tool should resolve to the Avalonia surface under the New UI mode.");

			// Active-host contract (task 3.10): the legacy DataTree must not have been initialized or driven
			// while Avalonia is the active surface. This is the audited invariant.
			Assert.That(GetPrivateFieldValue(control, "m_legacySurfaceInitialized"), Is.EqualTo(false),
				"The active Avalonia path must not instantiate or drive the hidden legacy DataTree (task 3.10).");

			var panel = (Panel)GetPrivateFieldValue(control, "m_panel");
			var legacyDataTree = (DataTree)GetPrivateFieldValue(control, "m_dataEntryForm");
			Assert.That(panel.Controls.Contains(legacyDataTree), Is.False,
				"The dormant legacy DataTree must not remain parented in the panel while Avalonia is the active surface.");

			// Note: realizing the Avalonia WinForms-interop host requires a real UI context, which this
			// headless xWorks harness does not provide, so we do not assert the host was created here. The
			// FwAvaloniaTests headless suite covers Avalonia surface construction/rendering directly.
		}

		private void LoadRecordEditView(string toolValue)
		{
			var windowConfiguration = m_propertyTable.GetValue<XmlNode>("WindowConfiguration");
			Assert.That(windowConfiguration, Is.Not.Null);
			var controlNode = windowConfiguration.SelectSingleNode(
				string.Format("//tool[@value='{0}']/control//control[dynamicloaderinfo/@class='SIL.FieldWorks.XWorks.RecordEditView']", toolValue));
			Assert.That(controlNode, Is.Not.Null, "Expected the RecordEditView configuration node for tool '{0}'.", toolValue);

			m_propertyTable.SetProperty("currentContentControlParameters", controlNode, true);
			m_propertyTable.SetPropertyPersistence("currentContentControlParameters", false);
			m_propertyTable.SetProperty("currentContentControl", toolValue, true);
			m_propertyTable.SetPropertyPersistence("currentContentControl", false);
		}

		private void CreateLexiconTestData()
		{
			var stemMorphType = GetMorphTypeOrCreateOne("stem");
			var nounPartOfSpeech = GetGrammaticalCategoryOrCreateOne("noun", Cache.LangProject.PartsOfSpeechOA);
			AddLexeme(m_createdObjects, "contract-entry", stemMorphType, "contract gloss", nounPartOfSpeech);
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

		private static object GetPrivateFieldValue(object target, string fieldName)
		{
			var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(field, Is.Not.Null, "Missing private field: " + fieldName);
			return field.GetValue(target);
		}

		private void DrainMediatorAndIdleQueues()
		{
			var idleQueue = m_window.Mediator.IdleQueue;
			var processIdle = idleQueue.GetType().GetMethod("Application_Idle", BindingFlags.Instance | BindingFlags.NonPublic);
			Assert.That(processIdle, Is.Not.Null);

			for (var iteration = 0; iteration < 8; iteration++)
			{
				((MockFwXWindow)m_window).ProcessPendingItems();
				if (idleQueue.Count == 0 && m_window.Mediator.JobItems == 0)
					break;
				if (idleQueue.Count > 0)
					processIdle.Invoke(idleQueue, new object[] { this, EventArgs.Empty });
			}

			Application.DoEvents();
		}

		private void EnsureCurrentRecord(RecordEditView control)
		{
			if (control.Clerk.CurrentObject != null)
				return;
			control.Clerk.JumpToIndex(0);
			DrainMediatorAndIdleQueues();
			Assert.That(control.Clerk.CurrentObject, Is.Not.Null);
		}
	}
}

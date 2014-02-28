// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FwCoreDlgControls;
using SIL.FieldWorks.XWorks.DictionaryDetailsView;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	class DictionaryDetailsControllerTests : XWorksAppTestBase, IDisposable
	{
		private Mediator m_mediator;
		private FwStyleSheet m_styleSheet;

		#region IDisposable and Gendarme requirements
		~DictionaryDetailsControllerTests()
		{
			Dispose(false);
		}
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing && !IsDisposed)
			{
				if (m_mediator != null && !m_mediator.IsDisposed)
					m_mediator.RemoveColleague(m_window);

				if (m_window != null && !m_window.IsDisposed)
					m_window.Dispose();
				m_window = null;

				if (m_mediator != null && !m_mediator.IsDisposed)
					m_mediator.Dispose();
				m_mediator = null;
			}
			IsDisposed = true;
		}
		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		protected bool IsDisposed
		{
			get;
			private set;
		}
		#endregion IDisposable and Gendarme Requirements

		#region Setup and Teardown
		protected override void Init()
		{
			m_application = new MockFwXApp(new MockFwManager { Cache = this.Cache }, null, null);
			m_configFilePath = Path.Combine(DirectoryFinder.FWCodeDirectory, m_application.DefaultConfigurationPathname);
			m_window = new MockFwXWindow(m_application, m_configFilePath);
			((MockFwXWindow)m_window).Init(Cache); // initializes Mediator values
			m_mediator = m_window.Mediator;
			m_window.LoadUI(m_configFilePath); // actually loads UI here; needed for non-null stylesheet

			m_styleSheet = FontHeightAdjuster.StyleSheetFromMediator(m_mediator);
			GenerateStyles();
		}

		protected void GenerateStyles()
		{
			for (int i = 0; i < 5; i++)
			{
				m_styleSheet.Styles.Add(new BaseStyleInfo { Name = string.Format("ParaStyle{0}", i), IsParagraphStyle = true });
				m_styleSheet.Styles.Add(new BaseStyleInfo { Name = string.Format("CharStyle{0}", i), IsParagraphStyle = false });
			}
		}
		#endregion Setup and Teardown

		#region Helpers
		private static IList<StyleComboItem> GetAvailableStyles(DetailsView view)
		{
			var ddStyle = (ComboBox)ReflectionHelper.GetField(view, "dropDownStyle");
			return ddStyle.Items.Cast<StyleComboItem>().ToList();
		}

		private static void AssertShowingCharacterStyles(DetailsView view)
		{
			var styles = GetAvailableStyles(view);

			// The first character style should be (none), specified by null
			Assert.IsNull(styles[0].Style);

			// The rest should be character styles
			for (int i = 1; i < styles.Count; i++)
			{
				Assert.IsTrue(styles[i].Style.IsCharacterStyle);
			}
		}

		private static void AssertShowingParagraphStyles(DetailsView view)
		{
			foreach (var style in GetAvailableStyles(view))
			{
				Assert.IsTrue(style.Style.IsParagraphStyle);
			}
		}
		#endregion Helpers

		#region Sense tests
		[Test]
		public void SenseLoadsParagraphStyles()
		{
			using (var view = new DictionaryDetailsController( // SUT
				new ConfigurableDictionaryNode { DictionaryNodeOptions = new DictionaryNodeSenseOptions() }, m_mediator).View)
			{
				AssertShowingParagraphStyles(view);
			}
		}

		[Test]
		public void NonSenseLoadsCharacterStyles()
		{
			using(var view = new DictionaryDetailsController(new ConfigurableDictionaryNode(), m_mediator).View)
				AssertShowingCharacterStyles(view);
		}

		[Test]
		public void LoadNodeSwitchesStyles()
		{
			// Load character styles
			var node = new ConfigurableDictionaryNode { DictionaryNodeOptions = new DictionaryNodeListOptions() };
			var controller = new DictionaryDetailsController(node, m_mediator);
			AssertShowingCharacterStyles(controller.View);
			controller.View.Dispose();

			// Load paragraph styles
			node.DictionaryNodeOptions = new DictionaryNodeComplexFormOptions();
			controller.LoadNode(node); // SUT
			AssertShowingParagraphStyles(controller.View);
			controller.View.Dispose();

			// Load character styles
			node.DictionaryNodeOptions = new DictionaryNodeWritingSystemOptions();
			controller.LoadNode(node); // SUT
			AssertShowingCharacterStyles(controller.View);
			controller.View.Dispose();
		}
		#endregion Sense tests

		#region List tests (applicable to Writing System as well)
		[Test]
		public void CannotUncheckOnlyCheckedItemInList()
		{

		}

		[Test]
		public void CannotMoveTopItemUp()
		{

		}

		[Test]
		public void CannotMoveBottomItemDown()
		{

		}
		#endregion List tests (applicable to Writing System as well)

		#region Writing System tests
		[Test]
		// REVIEW (Hasso) 2014.02: would we like to permit checking both defaults?  Default Anal + named Vernac?
		public void CheckDefaultWsUnchecksAllOthers()
		{

		}

		[Test]
		public void CheckNamedWsUnchecksDefault()
		{

		}

		[Test]
		public void CheckNamedWsPreservesOtherNamedWss()
		{

		}

		[Test]
		public void CannotReorderDefaultWs()
		{

		}

		[Test]
		public void CannotMoveNamedWsAboveDefault()
		{

		}
		#endregion Writing System tests
	}
}

#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// Original author: MarkS 2010-06-29 TestColumnConfigureDialog.cs

using System.Collections.Generic;
using System.Linq;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;
using XCore;
using SIL.FieldWorks.FDO.FDOTests;

namespace XMLViewsTests
{
	/// <summary></summary>
	[TestFixture]
	public class TestColumnConfigureDialog : BaseTest
	{
		private Mediator m_mediator;
		private FdoCache m_cache;

		[SetUp]
		public void SetUp()
		{
			m_mediator = new Mediator();
			m_mediator.StringTbl = new StringTable("../../DistFiles/Language Explorer/Configuration");
			m_cache = FdoCache.CreateCacheWithNewBlankLangProj(
				new TestProjectId(FDOBackendProviderType.kMemoryOnly, null), "en", "en", "en", new ThreadHelper());
			m_mediator.PropertyTable.SetProperty("cache", m_cache);
		}

		[TearDown]
		public void TearDown()
		{
			m_mediator.Dispose();
			m_cache.ThreadHelper.Dispose();
			m_cache.Dispose();
		}

		#region AfterMovingItemArrowsAreNotImproperlyDisabled
		private ColumnConfigureDialog CreateColumnConfigureDialog()
		{
			// Create window and populate currentColumns with a few items.

			string currentColumns_data = "<root><column layout=\"EntryHeadwordForEntry\" label=\"Headword\" ws=\"$ws=vernacular\" width=\"72000\" sortmethod=\"FullSortKey\" cansortbylength=\"true\" visibility=\"always\" /><column layout=\"LexemeFormForEntry\" label=\"Lexeme Form\" common=\"true\" width=\"72000\" ws=\"$ws=vernacular\" sortmethod=\"MorphSortKey\" cansortbylength=\"true\" visibility=\"always\" transduce=\"LexEntry.LexemeForm.Form\" transduceCreateClass=\"MoStemAllomorph\" /><column layout=\"GlossesForSense\" label=\"Glosses\" multipara=\"true\" width=\"72000\" ws=\"$ws=analysis\" transduce=\"LexSense.Gloss\" cansortbylength=\"true\" visibility=\"always\" /><column layout=\"GrammaticalInfoFullForSense\" headerlabel=\"Grammatical Info.\" chooserFilter=\"external\" label=\"Grammatical Info. (Full)\" multipara=\"true\" width=\"72000\" visibility=\"always\"><dynamicloaderinfo assemblyPath=\"FdoUi.dll\" class=\"SIL.FieldWorks.FdoUi.PosFilter\" /></column></root>";
			XmlDocument currentColumns_document = new XmlDocument();
			currentColumns_document.LoadXml(currentColumns_data);
			List<XmlNode> currentColumns = currentColumns_document.FirstChild.ChildNodes.Cast<XmlNode>().ToList();

			List<XmlNode> possibleColumns = new List<XmlNode>();

			ColumnConfigureDialog window = new ColumnConfigureDialog(possibleColumns, currentColumns, m_mediator,
				m_mediator.StringTbl);

			return window;
		}

		/// <summary>
		/// FWNX-313: flex configure column arrows inappropriately disabling
		/// After moving a current columns item up or down, the up and down buttons
		/// should not both be disabled.
		/// <seealso cref="AfterMovingItemArrowsAreNotImproperlyDisabled_OnUp"/>
		/// </summary>
		[Test]
		public void AfterMovingItemArrowsAreNotImproperlyDisabled_OnDown()
		{
			using (var window = CreateColumnConfigureDialog())
			{
			window.Show();
			window.currentList.Items[1].Selected = true;
			window.moveDownButton.PerformClick();
			Assert.True(window.moveUpButton.Enabled,
				"Up button should not be disabled after moving an item down.");
		}
		}

		/// <see cref="AfterMovingItemArrowsAreNotImproperlyDisabled_OnDown"/>
		[Test]
		public void AfterMovingItemArrowsAreNotImproperlyDisabled_OnUp()
		{
			using (var window = CreateColumnConfigureDialog())
			{
			window.Show();
			window.currentList.Items[1].Selected = true;
			window.moveUpButton.PerformClick();
			Assert.True(window.moveDownButton.Enabled,
				"Down button should not be disabled after moving an item up.");
		}
		}
		#endregion
	}
}

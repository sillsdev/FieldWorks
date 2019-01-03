// Copyright (c) 2010-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using LanguageExplorer.Controls.DetailControls;
using NUnit.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorerTests.Controls.DetailControls
{
	[TestFixture]
	class InnerLabeledMultiStringViewTests : MemoryOnlyBackendProviderTestBase
	{
		private ITsString m_tss;
		private int m_wsEn;
		private int m_wsFr;

		#region Overrides of LcmTestBase
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			m_wsEn = Cache.WritingSystemFactory.get_Engine("en-US").Handle;
			m_wsFr = Cache.WritingSystemFactory.get_Engine("fr").Handle;
		}

		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();

			m_tss = TsStringSerializer.DeserializeTsStringFromXml("<AStr ws='en-US'><Run ws='en-US'>English</Run><Run ws='fr'>french</Run><Run ws='en-US'>English</Run></AStr>",
				Cache.ServiceLocator.GetInstance<WritingSystemManager>());
		}
		#endregion

		[Test]
		public void PasteIntoStringFieldDoesNotFlattenWsStyle()
		{
			var args = new FwPasteFixTssEventArgs(m_tss, new TextSelInfo((IVwSelection)null));
			// Verify that we are testing  with a field of the correct type (if this fails the model changed)
			Assert.AreEqual((int)CellarPropertyType.String, Cache.MetaDataCacheAccessor.GetFieldType(LexEntryTags.kflidImportResidue));
			//SUT
			InnerLabeledMultiStringView.EliminateExtraStyleAndWsInfo(Cache.MetaDataCacheAccessor, args, LexEntryTags.kflidImportResidue);
			string differences;
			Assert.True(TsStringHelper.TsStringsAreEqual(m_tss, args.TsString, out differences), differences);
		}

		[Test]
		public void PasteIntoMultiStringFieldDoesNotFlattenWsStyle()
		{
			var args = new FwPasteFixTssEventArgs(m_tss, new TextSelInfo((IVwSelection)null));
			// Verify that we are testing  with a field of the correct type (if this fails the model changed)
			Assert.AreEqual((int)CellarPropertyType.MultiString, Cache.MetaDataCacheAccessor.GetFieldType(LexSenseTags.kflidGeneralNote));
			//SUT
			InnerLabeledMultiStringView.EliminateExtraStyleAndWsInfo(Cache.MetaDataCacheAccessor, args, LexSenseTags.kflidGeneralNote);
			string differences;
			Assert.True(TsStringHelper.TsStringsAreEqual(m_tss, args.TsString, out differences), differences);
		}

		[Test]
		public void PasteIntoUnicodeFieldFlattensWsStyle()
		{
			var args = new FwPasteFixTssEventArgs(m_tss, new TextSelInfo((IVwSelection)null));
			// Verify that we are testing  with a field of the correct type (if this fails the model changed)
			Assert.AreEqual((int)CellarPropertyType.Unicode, Cache.MetaDataCacheAccessor.GetFieldType(LexEntryTags.kflidLiftResidue));
			//SUT
			InnerLabeledMultiStringView.EliminateExtraStyleAndWsInfo(Cache.MetaDataCacheAccessor, args, LexEntryTags.kflidLiftResidue);
			string differences;
			Assert.False(TsStringHelper.TsStringsAreEqual(m_tss, args.TsString, out differences), differences);
			Assert.That(differences, Is.StringContaining("TsStrings have different number of runs"));
		}

		[Test]
		public void PasteIntoMultiUnicodeFieldFlattensWsStyle()
		{
			var args = new FwPasteFixTssEventArgs(m_tss, new TextSelInfo((IVwSelection)null));
			// Verify that we are testing  with a field of the correct type (if this fails the model changed)
			Assert.AreEqual((int)CellarPropertyType.MultiUnicode, Cache.MetaDataCacheAccessor.GetFieldType(LexEntryTags.kflidCitationForm));
			//SUT
			InnerLabeledMultiStringView.EliminateExtraStyleAndWsInfo(Cache.MetaDataCacheAccessor, args, LexEntryTags.kflidCitationForm);
			string differences;
			Assert.False(TsStringHelper.TsStringsAreEqual(m_tss, args.TsString, out differences), differences);
			Assert.That(differences, Is.StringContaining("TsStrings have different number of runs"));
		}

		[Test]
		public void InnerViewRefreshesWhenRefreshIsPending()
		{
			ILexEntry entry = null;
			var flid = 5035001; // MoForm flid
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				entry.LexemeFormOA = morph;
			});

			var dummyForm = new Form();
			var view = new LabeledMultiStringView(entry.LexemeFormOA.Hvo, flid, WritingSystemServices.kwsVern, false, true);
			dummyForm.Controls.Add(view);
			var innerView = view.InnerView;
			view.FinishInit();
			innerView.Cache = Cache;

			// Access the Handle of the innerView to construct the RootBox.
			var handle = innerView.Handle;
			Assert.IsFalse(innerView.Visible);
			Assert.IsFalse(innerView.RefreshPending);
			view.WritingSystemsToDisplay = WritingSystemServices.GetWritingSystemList(Cache, WritingSystemServices.kwsVern, false);
			view.RefreshDisplay();
			// The flag gets set because the view is not yet visible, so the display cannot refresh.
			Assert.IsTrue(innerView.RefreshPending);
			// Trigger the display to refresh by making the form visible.
			dummyForm.Visible = true;
			Assert.IsFalse(innerView.RefreshPending);
			view.Dispose();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () => entry.Delete());
		}
	}
}
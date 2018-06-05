// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer.Controls.DetailControls;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.RootSites;

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
			// Veryify that we are testing  with a field of the correct type (if this fails the model changed)
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
			// Veryify that we are testing  with a field of the correct type (if this fails the model changed)
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
			// Veryify that we are testing  with a field of the correct type (if this fails the model changed)
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
			// Veryify that we are testing  with a field of the correct type (if this fails the model changed)
			Assert.AreEqual((int)CellarPropertyType.MultiUnicode, Cache.MetaDataCacheAccessor.GetFieldType(LexEntryTags.kflidCitationForm));
			//SUT
			InnerLabeledMultiStringView.EliminateExtraStyleAndWsInfo(Cache.MetaDataCacheAccessor, args, LexEntryTags.kflidCitationForm);
			string differences;
			Assert.False(TsStringHelper.TsStringsAreEqual(m_tss, args.TsString, out differences), differences);
			Assert.That(differences, Is.StringContaining("TsStrings have different number of runs"));
		}
	}
}

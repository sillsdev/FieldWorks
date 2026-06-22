// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Text;

namespace SIL.FieldWorks.XWorks.xWorksTests
{
	[TestFixture]
	public class LexicalEditPocMapperTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		[Test]
		public void CreateDto_LexEntry_MapsLexemeFormMorphTypeAndFirstSenseGloss()
		{
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var lexemeForm = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entry.LexemeFormOA = lexemeForm;
			lexemeForm.Form.set_String(
				Cache.DefaultVernWs,
				TsStringUtils.MakeString("kazi", Cache.DefaultVernWs));
			entry.CitationForm.VernacularDefaultWritingSystem =
				TsStringUtils.MakeString("citation", Cache.DefaultVernWs);
			lexemeForm.MorphTypeRA =
				Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphPrefix);

			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(sense);
			sense.Gloss.set_String(Cache.DefaultAnalWs, "to work");

			var dto = LexicalEditPocMapper.CreateDto(entry, Cache);

			Assert.That(dto, Is.Not.Null);
			Assert.That(dto.LexemeForm.Count, Is.EqualTo(1));
			Assert.That(dto.LexemeForm[0].Value, Is.EqualTo("kazi"));
			Assert.That(dto.MorphTypeKey, Is.EqualTo("prefix"));
			Assert.That(dto.SenseGloss.Count, Is.EqualTo(1));
			Assert.That(dto.SenseGloss[0].Value, Is.EqualTo("to work"));
		}

		[Test]
		public void CreateDto_NonLexEntry_ReturnsNull()
		{
			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			Assert.That(LexicalEditPocMapper.CreateDto(sense, Cache), Is.Null);
		}
	}
}

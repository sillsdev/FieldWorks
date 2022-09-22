// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using Rhino.Mocks;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using XCore;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// Very incomplete test of combo handlers...so far just for bugs we fixed.
	/// </summary>
	[TestFixture]
	public class ComboHandlerTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private Mediator m_mediator;
		private PropertyTable m_propertyTable;

		#region Overrides of MemoryOnlyBackendProviderRestoredForEachTestTestBase

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override to start an undoable UOW.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();

			m_mediator = new Mediator();
			m_propertyTable = new PropertyTable(m_mediator);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override to end the undoable UOW, Undo everything, and 'commit',
		/// which will essentially clear out the Redo stack.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestTearDown()
		{
			if (m_mediator != null)
			{
				m_mediator.Dispose();
				m_mediator = null;
			}
			if (m_propertyTable != null)
			{
				m_propertyTable.Dispose();
				m_propertyTable = null;
			}
			base.TestTearDown();
		}

		#endregion

		/// <summary>
		/// Test the case where an analysis is created from a sense with no MSA, hence the bundle has no MSA,
		/// then the user selects that same sense; this method should return true so we will set the MSA of
		/// the analysis (LT-14574).
		/// </summary>
		[Test]
		public void EntryHandler_NeedSelectSame_SelectSenseWhenAnalysisHasNoPos_ReturnsTrue()
		{
			// Make an entry with a morph and a sense with no MSA.
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entry.LexemeFormOA = morph;
			morph.Form.SetVernacularDefaultWritingSystem("kick");
			morph.MorphTypeRA =
				Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(MoMorphTypeTags.kguidMorphRoot);
			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(sense);
			sense.Gloss.SetAnalysisDefaultWritingSystem("strike with foot");

			// Make an analysis from that MSA.
			var wf = Cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create();
			wf.Form.SetVernacularDefaultWritingSystem("kick");
			var wa = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
			wf.AnalysesOC.Add(wa);
			var mb = Cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>().Create();
			wa.MorphBundlesOS.Add(mb);
			mb.SenseRA = sense;
			mb.MorphRA = morph;

			// Make a sandbox and sut
			InterlinLineChoices lineChoices = InterlinLineChoices.DefaultChoices(Cache.LangProject,
				Cache.DefaultVernWs, Cache.DefaultAnalWs, InterlinLineChoices.InterlinMode.Analyze);
			using (var sut = new SandboxBase.IhMissingEntry(null))
			{
				using (var sandbox = new SandboxBase(Cache, m_mediator, m_propertyTable, null, lineChoices, wa.Hvo))
				{
					sut.SetSandboxForTesting(sandbox);
					var mockList = MockRepository.GenerateMock<IComboList>();
					sut.SetComboListForTesting(mockList);
					sut.SetMorphForTesting(0);
					sut.LoadMorphItems();
					Assert.That(sut.NeedSelectSame(), Is.True);
				}

				// But if it already has an MSA it is not true.
				var msa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
				entry.MorphoSyntaxAnalysesOC.Add(msa);
				sense.MorphoSyntaxAnalysisRA = msa;
				mb.MsaRA = msa;
				using (var sandbox = new SandboxBase(Cache, m_mediator, m_propertyTable, null, lineChoices, wa.Hvo))
				{
					sut.SetSandboxForTesting(sandbox);
					Assert.That(sut.NeedSelectSame(), Is.False);
				}
			}
		}

		[Test]
		public void MakeCombo_SelectionIsInvalid_Throws()
		{
			var vwsel = MockRepository.GenerateMock<IVwSelection>();
			vwsel.Stub(s => s.IsValid).Return(false);
			Assert.That(() => SandboxBase.InterlinComboHandler.MakeCombo(null, vwsel, null, true), Throws.ArgumentException);
		}

		[Test]
		public void ChooseAnalysisHandler_UsesDefaultSenseWhenSenseRAIsNull()
		{
			// Mock the various model objects to avoid having to create entries,
			// senses, texts, analysis and morph bundles when we really just need to test
			// the behaviour around a specific set of conditions
			var glossString = MockRepository.GenerateStub<IMultiUnicode>();
			glossString.Stub(g => g.get_String(Cache.DefaultAnalWs))
				.Return(TsStringUtils.MakeString("hello", Cache.DefaultAnalWs));
			var formString = MockRepository.GenerateStub<IMultiString>();
			formString.Stub(f => f.get_String(Cache.DefaultVernWs))
				.Return(TsStringUtils.MakeString("hi", Cache.DefaultVernWs));
			var sense = MockRepository.GenerateStub<ILexSense>();
			sense.Stub(s => s.Gloss).Return(glossString);
			var bundle = MockRepository.GenerateStub<IWfiMorphBundle>();
			bundle.Stub(b => b.Form).Return(formString);
			bundle.Stub(b => b.DefaultSense).Return(sense);
			var bundleList = MockRepository.GenerateStub<ILcmOwningSequence<IWfiMorphBundle>>();
			bundleList.Stub(x => x.Count).Return(1);
			bundleList[0] = bundle;
			var wfiAnalysis = MockRepository.GenerateStub<IWfiAnalysis>();
			wfiAnalysis.Stub(x => x.MorphBundlesOS).Return(bundleList);
			// SUT
			var result = ChooseAnalysisHandler.MakeAnalysisStringRep(wfiAnalysis, Cache, false,
				Cache.DefaultVernWs);
			// Verify that the form value of the IWfiMorphBundle is displayed (test verification)
			Assert.That(result.Text, Does.Contain("hi"));
			// Verify that the sense reference in the bundle is null (key condition for the test)
			Assert.That(bundle.SenseRA, Is.Null);
			// Verify that the gloss for the DefaultSense is displayed (key test data)
			Assert.That(result.Text, Does.Contain("hello"));
		}
	}
}

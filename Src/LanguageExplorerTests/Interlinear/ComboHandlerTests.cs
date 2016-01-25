// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using NUnit.Framework;
using Rhino.Mocks;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;

namespace LanguageExplorerTests.Interlinear
{
	/// <summary>
	/// Very incomplete test of combo handlers...so far just for bugs we fixed.
	/// </summary>
	[TestFixture]
	public class ComboHandlerTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private IPropertyTable m_propertyTable;
		private IPublisher m_publisher;
		private ISubscriber m_subscriber;

		#region Overrides of MemoryOnlyBackendProviderRestoredForEachTestTestBase

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override to start an undoable UOW.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();

			PubSubSystemFactory.CreatePubSubSystem(out m_publisher, out m_subscriber);
			m_propertyTable = PropertyTableFactory.CreatePropertyTable(m_publisher);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override to end the undoable UOW, Undo everything, and 'commit',
		/// which will essentially clear out the Redo stack.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestTearDown()
		{
			if (m_propertyTable != null)
			{
				m_propertyTable.Dispose();
				m_propertyTable = null;
			}
			m_publisher = null;
			m_subscriber = null;
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
				using (var sandbox = new SandboxBase(Cache, null, lineChoices, wa.Hvo))
				{
					sandbox.InitializeFlexComponent(m_propertyTable, m_publisher, m_subscriber);
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
				using (var sandbox = new SandboxBase(Cache, null, lineChoices, wa.Hvo))
				{
					sandbox.InitializeFlexComponent(m_propertyTable, m_publisher, m_subscriber);
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
	}
}

using System;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.IText
{
#if RANDYTODO
	/// <summary>
	/// Very incomplete test of combo handlers...so far just for bugs we fixed.
	/// </summary>
	[TestFixture]
	public class ComboHandlerTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private Mediator m_mediator;
		private IPropertyTable m_propertyTable;

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
			m_propertyTable = PropertyTableFactory.CreatePropertyTable(new MockPublisher());
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
					var mockList = new MockComboList();
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
	}

	class MockComboList : IComboList
	{
		public MockComboList()
		{
			SelectedIndexChanged += OnSelectedIndexChanged; // stupid compiler insists event must be used.
			SelectedIndexChanged(this, new EventArgs());
		}

		private void OnSelectedIndexChanged(object sender, EventArgs eventArgs)
		{
		}

		public event EventHandler SelectedIndexChanged;
		public ComboBoxStyle DropDownStyle { get; set; }
		public int SelectedIndex { get; set; }
		public string Text { get; set; }
		public FwListBox.ObjectCollection Items { get {throw new NotImplementedException();} }
		public int FindStringExact(string str)
		{
			throw new NotImplementedException();
		}

		public ILgWritingSystemFactory WritingSystemFactory { get; set; }
		public object SelectedItem { get; set; }
		public IVwStylesheet StyleSheet { get; set; }
	}
#endif
}

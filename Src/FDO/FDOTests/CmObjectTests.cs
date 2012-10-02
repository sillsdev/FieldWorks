using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// Tests related to the methods on CmObject itself.
	/// </summary>
	[TestFixture]
	public class CmObjectTests : MemoryOnlyBackendProviderTestBase
	{
		/// <summary>
		/// The OwningFlid method.
		/// </summary>
		[Test]
		public void TestOwningFlid()
		{
			Assert.AreEqual(0, Cache.LangProject.OwningFlid); // no owner
			Assert.AreEqual(LangProjectTags.kflidLexDb, Cache.LangProject.LexDbOA.OwningFlid);
			// atomic, on an owner with many
			var servLoc = Cache.ServiceLocator;
			ILexEntry entry = null;
			ICmResource resource = null;
			ILexSense sense1 = null;
			ILexSense sense2 = null;
			ILexSense sense3 = null;
			IMoForm form1 = null;
			IMoForm form2 = null;
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler, () =>
					{
						resource = servLoc.GetInstance<ICmResourceFactory>().Create();
						Cache.LangProject.LexDbOA.ResourcesOC.Add(resource);
						entry = servLoc.GetInstance<ILexEntryFactory>().Create();
						sense1 = servLoc.GetInstance<ILexSenseFactory>().Create();
						entry.SensesOS.Add(sense1);
						sense2 = servLoc.GetInstance<ILexSenseFactory>().Create();
						entry.SensesOS.Add(sense2);
						sense3 = servLoc.GetInstance<ILexSenseFactory>().Create();
						entry.SensesOS.Add(sense3);
						form1 = servLoc.GetInstance<IMoStemAllomorphFactory>().Create();
						entry.AlternateFormsOS.Add(form1);
						form2 = servLoc.GetInstance<IMoStemAllomorphFactory>().Create();
						entry.AlternateFormsOS.Add(form2);
					}
				);
			Assert.AreEqual(LexDbTags.kflidResources, resource.OwningFlid); // owning collection
			Assert.AreEqual(LexEntryTags.kflidSenses, sense1.OwningFlid); // owning sequence
			Assert.AreEqual(LexEntryTags.kflidSenses, sense2.OwningFlid); // owning sequence, middle
			Assert.AreEqual(LexEntryTags.kflidSenses, sense3.OwningFlid); // owning sequence, last
			Assert.AreEqual(LexEntryTags.kflidAlternateForms, form1.OwningFlid); // owning sequence, not longest
			Assert.AreEqual(LexEntryTags.kflidAlternateForms, form2.OwningFlid); // owning sequence, not longest or first
		}

		/// <summary>
		/// Test the AllReferencedObjects method.
		/// </summary>
		[Test]
		public void AllReferencedObjects()
		{
			UndoableUnitOfWorkHelper.Do("Undo stuff", "Redo stuff", m_actionHandler,
				() =>
					{
						var entry1 = MakeEntry("kick", "strike with foot");
						var sense1 = entry1.SensesOS[0];
						var referencedObjectCollector = new List<ICmObject>();
						entry1.AllReferencedObjects(referencedObjectCollector);
						Assert.That(referencedObjectCollector, Is.Empty);

						// atomic
						var mb = MakeBundle("kick", sense1);
						mb.AllReferencedObjects(referencedObjectCollector);
						Assert.That(referencedObjectCollector, Has.Member(sense1).And.Count.EqualTo(1));

						// sequence.
						var entry2 = MakeEntry("punch", "strike with fist");
						entry2.MainEntriesOrSensesRS.Add(entry1);
						entry2.AllReferencedObjects(referencedObjectCollector);
						Assert.That(referencedObjectCollector, Has.Member(sense1), "still there...checks it adds things");
						Assert.That(referencedObjectCollector, Has.Member(entry1).And.Count.EqualTo(2));
						// colletion
						var dtList = Cache.LangProject.LexDbOA.DomainTypesOA;
						if (dtList == null)
						{
							dtList = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
							Cache.LangProject.LexDbOA.DomainTypesOA = dtList;
						}
						var item = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
						dtList.PossibilitiesOS.Add(item);
						sense1.DomainTypesRC.Add(item);
						referencedObjectCollector.Clear();
						sense1.AllReferencedObjects(referencedObjectCollector);
						Assert.That(referencedObjectCollector, Has.Member(item).And.Count.EqualTo(1));
					});
		}

		/// <summary>
		/// Copied from StringServicesTests; possibly best for each test set to have own utility functions?
		/// </summary>
		private ILexEntry MakeEntry(string lf, string gloss)
		{
			ILexEntry entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var form = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entry.LexemeFormOA = form;
			form.Form.VernacularDefaultWritingSystem =
				Cache.TsStrFactory.MakeString(lf, Cache.DefaultVernWs);
			AddSense(entry, gloss);
			return entry;
		}

		private ILexSense AddSense(ILexEntry entry, string gloss)
		{
			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(sense);
			sense.Gloss.AnalysisDefaultWritingSystem = Cache.TsStrFactory.MakeString(gloss,
				Cache.DefaultAnalWs);
			return sense;
		}

		private IWfiMorphBundle MakeBundle(string wordform, ILexSense sense)
		{
			var wf = Cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create();
			wf.Form.VernacularDefaultWritingSystem = Cache.TsStrFactory.MakeString(wordform, Cache.DefaultVernWs);
			var wa = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
			wf.AnalysesOC.Add(wa);
			var mb = Cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>().Create();
			wa.MorphBundlesOS.Add(mb);
			mb.SenseRA = sense;
			return mb;
		}
	}
}

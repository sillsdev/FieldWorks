using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.CoreTests;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO.Infrastructure.Impl;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// Test functionality related to Client-Server operation
	/// </summary>
	[TestFixture]
	public class ClientServerTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary>
		/// Test the OkToReconcileChanges() method.
		/// </summary>
		[Test]
		public void VerifyOkToReconcileChanges()
		{
			// Create some test data to play with.
			var entry1 = MakeEntry("kick", "strike with foot");
			var sense1_2 = AddSense(entry1, "propel football");
			var entry2 = MakeEntry("punch", "strike with hand");
			var sense1 = entry1.SensesOS[0];
			// From here on w want to manage our own units of work.
			m_actionHandler.EndUndoTask();
			// Mark this as the starting point.
			var undoManager = Cache.ServiceLocator.GetInstance<IUndoStackManager>();
			undoManager.Save();
			UndoableUnitOfWorkHelper.Do("undo change gloss", "redo", m_actionHandler,
				()=>sense1.Gloss.AnalysisDefaultWritingSystem =
					Cache.TsStrFactory.MakeString("strike with boot", Cache.DefaultAnalWs));
			var cmoFactory = Cache.ServiceLocator.GetInstance<ICmObjectSurrogateFactory>();
			var modified = new List<ICmObjectSurrogate>();
			modified.Add(GetSurrogate(sense1, cmoFactory));
			m_actionHandler.Undo();
			var uowService = (UnitOfWorkService) undoManager;
			var reconciler = uowService.Reconciler(new List<ICmObjectSurrogate>(), modified, new List<ICmObjectId>());

			// Only other client made changes
			Assert.That(reconciler.OkToReconcileChanges(), Is.True,
				"we should be able to make any change if there are no outstanding changes.");

			// Both clients changed the same thing
			UndoableUnitOfWorkHelper.Do("undo change gloss", "redo", m_actionHandler,
				() => sense1.Gloss.AnalysisDefaultWritingSystem =
					Cache.TsStrFactory.MakeString("strike with shoe", Cache.DefaultAnalWs));
			reconciler = uowService.Reconciler(new List<ICmObjectSurrogate>(), modified, new List<ICmObjectId>());
			Assert.That(reconciler.OkToReconcileChanges(), Is.False,
				"we should not be able to make a change if we have a conflicting change to the same object.");
			m_actionHandler.Undo();
			reconciler = uowService.Reconciler(new List<ICmObjectSurrogate>(), modified, new List<ICmObjectId>());
			Assert.That(reconciler.OkToReconcileChanges(), Is.True);

			// Foreign modified, local deleted.
			UndoableUnitOfWorkHelper.Do("undo delete sense", "redo", m_actionHandler,
				() => entry1.SensesOS.RemoveAt(0));
			reconciler = uowService.Reconciler(new List<ICmObjectSurrogate>(), modified, new List<ICmObjectId>());
			Assert.That(reconciler.OkToReconcileChanges(), Is.False,
				"we should not be able to make a change if we have deleted an object that the change modifies.");
			m_actionHandler.Undo();
			reconciler = uowService.Reconciler(new List<ICmObjectSurrogate>(), modified, new List<ICmObjectId>());
			Assert.That(reconciler.OkToReconcileChanges(), Is.True);

			// Local modified, foreign deleted.
			UndoableUnitOfWorkHelper.Do("undo change gloss", "redo", m_actionHandler,
				() => sense1.Gloss.AnalysisDefaultWritingSystem =
					Cache.TsStrFactory.MakeString("strike with boot", Cache.DefaultAnalWs));
			var listOfSense1Id = new List<ICmObjectId>();
			listOfSense1Id.Add(sense1.Id);
			reconciler = uowService.Reconciler(new List<ICmObjectSurrogate>(), new List<ICmObjectSurrogate>(), listOfSense1Id);
			Assert.That(reconciler.OkToReconcileChanges(), Is.False,
				"we should not be able to make a change if it involves deleting an object that we have modified.");
			m_actionHandler.Undo();
			reconciler = uowService.Reconciler(new List<ICmObjectSurrogate>(), modified, new List<ICmObjectId>());
			Assert.That(reconciler.OkToReconcileChanges(), Is.True);

			// We added a reference to something they deleted (on a new object).
			IWfiMorphBundle bundle = null;
			UndoableUnitOfWorkHelper.Do("undo make ref", "redo", m_actionHandler,
				() => bundle = MakeBundle("kick", sense1));
			// We will use these surrogates in a later test.
			var newObjectSurrogates = new List<ICmObjectSurrogate>();
			newObjectSurrogates.Add(GetSurrogate(bundle.Owner.Owner, cmoFactory));
			newObjectSurrogates.Add(GetSurrogate(bundle.Owner,cmoFactory));
			newObjectSurrogates.Add(GetSurrogate(bundle, cmoFactory));
			reconciler = uowService.Reconciler(new List<ICmObjectSurrogate>(), new List<ICmObjectSurrogate>(), listOfSense1Id);
			Assert.That(reconciler.OkToReconcileChanges(), Is.False,
				"we should not be able to make a change if it involves deleting an object that one we have added refers to.");

			// We added a reference to something they deleted (on an existing object).
			UndoableUnitOfWorkHelper.Do("undo clear ref", "redo", m_actionHandler,
				() => bundle.SenseRA = null);
			undoManager.Save();
			reconciler = uowService.Reconciler(new List<ICmObjectSurrogate>(), new List<ICmObjectSurrogate>(), listOfSense1Id);
			Assert.That(reconciler.OkToReconcileChanges(),Is.True);
			UndoableUnitOfWorkHelper.Do("undo set ref", "redo", m_actionHandler,
				() => bundle.SenseRA = sense1);
			reconciler = uowService.Reconciler(new List<ICmObjectSurrogate>(), new List<ICmObjectSurrogate>(), listOfSense1Id);
			Assert.That(reconciler.OkToReconcileChanges(), Is.False,
				"we should not be able to make a change if it involves deleting an object that we have made a reference to.");
			m_actionHandler.Undo(); // setting the sense of the bundle
			m_actionHandler.Undo(); // clearing the sense of the bundle
			m_actionHandler.Undo(); // creating the bundle.
			undoManager.Save(); // back to square 1.

			// They added a reference to something we deleted
			UndoableUnitOfWorkHelper.Do("undo delete sense", "redo", m_actionHandler,
				() => entry1.SensesOS.RemoveAt(0));
			// Now pretend THEY made the change adding the objects referring to sense1, which we just deleted.
			reconciler = uowService.Reconciler(newObjectSurrogates, new List<ICmObjectSurrogate>(), new List<ICmObjectId>());
			Assert.That(reconciler.OkToReconcileChanges(), Is.False,
				"we should not be able to make a change if it involves adding an object that refers to an object we have deleted.");

			// This is cheating a little bit, because we're passing as modified objects things not in our db at all.
			// But it exercises the relevant code, making sure we check the modified objects for refs to our deleted ones.
			reconciler = uowService.Reconciler(new List<ICmObjectSurrogate>(), newObjectSurrogates, new List<ICmObjectId>());
			Assert.That(reconciler.OkToReconcileChanges(), Is.False,
				"we should not be able to make a change if it involves adding a ref to an object we have deleted.");
		}

		/// <summary>
		/// Verifies both clients can create an entry.
		/// </summary>
		[Test]
		public void ReconcileCollectionChanges()
		{
			int entryCount = Cache.LangProject.LexDbOA.Entries.Count();
			var destroy = MakeEntry("destroy", "wipe out");
			var destroyId = destroy.Id;
			var remove = MakeEntry("remove", "get rid of");
			var removeId = remove.Id;
			ILexEntry right = null;
			ICmObjectId rightId = null;
			ILexEntry kick = null;
			List<ICmObjectSurrogate> newbiesClockwise;
			List<ICmObjectSurrogate> dirtballsClockwise;
			List<ICmObjectId> gonersClockwise;
			GetEffectsOfChange(() =>
								{
									right = MakeEntry("right", "correct");
									destroy.Delete();
								},
				() => rightId = right.Id,
				out newbiesClockwise, out dirtballsClockwise, out gonersClockwise);
			UndoableUnitOfWorkHelper.Do("undo", "redo", m_actionHandler,
				() =>
					{
						kick = MakeEntry("kick", "strike with foot");
						remove.Delete();
					});
			var uowService = Cache.ServiceLocator.GetInstance<IUnitOfWorkService>();
			var reconciler = ((UnitOfWorkService)uowService).Reconciler(newbiesClockwise, dirtballsClockwise, gonersClockwise);
			var notifiee = new Notifiee();
			Cache.DomainDataByFlid.AddNotification(notifiee);
			Assert.That(reconciler.OkToReconcileChanges(), Is.True);
			reconciler.ReconcileForeignChanges(); // In effect the other client added 'right' and deleted 'destroy'.

			Assert.IsTrue(Cache.LangProject.LexDbOA.Entries.Contains(kick));
			right = (ILexEntry)Cache.ServiceLocator.GetObject(rightId); // will throw if it doesn't exist.
			Assert.IsTrue(Cache.LangProject.LexDbOA.Entries.Contains(right));
			Assert.IsFalse(Cache.LangProject.LexDbOA.Entries.Contains(remove));
			Assert.IsFalse(Cache.ServiceLocator.ObjectRepository.IsValidObjectId(destroyId.Guid));
			// One way to be sure 'destroy' really got destroyed.
			Assert.That(Cache.LangProject.LexDbOA.Entries.Count(), Is.EqualTo(entryCount + 2));

			// See if we got PropChanged notifications on 'Entries'
			int flidEntries = Cache.MetaDataCache.GetFieldId2(LexDbTags.kClassId, "Entries", false);
			notifiee.CheckChangesWeaker(
				new[] {new ChangeInformationTest(Cache.LangProject.LexDbOA.Hvo, flidEntries, 0, entryCount + 2, entryCount + 2)},
				"missing Entries propcount");

			// adding kick; right should still be there. Remove should come back, Destroy still be gone.
			m_actionHandler.Undo();
			Assert.IsFalse(Cache.LangProject.LexDbOA.Entries.Contains(kick));
			Assert.IsTrue(Cache.LangProject.LexDbOA.Entries.Contains(right));
			Assert.IsTrue(Cache.LangProject.LexDbOA.Entries.Contains(remove));
			Assert.That(Cache.LangProject.LexDbOA.Entries.Count(), Is.EqualTo(entryCount + 2));

			m_actionHandler.Redo(); // restore kick, re-delete Remove.
			Assert.IsTrue(Cache.LangProject.LexDbOA.Entries.Contains(kick));
			Assert.IsTrue(Cache.LangProject.LexDbOA.Entries.Contains(right));
			Assert.IsFalse(Cache.LangProject.LexDbOA.Entries.Contains(remove));
			Assert.That(Cache.LangProject.LexDbOA.Entries.Count(), Is.EqualTo(entryCount + 2));
		}

		/// <summary>
		/// Verifies we can modify two senses of the same entry (in particular, that the
		/// conflicting modify times on the Entry can be handled) when the foreign UOW
		/// has an EARLIER modify time than the local one
		/// </summary>
		[Test]
		public void ModifiedDifferentSenses_EarlierTime()
		{
			var entry1 = MakeEntry("right", "correct");
			var senseCorrect = entry1.SensesOS[0];
			var senseClockwise = AddSense(entry1, "clockwise");
			//List<ICmObjectSurrogate> newbiesCorrect;
			//List<ICmObjectSurrogate> dirtballsCorrect;
			//List<ICmObjectId> gonersCorrect;
			//GetEffectsOfChange(() => SetString(senseCorrect.Definition, Cache.DefaultAnalWs, "proper, correct, free from mistakes"),
			//    out newbiesCorrect, out dirtballsCorrect, out gonersCorrect);

			DateTime originalTime = DateTime.Now;

			DateTime oldTime = DateTime.Now;
			List<ICmObjectSurrogate> newbiesClockwise;
			List<ICmObjectSurrogate> dirtballsClockwise;
			List<ICmObjectId> gonersClockwise;
			GetEffectsOfChange(() =>
								{
									// Getting the original date modified has to be inside this block,
									// after the modify time is first set at the end of the UOW in which
									// the entry is created.
									originalTime = entry1.DateModified;
									WaitForTimeToChange(originalTime); // make sure the mod time in the foreign UOW is later.
									SetString(senseClockwise.Definition, Cache.DefaultAnalWs,
										"the clockwise direction (looking down from above)");
								},
									() => oldTime = entry1.DateModified,
				out newbiesClockwise, out dirtballsClockwise, out gonersClockwise);

			Assert.That(oldTime, Is.GreaterThan(originalTime));
			WaitForTimeToChange(oldTime);

			Save();
			UndoableUnitOfWorkHelper.Do("undo", "redo", m_actionHandler,
				() => SetString(senseCorrect.Definition, Cache.DefaultAnalWs, "proper, correct, free from mistakes"));
			var newTime = entry1.DateModified; // should be definitely after oldTime.
			Assert.That(newTime, Is.GreaterThan(oldTime));
			var uowService = Cache.ServiceLocator.GetInstance<IUnitOfWorkService>();
			var reconciler = ((UnitOfWorkService)uowService).Reconciler(newbiesClockwise, dirtballsClockwise, gonersClockwise);
			Assert.That(reconciler.OkToReconcileChanges(), Is.True);
			reconciler.ReconcileForeignChanges();

			Assert.That(entry1.DateModified, Is.EqualTo(newTime));
			Assert.That(entry1.SensesOS[0], Is.EqualTo(senseCorrect));
			Assert.That(entry1.SensesOS[1], Is.EqualTo(senseClockwise));
			Assert.That(senseCorrect.Definition.AnalysisDefaultWritingSystem.Text, Is.EqualTo("proper, correct, free from mistakes"));
			Assert.That(senseClockwise.Definition.AnalysisDefaultWritingSystem.Text,
				Is.EqualTo("the clockwise direction (looking down from above)"));

			m_actionHandler.Undo();
			// from the foreign change, not the previous time when we actually made the change.
			Assert.That(IsTimeNearlyEqual(entry1.DateModified, oldTime), Is.True);
			Assert.That(senseClockwise.Definition.AnalysisDefaultWritingSystem.Text,
				Is.EqualTo("the clockwise direction (looking down from above)"), "foreign change should not be undone");
			Assert.That(senseCorrect.Definition.AnalysisDefaultWritingSystem.Length, Is.EqualTo(0), "local change should be undone");
		}

		/// <summary>
		/// Verifies we can modify two senses of the same entry (in particular, that the
		/// conflicting modify times on the Entry can be handled) when the foreign UOW has
		/// a LATER modify time than the local one
		/// </summary>
		[Test]
		public void ModifiedDifferentSenses_LaterTime()
		{
			var entry1 = MakeEntry("right", "correct");
			var senseCorrect = entry1.SensesOS[0];
			var senseClockwise = AddSense(entry1, "clockwise");

			DateTime originalTime = DateTime.Now;

			DateTime oldTime = DateTime.Now;
			List<ICmObjectSurrogate> newbiesClockwise;
			List<ICmObjectSurrogate> dirtballsClockwise;
			List<ICmObjectId> gonersClockwise;

			GetEffectsOfChange(() =>
			{
				// Getting the original date modified has to be inside this block,
				// after the modify time is first set at the end of the UOW in which
				// the entry is created.
				originalTime = entry1.DateModified;
				WaitForTimeToChange(originalTime); // make sure the mod time in the foreign UOW is later.
				SetString(senseClockwise.Definition, Cache.DefaultAnalWs,
					"the clockwise direction (looking down from above)");
			},
									() => oldTime = entry1.DateModified,
				out newbiesClockwise, out dirtballsClockwise, out gonersClockwise);

			Assert.That(oldTime, Is.GreaterThan(originalTime));
			WaitForTimeToChange(oldTime);

			// The above test simulates the behavior when the foreign UOW has an EARLIER modify time than the local one.
			// Now we want to simulate it having a LATER one. This is more unusual, but can happen if our change was actually
			// made first, but theirs got saved first.
			var entryDirtball = (from db in dirtballsClockwise where db.Id == entry1.Id select db).First();
			var xml = entryDirtball.XML;
			var element = XElement.Parse(xml);
			var timeElt = element.Element("DateModified");
			var foreignTime = DateTime.Now + TimeSpan.FromSeconds(20);
			timeElt.Attribute("val").SetValue(ReadWriteServices.FormatDateTime(foreignTime.ToUniversalTime()));
			entryDirtball.Reset(entryDirtball.Classname, element.ToString());
			UndoableUnitOfWorkHelper.Do("undo", "redo", m_actionHandler, () =>
				SetString(senseClockwise.Definition, Cache.DefaultAnalWs, ""));// so we can see that the foreign UOW modifies it.

			Save();
			UndoableUnitOfWorkHelper.Do("undo", "redo", m_actionHandler,
				() => SetString(senseCorrect.Definition, Cache.DefaultAnalWs, "proper, correct, free from errors"));
			var newTime = entry1.DateModified; // should be definitely after oldTime.
			Assert.That(newTime, Is.GreaterThan(oldTime));
			Assert.That(newTime, Is.LessThan(foreignTime)); // might not be if stepping in debugger

			var uowService = Cache.ServiceLocator.GetInstance<IUnitOfWorkService>();
			var reconciler = ((UnitOfWorkService)uowService).Reconciler(newbiesClockwise, dirtballsClockwise, gonersClockwise);
			Assert.That(reconciler.OkToReconcileChanges(), Is.True);
			reconciler.ReconcileForeignChanges();

			Assert.That(IsTimeNearlyEqual(entry1.DateModified, foreignTime), Is.True, "larger foreign time should win");
			Assert.That(entry1.SensesOS[0], Is.EqualTo(senseCorrect));
			Assert.That(entry1.SensesOS[1], Is.EqualTo(senseClockwise));
			Assert.That(senseCorrect.Definition.AnalysisDefaultWritingSystem.Text, Is.EqualTo("proper, correct, free from errors"));
			Assert.That(senseClockwise.Definition.AnalysisDefaultWritingSystem.Text,
				Is.EqualTo("the clockwise direction (looking down from above)"));

			m_actionHandler.Undo();
			// from the foreign change, not the previous time when we actually made the change.
			Assert.That(IsTimeNearlyEqual(entry1.DateModified, foreignTime), Is.True, "Undo can't set back before foreign time");
			Assert.That(senseClockwise.Definition.AnalysisDefaultWritingSystem.Text,
				Is.EqualTo("the clockwise direction (looking down from above)"), "foreign change should not be undone");
			Assert.That(senseCorrect.Definition.AnalysisDefaultWritingSystem.Length, Is.EqualTo(0), "local change should be undone");
		}

		/// <summary>
		/// Not more than a ms off...rounding errors because we only store it to the MS in XML.
		/// </summary>
		private bool IsTimeNearlyEqual(DateTime originalTime, DateTime newTime)
		{
			return Math.Abs((originalTime - newTime).Milliseconds) < 1;
		}

		/// <summary>
		/// Busy-wait until we can be sure Now will be a (significantly) later time than the input.
		/// We record times in files to 1 ms, and compare +-3ms, so 5 is enough to ensure this.
		/// </summary>
		/// <param name="old"></param>
		private void WaitForTimeToChange(DateTime old)
		{
			while ((DateTime.Now - old).Milliseconds < 5)
			{ }
		}

		private ICmObjectSurrogate GetSurrogate(ICmObject obj, ICmObjectSurrogateFactory cmoFactory)
		{
			return cmoFactory.Create(obj);
		}

		/// <summary>
		/// Copied from StringServicesTests (plus UOW); possibly best for each test set to have own utility functions?
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

		/// <summary>
		/// This test ensures the reconciler can create objects, set multi-string properties,
		/// set owning sequence and atomic properties, and add to an owning collection.
		/// </summary>
		[Test]
		public void ReconcileMakingEntryAndSense()
		{
			VerifyReconcilingAChange(()=>MakeEntry("kick", "strike with foot"), "make entry and sense");
		}

		/// <summary>
		/// Check we can reconcile changes to custom properties.
		/// </summary>
		[Test]
		public void ReconcilingModifiedCustomProperties()
		{
			var mdc = Cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
			var customCertifiedFlid = mdc.AddCustomField("WfiWordform", "Certified", CellarPropertyType.Boolean, 0);
			var customITsStringFlid = mdc.AddCustomField("WfiWordform", "NewTsStringProp", CellarPropertyType.String, 0);
			var customMultiUnicodeFlid = mdc.AddCustomField("WfiWordform", "MultiUnicodeProp", CellarPropertyType.MultiUnicode, 0);
			var customAtomicReferenceFlid = mdc.AddCustomField("WfiWordform", "NewAtomicRef", CellarPropertyType.ReferenceAtomic, CmPersonTags.kClassId);
			var customAtomicOwningFlid = mdc.AddCustomField("WfiWordform", "NewAtomicOwning", CellarPropertyType.OwningAtomic, StTextTags.kClassId);

			var wf = Cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create();
			Cache.LangProject.PeopleOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			var person = Cache.ServiceLocator.GetInstance<ICmPersonFactory>().Create();
			Cache.LangProject.PeopleOA.PossibilitiesOS.Add(person);

			var sda = Cache.MainCacheAccessor;
			VerifyReconcilingAChange(
				() => sda.SetBoolean(wf.Hvo, customCertifiedFlid, true),
				"set custom bool property");
			VerifyReconcilingAChange(
				() => sda.SetString(wf.Hvo, customITsStringFlid, Cache.TsStrFactory.MakeString("test", Cache.DefaultVernWs)),
				"set custom string property");
			VerifyReconcilingAChange(
				() => sda.SetMultiStringAlt(wf.Hvo, customMultiUnicodeFlid, Cache.DefaultVernWs,
					Cache.TsStrFactory.MakeString("test", Cache.DefaultVernWs)),
				"set custom multistring property");
			VerifyReconcilingAChange(
				() => sda.SetObjProp(wf.Hvo, customAtomicReferenceFlid, person.Hvo),
				"set custom atomic ref property");
			VerifyReconcilingAChange(
				() =>
					{
						sda.MakeNewObject(StTextTags.kClassId, wf.Hvo, customAtomicOwningFlid, -2);
					},
				"set custom owning atomic property");
			var text = Cache.ServiceLocator.GetObject(sda.get_ObjectProp(wf.Hvo, customAtomicOwningFlid)) as StText;
			Assert.That(text, Is.Not.Null);
			Assert.That(text.Owner, Is.EqualTo(wf));
		}

		/// <summary>
		/// This test ensures the reconciler can modify various kinds of properties.
		/// </summary>
		[Test]
		public void ReconcileModifyingProperties()
		{
			var entry1 = MakeEntry("kick", "strike with foot");
			var sense1 = entry1.SensesOS[0];
			VerifyReconcilingAChange(
				() =>
					{
						SetString(sense1.Definition, Cache.DefaultAnalWs, "swing foot and make sharp contact with object");
						SetString(sense1.Definition, Cache.DefaultVernWs, "slam with nether appendage");
						SetString(sense1.Gloss, Cache.DefaultVernWs, "hit with foot");
					},
				"set multistring properties");
			VerifyReconcilingAChange(
				() => AddSense(entry1, "strike with boot"),
				"edit owing sequence");
			IText text = null;
			UndoableUnitOfWorkHelper.Do("undo make text", "redo make text", m_actionHandler,
				() =>
					{
						text = Cache.ServiceLocator.GetInstance<ITextFactory>().Create();
						//Cache.LangProject.TextsOC.Add(text);
					});
			IStText stText = null;
			ICmObjectId id1 = null;
			VerifyReconcilingAChange(
				() =>
					{
						stText = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
						text.ContentsOA = stText;
						id1 = stText.Id;
					},
				"set owning atomic");
			stText = (IStText)Cache.ServiceLocator.GetObject(id1);
			ICmPossibility senseType = null;
			UndoableUnitOfWorkHelper.Do("undo make sense type", "redo make sense type", m_actionHandler,
				() =>
					{
						var senseTypesList = Cache.LangProject.LexDbOA.SenseTypesOA;
						if (senseTypesList == null)
						{
							senseTypesList = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
							Cache.LangProject.LexDbOA.SenseTypesOA = senseTypesList;
						}
						senseType = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
						senseTypesList.PossibilitiesOS.Add(senseType);

					});
			VerifyReconcilingAChange(
				() => sense1.SenseTypeRA = senseType,
				"set ref atomic");
			ICmPossibilityList thesaurusItemsList = null;
			UndoableUnitOfWorkHelper.Do("undo make thes list", "redo make thes list", m_actionHandler,
				() =>
					{
						thesaurusItemsList = Cache.LangProject.ThesaurusRA;
						if (thesaurusItemsList == null)
						{
							thesaurusItemsList = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
							Cache.LangProject.ThesaurusRA = thesaurusItemsList;
						}

					});
			ICmPossibility thesaurusItem1 = null;
			ICmPossibility thesaurusItem2 = null;
			ICmObjectId id2 = null;
			VerifyReconcilingAChange(
				() =>
					{
						thesaurusItem1 = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
						thesaurusItemsList.PossibilitiesOS.Add(thesaurusItem1);
						id1 = thesaurusItem1.Id;
						thesaurusItem2 = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
						thesaurusItemsList.PossibilitiesOS.Add(thesaurusItem2);
						id2 = thesaurusItem2.Id;
					},
				"adding two items to an owning collection");
			// Verify undoes creating them, then creates new ones with the same IDs. We need the new valid objects.
			thesaurusItem1 = (ICmPossibility)Cache.ServiceLocator.GetObject(id1);
			thesaurusItem2 = (ICmPossibility)Cache.ServiceLocator.GetObject(id1);
			VerifyReconcilingAChange(
				() =>
					{
						sense1.ThesaurusItemsRC.Add(thesaurusItem1);
						sense1.ThesaurusItemsRC.Add(thesaurusItem2);
					},
				"adding two items to a reference collection");
			VerifyReconcilingAChange(
				() =>
					{
						entry1.MainEntriesOrSensesRS.Add(sense1);
					},
				"adding an item to a reference sequence");
			IStTxtPara para = null;
			UndoableUnitOfWorkHelper.Do("undo make para", "redo make para", m_actionHandler,
				() =>
				{
						para = Cache.ServiceLocator.GetInstance<IStTxtParaFactory>().Create();
						stText.ParagraphsOS.Add(para);
				});
			VerifyReconcilingAChange(
				() =>
					{
						SetParaContents(para, "Hello world"); // big string
						para.Label = Cache.TsStrFactory.MakeString("label", Cache.DefaultVernWs); // non-big string, just to be sure
					},
				"setting a simple string, previously empty");
			VerifyReconcilingAChange(
				() => SetParaContents(para, "Goodbye, world"),
				"setting a simple string, previously non-empty");
			VerifyReconcilingAChange(
				() => para.ParseIsCurrent = true,
				"setting a boolean");
			VerifyReconcilingAChange(
				() => thesaurusItem2.HelpId = "look for help here!",
				"setting a plain unicode string");
			VerifyReconcilingAChange(
				() => thesaurusItemsList.Depth = 17,
				"setting an integer");
			VerifyReconcilingAChange(
				() => thesaurusItemsList.ListVersion = Guid.NewGuid(),
				"setting a guid property");
			var propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "red hot");
			var props = propsBldr.GetTextProps();
			VerifyReconcilingAChange(
				() => para.StyleRules = props,
				"setting a text props");
			IRnGenericRec genericRec = null;
			UndoableUnitOfWorkHelper.Do("undo make notebook record", "redo make notebook record", m_actionHandler,
				() =>
				{
					genericRec = Cache.ServiceLocator.GetInstance<IRnGenericRecFactory>().Create();
					Cache.LangProject.ResearchNotebookOA.RecordsOC.Add(genericRec);
				});
			VerifyReconcilingAChange(
				() => genericRec.DateOfEvent = new GenDate(GenDate.PrecisionType.Exact, 7, 23, 1908, true),
				"setting a generic date");
			IUserConfigAcct acct = null;
			UndoableUnitOfWorkHelper.Do("undo make user view", "redo", m_actionHandler,
				() =>
				{
					acct = Cache.ServiceLocator.GetInstance<IUserConfigAcctFactory>().Create();
					Cache.LanguageProject.UserAccountsOC.Add(acct);
				});
			VerifyReconcilingAChange(() => acct.Sid = new byte[] {1, 2, 3}, "setting a binary property");
		}

		/// <summary>
		/// Varions modes of deleting things.
		/// </summary>
		[Test]
		public void ReconcilingDeletingObjects()
		{
			var kick = MakeEntry("kick", "punish severely");
			var kick1 = AddSense(kick, "strike with foot");
			var text = Cache.ServiceLocator.GetInstance<ITextFactory>().Create();
			if (Cache.LangProject.PartsOfSpeechOA == null)
				Cache.LangProject.PartsOfSpeechOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();

			VerifyReconcilingAChange(() => kick.SensesOS.RemoveAt(0), "deleting first sense");
			ILexEntry punch = null;
			UndoableUnitOfWorkHelper.Do("undo make notebook record", "redo make notebook record", m_actionHandler,
				() => punch = MakeEntry("punch", "strike with foot"));
			VerifyReconcilingAChange(() => punch.Delete(), "deleting from collection");
			VerifyReconcilingAChange(() => Cache.LangProject.PartsOfSpeechOA = null, "deleting from owning atomic");
		}

		/// <summary>
		/// Varions modes of moving things.
		/// </summary>
		[Test]
		public void ReconcilingMovingObjects()
		{
			var kick = MakeEntry("kick", "punish severely");
			var senseStrike = AddSense(kick, "strike with foot");
			var sensePunish = kick.SensesOS[0];

			VerifyReconcilingAChange(() => senseStrike.SensesOS.Add(sensePunish), "move sense to subsense");
		}

		void SetParaContents(IStTxtPara para, string contents)
		{
			para.Contents = Cache.TsStrFactory.MakeString(contents, Cache.DefaultVernWs);
		}

		void SetString(IMultiString target, int ws, string text)
		{
			target.set_String(ws, Cache.TsStrFactory.MakeString(text,ws));
		}

		void SetString(IMultiUnicode target, int ws, string text)
		{
			target.set_String(ws, Cache.TsStrFactory.MakeString(text, ws));
		}

		/// <summary>
		/// Helper method for tests on this pattern:
		/// Create some initial state;
		/// Save;
		/// Make a change locally;
		/// Record the changes and their final state;
		/// Undo;
		/// Wait until a new DateTime occurs;
		/// Make the other change locally;
		/// Reconcile the changes in the first group;
		/// </summary>
		/// <param name="firstChange"></param>
		/// <param name="secondChange"></param>
		/// <param name="label"></param>
		void VerifyCheckingAndReconcilingAChange(Action firstChange, Action secondChange, string label)
		{

		}


		/// <summary>
		/// Helper method for several tests on this pattern:
		/// create an object that has the required kind of data;
		/// Save;
		/// make the change;
		/// make a set of newby/dirtball/deleted surrogates and Ids of the unsaved changes;
		/// Undo the change;
		/// pass the set of changes to the reconciler;
		/// verify that all expected objects are deleted;
		/// verify that all expected newbies exist;
		/// verify the current state of the newbies and dirtballs by checking that their XML dump is exactly what it was after the change.
		/// The calling test takes things as far as creating the initial state. This routine takes over there.
		/// </summary>
		void VerifyReconcilingAChange(Action makeTheChange, string label)
		{
			// Wrap up the Undo task for setting up the data and 'save' it
			List<ICmObjectSurrogate> newbySurrogates;
			List<ICmObjectSurrogate> dirtballSurrogates;
			List<ICmObjectId> gonerList;
			GetEffectsOfChange(makeTheChange, out newbySurrogates, out dirtballSurrogates, out gonerList);
			var undoManager = Cache.ServiceLocator.GetInstance<IUndoStackManager>();
			// Re-apply the changes using the reconciliation mechanism
			var reconciler = ((UnitOfWorkService)undoManager).Reconciler(newbySurrogates, dirtballSurrogates, gonerList);
			reconciler.ReconcileForeignChanges();
			// verify deletions
			foreach (var id in gonerList)
				Assert.That(Cache.ServiceLocator.ObjectRepository.IsValidObjectId(id.Guid), Is.False,
					"An object was not deleted in case " + label);
			// verify additions
			foreach (var surr in newbySurrogates)
				Assert.That(Cache.ServiceLocator.ObjectRepository.IsValidObjectId(surr.Guid), Is.True,
					"An object was not created in case " + label);
			// verify contents
			foreach (var surrogate in newbySurrogates.Concat(dirtballSurrogates))
			{
				var expectedXml = surrogate.XML;
				var obj = Cache.ServiceLocator.GetObject(surrogate.Id);
				Assert.That(((ICmObjectInternal)obj).ToXmlString(), Is.EqualTo(expectedXml), "Wrong final state of object in " + label);
			}
			// Ideally we would also verify that the expected PropChanges were sent. However we know the basic
			// mechanism is to set up undo tasks.
		}

		/// <summary>
		/// Get the effects of making the change (done by executing the "makeTheChange" action) in the form of lists of
		/// new, modified, and deleted objects.
		/// </summary>
		private void GetEffectsOfChange(Action makeTheChange, out List<ICmObjectSurrogate> newbySurrogates,
			out List<ICmObjectSurrogate> dirtballSurrogates, out List<ICmObjectId> gonerList)
		{
			GetEffectsOfChange(makeTheChange, null, out newbySurrogates, out dirtballSurrogates, out gonerList);
		}
		/// <summary>
		/// Get the effects of making the change (done by executing the "makeTheChange" action) in the form of lists of
		/// new, modified, and deleted objects. The additional action, doAfterUow, is 'done' after the UOW is complete,
		/// before the Undo.
		/// </summary>
		private void GetEffectsOfChange(Action makeTheChange, Action doAfterUow, out List<ICmObjectSurrogate> newbySurrogates,
			out List<ICmObjectSurrogate> dirtballSurrogates, out List<ICmObjectId> gonerList)
		{
			if (m_actionHandler.CurrentDepth > 0)
				m_actionHandler.EndUndoTask();
			Save();
			// Do the actual change that we want to verify can be reconciled.
			UndoableUnitOfWorkHelper.Do("undo", "redo", m_actionHandler, makeTheChange);
			if (doAfterUow != null)
				doAfterUow();
			// Gather the changed object information
			var uowService = Cache.ServiceLocator.GetInstance<IUnitOfWorkService>();
			var newbies = new HashSet<ICmObjectId>();
			var dirtballs = new HashSet<ICmObjectOrSurrogate>(new ObjectSurrogateEquater());
			var goners = new HashSet<ICmObjectId>();
			uowService.GatherChanges(newbies, dirtballs, goners);
			// Convert to surrogates as needed.
			var cmoFactory = Cache.ServiceLocator.GetInstance<ICmObjectSurrogateFactory>();
			newbySurrogates = new List<ICmObjectSurrogate>(
				from id in newbies select GetSurrogate(Cache.ServiceLocator.GetObject(id), cmoFactory));
			dirtballSurrogates = new List<ICmObjectSurrogate>(
				from obj in dirtballs select GetSurrogate(obj.Object, cmoFactory));
			gonerList = new List<ICmObjectId>(goners);
			// Undo the changes, getting back to original state
			m_actionHandler.Undo();
		}

		private void Save()
		{
			Cache.ServiceLocator.GetInstance<IUndoStackManager>().Save();
		}
	}
}

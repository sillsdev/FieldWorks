// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.DetailRules;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// The Insert Sound or Movie rule: which entry and pronunciation take the files, the
	/// pronunciation created for an entry without one, and the media links.
	/// </summary>
	[TestFixture]
	public class MediaFileInsertionTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry _entry;
		private ILexEntry _bare;
		private ILexSense _sense;
		private ILexPronunciation _first;
		private ILexPronunciation _second;

		// An entry with a sense and two pronunciations, and a bare entry with neither.
		[SetUp]
		public void MakeEntries()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				_sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				_entry.SensesOS.Add(_sense);
				var pronunciations = Cache.ServiceLocator.GetInstance<ILexPronunciationFactory>();
				_entry.PronunciationsOS.Add(_first = pronunciations.Create());
				_entry.PronunciationsOS.Add(_second = pronunciations.Create());
				_bare = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			});
		}

		[TearDown]
		public void DeleteEntries()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				if (_entry.IsValidObject)
					_entry.Delete();
				if (_bare.IsValidObject)
					_bare.Delete();
			});
		}

		[Test]
		public void TargetEntry_IsTheRowsEntry_ElseTheRootEntry()
		{
			Assert.That(MediaFileInsertion.TargetEntry(_second, _bare), Is.SameAs(_entry), "a pronunciation's entry");
			Assert.That(MediaFileInsertion.TargetEntry(_sense, _bare), Is.SameAs(_entry), "a sense's entry");
			Assert.That(MediaFileInsertion.TargetEntry(_entry, _bare), Is.SameAs(_entry), "the entry itself");
			Assert.That(MediaFileInsertion.TargetEntry(Cache.LangProject, _bare), Is.SameAs(_bare), "no entry: the root");
			Assert.That(MediaFileInsertion.TargetEntry(null, _bare), Is.SameAs(_bare));
			Assert.That(MediaFileInsertion.TargetEntry(Cache.LangProject, Cache.LangProject), Is.Null);
			Assert.That(MediaFileInsertion.TargetEntry(null, null), Is.Null);
		}

		[Test]
		public void TargetPronunciation_IsTheRowsOwn_ElseTheEntrysFirst_ElseNone()
		{
			Assert.That(MediaFileInsertion.TargetPronunciation(_second, null), Is.SameAs(_second));
			Assert.That(MediaFileInsertion.TargetPronunciation(_sense, null), Is.SameAs(_first));
			Assert.That(MediaFileInsertion.TargetPronunciation(_entry, null), Is.SameAs(_first));
			Assert.That(MediaFileInsertion.TargetPronunciation(_bare, null), Is.Null, "the bare entry has none yet");
			Assert.That(MediaFileInsertion.TargetPronunciation(null, null), Is.Null);
		}

		[Test]
		public void FirstOrCreatePronunciation_CreatesOne_OnlyWhenTheEntryHasNone()
		{
			Assert.That(MediaFileInsertion.FirstOrCreatePronunciation(_entry, "undo", "redo"), Is.SameAs(_first));
			Assert.That(_entry.PronunciationsOS, Has.Count.EqualTo(2));

			var created = MediaFileInsertion.FirstOrCreatePronunciation(_bare, "undo", "redo");

			Assert.That(_bare.PronunciationsOS.ToList(), Is.EqualTo(new[] { created }));
			Assert.That(Cache.ActionHandlerAccessor.GetUndoText(), Is.EqualTo("undo"));
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(_bare.PronunciationsOS, Is.Empty, "one Undo removes it");
		}

		// The dialog lists the last picked file first, so the names land in reverse after the
		// existing links: two picked files end in pick order.
		[Test]
		public void Insert_LinksEachNamedFile_InTheMediaFolder_InOneUndoableStep()
		{
			var existing = MediaFileInsertion.Insert(_second, new[] { "existing.wav" }, "undo", "redo").Single();

			var created = MediaFileInsertion.Insert(_second, new[] { "two.mp4", "", "one.wav" }, "undo", "redo");

			Assert.That(_second.MediaFilesOS.ToList(), Is.EqualTo(new[] { existing }.Concat(created)),
				"after the existing link, the empty name skipped");
			Assert.That(created.Select(m => m.MediaFileRA.InternalPath), Is.EqualTo(new[] { "one.wav", "two.mp4" }),
				"in reverse of the names' order");
			var folder = created[0].MediaFileRA.Owner as ICmFolder;
			Assert.That(folder?.Name.BestAnalysisAlternative.Text, Is.EqualTo(MediaFileInsertion.MediaFolderName()));
			Assert.That(folder.Owner, Is.SameAs(Cache.LangProject), "the project's media folder");
			Assert.That(created[1].MediaFileRA.Owner, Is.SameAs(folder));
			Assert.That(Cache.ActionHandlerAccessor.GetUndoText(), Is.EqualTo("undo"));
			Cache.ActionHandlerAccessor.Undo();
			Assert.That(_second.MediaFilesOS.ToList(), Is.EqualTo(new[] { existing }), "one Undo removes both new links");
		}

		[Test]
		public void Insert_DoesNothing_WithoutANamedFile()
		{
			Assert.That(MediaFileInsertion.Insert(_second, new[] { "", null }, "undo", "redo"), Is.Empty);
			Assert.That(_second.MediaFilesOS, Is.Empty);
		}

		[Test]
		public void MediaFolderName_IsNeverEmpty()
		{
			Assert.That(MediaFileInsertion.MediaFolderName(), Is.Not.Empty);
			Assert.That(MediaFileInsertion.MediaFolderName(), Does.Not.StartWith("*"), "no unresolved table key");
		}
	}
}

// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using FwAvaloniaDialogs;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.LexText.Controls;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LexTextControlsTests
{
	/// <summary>
	/// The LCModel-aware side of the reusable Avalonia chooser dialog launcher
	/// (<see cref="LcmChooserDialogLauncher"/>): building flat (depth-indented) candidates from a real
	/// possibility list, mapping current objects to the initial guid-string keys, and the full input mapping
	/// (candidates + initial selection + the Phase 1 require-a-selection-unless-empty OK rule). The modal loop
	/// itself is desktop-only (it needs an Avalonia app + a WinForms-owned modal Form), so it is exercised by the
	/// headless ChooserDialogTests in FwAvaloniaDialogsTests; here we cover the pure LCModel mapping over a real
	/// LcmCache, visible via InternalsVisibleTo.
	/// </summary>
	[TestFixture]
	public class LcmChooserDialogLauncherTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private ICmPossibilityList _list;
		private ICmPossibility _noun;
		private ICmPossibility _properNoun; // child of noun (depth 1)
		private ICmPossibility _verb;

		// The base (MemoryOnlyBackendProviderRestoredForEachTestTestBase) opens an undoable UOW in TestSetup and
		// calls CreateTestData() inside it, so data is created directly here with NO UOW wrapper. Wrapping this in
		// NonUndoableUnitOfWorkHelper.Do(...) would begin a task while one is already open and throw
		// "Nested tasks are not supported" (UndoStack.CheckNotProcessingDataChanges).
		protected override void CreateTestData()
		{
			base.CreateTestData();

			var listFactory = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>();
			var possFactory = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>();
			var ws = Cache.DefaultAnalWs;

			// A fresh, owned (so the items are valid), guaranteed-empty possibility list: assign it to the
			// LangProject's Locations slot. We add two top-level possibilities + one nested child to exercise
			// the depth walk deterministically (the list starts empty).
			_list = listFactory.Create();
			Cache.LangProject.LocationsOA = _list;

			_noun = possFactory.Create();
			_list.PossibilitiesOS.Add(_noun);
			_noun.Name.set_String(ws, "Noun");

			_properNoun = possFactory.Create();
			_noun.SubPossibilitiesOS.Add(_properNoun); // nested => depth 1
			_properNoun.Name.set_String(ws, "Proper noun");

			_verb = possFactory.Create();
			_list.PossibilitiesOS.Add(_verb);
			_verb.Name.set_String(ws, "Verb");
		}

		// ----- BuildCandidates: flat, depth-carrying, guid keys, best-WS names -----

		[Test]
		public void BuildCandidates_WalksTheListInDocumentOrder_CarryingDepthAndGuidKeys()
		{
			var candidates = LcmChooserDialogLauncher.BuildCandidates(_list);
			var byKey = candidates.ToDictionary(c => c.Key);

			// The key is the possibility's guid string, and each candidate's name is its best-analysis name.
			Assert.That(byKey[_noun.Guid.ToString()].Name, Is.EqualTo("Noun"));
			Assert.That(byKey[_properNoun.Guid.ToString()].Name, Is.EqualTo("Proper noun"));
			Assert.That(byKey[_verb.Guid.ToString()].Name, Is.EqualTo("Verb"));

			// A sub-possibility carries depth 1 (rendered as indentation in Phase 1); top-level items are depth 0.
			Assert.That(byKey[_noun.Guid.ToString()].Depth, Is.EqualTo(0));
			Assert.That(byKey[_properNoun.Guid.ToString()].Depth, Is.EqualTo(1));
			Assert.That(byKey[_verb.Guid.ToString()].Depth, Is.EqualTo(0));

			// Parent precedes its child, which precedes the next top-level item (document-order, parent-before-children).
			var keys = candidates.Select(c => c.Key).ToList();
			Assert.That(keys.IndexOf(_noun.Guid.ToString()), Is.LessThan(keys.IndexOf(_properNoun.Guid.ToString())),
				"parent precedes its children (a flat list walked in document order, not a tree)");
			Assert.That(keys.IndexOf(_properNoun.Guid.ToString()), Is.LessThan(keys.IndexOf(_verb.Guid.ToString())),
				"a child precedes the next top-level sibling");
		}

		// ----- MapToKeys: current objects -> initial selection -----

		[Test]
		public void MapToKeys_MapsCurrentObjectsToTheirGuidStrings()
		{
			var keys = LcmChooserDialogLauncher.MapToKeys(new ICmObject[] { _verb, _properNoun });
			Assert.That(keys, Is.EqualTo(new[] { _verb.Guid.ToString(), _properNoun.Guid.ToString() }));
		}

		[Test]
		public void MapToKeys_NullAndNullEntries_AreTolerated()
		{
			Assert.That(LcmChooserDialogLauncher.MapToKeys(null), Is.Empty);
			Assert.That(LcmChooserDialogLauncher.MapToKeys(new ICmObject[] { null, _noun }),
				Is.EqualTo(new[] { _noun.Guid.ToString() }), "null current entries are dropped");
		}

		// ----- BuildInput: the full state mapping -----

		[Test]
		public void BuildInput_Single_MapsCandidatesCurrentAndForbidsEmptyWhenNotAllowingEmpty()
		{
			var input = LcmChooserDialogLauncher.BuildInput(_list, ChooserSelectionMode.Single,
				new[] { _verb }, allowEmpty: false, prompt: "Pick one", helpTopic: "khtpPickOne");

			Assert.That(input.SelectionMode, Is.EqualTo(ChooserSelectionMode.Single));
			Assert.That(input.Candidates.Select(c => c.Key),
				Is.SupersetOf(new[] { _noun.Guid.ToString(), _properNoun.Guid.ToString(), _verb.Guid.ToString() }));
			Assert.That(input.InitialSelectedKeys, Is.EqualTo(new[] { _verb.Guid.ToString() }));
			Assert.That(input.AllowEmpty, Is.False);
			Assert.That(input.ForbidEmptySelection, Is.True, "no AllowEmpty => a selection is required");
			Assert.That(input.Prompt, Is.EqualTo("Pick one"));
			Assert.That(input.HelpTopic, Is.EqualTo("khtpPickOne"));
		}

		[Test]
		public void BuildInput_AllowEmpty_DoesNotForbidEmptySelection()
		{
			var input = LcmChooserDialogLauncher.BuildInput(_list, ChooserSelectionMode.Single,
				current: null, allowEmpty: true, prompt: null, helpTopic: null);

			Assert.That(input.AllowEmpty, Is.True);
			Assert.That(input.ForbidEmptySelection, Is.False, "AllowEmpty relaxes the required-selection gate (atomic clear)");
			Assert.That(input.InitialSelectedKeys, Is.Empty);
		}

		[Test]
		public void BuildInput_Multi_CarriesMode()
		{
			var input = LcmChooserDialogLauncher.BuildInput(_list, ChooserSelectionMode.Multi,
				new[] { _noun, _verb }, allowEmpty: false, prompt: null, helpTopic: null);
			Assert.That(input.SelectionMode, Is.EqualTo(ChooserSelectionMode.Multi));
			Assert.That(input.InitialSelectedKeys,
				Is.EqualTo(new[] { _noun.Guid.ToString(), _verb.Guid.ToString() }));
		}

		// ----- round-trip: a VM built over the launcher input maps current -> selected and keys -> objects -----

		[Test]
		public void ViewModel_OverLauncherInput_PrimesInitialSelection_AndKeysRoundTripToObjects()
		{
			// BuildState/CreateViewModel are pure-ish; only the modal ShowModal loop is desktop-only. We can
			// build the VM directly (no Avalonia app needed for the VM itself) to prove the current->selected
			// mapping, and resolve the chosen keys back to objects via the repository the way Apply does.
			var input = LcmChooserDialogLauncher.BuildInput(_list, ChooserSelectionMode.Multi,
				new[] { _verb }, allowEmpty: false, prompt: null, helpTopic: null);
			var vm = new ChooserDialogViewModel(input);

			Assert.That(vm.ChosenKeys, Is.EqualTo(new[] { _verb.Guid.ToString() }),
				"the current object is primed as the initial chosen key");

			// What Apply does with the chosen keys: resolve guid strings back to objects, dropping the empty key.
			var repo = Cache.ServiceLocator.ObjectRepository;
			var resolved = vm.ChosenKeys
				.Where(k => !string.IsNullOrEmpty(k))
				.Select(k => repo.GetObject(new System.Guid(k)))
				.ToList();
			Assert.That(resolved, Is.EqualTo(new ICmObject[] { _verb }),
				"chosen guid keys resolve back to the live objects (empty key => no object)");
		}
	}
}

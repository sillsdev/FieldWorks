// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// avalonia-rule-formula-editor (Block 3/4) — verifies the supporting-editor record details compose
	/// END TO END on the Avalonia surface after the multi-child-part importer fix: the bespoke custom field
	/// resolves to its Avalonia editor AND the surrounding standard Name/Description fields compose editably.
	/// These gate the tool flips (EnvironmentEdit / phonemeEdit).
	/// </summary>
	[TestFixture]
	public class SupportingEditorComposeTests : MemoryOnlyBackendProviderTestBase
	{
		[Test]
		public void Compose_PhEnvironment_HasEnvironmentEditorAndEditableNameDescription()
		{
			IPhEnvironment env = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				env = Cache.ServiceLocator.GetInstance<IPhEnvironmentFactory>().Create();
				Cache.LangProject.PhonologicalDataOA.EnvironmentsOS.Add(env);
				env.Name.SetAnalysisDefaultWritingSystem("intervocalic");
				env.Description.SetAnalysisDefaultWritingSystem("between vowels");
			});

			var composed = FullEntryRegionComposer.Compose(env, Cache, layoutName: "Edit",
				plugins: RegionEditorPluginRegistry.Default);

			var editors = composed.Model.Fields.Where(f => f.Kind == RegionFieldKind.Custom)
				.Select(f => f.ControlFactory?.Invoke()).OfType<PhEnvironmentEditor>().ToList();
			Assert.That(editors, Is.Not.Empty, "the StringRepresentation slice resolves to the Avalonia environment editor");
			Assert.That(composed.Model.Fields.Any(f => f.Kind == RegionFieldKind.Text),
				"Name/Description compose as editable text (multi-child <if> import fix)");
		}

		[Test]
		public void Compose_PhPhoneme_HasIpaSymbolEditor()
		{
			IPhPhoneme phoneme = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				Cache.LangProject.PhonologicalDataOA.PhonemeSetsOS.Add(
					Cache.ServiceLocator.GetInstance<IPhPhonemeSetFactory>().Create());
				phoneme = Cache.ServiceLocator.GetInstance<IPhPhonemeFactory>().Create();
				Cache.LangProject.PhonologicalDataOA.PhonemeSetsOS[0].PhonemesOC.Add(phoneme);
				phoneme.Name.SetVernacularDefaultWritingSystem("p");
			});

			var composed = FullEntryRegionComposer.Compose(phoneme, Cache, layoutName: "Edit",
				plugins: RegionEditorPluginRegistry.Default);

			var editors = composed.Model.Fields.Where(f => f.Kind == RegionFieldKind.Custom)
				.Select(f => f.ControlFactory?.Invoke()).OfType<BasicIPASymbolEditor>().ToList();
			Assert.That(editors, Is.Not.Empty, "the BasicIPASymbol slice resolves to the Avalonia IPA symbol editor");
		}
	}
}

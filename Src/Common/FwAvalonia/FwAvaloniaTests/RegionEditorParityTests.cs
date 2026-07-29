// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Editor-type parity for the lexical detail view:
	/// the importer carries an enumComboBox's stringList ids/group onto the node (the metadata
	/// survives even though the region does not render a closed enum combo);
	/// FwReferenceVectorField.Dispose detaches every handler it wired (count >0 → 0).
	/// </summary>
	[TestFixture]
	public class RegionEditorParityTests
	{
		// ---- The importer carries the enumComboBox stringList ----

		private static ViewDefinitionModel Import(string layoutXml, params (string id, string xml)[] parts)
		{
			var resolver = new InlinePartResolver(parts);
			return new XmlLayoutImporter().Import(XElement.Parse(layoutXml), resolver);
		}

		[AvaloniaTest]
		public void Importer_EnumComboBox_CarriesStringListIdsAndGroup()
		{
			var model = Import(
				"<layout class='WfiWordform' type='detail' name='T'><part ref='SpellingStatus'/></layout>",
				("SpellingStatus", @"<slice label='Spelling Status' field='SpellingStatus' editor='enumComboBox'>
					<deParams>
						<stringList group='Linguistics/WFI/SpellingStatus'
							ids='UndecidedSpellingStatus, CorrectSpellingStatus, IncorrectSpellingStatus'/>
					</deParams>
				</slice>"));

			var node = model.Roots.Single();
			Assert.That(node.EnumStringList, Is.Not.Null, "the importer no longer drops the stringList");
			Assert.That(node.EnumStringList.Ids, Is.EqualTo(new[]
			{
				"UndecidedSpellingStatus", "CorrectSpellingStatus", "IncorrectSpellingStatus"
			}), "ids are split and trimmed in document order (the stored enum int indexes this list)");
			Assert.That(node.EnumStringList.Group, Is.EqualTo("Linguistics/WFI/SpellingStatus"));
		}

		[AvaloniaTest]
		public void Importer_EnumComboBox_NoStringList_ReportsAndCarriesNothing()
		{
			var model = Import(
				"<layout class='X' type='detail' name='T'><part ref='S'/></layout>",
				("S", "<slice label='S' field='S' editor='enumComboBox'><deParams/></slice>"));

			var node = model.Roots.Single();
			Assert.That(node.EnumStringList, Is.Null);
			Assert.That(model.Diagnostics.Any(d => d.Code == "slice-content-dropped"
				&& d.Message.Contains("deParams")), Is.True, "a deParams without a stringList is reported, not silently dropped");
		}

		// ---- FwReferenceVectorField.Dispose detaches every handler ----

		private static LexicalEditRegionField VectorFieldWithItems() => new LexicalEditRegionField(
			"v1", "Publish In", "PublishIn", null, RegionFieldKind.ReferenceVector,
			EditorClassification.Known, "PublishIn", null, SurfaceRouting.Inherit, null,
			new List<RegionChoiceOption>
			{
				new RegionChoiceOption("p1", "Main Dictionary"),
				new RegionChoiceOption("p2", "Pocket")
			},
			null, isEditable: true,
			items: new List<RegionChoiceOption> { new RegionChoiceOption("p1", "Main Dictionary") });

		[AvaloniaTest]
		public void ReferenceVector_Dispose_DetachesEveryHandler()
		{
			var vector = new FwReferenceVectorField(VectorFieldWithItems(), "PublishIn",
				new FakeRegionEditContext());
			var window = new Window { Content = vector, Width = 480, Height = 200 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			Assert.That(vector.AttachedHandlerCount, Is.GreaterThan(0),
				"the editable vector wired per-item Remove handlers and the add picker subscriptions");
			vector.Dispose();
			Assert.That(vector.AttachedHandlerCount, Is.EqualTo(0),
				"Dispose detaches every wired handler/subscription");

			vector.Dispose(); // idempotent
			Assert.That(vector.AttachedHandlerCount, Is.EqualTo(0));
		}

		[AvaloniaTest]
		public void ReferenceVector_ReadOnly_HasNothingToDetach()
		{
			// A read-only vector (no edit context) wires no edit handlers, so its teardown is empty —
			// Dispose is a safe no-op.
			var vector = new FwReferenceVectorField(VectorFieldWithItems(), "PublishIn", editContext: null);
			Assert.That(vector.AttachedHandlerCount, Is.EqualTo(0));
			vector.Dispose();
			Assert.That(vector.AttachedHandlerCount, Is.EqualTo(0));
		}

		// A minimal IPartResolver that returns the inline part content by ref name.
		private sealed class InlinePartResolver : IPartResolver
		{
			private readonly Dictionary<string, XElement> _parts = new Dictionary<string, XElement>();

			public InlinePartResolver((string id, string xml)[] parts)
			{
				foreach (var (id, xml) in parts)
					_parts[id] = XElement.Parse(xml);
			}

			public XElement ResolvePart(string className, string layoutType, string refName)
				=> _parts.TryGetValue(refName, out var el) ? el : null;

			public System.Collections.Generic.IReadOnlyList<XElement> ResolvePartContents(string className, string layoutType, string refName)
				=> _parts.TryGetValue(refName, out var el) ? new[] { el } : System.Array.Empty<XElement>();

			public XElement ResolvePartByRef(string refName)
				=> _parts.TryGetValue(refName, out var el) ? el : null;
		}
	}
}

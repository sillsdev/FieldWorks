// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The composer's layout/parts sources are part of its interface, not a hidden read off the
	/// installed DistFiles. These tests compose against a synthetic search path so the
	/// unresolved-part class of defect (LT-22772: an input the composer derives differently from
	/// legacy, dropping slices with no error) is asserted through the composer itself rather than
	/// only through the XML importer's coverage report.
	/// </summary>
	[TestFixture]
	public class DetailComposerSourcesTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry m_entry;
		private string m_root;

		public override void TestSetup()
		{
			base.TestSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				m_entry.LexemeFormOA = morph;
				morph.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("casa", Cache.DefaultVernWs));
				m_entry.CitationForm.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("casa", Cache.DefaultVernWs));
			});
			m_root = Path.Combine(Path.GetTempPath(), "DetailComposerSourcesTests-" + Guid.NewGuid());
		}

		public override void TestTearDown()
		{
			if (Directory.Exists(m_root))
				Directory.Delete(m_root, true);
			base.TestTearDown();
		}

		// A two-directory search path shaped like the shipped one: hand-authored, then generated.
		private string[] WriteSearchPath(string layoutXml, string partsXml)
		{
			var custom = Path.Combine(m_root, "custom");
			var generated = Path.Combine(m_root, "generated");
			Directory.CreateDirectory(custom);
			Directory.CreateDirectory(generated);
			File.WriteAllText(Path.Combine(custom, "Lexicon.fwlayout"), layoutXml);
			File.WriteAllText(Path.Combine(custom, "LexEntryParts.xml"), partsXml);
			return new[] { custom, generated };
		}

		private const string CitationFormPart =
			"<PartInventory><bin class=\"LexEntry\">"
			+ "<part id=\"LexEntry-Detail-CitationForm\" type=\"Detail\">"
			+ "<slice field=\"CitationForm\" label=\"Citation Form\" editor=\"multistring\" ws=\"vernacular\"/>"
			+ "</part></bin></PartInventory>";

		[Test]
		public void Compose_OverASearchPathMissingAReferencedPart_ReportsTheUnresolvedPartDiagnostic()
		{
			var searchPath = WriteSearchPath(
				"<LayoutInventory><layout class=\"LexEntry\" type=\"detail\" name=\"Normal\">"
				+ "<part ref=\"CitationForm\"/><part ref=\"Nope\"/></layout></LayoutInventory>",
				CitationFormPart);

			using (DetailComposer.OverrideSearchPath(searchPath))
			{
				var composed = DetailComposer.Compose(m_entry, Cache);

				Assert.That(composed, Is.Not.Null, "a resolvable layout composes even when one ref is missing");
				Assert.That(composed.Model.Fields.Select(f => f.Field), Does.Contain("CitationForm"));
				var unresolved = composed.Model.Diagnostics.Where(d => d.Code == "unresolved-part").ToList();
				Assert.That(unresolved.Count, Is.EqualTo(1),
					"the missing part is reported on the model, never silently dropped");
				Assert.That(unresolved[0].Message, Does.Contain("Nope"));
			}
		}

		[Test]
		public void Compose_OverASearchPathThatResolvesEveryRef_ReportsNoUnresolvedPart()
		{
			var searchPath = WriteSearchPath(
				"<LayoutInventory><layout class=\"LexEntry\" type=\"detail\" name=\"Normal\">"
				+ "<part ref=\"CitationForm\"/></layout></LayoutInventory>",
				CitationFormPart);

			using (DetailComposer.OverrideSearchPath(searchPath))
			{
				var composed = DetailComposer.Compose(m_entry, Cache);

				Assert.That(composed.Model.Diagnostics.Where(d => d.Code == "unresolved-part"), Is.Empty);
				Assert.That(composed.Model.Fields.Select(f => f.Field).ToList(), Is.EqualTo(new[] { "CitationForm" }));
			}
		}

		[Test]
		public void OverrideSearchPath_Disposed_RestoresTheShippedSources()
		{
			var searchPath = WriteSearchPath(
				"<LayoutInventory><layout class=\"LexEntry\" type=\"detail\" name=\"Normal\">"
				+ "<part ref=\"CitationForm\"/></layout></LayoutInventory>",
				CitationFormPart);

			using (DetailComposer.OverrideSearchPath(searchPath))
			{
				Assert.That(DetailComposer.Compose(m_entry, Cache).Model.Fields.Select(f => f.Field).ToList(),
					Is.EqualTo(new[] { "CitationForm" }), "inside the scope the synthetic path is composed");
			}

			var shipped = DetailComposer.Compose(m_entry, Cache);
			Assert.That(shipped.Model.Fields.Select(f => f.Field), Does.Contain("Senses"),
				"after the scope the shipped LexEntry/Normal layout, which the synthetic one lacks, is composed again");
		}
	}
}

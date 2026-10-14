// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// How composed section headers start and where their expansion state is kept (LT-22689).
	/// Every section of the shipped Lexicon Edit layout starts expanded. State is keyed per
	/// section, naming the object by guid, so a state persisted in one session still finds its
	/// object in the next and follows a sense through a reorder.
	/// </summary>
	[TestFixture]
	public class DetailExpansionComposeTests : MemoryOnlyBackendProviderTestBase
	{
		private ILexEntry m_entry;

		public override void TestSetup()
		{
			base.TestSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				m_entry.LexemeFormOA = morph;
				morph.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("casa", Cache.DefaultVernWs));
				foreach (var gloss in new[] { "house", "home" })
				{
					var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
					m_entry.SensesOS.Add(sense);
					sense.Gloss.set_String(Cache.DefaultAnalWs, TsStringUtils.MakeString(gloss, Cache.DefaultAnalWs));
				}
			});
		}

		private DetailField[] CollapsibleHeaders() => DetailComposer.Compose(m_entry, Cache).Model.Fields
			.Where(f => f.Kind == DetailFieldKind.Header && f.IsCollapsible).ToArray();

		// The shipped layouts mark sections "expanded" or say nothing, so with no recorded state
		// every section opens, as it does in the WinForms DataTree.
		[Test]
		public void Compose_ShippedLayout_StartsEverySectionExpanded()
		{
			var headers = CollapsibleHeaders();
			Assert.That(headers, Is.Not.Empty, "precondition: the entry composes collapsible headers");

			Assert.That(headers.Where(h => !h.IsInitiallyExpanded).Select(h => h.Label), Is.Empty,
				"sections that would start collapsed");
		}

		[Test]
		public void Compose_CollapsibleHeaders_KeyTheirStateByObjectGuid_OnePerSection()
		{
			var headers = CollapsibleHeaders();
			Assert.That(headers, Is.Not.Empty, "precondition: the entry composes collapsible headers");

			foreach (var header in headers)
			{
				var obj = Cache.ServiceLocator.ObjectRepository.GetObject(header.ObjectHvo);
				Assert.That(header.ExpansionKey, Does.EndWith("@" + obj.Guid.ToString("N")),
					$"'{header.Label}' names its object by guid");
				Assert.That(header.ExpansionKey, Is.Not.EqualTo(header.StableId),
					$"'{header.Label}' does not name its object by its per-session hvo");
			}
			Assert.That(headers.Select(h => h.ExpansionKey), Is.Unique,
				"each section has its own key, even where sections share an object");
		}

		[Test]
		public void Compose_SenseHeaderKey_FollowsTheSenseThroughAReorder()
		{
			var second = m_entry.SensesOS[1];
			DetailField ItemHeader() => CollapsibleHeaders()
				.First(h => h.ObjectHvo == second.Hvo && h.StableId.Contains("/item"));
			var before = ItemHeader();

			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				() => m_entry.SensesOS.MoveTo(1, 1, m_entry.SensesOS, 0));
			var after = ItemHeader();

			Assert.That(after.StableId, Is.Not.EqualTo(before.StableId),
				"precondition: the row's position-based id changed with the move");
			Assert.That(after.ExpansionKey, Is.EqualTo(before.ExpansionKey),
				"the moved sense keeps the expansion state recorded for it");
		}
	}
}

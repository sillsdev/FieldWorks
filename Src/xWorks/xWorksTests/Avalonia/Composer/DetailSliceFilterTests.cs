// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The TOOL's gate on a row: a slice whose authored id appears in the filter list the
	/// tool's filterPath names is withheld, whatever the domain says about it (LT-22802). The
	/// domain's own gate is separate, and is covered by DetailFieldRelevanceTests.
	///
	/// Composed against CmPossibility because that is the only class any shipped filter list
	/// reaches: the five CmPossibility slice ids in basicPlusFilter.xml are the only entries
	/// across the six filter files that name a slice the parts inventory defines.
	/// </summary>
	[TestFixture]
	public class DetailSliceFilterTests : MemoryOnlyBackendProviderTestBase
	{
		private ICmPossibility m_possibility;

		[SetUp]
		public void CreateProductionRestriction()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				var list = Cache.LangProject.MorphologicalDataOA.ProdRestrictOA;
				m_possibility = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>()
					.Create();
				list.PossibilitiesOS.Add(m_possibility);
				m_possibility.Name.set_String(Cache.DefaultAnalWs,
					TsStringUtils.MakeString("restriction", Cache.DefaultAnalWs));
			});
		}

		private IReadOnlyList<DetailField> Compose(params string[] hiddenSliceIds)
			=> DetailComposer.Compose(m_possibility, Cache, "default", showHiddenFields: true,
					hiddenSliceIds: hiddenSliceIds.Length == 0
						? null
						: new HashSet<string>(hiddenSliceIds))
				.Model.Fields;

		[Test]
		public void AFilteredSliceId_WithholdsThatRow_AndNothingElse()
		{
			var unfiltered = Compose();
			Assume.That(unfiltered.Any(f => f.Field == "Status"), Is.True,
				"fixture check: unfiltered, the Status row composes");

			var filtered = Compose("CmPossibilityStatus");

			Assert.That(filtered.Any(f => f.Field == "Status"), Is.False,
				"the tool's filter list names this row, so it is withheld");
			Assert.That(filtered.Count, Is.EqualTo(unfiltered.Count - 1),
				"and ONLY that row: a withheld node must not take unrelated rows with it");
		}

		/// <summary>
		/// A whole shipped filter list at once: every id it names goes, and the rows it does
		/// not name stay.
		/// </summary>
		[Test]
		public void AWholeFilterList_WithholdsEveryIdItNames_AndNothingElse()
		{
			var filtered = Compose("CmPossibilityStatus", "CmPossibilityDiscussion",
				"CmPossibilityConfidence", "CmPossibilityResearchers", "CmPossibilityRestrictions");
			var fields = filtered.Select(f => f.Field).ToList();

			Assert.That(fields, Has.No.Member("Status").And.No.Member("Discussion")
				.And.No.Member("Confidence").And.No.Member("Researchers")
				.And.No.Member("Restrictions"),
				"every id the filter lists is withheld. Composed:\n  "
				+ string.Join("\n  ", fields));
			Assert.That(fields, Does.Contain("Name").And.Contains("Abbreviation"),
				"and the rows the filter does not name are untouched");
		}

		[Test]
		public void AnIdInNoFilterList_LeavesEveryRowAlone()
		{
			var unfiltered = Compose();

			var filtered = Compose("NotAnIdAnySliceAuthors");

			Assert.That(filtered.Count, Is.EqualTo(unfiltered.Count),
				"a filter naming nothing withholds nothing -- most shipped filter entries are "
				+ "stale ids that resolve to no slice, and they must stay harmless");
		}
	}
}

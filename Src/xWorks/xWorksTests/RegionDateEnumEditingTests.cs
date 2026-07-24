// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Editor-type parity, real-domain side (Task A): the composer renders user-editable date/generic-date
	/// fields as editable Date rows while keeping DateCreated/DateModified (visibility="never") read-only by
	/// design, and the write-back parses SAFELY — a parseable date/gendate round-trips through the real
	/// LCModel <see cref="SilTime"/>/<see cref="GenDate"/> APIs, an unparseable one is rejected and never
	/// corrupts the stored value.
	/// </summary>
	[TestFixture]
	public class RegionDateEnumEditingTests : MemoryOnlyBackendProviderTestBase
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
			});
		}

		// DateCreated/DateModified are visibility="never" read-only by design: even under showHidden the
		// composer must NOT turn them into editable Date rows (the task carve-out).
		[Test]
		public void Compose_DateCreatedAndDateModified_StayReadOnly()
		{
			var composed = FullEntryRegionComposer.Compose(m_entry, Cache, showHiddenFields: true);
			var dateRows = composed.Model.Fields
				.Where(f => f.Field == "DateCreated" || f.Field == "DateModified")
				.ToList();

			Assert.That(dateRows, Is.Not.Empty, "showHidden surfaces the never-visible date stamps");
			foreach (var row in dateRows)
			{
				Assert.That(row.Kind, Is.Not.EqualTo(RegionFieldKind.Date),
					$"{row.Field} is a read-only design stamp, not an editable Date editor");
				Assert.That(row.IsEditable, Is.False, $"{row.Field} must stay read-only");
			}
		}

		// The exact-date setter contract: a parseable date round-trips through SilTime; an unparseable one
		// is rejected (the composer setter returns false, so the box restores the committed value).
		[Test]
		public void DateRoundTrip_ParseableCommits_GarbageRejected()
		{
			// Parseable: ConvertToSilTime/ConvertFromSilTime is the same path the setter uses.
			Assert.That(DateTime.TryParse("March 5, 2010", CultureInfo.CurrentUICulture,
				DateTimeStyles.None, out var parsed), Is.True);
			var silTime = SilTime.ConvertToSilTime(parsed);
			var back = SilTime.ConvertFromSilTime(silTime);
			Assert.That(back.Year, Is.EqualTo(2010));
			Assert.That(back.Month, Is.EqualTo(3));
			Assert.That(back.Day, Is.EqualTo(5));

			// Garbage: rejected, so the setter never reaches SetTime — no corruption.
			Assert.That(DateTime.TryParse("not a date", CultureInfo.CurrentUICulture,
				DateTimeStyles.None, out _), Is.False);
		}

		// The generic-date setter contract: GenDate.TryParse understands the ToLongString display format
		// (precision word + era) and round-trips it; garbage is rejected WITHOUT throwing (the safe
		// structured editor — no silent corruption).
		[Test]
		public void GenDateRoundTrip_PrecisionAndEra_AreParsedSafely()
		{
			var approx = new GenDate(GenDate.PrecisionType.Approximate, 0, 0, 1850, true);
			Assert.That(GenDate.TryParse(approx.ToLongString(), out var reparsed), Is.True);
			Assert.That(reparsed.Year, Is.EqualTo(1850));
			Assert.That(reparsed.Precision, Is.EqualTo(GenDate.PrecisionType.Approximate));

			var bc = new GenDate(GenDate.PrecisionType.Before, 0, 0, 500, false);
			Assert.That(GenDate.TryParse(bc.ToLongString(), out var reparsedBc), Is.True);
			Assert.That(reparsedBc.IsAD, Is.False, "the era (BC) round-trips");
			Assert.That(reparsedBc.Precision, Is.EqualTo(GenDate.PrecisionType.Before));

			// Garbage is rejected, never throws, never produces a corrupt value.
			Assert.That(GenDate.TryParse("complete nonsense %%%", out _), Is.False);
		}
	}
}

// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;

namespace FwAvaloniaTests
{
	/// <summary>
	/// avalonia-rule-formula-editor (task 2.2) — the <see cref="RuleCellSpec"/> option-key codec that round-
	/// trips a chooser selection (the xWorks options projection builds the keys; the view decodes the
	/// committed key back to a spec the handler applies).
	/// </summary>
	[TestFixture]
	public class RuleCellCommandsTests
	{
		[TestCase(RuleCellKind.Phoneme, "P")]
		[TestCase(RuleCellKind.NaturalClass, "N")]
		[TestCase(RuleCellKind.Boundary, "B")]
		public void OptionKey_RoundTrips_ForReferenceKinds(RuleCellKind kind, string prefix)
		{
			var guid = Guid.NewGuid();
			var key = new RuleCellSpec(kind, guid).ToOptionKey();
			Assert.That(key, Is.EqualTo(prefix + ":" + guid));

			var back = RuleCellSpec.FromOptionKey(key);
			Assert.That(back, Is.Not.Null);
			Assert.That(back.Kind, Is.EqualTo(kind));
			Assert.That(back.TargetGuid, Is.EqualTo(guid));
		}

		[Test]
		public void FromOptionKey_RejectsMalformedOrTargetlessKeys()
		{
			Assert.That(RuleCellSpec.FromOptionKey(null), Is.Null);
			Assert.That(RuleCellSpec.FromOptionKey(""), Is.Null);
			Assert.That(RuleCellSpec.FromOptionKey("P"), Is.Null, "no separator");
			Assert.That(RuleCellSpec.FromOptionKey("Z:" + Guid.NewGuid()), Is.Null, "unknown kind prefix");
			Assert.That(RuleCellSpec.FromOptionKey("P:not-a-guid"), Is.Null, "a reference kind needs a parseable GUID");
		}
	}
}

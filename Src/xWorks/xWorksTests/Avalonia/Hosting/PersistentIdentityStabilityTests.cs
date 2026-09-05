// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Detail;

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	public class PersistentIdentityStabilityTests
	{
		[Test]
		public void PersistentCommandTargetIdentity_FreezesCallerLayoutPathForSetMembership()
		{
			var layout = new DetailLayoutIdentity("LexEntry", "detail", "Normal", null);
			var replacement = new DetailLayoutIdentity("LexSense", "detail", "Normal", "choice");
			var source = new List<DetailLayoutIdentity> { layout };
			var identity = new RecordEditView.PersistentCommandTargetIdentity(1, "Field",
				"LexEntry", "Normal", "part[0]", "detail", null, source);
			var expected = new RecordEditView.PersistentCommandTargetIdentity(1, "Field",
				"LexEntry", "Normal", "part[0]", "detail", null, new[] { layout });
			var hash = identity.GetHashCode();
			var set = new HashSet<RecordEditView.PersistentCommandTargetIdentity> { identity };

			source[0] = replacement;
			source.Add(replacement);

			Assert.That(identity.LayoutPath, Has.Count.EqualTo(1));
			Assert.That(identity.LayoutPath[0], Is.EqualTo(layout));
			var list = (IList<DetailLayoutIdentity>)identity.LayoutPath;
			Assert.That(list.IsReadOnly, Is.True);
			Assert.Throws<NotSupportedException>(() => list[0] = replacement);
			Assert.That(identity.GetHashCode(), Is.EqualTo(hash));
			Assert.That(set.Contains(identity), Is.True);
			Assert.That(identity, Is.EqualTo(expected));
		}
	}
}

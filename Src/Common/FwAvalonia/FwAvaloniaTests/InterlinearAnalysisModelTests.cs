// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;

namespace FwAvaloniaTests
{
	/// <summary>
	/// avalonia-interlinear-editor (task 1.2) — the LCModel-free projection DTO the Avalonia interlinear
	/// control binds to. The Morphology plugin builds it (read) and maps per-bundle edits back with the
	/// Sandbox-parity MSA prune (write); the view never sees LCModel or a Sandbox.
	/// </summary>
	[TestFixture]
	public class InterlinearAnalysisModelTests
	{
		[Test]
		public void Bundle_CarriesFormsAndGuids()
		{
			var morph = Guid.NewGuid();
			var sense = Guid.NewGuid();
			var msa = Guid.NewGuid();
			var b = new InterlinearBundle("ka-", "P",  "pfx", morph, sense, msa);
			Assert.That(b.Morph, Is.EqualTo("ka-"));
			Assert.That(b.Gloss, Is.EqualTo("P"));
			Assert.That(b.GrammaticalInfo, Is.EqualTo("pfx"));
			Assert.That(b.MorphGuid, Is.EqualTo(morph));
			Assert.That(b.SenseGuid, Is.EqualTo(sense));
			Assert.That(b.MsaGuid, Is.EqualTo(msa));
		}

		[Test]
		public void Model_WithBundles_HasAnalysisTrue()
		{
			var model = new InterlinearAnalysisModel("kapula", new[]
			{
				new InterlinearLine("kapula", new[]
				{
					new InterlinearBundle("ka-", "P", "pfx"),
					new InterlinearBundle("pula", "rain", "n"),
				}, Guid.NewGuid()),
			}, Guid.NewGuid());

			Assert.That(model.HasAnalysis, Is.True);
			Assert.That(model.Lines, Has.Count.EqualTo(1));
			Assert.That(model.Lines[0].Bundles, Has.Count.EqualTo(2));
			Assert.That(model.Lines[0].Bundles[1].Gloss, Is.EqualTo("rain"));
		}

		[Test]
		public void Model_NoAnalysisOrEmptyLines_HasAnalysisFalse()
		{
			Assert.That(new InterlinearAnalysisModel("kapula", null).HasAnalysis, Is.False,
				"a bare wordform with no analyses renders the bare-wordform state, not the grid");
			Assert.That(new InterlinearAnalysisModel("kapula", new[]
			{
				new InterlinearLine("kapula", new List<InterlinearBundle>()),
			}).HasAnalysis, Is.False, "an analysis line with no bundles is not a renderable interlinear");
		}
	}
}

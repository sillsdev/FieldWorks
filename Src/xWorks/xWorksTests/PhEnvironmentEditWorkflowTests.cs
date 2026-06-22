// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// avalonia-rule-formula-editor (task 3.2) — T1 validate + T4 commit round-trip for the phonological
	/// environment editor. The sink validates through the same <c>PhonEnvRecognizer</c> the legacy slice
	/// used and writes <c>StringRepresentation</c> through the region's fenced session (one undo step).
	/// </summary>
	[TestFixture]
	public class PhEnvironmentEditWorkflowTests : MemoryOnlyBackendProviderTestBase
	{
		private IPhEnvironment m_env;

		public override void TestSetup()
		{
			base.TestSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				Cache.LangProject.PhonologicalDataOA.PhonemeSetsOS.Add(
					Cache.ServiceLocator.GetInstance<IPhPhonemeSetFactory>().Create());
				var a = Cache.ServiceLocator.GetInstance<IPhPhonemeFactory>().Create();
				Cache.LangProject.PhonologicalDataOA.PhonemeSetsOS[0].PhonemesOC.Add(a);
				a.Name.SetVernacularDefaultWritingSystem("a");
				var code = a.CodesOS.Count > 0 ? a.CodesOS[0]
					: Cache.ServiceLocator.GetInstance<IPhCodeFactory>().Create();
				if (a.CodesOS.Count == 0) a.CodesOS.Add(code);
				code.Representation.SetVernacularDefaultWritingSystem("a");

				m_env = Cache.ServiceLocator.GetInstance<IPhEnvironmentFactory>().Create();
				Cache.LangProject.PhonologicalDataOA.EnvironmentsOS.Add(m_env);
			});
		}

		private PhEnvironmentEditSink NewSink()
		{
			var host = new ComposedRegionEditContext(Cache, m_env,
				new Dictionary<string, Func<string, string, bool>>(),
				new Dictionary<string, Func<string, bool>>());
			return new PhEnvironmentEditSink(m_env, Cache, host);
		}

		[Test]
		public void Validate_RejectsMalformedEnvironment()
		{
			var sink = NewSink();
			Assert.That(sink.Validate("[[[ bogus"), Is.False, "a malformed environment is rejected by the recognizer");
		}

		[Test]
		public void Commit_WritesStringRepresentation_AndUndoRestores()
		{
			var sink = NewSink();
			Assert.That(sink.Commit("/ _ a"), Is.True);
			Assert.That(m_env.StringRepresentation.Text, Is.EqualTo("/ _ a"),
				"the committed environment string persisted to the domain");

			Cache.ActionHandlerAccessor.Undo();
			Assert.That(m_env.StringRepresentation?.Text ?? string.Empty, Is.Not.EqualTo("/ _ a"),
				"one Undo reverts the environment edit");
		}
	}
}

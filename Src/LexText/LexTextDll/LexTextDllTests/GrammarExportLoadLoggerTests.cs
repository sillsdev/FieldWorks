// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System.Collections.Generic;
using NUnit.Framework;
using SIL.FieldWorks.XWorks.LexText;

namespace LexTextDllTests
{
	[TestFixture]
	public class GrammarExportLoadLoggerTests
	{
		[Test]
		public void InvalidPhoneme_AddsAMessage_DoesNotThrow()
		{
			var messages = new List<string>();
			var logger = new GrammarExportLoadLogger(messages);

			Assert.DoesNotThrow(() => logger.InvalidPhoneme(null));

			Assert.That(messages, Has.Count.EqualTo(1));
		}

		[Test]
		public void InvalidStrata_AddsTheReasonToTheMessage()
		{
			var messages = new List<string>();
			var logger = new GrammarExportLoadLogger(messages);

			logger.InvalidStrata("Stratum1", "circular dependency");

			Assert.That(messages[0], Does.Contain("circular dependency"));
		}
	}
}

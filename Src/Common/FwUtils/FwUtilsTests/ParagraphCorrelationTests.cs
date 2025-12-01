// Copyright (c) 2006-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// </summary>
	[TestFixture]
	public class ParagraphCorrelationTests
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test calculating the correlation factor for various paragraphs
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CorrelationFactor()
		{
			ParagraphCorrelation pc = new ParagraphCorrelation("Hello", "Hello");
			Assert.That(pc.CorrelationFactor, Is.EqualTo(1.0));

			pc = new ParagraphCorrelation("Hello", "Hello ");
			Assert.That(pc.CorrelationFactor, Is.EqualTo(1.0));

			pc = new ParagraphCorrelation(" Hello", "Hello");
			Assert.That(pc.CorrelationFactor, Is.EqualTo(1.0));

			pc = new ParagraphCorrelation("Hello", "Hello there");
			Assert.That(pc.CorrelationFactor, Is.EqualTo(0.5));

			pc = new ParagraphCorrelation("Hello over there", "Hello over here");
			Assert.That(pc.CorrelationFactor, Is.EqualTo(0.5));

			pc = new ParagraphCorrelation("Hello there", "there Hello");
			Assert.That(pc.CorrelationFactor, Is.EqualTo(1.0));

			pc = new ParagraphCorrelation("I am really excited",
				"I am really really really really excited");
			Assert.That(pc.CorrelationFactor, Is.EqualTo(0.8125));

			pc = new ParagraphCorrelation(string.Empty, "What will happen here?");
			Assert.That(pc.CorrelationFactor, Is.EqualTo(0.0));

			pc = new ParagraphCorrelation(string.Empty, string.Empty);
			Assert.That(pc.CorrelationFactor, Is.EqualTo(1.0));

			pc = new ParagraphCorrelation(null, null);
			Assert.That(pc.CorrelationFactor, Is.EqualTo(1.0));

			pc = new ParagraphCorrelation(null, "what?");
			Assert.That(pc.CorrelationFactor, Is.EqualTo(0.0));

			pc = new ParagraphCorrelation("what?", null);
			Assert.That(pc.CorrelationFactor, Is.EqualTo(0.0));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test calculating the correlation factor for various paragraphs with digits and
		/// punctuation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CorrelationFactor_WithDigitsAndPunc()
		{
			ParagraphCorrelation pc = new ParagraphCorrelation("Hello!", "2Hello.");
			Assert.That(pc.CorrelationFactor, Is.EqualTo(1.0));

			pc = new ParagraphCorrelation("Hello", "Hello, there");
			Assert.That(pc.CorrelationFactor, Is.EqualTo(0.5));

			pc = new ParagraphCorrelation("3Hello over there", "Hello over here");
			Assert.That(pc.CorrelationFactor, Is.EqualTo(0.5));

			pc = new ParagraphCorrelation("Hello there?", "4there Hello!");
			Assert.That(pc.CorrelationFactor, Is.EqualTo(1.0));

			pc = new ParagraphCorrelation("5I am really excited!",
				"6I am really really really really excited.");
			Assert.That(pc.CorrelationFactor, Is.EqualTo(0.8125));
		}
	}
}

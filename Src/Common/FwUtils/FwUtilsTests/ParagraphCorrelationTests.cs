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
			Assert.AreEqual(1.0, pc.CorrelationFactor);

			pc = new ParagraphCorrelation("Hello", "Hello ");
			Assert.AreEqual(1.0, pc.CorrelationFactor);

			pc = new ParagraphCorrelation(" Hello", "Hello");
			Assert.AreEqual(1.0, pc.CorrelationFactor);

			pc = new ParagraphCorrelation("Hello", "Hello there");
			Assert.AreEqual(0.5, pc.CorrelationFactor);

			pc = new ParagraphCorrelation("Hello over there", "Hello over here");
			Assert.AreEqual(0.5, pc.CorrelationFactor);

			pc = new ParagraphCorrelation("Hello there", "there Hello");
			Assert.AreEqual(1.0, pc.CorrelationFactor);

			pc = new ParagraphCorrelation("I am really excited",
				"I am really really really really excited");
			Assert.AreEqual(0.8125, pc.CorrelationFactor);

			pc = new ParagraphCorrelation(string.Empty, "What will happen here?");
			Assert.AreEqual(0.0, pc.CorrelationFactor);

			pc = new ParagraphCorrelation(string.Empty, string.Empty);
			Assert.AreEqual(1.0, pc.CorrelationFactor);

			pc = new ParagraphCorrelation(null, null);
			Assert.AreEqual(1.0, pc.CorrelationFactor);

			pc = new ParagraphCorrelation(null, "what?");
			Assert.AreEqual(0.0, pc.CorrelationFactor);

			pc = new ParagraphCorrelation("what?", null);
			Assert.AreEqual(0.0, pc.CorrelationFactor);
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
			Assert.AreEqual(1.0, pc.CorrelationFactor);

			pc = new ParagraphCorrelation("Hello", "Hello, there");
			Assert.AreEqual(0.5, pc.CorrelationFactor);

			pc = new ParagraphCorrelation("3Hello over there", "Hello over here");
			Assert.AreEqual(0.5, pc.CorrelationFactor);

			pc = new ParagraphCorrelation("Hello there?", "4there Hello!");
			Assert.AreEqual(1.0, pc.CorrelationFactor);

			pc = new ParagraphCorrelation("5I am really excited!",
				"6I am really really really really excited.");
			Assert.AreEqual(0.8125, pc.CorrelationFactor);
		}
	}
}

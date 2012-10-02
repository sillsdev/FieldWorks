// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ParagraphCorrelationTests.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.Utils
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
			ILgCharacterPropertyEngine engine = LgIcuCharPropEngineClass.Create();

			ParagraphCorrelation pc = new ParagraphCorrelation("Hello", "Hello", engine);
			Assert.AreEqual(1.0, pc.CorrelationFactor);

			pc = new ParagraphCorrelation("Hello", "Hello ", engine);
			Assert.AreEqual(1.0, pc.CorrelationFactor);

			pc = new ParagraphCorrelation(" Hello", "Hello", engine);
			Assert.AreEqual(1.0, pc.CorrelationFactor);

			pc = new ParagraphCorrelation("Hello", "Hello there", engine);
			Assert.AreEqual(0.5, pc.CorrelationFactor);

			pc = new ParagraphCorrelation("Hello over there", "Hello over here", engine);
			Assert.AreEqual(0.5, pc.CorrelationFactor);

			pc = new ParagraphCorrelation("Hello there", "there Hello", engine);
			Assert.AreEqual(1.0, pc.CorrelationFactor);

			pc = new ParagraphCorrelation("I am really excited",
				"I am really really really really excited", engine);
			Assert.AreEqual(0.8125, pc.CorrelationFactor);

			pc = new ParagraphCorrelation(string.Empty, "What will happen here?", engine);
			Assert.AreEqual(0.0, pc.CorrelationFactor);

			pc = new ParagraphCorrelation(string.Empty, string.Empty, engine);
			Assert.AreEqual(1.0, pc.CorrelationFactor);

			pc = new ParagraphCorrelation(null, null, engine);
			Assert.AreEqual(1.0, pc.CorrelationFactor);

			pc = new ParagraphCorrelation(null, "what?", engine);
			Assert.AreEqual(0.0, pc.CorrelationFactor);

			pc = new ParagraphCorrelation("what?", null, engine);
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
			ILgCharacterPropertyEngine engine = LgIcuCharPropEngineClass.Create();

			ParagraphCorrelation pc = new ParagraphCorrelation("Hello!", "2Hello.", engine);
			Assert.AreEqual(1.0, pc.CorrelationFactor);

			pc = new ParagraphCorrelation("Hello", "Hello, there", engine);
			Assert.AreEqual(0.5, pc.CorrelationFactor);

			pc = new ParagraphCorrelation("3Hello over there", "Hello over here", engine);
			Assert.AreEqual(0.5, pc.CorrelationFactor);

			pc = new ParagraphCorrelation("Hello there?", "4there Hello!", engine);
			Assert.AreEqual(1.0, pc.CorrelationFactor);

			pc = new ParagraphCorrelation("5I am really excited!",
				"6I am really really really really excited.", engine);
			Assert.AreEqual(0.8125, pc.CorrelationFactor);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the LongestUsefulSubstring method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LongestUsefulSubstring()
		{
			ILgCharacterPropertyEngine engine = LgIcuCharPropEngineClass.Create();

			// two equal strings
			ParagraphCorrelation pc = new ParagraphCorrelation("Hello", "Hello", engine);
			Assert.AreEqual("Hello", pc.LongestUsefulSubstring);

			// LCS at the start
			pc = new ParagraphCorrelation("Hello over there", "Hello over here", engine);
			Assert.AreEqual("Hello over ", pc.LongestUsefulSubstring);

			// LCS in the middle
			pc = new ParagraphCorrelation("I want to be over there",
				"You want to be over here", engine);
			Assert.AreEqual(" want to be over ", pc.LongestUsefulSubstring);

			// LCS at the end
			pc = new ParagraphCorrelation("Will you come to visit my relatives?",
				"Do I ever visit my relatives?", engine);
			Assert.AreEqual(" visit my relatives?", pc.LongestUsefulSubstring);

			// two common strings, find the longest
			pc = new ParagraphCorrelation("This sentence has common words",
				"This paragraph has common words", engine);
			Assert.AreEqual(" has common words", pc.LongestUsefulSubstring);

			// nothing at all in common
			pc = new ParagraphCorrelation("We have nothing in common",
				"absolutely nill items", engine);
			Assert.AreEqual(string.Empty, pc.LongestUsefulSubstring);

			// pathological cases
			pc = new ParagraphCorrelation(string.Empty, string.Empty, engine);
			Assert.AreEqual(string.Empty, pc.LongestUsefulSubstring);
			pc = new ParagraphCorrelation(null, string.Empty, engine);
			Assert.AreEqual(string.Empty, pc.LongestUsefulSubstring);
			pc = new ParagraphCorrelation(string.Empty, "Hello there", engine);
			Assert.AreEqual(string.Empty, pc.LongestUsefulSubstring);
		}
	}
}

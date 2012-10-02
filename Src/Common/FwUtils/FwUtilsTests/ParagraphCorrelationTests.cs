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
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// </summary>
	[TestFixture]
	public class ParagraphCorrelationTests: SIL.FieldWorks.Test.TestUtils.BaseTest
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
	}
}

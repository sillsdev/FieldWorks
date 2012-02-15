// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2012, SIL International. All Rights Reserved.
// <copyright from='2011' to='2012' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: QuestionProviderTests.cs
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SILUBS.SharedScrUtils;

namespace SILUBS.PhraseTranslationHelper
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests the QuestionProviderBase implementation
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class QuestionProviderTests
	{
		[TestFixtureSetUp]
		public void SetupFixture()
		{
			BCVRefTests.InitializeVersificationTable();
		}

		#region Private helper methods
		private Section CreateSection(string sRef, string heading, int startRef, int endRef, int cOverviewQuestions, int cDetailQuestions)
		{
			Section s = new Section();
			s.ScriptureReference = sRef;
			s.Heading = heading;
			s.StartRef = startRef;
			s.EndRef = endRef;
			s.Categories = new Category[2];
			s.Categories[0] = new Category();
			s.Categories[0].Type = "Overview";
			s.Categories[0].Questions = new Question[cOverviewQuestions];
			for (int i = 0; i < cOverviewQuestions; i++)
				s.Categories[0].Questions[i] = new Question();

			s.Categories[1] = new Category();
			s.Categories[1].Type = "Details";
			s.Categories[1].Questions = new Question[cDetailQuestions];
			for (int i = 0; i < cDetailQuestions; i++)
				s.Categories[1].Questions[i] = new Question();
			return s;
		}
		#endregion

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests enumerating overview and detail categories and questions with answers and
		/// comments.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void EnumeratePhrases_Basic()
		{
			QuestionSections qs = new QuestionSections();
			qs.Items = new Section[2];
			int iS= 0;
			qs.Items[iS] = CreateSection("ACT 1.1-5", "Acts 1:1-5 Introduction to the book.", 44001001,
				44001005, 2, 1);
			int iC = 0;
			Question q = qs.Items[iS].Categories[iC].Questions[0];
			q.Text = "What information did Luke, the writer of this book, give in this introduction?";
			q.Answers = new [] { "Luke reminded his readers that he was about to continue the true story about Jesus" };
			q = qs.Items[iS].Categories[iC].Questions[1];
			q.Text = "What do you think an apostle of Jesus is?";
			q.Answers = new [] { "Key Term Check: To be an apostle of Jesus means to be a messenger", "Can also be translated as \"sent one\"" };
			q.Notes = new [] {"Note: apostles can be real sweethearts sometimes"};

			iC = 1;
			q = qs.Items[iS].Categories[iC].Questions[0];
			q.Text = "To whom did the writer of Acts address this book?";
			q.Answers = new [] { "He addressed this book to Theophilus." };

			iS= 1;
			qs.Items[iS] = CreateSection("ACT 1.6-10", "Acts 1:6-10 The continuing saga.", 44001006, 44001010, 1, 1);
			iC = 0;
			q = qs.Items[iS].Categories[iC].Questions[0];
			q.Text = "What happened?";
			q.Answers = new [] { "Stuff" };

			iC = 1;
			q = qs.Items[iS].Categories[iC].Questions[0];
			q.ScriptureReference = "ACT 1.6";
			q.StartRef = 44001006;
			q.EndRef = 44001006;
			q.Text = "What question did the apostles ask Jesus about his kingdom?";
			q.Answers = new [] { "The apostles asked Jesus whether he was soon going to set up his kingdom in a way that everybody could see and cause the people of Israel to have power in that kingdom." };

			QuestionProvider qp = new QuestionProvider(qs, null);

			Assert.AreEqual(2, qp.SectionHeads.Count);
			Assert.AreEqual("Acts 1:1-5 Introduction to the book.", qp.SectionHeads["ACT 1.1-5"]);
			Assert.AreEqual("Acts 1:6-10 The continuing saga.", qp.SectionHeads["ACT 1.6-10"]);
			Assert.AreEqual(1, qp.AvailableBookIds.Length);
			Assert.AreEqual(44, qp.AvailableBookIds[0]);
			List<TranslatablePhrase> phrases = qp.ToList();
			Assert.AreEqual(7, phrases.Count);

			TranslatablePhrase phrase = phrases[0];
			Assert.AreEqual("Overview", phrase.PhraseInUse);
			Assert.AreEqual(-1, phrase.Category);
			Assert.AreEqual(string.Empty, phrase.Reference);
			Assert.AreEqual(001001001, phrase.StartRef);
			Assert.AreEqual(066022021, phrase.EndRef);
			Assert.AreEqual(0, phrase.SequenceNumber);
			Assert.IsNull(phrase.QuestionInfo);

			phrase = phrases[1];
			Assert.AreEqual("What information did Luke, the writer of this book, give in this introduction?",
				phrase.PhraseInUse);
			Assert.AreEqual(0, phrase.Category);
			Assert.AreEqual("ACT 1.1-5", phrase.Reference);
			Assert.AreEqual(44001001, phrase.StartRef);
			Assert.AreEqual(44001005, phrase.EndRef);
			Assert.AreEqual(1, phrase.SequenceNumber);
			Assert.IsNotNull(phrase.QuestionInfo);
			Assert.AreEqual(1, phrase.QuestionInfo.Answers.Count());
			Assert.IsNull(phrase.QuestionInfo.Notes);
			Assert.AreEqual("Luke reminded his readers that he was about to continue the true story about Jesus",
				phrase.QuestionInfo.Answers.First());

			phrase = phrases[2];
			Assert.AreEqual("What do you think an apostle of Jesus is?", phrase.PhraseInUse);
			Assert.AreEqual(0, phrase.Category);
			Assert.AreEqual("ACT 1.1-5", phrase.Reference);
			Assert.AreEqual(44001001, phrase.StartRef);
			Assert.AreEqual(44001005, phrase.EndRef);
			Assert.AreEqual(phrases[1].SequenceNumber + 1, phrase.SequenceNumber);
			Assert.IsNotNull(phrase.QuestionInfo);
			Assert.AreEqual(2, phrase.QuestionInfo.Answers.Count());
			Assert.AreEqual(1, phrase.QuestionInfo.Notes.Count());
			Assert.AreEqual("Key Term Check: To be an apostle of Jesus means to be a messenger",
				phrase.QuestionInfo.Answers.ElementAt(0));
			Assert.AreEqual("Can also be translated as \"sent one\"",
				phrase.QuestionInfo.Answers.ElementAt(1));
			Assert.AreEqual("Note: apostles can be real sweethearts sometimes",
				phrase.QuestionInfo.Notes.ElementAt(0));

			phrase = phrases[3];
			Assert.AreEqual("Details", phrase.PhraseInUse);
			Assert.AreEqual(-1, phrase.Category);
			Assert.AreEqual(string.Empty, phrase.Reference);
			Assert.AreEqual(001001001, phrase.StartRef);
			Assert.AreEqual(066022021, phrase.EndRef);
			Assert.AreEqual(1, phrase.SequenceNumber);
			Assert.IsNull(phrase.QuestionInfo);

			phrase = phrases[4];
			Assert.AreEqual("To whom did the writer of Acts address this book?",
				phrase.PhraseInUse);
			Assert.AreEqual(1, phrase.Category);
			Assert.AreEqual("ACT 1.1-5", phrase.Reference);
			Assert.AreEqual(44001001, phrase.StartRef);
			Assert.AreEqual(44001005, phrase.EndRef);
			Assert.AreEqual(1, phrase.SequenceNumber);
			Assert.IsNotNull(phrase.QuestionInfo);
			Assert.AreEqual(1, phrase.QuestionInfo.Answers.Count());
			Assert.IsNull(phrase.QuestionInfo.Notes);
			Assert.AreEqual("He addressed this book to Theophilus.",
				phrase.QuestionInfo.Answers.First());

			phrase = phrases[5];
			Assert.AreEqual("What happened?", phrase.PhraseInUse);
			Assert.AreEqual(0, phrase.Category);
			Assert.AreEqual("ACT 1.6-10", phrase.Reference);
			Assert.AreEqual(44001006, phrase.StartRef);
			Assert.AreEqual(44001010, phrase.EndRef);
			Assert.AreEqual(1, phrase.SequenceNumber);
			Assert.IsNotNull(phrase.QuestionInfo);
			Assert.AreEqual(1, phrase.QuestionInfo.Answers.Count());
			Assert.IsNull(phrase.QuestionInfo.Notes);
			Assert.AreEqual("Stuff", phrase.QuestionInfo.Answers.First());

			phrase = phrases[6];
			Assert.AreEqual("What question did the apostles ask Jesus about his kingdom?",
				phrase.PhraseInUse);
			Assert.AreEqual(1, phrase.Category);
			Assert.AreEqual("ACT 1.6", phrase.Reference);
			Assert.AreEqual(44001006, phrase.StartRef);
			Assert.AreEqual(44001006, phrase.EndRef);
			Assert.AreEqual(1, phrase.SequenceNumber);
			Assert.IsNotNull(phrase.QuestionInfo);
			Assert.AreEqual(1, phrase.QuestionInfo.Answers.Count());
			Assert.IsNull(phrase.QuestionInfo.Notes);
			Assert.AreEqual("The apostles asked Jesus whether he was soon going to set up his kingdom in a way that everybody could see and cause the people of Israel to have power in that kingdom.",
				phrase.QuestionInfo.Answers.First());
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that excluded questions are properly noted.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ExcludePhrases()
		{
			List<PhraseCustomization> customizations = new List<PhraseCustomization>();
			PhraseCustomization pc = new PhraseCustomization();
			pc.Reference = "ACT 1.1-5";
			pc.OriginalPhrase = "What do you think an apostle of Jesus is?";
			pc.Type = PhraseCustomization.CustomizationType.Deletion;
			customizations.Add(pc);
			pc = new PhraseCustomization();
			pc.Reference = "ACT 1.6";
			pc.OriginalPhrase = "What question did the apostles ask Jesus about his kingdom?";
			pc.Type = PhraseCustomization.CustomizationType.Deletion;
			customizations.Add(pc);

			QuestionSections qs = new QuestionSections();
			qs.Items = new Section[2];
			int iS = 0;
			qs.Items[iS] = CreateSection("ACT 1.1-5", "Acts 1:1-5 Introduction to the book.", 44001001,
				44001005, 2, 1);
			int iC = 0;
			Question q = qs.Items[iS].Categories[iC].Questions[0];
			q.Text = "What information did Luke, the writer of this book, give in this introduction?";
			q.Answers = new[] { "Luke reminded his readers that he was about to continue the true story about Jesus" };
			q = qs.Items[iS].Categories[iC].Questions[1];
			q.Text = "What do you think an apostle of Jesus is?";
			q.Answers = new[] { "Key Term Check: To be an apostle of Jesus means to be a messenger"};

			iC = 1;
			q = qs.Items[iS].Categories[iC].Questions[0];
			q.Text = "To whom did the writer of Acts address this book?";
			q.Answers = new[] { "He addressed this book to Theophilus." };

			iS = 1;
			qs.Items[iS] = CreateSection("ACT 1.6-10", "Acts 1:6-10 The continuing saga.", 44001006, 44001010, 1, 1);
			iC = 0;
			q = qs.Items[iS].Categories[iC].Questions[0];
			q.Text = "What happened?";
			q.Answers = new[] { "Stuff" };

			iC = 1;
			q = qs.Items[iS].Categories[iC].Questions[0];
			q.ScriptureReference = "ACT 1.6";
			q.StartRef = 44001006;
			q.EndRef = 44001006;
			q.Text = "What question did the apostles ask Jesus about his kingdom?";
			q.Answers = new[] { "The apostles asked Jesus whether he was soon going to set up his kingdom in a way that everybody could see and cause the people of Israel to have power in that kingdom." };

			QuestionProvider qp = new QuestionProvider(qs, customizations);

			List<TranslatablePhrase> phrases = qp.ToList();
			Assert.AreEqual(7, phrases.Count);
			Assert.AreEqual(1, qp.AvailableBookIds.Length);
			Assert.AreEqual(44, qp.AvailableBookIds[0]);
			Assert.AreEqual(2, qp.SectionHeads.Count);
			Assert.AreEqual("Acts 1:1-5 Introduction to the book.", qp.SectionHeads["ACT 1.1-5"]);
			Assert.AreEqual("Acts 1:6-10 The continuing saga.", qp.SectionHeads["ACT 1.6-10"]);

			TranslatablePhrase phrase = phrases[0];
			Assert.AreEqual("Overview", phrase.PhraseInUse);
			Assert.IsFalse(phrase.IsExcluded);

			phrase = phrases[1];
			Assert.AreEqual("What information did Luke, the writer of this book, give in this introduction?",
				phrase.PhraseInUse);
			Assert.IsFalse(phrase.IsExcluded);

			phrase = phrases[2];
			Assert.AreEqual("What do you think an apostle of Jesus is?", phrase.PhraseInUse);
			Assert.IsTrue(phrase.IsExcluded);

			phrase = phrases[3];
			Assert.AreEqual("Details", phrase.PhraseInUse);
			Assert.IsFalse(phrase.IsExcluded);

			phrase = phrases[4];
			Assert.AreEqual("To whom did the writer of Acts address this book?",
				phrase.PhraseInUse);
			Assert.IsFalse(phrase.IsExcluded);

			phrase = phrases[5];
			Assert.AreEqual("What happened?", phrase.PhraseInUse);
			Assert.IsFalse(phrase.IsExcluded);

			phrase = phrases[6];
			Assert.AreEqual("What question did the apostles ask Jesus about his kingdom?",
				phrase.PhraseInUse);
			Assert.IsTrue(phrase.IsExcluded);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that questions that have modifications are properly enumerated.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void ModifiedPhrases()
		{
			List<PhraseCustomization> customizations = new List<PhraseCustomization>();
			PhraseCustomization pc = new PhraseCustomization();
			pc.Reference = "ACT 1.1-5";
			pc.OriginalPhrase = "What do you think an apostle of Jesus is?";
			pc.ModifiedPhrase = "What do you think an apostle of Jesus Christ is?";
			pc.Type = PhraseCustomization.CustomizationType.Modification;
			customizations.Add(pc);
			pc = new PhraseCustomization();
			pc.Reference = "ACT 1.6";
			pc.OriginalPhrase = "What question did the apostles ask Jesus about his kingdom?";
			pc.ModifiedPhrase = "What query did the apostles pose to Jesus about his realm?";
			pc.Type = PhraseCustomization.CustomizationType.Modification;
			customizations.Add(pc);

			QuestionSections qs = new QuestionSections();
			qs.Items = new Section[2];
			int iS = 0;
			qs.Items[iS] = CreateSection("ACT 1.1-5", "Acts 1:1-5 Introduction to the book.", 44001001,
				44001005, 2, 1);
			int iC = 0;
			Question q = qs.Items[iS].Categories[iC].Questions[0];
			q.Text = "What information did Luke, the writer of this book, give in this introduction?";
			q.Answers = new[] { "Luke reminded his readers that he was about to continue the true story about Jesus" };
			q = qs.Items[iS].Categories[iC].Questions[1];
			q.Text = "What do you think an apostle of Jesus is?";
			q.Answers = new[] { "Key Term Check: To be an apostle of Jesus means to be a messenger" };

			iC = 1;
			q = qs.Items[iS].Categories[iC].Questions[0];
			q.Text = "To whom did the writer of Acts address this book?";
			q.Answers = new[] { "He addressed this book to Theophilus." };

			iS = 1;
			qs.Items[iS] = CreateSection("ACT 1.6-10", "Acts 1:6-10 The continuing saga.", 44001006, 44001010, 1, 1);
			iC = 0;
			q = qs.Items[iS].Categories[iC].Questions[0];
			q.Text = "What happened?";
			q.Answers = new[] { "Stuff" };

			iC = 1;
			q = qs.Items[iS].Categories[iC].Questions[0];
			q.ScriptureReference = "ACT 1.6";
			q.StartRef = 44001006;
			q.EndRef = 44001006;
			q.Text = "What question did the apostles ask Jesus about his kingdom?";
			q.Answers = new[] { "The apostles asked Jesus whether he was soon going to set up his kingdom in a way that everybody could see and cause the people of Israel to have power in that kingdom." };

			QuestionProvider qp = new QuestionProvider(qs, customizations);

			List<TranslatablePhrase> phrases = qp.ToList();
			Assert.AreEqual(7, phrases.Count);
			Assert.AreEqual(1, qp.AvailableBookIds.Length);
			Assert.AreEqual(44, qp.AvailableBookIds[0]);
			Assert.AreEqual(2, qp.SectionHeads.Count);
			Assert.AreEqual("Acts 1:1-5 Introduction to the book.", qp.SectionHeads["ACT 1.1-5"]);
			Assert.AreEqual("Acts 1:6-10 The continuing saga.", qp.SectionHeads["ACT 1.6-10"]);

			TranslatablePhrase phrase = phrases[0];
			Assert.AreEqual("Overview", phrase.PhraseInUse);
			Assert.AreEqual(-1, phrase.Category);
			Assert.AreEqual(string.Empty, phrase.Reference);
			Assert.AreEqual(001001001, phrase.StartRef);
			Assert.AreEqual(066022021, phrase.EndRef);
			Assert.AreEqual(0, phrase.SequenceNumber);
			Assert.IsNull(phrase.QuestionInfo);

			phrase = phrases[1];
			Assert.AreEqual("What information did Luke, the writer of this book, give in this introduction?",
				phrase.PhraseInUse);
			Assert.AreEqual(0, phrase.Category);
			Assert.AreEqual("ACT 1.1-5", phrase.Reference);
			Assert.AreEqual(44001001, phrase.StartRef);
			Assert.AreEqual(44001005, phrase.EndRef);
			Assert.AreEqual(1, phrase.SequenceNumber);
			Assert.IsNotNull(phrase.QuestionInfo);
			Assert.AreEqual(1, phrase.QuestionInfo.Answers.Count());
			Assert.AreEqual("Luke reminded his readers that he was about to continue the true story about Jesus",
				phrase.QuestionInfo.Answers.First());

			phrase = phrases[2];
			Assert.AreEqual("What do you think an apostle of Jesus Christ is?", phrase.PhraseInUse);
			Assert.AreEqual(0, phrase.Category);
			Assert.AreEqual("ACT 1.1-5", phrase.Reference);
			Assert.AreEqual(44001001, phrase.StartRef);
			Assert.AreEqual(44001005, phrase.EndRef);
			Assert.AreEqual(phrases[1].SequenceNumber + 1, phrase.SequenceNumber);
			Assert.IsNotNull(phrase.QuestionInfo);
			Assert.AreEqual(1, phrase.QuestionInfo.Answers.Count());
			Assert.AreEqual("Key Term Check: To be an apostle of Jesus means to be a messenger",
				phrase.QuestionInfo.Answers.ElementAt(0));

			phrase = phrases[3];
			Assert.AreEqual("Details", phrase.PhraseInUse);
			Assert.AreEqual(-1, phrase.Category);
			Assert.AreEqual(string.Empty, phrase.Reference);
			Assert.AreEqual(001001001, phrase.StartRef);
			Assert.AreEqual(066022021, phrase.EndRef);
			Assert.AreEqual(1, phrase.SequenceNumber);
			Assert.IsNull(phrase.QuestionInfo);

			phrase = phrases[4];
			Assert.AreEqual("To whom did the writer of Acts address this book?",
				phrase.PhraseInUse);
			Assert.AreEqual(1, phrase.Category);
			Assert.AreEqual("ACT 1.1-5", phrase.Reference);
			Assert.AreEqual(44001001, phrase.StartRef);
			Assert.AreEqual(44001005, phrase.EndRef);
			Assert.AreEqual(1, phrase.SequenceNumber);
			Assert.IsNotNull(phrase.QuestionInfo);
			Assert.AreEqual(1, phrase.QuestionInfo.Answers.Count());
			Assert.AreEqual("He addressed this book to Theophilus.",
				phrase.QuestionInfo.Answers.First());

			phrase = phrases[5];
			Assert.AreEqual("What happened?", phrase.PhraseInUse);
			Assert.AreEqual(0, phrase.Category);
			Assert.AreEqual("ACT 1.6-10", phrase.Reference);
			Assert.AreEqual(44001006, phrase.StartRef);
			Assert.AreEqual(44001010, phrase.EndRef);
			Assert.AreEqual(1, phrase.SequenceNumber);
			Assert.IsNotNull(phrase.QuestionInfo);
			Assert.AreEqual(1, phrase.QuestionInfo.Answers.Count());
			Assert.AreEqual("Stuff", phrase.QuestionInfo.Answers.First());

			phrase = phrases[6];
			Assert.AreEqual("What query did the apostles pose to Jesus about his realm?",
				phrase.PhraseInUse);
			Assert.AreEqual(1, phrase.Category);
			Assert.AreEqual("ACT 1.6", phrase.Reference);
			Assert.AreEqual(44001006, phrase.StartRef);
			Assert.AreEqual(44001006, phrase.EndRef);
			Assert.AreEqual(1, phrase.SequenceNumber);
			Assert.IsNotNull(phrase.QuestionInfo);
			Assert.AreEqual(1, phrase.QuestionInfo.Answers.Count());
			Assert.AreEqual("The apostles asked Jesus whether he was soon going to set up his kingdom in a way that everybody could see and cause the people of Israel to have power in that kingdom.",
				phrase.QuestionInfo.Answers.First());
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that questions are properly enumerated when the user has added some.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void AddedPhrases_Basic()
		{
			List<PhraseCustomization> customizations = new List<PhraseCustomization>();
			PhraseCustomization pc = new PhraseCustomization();
			pc.Reference = "ACT 1.1-5";
			pc.OriginalPhrase = "What do you think an apostle of Jesus is?";
			pc.ModifiedPhrase = "Is this question before the one about the meaning of apostle?";
			pc.Answer = "Yup";
			pc.Type = PhraseCustomization.CustomizationType.InsertionBefore;
			customizations.Add(pc);
			pc = new PhraseCustomization();
			pc.Reference = "ACT 1.6";
			pc.OriginalPhrase = "What question did the apostles ask Jesus about his kingdom?";
			pc.ModifiedPhrase = "What did He answer?";
			pc.Type = PhraseCustomization.CustomizationType.AdditionAfter;
			customizations.Add(pc);

			QuestionSections qs = new QuestionSections();
			qs.Items = new Section[2];
			int iS = 0;
			qs.Items[iS] = CreateSection("ACT 1.1-5", "Acts 1:1-5 Introduction to the book.", 44001001,
				44001005, 2, 1);
			int iC = 0;
			Question q = qs.Items[iS].Categories[iC].Questions[0];
			q.Text = "What information did Luke, the writer of this book, give in this introduction?";
			q.Answers = new[] { "Luke reminded his readers that he was about to continue the true story about Jesus" };
			q = qs.Items[iS].Categories[iC].Questions[1];
			q.Text = "What do you think an apostle of Jesus is?";
			q.Answers = new[] { "Key Term Check: To be an apostle of Jesus means to be a messenger" };

			iC = 1;
			q = qs.Items[iS].Categories[iC].Questions[0];
			q.Text = "To whom did the writer of Acts address this book?";
			q.Answers = new[] { "He addressed this book to Theophilus." };

			iS = 1;
			qs.Items[iS] = CreateSection("ACT 1.6-10", "Acts 1:6-10 The continuing saga.", 44001006, 44001010, 1, 1);
			iC = 0;
			q = qs.Items[iS].Categories[iC].Questions[0];
			q.Text = "What happened?";
			q.Answers = new[] { "Stuff" };

			iC = 1;
			q = qs.Items[iS].Categories[iC].Questions[0];
			q.ScriptureReference = "ACT 1.6";
			q.StartRef = 44001006;
			q.EndRef = 44001006;
			q.Text = "What question did the apostles ask Jesus about his kingdom?";
			q.Answers = new[] { "The apostles asked Jesus whether he was soon going to set up his kingdom in a way that everybody could see and cause the people of Israel to have power in that kingdom." };

			QuestionProvider qp = new QuestionProvider(qs, customizations);

			List<TranslatablePhrase> phrases = qp.ToList();
			Assert.AreEqual(9, phrases.Count);
			Assert.AreEqual(1, qp.AvailableBookIds.Length);
			Assert.AreEqual(44, qp.AvailableBookIds[0]);
			Assert.AreEqual(2, qp.SectionHeads.Count);
			Assert.AreEqual("Acts 1:1-5 Introduction to the book.", qp.SectionHeads["ACT 1.1-5"]);
			Assert.AreEqual("Acts 1:6-10 The continuing saga.", qp.SectionHeads["ACT 1.6-10"]);

			TranslatablePhrase phrase = phrases[0];
			Assert.AreEqual("Overview", phrase.PhraseInUse);
			Assert.AreEqual(-1, phrase.Category);
			Assert.AreEqual(string.Empty, phrase.Reference);
			Assert.AreEqual(001001001, phrase.StartRef);
			Assert.AreEqual(066022021, phrase.EndRef);
			Assert.AreEqual(0, phrase.SequenceNumber);
			Assert.IsNull(phrase.QuestionInfo);

			phrase = phrases[1];
			Assert.AreEqual("What information did Luke, the writer of this book, give in this introduction?",
				phrase.PhraseInUse);
			Assert.AreEqual(0, phrase.Category);
			Assert.AreEqual("ACT 1.1-5", phrase.Reference);
			Assert.AreEqual(44001001, phrase.StartRef);
			Assert.AreEqual(44001005, phrase.EndRef);
			Assert.Greater(phrase.SequenceNumber, 0);
			Assert.IsNotNull(phrase.QuestionInfo);
			Assert.AreEqual(1, phrase.QuestionInfo.Answers.Count());
			Assert.AreEqual("Luke reminded his readers that he was about to continue the true story about Jesus",
				phrase.QuestionInfo.Answers.First());

			phrase = phrases[2];
			Assert.AreEqual("Is this question before the one about the meaning of apostle?", phrase.PhraseInUse);
			Assert.AreEqual(0, phrase.Category);
			Assert.AreEqual("ACT 1.1-5", phrase.Reference);
			Assert.AreEqual(44001001, phrase.StartRef);
			Assert.AreEqual(44001005, phrase.EndRef);
			Assert.Less(phrases[1].SequenceNumber, phrase.SequenceNumber);
			Assert.AreEqual("Yup", phrase.QuestionInfo.Answers.First());

			phrase = phrases[3];
			Assert.AreEqual("What do you think an apostle of Jesus is?", phrase.PhraseInUse);
			Assert.AreEqual(0, phrase.Category);
			Assert.AreEqual("ACT 1.1-5", phrase.Reference);
			Assert.AreEqual(44001001, phrase.StartRef);
			Assert.AreEqual(44001005, phrase.EndRef);
			Assert.Less(phrases[2].SequenceNumber, phrase.SequenceNumber);
			Assert.AreEqual("Is this question before the one about the meaning of apostle?", phrase.InsertedPhraseBefore.Text);
			Assert.IsNotNull(phrase.QuestionInfo);
			Assert.AreEqual(1, phrase.QuestionInfo.Answers.Count());
			Assert.AreEqual("Key Term Check: To be an apostle of Jesus means to be a messenger",
				phrase.QuestionInfo.Answers.ElementAt(0));

			phrase = phrases[4];
			Assert.AreEqual("Details", phrase.PhraseInUse);
			Assert.AreEqual(-1, phrase.Category);
			Assert.AreEqual(string.Empty, phrase.Reference);
			Assert.AreEqual(001001001, phrase.StartRef);
			Assert.AreEqual(066022021, phrase.EndRef);
			Assert.AreEqual(1, phrase.SequenceNumber);
			Assert.IsNull(phrase.QuestionInfo);

			phrase = phrases[5];
			Assert.AreEqual("To whom did the writer of Acts address this book?",
				phrase.PhraseInUse);
			Assert.AreEqual(1, phrase.Category);
			Assert.AreEqual("ACT 1.1-5", phrase.Reference);
			Assert.AreEqual(44001001, phrase.StartRef);
			Assert.AreEqual(44001005, phrase.EndRef);
			Assert.Greater(phrase.SequenceNumber, 0);
			Assert.IsNotNull(phrase.QuestionInfo);
			Assert.AreEqual(1, phrase.QuestionInfo.Answers.Count());
			Assert.AreEqual("He addressed this book to Theophilus.",
				phrase.QuestionInfo.Answers.First());

			phrase = phrases[6];
			Assert.AreEqual("What happened?", phrase.PhraseInUse);
			Assert.AreEqual(0, phrase.Category);
			Assert.AreEqual("ACT 1.6-10", phrase.Reference);
			Assert.AreEqual(44001006, phrase.StartRef);
			Assert.AreEqual(44001010, phrase.EndRef);
			Assert.Greater(phrase.SequenceNumber, 0);
			Assert.IsNotNull(phrase.QuestionInfo);
			Assert.AreEqual(1, phrase.QuestionInfo.Answers.Count());
			Assert.AreEqual("Stuff", phrase.QuestionInfo.Answers.First());

			phrase = phrases[7];
			Assert.AreEqual("What question did the apostles ask Jesus about his kingdom?",
				phrase.PhraseInUse);
			Assert.AreEqual(1, phrase.Category);
			Assert.AreEqual("ACT 1.6", phrase.Reference);
			Assert.AreEqual(44001006, phrase.StartRef);
			Assert.AreEqual(44001006, phrase.EndRef);
			Assert.Greater(phrase.SequenceNumber, 0);
			Assert.AreEqual("What did He answer?", phrase.AddedPhraseAfter.Text);
			Assert.IsNotNull(phrase.QuestionInfo);
			Assert.AreEqual(1, phrase.QuestionInfo.Answers.Count());
			Assert.AreEqual("The apostles asked Jesus whether he was soon going to set up his kingdom in a way that everybody could see and cause the people of Israel to have power in that kingdom.",
				phrase.QuestionInfo.Answers.First());

			phrase = phrases[8];
			Assert.AreEqual("What did He answer?",
				phrase.PhraseInUse);
			Assert.AreEqual(1, phrase.Category);
			Assert.AreEqual("ACT 1.6", phrase.Reference);
			Assert.AreEqual(44001006, phrase.StartRef);
			Assert.AreEqual(44001006, phrase.EndRef);
			Assert.Less(phrases[7].SequenceNumber, phrase.SequenceNumber);
			Assert.IsNull(phrase.QuestionInfo.Answers);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that questions are properly enumerated when the user has added some and then
		/// added some more to those.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void AddedPhrases_Compound()
		{
			List<PhraseCustomization> customizations = new List<PhraseCustomization>();
			PhraseCustomization pc = new PhraseCustomization();
			pc.Reference = "ACT 1.1-5";
			pc.OriginalPhrase = "What do you think an apostle of Jesus is?";
			pc.ModifiedPhrase = "Is this question before the one about the meaning of apostle?";
			pc.Answer = "Yup";
			pc.Type = PhraseCustomization.CustomizationType.InsertionBefore;
			customizations.Add(pc);
			pc = new PhraseCustomization();
			pc.Reference = "ACT 1.6";
			pc.OriginalPhrase = "What question did the apostles ask Jesus about his kingdom?";
			pc.ModifiedPhrase = "What did He answer?";
			pc.Answer = "He told them to mind their own business.";
			pc.Type = PhraseCustomization.CustomizationType.AdditionAfter;
			customizations.Add(pc);
			pc = new PhraseCustomization();
			pc.Reference = "ACT 1.1-5";
			pc.OriginalPhrase = "Is this question before the one about the meaning of apostle?";
			pc.ModifiedPhrase = "Is this question before the one before the one about the meaning of apostle?";
			pc.Type = PhraseCustomization.CustomizationType.InsertionBefore;
			customizations.Add(pc);
			pc = new PhraseCustomization();
			pc.Reference = "ACT 1.1-5";
			pc.OriginalPhrase = "Is this question before the one about the meaning of apostle?";
			pc.ModifiedPhrase = "This is going to hurt, isn't it?";
			pc.Type = PhraseCustomization.CustomizationType.AdditionAfter;
			customizations.Add(pc);
			pc = new PhraseCustomization();
			pc.Reference = "ACT 1.6";
			pc.OriginalPhrase = "What did He answer?";
			pc.ModifiedPhrase = "I said, what did He answer?";
			pc.Type = PhraseCustomization.CustomizationType.AdditionAfter;
			customizations.Add(pc);
			pc = new PhraseCustomization();
			pc.Reference = "ACT 1.6";
			pc.OriginalPhrase = "I said, what did He answer?";
			pc.ModifiedPhrase = "Can I just go home now?";
			pc.Type = PhraseCustomization.CustomizationType.AdditionAfter;
			customizations.Add(pc);

			QuestionSections qs = new QuestionSections();
			qs.Items = new Section[2];
			int iS = 0;
			qs.Items[iS] = CreateSection("ACT 1.1-5", "Acts 1:1-5 Introduction to the book.", 44001001,
				44001005, 2, 1);
			int iC = 0;
			Question q = qs.Items[iS].Categories[iC].Questions[0];
			q.Text = "What information did Luke, the writer of this book, give in this introduction?";
			q.Answers = new[] { "Luke reminded his readers that he was about to continue the true story about Jesus" };
			q = qs.Items[iS].Categories[iC].Questions[1];
			q.Text = "What do you think an apostle of Jesus is?";
			q.Answers = new[] { "Key Term Check: To be an apostle of Jesus means to be a messenger" };

			iC = 1;
			q = qs.Items[iS].Categories[iC].Questions[0];
			q.Text = "To whom did the writer of Acts address this book?";
			q.Answers = new[] { "He addressed this book to Theophilus." };

			iS = 1;
			qs.Items[iS] = CreateSection("ACT 1.6-10", "Acts 1:6-10 The continuing saga.", 44001006, 44001010, 1, 1);
			iC = 0;
			q = qs.Items[iS].Categories[iC].Questions[0];
			q.Text = "What happened?";
			q.Answers = new[] { "Stuff" };

			iC = 1;
			q = qs.Items[iS].Categories[iC].Questions[0];
			q.ScriptureReference = "ACT 1.6";
			q.StartRef = 44001006;
			q.EndRef = 44001006;
			q.Text = "What question did the apostles ask Jesus about his kingdom?";
			q.Answers = new[] { "The apostles asked Jesus whether he was soon going to set up his kingdom in a way that everybody could see and cause the people of Israel to have power in that kingdom." };

			QuestionProvider qp = new QuestionProvider(qs, customizations);

			List<TranslatablePhrase> phrases = qp.ToList();
			Assert.AreEqual(13, phrases.Count);
			Assert.AreEqual(1, qp.AvailableBookIds.Length);
			Assert.AreEqual(44, qp.AvailableBookIds[0]);
			Assert.AreEqual(2, qp.SectionHeads.Count);
			Assert.AreEqual("Acts 1:1-5 Introduction to the book.", qp.SectionHeads["ACT 1.1-5"]);
			Assert.AreEqual("Acts 1:6-10 The continuing saga.", qp.SectionHeads["ACT 1.6-10"]);

			TranslatablePhrase phrase = phrases[0];
			Assert.AreEqual("Overview", phrase.PhraseInUse);
			Assert.AreEqual(-1, phrase.Category);
			Assert.AreEqual(string.Empty, phrase.Reference);
			Assert.AreEqual(001001001, phrase.StartRef);
			Assert.AreEqual(066022021, phrase.EndRef);
			Assert.AreEqual(0, phrase.SequenceNumber);
			Assert.IsNull(phrase.QuestionInfo);

			phrase = phrases[1];
			Assert.AreEqual("What information did Luke, the writer of this book, give in this introduction?",
				phrase.PhraseInUse);
			Assert.AreEqual(0, phrase.Category);
			Assert.AreEqual("ACT 1.1-5", phrase.Reference);
			Assert.AreEqual(44001001, phrase.StartRef);
			Assert.AreEqual(44001005, phrase.EndRef);
			Assert.Greater(phrase.SequenceNumber, 0);
			Assert.IsNull(phrase.InsertedPhraseBefore);
			Assert.IsNull(phrase.AddedPhraseAfter);
			Assert.IsNotNull(phrase.QuestionInfo);
			Assert.AreEqual(1, phrase.QuestionInfo.Answers.Count());
			Assert.AreEqual("Luke reminded his readers that he was about to continue the true story about Jesus",
				phrase.QuestionInfo.Answers.First());

			phrase = phrases[2];
			Assert.AreEqual("Is this question before the one before the one about the meaning of apostle?", phrase.PhraseInUse);
			Assert.AreEqual(0, phrase.Category);
			Assert.AreEqual("ACT 1.1-5", phrase.Reference);
			Assert.AreEqual(44001001, phrase.StartRef);
			Assert.AreEqual(44001005, phrase.EndRef);
			Assert.Less(phrases[1].SequenceNumber, phrase.SequenceNumber);
			Assert.IsNull(phrase.InsertedPhraseBefore);
			Assert.IsNull(phrase.AddedPhraseAfter);
			Assert.IsNull(phrase.QuestionInfo.Answers);

			phrase = phrases[3];
			Assert.AreEqual("Is this question before the one about the meaning of apostle?", phrase.PhraseInUse);
			Assert.AreEqual(0, phrase.Category);
			Assert.AreEqual("ACT 1.1-5", phrase.Reference);
			Assert.AreEqual(44001001, phrase.StartRef);
			Assert.AreEqual(44001005, phrase.EndRef);
			Assert.Less(phrases[2].SequenceNumber, phrase.SequenceNumber);
			Assert.AreEqual(phrases[2].QuestionInfo, phrase.InsertedPhraseBefore);
			Assert.AreEqual(phrases[4].QuestionInfo, phrase.AddedPhraseAfter);
			Assert.AreEqual("Yup", phrase.QuestionInfo.Answers.First());

			phrase = phrases[4];
			Assert.AreEqual("This is going to hurt, isn't it?", phrase.PhraseInUse);
			Assert.AreEqual(0, phrase.Category);
			Assert.AreEqual("ACT 1.1-5", phrase.Reference);
			Assert.AreEqual(44001001, phrase.StartRef);
			Assert.AreEqual(44001005, phrase.EndRef);
			Assert.Less(phrases[3].SequenceNumber, phrase.SequenceNumber);
			Assert.IsNull(phrase.InsertedPhraseBefore);
			Assert.IsNull(phrase.AddedPhraseAfter);
			Assert.IsNull(phrase.QuestionInfo.Answers);

			phrase = phrases[5];
			Assert.AreEqual("What do you think an apostle of Jesus is?", phrase.PhraseInUse);
			Assert.AreEqual(0, phrase.Category);
			Assert.AreEqual("ACT 1.1-5", phrase.Reference);
			Assert.AreEqual(44001001, phrase.StartRef);
			Assert.AreEqual(44001005, phrase.EndRef);
			Assert.Less(phrases[4].SequenceNumber, phrase.SequenceNumber);
			Assert.AreEqual("Is this question before the one about the meaning of apostle?", phrase.InsertedPhraseBefore.Text);
			Assert.IsNull(phrase.AddedPhraseAfter);
			Assert.IsNotNull(phrase.QuestionInfo);
			Assert.AreEqual(1, phrase.QuestionInfo.Answers.Count());
			Assert.AreEqual("Key Term Check: To be an apostle of Jesus means to be a messenger",
				phrase.QuestionInfo.Answers.ElementAt(0));

			phrase = phrases[6];
			Assert.AreEqual("Details", phrase.PhraseInUse);
			Assert.AreEqual(-1, phrase.Category);
			Assert.AreEqual(string.Empty, phrase.Reference);
			Assert.AreEqual(001001001, phrase.StartRef);
			Assert.AreEqual(066022021, phrase.EndRef);
			Assert.AreEqual(1, phrase.SequenceNumber);
			Assert.IsNull(phrase.InsertedPhraseBefore);
			Assert.IsNull(phrase.AddedPhraseAfter);
			Assert.IsNull(phrase.QuestionInfo);

			phrase = phrases[7];
			Assert.AreEqual("To whom did the writer of Acts address this book?", phrase.PhraseInUse);
			Assert.AreEqual(1, phrase.Category);
			Assert.AreEqual("ACT 1.1-5", phrase.Reference);
			Assert.AreEqual(44001001, phrase.StartRef);
			Assert.AreEqual(44001005, phrase.EndRef);
			Assert.AreEqual(1, phrase.SequenceNumber);
			Assert.IsNull(phrase.InsertedPhraseBefore);
			Assert.IsNull(phrase.AddedPhraseAfter);
			Assert.IsNotNull(phrase.QuestionInfo);
			Assert.AreEqual(1, phrase.QuestionInfo.Answers.Count());
			Assert.AreEqual("He addressed this book to Theophilus.",
				phrase.QuestionInfo.Answers.First());

			phrase = phrases[8];
			Assert.AreEqual("What happened?", phrase.PhraseInUse);
			Assert.AreEqual(0, phrase.Category);
			Assert.AreEqual("ACT 1.6-10", phrase.Reference);
			Assert.AreEqual(44001006, phrase.StartRef);
			Assert.AreEqual(44001010, phrase.EndRef);
			Assert.AreEqual(1, phrase.SequenceNumber);
			Assert.IsNull(phrase.InsertedPhraseBefore);
			Assert.IsNull(phrase.AddedPhraseAfter);
			Assert.IsNotNull(phrase.QuestionInfo);
			Assert.AreEqual(1, phrase.QuestionInfo.Answers.Count());
			Assert.AreEqual("Stuff", phrase.QuestionInfo.Answers.First());

			phrase = phrases[9];
			Assert.AreEqual("What question did the apostles ask Jesus about his kingdom?",
				phrase.PhraseInUse);
			Assert.AreEqual(1, phrase.Category);
			Assert.AreEqual("ACT 1.6", phrase.Reference);
			Assert.AreEqual(44001006, phrase.StartRef);
			Assert.AreEqual(44001006, phrase.EndRef);
			Assert.Greater(phrase.SequenceNumber, 0);
			Assert.IsNull(phrase.InsertedPhraseBefore);
			Assert.AreEqual(phrases[10].QuestionInfo, phrase.AddedPhraseAfter);
			Assert.IsNotNull(phrase.QuestionInfo);
			Assert.AreEqual(1, phrase.QuestionInfo.Answers.Count());
			Assert.AreEqual("The apostles asked Jesus whether he was soon going to set up his kingdom in a way that everybody could see and cause the people of Israel to have power in that kingdom.",
				phrase.QuestionInfo.Answers.First());

			phrase = phrases[10];
			Assert.AreEqual("What did He answer?", phrase.PhraseInUse);
			Assert.AreEqual(1, phrase.Category);
			Assert.AreEqual("ACT 1.6", phrase.Reference);
			Assert.AreEqual(44001006, phrase.StartRef);
			Assert.AreEqual(44001006, phrase.EndRef);
			Assert.Less(phrases[9].SequenceNumber, phrase.SequenceNumber);
			Assert.IsNull(phrase.InsertedPhraseBefore);
			Assert.AreEqual(phrases[11].QuestionInfo, phrase.AddedPhraseAfter);
			Assert.AreEqual("He told them to mind their own business.", phrase.QuestionInfo.Answers.First());

			phrase = phrases[11];
			Assert.AreEqual("I said, what did He answer?", phrase.PhraseInUse);
			Assert.AreEqual(1, phrase.Category);
			Assert.AreEqual("ACT 1.6", phrase.Reference);
			Assert.AreEqual(44001006, phrase.StartRef);
			Assert.AreEqual(44001006, phrase.EndRef);
			Assert.Less(phrases[10].SequenceNumber, phrase.SequenceNumber);
			Assert.IsNull(phrase.InsertedPhraseBefore);
			Assert.AreEqual(phrases[12].QuestionInfo, phrase.AddedPhraseAfter);
			Assert.IsNull(phrase.QuestionInfo.Answers);

			phrase = phrases[12];
			Assert.AreEqual("Can I just go home now?", phrase.PhraseInUse);
			Assert.AreEqual(1, phrase.Category);
			Assert.AreEqual("ACT 1.6", phrase.Reference);
			Assert.AreEqual(44001006, phrase.StartRef);
			Assert.AreEqual(44001006, phrase.EndRef);
			Assert.Less(phrases[11].SequenceNumber, phrase.SequenceNumber);
			Assert.IsNull(phrase.QuestionInfo.Answers);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that questions are properly enumerated when the user has added a question
		/// after a question that was inserted before a factory-supplied question. This case is
		/// interesting because it makes it hard to get the sequence numbers right.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void AddedPhrases_AddAfterInsertionBefore()
		{
			List<PhraseCustomization> customizations = new List<PhraseCustomization>();
			PhraseCustomization pc = new PhraseCustomization();
			pc.Reference = "ACT 1.6";
			pc.OriginalPhrase = "What question did the apostles ask Jesus about his kingdom?";
			pc.ModifiedPhrase = "Is this question before the one about the meaning of apostle?";
			pc.Type = PhraseCustomization.CustomizationType.InsertionBefore;
			customizations.Add(pc);
			pc = new PhraseCustomization();
			pc.Reference = "ACT 1.6";
			pc.OriginalPhrase = "Is this question before the one about the meaning of apostle?";
			pc.ModifiedPhrase = "This is a phrase after the inserted question.";
			pc.Type = PhraseCustomization.CustomizationType.AdditionAfter;
			customizations.Add(pc);

			QuestionSections qs = new QuestionSections();
			qs.Items = new Section[1];
			qs.Items[0] = CreateSection("ACT 1.1-6", "Acts 1:1-6 Introduction to the book.", 44001001,
				44001006, 0, 1);
			Question q = qs.Items[0].Categories[1].Questions[0];
			q.ScriptureReference = "ACT 1.6";
			q.StartRef = 44001006;
			q.EndRef = 44001006;
			q.Text = "What question did the apostles ask Jesus about his kingdom?";
			q.Answers = new[] { "Stuff." };

			QuestionProvider qp = new QuestionProvider(qs, customizations);

			List<TranslatablePhrase> phrases = qp.ToList();
			Assert.AreEqual(5, phrases.Count);

			TranslatablePhrase phrase = phrases[2];
			Assert.AreEqual("Is this question before the one about the meaning of apostle?", phrase.PhraseInUse);
			Assert.AreEqual(1, phrase.Category);
			Assert.AreEqual("ACT 1.6", phrase.Reference);
			Assert.AreEqual(44001006, phrase.StartRef);
			Assert.AreEqual(44001006, phrase.EndRef);
			Assert.GreaterOrEqual(phrase.SequenceNumber, 0);
			Assert.Less(phrase.SequenceNumber, phrases[3].SequenceNumber);
			Assert.IsNull(phrase.InsertedPhraseBefore);
			Assert.AreEqual(phrases[3].QuestionInfo, phrase.AddedPhraseAfter);
			Assert.IsNull(phrase.QuestionInfo.Answers);

			phrase = phrases[3];
			Assert.AreEqual("This is a phrase after the inserted question.", phrase.PhraseInUse);
			Assert.AreEqual(1, phrase.Category);
			Assert.AreEqual("ACT 1.6", phrase.Reference);
			Assert.AreEqual(44001006, phrase.StartRef);
			Assert.AreEqual(44001006, phrase.EndRef);
			Assert.Less(phrase.SequenceNumber, phrases[4].SequenceNumber);
			Assert.IsNull(phrase.InsertedPhraseBefore);
			Assert.IsNull(phrase.AddedPhraseAfter);
			Assert.IsNull(phrase.QuestionInfo.Answers);

			phrase = phrases[4];
			Assert.AreEqual("What question did the apostles ask Jesus about his kingdom?", phrase.PhraseInUse);
			Assert.AreEqual(1, phrase.Category);
			Assert.AreEqual("ACT 1.6", phrase.Reference);
			Assert.AreEqual(44001006, phrase.StartRef);
			Assert.AreEqual(44001006, phrase.EndRef);
			Assert.AreEqual(phrases[2].QuestionInfo, phrase.InsertedPhraseBefore);
			Assert.IsNull(phrase.AddedPhraseAfter);
			Assert.IsNotNull(phrase.QuestionInfo);
			Assert.AreEqual(1, phrase.QuestionInfo.Answers.Count());
			Assert.AreEqual("Stuff.", phrase.QuestionInfo.Answers.ElementAt(0));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that questions are properly enumerated when the user has added a question
		/// before a question that was added after a question that was inserted before a
		/// factory-supplied question. This case is interesting because it makes it even harder
		/// to get the sequence numbers right.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void AddedPhrases_InsertAfterAdditionAfterInsertionBefore()
		{
			List<PhraseCustomization> customizations = new List<PhraseCustomization>();
			PhraseCustomization pc = new PhraseCustomization();
			pc.Reference = "ACT 1.6";
			pc.OriginalPhrase = "Base question";
			pc.ModifiedPhrase = "AAA";
			pc.Type = PhraseCustomization.CustomizationType.InsertionBefore;
			customizations.Add(pc);
			pc = new PhraseCustomization();
			pc.Reference = "ACT 1.6";
			pc.OriginalPhrase = "AAA";
			pc.ModifiedPhrase = "CCC";
			pc.Type = PhraseCustomization.CustomizationType.AdditionAfter;
			customizations.Add(pc);
			pc = new PhraseCustomization();
			pc.Reference = "ACT 1.6";
			pc.OriginalPhrase = "CCC";
			pc.ModifiedPhrase = "BBB";
			pc.Type = PhraseCustomization.CustomizationType.InsertionBefore;
			customizations.Add(pc);

			QuestionSections qs = new QuestionSections();
			qs.Items = new Section[1];
			qs.Items[0] = CreateSection("ACT 1.1-6", "Acts 1:1-6 Introduction to the book.", 44001001,
				44001006, 0, 1);
			Question q = qs.Items[0].Categories[1].Questions[0];
			q.ScriptureReference = "ACT 1.6";
			q.StartRef = 44001006;
			q.EndRef = 44001006;
			q.Text = "Base question";

			QuestionProvider qp = new QuestionProvider(qs, customizations);

			List<TranslatablePhrase> phrases = qp.ToList();
			Assert.AreEqual(6, phrases.Count);

			TranslatablePhrase phrase = phrases[2];
			Assert.AreEqual("AAA", phrase.PhraseInUse);
			Assert.GreaterOrEqual(phrase.SequenceNumber, 0);
			Assert.Less(phrase.SequenceNumber, phrases[3].SequenceNumber);
			Assert.IsNull(phrase.InsertedPhraseBefore);
			Assert.AreEqual(phrases[4].QuestionInfo, phrase.AddedPhraseAfter);

			phrase = phrases[3];
			Assert.AreEqual("BBB", phrase.PhraseInUse);
			Assert.Less(phrase.SequenceNumber, phrases[4].SequenceNumber);
			Assert.IsNull(phrase.InsertedPhraseBefore);
			Assert.IsNull(phrase.AddedPhraseAfter);

			phrase = phrases[4];
			Assert.AreEqual("CCC", phrase.PhraseInUse);
			Assert.Less(phrase.SequenceNumber, phrases[5].SequenceNumber);
			Assert.AreEqual(phrases[3].QuestionInfo, phrase.InsertedPhraseBefore);
			Assert.IsNull(phrase.AddedPhraseAfter);

			phrase = phrases[5];
			Assert.AreEqual("Base question", phrase.PhraseInUse);
			Assert.AreEqual(phrases[2].QuestionInfo, phrase.InsertedPhraseBefore);
			Assert.IsNull(phrase.AddedPhraseAfter);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that questions are properly enumerated when the user has added a question
		/// before a question that was added after a question that was inserted before a
		/// factory-supplied question. This case is interesting because it makes it even harder
		/// to get the sequence numbers right.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void AddedPhrases_InsertAfterPhraseWithoutEnglishVersion()
		{
			List<PhraseCustomization> customizations = new List<PhraseCustomization>();
			PhraseCustomization pc = new PhraseCustomization();
			pc.Reference = "ACT 1.6";
			pc.OriginalPhrase = "Base question";
			Guid guidOfAddedQuestion = Guid.NewGuid();
			pc.ModifiedPhrase = Question.kGuidPrefix + guidOfAddedQuestion;
			pc.Type = PhraseCustomization.CustomizationType.AdditionAfter;
			customizations.Add(pc);
			pc = new PhraseCustomization();
			pc.Reference = "ACT 1.6";
			pc.OriginalPhrase = Question.kGuidPrefix + guidOfAddedQuestion;
			pc.ModifiedPhrase = "Is this English, or what?";
			pc.Type = PhraseCustomization.CustomizationType.AdditionAfter;
			customizations.Add(pc);

			QuestionSections qs = new QuestionSections();
			qs.Items = new Section[1];
			qs.Items[0] = CreateSection("ACT 1.1-6", "Acts 1:1-6 Introduction to the book.", 44001001,
				44001006, 0, 1);
			Question q = qs.Items[0].Categories[1].Questions[0];
			q.ScriptureReference = "ACT 1.6";
			q.StartRef = 44001006;
			q.EndRef = 44001006;
			q.Text = "Base question";

			QuestionProvider qp = new QuestionProvider(qs, customizations);

			List<TranslatablePhrase> phrases = qp.ToList();
			Assert.AreEqual(5, phrases.Count);

			TranslatablePhrase phrase = phrases[2];
			Assert.AreEqual("Base question", phrase.PhraseInUse);
			Assert.Less(phrase.SequenceNumber, phrases[3].SequenceNumber);
			Assert.IsNull(phrase.InsertedPhraseBefore);
			Assert.AreEqual(phrases[3].QuestionInfo, phrase.AddedPhraseAfter);

			phrase = phrases[3];
			Assert.AreEqual("User-added question with no English version", phrase.PhraseToDisplayInUI);
			Assert.AreEqual(Question.kGuidPrefix + guidOfAddedQuestion, phrase.PhraseKey.Text);
			Assert.AreEqual(string.Empty, phrase.OriginalPhrase);
			Assert.Less(phrase.SequenceNumber, phrases[4].SequenceNumber);
			Assert.IsNull(phrase.InsertedPhraseBefore);
			Assert.AreEqual(phrases[4].QuestionInfo, phrase.AddedPhraseAfter);

			phrase = phrases[4];
			Assert.AreEqual("Is this English, or what?", phrase.PhraseInUse);
			Assert.IsNull(phrase.InsertedPhraseBefore);
			Assert.IsNull(phrase.AddedPhraseAfter);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that questions are properly enumerated when the user has added a question
		/// that doesn't have an English translation (just a GUID).
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void AddedPhrases_AddQuestionWithoutEnglishVersion()
		{
			List<PhraseCustomization> customizations = new List<PhraseCustomization>();
			PhraseCustomization pc = new PhraseCustomization();
			pc.Reference = "ACT 1.6";
			pc.OriginalPhrase = "What question did the apostles ask Jesus about his kingdom?";
			Guid guidOfAddedQuestion = Guid.NewGuid();
			pc.ModifiedPhrase = Question.kGuidPrefix + guidOfAddedQuestion;
			pc.Type = PhraseCustomization.CustomizationType.InsertionBefore;
			customizations.Add(pc);

			QuestionSections qs = new QuestionSections();
			qs.Items = new Section[1];
			qs.Items[0] = CreateSection("ACT 1.1-6", "Acts 1:1-6 Introduction to the book.", 44001001,
				44001006, 0, 1);
			Question q = qs.Items[0].Categories[1].Questions[0];
			q.ScriptureReference = "ACT 1.6";
			q.StartRef = 44001006;
			q.EndRef = 44001006;
			q.Text = "What question did the apostles ask Jesus about his kingdom?";
			q.Answers = new[] { "Stuff." };

			QuestionProvider qp = new QuestionProvider(qs, customizations);

			List<TranslatablePhrase> phrases = qp.ToList();
			Assert.AreEqual(4, phrases.Count);

			TranslatablePhrase phrase = phrases[2];
			Assert.AreEqual(string.Empty, phrase.PhraseInUse);
			Assert.AreEqual("User-added question with no English version", phrase.PhraseToDisplayInUI);
			Assert.AreEqual(Question.kGuidPrefix + guidOfAddedQuestion, phrase.PhraseKey.Text);
			Assert.AreEqual(string.Empty, phrase.OriginalPhrase);
			Assert.AreEqual(1, phrase.Category);
			Assert.AreEqual("ACT 1.6", phrase.Reference);
			Assert.AreEqual(44001006, phrase.StartRef);
			Assert.AreEqual(44001006, phrase.EndRef);
			Assert.GreaterOrEqual(phrase.SequenceNumber, 0);
			Assert.Less(phrase.SequenceNumber, phrases[3].SequenceNumber);
			Assert.IsNull(phrase.InsertedPhraseBefore);
			Assert.IsNull(phrase.AddedPhraseAfter);
			Assert.IsNull(phrase.QuestionInfo.Answers);

			phrase = phrases[3];
			Assert.AreEqual("What question did the apostles ask Jesus about his kingdom?", phrase.PhraseInUse);
			Assert.AreEqual(1, phrase.Category);
			Assert.AreEqual("ACT 1.6", phrase.Reference);
			Assert.AreEqual(44001006, phrase.StartRef);
			Assert.AreEqual(44001006, phrase.EndRef);
			Assert.AreEqual(phrases[2].QuestionInfo, phrase.InsertedPhraseBefore);
			Assert.IsNull(phrase.AddedPhraseAfter);
			Assert.IsNotNull(phrase.QuestionInfo);
			Assert.AreEqual(1, phrase.QuestionInfo.Answers.Count());
			Assert.AreEqual("Stuff.", phrase.QuestionInfo.Answers.ElementAt(0));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that a category that doesn't have a Type specified is skipped when enumerating.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void EnumeratePhrases_UnnamedCategory()
		{
			QuestionSections qs = new QuestionSections();
			qs.Items = new Section[1];
			int iS= 0;
			Section s = new Section();
			qs.Items[iS] = s;

			s.ScriptureReference = "ROM 1.1-17";
			s.Heading = "Romans 1:1-17 Introduction to the book.";
			s.StartRef = 45001001;
			s.EndRef = 45001017;
			s.Categories = new Category[1];
			s.Categories[0] = new Category();
			s.Categories[0].Questions = new Question[1];
			Question q = s.Categories[0].Questions[0] = new Question();
			q.Text = "Who wrote this book?";
			q.Answers = new[] { "Paul." };

			QuestionProvider qp = new QuestionProvider(qs, null);

			Assert.AreEqual(1, qp.SectionHeads.Count);
			Assert.AreEqual("Romans 1:1-17 Introduction to the book.", qp.SectionHeads["ROM 1.1-17"]);
			Assert.AreEqual(1, qp.AvailableBookIds.Length);
			Assert.AreEqual(45, qp.AvailableBookIds[0]);
			List<TranslatablePhrase> phrases = qp.ToList();
			Assert.AreEqual(1, phrases.Count);

			TranslatablePhrase phrase = phrases[0];
			Assert.AreEqual("Who wrote this book?", phrase.PhraseInUse);
			Assert.AreEqual(0, phrase.Category);
			Assert.AreEqual("ROM 1.1-17", phrase.Reference);
			Assert.AreEqual(45001001, phrase.StartRef);
			Assert.AreEqual(45001017, phrase.EndRef);
			Assert.AreEqual(1, phrase.SequenceNumber);
			Assert.IsNotNull(phrase.QuestionInfo);
			Assert.AreEqual(1, phrase.QuestionInfo.Answers.Count());
			Assert.IsNull(phrase.QuestionInfo.Notes);
			Assert.AreEqual("Paul.", phrase.QuestionInfo.Answers.First());
		}


		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that two categories whose types differ only by case are not enumerated
		/// distinctly.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void EnumeratePhrases_CategoriesDifferOnlyByCase()
		{
			QuestionSections qs = new QuestionSections();
			qs.Items = new Section[2];
			int iS = 0;
			qs.Items[iS] = CreateSection("ACT 1.1-5", "Acts 1:1-5 Introduction to the book.", 44001001,
				44001005, 1, 1);
			int iC = 0;
			Question q = qs.Items[iS].Categories[iC].Questions[0];
			q.Text = "What information did Luke, the writer of this book, give in this introduction?";
			q.Answers = new[] { "Luke reminded his readers that he was about to continue the true story about Jesus" };

			iC = 1;
			q = qs.Items[iS].Categories[iC].Questions[0];
			q.Text = "To whom did the writer of Acts address this book?";
			q.Answers = new[] { "He addressed this book to Theophilus." };

			iS = 1;
			qs.Items[iS] = CreateSection("ACT 1.6-10", "Acts 1:6-10 The continuing saga.", 44001006, 44001010, 1, 1);
			iC = 0;
			qs.Items[iS].Categories[iC].Type = "overview";
			q = qs.Items[iS].Categories[iC].Questions[0];
			q.Text = "What happened?";
			q.Answers = new[] { "Stuff" };

			iC = 1;
			qs.Items[iS].Categories[iC].Type = "details";
			q = qs.Items[iS].Categories[iC].Questions[0];
			q.ScriptureReference = "ACT 1.6";
			q.StartRef = 44001006;
			q.EndRef = 44001006;
			q.Text = "What question did the apostles ask Jesus about his kingdom?";
			q.Answers = new[] { "The apostles asked Jesus whether he was soon going to set up his kingdom in a way that everybody could see and cause the people of Israel to have power in that kingdom." };

			QuestionProvider qp = new QuestionProvider(qs, null);

			List<TranslatablePhrase> phrases = qp.ToList();
			Assert.AreEqual(6, phrases.Count);
			Assert.AreEqual(1, qp.AvailableBookIds.Length);
			Assert.AreEqual(44, qp.AvailableBookIds[0]);
			Assert.AreEqual(2, qp.SectionHeads.Count);
			Assert.AreEqual("Acts 1:1-5 Introduction to the book.", qp.SectionHeads["ACT 1.1-5"]);
			Assert.AreEqual("Acts 1:6-10 The continuing saga.", qp.SectionHeads["ACT 1.6-10"]);

			TranslatablePhrase phrase = phrases[0];
			Assert.AreEqual("Overview", phrase.PhraseInUse);
			Assert.AreEqual(-1, phrase.Category);
			Assert.AreEqual(string.Empty, phrase.Reference);
			Assert.AreEqual(001001001, phrase.StartRef);
			Assert.AreEqual(066022021, phrase.EndRef);
			Assert.AreEqual(0, phrase.SequenceNumber);
			Assert.IsNull(phrase.QuestionInfo);

			phrase = phrases[1];
			Assert.AreEqual("What information did Luke, the writer of this book, give in this introduction?",
				phrase.PhraseInUse);
			Assert.AreEqual(0, phrase.Category);
			Assert.AreEqual("ACT 1.1-5", phrase.Reference);
			Assert.AreEqual(44001001, phrase.StartRef);
			Assert.AreEqual(44001005, phrase.EndRef);
			Assert.AreEqual(1, phrase.SequenceNumber);
			Assert.IsNotNull(phrase.QuestionInfo);
			Assert.AreEqual(1, phrase.QuestionInfo.Answers.Count());
			Assert.AreEqual("Luke reminded his readers that he was about to continue the true story about Jesus",
				phrase.QuestionInfo.Answers.First());

			phrase = phrases[2];
			Assert.AreEqual("Details", phrase.PhraseInUse);
			Assert.AreEqual(-1, phrase.Category);
			Assert.AreEqual(string.Empty, phrase.Reference);
			Assert.AreEqual(001001001, phrase.StartRef);
			Assert.AreEqual(066022021, phrase.EndRef);
			Assert.AreEqual(1, phrase.SequenceNumber);
			Assert.IsNull(phrase.QuestionInfo);

			phrase = phrases[3];
			Assert.AreEqual("To whom did the writer of Acts address this book?",
				phrase.PhraseInUse);
			Assert.AreEqual(1, phrase.Category);
			Assert.AreEqual("ACT 1.1-5", phrase.Reference);
			Assert.AreEqual(44001001, phrase.StartRef);
			Assert.AreEqual(44001005, phrase.EndRef);
			Assert.AreEqual(1, phrase.SequenceNumber);
			Assert.IsNotNull(phrase.QuestionInfo);
			Assert.AreEqual(1, phrase.QuestionInfo.Answers.Count());
			Assert.AreEqual("He addressed this book to Theophilus.",
				phrase.QuestionInfo.Answers.First());

			phrase = phrases[4];
			Assert.AreEqual("What happened?", phrase.PhraseInUse);
			Assert.AreEqual(0, phrase.Category);
			Assert.AreEqual("ACT 1.6-10", phrase.Reference);
			Assert.AreEqual(44001006, phrase.StartRef);
			Assert.AreEqual(44001010, phrase.EndRef);
			Assert.AreEqual(1, phrase.SequenceNumber);
			Assert.IsNotNull(phrase.QuestionInfo);
			Assert.AreEqual(1, phrase.QuestionInfo.Answers.Count());
			Assert.AreEqual("Stuff", phrase.QuestionInfo.Answers.First());

			phrase = phrases[5];
			Assert.AreEqual("What question did the apostles ask Jesus about his kingdom?",
				phrase.PhraseInUse);
			Assert.AreEqual(1, phrase.Category);
			Assert.AreEqual("ACT 1.6", phrase.Reference);
			Assert.AreEqual(44001006, phrase.StartRef);
			Assert.AreEqual(44001006, phrase.EndRef);
			Assert.AreEqual(1, phrase.SequenceNumber);
			Assert.IsNotNull(phrase.QuestionInfo);
			Assert.AreEqual(1, phrase.QuestionInfo.Answers.Count());
			Assert.AreEqual("The apostles asked Jesus whether he was soon going to set up his kingdom in a way that everybody could see and cause the people of Israel to have power in that kingdom.",
				phrase.QuestionInfo.Answers.First());
		}
	}
}

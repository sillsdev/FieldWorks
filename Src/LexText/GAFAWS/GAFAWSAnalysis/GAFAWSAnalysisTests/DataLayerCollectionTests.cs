// --------------------------------------------------------------------------------------------
// <copyright from='2003' to='2003' company='SIL International'>
//    Copyright (c) 2003, SIL International. All Rights Reserved.
// </copyright>
//
// File: DataLayerCollectionTests.cs
// Responsibility: RandyR
// Last reviewed:
//
// <remarks>
// Unit tests that exercise the GAFAWS data layer's collection classes.
// </remarks>
//
// --------------------------------------------------------------------------------------------
using System;

using NUnit.Framework;

namespace SIL.WordWorks.GAFAWS
{
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Test class for working over the GAFAWS data layer collections.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	[TestFixture]
	public class DataLayerCollectionTests : DataLayerBase
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public DataLayerCollectionTests()
		{
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initialize a class before each test is run.
		/// This is called by NUnit before each test.
		/// It ensures each test will have a brand new GAFAWSData object to work with.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public void Init()
		{
			m_gd = GAFAWSData.Create();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Try adding nulls to the collections.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		//[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void AddNullItems()
		{
			m_gd.Challenges.Add(null);
			m_gd.Classes.PrefixClasses.Add(null);
			m_gd.Classes.SuffixClasses.Add(null);
			m_gd.Morphemes.Add(null);
			m_gd.Other.XmlElements.Add(null);
			m_gd.WordRecords.Add(null);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Try accessing index less than 0.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void AccessLessThanZero()
		{
			object obj = null;
			obj = m_gd.Challenges[-1];
			obj = m_gd.Classes.PrefixClasses[-1];
			obj = m_gd.Classes.SuffixClasses[-1];
			obj = m_gd.Morphemes[-1];
			obj = m_gd.Other.XmlElements[-1];
			obj = m_gd.WordRecords[-1];
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Try accessing index greater than Count.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void AccessGreaterThanCount()
		{
			int cnt = m_gd.Challenges.Count;
			object obj = m_gd.Challenges[cnt +1];

			cnt = m_gd.Classes.PrefixClasses.Count;
			obj = m_gd.Classes.PrefixClasses[cnt +1];

			cnt = m_gd.Classes.SuffixClasses.Count;
			obj = m_gd.Classes.SuffixClasses[cnt +1];

			cnt = m_gd.Morphemes.Count;
			obj = m_gd.Morphemes[cnt +1];

			cnt = m_gd.Other.XmlElements.Count;
			obj = m_gd.Other.XmlElements[cnt +1];

			cnt = m_gd.WordRecords.Count;
			obj = m_gd.WordRecords[cnt +1];
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Try adding an invalid morpheme to the collection.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		//[Test]
		[ExpectedException(typeof(InvalidOperationException))]
		public void AddInvalidMorpheme()
		{
			m_gd.Morphemes.Add(new Morpheme());
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Find extant and missing morphemes.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void FindMorphemes()
		{
			Morpheme mExtant = new Morpheme(MorphemeType.stem, "S1");
			m_gd.Morphemes.Add(mExtant);
			Assert.IsTrue(m_gd.Morphemes.Contains(mExtant), "Morpheme not found.");

			Morpheme mNotExtant = new Morpheme(MorphemeType.prefix, "P1");
			Assert.IsFalse(m_gd.Morphemes.Contains(mNotExtant), "Is in collection.");
		}
	}
}

// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: StringAndDateTests.cs
// Responsibility: JohnH, RandyR
// Last reviewed:
//
// <remarks>
// Implements StringAndDateTests.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;	// Needed for HashTable.
using System.Diagnostics;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.FDO.FDOTests
{
	//-------------------------------------------------------------------------------
	/// <summary>
	/// Implements various string and date tests. Tests are performed by NUnit.
	/// </summary>
	//-------------------------------------------------------------------------------
	[TestFixture]
	public class StringAndDateTests : InMemoryFdoTestBase
	{
		/// <summary>Random number generator</summary>
		protected Random m_rand = new Random();

		//-------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting and getting a SingleUnicode string
		/// </summary>
		//-------------------------------------------------------------------------------
		[Test]
		public void SetGetSingleUnicode()
		{
			CheckDisposed();

			StStyle st = new StStyle();
			Cache.LangProject.StylesOC.Add(st);
			string s = "NewName" + m_rand.Next().ToString();

			st.Name = s;

			Assert.AreEqual(s, st.Name);
		}

		//-------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting and getting a SingleTsString
		/// </summary>
		//-------------------------------------------------------------------------------
		[Test]
		public void SetGetSingleTsString()
		{
			CheckDisposed();

			m_inMemoryCache.InitializeLexDb();

			int[] morphTypeHVOs = Cache.LangProject.LexDbOA.MorphTypesOA.PossibilitiesOS.HvoArray;
			MoMorphType mmt = new MoMorphType(Cache, morphTypeHVOs[0]);

			// TEST WRITE
			mmt.Prefix = "*";

			// TEST READ
			Assert.AreEqual("*", mmt.Prefix);
		}

		//-------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting and getting a MultiTsString
		/// </summary>
		//-------------------------------------------------------------------------------
		[Test]
		public void SetGetMultiTsString()
		{
			CheckDisposed();

			CmAnthroItem p = new CmAnthroItem();
			Cache.LangProject.AnthroListOA.PossibilitiesOS.Append(p);

			// TEST WRITE
			p.Description.AnalysisDefaultWritingSystem.Text = "analysis description";
			// TEST READ
			Assert.AreEqual("analysis description", p.Description.AnalysisDefaultWritingSystem.Text);
		}

		//-------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting and getting a MultiUnicode string
		/// </summary>
		//-------------------------------------------------------------------------------
		[Test]
		public void SetGetMultiUnicode()
		{
			CheckDisposed();

			m_inMemoryCache.InitializeLexDb();

			WfiWordform w = new WfiWordform();
			Cache.LangProject.WordformInventoryOA.WordformsOC.Add(w);

			// TEST WRITE
			string s = "test" + m_rand.Next().ToString();
			w.Form.AnalysisDefaultWritingSystem = s;

			// TEST READ

			Assert.AreEqual(s, w.Form.AnalysisDefaultWritingSystem);
		}

		//-------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting a StTxtPara
		/// </summary>
		//-------------------------------------------------------------------------------
		[Test]
		public void GetStTxtPara()
		{
			CheckDisposed();

			IText itext = m_inMemoryCache.AddInterlinearTextToLangProj("My Interlinear Text");
			IStTxtPara para = m_inMemoryCache.AddParaToInterlinearTextContents(itext, "Once upon a time...");
			Assert.AreEqual("Once upon a time...", para.Contents.Text);
		}

		#region	Date & Time tests

		/// <summary>
		/// Set a datetime.
		/// </summary>
		[Test]
		public void SetDateTime()
		{
			CheckDisposed();

			m_inMemoryCache.InitializeLexDb();

			ILexEntry lme = Cache.LangProject.LexDbOA.EntriesOC.Add(new LexEntry());
			int flid = (int)LexEntry.LexEntryTags.kflidDateCreated;
			DateTime dt= new DateTime(1900, 1, 2, 10, 5, 1);

			Cache.SetTimeProperty(lme.Hvo, flid, dt);
			Assert.AreEqual(dt, lme.DateCreated, "Wrong set date.");
		}

		/// <summary>
		/// Get a datetime from the database.
		/// </summary>
		[Test]
		public void GetDateTime()
		{
			CheckDisposed();

			m_inMemoryCache.InitializeLexDb();

			ILexEntry lme = Cache.LangProject.LexDbOA.EntriesOC.Add(new LexEntry());
			DateTime dtCreated = new DateTime(2003, 8, 7, 8, 42, 42, 0);
			int flid = (int)LexEntry.LexEntryTags.kflidDateCreated;
			Cache.SetTimeProperty(lme.Hvo, flid, dtCreated);

			DateTime dt = Cache.GetTimeProperty(lme.Hvo, flid);

			Assert.AreEqual(dtCreated, dt, "Wrong create time.");
		}

		/// <summary>
		/// Reset the modified time.
		/// </summary>
		[Test]
		public void ResetModifiedTime()
		{
			CheckDisposed();

			m_inMemoryCache.InitializeLexDb();

			ILexEntry entry = Cache.LangProject.LexDbOA.EntriesOC.Add(new LexEntry());
			DateTime created = new DateTime(2001, 1, 1);
			entry.DateCreated = created;
			Assert.AreEqual(created, entry.DateCreated);

			DateTime modified = new DateTime(2002, 2, 2);
			entry.DateModified = modified;
			Assert.AreEqual(modified, entry.DateModified);

			// Check again, because there was a bug in the in-memory code,
			// where it stored it as the object's main timestamp, rather than
			// as an time property.
			Assert.AreEqual(created, entry.DateCreated);
		}
		#endregion	// Date & Time tests
	}
}

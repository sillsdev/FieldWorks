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
// File: CachingTests.cs
// Responsibility: JohnH, RandyR
// Last reviewed:
//
// <remarks>
// Implements CachingTests class.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Notebk;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// Implements caching tests. Tests are performed by NUnit.
	/// </summary>
	[TestFixture]
	public class CachingTests : InDatabaseFdoTestBase
	{
		/// <summary>
		/// Tests the loading of an owning atomic
		/// </summary>
		[Test]
		public void LoadOwningAtomic()
		{
			CheckDisposed();

			int[] hvos = GetHvosForFirstNObjectsOfClass(m_fdoCache.LangProject.ResearchNotebookOA.RecordsOC.HvoArray, RnEvent.kclsidRnEvent, 3);
			CmObject.LoadOwningAtomicData(m_fdoCache, CmObject.GetTypeFromFWClassID(m_fdoCache,RnEvent.kclsidRnEvent),hvos, -1);

			//first one should have a description; test that it was loaded
			Assert.IsTrue(m_fdoCache.GetObjProperty(hvos[0], (int)RnEvent.RnEventTags.kflidDescription) != 0);

			// test the difference in time it takes to load the atomic toning properties of
			//language project 100 times, using the old method vs. the new.
			//				LangProject lp = new LangProject(m_fdoCache, 1);
			const int kRepetitions = 1;

			int[] hvoLP = {1};
			for(int i=0; i< kRepetitions; i++)
				CmObject.LoadOwningAtomicData(m_fdoCache,CmObject.GetTypeFromFWClassID(m_fdoCache,RnEvent.kclsidRnEvent),  hvos,-1);
			}

		/// <summary>
		/// Tests the loading of vectors.
		/// </summary>
		//This is currently only a smoke test.
		[Test]
		public void LoadVectors()
		{
			CheckDisposed();

			RnEvent temp = new RnEvent();
			int[] hvos = GetHvosForFirstNObjectsOfClass(m_fdoCache.LangProject.ResearchNotebookOA.RecordsOC.HvoArray, RnEvent.kClassId, 3);
			CmObject.LoadVectorData(m_fdoCache, temp.GetType(), hvos);
			foreach(int hvo in hvos)
				m_fdoCache.GetVectorSize(hvo, (int)RnEvent.RnEventTags.kflidParticipants);
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void PreLoadStrings()
		{
			CheckDisposed();

			//review: I don't know how to actually test these, since the  lazy-loading code would make any subsequent
			//	request for the data succeed, even if these had failed.
			m_fdoCache.LoadAllOfMultiUnicode((int)SIL.FieldWorks.FDO.Ling.LexEntry.LexEntryTags.kflidCitationForm, "LexEntry");

			m_fdoCache.LoadAllOfOneWsOfAMultiUnicode((int)SIL.FieldWorks.FDO.Ling.LexEntry.LexEntryTags.kflidCitationForm, "LexEntry",
				m_fdoCache.LangProject.DefaultVernacularWritingSystem);
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		[Ignore("Waiting for a custom field to go into TestLangProj")]
		public void CustomFields()
		{
			CheckDisposed();

			uint flid = GetCustomFlid("LexEntry", "foo");
			Assert.IsTrue(m_fdoCache.GetIsCustomField(flid));

			int hvo = m_fdoCache.LangProject.LexDbOA.EntriesOC.HvoArray[0];
//			LexMajorEntry entry = new LexMajorEntry ();
//			m_fdoCache.LangProject.LexDbOA.EntriesOC.Add(entry);
		//	m_fdoCache.MainCacheAccessor.get_StringProp(entry.Hvo,flid);
			ITsString x= m_fdoCache.GetTsStringProperty(hvo, (int)flid);
			if (x != null)
			{
				int l = x.Length;
				Assert.IsTrue(l==0);
			}

			//m_fdoCache.MainCacheAccessor.RemoveObjRefs

			ITsStrBldr strBldr = TsStrBldrClass.Create();
			strBldr.Replace(0, 0, "Test", null);
//			string s;
//			Assert.IsTrue(TsStringHelper.TsStringsAreEqual(strBldr.GetString(),
//				strBldr.GetString(), out s));

			m_fdoCache.SetTsStringProperty(hvo, (int)flid, strBldr.GetString());
			m_fdoCache.Save();
		}

		private uint GetCustomFlid (string className, string field)
		{
			uint classId = m_fdoCache.MetaDataCacheAccessor.GetClassId(className);
			uint flid = m_fdoCache.MetaDataCacheAccessor.GetFieldId2(classId,field, true);
			if (flid < 0)
			{
				Assert.Fail("This test requires the custom field "+field + " to be installed on " + className+".");
			}
			return flid;
		}
	}
}

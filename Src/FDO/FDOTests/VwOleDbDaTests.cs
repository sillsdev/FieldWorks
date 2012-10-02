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
// File: TestVwOleDbDa.cs
// Responsibility: JohnT
// Last reviewed:
//
// <remarks>
// Tests some aspects of VwOleDbDa, specifically, the pre-loading code.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Notebk;
using System.Data.SqlClient;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Test.TestUtils;

using NUnit.Framework;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// VwOleDbDa tests
	/// </summary>
	[TestFixture]
	public class VwOleDbDaTests : BaseTest
	{
		/// <summary>One of the interfaces to the low-level cache we're testing.</summary>
		protected IVwOleDbDa m_da;
		/// <summary>One of the interfaces to the low-level cache we're testing.</summary>
		protected ISilDataAccess m_sda;
		/// <summary>The FDO cache</summary>
		protected FdoCache m_fdoCache;

		/// <summary>
		/// Initializes a new instance of the <c>TestVwOleDbDa</c> class
		/// </summary>
		public VwOleDbDaTests()
		{
		}

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_fdoCache != null)
					m_fdoCache.DisposeWithWSFactoryShutdown();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_sda = null;
			m_da = null;
			m_fdoCache = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// <summary>
		/// Initialize the FDO cache and open database
		/// </summary>
		/// <remarks>This method is called before each test</remarks>
		[SetUp]
		public void Initialize()
		{
			CheckDisposed();
			if (m_fdoCache != null)
				m_fdoCache.DisposeWithWSFactoryShutdown();
			m_fdoCache = InDatabaseFdoTestBase.SetupCache();
			m_sda = m_fdoCache.MainCacheAccessor;
			m_da = (IVwOleDbDa)m_sda;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public void Exit()
		{
			CheckDisposed();
			m_sda = null;
			m_da = null;
			m_fdoCache.DisposeWithWSFactoryShutdown();
			m_fdoCache = null;
		}


		/// <summary>
		/// Test the process of preloading blocks of data.
		/// </summary>
		[Test]
		public void LoadData()
		{
			// Get the language project as an FDO object
			ILangProject lp = m_fdoCache.LangProject;
			// Use it to get the HVO of the data notebook, but without making an FDO object
			// and thus loading data.
			Assert.IsTrue(m_sda.get_IsPropInCache(lp.Hvo,
					(int)LangProject.LangProjectTags.kflidResearchNotebook,
					(int)CellarModuleDefns.kcptOwningAtom, 0),
				"LP notebook loaded by FDO");
			int hvoNotebook = m_fdoCache.GetObjProperty(lp.Hvo,
				(int)LangProject.LangProjectTags.kflidResearchNotebook);
			Assert.IsFalse(m_sda.get_IsPropInCache(hvoNotebook,
					(int)RnResearchNbk.RnResearchNbkTags.kflidRecords,
					(int)CellarModuleDefns.kcptOwningCollection,0),
				"Nb records not loaded");
			// Do a simple pre-load to get the records.
			IVwDataSpec dts = VwDataSpecClass.Create();
			// Request loading the title of each record. (Note that currently we must make
			// an entry for each concrete class; making one for the base class does not work.)
			dts.AddField(RnAnalysis.kClassId,
				(int)RnGenericRec.RnGenericRecTags.kflidTitle,
				FldType.kftString,
				m_fdoCache.LanguageWritingSystemFactoryAccessor,
				0);
			dts.AddField(RnEvent.kClassId,
				(int)RnGenericRec.RnGenericRecTags.kflidTitle,
				FldType.kftString,
				m_fdoCache.LanguageWritingSystemFactoryAccessor,
				0);
			int[] clsids = new int[1];
			clsids[0] = RnResearchNbk.kClassId;
			int[] hvos = new int[1];
			hvos[0] = hvoNotebook;
			m_da.LoadData(hvos, clsids, hvos.Length, dts, null, true);
			Assert.IsTrue(m_sda.get_IsPropInCache(hvoNotebook,
					(int)RnResearchNbk.RnResearchNbkTags.kflidRecords,
					(int)CellarModuleDefns.kcptOwningCollection,0),
				"Nb records are loaded");
			int chvoRecs = m_sda.get_VecSize(hvoNotebook,
				(int)RnResearchNbk.RnResearchNbkTags.kflidRecords);
			// Be careful what we assert...don't want it too dependent on the exact data.
			// Should be OK to assume at least a few records.
			Assert.IsTrue(chvoRecs > 4, "at least 4 recs");
			int hvoRec3 = m_sda.get_VecItem(hvoNotebook,
				(int)RnResearchNbk.RnResearchNbkTags.kflidRecords, 3);
			Assert.IsTrue(m_sda.get_IsPropInCache(hvoRec3,
					(int)RnGenericRec.RnGenericRecTags.kflidTitle,
					(int)CellarModuleDefns.kcptString,0),
				"Got title of rec 3");
			ITsString qtssRec3Title = m_sda.get_StringProp(hvoRec3,
				(int)RnGenericRec.RnGenericRecTags.kflidTitle);
			// Now get the info through FDO
			ICmObject objRec3 = CmObject.CreateFromDBObject(m_fdoCache, hvoRec3);
			Assert.IsTrue(objRec3 is IRnGenericRec, "object of correct type");
			IRnGenericRec grRec3 = (IRnGenericRec)objRec3;
			// I'd prefer to use full ITsString equality test, but not sure how to get
			// a regular ITsString from FDO obj.
			Assert.AreEqual(qtssRec3Title.Text, grRec3.Title.Text,
				"two ways to retrieve R3 title match");
		}

		/// <summary>
		/// Test autoloading of various property types.
		/// </summary>
		[Test]
		public void AutoLoad()
		{
			// Get the language project as an FDO object
			ILangProject lp = m_fdoCache.LangProject;
			// Use it to get the HVO of the data notebook, but without making an FDO object
			// and thus loading data.
			Assert.IsTrue(m_sda.get_IsPropInCache(lp.Hvo,
				(int)LangProject.LangProjectTags.kflidResearchNotebook,
				(int)CellarModuleDefns.kcptOwningAtom, 0),
				"LP notebook loaded by FDO");
			int hvoNotebook = m_fdoCache.GetObjProperty(lp.Hvo,
				(int)LangProject.LangProjectTags.kflidResearchNotebook);

			// Owning atomic
			Assert.IsFalse(m_sda.get_IsPropInCache(hvoNotebook,
				(int)RnResearchNbk.RnResearchNbkTags.kflidEventTypes,
				(int)CellarModuleDefns.kcptOwningAtom, 0),
				"Notebook event types not preloaded");
			int hvoEventTypes = m_sda.get_ObjectProp(hvoNotebook,
				(int)RnResearchNbk.RnResearchNbkTags.kflidEventTypes);
			Assert.IsTrue(m_sda.get_IsPropInCache(hvoNotebook,
				(int)RnResearchNbk.RnResearchNbkTags.kflidEventTypes,
				(int)CellarModuleDefns.kcptOwningAtom, 0),
				"Notebook event types autoloaded");
			Assert.IsTrue(hvoEventTypes != 0, "got real event types");
			int flidET = m_sda.get_IntProp(hvoEventTypes, (int)CmObjectFields.kflidCmObject_OwnFlid);
			Assert.AreEqual((int)RnResearchNbk.RnResearchNbkTags.kflidEventTypes, flidET,
				"owning flid loaded correctly");
			int clsidET = m_sda.get_IntProp(hvoEventTypes, (int)CmObjectFields.kflidCmObject_Class);
			Assert.AreEqual((int)CmPossibilityList.kClassId, clsidET,
				"class autoloaded");
			int ownerET = m_sda.get_ObjectProp(hvoEventTypes, (int)CmObjectFields.kflidCmObject_Owner);
			Assert.AreEqual(hvoNotebook, ownerET,
				"owner auto-loaded");
			// Todo: test ref atomic.

			// Owning collection.
			Assert.IsFalse(m_sda.get_IsPropInCache(hvoNotebook,
				(int)RnResearchNbk.RnResearchNbkTags.kflidRecords,
				(int)CellarModuleDefns.kcptOwningCollection,0),
				"Nb records not preloaded");
			// Forces a load
			int chvoRecs = m_sda.get_VecSize(hvoNotebook,
				(int)RnResearchNbk.RnResearchNbkTags.kflidRecords);
			Assert.IsTrue(m_sda.get_IsPropInCache(hvoNotebook,
				(int)RnResearchNbk.RnResearchNbkTags.kflidRecords,
				(int)CellarModuleDefns.kcptOwningCollection,0),
				"Nb records autoloaded");
			// Be careful what we assert...don't want it too dependent on the exact data.
			// Should be OK to assume at least a few records.
			Assert.IsTrue(chvoRecs > 4, "at least 4 recs");

			int hvoRec3 = m_sda.get_VecItem(hvoNotebook,
				(int)RnResearchNbk.RnResearchNbkTags.kflidRecords, 3);
			int clsIDR3 = m_sda.get_IntProp(hvoRec3, (int)CmObjectFields.kflidCmObject_Class);
			Assert.IsTrue((int)RnEvent.kClassId == clsIDR3 || (int) RnAnalysis.kClassId == clsIDR3,
				"class of rec 3 valid");
			Assert.IsFalse(m_sda.get_IsPropInCache(hvoRec3,
				(int)RnGenericRec.RnGenericRecTags.kflidResearchers,
				(int)CellarModuleDefns.kcptReferenceCollection,0),
				"R3 researchers not preloaded");
			int chvoR3Researchers = m_sda.get_VecSize(hvoRec3,
				(int)RnGenericRec.RnGenericRecTags.kflidResearchers);
			Assert.IsTrue(m_sda.get_IsPropInCache(hvoRec3,
				(int)RnGenericRec.RnGenericRecTags.kflidResearchers,
				(int)CellarModuleDefns.kcptReferenceCollection,0),
				"R3 researchers autoloaded");
			// We can't assume anything about any one record, but we should be able to find
			// at least one where researchers is non-empty. Along the way we do at least
			// some testing of VecProp and also make sure we can load a string that is a
			// BigString
			bool fGotEmpty = false;
			bool fGotNonEmpty = false;
			bool fGotEvent = false;
			int hvoEvent = 0; // some record will be an event...
			int hvoText = 0; // that has a non-empty structured text...
			int hvoPara = 0; // that has a non-empty paragraph with a label.
			for (int ihvo = 0; ihvo < chvoRecs &&
				((!fGotEmpty) || (!fGotNonEmpty) || (!fGotEvent) || hvoText == 0); ihvo++)
			{
				int hvoRec = m_sda.get_VecItem(hvoNotebook,
					(int)RnResearchNbk.RnResearchNbkTags.kflidRecords, ihvo);
				int chvoResearchers = m_sda.get_VecSize(hvoRec,
					(int)RnGenericRec.RnGenericRecTags.kflidResearchers);
				if (chvoResearchers > 0)
				{
					if (!fGotNonEmpty)
					{
						fGotNonEmpty = true;
						// Try this on the first non-empty list.
						int hvoResearcher = m_sda.get_VecItem(hvoRec,
							(int)RnGenericRec.RnGenericRecTags.kflidResearchers, 0);
						int clsidResearcher = m_sda.get_IntProp(hvoResearcher,
							(int)CmObjectFields.kflidCmObject_Class);
						Assert.AreEqual(CmPerson.kClassId, clsidResearcher, "class of researcher");
					}
				}
				else
				{
					fGotEmpty = true;
					// should now be considered cached anyway.
					Assert.IsTrue(m_sda.get_IsPropInCache(hvoRec,
						(int)RnGenericRec.RnGenericRecTags.kflidResearchers,
						(int)CellarModuleDefns.kcptReferenceCollection,0),
						"empty researchers autoloaded");
				}
				int clsIDRec = m_sda.get_IntProp(hvoRec, (int)CmObjectFields.kflidCmObject_Class);
				if (clsIDRec == (int)RnEvent.kClassId && !fGotEvent)
				{
					hvoEvent = hvoRec;
					hvoText = m_sda.get_ObjectProp(hvoEvent, (int)RnEvent.RnEventTags.kflidDescription);
					if (hvoText != 0)
					{
						int chvo;
						using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative(1000, typeof(int)))
						{
							// if there's a description with more than 1000 paragraphs this will break and
							// we'll fix it then.
							m_sda.VecProp(hvoText, (int)StText.StTextTags.kflidParagraphs,
								1000, out chvo, arrayPtr);
							int[] rgHvo = (int[])MarshalEx.NativeToArray(arrayPtr, chvo, typeof(int));
							// search for the paragraph that has non-empty label (there are a couple).
							for (int ihvoPara = 0; ihvoPara < chvo; ++ihvoPara)
							{
								hvoPara = rgHvo[ihvoPara];
								// BigString
								ITsString tssPara = m_sda.get_StringProp(hvoPara,
									(int)StTxtPara.StTxtParaTags.kflidContents);
								if (tssPara.Length > 0)
								{
									string sname = m_sda.get_UnicodeProp(hvoPara,
										(int)StPara.StParaTags.kflidStyleName);
									if (sname != null && sname.Length > 0)
										fGotEvent = true;
									// Todo: it would be nice to test UnicodePropRgch, but we can't test
									// on the same prop because it's already loaded. Also, the modification
									// for data loading is shared code. We could make another cache instance
									// to test from, maybe? Or keep searching till we find another instance?
									// Could also look for a kcptBigUnicode, but implementation is identical.
								}
							}
						}
					}
				}


			}
			Assert.IsTrue(fGotEmpty && fGotNonEmpty, "found both empty and non-empty researcher lists");
			Assert.IsTrue(hvoEvent != 0, "got at least one event");
			// todo: test sequence (somehow verify order).
			// todo: test ref seq/collection (verify it does NOT set the owner to the referring obj).

			// Ref atomic
			Assert.IsFalse(m_sda.get_IsPropInCache(hvoRec3,
				(int)RnGenericRec.RnGenericRecTags.kflidConfidence,
				(int)CellarModuleDefns.kcptReferenceAtom,0),
				"R3 confidence not preloaded");
			int hvoConfidence = m_sda.get_ObjectProp(hvoRec3,
				(int)RnGenericRec.RnGenericRecTags.kflidConfidence);
			Assert.IsTrue(m_sda.get_IsPropInCache(hvoRec3,
				(int)RnGenericRec.RnGenericRecTags.kflidConfidence,
				(int)CellarModuleDefns.kcptReferenceAtom,0),
				"R3 confidence autoloaded");
			if (hvoConfidence != 0)
			{
				int clsidConfidence = m_sda.get_IntProp(hvoConfidence,
					(int)CmObjectFields.kflidCmObject_Class);
				Assert.AreEqual(CmPossibility.kClassId, clsidConfidence, "class of confidence");
			}


			// TsString.
			Assert.IsFalse(m_sda.get_IsPropInCache(hvoRec3,
				(int)RnGenericRec.RnGenericRecTags.kflidTitle,
				(int)CellarModuleDefns.kcptString,0),
				"title of rec 3 not preloaded");
			ITsString qtssRec3Title = m_sda.get_StringProp(hvoRec3,
				(int)RnGenericRec.RnGenericRecTags.kflidTitle);
			Assert.IsTrue(m_sda.get_IsPropInCache(hvoRec3,
				(int)RnGenericRec.RnGenericRecTags.kflidTitle,
				(int)CellarModuleDefns.kcptString,0),
				"autoloaded title of rec 3");

			// Int (e.g., gendate)
			Assert.IsFalse(m_sda.get_IsPropInCache(hvoEvent,
				(int)RnEvent.RnEventTags.kflidDateOfEvent,
				(int)CellarModuleDefns.kcptInteger,0),
				"date of event not preloaded");
			int nDateEvent = m_sda.get_IntProp(hvoEvent,
				(int)RnEvent.RnEventTags.kflidDateOfEvent);
			Assert.IsTrue(m_sda.get_IsPropInCache(hvoEvent,
				(int)RnEvent.RnEventTags.kflidDateOfEvent,
				(int)CellarModuleDefns.kcptInteger,0),
				"autoloaded date of event");

			// Todo: find example of int64 prop and test.

			// Test loading of binary data (into a TsTextProps).
			object obj = m_sda.get_UnknownProp(hvoPara, (int)StPara.StParaTags.kflidStyleRules);
			Assert.IsNotNull(obj as ITsTextProps);

			// Also loading of raw binary data, using the same prop.
			using (ArrayPtr rgbData = MarshalEx.ArrayToNative(10000, typeof(byte)))
			{
				int cb = -1;
				m_sda.BinaryPropRgb(hvoPara, (int)StPara.StParaTags.kflidStyleRules,
					rgbData, 10000, out cb);
				Assert.IsTrue(cb > 0, "got some bytes using BinaryPropRgb");
				// Enhance JohnT: wish I could figure what they ought to be and test...

				// Get a UserView object (they have no owner, so go direct)
				SqlConnection con = new SqlConnection(string.Format("Server={0}; Database={1}; User ID = fwdeveloper;"
					+ "Password=careful; Pooling=false;",
					m_fdoCache.ServerName, m_fdoCache.DatabaseName));
				con.Open();
				SqlCommand cmd = con.CreateCommand();
				cmd.CommandText ="select top 2 id from UserView";
				SqlDataReader reader= cmd.ExecuteReader();
				reader.Read();
				int hvoUv = reader.GetInt32(0);
				reader.Close();
				con.Close();

				// Guid prop
				Guid guidUv = m_sda.get_GuidProp(hvoUv, (int)UserView.UserViewTags.kflidApp);
				Assert.IsFalse(guidUv == Guid.Empty, "got non-empty guid");

				// Time prop
				long lEventTime = m_sda.get_TimeProp(hvoEvent,
					(int)RnGenericRec.RnGenericRecTags.kflidDateCreated);
				Assert.IsFalse(lEventTime == 0, "got meaningful time"); // Enhance JohnT: really verify.

				// Int prop
				int viewSubtype = m_sda.get_IntProp(hvoUv, (int)UserView.UserViewTags.kflidSubType);
				// Enhance JohnT: think of a way to verify...

				// get_Prop: Time
				object objMod = m_sda.get_Prop(hvoRec3,
					(int)RnGenericRec.RnGenericRecTags.kflidDateModified);
				Assert.IsTrue(objMod is long);
				// get_Prop: String
				int hvoRec0 = m_sda.get_VecItem(hvoNotebook,
					(int)RnResearchNbk.RnResearchNbkTags.kflidRecords, 0);
				object objTitle = m_sda.get_Prop(hvoRec0,
					(int)RnGenericRec.RnGenericRecTags.kflidTitle);
				Assert.IsTrue(objTitle is ITsString, "get_Prop title is string");
				// get_Prop: Int
				object objType = m_sda.get_Prop(hvoUv, (int)UserView.UserViewTags.kflidType);
				Assert.IsTrue(objType is int, "get_Prop type is integer");

				// Confirm some more results by loading through FDO
				ICmObject objRec3 = CmObject.CreateFromDBObject(m_fdoCache, hvoRec3);
				Assert.IsTrue(objRec3 is IRnGenericRec, "object of correct type");
				IRnGenericRec grRec3 = (IRnGenericRec)objRec3;
				// I'd prefer to use full ITsString equality test, but not sure how to get
				// a regular ITsString from FDO obj.
				Assert.AreEqual(qtssRec3Title.Text, grRec3.Title.Text,
					"two ways to retrieve R3 title match");

				// Can't try this yet because FDO GenDate not yet implemented. The data type might not
				// be right anyway.
				//			CmObject objEvent = CmObject.CreateFromDBObject(m_fdoCache, hvoEvent);
				//			Assert.IsTrue(objEvent is RnEvent, "type of event");
				//			RnEvent ev1 = (RnEvent) objEvent;
				//			Assert.AreEqual(ev1.DateOfEvent, nDateEvent, "date of event matches");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that calling UpdatePropIfCached for a guid property that isn't an object
		/// guid will not mess up the cache's internal object cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestUpdatePropIfCached()
		{
			// Create two test objects and add them to the language project.
			// It doesn't really matter what type of object they are as long
			// as the object contains at least one guid property.
			CmFilter testObj1 = new CmFilter();
			CmFilter testObj2 = new CmFilter();
			m_fdoCache.LangProject.FiltersOC.Add(testObj1);
			m_fdoCache.LangProject.FiltersOC.Add(testObj2);

			// Set a guid property for the 2nd test object
			// that is equal to the first test object's guid.
			m_sda.SetGuid(testObj2.Hvo, (int)CmFilter.CmFilterTags.kflidApp, testObj1.Guid);

			// This should not delete guid from the cache's internal object guid cache.
			m_da.UpdatePropIfCached(testObj2.Hvo, testObj2.FieldId,
				(int)CellarModuleDefns.kcptGuid, 0);

			// Make sure we still can create the first test object from it's guid.
			Assert.AreEqual(testObj1.Hvo, m_sda.get_ObjFromGuid(testObj1.Guid));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that calling ClearInfoAbout for an object containing a guid property that
		/// is equal to another object's guid, won't delete the other object's guid from the
		/// cache's internal cache of object guids
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestClearInfoAbout()
		{
			// Create two test objects and add them to the language project.
			// It doesn't really matter what type of object they are as long
			// as the object contains at least one guid property.
			CmFilter testObj1 = new CmFilter();
			CmFilter testObj2 = new CmFilter();
			m_fdoCache.LangProject.FiltersOC.Add(testObj1);
			m_fdoCache.LangProject.FiltersOC.Add(testObj2);

			// Set a guid property for the 2nd test object
			// that is equal to the first test object's guid.
			m_sda.SetGuid(testObj2.Hvo, (int)CmFilter.CmFilterTags.kflidApp, testObj1.Guid);

			// This should not delete the first test object's guid
			// from the cache's internal cache of object guids.
			((IVwCacheDa)m_da).ClearInfoAbout(testObj2.Hvo,
				VwClearInfoAction.kciaRemoveObjectAndOwnedInfo);

			// This will make sure that get_ObjFromGuid()j doesn't defer to
			// the database when it cannot find the object in the cache.
			m_da.AutoloadPolicy = AutoloadPolicies.kalpNoAutoload;

			// Make sure we still can create the first test object from it's guid.
			Assert.AreEqual(testObj1.Hvo, m_sda.get_ObjFromGuid(testObj1.Guid));
		}
	}
}

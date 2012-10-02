// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: Test.cs
// Authorship History: John Hatton
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System.Collections.Generic;
using NUnit.Framework;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.XWorks
{
	public abstract class  RecordClerkTests : XWorksAppTestBase
	{
		protected int m_howManyRecords = 5;
		protected string m_vectorName;

		public RecordClerkTests()//, string configurationFilePath)
		{
			m_vectorName= "";//vectorName;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Instantiate a TestXCoreApp object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
//		[TestFixtureSetUp]
//		public void FixtureInit()
//		{
//			//FwXApp a = new FwXApp();
//		//	DynamicMock app = new DynamicMock(typeof(FwXApp));
//
//			m_window = new FwXWindow(LoadCache("TestLangProj"), null, null, m_configurationFilePath, a, true);// (FwXApp)app.MockInstance);
//
//			/* note that someday, when we write a test to test the persistence function,
//			 * set "TestRestoringFromTestSettings" the second time the application has run in order to pick up
//			 * the settings from the first run. The code for this is already in xWindow.
//			 */
//
//			m_window.Show();
//
//		}
//		protected FdoCache LoadCache(string name)
//		{
//			System.Collections.Hashtable htCacheOptions = new System.Collections.Hashtable();
//			htCacheOptions.Add("db", name);
//			return FdoCache.Create(htCacheOptions);
//		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the TestXCoreApp object is destroyed.
		/// Especially since the splash screen it puts up needs to be closed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
//		[TestFixtureTearDown]
//		public void FixtureCleanUp()
//		{
//		}

//		/// -----------------------------------------------------------------------------------
//		/// <summary>
//		/// </summary>
//		/// -----------------------------------------------------------------------------------
//		[SetUp]
//		public void Init()
//		{
//			Application.DoEvents();
//		}

		//[Test]
		public void NextToTheEnd()
		{

		}

		public string VectorName
		{
			get
			{
				return m_vectorName;
			}
			set
			{
				m_vectorName= value;
			}
		}

		public void NavigatePrevious()
		{
			DoCommandRepeatedly("CmdPreviousRecord", m_howManyRecords);
		}


		protected RecordClerk CorrespondingRecordClerk
		{
			get
			{
				RecordClerk clerk = (RecordClerk)Properties.GetValue(RecordClerk.GetCorrespondingPropertyName(m_vectorName));
				Assert.IsNotNull(clerk);
				return clerk;
			}
		}


		/// <summary>
		///Using the user interface, walk through each of the records and delete each record that is indeed given list.
		/// </summary>
		/// <remarks> This will fail if/when it becomes possible to add records that are filtered out.</remarks>
		/// <param name="records"></param>
		public void DeleteRecords(List<int> records)
		{
			DoCommand("CmdFirstRecord");
			bool dontAdvance = true;
			do
			{
				if (!dontAdvance)
				{
					DoCommand("CmdNextRecord");

				}
				dontAdvance = false;
				int hvo = CorrespondingRecordClerk.CurrentObject.Hvo;
				if(records.Contains(hvo))
				{
					DoCommand("CmdDeleteRecord");
					records.Remove(hvo);
					//removing this record will have the same effect
					//as moving to the next record, so we don't want to advance over it.
					dontAdvance = true;
				}
				else	//cleanup any entries left over from a previous, failed test.
				{
					string s = CorrespondingRecordClerk.CurrentObject.ShortName;
					if (s != null && s.EndsWith("delete"))
					{
						DoCommand("CmdDeleteRecord");
						//removing this record will have the same effect
						//as moving to the next record, so we don't want to advance over it.
						dontAdvance = true;
					}
				}
			} while(!CorrespondingRecordClerk.OnLast && records.  Count > 0);
			Assert.IsTrue(records.Count== 0, "Not all of the inserted records were found, so they were not all deleted.");
		}


		/// <summary>
		/// add some records, then delete them.
		/// </summary>
		/// <param name="insertCmdId"></param>
		public 	void DoInsertAndDeletionTest(string insertCmdId)
		{
			//use widely spaced labels so that we are testing these being put in more than one place,
			//if it is sorted. (lot of ifs here...
			//todo: we need to be more careful about testing the boundary conditions
			string[] labels = new string[]{"  aaaa", "bbbb", "ffff", "zzzzzzzzzzz"};

			//at the moment, the above code  makes no difference because we do not resort automatically
			//any new record is always put at the beginning of the list, because when it is created, its name is empty,
			//which puts it before anything which has characters.

			//however, I leave this in so we will know if we break something when we do automatically sort.
			List<int> addedRecords = new List<int>();
			//add some records
			for(int i=0; i<m_howManyRecords; i++)
			{
				DoCommand(insertCmdId);
				int hvo = CorrespondingRecordClerk.CurrentObject.Hvo;
				addedRecords.Add(hvo);
				var p = (IPartOfSpeech)CorrespondingRecordClerk.CurrentObject;
				FdoCache cache = p.Cache;
				int defAnalWs = cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle;
				p.Name.set_String(defAnalWs, cache.TsStrFactory.MakeString(labels[i % labels.Length] + i + " delete", defAnalWs));
			}
			DeleteRecords(addedRecords);
		}

		public void InsertDelete(string vectorName, string toolId, string insertCmdId)
		{
			VectorName = vectorName;
			SetTool(toolId);
			//DeleteUnnamedRecords();//temp
			DoInsertAndDeletionTest(insertCmdId);
		}

	}

}

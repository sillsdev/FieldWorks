using System;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// Test the CreateModifyTimeManager
	/// </summary>
	///
	[TestFixture]
	public class CreateModifyTimeTests : InMemoryFdoTestBase
	{
		CreateModifyTimeManager m_cmtManager;
		/// <summary>
		/// Make one.
		/// </summary>
		public CreateModifyTimeTests()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Our test will involve lexical database objects, so make a dummy one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_inMemoryCache.InitializeLexDb();
			if (m_cmtManager != null)
				m_cmtManager.Dispose();
			m_cmtManager = new CreateModifyTimeManager(Cache);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shuts down the FDO cache
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			if (m_cmtManager != null)
				m_cmtManager.Dispose();
			m_cmtManager = null;

			base.Exit();
		}

		/// <summary>
		/// Busy-wait until there is a detectable difference between the current time
		/// and the argument. Also resets the cmtManager so that any modification
		/// will record a current time immediately.
		/// </summary>
		/// <param name="start"></param>
		void WaitForDetectableChange(DateTime start)
		{
			m_cmtManager.ResetDelay();
			// busy-wait until there is a detectable difference in current time.
			while (DateTime.Compare(start, DateTime.UtcNow) == 0)
				;
		}

		/// <summary>
		/// Test we can modify an object and its (own) modify time gets set.
		/// </summary>
		[Test]
		public void ModifyMajorObject()
		{
			CheckDisposed();

			ILexDb ldb = Cache.LangProject.LexDbOA;
			ILexEntry le = (ILexEntry)ldb.EntriesOC.Add(new LexEntry());
			DateTime current = le.DateModified.Subtract(TimeSpan.FromMinutes(2.0));
			m_cmtManager.Disabled = true; // prevent resetting as a side-effect of the change!
			le.DateModified = current;
			m_cmtManager.Disabled = false;
			le.CitationForm.VernacularDefaultWritingSystem = "abc";
			Assert.IsTrue(DateTime.Compare(le.DateModified, current) > 0,
				"modify time increased setting string prop");
		}
		/// <summary>
		/// Test we can modify a child object and its (indirect owner) modify time gets set.
		/// </summary>
		[Test]
		public void ModifyMinorObject()
		{
			CheckDisposed();

			ILexDb ldb = Cache.LangProject.LexDbOA;
			ILexEntry le = (ILexEntry)ldb.EntriesOC.Add(new LexEntry());
			ILexSense ls = (ILexSense)le.SensesOS.Append(new LexSense());
			// Don't try this...LexExampleSentence has an initialize method that needs the database.
//			LexExampleSentence es = new LexExampleSentence();
//			ls.ExamplesOS.Append(es);
			ILexSense ls2 = (ILexSense)ls.SensesOS.Append(new LexSense());
			DateTime current = le.DateModified.Subtract(TimeSpan.FromMinutes(2.0));
			m_cmtManager.Disabled = true; // prevent resetting as a side-effect of the change!
			le.DateModified = current;
			m_cmtManager.Disabled = false;
			ls2.Gloss.VernacularDefaultWritingSystem = "abc";
			Assert.IsTrue(DateTime.Compare(le.DateModified, current) > 0,
				"modify time increased setting string prop of 2-level child");

			m_cmtManager.Disabled = true; // prevent resetting as a side-effect of the change!
			le.DateModified = current;
			m_cmtManager.Disabled = false;

			m_cmtManager.PropChanged(le.Hvo, (int)LexDb.LexDbTags.kflidEntries,
				0, 0, 0);

			ILexSense ls3 = (ILexSense)ls2.SensesOS.Append(new LexSense());
			Assert.IsTrue(DateTime.Compare(le.DateModified, current) > 0,
				"modify time increased setting object prop of 2-level child");
		}
	}
}

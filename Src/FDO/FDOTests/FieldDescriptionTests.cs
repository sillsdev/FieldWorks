using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;

using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the FieldDescription class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FieldDescriptionTests : BaseTest
	{
		/// <summary>The FDO cache</summary>
		protected FdoCache m_fdoCache;
		/// <summary>The SQL connection</summary>
		protected SqlConnection m_sqlCon;
		private const string ksLangProj = "TestLangProj";

		/// <summary>
		/// Constructor
		/// </summary>
		public FieldDescriptionTests()
		{
		}

		#region Setup/Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create temp registry settings and unzipped files
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			CheckDisposed();
			base.FixtureSetup();

			m_fdoCache = FdoCache.Create(ksLangProj);
			// For these tests we don't need to run InstallLanguage.
			ILgWritingSystemFactory wsf = m_fdoCache.LanguageWritingSystemFactoryAccessor;
			wsf.BypassInstall = true;
			// Use a transaction because fiddling with Field$ lies outside the Undo/Redo
			// mechanism.
			m_fdoCache.DatabaseAccessor.BeginTrans();
			string sSql = "Server=" + m_fdoCache.ServerName + "; Database=" + m_fdoCache.DatabaseName +
				"; User ID=FWDeveloper; Password=careful; Pooling=false;";
			m_sqlCon = new SqlConnection(sSql);
			m_sqlCon.Open();
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
				if (m_sqlCon != null)
					m_sqlCon.Close();
				if (m_fdoCache != null)
				{
					// Use a transaction because fiddling with Field$ lies outside the Undo/Redo
					// mechanism.
					if (m_fdoCache.DatabaseAccessor.IsTransactionOpen())
						m_fdoCache.DatabaseAccessor.RollbackTrans();
					m_fdoCache.Dispose();
				}

			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_sqlCon = null;
			m_fdoCache = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		#endregion Setup/Teardown

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes sure we get all of the rows in the Field$ table.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FieldDescriptorsCount()
		{
			CheckDisposed();
			SqlDataReader reader = null;
			try
			{
				SqlCommand command = m_sqlCon.CreateCommand();
				command.CommandText = "select count(*) from Field$";
				List<FieldDescription> descriptors = FieldDescription.FieldDescriptors(m_fdoCache);
				Assert.AreEqual(descriptors.Count, (int)command.ExecuteScalar());
			}
			finally
			{
				if (reader != null)
					reader.Close();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes sure we get all of the rows in the Field$ table.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddModifyDeleteFieldDescription()
		{
			CheckDisposed();
			SqlDataReader reader = null;
			try
			{
				FieldDescription fd = new FieldDescription(m_fdoCache);
				fd.Class = 1;
				fd.Userlabel = "TESTJUNK___NotPresent";
				fd.Type = 1;
				// Make sure new ones are created as custom.
				Assert.AreEqual(fd.Custom, 1, "Wrong value for Custom column in new FD.");

				SqlCommand command = m_sqlCon.CreateCommand();
				string qry1 = string.Format("select Id from Field$ where Userlabel='{0}'", fd.Userlabel);
				command.CommandText = qry1;
				object obj = command.ExecuteScalar();
				// Make sure there isn't one named TESTJUNK.
				Assert.IsNull(obj, "New FD should be null.");

				fd.UpdateDatabase();
				command.CommandText = qry1;
				obj = command.ExecuteScalar();
				// Make sure newly created one exists in DB now.
				Assert.IsNotNull(obj, "New FD should not be null.");
				Assert.AreEqual(fd.Id, (int)obj);

				string hs = "Abandon hope all ye who enter here.";
				fd.HelpString = hs;
				fd.UpdateDatabase();
				command.CommandText = string.Format("select Id from Field$ where HelpString='{0}'", fd.HelpString);
				obj = command.ExecuteScalar();
				Assert.IsNotNull(obj, "Modified FD should not be null.");

				fd.MarkForDeletion = true;
				fd.UpdateDatabase();
				command.CommandText = qry1;
				obj = command.ExecuteScalar();
				// Make sure it no longer exists, since we just deleted it.
				Assert.IsNull(obj, "New FD should be null.");
			}
			finally
			{
				if (reader != null)
					reader.Close();
			}
		}
	}
}

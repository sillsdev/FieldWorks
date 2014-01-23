// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: XmlListTests.cs
// Responsibility: FW Team

using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO.Infrastructure.Impl;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the FieldDescription class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FieldDescriptionTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary>the metadata cache</summary>
		protected IFwMetaDataCacheManaged m_mdc;

		#region Setup/Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create temporary FdoCache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="ThreadHelper is disposed in FixtureTeardown()")]
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			m_mdc = (IFwMetaDataCacheManaged)Cache.MetaDataCache;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureTearDown]
		public override void FixtureTeardown()
		{
			base.FixtureTeardown();
			m_mdc = null;
		}

		#endregion Setup/Teardown

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes sure we have the fields in the metadata cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FieldDescriptorsCount()
		{
			int cflid = m_mdc.FieldCount;
			Assert.LessOrEqual(800, cflid);	// was 952 when i checked recently...
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test adding a new field, modifying it, and then deleting it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddModifyDeleteFieldDescription()
		{
			FieldDescription fd = new FieldDescription(Cache)
			{	Class = 1,
				Name = "TESTJUNK___NotPresent",
				Type = CellarPropertyType.Boolean
			};
			Assert.AreEqual(fd.Custom, 1, "Wrong value for Custom column in new FD.");
			Assert.AreEqual(0, fd.Id, "new field should not have been assigned a flid yet");

			int flid;
			try
			{
				flid = m_mdc.GetFieldId2(fd.Class, fd.Name, true);
			}
			catch (FDOInvalidFieldException)
			{
				flid = 0;	// the new implementation throws instead of returning zero.
			}
			Assert.AreEqual(0, flid, "new field should not exist");

			fd.UpdateCustomField();
			flid = m_mdc.GetFieldId2(fd.Class, fd.Name, true);
			Assert.AreNotEqual(0, fd.Id, "field should have been assigned a flid");
			Assert.AreEqual(fd.Id, flid, "new field should exist");

			string hs = "Abandon hope all ye who enter here.";
			fd.HelpString = hs;
			fd.UpdateCustomField();
			string help = m_mdc.GetFieldHelp(fd.Id);
			Assert.AreEqual(hs, help, "Help string should have been updated");

			fd.MarkForDeletion = true;
			fd.UpdateCustomField();
			try
			{
				flid = m_mdc.GetFieldId2(fd.Class, fd.Name, true);
			}
			catch (FDOInvalidFieldException)
			{
				flid = 0;	// the new implementation throws instead of returning zero.
			}
			Assert.AreEqual(0, flid, "new field should have been deleted");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test adding a new field that tries to use a name previously used.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddNewFieldDescriptionWithOldName()
		{
			// Setup test
			var origName = "zCustom Field";
			var newName = "zNew Custom Field User label";
			var customField1 = CreateCustomFieldAndRelabel(origName, newName);
			var firstFlid = m_mdc.GetFieldId2(1, origName, true);
			var secondUserLabel = "unrelatedUserLabel";
			var customField2 = new FieldDescription(Cache)
			{
				Class = 1,
				Name = origName,
				Userlabel = secondUserLabel,
				Type = CellarPropertyType.String
			};

			// SUT
			customField2.UpdateCustomField(); // should change Name slightly

			// Verify
			var newExpectedName = origName + "1";
			var secondFlid = m_mdc.GetFieldId2(customField2.Class, newExpectedName, true);
			Assert.AreNotEqual(0, secondFlid, "Field not, or incorrectly, installed.");
			var actualNewName = m_mdc.GetFieldName(secondFlid);
			Assert.AreEqual(newExpectedName, actualNewName,
				string.Format("Field Name should be changed to {0}.", newExpectedName));
			Assert.AreEqual(secondUserLabel, m_mdc.GetFieldLabel(secondFlid), "User label shouldn't change.");

			// Cleanup
			m_mdc.DeleteCustomField(firstFlid);
			m_mdc.DeleteCustomField(secondFlid);
		}

		private FieldDescription CreateCustomFieldAndRelabel(string fieldname, string ultimateUserLabel)
		{
			// Phase 1: Create 1st Custom Field with original name
			var fd = new FieldDescription(Cache)
			{
				Class = 1,
				Name = fieldname,
				Type = CellarPropertyType.String
			};
			Assert.AreEqual(fd.Custom, 1, "Wrong value for Custom column in new FD.");
			Assert.AreEqual(0, fd.Id, "new field should not have been assigned a flid yet");

			int flid;
			try
			{
				flid = m_mdc.GetFieldId2(fd.Class, fd.Name, true);
			}
			catch (FDOInvalidFieldException)
			{
				flid = 0;	// the new implementation throws instead of returning zero.
			}
			Assert.AreEqual(0, flid, "new field should not exist");

			fd.UpdateCustomField();
			flid = m_mdc.GetFieldId2(fd.Class, fd.Name, true);
			Assert.AreNotEqual(0, fd.Id, "field should have been assigned a flid");
			Assert.AreEqual(fd.Id, flid, "new field should exist");

			// Phase 2: Replace 1st Custom field's User label
			fd.Userlabel = ultimateUserLabel;
			fd.UpdateCustomField();
			Assert.AreEqual(flid, m_mdc.GetFieldId2(fd.Class, fd.Name, true),
				"Flid should not have changed.");
			Assert.AreEqual(fieldname, fd.Name, "Internal field name must not change!");
			Assert.AreEqual(fd.Userlabel, ultimateUserLabel, "Field User label should have changed.");
			return fd;
		}

		/// <summary></summary>
		[Test]
		public void CreatingNumberCustomField_MakesInstancesDirty()
		{
			var fd = new FieldDescription(Cache)
			{
				Class = MoStemAllomorphTags.kClassId,
				Name = "MyNumber",
				Type = CellarPropertyType.Integer
			};

			var le = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var morph = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			le.LexemeFormOA = morph;
			m_actionHandler.EndUndoTask();

			UndoableUnitOfWorkHelper.Do("undo", "redo", m_actionHandler,
				() =>
					{
						fd.UpdateCustomField();
						Assert.That(((UndoStack) m_actionHandler).m_currentBundle.DirtyObjects, Has.Member(morph));
					});
		}
	}
}

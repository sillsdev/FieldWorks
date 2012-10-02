using System;
using System.Collections;

using NUnit.Framework;
using NMock;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Test.TestUtils;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test fixture for FilteredSequenceTests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FilteredSequenceTests : ScrInMemoryFdoTestBase
	{
		#region Data Members
		UserView m_userView;
		DynamicMock m_mockedChooserDlg;
		DynamicMock m_mockedDataAccess;

		ScrBookAnnotations m_annotationsGen;

		IScrScriptureNote m_note1;
		IScrScriptureNote m_note2a;
		IScrScriptureNote m_note2b;
		IScrScriptureNote m_note3;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Implement CreateTestData, called by InMemoryFdoTestBase set up.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_mockedChooserDlg = new DynamicMock(typeof(ICmPossibilitySupplier));
			m_mockedDataAccess = new DynamicMock(typeof(IVwCacheDa));

			m_scrInMemoryCache.InitializeAnnotationDefs();
			m_scrInMemoryCache.InitializeScrAnnotationCategories();
			CreateTestUserView();

			// Create some Scripture annotations
			m_annotationsGen = (ScrBookAnnotations)m_scr.BookAnnotationsOS[0];
			ScrReference ref1 = new ScrReference(1, 1, 1, Paratext.ScrVers.English);
			ScrReference ref2 = new ScrReference(1, 1, 2, Paratext.ScrVers.English);
			ScrReference ref3 = new ScrReference(1, 1, 3, Paratext.ScrVers.English);

			// Insert notes for Genesis 1:1, 1:2, and 1:3
			m_note1 = m_annotationsGen.InsertNote(ref1, ref1, null, null, LangProject.kguidAnnConsultantNote);
			m_note2a = m_annotationsGen.InsertNote(ref2, ref2, null, null, LangProject.kguidAnnConsultantNote);
			m_note2b = m_annotationsGen.InsertNote(ref2, ref2, null, null, LangProject.kguidAnnConsultantNote);
			m_note3 = m_annotationsGen.InsertNote(ref3, ref3, null, null, LangProject.kguidAnnConsultantNote);

			m_note1.AnnotationTypeRA = m_inMemoryCache.m_consultantNoteDefn;
			m_note2a.AnnotationTypeRA = m_inMemoryCache.m_consultantNoteDefn;
			m_note2b.AnnotationTypeRA = m_inMemoryCache.m_translatorNoteDefn;
			m_note3.AnnotationTypeRA = m_inMemoryCache.m_translatorNoteDefn;

			m_note1.ResolutionStatus = NoteStatus.Open;
			m_note2a.ResolutionStatus = NoteStatus.Closed;
			m_note2b.ResolutionStatus = NoteStatus.Open;
			m_note3.ResolutionStatus = NoteStatus.Closed;

			m_note1.CategoriesRS.Append(m_inMemoryCache.m_categoryDiscourse);
			m_note2a.CategoriesRS.Append(m_inMemoryCache.m_categoryGrammar);
			m_note2b.CategoriesRS.Append(m_inMemoryCache.m_categoryGrammar_PronominalRef);
			m_note3.CategoriesRS.Append(m_inMemoryCache.m_categoryGrammar_PronominalRef_ExtendedUse);
			m_note3.CategoriesRS.Append(m_inMemoryCache.m_categoryDiscourse); // This note has 2 categories
		}

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a single user view for testing
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CreateTestUserView()
		{
			m_inMemoryCache.InitializeUserViews();
			IEnumerator userViews = Cache.UserViewSpecs.GetEnumerator();
			userViews.MoveNext();
			m_userView = (UserView)userViews.Current;

			// Scripture is displayed by showing it's Book Annotations (which are ScrScriptureNotes)
			UserViewRec rec = new UserViewRec();
			m_userView.RecordsOC.Add(rec);
			rec.Clsid = Scripture.Scripture.kClassId;

			UserViewField field = new UserViewField();
			rec.FieldsOS.Append(field);
			field.Flid = (int)Scripture.Scripture.ScriptureTags.kflidBookAnnotations;

			// Each ScrBookAnnotations record is displayed by showing its Notes (which are ScrScriptureNotes).
			rec = new UserViewRec();
			m_userView.RecordsOC.Add(rec);
			rec.Clsid = ScrBookAnnotations.kClassId;

			field = new UserViewField();
			rec.FieldsOS.Append(field);
			field.Flid = (int)ScrBookAnnotations.ScrBookAnnotationsTags.kflidNotes;

			// Each ScrScriptureNote record is displayed by showing its status, references, categories, etc.
			rec = new UserViewRec();
			m_userView.RecordsOC.Add(rec);
			rec.Clsid = ScrScriptureNote.kClassId;

			field = new UserViewField();
			rec.FieldsOS.Append(field);
			field.Flid = (int)ScrScriptureNote.ScrScriptureNoteTags.kflidResolutionStatus;

			field = new UserViewField();
			rec.FieldsOS.Append(field);
			field.Flid = (int)CmAnnotation.CmAnnotationTags.kflidAnnotationType;
			field.PossListRAHvo = Cache.LangProject.AnnotationDefsOAHvo;

			field = new UserViewField();
			rec.FieldsOS.Append(field);
			field.Flid = (int)ScrScriptureNote.ScrScriptureNoteTags.kflidCategories;
			field.PossListRAHvo = m_scr.NoteCategoriesOAHvo;
		}
		#endregion

		#region Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test filtering on an integer property
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestLoad_IntFilter()
		{
			CheckDisposed();

			// Set up a filter
			CmFilter filter = new CmFilter();
			Cache.LangProject.FiltersOC.Add(filter);
			filter.ClassId = ScrScriptureNote.kClassId;
			// We will filter ScrScriptureNotes on the ResolutionStatus field.
			filter.ColumnInfo = ScrScriptureNote.kclsidScrScriptureNoteString + "," +
				((int)ScrScriptureNote.ScrScriptureNoteTags.kflidResolutionStatus).ToString();
			filter.ShowPrompt = 0;
			CmRow row = new CmRow();
			filter.RowsOS.Append(row);
			CmCell cell = new CmCell();
			row.CellsOS.Append(cell);
			// Now specify the matching criteria for this filter cell
			ITsStrFactory factory = TsStrFactoryClass.Create();
			cell.Contents.UnderlyingTsString = factory.MakeString("= 0", Cache.DefaultUserWs);

			// Construct a handler to apply the above filter.
			filter.UserView = m_userView;
			FilteredSequenceHandler handler = new FilteredSequenceHandler(Cache,
				ScrBookAnnotations.kClassId, -42, filter, null, m_userView);
			// CacheVecProp() should be called with an array of HVOs representing only the open notes.
			m_mockedDataAccess.Expect("CacheVecProp",
				new object[] { m_annotationsGen.Hvo, -94, new int[] { m_note1.Hvo, m_note2b.Hvo }, 2 });

			// Now test the Load method
			handler.Load(m_annotationsGen.Hvo, -94, -1, (IVwCacheDa)m_mockedDataAccess.MockInstance);
			m_mockedDataAccess.Verify();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test not filtering (i.e., filter == null)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestLoad_NoFilter()
		{
			CheckDisposed();

			// Construct a handler to apply the non-existent filter.
			FilteredSequenceHandler handler = new FilteredSequenceHandler(Cache,
				ScrBookAnnotations.kClassId, -42, m_userView);
			// CacheVecProp() should be called with an array of HVOs representing all notes.
			m_mockedDataAccess.Expect("CacheVecProp",
				new object[] { m_annotationsGen.Hvo, -94,
					new int[] { m_note1.Hvo, m_note2a.Hvo, m_note2b.Hvo, m_note3.Hvo }, 4 });

			// Now test the Load method
			handler.Load(m_annotationsGen.Hvo, -94, -1, (IVwCacheDa)m_mockedDataAccess.MockInstance);
			m_mockedDataAccess.Verify();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test filtering on an atomic object property
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestLoad_AtomicObjectFilter()
		{
			CheckDisposed();

			// Set up a filter
			CmFilter filter = new CmFilter();
			Cache.LangProject.FiltersOC.Add(filter);
			filter.ClassId = ScrScriptureNote.kClassId;
			// We will filter ScrScriptureNotes on the AnnotationType field (which is actually a
			// field of the base class CmAnnotation).
			filter.ColumnInfo = ScrScriptureNote.kclsidScrScriptureNoteString + "," +
				((int)CmAnnotation.CmAnnotationTags.kflidAnnotationType).ToString();
			filter.ShowPrompt = 0;
			CmRow row = new CmRow();
			filter.RowsOS.Append(row);
			CmCell cell = new CmCell();
			row.CellsOS.Append(cell);
			// Now specify the matching criteria for this filter cell
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Matches ",
				SIL.FieldWorks.Common.FwUtils.StyleUtils.CharStyleTextProps(null, Cache.DefaultUserWs));
			StringUtils.InsertOrcIntoPara(m_inMemoryCache.m_translatorNoteDefn.Guid,
				FwObjDataTypes.kodtNameGuidHot,	bldr, bldr.Length,
				bldr.Length, Cache.DefaultUserWs);
			cell.Contents.UnderlyingTsString = bldr.GetString();

			// Construct a handler to apply the above filter.
			filter.UserView = m_userView;
			FilteredSequenceHandler handler = new FilteredSequenceHandler(Cache,
				ScrBookAnnotations.kClassId, -42, filter, null, m_userView);
			// CacheVecProp() should be called with an array of HVOs representing only the translator notes.
			m_mockedDataAccess.Expect("CacheVecProp",
				new object[] {m_annotationsGen.Hvo, -94, new int[] {m_note2b.Hvo, m_note3.Hvo}, 2});

			// Now test the Load method
			handler.Load(m_annotationsGen.Hvo, -94, -1, (IVwCacheDa)m_mockedDataAccess.MockInstance);
			m_mockedDataAccess.Verify();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test filtering on an object sequence property when the filter criteria has a
		/// default value set
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestLoad_ObjectSequenceFilterWithDefault()
		{
			CheckDisposed();

			// Set up a filter
			CmFilter filter = new CmFilter();
			Cache.LangProject.FiltersOC.Add(filter);
			filter.ClassId = ScrScriptureNote.kClassId;
			// We will filter ScrScriptureNotes on the Categories field.
			filter.ColumnInfo = ScrScriptureNote.kclsidScrScriptureNoteString + "," +
				((int)ScrScriptureNote.ScrScriptureNoteTags.kflidCategories).ToString();
			filter.ShowPrompt = 1;
			CmRow row = new CmRow();
			filter.RowsOS.Append(row);
			CmCell cell = new CmCell();
			row.CellsOS.Append(cell);
			// Now specify the matching criteria for this filter cell
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Matches ",
				SIL.FieldWorks.Common.FwUtils.StyleUtils.CharStyleTextProps(null, Cache.DefaultUserWs));
			StringUtils.InsertOrcIntoPara(m_inMemoryCache.m_categoryDiscourse.Guid,
				FwObjDataTypes.kodtNameGuidHot, bldr, bldr.Length, bldr.Length, Cache.DefaultUserWs);
			cell.Contents.UnderlyingTsString = bldr.GetString();

			// Set up mocked ChooserDlg to expect to be called with the default category of Discourse (from
			// filter cell criteria defined above) but return Grammar category.
			m_mockedChooserDlg.ExpectAndReturn("GetPossibility", m_inMemoryCache.m_categoryGrammar.Hvo,
				new object[] {m_scr.NoteCategoriesOA as CmPossibilityList, m_inMemoryCache.m_categoryDiscourse.Hvo});

			// Construct a handler to apply the above filter.
			filter.UserView = m_userView;
			filter.PossibilitySupplier = (ICmPossibilitySupplier)m_mockedChooserDlg.MockInstance;
			FilteredSequenceHandler handler = new FilteredSequenceHandler(Cache,
				ScrBookAnnotations.kClassId, -42, filter, null, m_userView);

			// Now test the Load method
			// CacheVecProp() should be called with an array of HVOs representing only the Grammar note.
			m_mockedDataAccess.Expect("CacheVecProp",
				new object[] {m_annotationsGen.Hvo, -94, new int[] {m_note2a.Hvo}, 1});
			handler.Load(m_annotationsGen.Hvo, -94, -1, (IVwCacheDa)m_mockedDataAccess.MockInstance);
			m_mockedDataAccess.Verify();
			m_mockedChooserDlg.Verify();

			// Make sure the newly-selected object (i.e., the Grammar category) has been stored as the
			// filter criteria (for next time).
			StringUtils.InsertOrcIntoPara(m_inMemoryCache.m_categoryGrammar.Guid,
				FwObjDataTypes.kodtNameGuidHot,	bldr, bldr.Length - 1,
				bldr.Length, Cache.DefaultUserWs);
			AssertEx.AreTsStringsEqual(bldr.GetString(), cell.Contents.UnderlyingTsString);
		}

//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		/// Test filtering on an object sequence property when the filter criteria changes
//		/// </summary>
//		/// ------------------------------------------------------------------------------------
//		[Test]
//		public void TestLoad_ObjectSequenceFilter_Reinitialize()
//		{
//			CheckDisposed();
//
//			// Set up a filter
//			CmFilter filter = new CmFilter();
//			m_fdoCache.LangProject.FiltersOC.Add(filter);
//			filter.ClassId = ScrScriptureNote.kClassId;
//			// We will filter ScrScriptureNotes on the Categories field.
//			filter.ColumnInfo = ScrScriptureNote.kclsidScrScriptureNoteString + "," +
//				((int)ScrScriptureNote.ScrScriptureNoteTags.kflidCategories).ToString();
//			filter.ShowPrompt = 0;
//			CmRow row = new CmRow();
//			filter.RowsOS.Append(row);
//			CmCell cell = new CmCell();
//			row.CellsOS.Append(cell);
//			// Now specify the matching criteria for this filter cell
//			ITsStrBldr bldr = TsStrBldrClass.Create();
//			bldr.Replace(0, 0, "Matches ",
//				SIL.FieldWorks.Common.FwUtils.StyleUtils.CharStyleTextProps(null, m_fdoCache.DefaultUserWs));
//			StringUtils.InsertOrcIntoPara(m_inMemoryCache.m_categoryDiscourse.Guid,
//				FwObjDataTypes.kodtNameGuidHot,	bldr, bldr.Length, bldr.Length, m_fdoCache.DefaultUserWs);
//			cell.Contents.UnderlyingTsString = bldr.GetString();
//
//			// Construct a handler to apply the above filter.
//			filter.UserView = m_userView;
//			FilteredSequenceHandler handler = new FilteredSequenceHandler(m_fdoCache,
//				ScrBookAnnotations.kClassId, -42, filter, null);
//
//			// Now test the Load method
//			// CacheVecProp() should be called with an array of HVOs representing only the Grammar note.
//			m_mockedDataAccess.Expect("CacheVecProp",
//				new object[] {m_annotationsGen.Hvo, -94, new int[] {m_note2a.Hvo}, 1});
//
//			handler.Load(m_annotationsGen.Hvo, -94, -1, (IVwCacheDa)m_mockedDataAccess.MockInstance);
//
//			// Now test the ReinitializeMethod
//			m_mockedDataAccess.Expect("ClearInfoAbout", new object[] {m_annotationsGen.Hvo, false});
//
//			m_mockedDataAccess.Verify();
//
//			// Make sure the newly-selected object (i.e., the Grammar category) has been stored as the
//			// filter criteria (for next time).
//			StringUtils.InsertOrcIntoPara(m_inMemoryCache.m_categoryGrammar.Guid,
//				FwObjDataTypes.kodtNameGuidHot,	bldr, bldr.Length - 1,
//				bldr.Length, m_fdoCache.DefaultUserWs);
//			AssertEx.AreTsStringsEqual(bldr.GetString(), cell.Contents.UnderlyingTsString);
//		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test filtering on an object sequence property when the filter criteria has no
		/// default value set
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestLoad_ObjectSequenceFilterWithoutDefault_NoMatchingRecordsExpected()
		{
			CheckDisposed();

			// Set up a filter
			CmFilter filter = new CmFilter();
			Cache.LangProject.FiltersOC.Add(filter);
			filter.ClassId = ScrScriptureNote.kClassId;
			// We will filter ScrScriptureNotes on the Categories field.
			filter.ColumnInfo = ScrScriptureNote.kclsidScrScriptureNoteString + "," +
				((int)ScrScriptureNote.ScrScriptureNoteTags.kflidCategories).ToString();
			filter.ShowPrompt = 1;
			CmRow row = new CmRow();
			filter.RowsOS.Append(row);
			CmCell cell = new CmCell();
			row.CellsOS.Append(cell);
			// Now specify the matching criteria for this filter cell
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Matches ",
				SIL.FieldWorks.Common.FwUtils.StyleUtils.CharStyleTextProps(null, Cache.DefaultUserWs));
			cell.Contents.UnderlyingTsString = bldr.GetString();

			// Set up mocked ChooserDlg to expect to be called with no default category (i.e., 0)
			// but return Gnarly category.
			m_mockedChooserDlg.ExpectAndReturn("GetPossibility", m_inMemoryCache.m_categoryGnarly.Hvo,
				new object[] {m_scr.NoteCategoriesOA, 0});

			// Construct a handler to apply the above filter.
			filter.UserView = m_userView;
			filter.PossibilitySupplier = (ICmPossibilitySupplier)m_mockedChooserDlg.MockInstance;
			FilteredSequenceHandler handler = new FilteredSequenceHandler(Cache,
				ScrBookAnnotations.kClassId, -42, filter, null, m_userView);

			// Now test the Load method
			// CacheVecProp() should be called with an empty array of HVOs (no notes are gnarly).
			m_mockedDataAccess.Expect("CacheVecProp",
				new object[] {m_annotationsGen.Hvo, -94, new int[] {}, 0});
			handler.Load(m_annotationsGen.Hvo, -94, -1, (IVwCacheDa)m_mockedDataAccess.MockInstance);
			m_mockedDataAccess.Verify();
			m_mockedChooserDlg.Verify();

			// Make sure the newly-selected object (i.e., the Gnarly category) has been stored as the
			// filter criteria (for next time).
			StringUtils.InsertOrcIntoPara(m_inMemoryCache.m_categoryGnarly.Guid,
				FwObjDataTypes.kodtNameGuidHot,	bldr, bldr.Length,
				bldr.Length, Cache.DefaultUserWs);
			AssertEx.AreTsStringsEqual(bldr.GetString(), cell.Contents.UnderlyingTsString);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test filtering on an object sequence property where subitems have to match.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestLoad_ObjectSequenceFilterWithSubItems()
		{
			CheckDisposed();

			// Set up a filter
			CmFilter filter = new CmFilter();
			Cache.LangProject.FiltersOC.Add(filter);
			filter.ClassId = ScrScriptureNote.kClassId;
			// We will filter ScrScriptureNotes on the Categories field.
			filter.ColumnInfo = ScrScriptureNote.kclsidScrScriptureNoteString + "," +
				((int)ScrScriptureNote.ScrScriptureNoteTags.kflidCategories).ToString();
			filter.ShowPrompt = 1;
			CmRow row = new CmRow();
			filter.RowsOS.Append(row);
			CmCell cell = new CmCell();
			row.CellsOS.Append(cell);
			// Now specify the matching criteria for this filter cell
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Matches  +subitems",
				SIL.FieldWorks.Common.FwUtils.StyleUtils.CharStyleTextProps(null, Cache.DefaultUserWs));
			StringUtils.InsertOrcIntoPara(m_inMemoryCache.m_categoryDiscourse.Guid,
				FwObjDataTypes.kodtNameGuidHot, bldr, 8, 8, Cache.DefaultUserWs);
			cell.Contents.UnderlyingTsString = bldr.GetString();

			// Set up mocked ChooserDlg to expect to be called with the default category of Discourse (from
			// filter cell criteria defined above) but return Grammar category.
			m_mockedChooserDlg.ExpectAndReturn("GetPossibility", m_inMemoryCache.m_categoryGrammar.Hvo,
				new object[] {m_scr.NoteCategoriesOA, m_inMemoryCache.m_categoryDiscourse.Hvo});

			// Construct a handler to apply the above filter.
			filter.UserView = m_userView;
			filter.PossibilitySupplier = (ICmPossibilitySupplier)m_mockedChooserDlg.MockInstance;
			FilteredSequenceHandler handler = new FilteredSequenceHandler(Cache,
				ScrBookAnnotations.kClassId, -42, filter, null, m_userView);

			// Now test the Load method
			// CacheVecProp() should be called with an array of HVOs representing all the Grammar notes,
			// including those that refer to sub-items of the main Grammar category.
			m_mockedDataAccess.Expect("CacheVecProp",
				new object[] {m_annotationsGen.Hvo, -94, new int[] {m_note2a.Hvo, m_note2b.Hvo, m_note3.Hvo}, 3});
			handler.Load(m_annotationsGen.Hvo, -94, -1, (IVwCacheDa)m_mockedDataAccess.MockInstance);
			m_mockedDataAccess.Verify();
			m_mockedChooserDlg.Verify();

			// Make sure the newly-selected object (i.e., the Grammar category) has been stored as the
			// filter criteria (for next time).
			StringUtils.InsertOrcIntoPara(m_inMemoryCache.m_categoryGrammar.Guid,
				FwObjDataTypes.kodtNameGuidHot, bldr, 8, 9, Cache.DefaultUserWs);
			AssertEx.AreTsStringsEqual(bldr.GetString(), cell.Contents.UnderlyingTsString);
		}
		#endregion
	}
}

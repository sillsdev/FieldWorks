using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO.LangProj;

using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.Discourse
{
	/// <summary>
	/// Base class for several sets of tests for the Constituent chart, which share the need
	/// to create an in-memory FDO cache, a text, at least one paragraph, and make annotations
	/// in that paragraph.
	/// </summary>
	public class InMemoryDiscourseTestBase : InMemoryFdoTestBase
	{
		internal StText m_stText;
		internal StTxtPara m_firstPara;
		//internal ITsStrFactory m_tsf;
		internal DiscourseTestHelper m_helper;
		protected string m_preposedMrkr = DiscourseStrings.ksMovedTextBefore;
		protected string m_postposedMrkr = DiscourseStrings.ksMovedTextAfter;


		public InMemoryDiscourseTestBase()
		{
		}
		#region Test setup
		public override void Initialize()
		{
			base.Initialize();
			//CreateTestData(); already done in base class
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create minimal test data required for every test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			base.CreateTestData();
			m_inMemoryCache.InitializeWritingSystemEncodings();
			m_inMemoryCache.InitializeLexDb();
			m_helper = new DiscourseTestHelper(Cache);
			m_firstPara = m_helper.FirstPara;
			m_stText = m_firstPara.Owner as StText;
			//// We want a real action handler so we can test Undo/Redo.
			//m_originalHandler = m_inMemoryCache.Cache.ActionHandlerAccessor;
			//((NewFdoCache)(m_inMemoryCache.Cache)).ActionHandler = ActionHandlerClass.Create();
			//// Make the key annotationdefns.
			ICmPossibilityList defns = Cache.LangProject.AnnotationDefsOA;
			if (defns == null)
			{
				defns = new CmPossibilityList();
				Cache.LangProject.AnnotationDefsOA = defns;
			}
			MakeAnnDefn(defns, LangProject.kguidAnnWordformInContext);
			MakeAnnDefn(defns, LangProject.kguidConstituentChartRow);
			MakeAnnDefn(defns, LangProject.kguidConstituentChartAnnotation);
		}

		private void MakeAnnDefn(ICmPossibilityList defns, string guid)
		{
			CmAnnotationDefn defn = new CmAnnotationDefn();
			defns.PossibilitiesOS.Append(defn);
			Cache.VwCacheDaAccessor.CacheGuidProp(defn.Hvo, (int)CmObjectFields.kflidCmObject_Guid,
				new Guid(guid));
		}
		internal StTxtPara MakeParagraph()
		{
			return m_helper.MakeParagraph();
		}

		// Make some sort of wfics for the text of the specified paragraph. Assumes no double spaces!
		// Caches results and does not repeat on same para
		internal int[] MakeAnnotations(StTxtPara para)
		{
			return m_helper.MakeAnnotations(para);
		}

		protected ChartLocation MakeLocObj(ICmIndirectAnnotation row, int icol)
		{
			return new ChartLocation(icol, row);
		}

		public override void Exit()
		{
			//IActionHandler handler = m_inMemoryCache.Cache.ActionHandlerAccessor;
			//if (handler != null && Marshal.IsComObject(handler))
			//{
			//    Marshal.ReleaseComObject(handler);
			//}
			//((NewFdoCache)(m_inMemoryCache.Cache)).ActionHandler = m_originalHandler;
			m_helper.Dispose();
			base.Exit();
		}

		#endregion

		#region test data creation



		#endregion

	}
}

using System;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// Summary description for TestVirtualHandler.
	/// </summary>
	[TestFixture]
	public class VirtualHandlerTests : InDatabaseFdoTestBase
	{
		/// <summary>
		/// A simple virtual handler that returns the size of the paragraphs of an StText.
		/// </summary>
		private class StTextParaCountVh : BaseVirtualHandler
		{
			/// <summary>
			/// This property represents the length of the list of paragraphs of the specified
			/// StText.  We implement load by reading the length of the vector from the DA, and
			/// caching it as the value of the virtual property.
			/// </summary>
			/// <param name="hvo"></param>
			/// <param name="tag"></param>
			/// <param name="ws"></param>
			/// <param name="cda"></param>
			public override void Load(int hvo, int tag, int ws,
				SIL.FieldWorks.Common.COMInterfaces.IVwCacheDa cda)
			{
				ISilDataAccess sda = (ISilDataAccess)cda;
				cda.CacheIntProp(hvo, tag, sda.get_VecSize(hvo,
					(int)StText.StTextTags.kflidParagraphs));
			}
		}

		const int khvoTest = 12009876;

		/// <summary>
		/// Test installing a virtual handler, and accessing the data it produces.
		/// </summary>
		[Test]
		public void InstallVirtualHandler()
		{
			CheckDisposed();

			StTextParaCountVh vh = new StTextParaCountVh();
			ISilDataAccess sda = m_fdoCache.MainCacheAccessor;
			IVwCacheDa cda = sda as IVwCacheDa;
			vh.ClassName = "StText";
			vh.FieldName = "ParagraphCount";
			vh.Type = (int)CellarModuleDefns.kcptInteger;
			cda.InstallVirtual(vh);
			// Rather than try to find a real StText, simulate one.
			cda.CacheVecProp(khvoTest, (int)StText.StTextTags.kflidParagraphs,
				new int[] {10, 20, 30, 40}, 4);
			Assert.AreEqual(4, sda.get_IntProp(khvoTest, vh.Tag));
		}

		/// <summary>
		/// Test installing and using the virtual handler written for multilingual strings.
		/// </summary>
		[Test]
		public void MultiStringVirtualHandler()
		{
			CheckDisposed();

			MultiStringVirtualHandler msvh = new MultiStringVirtualHandler("LexDb", "NewString");
			m_fdoCache.InstallVirtualProperty(msvh);
			Assert.IsTrue(msvh.Writeable);
			Assert.AreEqual((int)CellarModuleDefns.kcptMultiString, msvh.Type);
			Assert.IsFalse(msvh.ComputeEveryTime);

			int hvoLexDb = 0;
			if (m_fdoCache.LangProject != null &&
				m_fdoCache.LangProject.LexDbOA != null)
			{
				hvoLexDb = m_fdoCache.LangProject.LexDbOA.Hvo;
			}
			if (hvoLexDb == 0)
			{
				// Rather than try to create a real LexDb, simulate one.
				hvoLexDb = khvoTest;
			}
			ISilDataAccess sda = m_fdoCache.MainCacheAccessor;
			IVwCacheDa cda = sda as IVwCacheDa;
			int wsVern = m_fdoCache.LangProject.DefaultVernacularWritingSystem;
			msvh.Load(hvoLexDb, msvh.Tag, wsVern, cda);
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			ITsString tss = tsf.MakeString("", wsVern);
			ITsString tss1 = sda.get_MultiStringAlt(hvoLexDb, msvh.Tag, wsVern);
			Assert.IsTrue(tss.Equals(tss1));
			Assert.IsTrue(msvh.Writeable);
			tss = tsf.MakeString("This is a test", wsVern);
			msvh.WriteObj(hvoLexDb, msvh.Tag, wsVern, tss, sda);
			cda.CacheStringAlt(hvoLexDb, msvh.Tag, wsVern, tss);
			tss1 = sda.get_MultiStringAlt(hvoLexDb, msvh.Tag, wsVern);
			Assert.IsTrue(tss.Equals(tss1));
		}
	}
}

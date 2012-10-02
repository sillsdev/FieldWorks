using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.CoreTests;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	public class TestObjectListPublisher : MemoryOnlyBackendProviderTestBase
	{
		private const int ObjectListFlid = 89999956;

		[Test]
		public void SetAndAccessDummyList()
		{
			ILexDb lexDb = Cache.LangProject.LexDbOA;
			ILexEntry entry1 = null;
			ICmResource res1 = null;
			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
			{
				var leFactory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
				entry1 = leFactory.Create();
				ILexEntry entry2 = leFactory.Create();
				res1 = Cache.ServiceLocator.GetInstance<ICmResourceFactory>().Create();
				lexDb.ResourcesOC.Add(res1);
			});

			int hvoRoot = 10578;
			ObjectListPublisher publisher = new ObjectListPublisher(Cache.MainCacheAccessor as ISilDataAccessManaged, ObjectListFlid);
			var values = new int[] {23, 56, 2048};
			Notifiee recorder = new Notifiee();
			publisher.AddNotification(recorder);
			publisher.CacheVecProp(hvoRoot, values);
			Assert.AreEqual(values.Length, publisher.get_VecSize(hvoRoot, ObjectListFlid), "override of vec size");
			Assert.AreEqual(Cache.LangProject.TextsOC.Count, publisher.get_VecSize(Cache.LangProject.Hvo, LangProjectTags.kflidTexts), "base vec size");

			Assert.AreEqual(23, publisher.get_VecItem(hvoRoot, ObjectListFlid, 0), "override of vec item");
			Assert.AreEqual(res1.Hvo, publisher.get_VecItem(lexDb.Hvo, LexDbTags.kflidResources, 0), "base vec item");
			Assert.AreEqual(56, publisher.get_VecItem(hvoRoot, ObjectListFlid, 1), "override of vec item, non-zero index");

			VerifyCurrentValue(hvoRoot, publisher, values, "original value");
			Assert.AreEqual(lexDb.ResourcesOC.Count(), publisher.VecProp(lexDb.Hvo, LexDbTags.kflidResources).Length, "base VecProp");

			recorder.CheckChanges(new ChangeInformationTest[] { new ChangeInformationTest(hvoRoot, ObjectListFlid, 0, values.Length, 0) },
				"expected PropChanged from caching HVOs");
			publisher.RemoveNotification(recorder);

			recorder = new Notifiee();
			publisher.AddNotification(recorder);
			publisher.Replace(hvoRoot, 1, new int[] {97, 98}, 0);
			VerifyCurrentValue(hvoRoot, publisher, new int[] {23, 97, 98, 56, 2048}, "after inserting 97, 98" );
			recorder.CheckChanges(new ChangeInformationTest[] { new ChangeInformationTest(hvoRoot, ObjectListFlid, 1, 2, 0) },
				"expected PropChanged from caching HVOs");
			publisher.RemoveNotification(recorder);

			recorder = new Notifiee();
			publisher.AddNotification(recorder);
			publisher.Replace(hvoRoot, 1, new int[0] , 2);
			VerifyCurrentValue(hvoRoot, publisher, new int[] { 23, 56, 2048 }, "after deleting 97, 98");
			recorder.CheckChanges(new ChangeInformationTest[] { new ChangeInformationTest(hvoRoot, ObjectListFlid, 1, 0, 2) },
				"expected PropChanged from caching HVOs");
			publisher.RemoveNotification(recorder);
		}

		private void VerifyCurrentValue(int hvoRoot, ObjectListPublisher publisher, int[] values, string label)
		{
			int[] newValues = publisher.VecProp(hvoRoot, ObjectListFlid);
			Assert.AreEqual(values.Length, newValues.Length, label + "length from VecProp");
			for (int i = 0; i < values.Length; i++)
				Assert.AreEqual(values[i], newValues[i], label + " item " + i +" from VecProp");
		}
	}
}

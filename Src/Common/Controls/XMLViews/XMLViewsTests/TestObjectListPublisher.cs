// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.FieldWorks.Common.Controls;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Infrastructure.Impl;

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
			publisher.CacheVecProp(hvoRoot, values, true);
			Assert.That(publisher.get_VecSize(hvoRoot, ObjectListFlid), Is.EqualTo(values.Length), "override of vec size");
			//Assert.That(publisher.get_VecSize(Cache.LangProject.Hvo, LangProjectTags.kflidTexts), Is.EqualTo(Cache.LangProject.Texts.Count), "base vec size");

			Assert.That(publisher.get_VecItem(hvoRoot, ObjectListFlid, 0), Is.EqualTo(23), "override of vec item");
			Assert.That(publisher.get_VecItem(lexDb.Hvo, LexDbTags.kflidResources, 0), Is.EqualTo(res1.Hvo), "base vec item");
			Assert.That(publisher.get_VecItem(hvoRoot, ObjectListFlid, 1), Is.EqualTo(56), "override of vec item, non-zero index");

			VerifyCurrentValue(hvoRoot, publisher, values, "original value");
			Assert.That(publisher.VecProp(lexDb.Hvo, LexDbTags.kflidResources).Length, Is.EqualTo(lexDb.ResourcesOC.Count()), "base VecProp");

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
			Assert.That(newValues.Length, Is.EqualTo(values.Length), label + "length from VecProp");
			for (int i = 0; i < values.Length; i++)
				Assert.That(newValues[i], Is.EqualTo(values[i]), label + " item " + i +" from VecProp");
		}
	}
}

// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using LanguageExplorer.Controls.XMLViews;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Infrastructure.Impl;

namespace LanguageExplorerTests.Controls.XMLViews
{
	[TestFixture]
	public class TestObjectListPublisher : MemoryOnlyBackendProviderTestBase
	{
		private const int ObjectListFlid = 89999956;

		[Test]
		public void SetAndAccessDummyList()
		{
			var lexDb = Cache.LangProject.LexDbOA;
			ILexEntry entry1 = null;
			ICmResource res1 = null;
			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
			{
				var leFactory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
				entry1 = leFactory.Create();
				var entry2 = leFactory.Create();
				res1 = Cache.ServiceLocator.GetInstance<ICmResourceFactory>().Create();
				lexDb.ResourcesOC.Add(res1);
			});

			var hvoRoot = 10578;
			var publisher = new ObjectListPublisher(Cache.MainCacheAccessor as ISilDataAccessManaged, ObjectListFlid);
			var values = new int[] { 23, 56, 2048 };
			var recorder = new Notifiee();
			publisher.AddNotification(recorder);
			publisher.CacheVecProp(hvoRoot, values);
			Assert.AreEqual(values.Length, publisher.get_VecSize(hvoRoot, ObjectListFlid), "override of vec size");

			Assert.AreEqual(23, publisher.get_VecItem(hvoRoot, ObjectListFlid, 0), "override of vec item");
			Assert.AreEqual(res1.Hvo, publisher.get_VecItem(lexDb.Hvo, LexDbTags.kflidResources, 0), "base vec item");
			Assert.AreEqual(56, publisher.get_VecItem(hvoRoot, ObjectListFlid, 1), "override of vec item, non-zero index");

			VerifyCurrentValue(hvoRoot, publisher, values, "original value");
			Assert.AreEqual(lexDb.ResourcesOC.Count(), publisher.VecProp(lexDb.Hvo, LexDbTags.kflidResources).Length, "base VecProp");

			recorder.CheckChanges(new[] { new ChangeInformationTest(hvoRoot, ObjectListFlid, 0, values.Length, 0) }, "expected PropChanged from caching HVOs");
			publisher.RemoveNotification(recorder);

			recorder = new Notifiee();
			publisher.AddNotification(recorder);
			publisher.Replace(hvoRoot, 1, new[] { 97, 98 }, 0);
			VerifyCurrentValue(hvoRoot, publisher, new int[] { 23, 97, 98, 56, 2048 }, "after inserting 97, 98");
			recorder.CheckChanges(new[] { new ChangeInformationTest(hvoRoot, ObjectListFlid, 1, 2, 0) }, "expected PropChanged from caching HVOs");
			publisher.RemoveNotification(recorder);

			recorder = new Notifiee();
			publisher.AddNotification(recorder);
			publisher.Replace(hvoRoot, 1, new int[0], 2);
			VerifyCurrentValue(hvoRoot, publisher, new[] { 23, 56, 2048 }, "after deleting 97, 98");
			recorder.CheckChanges(new[] { new ChangeInformationTest(hvoRoot, ObjectListFlid, 1, 0, 2) }, "expected PropChanged from caching HVOs");
			publisher.RemoveNotification(recorder);
		}

		private void VerifyCurrentValue(int hvoRoot, ObjectListPublisher publisher, int[] values, string label)
		{
			var newValues = publisher.VecProp(hvoRoot, ObjectListFlid);
			Assert.AreEqual(values.Length, newValues.Length, label + "length from VecProp");
			for (var i = 0; i < values.Length; i++)
			{
				Assert.AreEqual(values[i], newValues[i], label + " item " + i + " from VecProp");
			}
		}
	}
}
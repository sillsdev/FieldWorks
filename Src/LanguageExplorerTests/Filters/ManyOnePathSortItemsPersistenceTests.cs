// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using LanguageExplorer;
using LanguageExplorer.Filters;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorerTests.Filters
{
	/// <summary>
	/// Tests persisting a list of ManyOnePathSortItems
	/// </summary>
	[TestFixture]
	public class ManyOnePathSortItemsPersistenceTests : MemoryOnlyBackendProviderTestBase
	{
		private List<IManyOnePathSortItem> m_list;
		private ILexEntry m_le1;
		private ILexEntry m_le2;

		public override void TestSetup()
		{
			base.TestSetup();

			IManyOnePathSortItem mopsi = new ManyOnePathSortItem(Cache.LangProject);
			m_list = new List<IManyOnePathSortItem> { mopsi };
			var leFactory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			UndoableUnitOfWorkHelper.Do("undoit", "redoit", Cache.ActionHandlerAccessor, () =>
			{
				m_le1 = leFactory.Create();
				m_le2 = leFactory.Create();
			});
			mopsi = new ManyOnePathSortItem(Cache.LangProject.LexDbOA.Hvo, new int[] { m_le1.Hvo, m_le2.Hvo }, new int[] { 2, 3 });
			m_list.Add(mopsi);
		}

		/// <summary>
		/// Test persisting a list of ManyOnePathSortItems.
		/// </summary>
		[Test]
		public void PersistMopsiList()
		{
			var mopsi = (IManyOnePathSortItem)m_list[m_list.Count - 1];
			using (var stream = new MemoryStream())
			{
				var objRepo = Cache.ServiceLocator.ObjectRepository;
				var originalPersistData = mopsi.PersistData(objRepo);
				using (var writer = new StreamWriter(stream))
				{
					ManyOnePathSortItem.WriteItems(m_list, writer, objRepo);
					stream.Seek(0, SeekOrigin.Begin);
					using (var reader = new StreamReader(stream))
					{
						var items = ManyOnePathSortItem.ReadItems(reader, objRepo);
						Assert.That(items.Count, Is.EqualTo(m_list.Count));
						mopsi = (IManyOnePathSortItem)items[0];
						Assert.That(mopsi.KeyObject, Is.EqualTo(Cache.LangProject.Hvo));
						Assert.That(mopsi.PathLength, Is.EqualTo(0));
						// Root object is key object, if no path.
						Assert.That(mopsi.RootObjectHvo, Is.EqualTo(Cache.LangProject.Hvo));
						Assert.That(mopsi.RootObjectUsing(Cache), Is.EqualTo(Cache.LangProject));
						// PathObject(0) is also the key, if no path.
						Assert.That(mopsi.PathObject(0), Is.EqualTo(Cache.LangProject.Hvo));
						mopsi = (IManyOnePathSortItem)items[1];
						Assert.That(mopsi.KeyObject, Is.EqualTo(Cache.LangProject.LexDbOA.Hvo));
						Assert.That(mopsi.PathLength, Is.EqualTo(2));
						Assert.That(mopsi.PathFlid(0), Is.EqualTo(2));
						Assert.That(mopsi.PathFlid(1), Is.EqualTo(3));
						Assert.That(mopsi.PathObject(0), Is.EqualTo(m_le1.Hvo));
						Assert.That(mopsi.PathObject(1), Is.EqualTo(m_le2.Hvo));
						Assert.That(mopsi.PathObject(2), Is.EqualTo(Cache.LangProject.LexDbOA.Hvo), "Index one too large yields key object.");
						Assert.That(mopsi.RootObjectHvo, Is.EqualTo(m_le1.Hvo));
						Assert.That(mopsi.RootObjectUsing(Cache), Is.EqualTo(m_le1));
						Assert.That(mopsi.KeyObjectUsing(Cache), Is.EqualTo(Cache.LangProject.LexDbOA));
						Assert.That(mopsi.PersistData(objRepo), Is.EqualTo(originalPersistData));
					}
				}
			}
		}

		/// <summary>
		/// Test persisting a list of ManyOnePathSortItems.
		/// </summary>
		[Test]
		public void PersistMopsiList_BadGUID()
		{
			// Now make one containing a bad GUID.
			using (var stream = new MemoryStream())
			{
				var objRepo = Cache.ServiceLocator.ObjectRepository;
				using (var writer = new StreamWriter(stream))
				{
					ManyOnePathSortItem.WriteItems(m_list, writer, objRepo);
					writer.WriteLine(Convert.ToBase64String(Guid.NewGuid().ToByteArray()));
					// fake item, bad guid
					writer.Flush();
					stream.Seek(0, SeekOrigin.Begin);
					using (var reader = new StreamReader(stream))
					{
						var items = ManyOnePathSortItem.ReadItems(reader, objRepo);
						Assert.That(items, Is.Null);
					}
				}
			}
		}
	}
}
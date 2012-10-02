// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ObjectSetTests.cs
// Responsibility: JohnH, RandyR
// Last reviewed:
//
// <remarks>
// Implements CachingTests class.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// Implements ObjectSet tests. Tests are performed by NUnit.
	/// </summary>
	[TestFixture]
	public class ObjectSetTests : InDatabaseFdoTestBase
	{
		/// <summary>
		/// Tests enumerating a vector
		/// </summary>
		//TODO(JohnH): This is currently only a speed test
		[Test]
		[Category("SmokeTest")]
		public void VectorEnumerator()
		{
			CheckDisposed();

			FdoOwningCollection<IWfiWordform> oc = m_fdoCache.LangProject.WordformInventoryOA.WordformsOC;
			//do this two times to reduce the interference of which method went first.
			for (int i = 0; i< 2; i++)
			{
				//test using hvos and no pre-caching
				int[] hvos = oc.HvoArray;
				foreach(int hvo in hvos)
				{
					WfiWordform word;
					word = new WfiWordform (m_fdoCache, hvo);
				}
			}
		}

		/// <summary>
		/// Tests an arbitrary enumerator
		/// </summary>
		[Test]
		[Category("SmokeTest")]
		public void ArbitraryEnumerator()
		{
			CheckDisposed();

			//select all of the objects which of type CmPossibility or subclasses
			string sqlQuery = "select ID, Class$ from CmPossibility_ order by Class$";
			FdoObjectSet<ICmPossibility> v = new FdoObjectSet<ICmPossibility>(m_fdoCache, sqlQuery, false);	// No ord column

			//select all of the objects which are not of type CmPossibility, but rather subclasses
			sqlQuery = "select ID, class$ from CmPossibility_ where class$ != 7 order by Class$";
			v = new FdoObjectSet<ICmPossibility>(m_fdoCache, sqlQuery, false);	// no ord column

			//now test an ordered set of objects
			int hvo = m_fdoCache.LangProject.AnthroListOA.Hvo;
			//construct the query so that everything is ordered backwards, for testing.
			sqlQuery = string.Format("select ID, Class$, OwnOrd$ from CmPossibility_ " +
				"where owner$={0} order by Class$, ID desc", hvo);
			v = new FdoObjectSet<ICmPossibility>(m_fdoCache, sqlQuery, true);	// Has ord column, but not a real one.
		}

		/// <summary>
		/// smoke Tests fJustLoadAllOfType parameter of FdoObjectSet constructor
		/// </summary>
		[Test]
		[Category("LongRunning")]
		public void JustLoadAllOfType()
		{
			CheckDisposed();

			//select all of the objects which of type CmPossibility or subclasses
			string sqlQuery = "select ID, Class$ from CmPossibility_ order by Class$";
			FdoObjectSet<ICmPossibility> v1 = new FdoObjectSet<ICmPossibility>(m_fdoCache, sqlQuery, false, // No ord column
				false);// that where clause specify each individual object's hvo
			FdoObjectSet<ICmPossibility> v2 = new FdoObjectSet<ICmPossibility>(m_fdoCache, sqlQuery, false, // No ord column
				true);	//just load all of this type (leave out the where clause)
			Assert.IsTrue(v1.Count == v2.Count);
		}

		/// <summary>
		/// creates an object set which should not have any objects
		/// </summary>
		/// <returns>an empty FdoObjectSet</returns>
		protected FdoObjectSet<ICmObject> GetEmptySet()
		{
			string sqlQuery = "select ID from CmObject where ID=-1969";
			return new FdoObjectSet<ICmObject>(m_fdoCache, sqlQuery, false/* No ord column */ );
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		[ExpectedException(typeof(NotSupportedException))]
		public void ObjectSetThatIsEmpty()
		{
			CheckDisposed();

			FdoObjectSet<ICmObject> os = GetEmptySet();
			Assert.AreEqual(0, os.Count);
			IEnumerator<ICmObject> v = os.GetEnumerator();
			v.Reset();	// Should throw an exception for the generic enumerator.
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void ObjectSetEnumeratorOverrun()
		{
			CheckDisposed();

			IEnumerator<ICmObject> v = GetEmptySet().GetEnumerator();
			Assert.IsFalse(v.MoveNext());
			ICmObject orange = v.Current;
			Assert.IsNull(orange);
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void PreserveHvoCountAndOrder ()
		{
			CheckDisposed();

			int[] hvos = GetHvosForFirstNObjectsOfClass(m_fdoCache.LangProject.PeopleOA.PossibilitiesOS.HvoArray, CmPerson.kClassId, 5);

			//mess with the order
			int h = hvos[0];
			hvos[0] = hvos[3];
			hvos[3] = h;

			// duplicate that last one to check for preserving duplicates (which is necessary for reference sequence attrs)
			hvos[4] = h;

			FdoObjectSet<ICmObject> v = new FdoObjectSet<ICmObject>(m_fdoCache, hvos, true);
			AssertMatchingCountAndOrder(hvos, v);

			// test when we give the signature, too
			v = new FdoObjectSet<ICmObject>(m_fdoCache, hvos, true, typeof(CmPerson));
			AssertMatchingCountAndOrder(hvos, v);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="hvos"></param>
		/// <param name="v"></param>
		protected void AssertMatchingCountAndOrder(int[] hvos, FdoObjectSet<ICmObject> v)
		{
			Assert.AreEqual(hvos.Length, v.Count, "Count was not preserved.");

			int i = 0;
			foreach(ICmObject o in v)
			{
				Assert.AreEqual(hvos[i], o.Hvo, "Order was not preserved.");
				i++;
			}

		}

		/// <summary></summary>
		protected FdoObjectSet<ICmPossibility> GetNonEmptySet()
		{
			string sqlQuery = "select ID, Class$ from CmPossibility_ order by Class$";
			return new FdoObjectSet<ICmPossibility>(m_fdoCache, sqlQuery, false);
		}

		/// <summary>
		/// </summary>
		[Test]
		[Category("LongRunning")]
		public void GetFirstItemOfObjectSet()
		{
			CheckDisposed();

			//test an empty set
			Assert.IsNull(GetEmptySet().FirstItem);

			//test a non-empty set
			FdoObjectSet<ICmPossibility> objects = this.GetNonEmptySet();
			Assert.IsNotNull(objects.FirstItem);//Enhance: just a smoke test
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that creating an object loads basic values into cache
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateAndLoadOwnedObject()
		{
			CheckDisposed();

			StStyle style = new StStyle();
			Cache.LangProject.StylesOC.Add(style);

			Guid guid = Cache.GetGuidFromId(style.Hvo);
			Assert.IsTrue(guid != Guid.Empty);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that creating an object loads basic values into cache
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateAndLoadOwnerlessObject()
		{
			CheckDisposed();

			int hvo = Cache.CreateObject(StFootnote.kClassId);

			Guid guid = Cache.GetGuidFromId(hvo);
			Assert.IsTrue(guid != Guid.Empty);
		}
	}
}

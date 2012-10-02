// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2010' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DataStoreInitializationServicesTests.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Rhino.Mocks;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.FDOTests
{
	#region class ArrayBasedOwningSequence
	class ArrayBasedOwningSequence<T> : IFdoOwningSequence<T> where T : class, ICmObject
	{
		private static int s_fakeHvo = 50000;

		private readonly List<T> m_internalList = new List<T>();
		private readonly FdoCache m_cache;
		private readonly IScrTxtPara m_owner;

		public ArrayBasedOwningSequence(FdoCache cache, IScrTxtPara owner)
		{
			m_cache = cache;
			m_owner = owner;
		}

		private void InitializeAddedObject(T item)
		{
			if (item.Hvo == (int)SpecialHVOValues.kHvoUninitializedObject)
			{
				ReflectionHelper.SetField(item, "m_cache", m_cache);
				ReflectionHelper.SetField(item, "m_owner", m_owner);
				ReflectionHelper.SetField(item, "m_guid", m_cache.ServiceLocator.GetInstance<ICmObjectIdFactory>().NewId());
				ReflectionHelper.SetField(item, "m_hvo", s_fakeHvo++);
			}
		}

		#region IFdoOwningSequence<T> Members
		public void MoveTo(int iStart, int iEnd, IFdoOwningSequence<T> seqDest, int iDestStart)
		{
			throw new NotImplementedException();
		}
		#endregion

		#region IFdoList<T> Members
		public void Replace(int start, int numberToDelete, IEnumerable<ICmObject> thingsToAdd)
		{
			m_internalList.RemoveRange(start, numberToDelete);
			m_internalList.InsertRange(start, thingsToAdd.Cast<T>());
		}

		public T[] ToArray()
		{
			return m_internalList.ToArray();
		}
		#endregion

		#region IFdoVector Members
		public System.Collections.Generic.IEnumerable<ICmObject> Objects
		{
			get { throw new NotImplementedException(); }
		}

		public Guid[] ToGuidArray()
		{
			throw new NotImplementedException();
		}

		public int[] ToHvoArray()
		{
			throw new NotImplementedException();
		}
		#endregion

		#region IList<T> Members

		public int IndexOf(T item)
		{
			return m_internalList.IndexOf(item);
		}

		public void Insert(int index, T item)
		{
			m_internalList.Insert(index, item);
			InitializeAddedObject(item);
		}

		public void RemoveAt(int index)
		{
			m_internalList.RemoveAt(index);
		}

		public T this[int index]
		{
			get { return m_internalList[index]; }
			set { m_internalList[index] = value; }
		}

		#endregion

		#region ICollection<T> Members

		public void Add(T item)
		{
			m_internalList.Add(item);
			InitializeAddedObject(item);
		}

		public void Clear()
		{
			m_internalList.Clear();
		}

		public bool Contains(T item)
		{
			return m_internalList.Contains(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			m_internalList.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get { return m_internalList.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(T item)
		{
			return m_internalList.Remove(item);
		}

		#endregion

		#region IEnumerable<T> Members

		public IEnumerator<T> GetEnumerator()
		{
			return m_internalList.GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion
	}
	#endregion

	#region class ArrayBasedOwningCollection
	class ArrayBasedOwningCollection<T> : IFdoOwningCollection<T> where T : class, ICmObject
	{
		private static int s_fakeHvo = 50000;

		private readonly List<T> m_internalList = new List<T>();
		private readonly FdoCache m_cache;
		private readonly IScrTxtPara m_owner;

		public ArrayBasedOwningCollection(FdoCache cache, IScrTxtPara owner)
		{
			m_cache = cache;
			m_owner = owner;
		}

		private void InitializeAddedObject(T item)
		{
			if (item.Hvo == (int)SpecialHVOValues.kHvoUninitializedObject)
			{
				ReflectionHelper.SetField(item, "m_cache", m_cache);
				ReflectionHelper.SetField(item, "m_owner", m_owner);
				ReflectionHelper.SetField(item, "m_guid", m_cache.ServiceLocator.GetInstance<ICmObjectIdFactory>().NewId());
				ReflectionHelper.SetField(item, "m_hvo", s_fakeHvo++);
			}
		}

		#region IFdoVector Members
		public System.Collections.Generic.IEnumerable<ICmObject> Objects
		{
			get { throw new NotImplementedException(); }
		}

		public Guid[] ToGuidArray()
		{
			throw new NotImplementedException();
		}

		public int[] ToHvoArray()
		{
			throw new NotImplementedException();
		}
		#endregion

		#region ICollection<T> Members

		public void Add(T item)
		{
			m_internalList.Add(item);
			InitializeAddedObject(item);
		}

		public void Clear()
		{
			m_internalList.Clear();
		}

		public bool Contains(T item)
		{
			return m_internalList.Contains(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			m_internalList.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get { return m_internalList.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(T item)
		{
			return m_internalList.Remove(item);
		}

		#endregion

		#region IEnumerable<T> Members

		public IEnumerator<T> GetEnumerator()
		{
			return m_internalList.GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		#region IFdoSet<T> Members
		public void Replace(IEnumerable<ICmObject> thingsToRemove, IEnumerable<ICmObject> thingsToAdd)
		{
			throw new NotImplementedException();
		}

		public T[] ToArray()
		{
			throw new NotImplementedException();
		}

		#endregion
	}
	#endregion

	#region DataStoreInitializationServicesTests test fixture
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class DataStoreInitializationServicesTests : ScrInMemoryFdoTestBase
	{
		#region Member variables and delegates
		private IScrTxtPara m_para;

		private delegate ICmTranslation GetBtDelegate();
		#endregion

		#region Test setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();
			ReflectionHelper.SetProperty(Cache, "FullyInitializedAndReadyToRock", false);
			m_para = MockRepository.GenerateStub<IScrTxtPara>();
			IFdoOwningSequence<ISegment> mockedSegments = new ArrayBasedOwningSequence<ISegment>(Cache, m_para);
			m_para.Stub(p => p.SegmentsOS).Return(mockedSegments);
			m_para.Stub(p => p.Services).Return(Cache.ServiceLocator);
			IFdoOwningCollection<ICmTranslation> mockedTranslations = new ArrayBasedOwningCollection<ICmTranslation>(Cache, m_para);
			m_para.Stub(p => p.TranslationsOC).Return(mockedTranslations);
			m_para.Stub(p => p.Cache).Return(Cache);
			m_para.Stub(p => p.ClassID).Return(ScrTxtParaTags.kClassId);
			ICmObjectId paraId = Cache.ServiceLocator.GetInstance<ICmObjectIdFactory>().NewId();
			m_para.Stub(p => p.Id).Return(paraId);
			ITsStrFactory fact = TsStrFactoryClass.Create();
			m_para.Contents = fact.MakeString(string.Empty, Cache.DefaultVernWs);

			GetBtDelegate getBtDelegate = () =>
				m_para.TranslationsOC.FirstOrDefault(trans => trans.TypeRA != null &&
					trans.TypeRA.Guid == CmPossibilityTags.kguidTranBackTranslation);
			m_para.Stub(p => p.GetBT()).Do(getBtDelegate);
		}
		#endregion

		#region EnsureBtForScrParas tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the EnsureBtForScrParas method does nothing when Scripture is null.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnsureBtForScrParas_NoScripture()
		{
			Cache.LanguageProject.TranslatedScriptureOA = null;
			int undoActions = m_actionHandler.UndoableActionCount;
			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "EnsureBtForScrParas", Cache);
			Assert.AreEqual(undoActions, m_actionHandler.UndoableActionCount);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the EnsureBtForScrParas method does nothing if the fix was already
		/// run before.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnsureBtForScrParas_AlreadyRun()
		{
			ReflectionHelper.SetProperty(m_scr, "FixedParasWithoutBt", true);

			int undoActions = m_actionHandler.UndoableActionCount;
			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "EnsureBtForScrParas", Cache);
			Assert.AreEqual(undoActions, m_actionHandler.UndoableActionCount);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the EnsureBtForScrParas method does nothing when all data already
		/// contains CmTranslations for their BTs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnsureBtForScrParas_AllHaveBts()
		{
			m_scr.ResourcesOC.Clear();

			CreateExodusData(); // Should create all data with BTs

			int undoActions = m_actionHandler.UndoableActionCount;
			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "EnsureBtForScrParas", Cache);
			Assert.AreEqual(undoActions + 1, m_actionHandler.UndoableActionCount, "Only action should be to set the CmResource indicating that we have done the fix.");
			Assert.IsTrue(m_scr.FixedParasWithoutBt);
		}
		#endregion

		#region EnsureBtForPara tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the EnsureBtForPara method when a paragraph contains no CmTranslations
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnsureBtForPara_NoCmTranslations()
		{
			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "EnsureBtForPara", m_para);

			Assert.AreEqual(1, m_para.TranslationsOC.Count);
			Assert.IsNotNull(m_para.GetBT());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the EnsureBtForPara method when a paragraph contains CmTranslations that
		/// are not back translations
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnsureBtForPara_OtherCmTranslations()
		{
			ICmTranslation freeTrans = Cache.ServiceLocator.GetInstance<ICmTranslationFactory>().Create(
				m_para, Cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(CmPossibilityTags.kguidTranFreeTranslation));
			ICmTranslation literalTrans = Cache.ServiceLocator.GetInstance<ICmTranslationFactory>().Create(
				m_para, Cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(CmPossibilityTags.kguidTranLiteralTranslation));
			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "EnsureBtForPara", m_para);

			Assert.AreEqual(1, m_para.TranslationsOC.Count);
			Assert.IsNotNull(m_para.GetBT());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the EnsureBtForPara method when a paragraph already contains a
		/// CmTranslation for the back translation
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnsureBtForPara_ExistingBt()
		{
			ICmTranslation existingTrans = Cache.ServiceLocator.GetInstance<ICmTranslationFactory>().Create(
				m_para, Cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(CmPossibilityTags.kguidTranBackTranslation));
			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "EnsureBtForPara", m_para);

			Assert.AreEqual(1, m_para.TranslationsOC.Count);
			Assert.AreEqual(existingTrans, m_para.GetBT());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the EnsureBtForPara method when a paragraph contains multiple CmTranslations
		/// for the back translation (Both are empty).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnsureBtForPara_MultipleExistingBt_Empty()
		{
			ICmPossibility btType = Cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(CmPossibilityTags.kguidTranBackTranslation);
			ICmTranslation existingTrans = Cache.ServiceLocator.GetInstance<ICmTranslationFactory>().Create(m_para, btType);
			ICmTranslation existingTrans2 = MockRepository.GenerateStub<ICmTranslation>();
			existingTrans2.TypeRA = btType;
			IMultiString translation = MockRepository.GenerateStub<IMultiString>();
			translation.Stub(t => t.get_String(Cache.DefaultAnalWs)).Return(
				Cache.TsStrFactory.MakeString(string.Empty, Cache.DefaultAnalWs));
			translation.Stub(t => t.AvailableWritingSystemIds).Return(new int[] { Cache.DefaultAnalWs });
			existingTrans2.Stub(t => t.Translation).Return(translation);
			m_para.TranslationsOC.Add(existingTrans2);
			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "EnsureBtForPara", m_para);

			Assert.AreEqual(1, m_para.TranslationsOC.Count);
			Assert.AreEqual(existingTrans, m_para.GetBT());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the EnsureBtForPara method when a paragraph contains multiple CmTranslations
		/// for the back translation (Only one is empty).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnsureBtForPara_MultipleExistingBt_OneEmpty()
		{
			ICmPossibility btType = Cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(CmPossibilityTags.kguidTranBackTranslation);
			ICmTranslation existingTrans = Cache.ServiceLocator.GetInstance<ICmTranslationFactory>().Create(m_para, btType);
			ICmTranslation existingTrans2 = MockRepository.GenerateStub<ICmTranslation>();
			existingTrans2.TypeRA = btType;
			m_para.TranslationsOC.Add(existingTrans2);
			IMultiString translation = MockRepository.GenerateStub<IMultiString>();
			translation.Stub(t => t.get_String(Cache.DefaultAnalWs)).Return(
				Cache.TsStrFactory.MakeString("I want a monkey.", Cache.DefaultAnalWs));
			existingTrans2.Stub(t => t.Translation).Return(translation);
			translation.Stub(t => t.AvailableWritingSystemIds).Return(new int[] { Cache.DefaultAnalWs });
			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "EnsureBtForPara", m_para);

			Assert.AreEqual(1, m_para.TranslationsOC.Count);
			Assert.IsNotNull(m_para.GetBT());
			Assert.AreEqual("I want a monkey.", m_para.GetBT().Translation.get_String(Cache.DefaultAnalWs).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the EnsureBtForPara method when a paragraph contains multiple CmTranslations
		/// for the back translation where one has a longer translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnsureBtForPara_MultipleExistingBt_NotEmptyOneLonger()
		{
			ICmPossibility btType = Cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(CmPossibilityTags.kguidTranBackTranslation);
			ICmTranslation existingTrans = Cache.ServiceLocator.GetInstance<ICmTranslationFactory>().Create(m_para, btType);
			existingTrans.Translation.set_String(Cache.DefaultAnalWs, "I want a monkey.");
			ICmTranslation existingTrans2 = MockRepository.GenerateStub<ICmTranslation>();
			existingTrans2.TypeRA = btType;
			m_para.TranslationsOC.Add(existingTrans2);
			IMultiString translation = MockRepository.GenerateStub<IMultiString>();
			translation.Stub(t => t.get_String(Cache.DefaultAnalWs)).Return(
				Cache.TsStrFactory.MakeString("I want another monkey.", Cache.DefaultAnalWs));
			existingTrans2.Stub(t => t.Translation).Return(translation);
			translation.Stub(t => t.AvailableWritingSystemIds).Return(new int[] { Cache.DefaultAnalWs });
			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "EnsureBtForPara", m_para);

			Assert.AreEqual(1, m_para.TranslationsOC.Count);
			Assert.IsNotNull(m_para.GetBT());
			Assert.AreEqual("I want another monkey.", m_para.GetBT().Translation.get_String(Cache.DefaultAnalWs).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the EnsureBtForPara method when a paragraph contains multiple CmTranslations
		/// for the back translation where both have a translation, but in different writing
		/// systems.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnsureBtForPara_MultipleExistingBt_NotEmptyDiffWs()
		{
			ICmPossibility btType = Cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(CmPossibilityTags.kguidTranBackTranslation);
			ICmTranslation existingTrans = Cache.ServiceLocator.GetInstance<ICmTranslationFactory>().Create(m_para, btType);
			existingTrans.Translation.set_String(Cache.DefaultAnalWs, "I want a monkey.");
			ICmTranslation existingTrans2 = MockRepository.GenerateStub<ICmTranslation>();
			existingTrans2.TypeRA = btType;
			m_para.TranslationsOC.Add(existingTrans2);
			IMultiString translation = MockRepository.GenerateStub<IMultiString>();
			translation.Stub(t => t.get_String(m_wsDe)).Return(
				Cache.TsStrFactory.MakeString("I want another monkey.", m_wsDe));
			translation.Stub(t => t.AvailableWritingSystemIds).Return(new int[]{m_wsDe});
			existingTrans2.Stub(t => t.Translation).Return(translation);
			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "EnsureBtForPara", m_para);

			Assert.AreEqual(1, m_para.TranslationsOC.Count);
			Assert.IsNotNull(m_para.GetBT());
			Assert.AreEqual("I want a monkey.", m_para.GetBT().Translation.get_String(Cache.DefaultAnalWs).Text);
			Assert.AreEqual("I want another monkey.", m_para.GetBT().Translation.get_String(m_wsDe).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the EnsureBtForPara method when a paragraph contains multiple CmTranslations
		/// for the back translation where both have two translations in the default and German
		/// writing systems. One CmTranslation will have a longer translation for the default
		/// writing system. The other CmTranslation will have a longer translation for the
		/// German writing system. The final CmTranslation should select the longest of each
		/// translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnsureBtForPara_MultipleExistingBt_BothTransWithTwoWs()
		{
			ICmPossibility btType = Cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(CmPossibilityTags.kguidTranBackTranslation);
			ICmTranslation existingTrans = Cache.ServiceLocator.GetInstance<ICmTranslationFactory>().Create(m_para, btType);
			existingTrans.Translation.set_String(Cache.DefaultAnalWs, "I want a monkey.");
			existingTrans.Translation.set_String(m_wsDe, "short de.");
			ICmTranslation existingTrans2 = MockRepository.GenerateStub<ICmTranslation>();
			existingTrans2.TypeRA = btType;
			m_para.TranslationsOC.Add(existingTrans2);
			IMultiString translation = MockRepository.GenerateStub<IMultiString>();
			translation.Stub(t => t.get_String(m_wsDe)).Return(
				Cache.TsStrFactory.MakeString("I want another monkey.", m_wsDe));
			translation.Stub(t => t.get_String(Cache.DefaultAnalWs)).Return(
				Cache.TsStrFactory.MakeString("short def.", Cache.DefaultAnalWs));
			translation.Stub(t => t.AvailableWritingSystemIds).Return(new int[] { Cache.DefaultAnalWs, m_wsDe });
			existingTrans2.Stub(t => t.Translation).Return(translation);
			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "EnsureBtForPara", m_para);

			Assert.AreEqual(1, m_para.TranslationsOC.Count);
			Assert.IsNotNull(m_para.GetBT());
			Assert.AreEqual("I want a monkey.", m_para.GetBT().Translation.get_String(Cache.DefaultAnalWs).Text);
			Assert.AreEqual("I want another monkey.", m_para.GetBT().Translation.get_String(m_wsDe).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the EnsureBtForPara method when a paragraph already contains a
		/// CmTranslation for the back translation but the CmTranslation holding the back
		/// translation does not have its type set. (FWR-164)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnsureBtForPara_ExistingTranslationWithoutType()
		{
			ICmTranslation existingTrans = (ICmTranslation)ReflectionHelper.CreateObject("FDO.dll",
				"SIL.FieldWorks.FDO.DomainImpl.CmTranslation", BindingFlags.NonPublic, null);
			m_para.TranslationsOC.Add(existingTrans);
			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "EnsureBtForPara", m_para);

			Assert.AreEqual(1, m_para.TranslationsOC.Count);
			Assert.AreEqual(existingTrans, m_para.GetBT());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the EnsureBtForPara method when a paragraph already contains multiple
		/// CmTranslations but the CmTranslations don't not have their type set. (FWR-164)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnsureBtForPara_MultipleExistingTranslationWithoutType()
		{
			ICmTranslation existingTrans1 = (ICmTranslation)ReflectionHelper.CreateObject("FDO.dll",
				"SIL.FieldWorks.FDO.DomainImpl.CmTranslation", BindingFlags.NonPublic, null);
			m_para.TranslationsOC.Add(existingTrans1);
			ICmTranslation existingTrans2 = (ICmTranslation)ReflectionHelper.CreateObject("FDO.dll",
				"SIL.FieldWorks.FDO.DomainImpl.CmTranslation", BindingFlags.NonPublic, null);
			m_para.TranslationsOC.Add(existingTrans2);
			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "EnsureBtForPara", m_para);

			Assert.AreEqual(1, m_para.TranslationsOC.Count);
			Assert.AreEqual(existingTrans1, m_para.GetBT());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the EnsureBtForPara method when a paragraph already contains a
		/// CmTranslation for the back translation but also contains another CmTranslation
		/// that does not have its type set. (FWR-164)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnsureBtForPara_ExistingBtPlusTranslationWithoutType()
		{
			ICmTranslation existingBtTrans = Cache.ServiceLocator.GetInstance<ICmTranslationFactory>().Create(
				m_para, Cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(CmPossibilityTags.kguidTranBackTranslation));
			ICmTranslation existingTrans = (ICmTranslation)ReflectionHelper.CreateObject("FDO.dll",
				"SIL.FieldWorks.FDO.DomainImpl.CmTranslation", BindingFlags.NonPublic, null);
			m_para.TranslationsOC.Add(existingTrans);
			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "EnsureBtForPara", m_para);

			Assert.AreEqual(1, m_para.TranslationsOC.Count);
			Assert.AreEqual(existingBtTrans, m_para.GetBT());
		}
		#endregion

		#region EnsureSegmentsForScrParas tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the EnsureSegmentsForScrParas method does nothing when Scripture is null.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnsureSegmentsForScrParas_NoScripture()
		{
			Cache.LanguageProject.TranslatedScriptureOA = null;
			int undoActions = m_actionHandler.UndoableActionCount;
			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "EnsureSegmentsForScrParas", Cache);
			Assert.AreEqual(undoActions, m_actionHandler.UndoableActionCount);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the EnsureSegmentsForScrParas method does nothing if the fix was already
		/// run before.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnsureSegmentsForScrParas_AlreadyRun()
		{
			ReflectionHelper.SetProperty(m_scr, "FixedParasWithoutSegments", true);

			int undoActions = m_actionHandler.UndoableActionCount;
			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "EnsureSegmentsForScrParas", Cache);
			Assert.AreEqual(undoActions, m_actionHandler.UndoableActionCount);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the EnsureSegmentsForScrParas method does nothing when all data already
		/// contains segments for their BTs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnsureSegmentsForScrParas_AllHaveSegments()
		{
			m_scr.ResourcesOC.Clear();

			CreateExodusData(); // Should create all data with BTs

			int undoActions = m_actionHandler.UndoableActionCount;
			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "EnsureSegmentsForScrParas", Cache);
			Assert.AreEqual(undoActions + 1, m_actionHandler.UndoableActionCount, "Only action should be to set the CmResource indicating that we have done the fix.");
			Assert.IsTrue(m_scr.FixedParasWithoutSegments);
		}
		#endregion

		#region EnsureSegmentsForPara tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the EnsureSegmentsForPara method when a paragraph has no contents.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnsureSegmentsForPara_NoContents()
		{
			Assert.IsNull(m_para.Contents.Text);
			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "EnsureSegmentsForPara", m_para);

			Assert.AreEqual(0, m_para.SegmentsOS.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the EnsureSegmentsForPara method when a paragraph has contents but no
		/// segments or translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnsureSegmentsForPara_Contents()
		{
			AddRunToMockedPara(m_para, "A simple run of text. And another segment.", m_wsEn);
			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "EnsureSegmentsForPara", m_para);

			Assert.AreEqual(2, m_para.SegmentsOS.Count);
			VerifySegment(0, 0, 22, null);
			VerifySegment(1, 22, 42, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the EnsureSegmentsForPara method when a paragraph has contents and a CmTranslation
		/// but no segments.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnsureSegmentsForPara_ContentsWithBt()
		{
			AddRunToMockedPara(m_para, "A simple run of text. And another segment.", m_wsEn);
			ICmTranslation existingTrans = Cache.ServiceLocator.GetInstance<ICmTranslationFactory>().Create(
				m_para, Cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(CmPossibilityTags.kguidTranBackTranslation));
			existingTrans.Translation.set_String(Cache.DefaultAnalWs, "simple back translation. And a segment.");
			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "EnsureSegmentsForPara", m_para);

			Assert.AreEqual(2, m_para.SegmentsOS.Count);
			VerifySegment(0, 0, 22, "simple back translation. ");
			VerifySegment(1, 22, 42, "And a segment.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the EnsureSegmentsForPara method when a paragraph has contents and segments.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnsureSegmentsForPara_ContentsWithExistingSegments()
		{
			AddRunToMockedPara(m_para, "A simple run of text. And another segment.", m_wsEn);
			AddSegmentToPara(m_para, 0, 22, "Whatever we want.", 0);
			AddSegmentToPara(m_para, 22, 42, "The second segment.", 0);

			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "EnsureSegmentsForPara", m_para);

			Assert.AreEqual(2, m_para.SegmentsOS.Count);
			ITsString tssSeg1 = Cache.TsStrFactory.MakeString("Whatever we want.", Cache.DefaultAnalWs);
			VerifySegment(0, tssSeg1, "Whateverx wex want.", new int[0]);
			ITsString tssSeg2 = Cache.TsStrFactory.MakeString("The second segment.", Cache.DefaultAnalWs);
			VerifySegment(1, tssSeg2, "Thex secondx segment.", new int[0]);
		}
		#endregion

		#region EnsureStylesInUseSetForScrParas tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the EnsureStylesInUseSetForScrParas method does nothing when Scripture is
		/// null.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnsureStylesInUseSetForScrParas_NoScripture()
		{
			Cache.LanguageProject.TranslatedScriptureOA = null;
			int undoActions = m_actionHandler.UndoableActionCount;
			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices),
				"EnsureStylesInUseSetForScrParas", Cache);
			Assert.AreEqual(undoActions, m_actionHandler.UndoableActionCount);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the EnsureStylesInUseSetForScrParas method does nothing if the fix was
		/// already run before.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnsureStylesInUseSetForScrParas_AlreadyRun()
		{
			ReflectionHelper.SetProperty(m_scr, "FixedStylesInUse", true);

			int undoActions = m_actionHandler.UndoableActionCount;
			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "EnsureStylesInUseSetForScrParas", Cache);
			Assert.AreEqual(undoActions, m_actionHandler.UndoableActionCount);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the EnsureStylesInUseSetForScrParas method does nothing when all styles
		/// have already had their InUse flag set.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnsureStylesInUseSetForScrParas_AlreadySet()
		{
			m_scr.ResourcesOC.Clear();

			CreateExodusData();

			int undoActions = m_actionHandler.UndoableActionCount;
			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices),
				"EnsureStylesInUseSetForScrParas", Cache);
			Assert.AreEqual(undoActions + 1, m_actionHandler.UndoableActionCount, "Only action should be to set the CmResource indicating that we have done the fix.");
			Assert.IsTrue(m_scr.FixedStylesInUse);
		}
		#endregion

		#region EnsureStylesInUseSetForPara tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the EnsureStylesInUseSetForPara method when the specified paragraph is empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnsureStylesInUseSetForPara_EmptyPara()
		{
			m_para.StyleName = ScrStyleNames.Line3;
			IStStyle style = m_scr.FindStyle(ScrStyleNames.Line3);
			Assert.IsFalse(style.InUse);

			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices),
				"EnsureStylesInUseSetForPara", m_para, m_scr);

			Assert.IsTrue(style.InUse);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the EnsureStylesInUseSetForPara method when the specified paragraph is empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnsureStylesInUseSetForPara_CharStyles()
		{
			m_para.StyleName = ScrStyleNames.Line3;
			IStStyle line3Style = m_scr.FindStyle(ScrStyleNames.Line3);
			Assert.IsFalse(line3Style.InUse);
			IStStyle doxologyStyle = m_scr.FindStyle(ScrStyleNames.Doxology);
			Assert.IsFalse(doxologyStyle.InUse);
			IStStyle altReadingStyle = m_scr.FindStyle(ScrStyleNames.AlternateReading);
			Assert.IsFalse(altReadingStyle.InUse);

			AddRunToMockedPara(m_para, "This", ScrStyleNames.Doxology);
			AddRunToMockedPara(m_para, " is a ", null);
			AddRunToMockedPara(m_para, "monkey.", ScrStyleNames.AlternateReading);

			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices),
				"EnsureStylesInUseSetForPara", m_para, m_scr);

			Assert.IsTrue(line3Style.InUse);
			Assert.IsTrue(doxologyStyle.InUse);
			Assert.IsTrue(altReadingStyle.InUse);
		}
		#endregion

		#region ParaSegmentsValid tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ParaSegmentsValid method with a paragraph whose segments are perfectly
		/// fine.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParaSegmentsValid_Valid()
		{
			ICmTranslation backTrans = Cache.ServiceLocator.GetInstance<ICmTranslationFactory>().Create(
				m_para, Cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(CmPossibilityTags.kguidTranBackTranslation));
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Short sentence. Another sentence. Hahahahaha",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0);
			AddSegmentToPara(m_para, 16);
			AddSegmentToPara(m_para, 34);

			bool result = (bool)ReflectionHelper.GetResult(typeof(DataStoreInitializationServices),
				"ParaSegmentsValid", m_para, true, true);
			Assert.IsTrue(result);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ParaSegmentsValid method with a paragraph whose segments are perfectly
		/// fine, but contain segments for ORCs (6.0 style) without verse numbers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParaSegmentsValid_ValidOrcSegmentsNoVerseNumbers()
		{
			ICmTranslation backTrans = Cache.ServiceLocator.GetInstance<ICmTranslationFactory>().Create(
				m_para, Cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(CmPossibilityTags.kguidTranBackTranslation));
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Footnotes go here:              |       |
			bldr.Replace(0, 0, "Short sentence. Another sentence. Hahahahaha",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			StringUtils.InsertOrcIntoPara(Guid.NewGuid(), FwObjDataTypes.kodtOwnNameGuidHot, bldr, 15, 15, Cache.DefaultVernWs);
			StringUtils.InsertOrcIntoPara(Guid.NewGuid(), FwObjDataTypes.kodtOwnNameGuidHot, bldr, 24, 24, Cache.DefaultVernWs);

			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0);
			AddSegmentToPara(m_para, 15);
			AddSegmentToPara(m_para, 17);
			AddSegmentToPara(m_para, 24);
			AddSegmentToPara(m_para, 26);
			AddSegmentToPara(m_para, 36);

			bool result = (bool)ReflectionHelper.GetResult(typeof(DataStoreInitializationServices),
				"ParaSegmentsValid", m_para, true, true);
			Assert.IsTrue(result);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ParaSegmentsValid method with a paragraph whose segments are perfectly
		/// fine, but contains segments for ORCs (6.0 style) also containing verse numbers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParaSegmentsValid_ValidOrcSegmentsWithVerseNumbers()
		{
			ICmTranslation backTrans = Cache.ServiceLocator.GetInstance<ICmTranslationFactory>().Create(
				m_para, Cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(CmPossibilityTags.kguidTranBackTranslation));
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Footnotes go here:                 |                 |
			bldr.Replace(0, 0, "1Short sentence. 2Another sentence. 3Hahahahaha",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			bldr.SetStrPropValue(0, 1, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			bldr.SetStrPropValue(17, 18, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			bldr.SetStrPropValue(36, 37, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			StringUtils.InsertOrcIntoPara(Guid.NewGuid(), FwObjDataTypes.kodtOwnNameGuidHot, bldr, 18, 18, Cache.DefaultVernWs);
			StringUtils.InsertOrcIntoPara(Guid.NewGuid(), FwObjDataTypes.kodtOwnNameGuidHot, bldr, 37, 37, Cache.DefaultVernWs);

			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0);
			AddSegmentToPara(m_para, 1);
			AddSegmentToPara(m_para, 17);
			AddSegmentToPara(m_para, 19);
			AddSegmentToPara(m_para, 37);
			AddSegmentToPara(m_para, 39);

			bool result = (bool)ReflectionHelper.GetResult(typeof(DataStoreInitializationServices),
				"ParaSegmentsValid", m_para, true, true);
			Assert.IsTrue(result);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ParaSegmentsValid method with a paragraph that has too many segments.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParaSegmentsValid_TooMany()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Short sentence. Another sentence. Hahahahaha",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0);
			AddSegmentToPara(m_para, 16);
			AddSegmentToPara(m_para, 34);
			AddSegmentToPara(m_para, 37);

			bool result = (bool)ReflectionHelper.GetResult(typeof(DataStoreInitializationServices),
				"ParaSegmentsValid", m_para, true, true);
			Assert.IsFalse(result);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ParaSegmentsValid method with a paragraph that has too many segments
		/// with the last one having a BeginOffset that is the length of the paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParaSegmentsValid_TooMany_LastIsEmpty()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Short sentence. Another sentence. Hahahahaha",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0);
			AddSegmentToPara(m_para, 16);
			AddSegmentToPara(m_para, 34);
			AddSegmentToPara(m_para, 44);

			bool result = (bool)ReflectionHelper.GetResult(typeof(DataStoreInitializationServices),
				"ParaSegmentsValid", m_para, true, true);
			Assert.IsFalse(result);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ParaSegmentsValid method with a paragraph that has too few segments.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParaSegmentsValid_TooFew()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Short sentence. Another sentence. Hahahahaha",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0);
			AddSegmentToPara(m_para, 16);

			bool result = (bool)ReflectionHelper.GetResult(typeof(DataStoreInitializationServices),
				"ParaSegmentsValid", m_para, true, true);
			Assert.IsFalse(result);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ParaSegmentsValid method with a paragraph that has the right number
		/// of segments, but the offsets are wrong.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParaSegmentsValid_OffsetsWrong()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Short sentence. Another sentence. Hahahahaha",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0);
			AddSegmentToPara(m_para, 12);
			AddSegmentToPara(m_para, 30);

			bool result = (bool)ReflectionHelper.GetResult(typeof(DataStoreInitializationServices),
				"ParaSegmentsValid", m_para, true, true);
			Assert.IsFalse(result);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ParaSegmentsValid method with a paragraph that has a segment whose
		/// begin and end offsets are the same (empty baseline).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParaSegmentsValid_EmptyBaseline()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Short sentence. Another sentence. Hahahahaha",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0);
			AddSegmentToPara(m_para, 16);
			AddSegmentToPara(m_para, 16);

			bool result = (bool)ReflectionHelper.GetResult(typeof(DataStoreInitializationServices),
				"ParaSegmentsValid", m_para, true, true);
			Assert.IsFalse(result);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ParaSegmentsValid method with a paragraph that has a segment whose
		/// begin offset overlaps with another segment.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParaSegmentsValid_Overlapping()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Short sentence. Another sentence. Hahahahaha",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0);
			AddSegmentToPara(m_para, 16);
			AddSegmentToPara(m_para, 15);

			bool result = (bool)ReflectionHelper.GetResult(typeof(DataStoreInitializationServices),
				"ParaSegmentsValid", m_para, true, true);
			Assert.IsFalse(result);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ParaSegmentsValid method with a paragraph that has a segment (non-
		/// adjacent) that whose begin offset is before another segment.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParaSegmentsValid_OverlappingNonAdjacent()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Short sentence. Another sentence. Hahahahaha",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 5);
			AddSegmentToPara(m_para, 16);
			AddSegmentToPara(m_para, 2);

			bool result = (bool)ReflectionHelper.GetResult(typeof(DataStoreInitializationServices),
				"ParaSegmentsValid", m_para, true, true);
			Assert.IsFalse(result);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ParaSegmentsValid method with a paragraph with a non-empty CmTranslation,
		/// but whose segments do not have tranlsations. This is considered invalid because we
		/// need to rebuild the segments using the CmTranslation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParaSegmentsValid_NonEmptyCmTranslation_EmptySegmentTranslations()
		{
			ICmTranslation backTrans = Cache.ServiceLocator.GetInstance<ICmTranslationFactory>().Create(
				m_para, Cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(CmPossibilityTags.kguidTranBackTranslation));
			backTrans.Translation.set_String(Cache.DefaultAnalWs, "Translation 1. Translation 2.");
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Short sentence. Another sentence. Hahahahaha",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0);
			AddSegmentToPara(m_para, 16);
			AddSegmentToPara(m_para, 34);

			bool result = (bool)ReflectionHelper.GetResult(typeof(DataStoreInitializationServices),
				"ParaSegmentsValid", m_para, true, true);
			Assert.IsFalse(result);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ParaSegmentsValid method with a paragraph with a non-empty CmTranslation,
		/// but whose only segment is a label. This is considered valid -- there is no place in
		/// the segment to store this extraneous CmTranslation, so it is ignored.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParaSegmentsValid_NonEmptyCmTranslation_OnlyLabelSegment()
		{
			ICmTranslation backTrans = Cache.ServiceLocator.GetInstance<ICmTranslationFactory>().Create(
				m_para, Cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(CmPossibilityTags.kguidTranBackTranslation));
			backTrans.Translation.set_String(Cache.DefaultAnalWs, "Translation 1. Translation 2.");
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "23", StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber, Cache.DefaultVernWs));
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0);

			bool result = (bool)ReflectionHelper.GetResult(typeof(DataStoreInitializationServices),
				"ParaSegmentsValid", m_para, true, true);
			Assert.IsTrue(result);
		}
		#endregion

		#region BlowAwaySegments tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the BlowAwaySegments method with a paragraph that has no translation text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BlowAwaySegments_EmptyTranslation()
		{
			ICmTranslation trans = Cache.ServiceLocator.GetInstance<ICmTranslationFactory>().Create(
				m_para, Cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(CmPossibilityTags.kguidTranBackTranslation));
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Short sentence. Another sentence. Hahahahaha",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0);
			AddSegmentToPara(m_para, 12);
			AddSegmentToPara(m_para, 30);

			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "BlowAwaySegments", m_para);
			VerifyParaSegmentBreaks();
			VerifySegment(0, 0, 16, null);
			VerifySegment(1, 16, 34, null);
			VerifySegment(2, 34, bldr.Length, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the BlowAwaySegments method with a paragraph that has translation for every
		/// segment.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BlowAwaySegments_FullTranslation()
		{
			ICmTranslation trans = Cache.ServiceLocator.GetInstance<ICmTranslationFactory>().Create(
				m_para, Cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(CmPossibilityTags.kguidTranBackTranslation));
			AddRunToMockedTrans(trans, Cache.DefaultAnalWs, "Monkey. I want a monkey. Waaaaaaa", null);
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Short sentence. Another sentence. Hahahahaha",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0);
			AddSegmentToPara(m_para, 12);
			AddSegmentToPara(m_para, 30);

			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "BlowAwaySegments", m_para);
			VerifyParaSegmentBreaks();
			VerifySegment(0, 0, 16, "Monkey. ");
			VerifySegment(1, 16, 34, "I want a monkey. ");
			VerifySegment(2, 34, bldr.Length, "Waaaaaaa");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the BlowAwaySegments method with a paragraph that has translation for every
		/// segment, but there are too many segments.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BlowAwaySegments_FullTranslation_TooManySegments()
		{
			ICmTranslation trans = Cache.ServiceLocator.GetInstance<ICmTranslationFactory>().Create(
				m_para, Cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(CmPossibilityTags.kguidTranBackTranslation));
			AddRunToMockedTrans(trans, Cache.DefaultAnalWs, "Monkey. I want a monkey. Waaaaaaa", null);
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Short sentence. Another sentence. Hahahahaha",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0);
			AddSegmentToPara(m_para, 12);
			AddSegmentToPara(m_para, 30);
			AddSegmentToPara(m_para, 44);

			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "BlowAwaySegments", m_para);
			VerifyParaSegmentBreaks();
			VerifySegment(0, 0, 16, "Monkey. ");
			VerifySegment(1, 16, 34, "I want a monkey. ");
			VerifySegment(2, 34, bldr.Length, "Waaaaaaa");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the BlowAwaySegments method with a paragraph that has translation for every
		/// segment, but there are too many segments.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BlowAwaySegments_FullTranslation_TooManySegments_NoCmTranslation()
		{
			ICmTranslation trans = Cache.ServiceLocator.GetInstance<ICmTranslationFactory>().Create(
				m_para, Cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(CmPossibilityTags.kguidTranBackTranslation));
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Short sentence. Another sentence. Hahahahaha",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0);
			AddSegmentToPara(m_para, 12);
			AddSegmentToPara(m_para, 30);
			AddSegmentToPara(m_para, 44);

			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "BlowAwaySegments", m_para);
			VerifyParaSegmentBreaks();
			VerifySegment(0, 0, 16, null);
			VerifySegment(1, 16, 34, null);
			VerifySegment(2, 34, bldr.Length, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the BlowAwaySegments method with a paragraph that has translation for every
		/// segment and analyses for every segment.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BlowAwaySegments_FullAnalysis()
		{
			ICmTranslation trans = Cache.ServiceLocator.GetInstance<ICmTranslationFactory>().Create(
				m_para, Cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(CmPossibilityTags.kguidTranBackTranslation));
			AddRunToMockedTrans(trans, Cache.DefaultAnalWs, "Monkey. I want a monkey. Waaaaaaa", null);
			ITsStrBldr bldr = TsStrBldrClass.Create();
			//                              |12               |30  |35
			bldr.Replace(0, 0, "Short sentence. Another sentence. Hahahahaha",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0, 12, "Monkey. ", 0);
			AddSegmentToPara(m_para, 12, 30, "I want a monkey. ", 0);
			AddSegmentToPara(m_para, 30, 35, "Waaaaaaa", 0);

			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "BlowAwaySegments", m_para);
			VerifyParaSegmentBreaks();
			VerifySegment(0, Cache.TsStrFactory.MakeString("Monkey. ", Cache.DefaultAnalWs),
				"Monkey.x ", new int[0], new int[0], new [] { 1 });
			VerifySegment(1, Cache.TsStrFactory.MakeString("I want a monkey. ", Cache.DefaultAnalWs),
				"Ix wantx ax monkey.x ", new int[0], new int[0], new [] { 1 });
			VerifySegment(2, Cache.TsStrFactory.MakeString("Waaaaaaa", Cache.DefaultAnalWs),
				"Waaaaaaa", new int[0], new int[0], new [] { 0 });
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the BlowAwaySegments method with a paragraph that has translation for every
		/// segment and analyses for every segment when the paragraph has a current parse.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BlowAwaySegments_FullAnalysis_CurrentParse()
		{
			ICmTranslation trans = Cache.ServiceLocator.GetInstance<ICmTranslationFactory>().Create(
				m_para, Cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(CmPossibilityTags.kguidTranBackTranslation));
			AddRunToMockedTrans(trans, Cache.DefaultAnalWs, "Monkey. I want a monkey. Waaaaaaa", null);
			ITsStrBldr bldr = TsStrBldrClass.Create();
			//                              |12               |30  |35
			bldr.Replace(0, 0, "Short sentence. Another sentence. Hahahahaha",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			m_para.Contents = bldr.GetString();
			m_para.ParseIsCurrent = true;

			AddSegmentToPara(m_para, 0, 12, "Monkey. ", 0);
			AddSegmentToPara(m_para, 12, 30, "I want a monkey. ", 0);
			AddSegmentToPara(m_para, 30, 35, "Waaaaaaa", 0);

			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "BlowAwaySegments", m_para);
			VerifyParaSegmentBreaks();
			VerifySegment(0, Cache.TsStrFactory.MakeString("Monkey. ", Cache.DefaultAnalWs),
				"Monkey.x ", new int[0], new int[0], new [] { 1 });
			VerifySegment(1, Cache.TsStrFactory.MakeString("I want a monkey. ", Cache.DefaultAnalWs),
				"Ix wantx ax monkey.x ", new int[0], new int[0], new[] { 1 });
			VerifySegment(2, Cache.TsStrFactory.MakeString("Waaaaaaa", Cache.DefaultAnalWs),
				"Waaaaaaa", new int[0], new int[0], new [] { 0 });
		}
		#endregion

		#region FixSegmentsForScriptureParas tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the FixSegmentsForScriptureParas method does nothing when Scripture is null.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForScriptureParas_NoScripture()
		{
			Cache.LanguageProject.TranslatedScriptureOA = null;
			int undoActions = m_actionHandler.UndoableActionCount;
			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "FixSegmentsForScriptureParas", Cache);
			Assert.AreEqual(undoActions, m_actionHandler.UndoableActionCount);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the FixSegmentsForScriptureParas method does nothing if the resegmentation was
		/// already run before.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForScriptureParas_AlreadyRun()
		{
			ReflectionHelper.SetProperty(m_scr, "ResegmentedParasWithOrcs", true);

			int undoActions = m_actionHandler.UndoableActionCount;
			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "FixSegmentsForScriptureParas", Cache);
			Assert.AreEqual(undoActions, m_actionHandler.UndoableActionCount);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the FixSegmentsForScriptureParas method does nothing when no paragraphs
		/// contain ORCs to process.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForScriptureParas_NoOrcs()
		{
			m_scr.ResourcesOC.Clear();

			CreateExodusData();

			int undoActions = m_actionHandler.UndoableActionCount;
			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "FixSegmentsForScriptureParas", Cache);
			Assert.AreEqual(undoActions + 1, m_actionHandler.UndoableActionCount, "Only action should be to set the CmResource indicating that we have done the fix.");
			Assert.IsTrue(m_scr.ResegmentedParasWithOrcs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the FixSegmentsForScriptureParas method does nothing when data contains
		/// an empty paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForScriptureParas_EmptyPara()
		{
			m_scr.ResourcesOC.Clear();

			IScrBook exodus = CreateExodusData();
			exodus.TitleOA[0].Contents = Cache.TsStrFactory.MakeString(String.Empty, Cache.DefaultVernWs);
			exodus.TitleOA[0].SegmentsOS.Clear();

			int undoActions = m_actionHandler.UndoableActionCount;
			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "FixSegmentsForScriptureParas", Cache);
			Assert.AreEqual(undoActions + 1, m_actionHandler.UndoableActionCount, "Only action should be to set the CmResource indicating that we have done the fix.");
			Assert.IsTrue(m_scr.ResegmentedParasWithOrcs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Special fix for single-segment paragraph which should have multiple
		/// segments (path with no analyses)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForScriptureParas_SplitSingleSegmentWithNoAnalyses()
		{
			IFdoServiceLocator sl = Cache.ServiceLocator;
			IScrBook book = sl.GetInstance<IScrBookFactory>().Create(2);
			IScrSection section = sl.GetInstance<IScrSectionFactory>().Create();
			book.SectionsOS.Add(section);
			IStText contents = sl.GetInstance<IStTextFactory>().Create();
			section.ContentOA = contents;
			IScrTxtPara para = sl.GetInstance<IScrTxtParaFactory>().CreateWithStyle(contents, "Paragraph");
			m_scr.ResourcesOC.Clear();
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Footnotes go here:      |          |        |
			bldr.Replace(0, 0, "This is a sentence for Tom and his wife. Good!",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			para.Contents = bldr.GetString(); // automatically creates TWO segments

			para.SegmentsOS.RemoveAt(1); // remove one, now it's in the bad state.
			para.ParseIsCurrent = false;
			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "FixSegmentsForScriptureParas", Cache);
			VerifyParaSegmentBreaks(para);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Special fix for single-segment paragraph which should have multiple segments (path
		/// with no analyses)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForScriptureParas_SplitSingleSegmentWithAnalyses()
		{
			IFdoServiceLocator sl = Cache.ServiceLocator;
			IScrBook book = sl.GetInstance<IScrBookFactory>().Create(2);
			IScrSection section = sl.GetInstance<IScrSectionFactory>().Create();
			book.SectionsOS.Add(section);
			IStText contents = sl.GetInstance<IStTextFactory>().Create();
			section.ContentOA = contents;
			IScrTxtPara para = sl.GetInstance<IScrTxtParaFactory>().CreateWithStyle(contents, "Paragraph");
			m_scr.ResourcesOC.Clear();
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Footnotes go here:      |          |        |
			bldr.Replace(0, 0, "This is a sentence for Tom and his wife. Good!",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			para.Contents = bldr.GetString(); // automatically creates TWO segments
			List<IAnalysis> expectedAnalyses1 = new List<IAnalysis>();
			List<IAnalysis> expectedAnalyses2 = new List<IAnalysis>();
			using (var parser = new ParagraphParser(para))
				parser.Parse(para); // now they have analyses!
			expectedAnalyses1.AddRange(para.SegmentsOS[0].AnalysesRS);
			expectedAnalyses2.AddRange(para.SegmentsOS[1].AnalysesRS);
			Assert.That(expectedAnalyses1, Has.Count.GreaterThan(0)); // not much of a test unless they both do
			Assert.That(expectedAnalyses2, Has.Count.GreaterThan(0));
			foreach (IAnalysis analysis in para.SegmentsOS[1].AnalysesRS)
				para.SegmentsOS[0].AnalysesRS.Add(analysis); // the first segment has all of them.

			para.SegmentsOS.RemoveAt(1); // remove one, now it's in the bad state.
			para.ParseIsCurrent = false;
			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "FixSegmentsForScriptureParas", Cache);

			VerifyParaSegmentBreaks(para);
			VerifySegmentAnalyses(para.SegmentsOS[0], expectedAnalyses1);
			VerifySegmentAnalyses(para.SegmentsOS[1], expectedAnalyses2);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This case tests a paragraph which was given 7.0-appropriate segments, probably as
		/// part of fluffing up, and then we try to migrate it. (We need at least four segments,
		/// so the merge will not reduce it to one, which triggers another repair process.)
		/// Need test where ORC occurs in a segment with punctuation but no word-forming chars.
		/// Jira FWR-1591 and FWR-1464.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForScriptureParas_FixFourSegmentsWithOrcNotIsolated()
		{
			var sl = Cache.ServiceLocator;
			var book = sl.GetInstance<IScrBookFactory>().Create(2);
			var section = sl.GetInstance<IScrSectionFactory>().Create();
			book.SectionsOS.Add(section);
			var contents = sl.GetInstance<IStTextFactory>().Create();
			section.ContentOA = contents;
			var para = sl.GetInstance<IScrTxtParaFactory>().CreateWithStyle(contents, "Paragraph");
			m_scr.ResourcesOC.Clear();
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Footnotes go here:                   |
			bldr.Replace(0, 0, "First seg. This is a sentence for Tom and his wife. Good! Bad!",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			Guid footnote1 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 20, 20, Cache.DefaultVernWs);
			para.Contents = bldr.GetString(); // automatically creates TWO segments
			para.ParseIsCurrent = false;
			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "FixSegmentsForScriptureParas", Cache);
			VerifyParaSegmentBreaks(para);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This case tests a paragraph which was given 7.0-appropriate segments, probably as
		/// part of fluffing up, and then we try to migrate it. This test covers the case
		/// where the ORC+wordforming data is the first segment. (We need at least three
		/// segments, so the merge will not reduce it to one, which triggers another repair
		/// process.)
		/// Jira FWR-1591 and FWR-1464.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForScriptureParas_FirstSegmentWithOrcNotIsolated()
		{
			var sl = Cache.ServiceLocator;
			var book = sl.GetInstance<IScrBookFactory>().Create(2);
			var section = sl.GetInstance<IScrSectionFactory>().Create();
			book.SectionsOS.Add(section);
			var contents = sl.GetInstance<IStTextFactory>().Create();
			section.ContentOA = contents;
			var para = sl.GetInstance<IScrTxtParaFactory>().CreateWithStyle(contents, "Paragraph");
			m_scr.ResourcesOC.Clear();
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Footnotes go here:                   |
			bldr.Replace(0, 0, "Looks like we have a sentence for Tom and his wife. Good! Deal!",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			Guid footnote1 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 20, 20, Cache.DefaultVernWs);
			para.Contents = bldr.GetString(); // automatically creates TWO segments
			para.ParseIsCurrent = false;
			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "FixSegmentsForScriptureParas", Cache);
			VerifyParaSegmentBreaks(para);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This case tests a paragraph which was given 7.0-appropriate segments, probably as
		/// part of fluffing up, and then we try to migrate it. This test covers the case
		/// where the ORC+wordforming data is the Last segment. (We need at least three
		/// segments, so the merge will not reduce it to one, which triggers another repair
		/// process.)
		/// Jira FWR-1591 and FWR-1464.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForScriptureParas_LastSegmentWithOrcNotIsolated()
		{
			var sl = Cache.ServiceLocator;
			var book = sl.GetInstance<IScrBookFactory>().Create(2);
			var section = sl.GetInstance<IScrSectionFactory>().Create();
			book.SectionsOS.Add(section);
			var contents = sl.GetInstance<IStTextFactory>().Create();
			section.ContentOA = contents;
			var para = sl.GetInstance<IScrTxtParaFactory>().CreateWithStyle(contents, "Paragraph");
			m_scr.ResourcesOC.Clear();
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Footnotes go here:                   |
			bldr.Replace(0, 0, "Good! Deal! Was it a sentence for Tom and his wife?",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			Guid footnote1 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 20, 20, Cache.DefaultVernWs);
			para.Contents = bldr.GetString(); // automatically creates TWO segments
			para.ParseIsCurrent = false;
			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "FixSegmentsForScriptureParas", Cache);
			VerifyParaSegmentBreaks(para);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This case tests a paragraph which was given 7.0-appropriate segments, probably as
		/// part of fluffing up, and then we try to migrate it. (We need at least four segments,
		/// so the merge will not reduce it to one, which triggers another repair process.)
		/// This is a bizarre edge-case where the ORC occurs in a segment with punctuation but
		/// no word-forming chars (to demonstrate why the fix can't be based on the presence of
		/// word-forming characters).
		/// Jira FWR-1591 and FWR-1464.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForScriptureParas_OrcPlusPunctuation()
		{
			var sl = Cache.ServiceLocator;
			var book = sl.GetInstance<IScrBookFactory>().Create(2);
			var section = sl.GetInstance<IScrSectionFactory>().Create();
			book.SectionsOS.Add(section);
			var contents = sl.GetInstance<IStTextFactory>().Create();
			section.ContentOA = contents;
			var para = sl.GetInstance<IScrTxtParaFactory>().CreateWithStyle(contents, "Paragraph");
			m_scr.ResourcesOC.Clear();
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Footnotes go here:                   |
			bldr.Replace(0, 0, "First seg. --,<>@~-$^&*()+=. Good! Bad!",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			Guid footnote1 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 20, 20, Cache.DefaultVernWs);
			para.Contents = bldr.GetString(); // automatically creates TWO segments
			para.ParseIsCurrent = false;
			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "FixSegmentsForScriptureParas", Cache);
			VerifyParaSegmentBreaks(para);
		}
		#endregion

		#region FixSegmentsForPara tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FixSegmentsForPara method when ORCs are in the middle of the paragraph
		/// contents and each segment has a full translation (free translation, literal
		/// translation, and notes for the segments). No other label segments (verse/chapter
		/// numbers)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForPara_ORCsInMiddleOfParaWithFullTrans()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Footnotes go here:      |          |        |
			bldr.Replace(0, 0, "This is a sentence for Tom and his wife. Good!",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			Guid footnote1 = Guid.NewGuid();
			Guid footnote2 = Guid.NewGuid();
			Guid footnote3 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 7, 7, Cache.DefaultVernWs);
			StringUtils.InsertOrcIntoPara(footnote2, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 19, 19, Cache.DefaultVernWs);
			StringUtils.InsertOrcIntoPara(footnote3, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 29, 29, Cache.DefaultVernWs);
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0, 7, "I ate", 1);
			AddSegmentToPara(m_para, 7, 9, null, 0);
			AddSegmentToPara(m_para, 9, 19, "a whole", 2);
			AddSegmentToPara(m_para, 19, 21, null, 0);
			AddSegmentToPara(m_para, 21, 29, "cow", 0);
			AddSegmentToPara(m_para, 29, 30, null, 0);
			AddSegmentToPara(m_para, 30, 44, "yesterday", 1);
			AddSegmentToPara(m_para, 44, bldr.Length, "Wonderful!", 1);

			bldr.Clear();
			// Footnotes go here:    |       |    |
			bldr.Replace(0, 0, "I ate a whole cow yesterday", StyleUtils.CharStyleTextProps(null, Cache.DefaultAnalWs));
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtNameGuidHot, bldr, 5, 5, Cache.DefaultAnalWs);
			StringUtils.InsertOrcIntoPara(footnote2, FwObjDataTypes.kodtNameGuidHot, bldr, 14, 14, Cache.DefaultAnalWs);
			StringUtils.InsertOrcIntoPara(footnote3, FwObjDataTypes.kodtNameGuidHot, bldr, 20, 20, Cache.DefaultAnalWs);
			ITsString expectedSegment0FT = bldr.GetString();

			CallFixSegmentsForPara();
			VerifyParaSegmentBreaks();
			VerifySegment(0, expectedSegment0FT, "Ix ate ax whole cow yesterday", new int[] { 1, 2, 1 });
			VerifySegment(1, Cache.TsStrFactory.MakeString("Wonderful!", Cache.DefaultAnalWs),
				"Wonderful!", new int[] { 1 });
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FixSegmentsForPara method when a hard line break character is followed by
		/// an ORC.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForPara_OrcFollowsHardLineBreak()
		{
			string strSeg1 = "This is a sentence. ";
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, strSeg1 + StringUtils.kChHardLB,
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			Guid footnote1 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtOwnNameGuidHot, bldr,
				bldr.Length, bldr.Length, Cache.DefaultVernWs);
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0, strSeg1.Length, "I ate", 1);
			AddSegmentToPara(m_para, strSeg1.Length, strSeg1.Length + 1, null, 0);
			AddSegmentToPara(m_para, bldr.Length - 1, bldr.Length, null, 0);

			CallFixSegmentsForPara();
			VerifyParaSegmentBreaks();
			ITsString emptyAnalFreeTrans = Cache.TsStrFactory.MakeString(String.Empty, Cache.DefaultAnalWs);
			VerifySegment(0, Cache.TsStrFactory.MakeString("I ate", Cache.DefaultAnalWs), "Ix ate", new int[] { 1 });
			VerifySegment(1, emptyAnalFreeTrans, null, new int[0]);
			VerifySegment(2, emptyAnalFreeTrans, null, new int[0]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FixSegmentsForPara method when a hard line break character precedes an ORC.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForPara_OrcPrecedesHardLineBreak()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Footnote here:  |
			bldr.Replace(0, 0, StringUtils.kChHardLB + "This is a sentence. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			Guid footnote1 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtOwnNameGuidHot, bldr,
				0, 0, Cache.DefaultVernWs);
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0, 1, null, 0);
			AddSegmentToPara(m_para, 1, 2, null, 0);
			AddSegmentToPara(m_para, 2, bldr.Length, "I ate", 1);

			CallFixSegmentsForPara();
			VerifyParaSegmentBreaks();
			ITsString emptyAnalFreeTrans = Cache.TsStrFactory.MakeString(String.Empty, Cache.DefaultAnalWs);
			VerifySegment(0, emptyAnalFreeTrans, null, new int[0]);
			VerifySegment(1, emptyAnalFreeTrans, null, new int[0]);
			VerifySegment(2, Cache.TsStrFactory.MakeString("I ate", Cache.DefaultAnalWs), "Ix ate", new int[] { 1 });
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FixSegmentsForPara method when a hard line break character precedes
		/// an ORC in the middle of a paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForPara_OrcPrecedesHardLineBreakInMiddle()
		{
			string strSeg1 = "This is a sentence. ";
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Footnote goes here:      |
			bldr.Replace(0, 0, strSeg1 + StringUtils.kChHardLB + "More text.",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			Guid footnote1 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtOwnNameGuidHot, bldr,
				strSeg1.Length, strSeg1.Length, Cache.DefaultVernWs);
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0, strSeg1.Length, "I ate", 1);
			AddSegmentToPara(m_para, strSeg1.Length, strSeg1.Length + 1, null, 0);
			AddSegmentToPara(m_para, strSeg1.Length + 1, strSeg1.Length + 2, null, 0);
			AddSegmentToPara(m_para, strSeg1.Length + 2, bldr.Length, "a monkey", 1);

			bldr.Clear();
			// Footnote here:         |
			bldr.Replace(0, 0, "I ate ", StyleUtils.CharStyleTextProps(null, Cache.DefaultAnalWs));
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtNameGuidHot, bldr, bldr.Length,
				bldr.Length, Cache.DefaultAnalWs);
			ITsString expectedSegment0FT = bldr.GetString();

			CallFixSegmentsForPara();
			VerifyParaSegmentBreaks();
			ITsString emptyAnalFreeTrans = Cache.TsStrFactory.MakeString(String.Empty, Cache.DefaultAnalWs);
			VerifySegment(0, expectedSegment0FT, "Ix ate", new int[] { 1 });
			VerifySegment(1, emptyAnalFreeTrans, null, new int[0]); // hard line break
			VerifySegment(2, Cache.TsStrFactory.MakeString("a monkey",
				Cache.DefaultAnalWs), "ax monkey", new int[] { 1 });
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FixSegmentsForPara method when a hard line break character is followed by
		/// an ORC in the middle of a paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForPara_OrcFollowsHardLineBreakInMiddle()
		{
			string strSeg1 = "This is a sentence. ";
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Footnote goes here:                                |
			bldr.Replace(0, 0, strSeg1 + StringUtils.kChHardLB + "More text.",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			Guid footnote1 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtOwnNameGuidHot, bldr,
				strSeg1.Length + 1, strSeg1.Length + 1, Cache.DefaultVernWs);
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0, strSeg1.Length, "I ate", 1);
			AddSegmentToPara(m_para, strSeg1.Length, strSeg1.Length + 1, null, 0);
			AddSegmentToPara(m_para, strSeg1.Length + 1, strSeg1.Length + 2, null, 0);
			AddSegmentToPara(m_para, strSeg1.Length + 2, bldr.Length, "a monkey", 1);

			bldr.Clear();
			// Footnote here:   |
			bldr.Replace(0, 0, "a monkey", StyleUtils.CharStyleTextProps(null, Cache.DefaultAnalWs));
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtNameGuidHot, bldr, 0, 0, Cache.DefaultAnalWs);
			ITsString expectedSegment2FT = bldr.GetString();

			CallFixSegmentsForPara();
			VerifyParaSegmentBreaks();
			ITsString emptyAnalFreeTrans = Cache.TsStrFactory.MakeString(String.Empty, Cache.DefaultAnalWs);
			VerifySegment(0, Cache.TsStrFactory.MakeString("I ate", Cache.DefaultAnalWs), "Ix ate", new int[] { 1 });
			VerifySegment(1, emptyAnalFreeTrans, null, new int[0]); // hard line break
			VerifySegment(2, expectedSegment2FT, "ax monkey", new int[] { 1 }); // foootnote and following text
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FixSegmentsForPara method when a hard line break character is followed by
		/// an ORC at the beginning of a label segment.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForPara_OrcAtStartOfLabelFollowsHardLineBreak()
		{
			string strSeg1 = "4This is a sentence. ";
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Footnote goes here:                                |
			bldr.Replace(0, 0, strSeg1 + StringUtils.kChHardLB + "5More text.",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			bldr.SetStrPropValue(0, 1, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			bldr.SetStrPropValue(strSeg1.Length + 1, strSeg1.Length + 2, (int)FwTextPropType.ktptNamedStyle,
				ScrStyleNames.VerseNumber);
			Guid footnote1 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtOwnNameGuidHot, bldr,
				strSeg1.Length + 1, strSeg1.Length + 1, Cache.DefaultVernWs);
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0, 1, null, 0);
			AddSegmentToPara(m_para, 1, strSeg1.Length, "I ate", 1);
			AddSegmentToPara(m_para, strSeg1.Length, strSeg1.Length + 1, null, 0);
			AddSegmentToPara(m_para, strSeg1.Length + 1, strSeg1.Length + 3, null, 0);
			AddSegmentToPara(m_para, strSeg1.Length + 3, bldr.Length, "a monkey", 1);

			CallFixSegmentsForPara();
			VerifyParaSegmentBreaks();
			ITsString emptyAnalFreeTrans = Cache.TsStrFactory.MakeString(String.Empty, Cache.DefaultAnalWs);
			VerifySegment(0, emptyAnalFreeTrans, null, new int[0]);
			VerifySegment(1, Cache.TsStrFactory.MakeString("I ate", Cache.DefaultAnalWs), "Ix ate", new int[] { 1 });
			VerifySegment(2, emptyAnalFreeTrans, null, new int[0]); // hard line break
			VerifySegment(3, emptyAnalFreeTrans, null, new int[0]); // foootnote and verse number
			VerifySegment(4, Cache.TsStrFactory.MakeString("a monkey", Cache.DefaultAnalWs), "ax monkey", new int[] { 1 });
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FixSegmentsForPara method when an ORC at the end of a label segment
		/// immediately precedes a hard line break character.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForPara_OrcAtEndOfLabelPrecedesHardLineBreak()
		{

			// Footnote goes here:                  |
			string strSeg1 = "4This is a sentence. 5";
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, strSeg1 + StringUtils.kChHardLB + "More text.",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			bldr.SetStrPropValue(0, 1, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			bldr.SetStrPropValue(strSeg1.Length - 1, strSeg1.Length, (int)FwTextPropType.ktptNamedStyle,
				ScrStyleNames.VerseNumber);
			Guid footnote1 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtOwnNameGuidHot, bldr,
				strSeg1.Length, strSeg1.Length, Cache.DefaultVernWs);
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0, 1, null, 0);
			AddSegmentToPara(m_para, 1, strSeg1.Length - 1, "I ate", 1);
			AddSegmentToPara(m_para, strSeg1.Length - 1, strSeg1.Length + 1, null, 0); // verse & footnote
			AddSegmentToPara(m_para, strSeg1.Length + 1, strSeg1.Length + 2, null, 0); // hard line break
			AddSegmentToPara(m_para, strSeg1.Length + 2, bldr.Length, "a monkey", 1);

			CallFixSegmentsForPara();

			VerifyParaSegmentBreaks();
			ITsString emptyAnalFreeTrans = Cache.TsStrFactory.MakeString(String.Empty, Cache.DefaultAnalWs);
			VerifySegment(0, emptyAnalFreeTrans, null, new int[0]);
			VerifySegment(1, Cache.TsStrFactory.MakeString("I ate", Cache.DefaultAnalWs), "Ix ate", new int[] { 1 });
			VerifySegment(2, emptyAnalFreeTrans, null, new int[0]); // verse number
			VerifySegment(3, emptyAnalFreeTrans, null, new int[0]); // foootnote
			VerifySegment(4, emptyAnalFreeTrans, null, new int[0]); // hard line break
			VerifySegment(5, Cache.TsStrFactory.MakeString("a monkey", Cache.DefaultAnalWs), "ax monkey", new int[] { 1 });
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FixSegmentsForPara method when an ORC at the end of a label segment
		/// immediately precedes a space and a hard line break character.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForPara_OrcAtEndOfLabelPrecedesSpaceAndHardLineBreak()
		{

			// Footnote goes here:                  |
			string strSeg1 = "4This is a sentence. 5 ";
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, strSeg1 + StringUtils.kChHardLB + "More text.",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			bldr.SetStrPropValue(0, 1, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			bldr.SetStrPropValue(strSeg1.Length - 2, strSeg1.Length - 1, (int)FwTextPropType.ktptNamedStyle,
				ScrStyleNames.VerseNumber);
			Guid footnote1 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtOwnNameGuidHot, bldr,
				strSeg1.Length - 1, strSeg1.Length - 1, Cache.DefaultVernWs);
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0, 1, null, 0);
			AddSegmentToPara(m_para, 1, strSeg1.Length - 2, "I ate", 1);
			AddSegmentToPara(m_para, strSeg1.Length - 2, strSeg1.Length + 1, null, 0); // verse & footnote & space
			AddSegmentToPara(m_para, strSeg1.Length + 1, strSeg1.Length + 2, null, 0); // hard line break
			AddSegmentToPara(m_para, strSeg1.Length + 2, bldr.Length, "a monkey", 1);

			CallFixSegmentsForPara();

			VerifyParaSegmentBreaks();
			ITsString emptyAnalFreeTrans = Cache.TsStrFactory.MakeString(String.Empty, Cache.DefaultAnalWs);
			VerifySegment(0, emptyAnalFreeTrans, null, new int[0]);
			VerifySegment(1, Cache.TsStrFactory.MakeString("I ate", Cache.DefaultAnalWs), "Ix ate", new int[] { 1 });
			VerifySegment(2, emptyAnalFreeTrans, null, new int[0]); // verse number
			VerifySegment(3, emptyAnalFreeTrans, null, new int[0]); // foootnote
			VerifySegment(4, emptyAnalFreeTrans, null, new int[0]); // hard line break
			VerifySegment(5, Cache.TsStrFactory.MakeString("a monkey", Cache.DefaultAnalWs), "ax monkey", new int[] { 1 });
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FixSegmentsForPara method when an ORC precedes a non-word forming character.
		/// (FWR-2873)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForPara_OrcPrecedesNonWordForming()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Footnote here:                      |
			bldr.Replace(0, 0, "This is a sentence.> Nother segment.",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			Guid footnote1 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtOwnNameGuidHot, bldr,
				19, 19, Cache.DefaultVernWs);
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0, 19, "I ate", 2);
			AddSegmentToPara(m_para, 19, 20, null, 0);
			AddSegmentToPara(m_para, 20, bldr.Length, "a monkey", 1);

			bldr.Clear();
			// Footnotes here:       |
			bldr.Replace(0, 0, "I ate", StyleUtils.CharStyleTextProps(null, Cache.DefaultAnalWs));
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtNameGuidHot, bldr, bldr.Length,
				bldr.Length, Cache.DefaultAnalWs);
			ITsString expectedSegment0FT = bldr.GetString();

			CallFixSegmentsForPara();
			VerifyParaSegmentBreaks();
			VerifySegment(0, expectedSegment0FT, "Ix ate", new int[] { 2 });
			VerifySegment(1, Cache.TsStrFactory.MakeString("a monkey", Cache.DefaultAnalWs), "ax monkey", new int[] { 1 });
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FixSegmentsForPara method when an ORC follows a space and a non-word
		/// forming character. (FWR-2873)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForPara_OrcFollowsSpaceAndNonWordForming()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Footnote here:                        |
			bldr.Replace(0, 0, "This is a sentence. <Nother segment.",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			Guid footnote1 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtOwnNameGuidHot, bldr,
				21, 21, Cache.DefaultVernWs);
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0, 21, "I ate", 2);
			AddSegmentToPara(m_para, 21, 22, null, 0);
			AddSegmentToPara(m_para, 22, bldr.Length, "a monkey", 1);

			bldr.Clear();
			// Footnotes here:  |
			bldr.Replace(0, 0, "a monkey", StyleUtils.CharStyleTextProps(null, Cache.DefaultAnalWs));
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtNameGuidHot, bldr, 0, 0, Cache.DefaultAnalWs);
			ITsString expectedSegment1FT = bldr.GetString();

			CallFixSegmentsForPara();
			VerifyParaSegmentBreaks();
			VerifySegment(0, Cache.TsStrFactory.MakeString("I ate", Cache.DefaultAnalWs), "Ix ate", new int[] { 2 });
			VerifySegment(1, expectedSegment1FT, "ax monkey", new int[] { 1 });
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FixSegmentsForPara method with a paragraph whose segments do not have any
		/// analyses, translations, or notes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForPara_NoAnalysesOrTranslations()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Footnotes go here:      |          |        |
			bldr.Replace(0, 0, "This is a sentence for Tom and his wife. Good!",
			StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			Guid footnote1 = Guid.NewGuid();
			Guid footnote2 = Guid.NewGuid();
			Guid footnote3 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 7, 7, Cache.DefaultVernWs);
			StringUtils.InsertOrcIntoPara(footnote2, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 19, 19, Cache.DefaultVernWs);
			StringUtils.InsertOrcIntoPara(footnote3, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 29, 29, Cache.DefaultVernWs);
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0);
			AddSegmentToPara(m_para, 7);
			AddSegmentToPara(m_para, 9);
			AddSegmentToPara(m_para, 19);
			AddSegmentToPara(m_para, 21);
			AddSegmentToPara(m_para, 29);
			AddSegmentToPara(m_para, 30);
			AddSegmentToPara(m_para, 44);

			CallFixSegmentsForPara();
			VerifyParaSegmentBreaks();
			ITsString emptyAnalFreeTrans = Cache.TsStrFactory.MakeString(String.Empty, Cache.DefaultAnalWs);
			VerifySegment(0);
			VerifySegment(1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FixSegmentsForPara method with a paragraph having a segment before an ORC
		/// which has no translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForPara_EmptyTranslation()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Footnote goes here:    |
			bldr.Replace(0, 0, "Before after", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			Guid footnote1 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 6, 6, Cache.DefaultVernWs);
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0); // First segment gets no analyses
			AddSegmentToPara(m_para, 6, 8, null, 0);
			AddSegmentToPara(m_para, 8, bldr.Length, "despues", 0);

			bldr.Clear();
			// Footnotes here:  |
			bldr.Replace(0, 0, " despues", StyleUtils.CharStyleTextProps(null, Cache.DefaultAnalWs));
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtNameGuidHot, bldr, 0, 0, Cache.DefaultAnalWs);
			ITsString expectedSegment0FT = bldr.GetString();

			CallFixSegmentsForPara();
			VerifyParaSegmentBreaks();
			VerifySegment(0, expectedSegment0FT, "despues", new int[0], new int[] { 0 });
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FixSegmentsForPara method with a paragraph having a segment before an ORC
		/// which has no translation. This handles the special case where the vernacular segment
		/// before the ORC ends with a space. Jira issue is FWR-3295
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForPara_EmptyTranslation_PrecedingSegmentEndsWithSpace()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Footnote goes here:     |
			bldr.Replace(0, 0, "Before after", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			Guid footnote1 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 7, 7, Cache.DefaultVernWs);
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0);
			AddSegmentToPara(m_para, 7, 8, null, 0);
			AddSegmentToPara(m_para, 8, bldr.Length, "despues", 0);

			bldr.Clear();
			// Footnote here:   |
			bldr.Replace(0, 0, "despues", StyleUtils.CharStyleTextProps(null, Cache.DefaultAnalWs));
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtNameGuidHot, bldr, 0, 0, Cache.DefaultAnalWs);
			ITsString expectedSegment0FT = bldr.GetString();

			CallFixSegmentsForPara();
			VerifyParaSegmentBreaks();
			VerifySegment(0, expectedSegment0FT, "despues", new int[0], new int[] { 0 });
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FixSegmentsForPara method when a picture ORC is in the middle of the
		/// paragraph contents and each segment has a full translation (free translation,
		/// literal translation, and notes for the segments). No other label segments (verse/
		/// chapter numbers)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForPara_PictureORC()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Picture goes here:                 |
			bldr.Replace(0, 0, "This is a sentence for Tom and his wife.",
			StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			Guid picture1 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(picture1, FwObjDataTypes.kodtGuidMoveableObjDisp, bldr, 18, 18, Cache.DefaultVernWs);
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0, 18, "I ate a whole", 1);
			AddSegmentToPara(m_para, 18, 20, null, 0);
			AddSegmentToPara(m_para, 20, bldr.Length, "cow yesterday", 2);

			bldr.Clear();
			// Picture goes here:            |
			bldr.Replace(0, 0, "I ate a whole cow yesterday", StyleUtils.CharStyleTextProps(null, Cache.DefaultAnalWs));
			StringUtils.InsertOrcIntoPara(picture1, FwObjDataTypes.kodtGuidMoveableObjDisp, bldr, 13, 13, Cache.DefaultAnalWs);
			ITsString expectedSegment0FT = bldr.GetString();

			CallFixSegmentsForPara();
			VerifyParaSegmentBreaks();
			VerifySegment(0, expectedSegment0FT, "Ix atex ax whole cowx yesterday", new int[] { 1, 2 });
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FixSegmentsForPara method when an ORC is the only contents in a paragraph
		/// and each segment. No other label segments (verse/ chapter numbers). FWR-1503
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForPara_OrcOnly()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, string.Empty, StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			Guid footnote1 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 0, 0, Cache.DefaultVernWs);
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0, 1, string.Empty, 0);

			bldr.Clear();
			bldr.Replace(0, 0, string.Empty, StyleUtils.CharStyleTextProps(null, Cache.DefaultAnalWs));
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtNameGuidHot, bldr, 0, 0, Cache.DefaultAnalWs);
			ITsString expectedSegment0FT = bldr.GetString();

			CallFixSegmentsForPara();
			VerifyParaSegmentBreaks();
			VerifySegment(0, expectedSegment0FT, null, new int[0], null, new int[0], false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FixSegmentsForPara method when an ORC is in the middle of a word.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForPara_ORCInMiddleOfWord()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Picture goes here:                 |
			bldr.Replace(0, 0, "This is a sentencefor Tom and his wife.",
			StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			Guid picture1 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(picture1, FwObjDataTypes.kodtGuidMoveableObjDisp, bldr, 18, 18, Cache.DefaultVernWs);
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0, 18, "I ate a whole", 1);
			AddSegmentToPara(m_para, 18, 19, null, 0);
			AddSegmentToPara(m_para, 19, bldr.Length, "cow yesterday", 2);

			bldr.Clear();
			// Picture goes here:            |
			bldr.Replace(0, 0, "I ate a wholecow yesterday", StyleUtils.CharStyleTextProps(null, Cache.DefaultAnalWs));
			StringUtils.InsertOrcIntoPara(picture1, FwObjDataTypes.kodtGuidMoveableObjDisp, bldr, 13, 13, Cache.DefaultAnalWs);
			ITsString expectedSegment0FT = bldr.GetString();

			CallFixSegmentsForPara();
			VerifyParaSegmentBreaks();
			VerifySegment(0, expectedSegment0FT, "Ix atex ax wholecowx yesterday", new int[] { 1, 2 });
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FixSegmentsForPara method when an ORC is in the middle of a word.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForPara_ORCInMiddleOfWord_FollowingFreeTransHasExtraLeadingSpace()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Picture goes here:                 |
			bldr.Replace(0, 0, "This is a sentencefor Tom and his wife.",
			StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			Guid picture1 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(picture1, FwObjDataTypes.kodtGuidMoveableObjDisp, bldr, 18, 18, Cache.DefaultVernWs);
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0, 18, "I ate a whole", 1);
			AddSegmentToPara(m_para, 18, 19, null, 0);
			AddSegmentToPara(m_para, 19, bldr.Length, " cow yesterday", 2); // Notice extra leading space

			bldr.Clear();
			// Picture goes here:            |
			bldr.Replace(0, 0, "I ate a whole cow yesterday", StyleUtils.CharStyleTextProps(null, Cache.DefaultAnalWs));
			StringUtils.InsertOrcIntoPara(picture1, FwObjDataTypes.kodtGuidMoveableObjDisp, bldr, 13, 13, Cache.DefaultAnalWs);
			ITsString expectedSegment0FT = bldr.GetString();

			CallFixSegmentsForPara();
			VerifyParaSegmentBreaks();
			VerifySegment(0, expectedSegment0FT, "Ix atex ax wholex cowx yesterday", new int[] { 1, 2 });
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FixSegmentsForPara method when the free translation before an ORC has a
		/// trailing space which corresponds to the trailing space in the baseline segment.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForPara_FreeTransBeforeORCHasTrailingSpace()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Picture goes here:                  |
			bldr.Replace(0, 0, "This is a sentence for Tom and his wife.",
			StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			Guid picture1 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(picture1, FwObjDataTypes.kodtGuidMoveableObjDisp, bldr, 19, 19, Cache.DefaultVernWs);
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0, 19, "I ate a whole ", 1); // Notice pretty trailing space
			AddSegmentToPara(m_para, 19, 20, null, 0);
			AddSegmentToPara(m_para, 20, bldr.Length, "cow yesterday", 2);

			bldr.Clear();
			// Picture goes here:             |
			bldr.Replace(0, 0, "I ate a whole cow yesterday", StyleUtils.CharStyleTextProps(null, Cache.DefaultAnalWs));
			StringUtils.InsertOrcIntoPara(picture1, FwObjDataTypes.kodtGuidMoveableObjDisp, bldr, 14, 14, Cache.DefaultAnalWs);
			ITsString expectedSegment0FT = bldr.GetString();

			CallFixSegmentsForPara();
			VerifyParaSegmentBreaks();
			VerifySegment(0, expectedSegment0FT, "Ix atex ax wholex cowx yesterday", new int[] { 1, 2 });
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FixSegmentsForPara method when the free translation following an ORC has a
		/// leading space even though the space needed to provide the separation is already in
		/// the ORC segment.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForPara_FreeTransFollowingORCHasExtraLeadingSpace()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Picture goes here:                 |
			bldr.Replace(0, 0, "This is a sentence for Tom and his wife.",
			StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			Guid picture1 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(picture1, FwObjDataTypes.kodtGuidMoveableObjDisp, bldr, 18, 18, Cache.DefaultVernWs);
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0, 18, "I ate a whole", 1);
			AddSegmentToPara(m_para, 18, 20, null, 0);
			AddSegmentToPara(m_para, 20, bldr.Length, " cow yesterday", 2); // Notice extra leading space (deal with it)

			bldr.Clear();
			// Picture goes here:            |
			bldr.Replace(0, 0, "I ate a whole cow yesterday", StyleUtils.CharStyleTextProps(null, Cache.DefaultAnalWs));
			StringUtils.InsertOrcIntoPara(picture1, FwObjDataTypes.kodtGuidMoveableObjDisp, bldr, 13, 13, Cache.DefaultAnalWs);
			ITsString expectedSegment0FT = bldr.GetString();

			CallFixSegmentsForPara();
			VerifyParaSegmentBreaks();
			VerifySegment(0, expectedSegment0FT, "Ix atex ax whole x cowx yesterday", new int[] { 1, 2 });
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FixSegmentsForPara method when ORCs are at the beginning and end of
		/// the paragraph contents. No other label segments (verse/chapter numbers)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForPara_ORCsAtBeginningAndEndOfPara()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Footnotes here:  |                                             |
			bldr.Replace(0, 0, "This is a sentence for Tom and his wife. Good!",
			StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			Guid footnote1 = Guid.NewGuid();
			Guid footnote2 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 0, 0, Cache.DefaultVernWs);
			StringUtils.InsertOrcIntoPara(footnote2, FwObjDataTypes.kodtOwnNameGuidHot, bldr, bldr.Length, bldr.Length, Cache.DefaultVernWs);
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0, 1, null, 0);
			AddSegmentToPara(m_para, 1, 42, "I ate", 0);
			AddSegmentToPara(m_para, 42, bldr.Length - 1, "Wonderful!", 0);
			AddSegmentToPara(m_para, bldr.Length - 1, bldr.Length, null, 0);

			bldr.Clear();
			// Footnotes here:  |
			bldr.Replace(0, 0, "I ate", StyleUtils.CharStyleTextProps(null, Cache.DefaultAnalWs));
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtNameGuidHot, bldr, 0, 0, Cache.DefaultAnalWs);
			ITsString expectedSegment0FT = bldr.GetString();

			bldr.Clear();
			// Footnotes here:            |
			bldr.Replace(0, 0, "Wonderful!", StyleUtils.CharStyleTextProps(null, Cache.DefaultAnalWs));
			StringUtils.InsertOrcIntoPara(footnote2, FwObjDataTypes.kodtNameGuidHot, bldr, bldr.Length, bldr.Length, Cache.DefaultAnalWs);
			ITsString expectedSegment1FT = bldr.GetString();

			CallFixSegmentsForPara();
			VerifyParaSegmentBreaks();
			VerifySegment(0, expectedSegment0FT, "Ix ate", new int[0]);
			VerifySegment(1, expectedSegment1FT, "Wonderful!", new int[0]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FixSegmentsForPara method when an ORC is between two sentences and each
		/// sentence segment has a full translation (free translation, literal translation, and
		/// notes for the segments). No other label segments (verse/chapter numbers)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForPara_OrcBetweenSentencesWithFullTrans()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Footnotes go here:                    |              |
			bldr.Replace(0, 0, "This is sentence one. Sentence two. Sentence three.",
			StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			Guid footnote1 = Guid.NewGuid();
			Guid footnote2 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 21, 21, Cache.DefaultVernWs);
			StringUtils.InsertOrcIntoPara(footnote2, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 37, 37, Cache.DefaultVernWs);
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0, 21, "FT of sentence1.", 1);
			AddSegmentToPara(m_para, 21, 23, null, 0);
			AddSegmentToPara(m_para, 23, 37, "FT of Sentence2.", 2);
			AddSegmentToPara(m_para, 37, 38, null, 0);
			AddSegmentToPara(m_para, 38, bldr.Length, "FT of sentence 3.", 0);

			bldr.Clear();
			// Footnotes go here:               |
			bldr.Replace(0, 0, "FT of sentence1.", StyleUtils.CharStyleTextProps(null, Cache.DefaultAnalWs));
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtNameGuidHot, bldr, bldr.Length, bldr.Length, Cache.DefaultAnalWs);
			ITsString expectedSegment0FT = bldr.GetString();

			bldr.Clear();
			// Footnotes here:  |
			bldr.Replace(0, 0, "FT of sentence 3.", StyleUtils.CharStyleTextProps(null, Cache.DefaultAnalWs));
			StringUtils.InsertOrcIntoPara(footnote2, FwObjDataTypes.kodtNameGuidHot, bldr, 0, 0, Cache.DefaultAnalWs);
			ITsString expectedSegment2FT = bldr.GetString();

			CallFixSegmentsForPara();
			VerifyParaSegmentBreaks();
			VerifySegment(0, expectedSegment0FT, "FTx ofx sentence1.", new int[] { 1 });
			VerifySegment(1, Cache.TsStrFactory.MakeString("FT of Sentence2.", Cache.DefaultAnalWs),
				"FTx ofx Sentence2.", new int[] { 2 });
			VerifySegment(2, expectedSegment2FT, "FTx ofx sentencex 3.", new int[0]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FixSegmentsForPara method when an ORC following an end-of-sentence
		/// character and a quote mark and each sentence segment has a full translation (free
		/// translation, literal translation, and notes for the segments). No other label
		/// segments (verse/chapter numbers) (FWR-2207)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForPara_OrcFollowingEOSAndQuoteMarks()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Footnotes go here:                      |
			bldr.Replace(0, 0, "'This is sentence one.' Sentence two.",
			StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			Guid footnote1 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 23, 23, Cache.DefaultVernWs);
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0, 23, "FT of sentence1.", 0, false);
			AddSegmentToPara(m_para, 23, 25, null, 0, false);
			AddSegmentToPara(m_para, 25, bldr.Length, "FT of Sentence2.", 0, false);

			bldr.Clear();
			// Footnotes go here:               |
			bldr.Replace(0, 0, "FT of sentence1.", StyleUtils.CharStyleTextProps(null, Cache.DefaultAnalWs));
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtNameGuidHot, bldr, bldr.Length, bldr.Length, Cache.DefaultAnalWs);
			ITsString expectedSegment0FT = bldr.GetString();

			CallFixSegmentsForPara();
			VerifyParaSegmentBreaks();
			VerifySegment(0, expectedSegment0FT, "FTx ofx sentence1.", new int[0], new int[0], new int[0], false);
			VerifySegment(1, Cache.TsStrFactory.MakeString("FT of Sentence2.", Cache.DefaultAnalWs),
				"FTx ofx Sentence2.", new int[0], new int[0], new int[0], false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FixSegmentsForPara method when an ORC is before/after verse numbers.
		/// Each sentence segment has a full translation (free translation, literal translation,
		/// and notes for the segments).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForPara_OrcAdjacentToVerseNumbers()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Footnotes go here: |                   ||
			bldr.Replace(0, 0, "31 This is verse one. 2 Verse two.",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			bldr.SetStrPropValue(0, 1, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.ChapterNumber);
			bldr.SetStrPropValue(1, 2, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			bldr.SetStrPropValue(22, 23, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			Guid footnote1 = Guid.NewGuid();
			Guid footnote2 = Guid.NewGuid();
			Guid footnote3 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 2, 2, Cache.DefaultVernWs);
			StringUtils.InsertOrcIntoPara(footnote2, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 23, 23, Cache.DefaultVernWs);
			StringUtils.InsertOrcIntoPara(footnote3, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 25, 25, Cache.DefaultVernWs);
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0, 4, null, 0);
			AddSegmentToPara(m_para, 4, 23, "FT of verse 1.", 1);
			AddSegmentToPara(m_para, 23, 27, null, 0);
			AddSegmentToPara(m_para, 27, bldr.Length, "FT of verse2.", 2);

			bldr.Clear();
			// Footnotes here:  |               |
			bldr.Replace(0, 0, " FT of verse 1. ", StyleUtils.CharStyleTextProps(null, Cache.DefaultAnalWs));
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtNameGuidHot, bldr, 0, 0, Cache.DefaultAnalWs);
			StringUtils.InsertOrcIntoPara(footnote2, FwObjDataTypes.kodtNameGuidHot, bldr, bldr.Length, bldr.Length, Cache.DefaultAnalWs);
			ITsString expectedSegment1FT = bldr.GetString();
			bldr.Clear();
			// Footnotes here:  |
			bldr.Replace(0, 0, " FT of verse2.", StyleUtils.CharStyleTextProps(null, Cache.DefaultAnalWs));
			StringUtils.InsertOrcIntoPara(footnote3, FwObjDataTypes.kodtNameGuidHot, bldr, 0, 0, Cache.DefaultAnalWs);
			ITsString expectedSegment3FT = bldr.GetString();

			CallFixSegmentsForPara();
			VerifyParaSegmentBreaks();
			ITsString emptyAnalFreeTrans = Cache.TsStrFactory.MakeString(String.Empty, Cache.DefaultAnalWs);
			VerifySegment(0, emptyAnalFreeTrans, null, new int[0]);
			// ENHANCE: Ideally we wouldn't want the space at the end of the literral translation,
			// however, the current code adds one and we don't feel like it's critical enough
			// to remove.
			VerifySegment(1, expectedSegment1FT, "FTx ofx versex 1. ", new int[] { 1 });
			VerifySegment(2, emptyAnalFreeTrans, null, new int[0]);
			VerifySegment(3, expectedSegment3FT, "FTx ofx verse2.", new int[] { 2 });
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FixSegmentsForPara method when ORCs occur between chapter and verse
		/// numbers. Each sentence segment has a full translation (free translation, literal
		/// translation, and notes for the segments).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForPara_OrcBetweenChapterAndVerseNumbers()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Footnotes here:   ||
			bldr.Replace(0, 0, "312This is verse two.", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			bldr.SetStrPropValue(0, 1, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.ChapterNumber);
			bldr.SetStrPropValue(1, 3, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			Guid footnote1 = Guid.NewGuid();
			Guid footnote2 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 1, 1, Cache.DefaultVernWs);
			StringUtils.InsertOrcIntoPara(footnote2, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 3, 3, Cache.DefaultVernWs);
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0, 5, null, 0);
			AddSegmentToPara(m_para, 5, bldr.Length, "FT of verse 2.", 1);

			bldr.Clear();
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtNameGuidHot, bldr, 0, 0, Cache.DefaultAnalWs);
			ITsString expectedSegment1FT = bldr.GetString();

			bldr.Clear();
			StringUtils.InsertOrcIntoPara(footnote2, FwObjDataTypes.kodtNameGuidHot, bldr, 0, 0, Cache.DefaultAnalWs);
			ITsString expectedSegment3FT = bldr.GetString();

			CallFixSegmentsForPara();
			VerifyParaSegmentBreaks();
			ITsString emptyAnalFreeTrans = Cache.TsStrFactory.MakeString(String.Empty, Cache.DefaultAnalWs);
			VerifySegment(0, emptyAnalFreeTrans, null, new int[0]);
			VerifySegment(1, expectedSegment1FT, null, new int[0]);
			VerifySegment(2, emptyAnalFreeTrans, null, new int[0]);
			VerifySegment(3, expectedSegment3FT, null, new int[0]);
			VerifySegment(4, emptyAnalFreeTrans, null, new int[0]);
			VerifySegment(5, Cache.TsStrFactory.MakeString("FT of verse 2.", Cache.DefaultAnalWs),
				"FTx ofx versex 2.", new int[] { 1 });
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FixSegmentsForPara method when an ORC occurs after a verse number when there
		/// is a space immediately before the ORC. Each sentence segment has a full translation
		/// (free translation, literal translation, and notes for the segments).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForPara_OrcAfterSpaceAfterVerseNumber()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Footnotes here:    |
			bldr.Replace(0, 0, "1 This is verse two.", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			bldr.SetStrPropValue(0, 1, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			Guid footnote1 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 2, 2, Cache.DefaultVernWs);
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0, 3, null, 0);
			AddSegmentToPara(m_para, 3, bldr.Length, "FT of verse 2.", 1);

			bldr.Clear();
			// Footnotes here:  |
			bldr.Replace(0, 0, "FT of verse 2.", StyleUtils.CharStyleTextProps(null, Cache.DefaultAnalWs));
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtNameGuidHot, bldr, 0, 0, Cache.DefaultAnalWs);
			ITsString expectedSegment2FT = bldr.GetString();

			CallFixSegmentsForPara();
			VerifyParaSegmentBreaks();
			ITsString emptyAnalFreeTrans = Cache.TsStrFactory.MakeString(String.Empty, Cache.DefaultAnalWs);
			VerifySegment(0, emptyAnalFreeTrans, null, new int[0]);
			VerifySegment(1, expectedSegment2FT, "FTx ofx versex 2.", new int[] { 1 });
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FixSegmentsForPara method when an ORC occurs before a verse number when there
		/// is a space immediately after the ORC. Each sentence segment has a full translation
		/// (free translation, literal translation, and notes for the segments).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForPara_OrcBeforeSpaceBeforeVerseNumber()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Footnotes here:            |
			bldr.Replace(0, 0, "1Verse one 2This is verse two.", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			bldr.SetStrPropValue(0, 1, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			bldr.SetStrPropValue(11, 12, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			Guid footnote1 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 10, 10, Cache.DefaultVernWs);
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0, 1, null, 0);
			AddSegmentToPara(m_para, 1, 10, "FT of verse 1.", 2);
			AddSegmentToPara(m_para, 10, 13, null, 0);
			AddSegmentToPara(m_para, 13, bldr.Length, "FT of verse 2.", 1);

			bldr.Clear();
			// Footnotes here:                |
			bldr.Replace(0, 0, "FT of verse 1.", StyleUtils.CharStyleTextProps(null, Cache.DefaultAnalWs));
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtNameGuidHot, bldr, 14, 14, Cache.DefaultAnalWs);
			ITsString expectedSegment2FT = bldr.GetString();

			CallFixSegmentsForPara();
			VerifyParaSegmentBreaks();
			ITsString emptyAnalFreeTrans = Cache.TsStrFactory.MakeString(String.Empty, Cache.DefaultAnalWs);
			VerifySegment(0, emptyAnalFreeTrans, null, new int[0]);
			VerifySegment(1, expectedSegment2FT, "FTx ofx versex 1.", new int[] { 2 });
			VerifySegment(2, emptyAnalFreeTrans, null, new int[0]);
			VerifySegment(3, Cache.TsStrFactory.MakeString("FT of verse 2.", Cache.DefaultAnalWs),
				"FTx ofx versex 2.", new int[]{ 1 });
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FixSegmentsForPara method when two ORCs occur between verse numbers
		/// separated by one or more spaces. Each sentence segment has a full translation
		/// (free translation, literal translation, and notes for the segments).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForPara_OrcsBetweenVerseNumbersWithSpaces()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Footnotes here:    | |
			bldr.Replace(0, 0, "1     2 Verse two.", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			bldr.SetStrPropValue(0, 1, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			bldr.SetStrPropValue(6, 7, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			Guid footnote1 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 2, 2, Cache.DefaultVernWs);
			Guid footnote2 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(footnote2, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 5, 5, Cache.DefaultVernWs);
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0, 10, null, 0);
			AddSegmentToPara(m_para, 10, bldr.Length, "FT of verse two.", 0);

			bldr.Clear();
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtNameGuidHot, bldr, 0, 0, Cache.DefaultAnalWs);
			StringUtils.InsertOrcIntoPara(footnote2, FwObjDataTypes.kodtNameGuidHot, bldr, 1, 1, Cache.DefaultAnalWs);
			ITsString expectedSegment1FT = bldr.GetString();

			CallFixSegmentsForPara();
			VerifyParaSegmentBreaks();
			ITsString emptyAnalFreeTrans = Cache.TsStrFactory.MakeString(String.Empty, Cache.DefaultAnalWs);
			VerifySegment(0, emptyAnalFreeTrans, null, new int[0]);
			VerifySegment(1, expectedSegment1FT, null, new int[0]);
			VerifySegment(2, emptyAnalFreeTrans, null, new int[0]);
			VerifySegment(3, Cache.TsStrFactory.MakeString("FT of verse two.", Cache.DefaultAnalWs),
				"FTx ofx versex two.", new int[0]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FixSegmentsForPara method when two ORCs occur between verse numbers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForPara_DualOrcsBetweenVerseNumbers()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Dual Footnotes:   |
			bldr.Replace(0, 0, "12", StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber, Cache.DefaultVernWs));
			Guid footnote1 = Guid.NewGuid();
			Guid footnote2 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 1, 1, Cache.DefaultVernWs);
			StringUtils.InsertOrcIntoPara(footnote2, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 2, 2, Cache.DefaultVernWs);
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0, 4, null, 0);

			CallFixSegmentsForPara();
			VerifyParaSegmentBreaks();
			ITsString emptyAnalFreeTrans = Cache.TsStrFactory.MakeString(String.Empty, Cache.DefaultAnalWs);
			VerifySegment(0);
			VerifySegment(1);
			VerifySegment(2);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the FixSegmentsForPara method doesn't change anything when an ORC is at
		/// the start of a paragraph, immediately followed by a verse number. Each sentence
		/// segment has a full translation (free translation, literal translation, and notes for
		/// the segments).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForPara_OrcBeforeVerseNumberAtStartOfPara()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Footnote here:   |
			bldr.Replace(0, 0, "4This is verse four.", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			bldr.SetStrPropValue(0, 1, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			Guid footnote1 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 0, 0, Cache.DefaultVernWs);
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0, 2, null, 0);
			AddSegmentToPara(m_para, 2, bldr.Length, "FT of verse 4.", 1);

			CallFixSegmentsForPara();
			VerifyParaSegmentBreaks();
			ITsString emptyAnalFreeTrans = Cache.TsStrFactory.MakeString(String.Empty, Cache.DefaultAnalWs);
			VerifySegment(0, emptyAnalFreeTrans, null, new int[0]);
			VerifySegment(1, Cache.TsStrFactory.MakeString("FT of verse 4.", Cache.DefaultAnalWs),
				"FTx ofx versex 4.", new int[] { 1 });
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the FixSegmentsForPara method doesn't change anything when there are two
		/// ORCs at the start of a paragraph, immediately followed by a verse number. Each
		/// sentence segment has a full translation (free translation, literal translation,
		/// and notes for the segments).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForPara_DualOrcsBeforeVerseNumberAtStartOfPara()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Dual Footnotes:  |
			bldr.Replace(0, 0, "4This is verse four.", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			bldr.SetStrPropValue(0, 1, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			Guid footnote1 = Guid.NewGuid();
			Guid footnote2 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 0, 0, Cache.DefaultVernWs);
			StringUtils.InsertOrcIntoPara(footnote2, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 1, 1, Cache.DefaultVernWs);
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0, 3, null, 0);
			AddSegmentToPara(m_para, 3, bldr.Length, "FT of verse 4.", 1);

			CallFixSegmentsForPara();
			VerifyParaSegmentBreaks();
			ITsString emptyAnalFreeTrans = Cache.TsStrFactory.MakeString(String.Empty, Cache.DefaultAnalWs);
			VerifySegment(0, emptyAnalFreeTrans, null, new int[0]);
			VerifySegment(1, Cache.TsStrFactory.MakeString("FT of verse 4.", Cache.DefaultAnalWs),
				"FTx ofx versex 4.", new int[] { 1 });
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the FixSegmentsForPara method doesn't change anything when there are two
		/// ORCs at the start of a paragraph, immediately followed by a verse number. Each
		/// sentence segment has a full translation (free translation, literal translation,
		/// and notes for the segments).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForPara_DualOrcsBeforeVerseNumberMidPara()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Dual Footnotes:                       |
			bldr.Replace(0, 0, "3This is verse three. 4This is verse four.", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			bldr.SetStrPropValue(0, 1, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			int ich = bldr.Text.IndexOf('.') + 1;
			bldr.SetStrPropValue(ich + 1, ich + 2, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			Guid footnote1 = Guid.NewGuid();
			Guid footnote2 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtOwnNameGuidHot, bldr, ich, ich, Cache.DefaultVernWs);
			StringUtils.InsertOrcIntoPara(footnote2, FwObjDataTypes.kodtOwnNameGuidHot, bldr, ich + 1, ich + 1, Cache.DefaultVernWs);
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0, 1, null, 0);
			AddSegmentToPara(m_para, 1, ich, "FT of verse 3.", 1);
			AddSegmentToPara(m_para, ich, ich + 4, null, 0);
			AddSegmentToPara(m_para, ich + 4, bldr.Length, "FT of verse 4.", 2);

			bldr.Clear();
			// Footnotes here:                |
			bldr.Replace(0, 0, "FT of verse 3.", StyleUtils.CharStyleTextProps(null, Cache.DefaultAnalWs));
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtNameGuidHot, bldr, bldr.Length, bldr.Length, Cache.DefaultAnalWs);
			StringUtils.InsertOrcIntoPara(footnote2, FwObjDataTypes.kodtNameGuidHot, bldr, bldr.Length, bldr.Length, Cache.DefaultAnalWs);
			ITsString expectedSegment1FT = bldr.GetString();

			CallFixSegmentsForPara();
			VerifyParaSegmentBreaks();
			ITsString emptyAnalFreeTrans = Cache.TsStrFactory.MakeString(String.Empty, Cache.DefaultAnalWs);
			VerifySegment(0, emptyAnalFreeTrans, null, new int[0]);
			VerifySegment(1, expectedSegment1FT, "FTx ofx versex 3.", new [] { 1 });
			VerifySegment(2, emptyAnalFreeTrans, null, new int[0]);
			VerifySegment(3, Cache.TsStrFactory.MakeString("FT of verse 4.", Cache.DefaultAnalWs),
				"FTx ofx versex 4.", new [] { 2 });
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FixSegmentsForPara method when an ORC is at the end of a paragraph,
		/// immediately following a verse number. Each sentence segment has a full translation
		/// (free translation, literal translation, and notes for the segments).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForPara_OrcFollowingVerseNumberAtEndOfPara()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Footnote goes here:                    |
			bldr.Replace(0, 0, "4This is verse four. 5", StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			bldr.SetStrPropValue(0, 1, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			bldr.SetStrPropValue(bldr.Length - 1, bldr.Length, (int)FwTextPropType.ktptNamedStyle, ScrStyleNames.VerseNumber);
			Guid footnote1 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtOwnNameGuidHot, bldr, bldr.Length, bldr.Length, Cache.DefaultVernWs);
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0, 1, null, 0);
			AddSegmentToPara(m_para, 1, bldr.Length - 2, "FT of verse 4.", 1);
			AddSegmentToPara(m_para, bldr.Length - 2, bldr.Length, null, 0);

			CallFixSegmentsForPara();
			VerifyParaSegmentBreaks();
			ITsString emptyAnalFreeTrans = Cache.TsStrFactory.MakeString(String.Empty, Cache.DefaultAnalWs);
			VerifySegment(0, emptyAnalFreeTrans, null, new int[0]);
			VerifySegment(1, Cache.TsStrFactory.MakeString("FT of verse 4.", Cache.DefaultAnalWs),
				"FTx ofx versex 4.", new int[] { 1 });
			VerifySegment(2, emptyAnalFreeTrans, null, new int[0]);
			VerifySegment(3, emptyAnalFreeTrans, null, new int[0]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FixSegmentsForPara method when an ORC is right before the end of a
		/// sentence and each sentence segment has a full translation (free translation,
		/// literal translation, and notes for the segments). No other label
		/// segments (verse/chapter numbers)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForPara_OrcRightBeforeEndOfSentenceWithFullTrans()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Footnotes go here:                   |
			bldr.Replace(0, 0, "This is sentence one. Sentence two.",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			Guid footnote1 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 20, 20, Cache.DefaultVernWs);
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0, 20, "FT of sentence1.", 1);
			AddSegmentToPara(m_para, 20, 21, null, 0);
			AddSegmentToPara(m_para, 21, bldr.Length, "FT of Sentence2.", 2);

			bldr.Clear();
			// Footnotes go here:               |
			bldr.Replace(0, 0, "FT of sentence1.", StyleUtils.CharStyleTextProps(null, Cache.DefaultAnalWs));
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtNameGuidHot, bldr, bldr.Length, bldr.Length, Cache.DefaultAnalWs);
			ITsString expectedSegment0FT = bldr.GetString();

			CallFixSegmentsForPara();
			VerifyParaSegmentBreaks();
			VerifySegment(0, expectedSegment0FT, "FTx ofx sentence1.", new int[] { 1 });
			VerifySegment(1, Cache.TsStrFactory.MakeString("FT of Sentence2.", Cache.DefaultAnalWs),
				"FTx ofx Sentence2.", new int[] { 2 });
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FixSegmentsForPara method when an ORC is right before the end of a
		/// sentence at the end of a paragraph. No other label segments (verse/chapter numbers)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForPara_OrcRightBeforePunctAtEndOfPara()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Footnotes go here:                   |
			bldr.Replace(0, 0, "This is sentence one.",
			StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			Guid footnote1 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 20, 20, Cache.DefaultVernWs);
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0, 20, "FT of sentence1.", 0);
			AddSegmentToPara(m_para, 20, 21, null, 0);
			AddSegmentToPara(m_para, 21, bldr.Length, "FT of period", 1);

			bldr.Clear();
			// Footnotes go here:               |
			bldr.Replace(0, 0, "FT of sentence1.FT of period", StyleUtils.CharStyleTextProps(null, Cache.DefaultAnalWs));
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtNameGuidHot, bldr, 16, 16, Cache.DefaultAnalWs);
			ITsString expectedSegment0FT = bldr.GetString();

			CallFixSegmentsForPara();
			VerifyParaSegmentBreaks();
			VerifySegment(0, expectedSegment0FT, "FTx ofx sentence1.FTx ofx period", new int[] { 1 });
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FixSegmentsForPara method when two ORCs are between two sentences
		/// or in the middle of a sentence and each sentence segment has a full translation
		/// (free translation, literal translation, and notes for the segments). No other label
		/// segments (verse/chapter numbers)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForPara_DualOrcsWithFullTrans()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Dual Footnotes here:    |             |              |
			bldr.Replace(0, 0, "This is sentence one. Sentence two. Sentence three.",
			StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			Guid footnote1 = Guid.NewGuid();
			Guid footnote2 = Guid.NewGuid();
			Guid footnote3 = Guid.NewGuid();
			Guid footnote4 = Guid.NewGuid();
			Guid footnote5 = Guid.NewGuid();
			Guid footnote6 = Guid.NewGuid();
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 7, 7, Cache.DefaultVernWs);
			StringUtils.InsertOrcIntoPara(footnote2, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 8, 8, Cache.DefaultVernWs);
			StringUtils.InsertOrcIntoPara(footnote3, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 23, 23, Cache.DefaultVernWs);
			StringUtils.InsertOrcIntoPara(footnote4, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 24, 24, Cache.DefaultVernWs);
			StringUtils.InsertOrcIntoPara(footnote5, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 40, 40, Cache.DefaultVernWs);
			StringUtils.InsertOrcIntoPara(footnote6, FwObjDataTypes.kodtOwnNameGuidHot, bldr, 41, 41, Cache.DefaultVernWs);
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0, 7, "FT of", 1);
			AddSegmentToPara(m_para, 7, 10, null, 0);
			AddSegmentToPara(m_para, 10, 23, "sentence1.", 2);
			AddSegmentToPara(m_para, 23, 26, null, 0);
			AddSegmentToPara(m_para, 26, 40, "FT of sentence2.", 0);
			AddSegmentToPara(m_para, 40, 42, null, 0);
			AddSegmentToPara(m_para, 42, bldr.Length, "FT of sentence 3.", 0);

			bldr.Clear();
			// Dual FN here:         |          |
			bldr.Replace(0, 0, "FT of sentence1.", StyleUtils.CharStyleTextProps(null, Cache.DefaultAnalWs));
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtNameGuidHot, bldr, 5, 5, Cache.DefaultAnalWs);
			StringUtils.InsertOrcIntoPara(footnote2, FwObjDataTypes.kodtNameGuidHot, bldr, 6, 6, Cache.DefaultAnalWs);
			StringUtils.InsertOrcIntoPara(footnote3, FwObjDataTypes.kodtNameGuidHot, bldr, bldr.Length, bldr.Length, Cache.DefaultAnalWs);
			StringUtils.InsertOrcIntoPara(footnote4, FwObjDataTypes.kodtNameGuidHot, bldr, bldr.Length, bldr.Length, Cache.DefaultAnalWs);
			ITsString expectedSegment0FT = bldr.GetString();

			bldr.Clear();
			// Dual FN here:    |
			bldr.Replace(0, 0, "FT of sentence 3.", StyleUtils.CharStyleTextProps(null, Cache.DefaultAnalWs));
			StringUtils.InsertOrcIntoPara(footnote5, FwObjDataTypes.kodtNameGuidHot, bldr, 0, 0, Cache.DefaultAnalWs);
			StringUtils.InsertOrcIntoPara(footnote6, FwObjDataTypes.kodtNameGuidHot, bldr, 1, 1, Cache.DefaultAnalWs);
			ITsString expectedSegment2FT = bldr.GetString();

			CallFixSegmentsForPara();
			VerifyParaSegmentBreaks();
			VerifySegment(0, expectedSegment0FT, "FTx of sentence1.", new int[] { 1, 2 });
			VerifySegment(1, Cache.TsStrFactory.MakeString("FT of sentence2.", Cache.DefaultAnalWs),
				"FTx ofx sentence2.", new int[0]);
			VerifySegment(2, expectedSegment2FT, "FTx ofx sentencex 3.", new int[0]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FixSegmentsForPara method when two ORCs are between two sentences with a
		/// nice, fluffy space in between them. Both sentences' segments have a full translation
		/// (free translation, literal translation, and notes for the segments). No other label
		/// segments (verse/chapter numbers)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForPara_OrcsBetweenSentencesWithSpaces()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Footnotes here:                       ||
			bldr.Replace(0, 0, "This is sentence one. Sentence two.",
			StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			Guid footnote1 = Guid.NewGuid();
			Guid footnote2 = Guid.NewGuid();
			int ich1 = bldr.Text.IndexOf('.') + 1;
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtOwnNameGuidHot, bldr, ich1, ich1, Cache.DefaultVernWs);
			int ich2 = ich1 + 2; // skip past the first footnote ORC and the space
			StringUtils.InsertOrcIntoPara(footnote2, FwObjDataTypes.kodtOwnNameGuidHot, bldr, ich2, ich2, Cache.DefaultVernWs);
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0, ich1, "FT of sentence 1.", 1);
			AddSegmentToPara(m_para, ich1, ich2 + 1, null, 0);
			AddSegmentToPara(m_para, ich2 + 1, bldr.Length, "FT of sentence 2.", 0);

			bldr.Clear();
			// Footnote here:                    |
			bldr.Replace(0, 0, "FT of sentence 1.", StyleUtils.CharStyleTextProps(null, Cache.DefaultAnalWs));
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtNameGuidHot, bldr, bldr.Length, bldr.Length, Cache.DefaultAnalWs);
			ITsString expectedSegment0FT = bldr.GetString();

			bldr.Clear();
			// Footnote here:   |
			bldr.Replace(0, 0, "FT of sentence 2.", StyleUtils.CharStyleTextProps(null, Cache.DefaultAnalWs));
			StringUtils.InsertOrcIntoPara(footnote2, FwObjDataTypes.kodtNameGuidHot, bldr, 0, 0, Cache.DefaultAnalWs);
			ITsString expectedSegment1FT = bldr.GetString();

			CallFixSegmentsForPara();
			VerifyParaSegmentBreaks();
			VerifySegment(0, expectedSegment0FT, "FTx ofx sentencex 1.", new [] { 1 });
			VerifySegment(1, expectedSegment1FT, "FTx ofx sentencex 2.", new int[0]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FixSegmentsForPara method when an ORC is between two sentences with a
		/// nice, fluffy space on either side of it. Both sentences' segments have a full translation
		/// (free translation, literal translation, and notes for the segments). No other label
		/// segments (verse/chapter numbers)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForPara_OrcSurroundedBySpaces()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Footnote here:                         |
			bldr.Replace(0, 0, "This is sentence one.  Sentence two.",
			StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			Guid footnote = Guid.NewGuid();
			int ich = bldr.Text.IndexOf('.') + 2;
			StringUtils.InsertOrcIntoPara(footnote, FwObjDataTypes.kodtOwnNameGuidHot, bldr, ich, ich, Cache.DefaultVernWs);
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0, ich, "FT of sentence 1.", 1);
			AddSegmentToPara(m_para, ich, ich + 2, null, 0);
			AddSegmentToPara(m_para, ich + 2, bldr.Length, "FT of sentence 2.", 0);

			bldr.Clear();
			// Footnote here:   |
			bldr.Replace(0, 0, " FT of sentence 2.", StyleUtils.CharStyleTextProps(null, Cache.DefaultAnalWs));
			StringUtils.InsertOrcIntoPara(footnote, FwObjDataTypes.kodtNameGuidHot, bldr, 0, 0, Cache.DefaultAnalWs);
			ITsString expectedSegment1FT = bldr.GetString();

			CallFixSegmentsForPara();
			VerifyParaSegmentBreaks();
			VerifySegment(0, Cache.TsStrFactory.MakeString("FT of sentence 1.", Cache.DefaultAnalWs),
				"FTx ofx sentencex 1.", new[] { 1 });
			VerifySegment(1, expectedSegment1FT, "FTx ofx sentencex 2.", new int[0]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FixSegmentsForPara method when two ORCs are between two sentences with a
		/// nice, fluffy space in between them. Both sentences' segments have a full translation
		/// (free translation, literal translation, and notes for the segments). No other label
		/// segments (verse/chapter numbers)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FixSegmentsForPara_OrcBetweenSentencesWithNoSpace()
		{
			ITsStrBldr bldr = TsStrBldrClass.Create();
			// Footnotes here:                       |
			bldr.Replace(0, 0, "This is sentence one.Sentence two.",
			StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			Guid footnote1 = Guid.NewGuid();
			int ich = bldr.Text.IndexOf('.') + 1;
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtOwnNameGuidHot, bldr, ich, ich, Cache.DefaultVernWs);
			m_para.Contents = bldr.GetString();

			AddSegmentToPara(m_para, 0, ich, "FT of sentence 1.", 1);
			AddSegmentToPara(m_para, ich, ich + 1, null, 0);
			AddSegmentToPara(m_para, ich + 1, bldr.Length, "FT of sentence 2.", 0);

			bldr.Clear();
			// Footnote here:                    |
			bldr.Replace(0, 0, "FT of sentence 1.", StyleUtils.CharStyleTextProps(null, Cache.DefaultAnalWs));
			StringUtils.InsertOrcIntoPara(footnote1, FwObjDataTypes.kodtNameGuidHot, bldr, bldr.Length, bldr.Length, Cache.DefaultAnalWs);
			ITsString expectedSegment0FT = bldr.GetString();

			CallFixSegmentsForPara();
			VerifyParaSegmentBreaks();
			VerifySegment(0, expectedSegment0FT, "FTx ofx sentencex 1.", new[] { 1 });
			VerifySegment(1, Cache.TsStrFactory.MakeString("FT of sentence 2.", Cache.DefaultAnalWs),
				"FTx ofx sentencex 2.", new int[0]);
		}
		#endregion

		#region Helper methods

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calls the FixSegmentsForPara method on m_para after doing a sanity check on the test
		/// data to confirm that the segmenting is valid for FW 6.0.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CallFixSegmentsForPara()
		{
			Assert.IsTrue(ReflectionHelper.GetBoolResult(typeof(DataStoreInitializationServices),
				"ParaSegmentsValid", m_para, false, true), "Setup of test is probably wrong.");
			ReflectionHelper.CallMethod(typeof(DataStoreInitializationServices), "FixSegmentsForPara", m_para);
		}

		private static void VerifySegmentAnalyses(ISegment segment, List<IAnalysis> analyses)
		{
			Assert.That(segment.AnalysesRS.Count, Is.EqualTo(analyses.Count));
			for (int i = 0; i < analyses.Count; i++)
				Assert.That(segment.AnalysesRS[i], Is.EqualTo(analyses[i]));
		}

		private void VerifyParaSegmentBreaks()
		{
			VerifyParaSegmentBreaks(m_para);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies the resulting segment breaks of the mocked paragraph by creating and
		/// segmenting a real paragraph and checking to make sure the segments align correctly.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void VerifyParaSegmentBreaks(IScrTxtPara checkPara)
		{
			IScrBook book = AddBookToMockedScripture(1, "Genesis");
			IScrSection section = AddSectionToMockedBook(book);
			IScrTxtPara realPara = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			realPara.Contents = checkPara.Contents; // Should segment the real paragraph here

			Assert.AreEqual(realPara.SegmentsOS.Count, checkPara.SegmentsOS.Count, "Wrong number of segments in mocked paragraph");
			for (int iSeg = 0; iSeg < realPara.SegmentsOS.Count; iSeg++)
			{
				ISegment realSegment = realPara.SegmentsOS[iSeg];
				ISegment mockSegment = checkPara.SegmentsOS[iSeg];
				Assert.AreEqual(realSegment.BeginOffset, mockSegment.BeginOffset, "Segment " + iSeg + " has the wrong BeginOffset");
				Assert.AreEqual(realSegment.EndOffset, mockSegment.EndOffset, "Segment " + iSeg + " has the wrong EndOffset");
				AssertEx.AreTsStringsEqual(realSegment.BaselineText, mockSegment.BaselineText, "Segment " + iSeg + " has the wrong BaselineText");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a new segment to the specified paragraph, generating analyses for every word
		/// and punctuation form.
		/// </summary>
		/// <param name="para">The paragraph.</param>
		/// <param name="ichBeginOffset">The character offset for the beginning of the segment
		/// in the paragraph contents.</param>
		/// <param name="ichEndOffset">The character offset for the end of the segment
		/// in the paragraph contents.</param>
		/// <param name="freeTrans">The text of the segment's free translation in the default
		/// analysis writing system. (In a slightly modified form, this text is also used to mock
		/// up a sweet literal translation.)</param>
		/// <param name="cNotes">The count of notes to generate for the segment.</param>
		/// ------------------------------------------------------------------------------------
		private void AddSegmentToPara(IScrTxtPara para, int ichBeginOffset, int ichEndOffset,
			string freeTrans, int cNotes)
		{
			AddSegmentToPara(para, ichBeginOffset, ichEndOffset, freeTrans, cNotes, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a new segment to the specified paragraph, generating analyses for every word
		/// and punctuation form.
		/// </summary>
		/// <param name="para">The paragraph.</param>
		/// <param name="ichBeginOffset">The character offset for the beginning of the segment
		/// in the paragraph contents.</param>
		/// <param name="ichEndOffset">The character offset for the end of the segment
		/// in the paragraph contents.</param>
		/// <param name="freeTrans">The text of the segment's free translation in the default
		/// analysis writing system. (In a slightly modified form, this text is also used to mock
		/// up a sweet literal translation.)</param>
		/// <param name="cNotes">The count of notes to generate for the segment.</param>
		/// <param name="createAnalyses">if set to <c>true</c> create analyses.</param>
		/// ------------------------------------------------------------------------------------
		private void AddSegmentToPara(IScrTxtPara para, int ichBeginOffset, int ichEndOffset,
			string freeTrans, int cNotes, bool createAnalyses)
		{
			ISegment segment = AddSegmentToPara(para, ichBeginOffset);
			if (freeTrans != null)
			{
				segment.FreeTranslation.SetAnalysisDefaultWritingSystem(freeTrans);
				segment.LiteralTranslation.SetAnalysisDefaultWritingSystem(freeTrans.Replace(" ", "x "));
			}

			// Can't use the actual segment's baseline because the offset won't be right until
			// the next segment has been added.
			if (createAnalyses)
				FdoTestHelper.CreateAnalyses(segment, para.Contents, ichBeginOffset, ichEndOffset);

			if (!SegmentBreaker.HasLabelText(para.Contents, ichBeginOffset, ichEndOffset))
			{
				// Create Notes
				INoteFactory noteFactory = Cache.ServiceLocator.GetInstance<INoteFactory>();
				for (int iNote = 0; iNote < cNotes; iNote++)
				{
					INote note = noteFactory.Create();
					segment.NotesOS.Add(note);
					note.Content.SetAnalysisDefaultWritingSystem("Anal Note " + iNote);
					note.Content.SetVernacularDefaultWritingSystem("Vern Note " + iNote);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a new segment to the specified paragraph.
		/// </summary>
		/// <param name="para">The paragraph.</param>
		/// <param name="ichBeginOffset">The character offset for the beginning of the segment
		/// in the paragraph contents.</param>
		/// ------------------------------------------------------------------------------------
		private ISegment AddSegmentToPara(IScrTxtPara para, int ichBeginOffset)
		{
			return ((SegmentFactory)m_para.Services.GetInstance<ISegmentFactory>()).Create(para, ichBeginOffset);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks that the given segment has no free/literal translations, notes or analyses.
		/// </summary>
		/// <param name="iSeg">The index of the segment to check.</param>
		/// ------------------------------------------------------------------------------------
		private void VerifySegment(int iSeg)
		{
			ISegment seg = m_para.SegmentsOS[iSeg];
			Assert.AreEqual(0, seg.AnalysesRS.Count);
			Assert.AreEqual(0, seg.FreeTranslation.StringCount);
			Assert.AreEqual(0, seg.LiteralTranslation.StringCount);
			Assert.AreEqual(0, seg.NotesOS.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks that the begin and end offsets of the given segment.
		/// </summary>
		/// <param name="iSeg">The index of the segment to check.</param>
		/// <param name="beginOffset">The expected begin offset.</param>
		/// <param name="endOffset">The expected end offset.</param>
		/// <param name="strTrans">The expected free translation in the segment.</param>
		/// ------------------------------------------------------------------------------------
		private void VerifySegment(int iSeg, int beginOffset, int endOffset, string strTrans)
		{
			ISegment seg = m_para.SegmentsOS[iSeg];
			Assert.AreEqual(beginOffset, seg.BeginOffset);
			Assert.AreEqual(endOffset, seg.EndOffset);
			Assert.AreEqual(strTrans, seg.FreeTranslation.get_String(Cache.DefaultAnalWs).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks that the given segment has the expected free and literal translations, notes
		/// and analyses.
		/// </summary>
		/// <param name="iSeg">The index of the segment to check.</param>
		/// <param name="freeTrans">The expected free translation of the segment.</param>
		/// <param name="literalTrans">The expected text of the literal translation of the
		/// segment.</param>
		/// <param name="expectedNotes">The expected notes for this segment.</param>
		/// ------------------------------------------------------------------------------------
		private void VerifySegment(int iSeg, ITsString freeTrans, string literalTrans,
			IEnumerable<int> expectedNotes)
		{
			VerifySegment(iSeg, freeTrans, literalTrans, expectedNotes, new int[0]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks that the given segment has the expected free and literal translations, notes
		/// and analyses.
		/// </summary>
		/// <param name="iSeg">The index of the segment to check.</param>
		/// <param name="freeTrans">The expected free translation of the segment.</param>
		/// <param name="literalTrans">The expected text of the literal translation of the
		/// segment.</param>
		/// <param name="expectedNotes">The expected notes for this segment.</param>
		/// <param name="expectedFormsWithoutAnalyses">List of indices of wordforms which are
		/// not expected to have any analyses.</param>
		/// ------------------------------------------------------------------------------------
		private void VerifySegment(int iSeg, ITsString freeTrans, string literalTrans,
			IEnumerable<int> expectedNotes, IEnumerable<int> expectedFormsWithoutAnalyses)
		{
			VerifySegment(iSeg, freeTrans, literalTrans, expectedNotes,
				expectedFormsWithoutAnalyses, new int[0]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks that the given segment has the expected free and literal translations, notes
		/// and analyses.
		/// </summary>
		/// <param name="iSeg">The index of the segment to check.</param>
		/// <param name="freeTrans">The expected free translation of the segment.</param>
		/// <param name="literalTrans">The expected text of the literal translation of the
		/// segment.</param>
		/// <param name="expectedNotes">The expected notes for this segment.</param>
		/// <param name="expectedFormsWithoutAnalyses">List of indices of wordforms which are
		/// not expected to have any analyses.</param>
		/// <param name="expectedWordforms">List of indices of analyses that are expected to be
		/// word forms, rather than glosses (these should correspond to whole words that did not
		/// survive the resegmentation)</param>
		/// ------------------------------------------------------------------------------------
		private void VerifySegment(int iSeg, ITsString freeTrans, string literalTrans,
			IEnumerable<int> expectedNotes, IEnumerable<int> expectedFormsWithoutAnalyses,
			IEnumerable<int> expectedWordforms)
		{
			VerifySegment(iSeg, freeTrans, literalTrans, expectedNotes,
				expectedFormsWithoutAnalyses, expectedWordforms, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks that the given segment has the expected free and literal translations, notes
		/// and analyses.
		/// </summary>
		/// <param name="iSeg">The index of the segment to check.</param>
		/// <param name="freeTrans">The expected free translation of the segment.</param>
		/// <param name="literalTrans">The expected text of the literal translation of the
		/// segment.</param>
		/// <param name="expectedNotes">The expected notes for this segment.</param>
		/// <param name="expectedFormsWithoutAnalyses">List of indices of wordforms which are
		/// not expected to have any analyses.</param>
		/// <param name="expectedWordforms">List of indices of analyses that are expected to be
		/// word forms, rather than glosses (these should correspond to whole words that did not
		/// survive the resegmentation)</param>
		/// <param name="verifyAnalyses">if set to <c>true</c> verify analyses.</param>
		/// ------------------------------------------------------------------------------------
		private void VerifySegment(int iSeg, ITsString freeTrans, string literalTrans,
			IEnumerable<int> expectedNotes, IEnumerable<int> expectedFormsWithoutAnalyses,
			IEnumerable<int> expectedWordforms, bool verifyAnalyses)
		{
			ISegment seg = m_para.SegmentsOS[iSeg];
			AssertEx.AreTsStringsEqual(freeTrans, seg.FreeTranslation.AnalysisDefaultWritingSystem);
			Assert.AreEqual(literalTrans, seg.LiteralTranslation.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual(expectedNotes.Sum(), seg.NotesOS.Count);
			int iNote = 0;
			foreach (int cNotes in expectedNotes)
			{
				for (int i = 0; i < cNotes; i++)
				{
					INote note = seg.NotesOS[iNote++];
					Assert.AreEqual("Anal Note " + i, note.Content.AnalysisDefaultWritingSystem.Text);
					Assert.AreEqual("Vern Note " + i, note.Content.VernacularDefaultWritingSystem.Text);
				}
			}

			if (verifyAnalyses)
				FdoTestHelper.VerifyAnalysis(seg, iSeg, expectedFormsWithoutAnalyses, expectedWordforms);
		}

		#endregion
	}
	#endregion
}

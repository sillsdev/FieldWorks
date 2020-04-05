// Copyright (c) 2016-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using LanguageExplorer;
using Microsoft.Practices.ServiceLocation;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.TestUtilities;
using SIL.Xml;

namespace LanguageExplorerTests
{
	/// <summary>
	/// Unit tests for MiscExtensions.
	/// </summary>
	[TestFixture]
	public sealed class MiscExtensionsTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private ICmPossibilityList m_testList;
		private int m_userWs;

		protected override void CreateTestData()
		{
			base.CreateTestData();
			var servLoc = Cache.ServiceLocator;
			m_userWs = Cache.DefaultUserWs;
			const string name = "Test Custom List";
			const string description = "Test Custom list description";
			var listName = TsStringUtils.MakeString(name, m_userWs);
			var listDesc = TsStringUtils.MakeString(description, m_userWs);
			m_testList = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().CreateUnowned(listName.Text, m_userWs);
			m_testList.Name.set_String(m_userWs, listName);
			m_testList.Description.set_String(m_userWs, listDesc);
			// Set various properties of CmPossibilityList
			m_testList.DisplayOption = (int)PossNameType.kpntNameAndAbbrev;
			m_testList.PreventDuplicates = true;
			m_testList.IsSorted = true;
			m_testList.WsSelector = WritingSystemServices.kwsAnals;
			m_testList.IsVernacular = false;
			m_testList.Depth = 127;
		}
		/// <summary>
		/// SetConfigurationDisplayPropertyIfNeeded should set a displayProperty attribute in the configuration node.
		/// </summary>
		[Test]
		public void SetConfigurationDisplayPropertyIfNeeded_Works()
		{
			var configurationNode = TestUtilities.CreateXmlElementFromOuterXmlOf("<slice editor=\"autoCustom\" menu=\"mnuDataTree-Help\" helpTopicID=\"khtpCustomFields\" />");
			ICmObject cmObject = new CmObjectStub();
			const int cmObjectCustomFieldFlid = 5002500;
			ISilDataAccess mainCacheAccessor = new SilDataAccessStub();
			IFwMetaDataCache metadataCache = new FwMetaDataCacheStub(Cache.GetManagedMetaDataCache());

			CreateTestData();
			ICmPossibility cmPossibility = Cache.ServiceLocator.GetInstance<ICmCustomItemFactory>().Create(); //CreateCustomItemAddToList(m_testList, "itemname");
			m_testList.PossibilitiesOS.Add(cmPossibility);
			cmPossibility.Name.set_String(m_userWs, TsStringUtils.MakeString("itemname", m_userWs));
			ILcmServiceLocator lcmServiceLocator = new LcmServiceLocatorStub(cmPossibility);

			// SUT
			configurationNode.SetConfigurationDisplayPropertyIfNeeded(cmObject, cmObjectCustomFieldFlid, mainCacheAccessor, lcmServiceLocator, metadataCache);

			AssertThatXmlIn.String(configurationNode.GetOuterXml()).HasSpecifiedNumberOfMatchesForXpath("/slice/deParams[@displayProperty]", 1);
		}

		private sealed class LcmServiceLocatorStub : ILcmServiceLocator
		{
			readonly ICmPossibility _returnObject;
			internal LcmServiceLocatorStub(ICmPossibility returnObject)
			{
				_returnObject = returnObject;
			}

			IActionHandler ILcmServiceLocator.ActionHandler => throw new NotSupportedException();
			ICmObjectIdFactory ILcmServiceLocator.CmObjectIdFactory => throw new NotSupportedException();
			IDataSetup ILcmServiceLocator.DataSetup => throw new NotSupportedException();
			ICmObject ILcmServiceLocator.GetObject(int hvo) => _returnObject;
			bool ILcmServiceLocator.IsValidObjectId(int hvo) { throw new NotSupportedException(); }
			ICmObject ILcmServiceLocator.GetObject(Guid guid) { throw new NotSupportedException(); }
			ICmObject ILcmServiceLocator.GetObject(ICmObjectId id) { throw new NotSupportedException(); }
			WritingSystemManager ILcmServiceLocator.WritingSystemManager => throw new NotSupportedException();
			IWritingSystemContainer ILcmServiceLocator.WritingSystems => throw new NotSupportedException();
			ICmObjectRepository ILcmServiceLocator.ObjectRepository => throw new NotSupportedException();
			ICmObjectIdFactory ILcmServiceLocator.ObjectIdFactory => throw new NotSupportedException();
			IFwMetaDataCacheManaged ILcmServiceLocator.MetaDataCache => throw new NotSupportedException();
			ILgWritingSystemFactory ILcmServiceLocator.WritingSystemFactory => throw new NotSupportedException();
			IEnumerable<object> IServiceLocator.GetAllInstances(Type serviceType) => throw new NotSupportedException();
			IEnumerable<TService> IServiceLocator.GetAllInstances<TService>() => throw new NotSupportedException();
			object IServiceLocator.GetInstance(Type serviceType) => throw new NotSupportedException();
			object IServiceLocator.GetInstance(Type serviceType, string key) => throw new NotSupportedException();
			TService IServiceLocator.GetInstance<TService>() => throw new NotSupportedException();
			TService IServiceLocator.GetInstance<TService>(string key) => throw new NotSupportedException();
			object IServiceProvider.GetService(Type serviceType) => throw new NotSupportedException();
		}

		private sealed class SilDataAccessStub : SilDataAccessManagedBase
		{
			public override int get_VecSize(int hvo, int tag)
			{
				return 1;
			}

			public override int get_VecItem(int hvo, int tag, int index)
			{
				return 1234;
			}

		}

		private sealed class FwMetaDataCacheStub : LcmMetaDataCacheDecoratorBase
		{
			public FwMetaDataCacheStub(IFwMetaDataCacheManaged metaDataCache) : base(metaDataCache)
			{
			}

			public override void AddVirtualProp(string bstrClass, string bstrField, int luFlid, int type)
			{
				throw new NotSupportedException();
			}

			public override int GetFieldType(int flid)
			{
				return (int)CellarPropertyType.ReferenceSequence;
			}
		}

		private sealed class CmObjectStub : ICmObject
		{
			internal CmObjectStub()
			{
				Hvo = 1234;
				IsValidObject = true;
				Guid = Guid.NewGuid();
			}

			public IEnumerable<ICmObject> AllOwnedObjects { get; private set; }
			public int Hvo { get; }
			public ICmObject Owner { get; set; }
			public int OwningFlid => throw new NotSupportedException();

			public int OwnOrd => throw new NotSupportedException();

			public int ClassID => throw new NotSupportedException();
			public Guid Guid { get; }

			public ICmObjectId Id => throw new NotSupportedException();

			public ICmObject GetObject(ICmObjectRepository repo)
			{
				throw new NotSupportedException();
			}

			public string ToXmlString()
			{
				throw new NotSupportedException();
			}

			public string ClassName => throw new NotSupportedException();

			public void Delete()
			{
				throw new NotSupportedException();
			}

			public ILcmServiceLocator Services => throw new NotSupportedException();

			public ICmObject OwnerOfClass(int clsid)
			{
				throw new NotSupportedException();
			}

			public T OwnerOfClass<T>() where T : ICmObject
			{
				throw new NotSupportedException();
			}

			public ICmObject Self => throw new NotSupportedException();

			public bool CheckConstraints(int flidToCheck, bool createAnnotation, out ConstraintFailure failure)
			{
				throw new NotSupportedException();
			}

			public void PostClone(Dictionary<int, ICmObject> copyMap)
			{
				throw new NotImplementedException();
			}

			public void AllReferencedObjects(List<ICmObject> collector)
			{
				throw new NotSupportedException();
			}

			public bool IsFieldRelevant(int flid, HashSet<Tuple<int, int>> propsToMonitor)
			{
				throw new NotSupportedException();
			}

			public bool IsOwnedBy(ICmObject possibleOwner)
			{
				throw new NotSupportedException();
			}

			public ICmObject ReferenceTargetOwner(int flid)
			{
				throw new NotSupportedException();
			}

			public bool IsFieldRequired(int flid)
			{
				throw new NotSupportedException();
			}

			public int IndexInOwner => throw new NotSupportedException();

			public IEnumerable<ICmObject> ReferenceTargetCandidates(int flid)
			{
				throw new NotSupportedException();
			}

			public bool IsValidObject { get; }

			public LcmCache Cache => throw new NotSupportedException();

			public void MergeObject(ICmObject objSrc)
			{
				throw new NotSupportedException();
			}

			public void MergeObject(ICmObject objSrc, bool fLoseNoStringData)
			{
				throw new NotSupportedException();
			}

			public bool CanDelete => throw new NotSupportedException();

			public ITsString ObjectIdName => throw new NotSupportedException();

			public string ShortName => throw new NotSupportedException();

			public ITsString ShortNameTSS => throw new NotSupportedException();

			public ITsString DeletionTextTSS => throw new NotSupportedException();

			public ITsString ChooserNameTS => throw new NotSupportedException();

			public string SortKey => throw new NotSupportedException();

			public string SortKeyWs => throw new NotSupportedException();

			public int SortKey2 => throw new NotSupportedException();

			public string SortKey2Alpha => throw new NotSupportedException();

			public HashSet<ICmObject> ReferringObjects => throw new NotSupportedException();

			public IEnumerable<ICmObject> OwnedObjects { get; private set; }
		}
	}
}
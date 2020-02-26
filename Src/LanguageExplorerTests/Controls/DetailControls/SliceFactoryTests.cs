// Copyright (c) 2016-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using LanguageExplorer.Controls.DetailControls;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.TestUtilities;
using SIL.Xml;

namespace LanguageExplorerTests.Controls.DetailControls
{
	/// <summary>
	/// Unit tests for SliceFactory. Subclasses DeleteCustomListTestsBase, which has some helper methods that we are using.
	/// </summary>
	[TestFixture]
	public class SliceFactoryTests : DeleteCustomListTestsBase
	{
		/// <summary>
		/// SetConfigurationDisplayPropertyIfNeeded should set a displayProperty attribute in the configuration node.
		/// </summary>
		[Test]
		public void SetConfigurationDisplayPropertyIfNeeded_Works()
		{
			var configurationNode = SliceTests.CreateXmlElementFromOuterXmlOf("<slice editor=\"autoCustom\" menu=\"mnuDataTree-Help\" helpTopicID=\"khtpCustomFields\" />");
			ICmObject cmObject = new CmObjectStub();
			const int cmObjectCustomFieldFlid = 5002500;
			ISilDataAccess mainCacheAccessor = new SilDataAccessStub();
			IFwMetaDataCache metadataCache = new FwMetaDataCacheStub(Cache.GetManagedMetaDataCache());

			CreateTestData();
			ICmPossibility cmPossibility = CreateCustomItemAddToList(m_testList, "itemname");
			ILcmServiceLocator lcmServiceLocator = new LcmServiceLocatorStub(cmPossibility);

			// SUT
			SliceFactory.SetConfigurationDisplayPropertyIfNeeded(configurationNode, cmObject, cmObjectCustomFieldFlid, mainCacheAccessor, lcmServiceLocator, metadataCache);

			AssertThatXmlIn.String(configurationNode.GetOuterXml()).HasSpecifiedNumberOfMatchesForXpath("/slice/deParams[@displayProperty]", 1);
		}

		private sealed class LcmServiceLocatorStub : ILcmServiceLocator
		{
			ICmPossibility m_returnObject;
			public LcmServiceLocatorStub(ICmPossibility returnObject)
			{
				m_returnObject = returnObject;
			}
			public IActionHandler ActionHandler => throw new NotSupportedException();
			public ICmObjectIdFactory CmObjectIdFactory => throw new NotSupportedException();
			public IDataSetup DataSetup => throw new NotSupportedException();

			public ICmObject GetObject(int hvo)
			{
				return m_returnObject;
			}
			public bool IsValidObjectId(int hvo) { throw new NotSupportedException(); }
			public ICmObject GetObject(Guid guid) { throw new NotSupportedException(); }
			public ICmObject GetObject(ICmObjectId id) { throw new NotSupportedException(); }
			public WritingSystemManager WritingSystemManager => throw new NotSupportedException();
			public IWritingSystemContainer WritingSystems => throw new NotSupportedException();
			public ICmObjectRepository ObjectRepository => throw new NotSupportedException();
			public ICmObjectIdFactory ObjectIdFactory => throw new NotSupportedException();
			public IFwMetaDataCacheManaged MetaDataCache => throw new NotSupportedException();
			public ILgWritingSystemFactory WritingSystemFactory => throw new NotSupportedException();
			public IEnumerable<object> GetAllInstances(Type serviceType)
			{ throw new NotSupportedException(); }
			public IEnumerable<TService> GetAllInstances<TService>()
			{ throw new NotSupportedException(); }
			public object GetInstance(Type serviceType)
			{ throw new NotSupportedException(); }
			public object GetInstance(Type serviceType, string key)
			{ throw new NotSupportedException(); }
			public TService GetInstance<TService>()
			{ throw new NotSupportedException(); }
			public TService GetInstance<TService>(string key)
			{ throw new NotSupportedException(); }
			public object GetService(Type serviceType)
			{ throw new NotSupportedException(); }
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
// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// Original author: MarkS 2016-07-27 SliceFactoryTests.cs

using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.Infrastructure;
using System;
using SIL.TestUtilities;
using SIL.CoreImpl;
using System.Collections.Generic;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.XWorks;

namespace SIL.FieldWorks.Common.Framework.DetailControls
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
			XmlNode configurationNode = DetailControls.SliceTests.CreateXmlElementFromOuterXmlOf("<slice editor=\"autoCustom\" menu=\"mnuDataTree-Help\" helpTopicID=\"khtpCustomFields\" />");
			ICmObject cmObject = new CmObjectStub();
			int cmObjectCustomFieldFlid = 5002500;
			ISilDataAccess mainCacheAccessor = new SILDataAccessStub();
			IFwMetaDataCache metadataCache=new FwMetaDataCacheStub((IFwMetaDataCacheManaged)Cache.MetaDataCacheAccessor);

			CreateTestData();
			ICmPossibility cmPossibility = CreateCustomItemAddToList(m_testList, "itemname");
			IFdoServiceLocator fdoServiceLocator = new FdoServiceLocatorStub(cmPossibility);

			// SUT
			SliceFactory.SetConfigurationDisplayPropertyIfNeeded(configurationNode, cmObject, cmObjectCustomFieldFlid, mainCacheAccessor, fdoServiceLocator, metadataCache);

			AssertThatXmlIn.String(configurationNode.OuterXml).HasSpecifiedNumberOfMatchesForXpath("/slice/deParams[@displayProperty]", 1);
		}

		class FdoServiceLocatorStub : IFdoServiceLocator
		{
			ICmPossibility m_returnObject;
			public FdoServiceLocatorStub(ICmPossibility returnObject)
			{
				m_returnObject = returnObject;
			}
			public IActionHandler ActionHandler { get{throw new NotImplementedException();} }
			public ICmObjectIdFactory CmObjectIdFactory { get{throw new NotImplementedException();} }
			public IDataSetup DataSetup { get{throw new NotImplementedException();} }
			public ICmObject GetObject(int hvo)
			{
				return m_returnObject;
			}
			public bool IsValidObjectId(int hvo) {throw new NotImplementedException();}
			public ICmObject GetObject(Guid guid) {throw new NotImplementedException();}
			public ICmObject GetObject(ICmObjectId id) {throw new NotImplementedException();}
			public WritingSystemManager WritingSystemManager { get { throw new NotImplementedException(); } }
			public IWritingSystemContainer WritingSystems { get{throw new NotImplementedException();} }
			public ICmObjectRepository ObjectRepository { get{throw new NotImplementedException();} }
			public ICmObjectIdFactory ObjectIdFactory { get{throw new NotImplementedException();} }
			public IFwMetaDataCacheManaged MetaDataCache  { get{throw new NotImplementedException();} }
			public ILgWritingSystemFactory WritingSystemFactory { get{throw new NotImplementedException();} }
			public ILgCharacterPropertyEngine UnicodeCharProps { get{throw new NotImplementedException();} }
			public IEnumerable<object> GetAllInstances(Type serviceType)
			{throw new NotImplementedException();}
			public IEnumerable<TService> GetAllInstances<TService>()
			{throw new NotImplementedException();}
			public object GetInstance(Type serviceType)
			{throw new NotImplementedException();}
			public object GetInstance(Type serviceType, string key)
			{throw new NotImplementedException();}
			public TService GetInstance<TService>()
			{throw new NotImplementedException();}
			public TService GetInstance<TService>(string key)
			{throw new NotImplementedException();}
			public object GetService(Type serviceType)
			{throw new NotImplementedException();}
		}

		class SILDataAccessStub : SilDataAccessManagedBase
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

		class FwMetaDataCacheStub : FdoMetaDataCacheDecoratorBase
		{
			public FwMetaDataCacheStub(IFwMetaDataCacheManaged metaDataCache) : base(metaDataCache)
			{
			}

			public override void AddVirtualProp(string bstrClass, string bstrField, int luFlid, int type)
			{
				throw new NotImplementedException();
			}

			public override int GetFieldType(int flid)
			{
				return (int)CellarPropertyType.ReferenceSequence;
			}
		}

		internal class CmObjectStub : ICmObject
		{
			internal CmObjectStub()
			{
				Hvo = 1234;
				IsValidObject = true;
				Guid = Guid.NewGuid();
			}

			public IEnumerable<ICmObject> AllOwnedObjects { get; private set; }
			public int Hvo { get; private set;}
			public ICmObject Owner { get; set;}
			public int OwningFlid
			{
				get { throw new NotImplementedException(); }
			}
			public int OwnOrd
			{
				get { throw new NotImplementedException(); }
			}
			public int ClassID
			{
				get { throw new NotImplementedException(); }
			}
			public Guid Guid { get; private set;}

			public ICmObjectId Id
			{
				get { throw new NotImplementedException(); }
			}

			public ICmObject GetObject(ICmObjectRepository repo)
			{
				throw new NotImplementedException();
			}

			public string ToXmlString()
			{
				throw new NotImplementedException();
			}

			public string ClassName
			{
				get { throw new NotImplementedException(); }
			}
			public void Delete()
			{
				throw new NotImplementedException();
			}

			public IFdoServiceLocator Services
			{
				get { throw new NotImplementedException(); }
			}

			public ICmObject OwnerOfClass(int clsid)
			{
				throw new NotImplementedException();
			}

			public T OwnerOfClass<T>() where T : ICmObject
			{
				throw new NotImplementedException();
			}

			public ICmObject Self
			{
				get { throw new NotImplementedException(); }
			}

			public bool CheckConstraints(int flidToCheck, bool createAnnotation, out ConstraintFailure failure)
			{
				throw new NotImplementedException();
			}

			public void PostClone(Dictionary<int, ICmObject> copyMap)
			{
				throw new NotImplementedException();
			}

			public void AllReferencedObjects(List<ICmObject> collector)
			{
				throw new NotImplementedException();
			}

			public bool IsFieldRelevant(int flid, HashSet<Tuple<int, int>> propsToMonitor)
			{
				throw new NotImplementedException();
			}

			public bool IsOwnedBy(ICmObject possibleOwner)
			{
				throw new NotImplementedException();
			}

			public ICmObject ReferenceTargetOwner(int flid)
			{
				throw new NotImplementedException();
			}

			public bool IsFieldRequired(int flid)
			{
				throw new NotImplementedException();
			}

			public int IndexInOwner
			{
				get { throw new NotImplementedException(); }
			}

			public IEnumerable<ICmObject> ReferenceTargetCandidates(int flid)
			{
				throw new NotImplementedException();
			}

			public bool IsValidObject { get; set; }

			public FdoCache Cache
			{
				get { throw new NotImplementedException(); }
			}

			public void MergeObject(ICmObject objSrc)
			{
				throw new NotImplementedException();
			}

			public void MergeObject(ICmObject objSrc, bool fLoseNoStringData)
			{
				throw new NotImplementedException();
			}

			public bool CanDelete
			{
				get { throw new NotImplementedException(); }
			}

			public ITsString ObjectIdName
			{
				get { throw new NotImplementedException(); }
			}

			public string ShortName
			{
				get { throw new NotImplementedException(); }
			}

			public ITsString ShortNameTSS
			{
				get { throw new NotImplementedException(); }
			}

			public ITsString DeletionTextTSS
			{
				get { throw new NotImplementedException(); }
			}

			public ITsString ChooserNameTS
			{
				get { throw new NotImplementedException(); }
			}

			public string SortKey
			{
				get { throw new NotImplementedException(); }
			}

			public string SortKeyWs
			{
				get { throw new NotImplementedException(); }
			}

			public int SortKey2
			{
				get { throw new NotImplementedException(); }
			}

			public string SortKey2Alpha
			{
				get { throw new NotImplementedException(); }
			}

			public HashSet<ICmObject> ReferringObjects
			{
				get { throw new NotImplementedException(); }
			}

			public IEnumerable<ICmObject> OwnedObjects { get; private set; }
		}
	}
}

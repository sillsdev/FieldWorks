using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// Tests related to CmObjectId.
	/// </summary>
	[TestFixture]
	public class CmObjectIdTests : MemoryOnlyBackendProviderTestBase
	{
		/// <summary>
		/// Test we can make them in multiple ways and always get the same instance.
		/// </summary>
		[Test]
		public void MakingInstances()
		{
			var factory = Cache.ServiceLocator.GetInstance<ICmObjectIdFactory>();
			Guid test = Guid.NewGuid();
			var id1 = factory.FromGuid(test);
			var id2 = factory.FromGuid(test);
			Assert.IsTrue(object.ReferenceEquals(id1, id2), "separately obtained object ids should be the same object");
			var id3 = factory.FromBase64String(GuidServices.GetString(test));
			Assert.IsTrue(object.ReferenceEquals(id1, id3), "object id from string should be the same object as from guid");
			Guid test2 = Guid.NewGuid();
			var id4 = factory.FromGuid(test2);
			Assert.AreNotEqual(id1, id4, "object ids from different guids should not be equal.");
			Guid test3 = id1.Guid;
			Assert.AreEqual(test, test3);
		}

		/// <summary>
		/// Test that we can make a set that treats objects and IDs the same.
		/// </summary>
		[Test]
		public void CmObjectOrIdSets()
		{
			var item1 = Cache.LangProject; // just need a couple of arbitrary objects.
			var item2 = item1.LexDbOA;
			HashSet<ICmObjectOrId> testSet = new HashSet<ICmObjectOrId>(new ObjectIdEquater());
			testSet.Add(item1);
			testSet.Add(item2.Id);
			Assert.IsTrue(testSet.Contains(item1.Id));
			Assert.IsTrue(testSet.Contains(item2));
		}

		/// <summary>
		/// Test the tricks for getting real objects from anything that claims to be a CmObjectOrId.
		/// </summary>
		[Test]
		public void CmObjectOrIdGetObject()
		{
			var item = Cache.LangProject.LexDbOA; // an arbitrary object.
			var itemOrId = item as ICmObjectOrId;
			var repo = Cache.ServiceLocator.GetInstance<ICmObjectRepository>();
			Assert.AreEqual(item, itemOrId.GetObject(repo));
			var idAsItemOrId = item.Id as ICmObjectOrId;
			Assert.AreEqual(item, idAsItemOrId.GetObject(repo));
		}
	}
}

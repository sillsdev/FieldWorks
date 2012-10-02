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
	/// Test stuff related to notebook classes.
	/// </summary>
	[TestFixture]
	public class NotebookTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary>
		/// Test the indicated method.
		/// </summary>
		[Test]
		public void SubrecordOfKey()
		{
			var recordEmpty = (RnGenericRec)Cache.ServiceLocator.GetInstance<IRnGenericRecFactory>().Create(); // just in case, no title at all.
			Cache.LangProject.ResearchNotebookOA.RecordsOC.Add(recordEmpty);
			var recordFish = MakeRnRecord("Fishing for Piranha");
			var recordHunt = MakeRnRecord("Hunting wild pigs");
			var recordWedding = MakeRnRecord("Wedding of the chief");
			Assert.That(recordEmpty.SubrecordOfKey(false), Is.LessThan(recordFish.SubrecordOfKey(false)));
			Assert.That(recordFish.SubrecordOfKey(false), Is.LessThan(recordHunt.SubrecordOfKey(false)));
			Assert.That(recordHunt.SubrecordOfKey(false), Is.LessThan(recordWedding.SubrecordOfKey(false)));
			var subFish1 = MakeSubRecord(recordFish, "Preparations", 0);
			Assert.That(recordFish.SubrecordOfKey(false), Is.LessThan(subFish1.SubrecordOfKey(false)));
			Assert.That(recordEmpty.SubrecordOfKey(false), Is.LessThan(subFish1.SubrecordOfKey(false)));
			Assert.That(subFish1.SubrecordOfKey(false), Is.LessThan(recordHunt.SubrecordOfKey(false)));
			var subHunt1 = MakeSubRecord(recordHunt, "Preparations", 0);
			Assert.That(recordHunt.SubrecordOfKey(false), Is.LessThan(subHunt1.SubrecordOfKey(false)));
			Assert.That(recordFish.SubrecordOfKey(false), Is.LessThan(subHunt1.SubrecordOfKey(false)));
			Assert.That(subFish1.SubrecordOfKey(false), Is.LessThan(recordWedding.SubrecordOfKey(false)));
			var subHunt2 = MakeSubRecord(recordHunt, "Going to the forest", 1);
			var subFish2 = MakeSubRecord(recordFish, "Going to the lake", 1);
			// Fish2 comes before Hunt2 even though "Going to the forest" is less than "going to the lake".
			Assert.That(subFish2.SubrecordOfKey(false), Is.LessThan(subHunt2.SubrecordOfKey(false)));
			// All the Fish records come before the Hunt ones, even though SubrecordOf starts with 2 rather than 1.
			Assert.That(subFish2.SubrecordOfKey(false), Is.LessThan(subHunt1.SubrecordOfKey(false)));

			// Now we want to make sure the sorting of subrecords is truly numeric. This calls for at least 10.
			MakeSubRecord(recordFish, "Loading the boat", 2);
			MakeSubRecord(recordFish, "Launching the boat", 3);
			MakeSubRecord(recordFish, "Finding the fish", 4);
			MakeSubRecord(recordFish, "Spreading the net", 5);
			MakeSubRecord(recordFish, "Surrounding the fish", 6);
			MakeSubRecord(recordFish, "Hauling the net", 7);
			MakeSubRecord(recordFish, "Filling the boat", 8);
			MakeSubRecord(recordFish, "Landing", 9);
			var subFish10 = MakeSubRecord(recordFish, "Cleaning the fish", 10);
			Assert.That(subFish2.SubrecordOfKey(false), Is.LessThan(subFish10.SubrecordOfKey(false)));
		}
		/// <summary>
		/// Ensure there is a record types possibility list with at least one type.
		/// </summary>
		/// <returns></returns>
		private ICmPossibilityList EnsureRecTypesList()
		{
			var recTypes = Cache.LangProject.ResearchNotebookOA.RecTypesOA;
			if (recTypes == null)
				recTypes = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			Cache.LangProject.ResearchNotebookOA.RecTypesOA = recTypes;
			if (recTypes.PossibilitiesOS.Count == 0)
			{
				var aType = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
				recTypes.PossibilitiesOS.Add(aType);
				aType.Name.AnalysisDefaultWritingSystem = Cache.TsStrFactory.MakeString("test type", Cache.DefaultAnalWs);
			}
			return recTypes;
		}

		private RnGenericRec MakeRnRecord(string title)
		{
			var typeList = EnsureRecTypesList();
			IRnGenericRec entry = null;
			entry = Cache.ServiceLocator.GetInstance<IRnGenericRecFactory>().Create();
			Cache.LangProject.ResearchNotebookOA.RecordsOC.Add(entry);
			entry.Title = Cache.TsStrFactory.MakeString(title, Cache.DefaultAnalWs);
			entry.TypeRA = typeList.PossibilitiesOS[0];
			return (RnGenericRec)entry;
		}

		private RnGenericRec MakeSubRecord(IRnGenericRec parent, string title, int index)
		{
			var typeList = EnsureRecTypesList();
			IRnGenericRec entry = null;
			entry = Cache.ServiceLocator.GetInstance<IRnGenericRecFactory>().Create();
			parent.SubRecordsOS.Insert(index, entry);
			entry.Title = Cache.TsStrFactory.MakeString(title, Cache.DefaultAnalWs);
			entry.TypeRA = typeList.PossibilitiesOS[0];
			return (RnGenericRec)entry;
		}
	}
}

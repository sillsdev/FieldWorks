using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// Tests FdoInvertSet, a set which implements IFdoSet by wrapping another IFdoSet
	/// and an enumerable and behaving so that the items in this set are the ones in the enumeration
	/// that are not in the wrapped set. This is used to implement PublishIn, so we can test it there.
	/// </summary>
	[TestFixture]
	public class FdoInvertSetTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary>
		/// Test the replace method. This is what we most care about.
		/// </summary>
		[Test]
		public void Replace()
		{
			var kick = MakeEntry("kick", "strike with foot");
			var mainDict = Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS[0];
			var pocket = MakePossibility(Cache.LangProject.LexDbOA.PublicationTypesOA, "pocket");
			var scholar = MakePossibility(Cache.LangProject.LexDbOA.PublicationTypesOA, "scholar");
			Assert.That(kick.PublishIn.Count(), Is.EqualTo(3));
			kick.PublishIn.Replace(new ICmObject[] {pocket}, new ICmObject[0]);
			var result = kick.PublishIn.ToArray();
			Assert.That(result.Length, Is.EqualTo(2));
			Assert.That(result[0], Is.EqualTo(mainDict));
			Assert.That(result[1], Is.EqualTo(scholar));
			kick.PublishIn.Replace(new ICmObject[] { mainDict }, new ICmObject[] { pocket });
			result = kick.PublishIn.ToArray();
			Assert.That(result.Length, Is.EqualTo(2));
			Assert.That(result[0], Is.EqualTo(pocket));
			Assert.That(result[1], Is.EqualTo(scholar));

			int publishInFlid = Cache.MetaDataCacheAccessor.GetFieldId2(LexEntryTags.kClassId, "PublishIn", false);
			Cache.DomainDataByFlid.Replace(kick.Hvo, publishInFlid, 1, 2, new int[] {mainDict.Hvo}, 1);
			result = kick.PublishIn.ToArray();
			Assert.That(result.Length, Is.EqualTo(2));
			Assert.That(result[0], Is.EqualTo(mainDict));
			Assert.That(result[1], Is.EqualTo(pocket));
			Cache.DomainDataByFlid.Replace(kick.Hvo, publishInFlid, 1, 2, new int[0] , 0);
			result = kick.PublishIn.ToArray();
			Assert.That(result.Length, Is.EqualTo(1));
			Assert.That(result[0], Is.EqualTo(mainDict));
			Cache.DomainDataByFlid.Replace(kick.Hvo, publishInFlid, 0, 1, new int[] { mainDict.Hvo, pocket.Hvo}, 2);
			result = kick.PublishIn.ToArray();
			Assert.That(result.Length, Is.EqualTo(2));
			Assert.That(result[0], Is.EqualTo(mainDict));
			Assert.That(result[1], Is.EqualTo(pocket));
		}

		/// <summary>
		/// Test the replace method. This is what we most care about.
		/// </summary>
		[Test]
		public void AddRemove()
		{
			var kick = MakeEntry("kick", "strike with foot");
			var mainDict = Cache.LangProject.LexDbOA.PublicationTypesOA.PossibilitiesOS[0];
			var pocket = MakePossibility(Cache.LangProject.LexDbOA.PublicationTypesOA, "pocket");
			var result = kick.PublishIn.ToArray();
			Assert.That(result.Length, Is.EqualTo(2)); // no action, they are all there to start!
			Assert.That(result[0], Is.EqualTo(mainDict));
			Assert.That(result[1], Is.EqualTo(pocket));

			kick.PublishIn.Remove(mainDict);
			result = kick.PublishIn.ToArray();
			Assert.That(result.Length, Is.EqualTo(1));
			Assert.That(result[0], Is.EqualTo(pocket));

			kick.PublishIn.Remove(pocket);
			result = kick.PublishIn.ToArray();
			Assert.That(result.Length, Is.EqualTo(0));

			kick.PublishIn.Add(mainDict);
			result = kick.PublishIn.ToArray();
			Assert.That(result.Length, Is.EqualTo(1));
			Assert.That(result[0], Is.EqualTo(mainDict));

			kick.PublishIn.Add(pocket);
			result = kick.PublishIn.ToArray();
			Assert.That(result.Length, Is.EqualTo(2));
			Assert.That(result[0], Is.EqualTo(mainDict));
			Assert.That(result[1], Is.EqualTo(pocket));
		}

		private ILexEntry MakeEntry(string lf, string gloss)
		{
			var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var form = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entry.LexemeFormOA = form;
			form.Form.VernacularDefaultWritingSystem =
				Cache.TsStrFactory.MakeString(lf, Cache.DefaultVernWs);
			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(sense);
			sense.Gloss.AnalysisDefaultWritingSystem = Cache.TsStrFactory.MakeString(gloss, Cache.DefaultAnalWs);
			return entry;
		}

		private ICmPossibility MakePossibility(ICmPossibilityList list, string name)
		{
			var result = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create();
			list.PossibilitiesOS.Add(result);
			result.Name.AnalysisDefaultWritingSystem = AnalysisTss(name);
			return result;
		}

		private ITsString AnalysisTss(string form)
		{
			return Cache.TsStrFactory.MakeString(form, Cache.DefaultAnalWs);
		}
	}
}

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
	/// Tests functions related to preventing circular chains of complex forms.
	/// </summary>
	[TestFixture]
	public class CircularReferenceTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary>
		/// Test the indicated method.
		/// </summary>
		[Test]
		public void IsComponent()
		{
			var kick = MakeEntry("kick", "strike with foot");
			Assert.That(kick.IsComponent(kick), Is.True, "entry should be considered a component of itself");

			var kickBucket = MakeEntry("kick the bucket", "die");
			var ler = MakeComplexFormLexEntryRef(kickBucket);
			ler.ComponentLexemesRS.Add(kick);
			Assert.That(kickBucket.IsComponent(kick), Is.True, "direct entry component should be found");
			Assert.That(kick.IsComponent(kickBucket), Is.False, "complex form is not component of its component");

			var run = MakeEntry("run", "move fast by foot"); // unrelated to anything.
			Assert.That(kickBucket.IsComponent(run), Is.False, "unrelated entry is not a component");

			var the = MakeEntry("the", "definite article");
			ler.ComponentLexemesRS.Add(the.SensesOS[0]);
			Assert.That(kickBucket.IsComponent(the), Is.True, "sense component should be noticed");

			var bucket = MakeEntry("bucket", "container");
			var waterHolder = MakeSense(bucket.SensesOS[0], "water container");
			ler.ComponentLexemesRS.Add(waterHolder);
			Assert.That(kickBucket.IsComponent(bucket), Is.True, "subsense component should be noticed");

			// now try indirection
			var intent = MakeEntry("intent", "focused");
			var intention = MakeCompound("intention", "purpose", new ICmObject[] {intent});
			var intentional = MakeCompound("intentional", "purposeful", new ICmObject[] {intention});
			var unintentional = MakeCompound("unintentional", "accidental", new ICmObject[] { intentional });
			Assert.That(unintentional.IsComponent(intent), Is.True, "indirect component should be noticed");

			// now try to break the rule
			Assert.Throws(typeof (ArgumentException), () => ler.ComponentLexemesRS.Add(kickBucket));
		}
		private ILexEntry MakeEntry(string lf, string gloss)
		{
			ILexEntry entry = null;
			entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			var form = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			entry.LexemeFormOA = form;
			form.Form.VernacularDefaultWritingSystem =
				Cache.TsStrFactory.MakeString(lf, Cache.DefaultVernWs);
			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			entry.SensesOS.Add(sense);
			sense.Gloss.AnalysisDefaultWritingSystem = Cache.TsStrFactory.MakeString(gloss, Cache.DefaultAnalWs);
			return entry;
		}

		private ILexEntry MakeCompound(string lf, string gloss, ICmObject[] components)
		{
			var result = MakeEntry(lf, gloss);
			var ler = MakeComplexFormLexEntryRef(result);
			foreach (var obj in components)
				ler.ComponentLexemesRS.Add(obj);
			return result;
		}

		private ILexEntryRef MakeComplexFormLexEntryRef(ILexEntry ownerEntry)
		{
			ILexEntryRef result = null;
			result = Cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
			ownerEntry.EntryRefsOS.Add(result);
			result.RefType = LexEntryRefTags.krtComplexForm;
			return result;
		}

		private ILexSense MakeSense(ILexSense owningSense, string gloss)
		{
			var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
			owningSense.SensesOS.Add(sense);
			sense.Gloss.AnalysisDefaultWritingSystem = Cache.TsStrFactory.MakeString(gloss, Cache.DefaultAnalWs);
			return sense;
		}
	}
}

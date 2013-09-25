using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.XWorks.LexEd;

namespace LexEdDllTests
{
	/// <summary>
	/// Tests (very incomplete as yet) for LexReferenceTreeRootLauncher
	/// </summary>
	[TestFixture]
	public class LexReferenceTreeRootLauncherTests : MemoryOnlyBackendProviderTestBase
	{

		/// <summary>
		/// This is a regression test (LT-14926) to make sure we don't reintroduce a problem where replacing a 'whole' that has only one part
		/// failed because it began by deleting the 'whole' from the relation, which left only one item and caused the entire relation to
		/// be deleted as a side effect.
		/// </summary>
		[Test]
		public void SettingTargetToReplaceWholeInRelationWithOnlyOnePartDoesNotDeleteRelation()
		{
			var lrtrl = new TestLrtrl();

			UndoableUnitOfWorkHelper.Do("undo", "redo", m_actionHandler,
				() =>
				{

					// Set up a part-whole type lexical relation and two lexical entries indicating that "nose" is a part of "face".
					var lrt = Cache.ServiceLocator.GetInstance<ILexRefTypeFactory>().Create();
					if (Cache.LangProject.LexDbOA.ReferencesOA == null)
					{
						// default state of cache may not have the PL we need to own our lexical relation type...if not create it.
						Cache.LangProject.LexDbOA.ReferencesOA =
							Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
					}
					Cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.Add(lrt);
					lrt.MappingType = (int)LexRefTypeTags.MappingTypes.kmtSenseAsymmetricPair; // e.g., part/whole

					var face = MakeEntry("face", "front of head");
					var nose = MakeEntry("nose", "pointy bit on front");
					var rel = Cache.ServiceLocator.GetInstance<ILexReferenceFactory>().Create();
					lrt.MembersOC.Add(rel);
					rel.TargetsRS.Add(face);
					rel.TargetsRS.Add(nose);

					// Here is an alternative 'whole' to be the root that 'nose' belongs to.
					var head = MakeEntry("head", "thing on top of body");

					// Now we want to configure the Lrtrl so that setting its target to 'head' will replace 'face' with 'head'.
					lrtrl.SetObject(rel);
					lrtrl.Child = nose; // the part for which we are changing the whole

					// This is the operation we want to test
					lrtrl.SetTarget(head);
					Assert.That(rel.IsValidObject);
					Assert.That(rel.TargetsRS, Has.Count.EqualTo(2));
					Assert.That(rel.TargetsRS[0], Is.EqualTo(head));
					Assert.That(rel.TargetsRS[1], Is.EqualTo(nose));
				});
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
	}

	/// <summary>
	/// Subclass to get access to the protected method and variable. This allows us to minimally initialize the instance for testing
	/// this method, without creating the whole hierarchy of nested controls that this object would normally be part of.
	/// </summary>
	class TestLrtrl : LexReferenceTreeRootLauncher
	{
		public void SetTarget(ICmObject target)
		{
			Target = target;
		}

		public void SetObject(ICmObject obj)
		{
			m_obj = obj;
		}

		public ICmObject Child { get; set; }

		internal override ICmObject GetChildObject()
		{
			return Child;
		}
	}
}

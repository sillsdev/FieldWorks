// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ValidationTests.cs
// Responsibility: RandyR
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.FDO.DomainServices;
using System.Collections.Generic;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test for validating objects.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ValidationTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary>
		/// Test to make sure the StringRepresentation property of PhEnvironment
		/// is valid or invalid, as the case may be.
		/// </summary>
		[Test]
		public void ValidatePhEnvironment_StringRepresentation()
		{
			// Add a character to the set of phoneme representations so that the environment below
			// will pass muster.
			IPhPhonemeSet phset = Cache.ServiceLocator.GetInstance<IPhPhonemeSetFactory>().Create();
			Cache.LanguageProject.PhonologicalDataOA.PhonemeSetsOS.Add(phset);
			AddPhone(phset, "a");
			AddPhone(phset, "b");
			AddPhone(phset, "c");
			AddPhone(phset, "d");
			AddPhone(phset, "e");

			ConstraintFailure failure = null;
			int strRepFlid = PhEnvironmentTags.kflidStringRepresentation;
			IPhEnvironment env = Cache.ServiceLocator.GetInstance<IPhEnvironmentFactory>().Create();
			Cache.LangProject.PhonologicalDataOA.EnvironmentsOS.Add(env);
			IEnumerable<ICmBaseAnnotation> os = GetAnnotationsForObject(env);
			Assert.AreEqual(0, os.Count(), "Wrong starting count of annotations.");

			env.StringRepresentation = Cache.TsStrFactory.MakeString(@"/ [BADCLASS] _", Cache.DefaultAnalWs);

			m_actionHandler.EndUndoTask(); // so we can verify it makes its own as needed, and doesn't do unnecessary ones.

			Assert.IsFalse(env.CheckConstraints(strRepFlid, true, out failure, true));
			Assert.IsNotNull(failure, "Didn't get an object back from the CheckConstraints method.");
			os = GetAnnotationsForObject(env);
			Assert.AreEqual(1, os.Count(), "Wrong invalid count of annotations.");

			int cUndoTasks = m_actionHandler.UndoableActionCount;
			Assert.IsFalse(env.CheckConstraints(strRepFlid, true, out failure, true));
			Assert.That(m_actionHandler.UndoableActionCount, Is.EqualTo(cUndoTasks), "should not make database changes when situation is unchanged");

			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
				env.StringRepresentation = Cache.TsStrFactory.MakeString(@"/ d _", Cache.DefaultAnalWs));
			Assert.IsTrue(env.CheckConstraints(strRepFlid, true, out failure, true));
			Assert.IsNull(failure, "Got an object back from the CheckConstraints method.");
			os = GetAnnotationsForObject(env);
			Assert.AreEqual(0, os.Count(), "Wrong valid count of annotations.");

			cUndoTasks = m_actionHandler.UndoableActionCount;
			Assert.IsTrue(env.CheckConstraints(strRepFlid, true, out failure, true));
			Assert.That(m_actionHandler.UndoableActionCount, Is.EqualTo(cUndoTasks), "should not make database changes when situation is unchanged");
		}

		private void AddPhone(IPhPhonemeSet phset, string sCode)
		{
			IPhPhoneme phone = Cache.ServiceLocator.GetInstance<IPhPhonemeFactory>().Create();
			phset.PhonemesOC.Add(phone);
			IPhCode code;
			if (phone.CodesOS.Count == 0)
			{
				code = Cache.ServiceLocator.GetInstance<IPhCodeFactory>().Create();
				phone.CodesOS.Add(code);
			}
			else
			{
				code = phone.CodesOS[0];
			}
			code.Representation.set_String(Cache.DefaultVernWs, sCode);
		}

		private IEnumerable<ICmBaseAnnotation> GetAnnotationsForObject(ICmObject obj)
		{
			var errors =
				from error in Cache.ServiceLocator.GetInstance<ICmBaseAnnotationRepository>().AllInstances()
				where error.BeginObjectRA == obj
				select error;
			return errors;
		}
	}
}

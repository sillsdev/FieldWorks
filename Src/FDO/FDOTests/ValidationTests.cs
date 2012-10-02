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

using NUnit.Framework;

using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Cellar;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// Test for validating objects.
	/// </summary>
	[TestFixture]
	public class ValidationTests : InDatabaseFdoTestBase
	{
		// TODO: Use in-memory FDO testbase. This requires some refactoring because at least
		// env.CheckConstraints uses SQL commands. Using in-memory cache is approx. 10x faster.

		/// <summary>
		/// Test to make sure the StringRepresentation property of PhEnvironment
		/// is valid or invalid, as the case may be.
		/// </summary>
		[Test]
		public void ValidatePhEnvironment_StringRepresentation()
		{
			CheckDisposed();

			ConstraintFailure failure = null;
			FdoObjectSet<ICmBaseAnnotation> os = null;
			int strRepFlid = (int)PhEnvironment.PhEnvironmentTags.kflidStringRepresentation;
			PhEnvironment env = (PhEnvironment)m_fdoCache.LangProject.PhonologicalDataOA.EnvironmentsOS.Append(new PhEnvironment());

			os = CmBaseAnnotation.AnnotationsForObject(m_fdoCache, env.Hvo);
			Assert.AreEqual(0, os.Count, "Wrong starting count of annotations.");

			env.StringRepresentation.Text = @"/ [BADCLASS] _";
			Assert.IsFalse(env.CheckConstraints(strRepFlid, out failure));
			Assert.IsNotNull(failure, "Didn't get an object back from the CheckConstraints method.");
			//Assert.IsTrue(obj is CmBaseAnnotation, "Didn't get a CmBaseAnnotation back from the Validate call.");
			os = CmBaseAnnotation.AnnotationsForObject(m_fdoCache, env.Hvo);
			Assert.AreEqual(1, os.Count, "Wrong invalid count of annotations.");

			env.StringRepresentation.Text= @"/ d _";
			Assert.IsTrue(env.CheckConstraints(strRepFlid, out failure));
			Assert.IsNull(failure, "Got an object back from the CheckConstraints method.");
			os = CmBaseAnnotation.AnnotationsForObject(m_fdoCache, env.Hvo);
			Assert.AreEqual(0, os.Count, "Wrong valid count of annotations.");
		}
	}
}

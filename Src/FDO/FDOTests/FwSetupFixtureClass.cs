// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
// ---------------------------------------------------------------------------------------------
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.Utils;

namespace SIL.FieldWorks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Helper class that contains methods that will be run before/after all other tests and
	/// fixture setups/teardowns are run.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[SetUpFixture]
	public class FwSetupFixtureClass
	{
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// SetUp method that will be run once before any tests or setup methods
		/// </summary>
		///--------------------------------------------------------------------------------------
		[SetUp]
		public void SetUp()
		{
			ClientServerServices.SetCurrentToDb4OBackend(new DummyFdoUI(), FwDirectoryFinder.FdoDirectories,
				() => FwDirectoryFinder.ProjectsDirectory == FwDirectoryFinder.ProjectsDirectoryLocalMachine);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Teardown method that will be run once after any tests or setup methods
		/// </summary>
		///--------------------------------------------------------------------------------------
		[TearDown]
		public void TearDown()
		{
			ReflectionHelper.CallStaticMethod("FwResources.dll",
				"SIL.FieldWorks.Resources.ResourceHelper", "ShutdownHelper");
		}
	}
}

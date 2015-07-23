// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;

namespace SIL.CoreImpl
{
	/// <summary>
	/// Test the AreaRepository.
	/// </summary>
	[TestFixture]
	[Category("ByHand")] // Needs to be run after everything has been built, or the areas may not be built yet.
	public class AreaRepositoryTests
	{
		private IAreaRepository m_areaRepository;

		/// <summary>
		/// Set up test fixture.
		/// </summary>
		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			m_areaRepository = AreaRepositoryFactory.CreateAreaRepository();
		}

		/// <summary>
		/// Tear down the test fixture.
		/// </summary>
		[TestFixtureTearDown]
		public void FextureTeardown()
		{
			m_areaRepository = null;
		}

		/// <summary>
		/// Doesn't have some unknown area.
		/// </summary>
		[Test]
		public void UnknownAreaNotPresent()
		{
			Assert.IsNull(m_areaRepository.GetArea("bogusArea"));
		}

		/// <summary>
		/// Make sure the AreaRepository has the expected nubmer of areas.
		/// </summary>
		[Test]
		public void AreaRepositoryHasLexiconArea()
		{
			Assert.IsNotNull(m_areaRepository.GetArea("lexicon"));
		}
	}
}

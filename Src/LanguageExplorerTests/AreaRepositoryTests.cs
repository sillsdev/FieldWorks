// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer;
using LanguageExplorer.Impls;
using NUnit.Framework;

namespace LanguageExplorerTests
{
	/// <summary>
	/// Test the AreaRepository.
	/// </summary>
	[TestFixture]
	public class AreaRepositoryTests
	{
		private IToolRepository _toolRepository;
		private IAreaRepository _areaRepository;

		/// <summary>
		/// Set up test fixture.
		/// </summary>
		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			_toolRepository = new ToolRepository();
			_areaRepository = new AreaRepository(_toolRepository);
		}

		/// <summary>
		/// Tear down the test fixture.
		/// </summary>
		[TestFixtureTearDown]
		public void FixtureTeardown()
		{
			_areaRepository = null;
			_toolRepository = null;
		}

		/// <summary>
		/// Doesn't have some unknown area.
		/// </summary>
		[Test]
		public void UnknownAreaNotPresent()
		{
			Assert.IsNull(_areaRepository.GetArea("bogusArea"));
		}

		/// <summary>
		/// Make sure the AreaRepository has the expected number of areas.
		/// </summary>
		[Test]
		public void AreaRepositoryHasAllAreas()
		{
			var allAreas = _areaRepository.AllAreasInOrder();
			Assert.AreEqual(5, allAreas.Count);
		}

		/// <summary>
		/// Make sure the AreaRepository has the expected areas.
		/// </summary>
		[Test]
		public void AreaRepositoryHasAllExpectedAreas()
		{
			Assert.IsNotNull(_areaRepository.GetArea("lexicon"));
			Assert.IsNotNull(_areaRepository.GetArea("textAndWords"));
			Assert.IsNotNull(_areaRepository.GetArea("grammar"));
			Assert.IsNotNull(_areaRepository.GetArea("notebook"));
			Assert.IsNotNull(_areaRepository.GetArea("lists"));
		}
	}
}

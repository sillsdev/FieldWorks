// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.ComponentModel.Composition.Hosting;
using LanguageExplorer;

namespace LanguageExplorerTests.Repository
{
	internal class MefTestBase
	{
		protected CompositionContainer _compositionContainer;
		protected IAreaRepository _areaRepository;

		/// <summary>
		/// Set up test fixture.
		/// </summary>
		public virtual void FixtureSetup()
		{
			// It is fine to have "LanguageExplorerCompositionServices.GetWindowScopedTypes()" be globally available for these tests.
			var aggregateCatalog = new AggregateCatalog();
			aggregateCatalog.Catalogs.Add(new TypeCatalog(LanguageExplorerCompositionServices.GetWindowScopedTypes()));
			_compositionContainer = new CompositionContainer(aggregateCatalog);

			_areaRepository = _compositionContainer.GetExportedValue<IAreaRepository>();
		}

		/// <summary>
		/// Tear down the test fixture.
		/// </summary>
		public virtual void FixtureTeardown()
		{
			_compositionContainer.Dispose();
			_compositionContainer = null;
			_areaRepository = null;
		}
	}
}
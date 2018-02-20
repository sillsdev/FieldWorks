﻿// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.ComponentModel.Composition.Hosting;
using LanguageExplorer;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.WritingSystems;

namespace LanguageExplorerTests.Repository
{
	internal class MefTestBase : MemoryOnlyBackendProviderTestBase
	{
		protected CompositionContainer _compositionContainer;
		protected IAreaRepository _areaRepository;
		protected IPublisher _publisher;
		protected IPropertyTable _propertyTable;

		/// <summary>
		/// Set up test fixture.
		/// </summary>
		public override void FixtureSetup()
		{
			if (!Sldr.IsInitialized)
			{
				// initialize the SLDR
				Sldr.Initialize();
			}

			base.FixtureSetup();

			// It is fine to have "LanguageExplorerCompositionServices.GetWindowScopedTypes()" be globally available for these tests.
			var aggregateCatalog = new AggregateCatalog();
			aggregateCatalog.Catalogs.Add(new TypeCatalog(LanguageExplorerCompositionServices.GetWindowScopedTypes()));
			_compositionContainer = new CompositionContainer(aggregateCatalog);

			_areaRepository = _compositionContainer.GetExportedValue<IAreaRepository>();
			_publisher = _compositionContainer.GetExportedValue<IPublisher>();
			_propertyTable = _compositionContainer.GetExportedValue<IPropertyTable>();
			_propertyTable.SetProperty("cache", Cache, SettingsGroup.LocalSettings, false, false);
		}

		/// <summary>
		/// Tear down the test fixture.
		/// </summary>
		public override void FixtureTeardown()
		{
			base.FixtureTeardown();

			if (Sldr.IsInitialized)
			{
				Sldr.Cleanup();
			}

			_compositionContainer.Dispose();
			_propertyTable.Dispose();
			_compositionContainer = null;
			_areaRepository = null;
			_publisher = null;
			_propertyTable = null;
		}
	}
}
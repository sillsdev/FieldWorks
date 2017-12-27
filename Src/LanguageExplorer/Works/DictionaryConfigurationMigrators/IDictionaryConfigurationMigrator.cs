// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Works.DictionaryConfigurationMigrators
{
	internal interface IDictionaryConfigurationMigrator
	{
		void MigrateIfNeeded(ISimpleLogger logger, IPropertyTable propertyTable, string applicationVersion);
	}
}
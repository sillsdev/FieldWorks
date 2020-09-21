// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks.DictionaryConfigurationMigrators
{
	public interface IDictionaryConfigurationMigrator
	{
		void MigrateIfNeeded(SimpleLogger logger, PropertyTable propertyTable, string applicationVersion);
	}
}
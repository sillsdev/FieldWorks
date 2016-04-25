using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks.DictionaryConfigurationMigrators
{
	public interface IDictionaryConfigurationMigrator
	{
		void MigrateIfNeeded(SimpleLogger mLogger, Mediator mediator, string applicationVersion);
	}
}
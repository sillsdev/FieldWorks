using System.Collections.Generic;
using SIL.LexiconUtils;
using SIL.WritingSystems;

namespace SIL.CoreImpl
{
	/// <summary>
	/// A file-based local writing system store.
	/// </summary>
	public class CoreLdmlInFolderWritingSystemRepository : LdmlInFolderWritingSystemRepository<CoreWritingSystemDefinition>
	{
		private readonly CoreGlobalWritingSystemRepository m_globalRepository;

		/// <summary>
		/// Initializes a new instance of the <see cref="CoreLdmlInFolderWritingSystemRepository"/> class.
		/// </summary>
		public CoreLdmlInFolderWritingSystemRepository(string path, ISettingsStore projectSettingsStore, ISettingsStore userSettingsStore,
			CoreGlobalWritingSystemRepository globalRepository = null)
			: base(path, CreateCustomDataMappers(projectSettingsStore, userSettingsStore), globalRepository)
		{
			m_globalRepository = globalRepository;
		}

		private static IEnumerable<ICustomDataMapper<CoreWritingSystemDefinition>> CreateCustomDataMappers(ISettingsStore projectSettingsStore, ISettingsStore userSettingsStore)
		{
			return new ICustomDataMapper<CoreWritingSystemDefinition>[]
			{
				new ProjectLexiconSettingsWritingSystemDataMapper<CoreWritingSystemDefinition>(projectSettingsStore),
				new UserLexiconSettingsWritingSystemDataMapper<CoreWritingSystemDefinition>(userSettingsStore)
			};
		}

		/// <summary>
		/// Gets the global file writing system store.
		/// </summary>
		public new CoreGlobalWritingSystemRepository GlobalWritingSystemRepository
		{
			get { return m_globalRepository; }
		}

		/// <summary>
		/// Creates the default writing system factory.
		/// </summary>
		protected override IWritingSystemFactory<CoreWritingSystemDefinition> CreateWritingSystemFactory()
		{
			return new CoreLdmlInFolderWritingSystemFactory(this);
		}
	}
}

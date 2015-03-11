using System.Collections.Generic;
using System.Linq;
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
		/// <param name="path">The path.</param>
		public CoreLdmlInFolderWritingSystemRepository(string path) : this(path, Enumerable.Empty<ICustomDataMapper<CoreWritingSystemDefinition>>())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CoreLdmlInFolderWritingSystemRepository"/> class.
		/// </summary>
		/// <param name="path">The path.</param>
		/// <param name="customDataMappers">The custom data mappers.</param>
		/// <param name="globalRepository">The global repository.</param>
		public CoreLdmlInFolderWritingSystemRepository(string path, IEnumerable<ICustomDataMapper<CoreWritingSystemDefinition>> customDataMappers,
			CoreGlobalWritingSystemRepository globalRepository = null)
			: base(path, customDataMappers.ToArray(), globalRepository)
		{
			m_globalRepository = globalRepository;
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

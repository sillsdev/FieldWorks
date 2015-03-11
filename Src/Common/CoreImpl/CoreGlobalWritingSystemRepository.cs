using SIL.WritingSystems;

namespace SIL.CoreImpl
{
	/// <summary>
	/// A file-based global writing system store.
	/// </summary>
	public class CoreGlobalWritingSystemRepository : GlobalWritingSystemRepository<CoreWritingSystemDefinition>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CoreGlobalWritingSystemRepository"/> class.
		/// </summary>
		public CoreGlobalWritingSystemRepository()
			: this(GlobalWritingSystemRepository.DefaultBasePath)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CoreGlobalWritingSystemRepository"/> class.
		/// </summary>
		internal CoreGlobalWritingSystemRepository(string basePath)
			: base(basePath)
		{
		}

		/// <summary>
		/// Creates the writing system factory.
		/// </summary>
		/// <returns></returns>
		protected override IWritingSystemFactory<CoreWritingSystemDefinition> CreateWritingSystemFactory()
		{
			return new CoreSldrWritingSystemFactory();
		}
	}
}

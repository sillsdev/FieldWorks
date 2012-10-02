namespace SIL.FieldWorks.FDO.Infrastructure
{
	/// <summary>
	/// Domain to have bulk loaded by the backend provider
	/// </summary>
	public enum BackendBulkLoadDomain
	{
		/// <summary>WFI and WfiWordforms</summary>
		WFI,
		/// <summary>Lexicon and entries.</summary>
		Lexicon,
		/// <summary>Texts and their paragraphs (no wordforms)</summary>
		Text,
		/// <summary></summary>
		Scripture,
		/// <summary>Load everything.</summary>
		All,
		/// <summary>Strictly 'on demand' loading</summary>
		None
	}

	/// <summary>
	/// Supported backend data providers.
	/// </summary>
	public enum FDOBackendProviderType
	{
		/// <summary>
		/// An invalid type
		/// </summary>
		kInvalid,

		/// <summary>
		/// A FieldWorks XML file.
		/// </summary>
		/// <remarks>uses XMLBackendProvider</remarks>
		kXML,

		/// <summary>
		/// A mostly 'do nothing' backend.
		/// This backend is used where there is no actual backend data store on the hard drive.
		/// This could be used for tests, for instance, that create all FDO test data themselves.
		/// </summary>
		/// <remarks>uses MemoryOnlyBackendProvider</remarks>
		kMemoryOnly,

#if USING_MERCURIALBACKEND
		/// <summary>
		/// </summary>
		kMercurial = 5,
#endif

		/// <summary>
		/// Attempt at using Git DVCS as a back end. Makes use of Git's ability to store
		/// blobs.
		/// </summary>
		kGit = 6,

#if USING_XMLFILES
		/// <summary>
		/// Multiple XML files
		/// </summary>
		/// <remarks>XMLFilesBackendProvider</remarks>
		kXmlFiles = 7,
#endif

#if USING_MYSQL
		/// <summary>
		/// A client/server MySQL database, with a MyISAM engine.
		/// </summary>
		/// <remarks>MySQLClientServer</remarks>
		kMySqlClientServer = 101,

		/// <summary>
		/// A client/server MySQL database, with an InnoDB engine.
		/// </summary>
		/// <remarks>MySQLClientServer</remarks>
		kMySqlClientServerInnoDB = 102,
#endif

		/// <summary>
		/// A FieldWorks XML file.
		/// This has an actual backend data store on the hard drive, but does not use a real
		/// repository of writing systems. There is probably no legitimate reason to use this
		/// except for testing the XML BEP.
		/// </summary>
		/// <remarks>uses XMLBackendProvider</remarks>
		kXMLWithMemoryOnlyWsMgr,

		/// <summary>
		/// A db4o client/server database
		/// </summary>
		/// <remarks>db4oClientServer</remarks>
		kDb4oClientServer = 103,
	};

	/// <summary>
	/// Enumeration of types or main object properties.
	/// </summary>
	internal enum ObjectPropertyType
	{
		Owning,
		Reference
	} ;

	/// <summary>
	/// Enumeration of DVCS types
	/// </summary>
	public enum DvcsType
	{
		/// <summary>
		/// No DVCS is being used
		/// </summary>
		None = 0,

		/// <summary>
		/// Mercurial
		/// </summary>
		Mercurial = 1, //FDOBackendProviderType.kMercurial,

		/// <summary>
		/// Git
		/// </summary>
		Git = 2, //FDOBackendProviderType.kGit
	} ;


}

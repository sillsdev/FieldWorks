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

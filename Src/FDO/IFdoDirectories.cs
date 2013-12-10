namespace SIL.FieldWorks.FDO
{
	/// <summary>
	/// This interface is used by FDO to retrieve directories that it needs.
	/// </summary>
	public interface IFdoDirectories
	{
		/// <summary>
		/// Gets the projects directory.
		/// </summary>
		string ProjectsDirectory { get; }

		/// <summary>
		/// Gets the template directory.
		/// </summary>
		string TemplateDirectory { get; }
	}
}

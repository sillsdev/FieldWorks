// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

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
		/// Gets the default projects directory.
		/// </summary>
		string DefaultProjectsDirectory { get; }

		/// <summary>
		/// Gets the template directory.
		/// </summary>
		string TemplateDirectory { get; }
	}
}

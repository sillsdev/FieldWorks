// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.LCModel
{
	/// <summary>
	/// This interface is used by LCM to retrieve directories that it needs.
	/// </summary>
	public interface ILcmDirectories
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

// Copyright (c) 2013-2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO
{
	/// <summary>
	/// Exception thrown when we try to open a project that belongs to a newer version of FieldWorks than this.
	/// </summary>
	public class FdoNewerVersionException : StartupException
	{
		/// <summary>
		/// Make one.
		/// </summary>
		public FdoNewerVersionException(string message) : base(message)
		{
		}
	}
}
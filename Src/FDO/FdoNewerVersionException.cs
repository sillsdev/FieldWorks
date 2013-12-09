// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2013, SIL International. All Rights Reserved.
// <copyright from='2013' to='2013' company='SIL International'>
//		Copyright (c) 2013, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
// ---------------------------------------------------------------------------------------------
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
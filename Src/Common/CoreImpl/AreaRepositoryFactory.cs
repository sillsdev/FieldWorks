// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.CoreImpl.Impls;

namespace SIL.CoreImpl
{
	/// <summary>
	/// Factory that creates an implementation of the IAreaRepository interface.
	/// </summary>
	public static class AreaRepositoryFactory
	{
		/// <summary>
		/// Create an implementation of the IAreaRepository interface.
		/// </summary>
		/// <returns></returns>
		public static IAreaRepository CreateAreaRepository()
		{
			return new AreaRepository();
		}
	}
}
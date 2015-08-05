// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using SIL.CoreImpl.Impls;

namespace SIL.CoreImpl
{
	/// <summary>
	/// Create an implementation of the IPropertyTable interface.
	/// </summary>
	public static class PropertyTableFactory
	{
		/// <summary>
		/// Create an implementation of IPropertyTable
		/// </summary>
		/// <param name="publisher">IPublisher instance that is used by the instance to publish select property changes.</param>
		/// <returns></returns>
		public static IPropertyTable CreatePropertyTable(IPublisher publisher)
		{
			return new PropertyTable(publisher);
		}
	}
}
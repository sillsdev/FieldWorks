// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.FwUtils.Impls;

namespace SIL.FieldWorks.Common.FwUtils
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
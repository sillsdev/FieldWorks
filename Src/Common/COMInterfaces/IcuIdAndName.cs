using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.FieldWorks.Common.COMInterfaces
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Simple class to carry name and ID (canonical name) strings for converters,
	/// transliterators, etc.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class IcuIdAndName
	{
		/// <summary>
		/// Id (canonical name) of the converter, transliterator, etc.
		/// </summary>
		public string Id { get; private set; }
		/// <summary>
		/// Name (non-canonical) of the converter, transliterator, etc.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="id"></param>
		/// <param name="name"></param>
		public IcuIdAndName(string id, string name)
		{
			Id = id;
			Name = name;
		}
	}
}

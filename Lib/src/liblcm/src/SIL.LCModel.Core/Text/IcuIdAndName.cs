// Copyright (c) 2009-2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.LCModel.Core.Text
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
		public string Id { get; }
		/// <summary>
		/// Name (non-canonical) of the converter, transliterator, etc.
		/// </summary>
		public string Name { get; }

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

// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections;
using System.Collections.Generic;
using SIL.Code;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// This class allows an array of bytes to be enumerated as a collection of characters
	/// </summary>
	internal sealed class CharEnumeratorForByteArray : IEnumerable<char>
	{
		private readonly byte[] _data;

		/// <summary />
		internal CharEnumeratorForByteArray(byte[] data)
		{
			Guard.AgainstNull(data, nameof(data));

			_data = data;
		}

		#region IEnumerable<char> Members

		/// <inheritdoc />
		IEnumerator<char> IEnumerable<char>.GetEnumerator()
		{
			for (var i = 0; i < _data.Length - 1; i += 2)
			{
				// ENHANCE: Need to change the byte order for Mac (or other big-endian) if we
				// support them
				yield return (char)(_data[i] | _data[i + 1] << 8);
			}

			if ((_data.Length & 1) != 0)
			{
				// We had an odd number of bytes, so return the last byte
				yield return (char)_data[_data.Length - 1];
			}
		}
		#endregion

		#region IEnumerable Members

		/// <inheritdoc />
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<char>)this).GetEnumerator();
		}
		#endregion
	}
}
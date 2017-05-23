// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.CoreImpl.KernelInterfaces;

namespace SIL.CoreImpl.Text
{
	/// <summary>
	/// This class represents a factory for creating <see cref="TsString"/> instances.
	/// </summary>
	public class TsStrFactory : ITsStrFactory
	{
		/// <summary>
		/// Creates a simple <see cref="ITsString"/> with the specified string and writing system.
		/// </summary>
		public ITsString MakeString(string bstr, int ws)
		{
			ThrowIfWSInvalid("ws", ws);

			if (string.IsNullOrEmpty(bstr))
				return EmptyString(ws);

			return new TsString(bstr, ws);
		}

		/// <summary>
		/// Creates a simple <see cref="ITsString"/> with the specified string, length, and writing system.
		/// </summary>
		public ITsString MakeStringRgch(string rgch, int cch, int ws)
		{
			ThrowIfStrLenOutOfRange("cch", cch, rgch == null ? 0 : rgch.Length);
			ThrowIfWSInvalid("ws", ws);

			return new TsString(rgch == null ? null : rgch.Substring(0, cch), ws);
		}

		/// <summary>
		/// Creates a simple <see cref="ITsString"/> with the specified string, length, and text properties.
		/// </summary>
		public ITsString MakeStringWithPropsRgch(string rgch, int cch, ITsTextProps ttp)
		{
			ThrowIfStrLenOutOfRange("cch", cch, rgch == null ? 0 : rgch.Length);
			if (ttp == null)
				throw new ArgumentNullException("ttp");

			return new TsString(rgch == null ? null : rgch.Substring(0, cch), (TsTextProps) ttp);
		}

		/// <summary>
		/// Creates an empty string builder.
		/// </summary>
		public ITsStrBldr GetBldr()
		{
			return new TsStrBldr();
		}

		/// <summary>
		/// Creates an empty incremental string builder.
		/// </summary>
		public ITsIncStrBldr GetIncBldr()
		{
			return new TsIncStrBldr();
		}

		/// <summary>
		/// Creates an empty string with the specified writing system.
		/// </summary>
		public ITsString EmptyString(int ws)
		{
			ThrowIfWSInvalid("ws", ws);

			return TsString.GetInternedEmptyString(ws);
		}

		private void ThrowIfWSInvalid(string paramName, int ws)
		{
			// TODO: should we support magic writing system codes?
			if (ws <= 0)
				throw new ArgumentOutOfRangeException(paramName);
		}

		private void ThrowIfStrLenOutOfRange(string paramName, int cch, int strLen)
		{
			if (cch < 0 || cch > strLen)
				throw new ArgumentOutOfRangeException(paramName);
		}
	}
}

// Copyright (c) 2017-2020 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;
using System.Text;

namespace SIL.FieldWorks.UnicodeCharEditor
{
	/// <inheritdoc />
	public abstract class UceException : Exception
	{
		/// <summary />
		protected readonly ErrorCodes m_errorCode;
		/// <summary />
		protected readonly string m_msg;

		/// <inheritdoc />
		protected UceException(ErrorCodes errorCode, string msg)
		{
			m_errorCode = errorCode;
			m_msg = msg;
		}

		/// <inheritdoc />
		public override string Message
		{
			get
			{
				var bldr = new StringBuilder();
				bldr.AppendLine(m_errorCode.GetDescription());
				if (!string.IsNullOrEmpty(m_msg))
				{
					bldr.Append(m_msg);
				}
				return bldr.ToString();
			}
		}
	}
}

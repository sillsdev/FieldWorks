// Copyright (c) 2017 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;
using System.Text;

namespace SIL.FieldWorks.UnicodeCharEditor
{
	/// <summary/>
	public abstract class UceException: Exception
	{
		/// <summary/>
		protected readonly ErrorCodes m_errorCode;
		/// <summary/>
		protected readonly string m_msg;

		///<summary>
		/// Constructor with a message.
		///</summary>
		protected UceException(ErrorCodes errorCode, string msg)
		{
			m_errorCode = errorCode;
			m_msg = msg;
		}

		/// <summary>
		/// Gets a message that describes the current exception.
		/// </summary>
		public override string Message
		{
			get
			{
				var bldr = new StringBuilder();
				bldr.AppendLine(m_errorCode.GetDescription());
				if (!string.IsNullOrEmpty(m_msg))
					bldr.Append(m_msg);
				return bldr.ToString();
			}
		}
	}
}

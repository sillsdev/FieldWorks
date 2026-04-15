// Copyright (c) 2020-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.FwUtils
{
	public sealed class PublisherParameterObject
	{
		public PublisherParameterObject(string message, object data = null)
		{
			if (string.IsNullOrWhiteSpace(message))
			{
				throw new ArgumentNullException(message, nameof(message));
			}

			Message = message;
			Data = data;
		}

		public string Message { get; }
		public object Data { get; }
	}

	/// <summary>
	/// Convenience class for use when we want to pass a return value back through the PublisherParameterObject.
	/// </summary>
	public class ReturnObject
	{
		public ReturnObject(object data)
		{
			Data = data;
			ReturnValue = false;
		}

		public object Data { get; set; }
		public bool ReturnValue { get; set; }
	}
}
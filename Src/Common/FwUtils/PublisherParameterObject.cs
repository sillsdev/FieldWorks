// Copyright (c) 2020-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.FwUtils
{
	public sealed class PublisherParameterObject
	{
		public PublisherParameterObject(string message, object newValue = null)
		{
			if (string.IsNullOrWhiteSpace(message))
			{
				throw new ArgumentNullException(message, nameof(message));
			}

			Message = message;
			NewValue = newValue;
		}

		public string Message { get; }
		public object NewValue { get; }
	}
}
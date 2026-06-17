// Copyright (c) 2020-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.FwUtils
{
	public sealed class PublisherParameterObject
	{
		public PublisherParameterObject(string message, object data, IPubSubScope scope)
		{
			if (string.IsNullOrWhiteSpace(message))
			{
				throw new ArgumentNullException(message, nameof(message));
			}

			Message = message;
			Data = data;
			Scope = scope;
		}

		public string Message { get; }
		public object Data { get; }

		/// <summary>
		/// Delivery scope (normally the publishing main window). When non-null, only
		/// subscribers that subscribed with the same scope (or with none) receive this publish.
		/// A null scope publishes to all subscribers, regardless of their scope. See <see cref="IPubSubScope"/>.
		/// </summary>
		public IPubSubScope Scope { get; }
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
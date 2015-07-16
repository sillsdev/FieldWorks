// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using SIL.CoreImpl;

namespace SIL.FieldWorks.Test.TestUtils
{
	/// <summary>
	/// Mocked IPublisher impl
	/// </summary>
	public class MockPublisher : IPublisher
	{
		#region Implementation of IPublisher

		/// <summary>
		/// Publish the message using the new value.
		/// </summary>
		/// <param name="message">The message to publish.</param>
		/// <param name="newValue">The new value to send to subscribers. This may be null.</param>
		public void Publish(string message, object newValue)
		{
		}

		#endregion
	}
}
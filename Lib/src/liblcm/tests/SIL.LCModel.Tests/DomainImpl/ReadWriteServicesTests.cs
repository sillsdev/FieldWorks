// Copyright (c) 2017 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)

using System.IO;
using System.Xml.Linq;
using NUnit.Framework;

namespace SIL.LCModel.DomainImpl
{
	/// <summary>
	/// Read write services tests.
	/// </summary>
	[TestFixture]
	public class ReadWriteServicesTests
	{
		/// <summary>
		/// Tests the LoadDateTime method
		/// </summary>
		[TestCase("2016-7-7 4:2:2", Result = "2016-07-07T04:02:02.0000000Z")]
		[TestCase("2016-7-7 4:2:2.0", Result = "2016-07-07T04:02:02.0000000Z")]
		[TestCase("2016-7-7 4:2:2.007", Result = "2016-07-07T04:02:02.0070000Z")]
		[TestCase("2016-07-07 04:02:02.700", Result = "2016-07-07T04:02:02.7000000Z")]
		[TestCase("2016-7-7 4:2:2.700", Result = "2016-07-07T04:02:02.7000000Z")]
		// NOTE: the current code interprets the next two test cases incorrectly (LT-18205)
		[TestCase("2016-7-7 4:2:2.70", Result = "2016-07-07T04:02:02.0700000Z")] // should really be "2016-07-07T04:02:02.7000000Z"
		[TestCase("2016-7-7 4:2:2.7", Result = "2016-07-07T04:02:02.0070000Z")] // should really be "2016-07-07T04:02:02.7000000Z"
		public string LoadDateTime(string datetime)
		{
			using (var stringReader = new StringReader(string.Format("<DateModified val=\"{0}\"/>", datetime)))
			{
				var reader = XElement.Load(stringReader);

				return ReadWriteServices.LoadDateTime(reader).ToUniversalTime().ToString("O");
			}
		}
	}
}

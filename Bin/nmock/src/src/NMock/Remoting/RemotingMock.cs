// Copyright c 2002, Joe Walnes, Chris Stevenson, Owen Rogers
// See LICENSE.txt for details.

using System;
using NMock.Dynamic;
using System.Collections;

namespace NMock.Remoting
{
	public class RemotingMock : DynamicMock
	{
		public RemotingMock(Type type) : base(type, "Mock" + type.Name, typeof(MarshalByRefObject)) { }

		public MarshalByRefObject MarshalByRefInstance
		{
			get
			{
				return (MarshalByRefObject)MockInstance;
			}
		}
	}
}

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

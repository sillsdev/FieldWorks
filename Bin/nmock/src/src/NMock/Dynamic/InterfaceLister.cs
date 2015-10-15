// Copyright c 2002, Joe Walnes, Chris Stevenson, Owen Rogers
// See LICENSE.txt for details.

using System;
using System.Collections;
using System.Reflection;

namespace NMock.Dynamic
{

	// List all interfaces implemented by an interface (includes classes)
	public class InterfaceLister
	{
		public Type[] List(Type i)
		{
			ArrayList found = new ArrayList();
			walk(found, i);
			return (Type[]) found.ToArray(typeof(Type));
		}

		private void walk(IList found, Type current)
		{
			if (current == null || current == typeof(Object))
			{
				return;
			}
			add(found, current);
			foreach(Type superType in current.GetInterfaces())
			{
				add(found, superType);
			}
			walk(found, current.BaseType);
		}

		private void add(IList found, Type item)
		{
			if (!found.Contains(item))
			{
				found.Add(item);
			}
		}
	}

}
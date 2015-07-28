// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.FieldWorks.FDO.Infrastructure.Impl
{
	/// <summary>
	/// This class allows the creation of hash sets of ICmObjectOrSurrogate which treat objects and surrogates
	/// with the same ID as equal. Such sets should always use this equater.
	/// </summary>
	class ObjectSurrogateEquater: IEqualityComparer<ICmObjectOrSurrogate>
	{
		public bool Equals(ICmObjectOrSurrogate x, ICmObjectOrSurrogate y)
		{
			return x.Id.Equals(y.Id);
		}

		public int GetHashCode(ICmObjectOrSurrogate obj)
		{
			return obj.Id.GetHashCode();
		}
	}

	/// <summary>
	/// This class implements just enough of ICmObjectOrSurrogate so that it can be used to test
	/// whether a set of them contains a particular ID. Only the Id method is (or should be) implemented.
	/// </summary>
	internal class IdSurrogateWrapper : ICmObjectOrSurrogate
	{
		private ICmObjectId m_id;
		public IdSurrogateWrapper(ICmObjectId id)
		{
			m_id = id;
		}

		public string XML
		{
			get { throw new NotImplementedException(); }
		}

		public ICmObjectId Id
		{
			get { return m_id; }
		}

		public string Classname
		{
			get { throw new NotImplementedException(); }
		}

		public byte[] XMLBytes
		{
			get { throw new NotImplementedException(); }
		}

		#region ICmObjectOrSurrogate Members


		public ICmObject Object
		{
			get { throw new NotImplementedException(); }
		}

		public bool HasObject
		{
			get { throw new NotImplementedException(); }
		}

		#endregion
	}
}

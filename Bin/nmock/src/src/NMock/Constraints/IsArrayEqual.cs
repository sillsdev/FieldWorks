// Contributed by Luke Maxon <ltmaxon@thoughtworks.com>

using System;
using System.Collections;

namespace NMock.Constraints
{

	public class IsListEqual : BaseConstraint
	{
		private object compare;

		public IsListEqual(object compare)
		{
			this.compare = compare;
		}

		public override bool Eval(object val)
		{
			IList thisList = compare as IList;
			IList thatList = val as IList;

			if(thisList == null || thatList == null)
			{
				return false;
			}

			if(thisList.Count != thatList.Count)
			{
				return false;
			}

			for(int i=0;i<thisList.Count;++i)
			{
				if(!thisList[i].Equals(thatList[i]))
				{
					return false;
				}
			}

			return true;
		}

		public override string Message
		{
			get { return "<" + compare + ">"; }
		}
	}

}
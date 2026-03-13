// Copyright (c) 2022-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SIL.AlloGenModel
{
	public class Operation : AlloGenBase
	{
		public string Name { get; set; } = "new operation";
		public string Description { get; set; } = "";
		public Pattern Pattern { get; set; } = new Pattern();
		public Action Action { get; set; } = new Action();

		public Operation() { }

		public Operation Duplicate()
		{
			Operation newOp = new Operation();
			newOp.Action = Action.Duplicate();
			newOp.Active = Active;
			newOp.Description = Description;
			newOp.Name = Name;
			newOp.Pattern = Pattern.Duplicate();
			return newOp;
		}

		override public string ToString()
		{
			return Name;
		}

		public override bool Equals(Object obj)
		{
			if (!base.Equals(obj))
				return false;

			//Check for null and compare run-time types.
			if ((obj == null) || !this.GetType().Equals(obj.GetType()))
				return false;
			else
			{
				Operation op = (Operation)obj;
				return (Name == op.Name)
					&& (Description == op.Description)
					&& (Pattern.Equals(op.Pattern))
					&& (Action.Equals(op.Action));
			}
		}

		public override int GetHashCode()
		{
			int result = base.GetHashCode();
			return result + Tuple.Create(Name, Description, Pattern, Action).GetHashCode();
		}
	}
}

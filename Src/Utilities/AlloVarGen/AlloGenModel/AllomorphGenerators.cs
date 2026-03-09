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
	public class AllomorphGenerators
	{
		[XmlAttribute("dbVersion")]
		public int DbVersion { get; set; } = 6;

		[XmlAttribute("applyTo")]
		public int ApplyTo { get; set; } = -1;
		public List<Operation> Operations { get; set; } = new List<Operation>();
		public List<Replace> ReplaceOperations { get; set; } = new List<Replace>();
		public List<WritingSystem> WritingSystems { get; set; } = new List<WritingSystem>();

		public AllomorphGenerators() { }

		public bool ReplaceOpExists(Replace replace)
		{
			if (replace == null)
			{
				return false;
			}
			return ReplaceOperations.Contains(replace);
		}

		public void AddReplaceOp(Replace replace)
		{
			if (replace != null)
			{
				if (ReplaceOpExists(replace))
				{
					ReplaceOperations.Remove(replace);
				}
				ReplaceOperations.Add(replace);
			}
		}

		public void DeleteReplaceOp(Replace replace)
		{
			if (ReplaceOpExists(replace))
			{
				DeleteReplaceOpRefs(replace);
				ReplaceOperations.Remove(replace);
			}
		}

		private void DeleteReplaceOpRefs(Replace replace)
		{
			foreach (Operation op in Operations)
			{
				AlloGenModel.Action action = op.Action;
				foreach (string replaceOpRef in action.ReplaceOpRefs.ToList())
				{
					if (replaceOpRef == replace.Guid)
					{
						action.ReplaceOpRefs.Remove(replaceOpRef);
					}
				}
			}
		}

		public Replace FindReplaceOp(string guid)
		{
			Replace replace = ReplaceOperations.FirstOrDefault(r => r.Guid == guid);
			return replace;
		}

		public List<Operation> FindOperationsUsedByReplaceOp(Replace replace)
		{
			List<Operation> operations = new List<Operation>();
			operations = Operations.FindAll(op => op.Action.ReplaceOpRefs.Contains(replace.Guid));
			return operations;
		}

		public Operation CreateNewOperation()
		{
			Operation op = new Operation();
			Operations.Add(op);
			Replace newReplace = new Replace();
			if (WritingSystems != null)
			{
				foreach (WritingSystem ws in WritingSystems)
				{
					newReplace.WritingSystemRefs.Add(ws.Name);
				}
			}
			ReplaceOperations.Add(newReplace);
			op.Action.ReplaceOpRefs.Add(newReplace.Guid);
			op.Pattern.SetDefaultMorphTypes();
			return op;
		}

		public void DeleteEmptyReplaceOperationsFromAnOperation(Operation op)
		{
			if (op == null)
				return;
			foreach (string repOpRef in op.Action.ReplaceOpRefs)
			{
				Replace rep = ReplaceOperations.FirstOrDefault(r => r.Guid == repOpRef);
				if (rep != null && rep.IsEmpty())
				{
					ReplaceOperations.Remove(rep);
				}
			}
		}
	}
}

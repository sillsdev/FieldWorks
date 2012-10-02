using System;

namespace NMock
{
	public class MethodSignature
	{
		public readonly string typeName;
		public readonly string methodName;
		public readonly string[] argumentTypes;

		public MethodSignature(string typeName, string methodName, Type[] argTypes)
		{
			this.typeName = typeName;
			this.methodName = methodName;
			this.argumentTypes = new string[argTypes.Length];
			for (int i = 0; i < argTypes.Length; i++)
				argumentTypes[i] = argTypes[i].FullName;
		}

		public MethodSignature(string typeName, string methodName, string[] argumentTypes)
		{
			this.typeName = typeName;
			this.methodName = methodName;
			this.argumentTypes = argumentTypes;
			if (argumentTypes == null)
				this.argumentTypes = new string[0];
		}

		public override string ToString()
		{
			return typeName + "." + methodName + "()";
		}

		public override bool Equals(object obj)
		{
			MethodSignature sig = obj as MethodSignature;

			if (sig != null)
			{
				if (sig.typeName != typeName
					|| sig.methodName != methodName
					|| sig.argumentTypes.Length != argumentTypes.Length)
					return false;

				for (int i = 0; i < argumentTypes.Length; i++)
				{
					if (argumentTypes[i] != sig.argumentTypes[i])
						return false;
				}

				return true;
			}

			return false;
		}

		public override int GetHashCode()
		{
			return typeName.GetHashCode() ^ methodName.GetHashCode()
				^ argumentTypes.GetHashCode();
		}
	}
}

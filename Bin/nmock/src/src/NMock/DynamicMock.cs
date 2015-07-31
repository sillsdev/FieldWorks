// Copyright c 2002, Joe Walnes, Chris Stevenson, Owen Rogers
// See LICENSE.txt for details.

using System;
using System.Collections;
using System.Reflection;
using NMock.Dynamic;
using NMock.Constraints;

namespace NMock
{
	public class DynamicMock : Mock
	{

		private object mockInstance;
		private Type type;
		private IList ignoredMethodNames;
		private readonly Type superclassIfTypeIsInterface;
		private string[] additionalReferences;

		public DynamicMock(Type type) : this(type, "Mock" +  type.Name) {}

		public DynamicMock(Type type, string name) : this (type, name, null) {}

		public DynamicMock(Type type, string name, Type superclassIfTypeIsInterface) : base(name)
		{
			this.ignoredMethodNames = new ArrayList();
			this.type = type;
			this.superclassIfTypeIsInterface = superclassIfTypeIsInterface;
		}

		public string[] AdditionalReferences
		{
			get { return additionalReferences; }
			set { additionalReferences = value; }
		}

		public override object MockInstance
		{
			get
			{
				if (mockInstance == null)
				{
					mockInstance = CreateClassGenerator().Generate();
				}
				return mockInstance;
			}
		}

		/// <summary>
		/// Don't generate mock method for supplied methodName.
		/// </summary>
		public virtual void Ignore(string methodName)
		{
			ignoredMethodNames.Add(methodName);
		}

		private ClassGenerator CreateClassGenerator()
		{
			return new ClassGenerator(type, this, ignoredMethodNames,
				superclassIfTypeIsInterface, additionalReferences);
		}

		protected override IMethod getMethod(MethodSignature signature)
		{
			checkMethodIsValidIfNoConstraints(signature);

			return base.getMethod(signature);
		}

		public override void SetupResult(string methodName, object returnVal, params Type[] argTypes)
		{
			MethodSignature signature = new MethodSignature(Name, methodName, argTypes);
			checkMethodIsValidIfNoConstraints(signature);
			checkReturnTypeIsValid(signature, returnVal);
			base.SetupResult(methodName, returnVal, argTypes);
		}

		void checkReturnTypeIsValid(MethodSignature signature, object returnVal)
		{
			if (returnVal == null)
			{
				return;
			}

			Type realReturnVal;
			MethodInfo method;
			PropertyInfo property;
			FindMethodOrProperty(type, signature, out method, out property);
			if (method == null)
			{
				if (property == null)
					throw new ArgumentException(string.Format("Method/property <{0}> not found", signature.methodName));
				else
					realReturnVal = property.PropertyType;
			}
			else
				realReturnVal = method.ReturnType;


			if (realReturnVal == null)
			{
				realReturnVal = GetPropertyHelper(type, signature).PropertyType;
			}

			if (realReturnVal != returnVal.GetType() && !realReturnVal.IsAssignableFrom(returnVal.GetType())
				&& !realReturnVal.IsInstanceOfType(returnVal))
			{
				throw new ArgumentException(String.Format("method <{0}> returns a {1}", signature.methodName, realReturnVal));
			}
		}

		void checkMethodIsValidIfNoConstraints(MethodSignature signature)
		{
			Type[] allTypes = new InterfaceLister().List(type);
			foreach (Type t in allTypes)
			{
				MethodInfo method;
				PropertyInfo property;
				FindMethodOrProperty(t, signature, out method, out property);
				if(method != null)
				{
					if(!method.IsVirtual)
					{
						string message;
						if (property != null)
							message = string.Format("Property <{0}> is not virtual", signature.methodName);
						else
							message = string.Format("Method <{0}> is not virtual", signature.methodName);
						throw new ArgumentException(message);
					}
					return;
				}
			}

			foreach(string argTypeStr in signature.argumentTypes)
			{
				if(typeof(IConstraint).IsAssignableFrom(Type.GetType(argTypeStr)))
					return;
			}

			throw new MissingMethodException(String.Format("method <{0}> not defined", signature.methodName));
		}

		private bool MethodFilter(MemberInfo thisMember, object match)
		{
			MethodSignature signature = (MethodSignature)match;
			if (thisMember.Name == signature.methodName)
			{
				MethodInfo info = thisMember as MethodInfo;
				ParameterInfo[] allParams = info.GetParameters();
				if (allParams.Length != signature.argumentTypes.Length && (allParams.Length == 0
					|| !allParams[allParams.Length - 1].IsDefined(typeof(ParamArrayAttribute), false)))
					return false;

				for (int i = 0; i < signature.argumentTypes.Length; i++)
				{
					Type sigType = Type.GetType(signature.argumentTypes[i]);
					if (signature.argumentTypes[i] != allParams[i].ParameterType.FullName
						&& !typeof(IConstraint).IsAssignableFrom(sigType)
						&& !allParams[i].ParameterType.IsAssignableFrom(sigType))
						return false;
				}
				return true;
			}
			return false;
		}

		private MethodInfo GetMethodHelper(Type type, MethodSignature signature)
		{
			MemberInfo[] methods = type.FindMembers(MemberTypes.Method,
				ClassGenerator.ALL_INSTANCE_METHODS,
				new MemberFilter(MethodFilter), signature);

			if (methods != null && methods.Length == 1)
				return methods[0] as MethodInfo;

			return null;
		}

		private bool PropertyFilter(MemberInfo thisMember, object match)
		{
			MethodSignature signature = (MethodSignature)match;
			if (thisMember.Name == signature.methodName)
			{
				return true;
			}
			return false;
		}

		private PropertyInfo GetPropertyHelper(Type type, MethodSignature signature)
		{
			MemberInfo[] properties = type.FindMembers(MemberTypes.Property,
				ClassGenerator.ALL_INSTANCE_METHODS,
				new MemberFilter(PropertyFilter), signature);

			if (properties != null && properties.Length == 1)
				return properties[0] as PropertyInfo;

			return null;
		}

		private void FindMethodOrProperty(Type t, MethodSignature signature,
			out MethodInfo method, out PropertyInfo property)
		{
			method = GetMethodHelper(t, signature);
			property = GetPropertyHelper(t, signature);
			if (property != null)
			{
				method = null;
				if (property.CanRead)
				{
					method = t.GetMethod("get_" + signature.methodName,
						ClassGenerator.ALL_INSTANCE_METHODS);
				}
				if (property.CanWrite && method == null)
				{
					method = t.GetMethod("set_" + signature.methodName,
						ClassGenerator.ALL_INSTANCE_METHODS);
				}
			}
			else if (method == null)
			{
				if (signature.argumentTypes.Length == 1)
				{
					// try to find a set_ method
					MethodSignature tmpSignature = new MethodSignature(signature.typeName,
						"set_" + signature.methodName, signature.argumentTypes);
					method = GetMethodHelper(t, tmpSignature);
				}
				else
				{
					// try to find a get_ method
					MethodSignature tmpSignature = new MethodSignature(signature.typeName,
						"get_" + signature.methodName, signature.argumentTypes);
					method = GetMethodHelper(t, tmpSignature);
				}
			}
		}
	}
}

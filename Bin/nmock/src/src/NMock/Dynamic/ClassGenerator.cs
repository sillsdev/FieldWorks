using System;
using System.Collections;
using System.CodeDom.Compiler;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.CSharp;
using Microsoft.Win32;

namespace NMock.Dynamic
{

	public class ClassGenerator
	{
		public const string INVOCATION_HANDLER_FIELD_NAME = "_invocationHandler";
		public const string METHODS_TO_IGNORE_FIELD_NAME = "_methodsToIgnore";

		internal const System.Reflection.BindingFlags ALL_INSTANCE_METHODS
			= BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

		readonly protected Type type;
		readonly protected IInvocationHandler handler;
		protected IList methodsToCompletelyIgnore;
		readonly protected IList methodsToIgnore;
		readonly protected Type superclassIfTypeIsInterface;

		private bool m_fGotProperty = false;
		/// <summary>Create a file with the source code we generate. Useful for debugging.</summary>
		private static bool s_fCreateSourceFile = false;
		private string m_LastProperty;
		private ArrayList m_Methods = new ArrayList();
		private static Hashtable m_Assemblies = new Hashtable();
		private string[] m_additonalReferences;
		private string s_piaPath;
		private string s_piaPath35;
		private string s_wcfPath30;

		public ClassGenerator(Type type, IInvocationHandler handler)
			: this(type, handler, new ArrayList()) {}

		public ClassGenerator(Type type, IInvocationHandler handler, IList methodsToIgnore)
			: this(type, handler, methodsToIgnore, null) {}

		public ClassGenerator(Type type, IInvocationHandler handler, IList methodsToIgnore,
			Type superclassIfTypeIsInterface)
			: this(type, handler, methodsToIgnore, superclassIfTypeIsInterface, null)
		{
		}

		public ClassGenerator(Type type, IInvocationHandler handler, IList methodsToIgnore,
			Type superclassIfTypeIsInterface, string[] additionalReferences)
		{
			if (s_piaPath35 == null)
			{
				RegistryKey key = Registry.LocalMachine.OpenSubKey(
						@"SOFTWARE\Microsoft\.NETFramework\AssemblyFolders\Microsoft .NET Framework 3.5 Reference Assemblies");
				if (key == null)
					System.Diagnostics.Trace.WriteLineIf(Environment.OSVersion.Platform != PlatformID.Unix,
						"Can't find path to .Net 3.5 Primary Interop Assemblies - build might not work correctly");
				else
					s_piaPath35 = (string)key.GetValue("");
			}

			if (s_piaPath == null)
			{
				// try to find the path where the Primary Interop Assemblies are stored
				// - unfortunately this can be at quite different locations!
				RegistryKey key = Registry.CurrentUser.OpenSubKey(
						@"SOFTWARE\Microsoft\.NETFramework\AssemblyFolders\Primary Interop Assemblies");
				if (key == null)
					key = Registry.LocalMachine.OpenSubKey(
						@"SOFTWARE\Microsoft\VisualStudio\8.0\AssemblyFolders\Primary Interop Assemblies");
				if (key == null)
					key = Registry.CurrentUser.OpenSubKey(
						@"SOFTWARE\Microsoft\VisualStudio\8.0\AssemblyFolders\Primary Interop Assemblies");
				if (key == null)
					key = Registry.LocalMachine.OpenSubKey(
						@"SOFTWARE\Microsoft\.NETFramework\v2.0.50727\AssemblyFoldersEx\Primary Interop Assemblies");
				if (key == null)
					key = Registry.CurrentUser.OpenSubKey(
						@"SOFTWARE\Microsoft\.NETFramework\v2.0.50727\AssemblyFoldersEx\Primary Interop Assemblies");
				if (key == null)
					key = Registry.LocalMachine.OpenSubKey(
						@"SOFTWARE\Microsoft\VisualStudio\7.1\AssemblyFolders\Primary Interop Assemblies");
				if (key == null)
					key = Registry.CurrentUser.OpenSubKey(
						@"SOFTWARE\Microsoft\VisualStudio\7.1\AssemblyFolders\Primary Interop Assemblies");
				if (key == null)
					key = Registry.LocalMachine.OpenSubKey(
						@"SOFTWARE\Microsoft\VisualStudio\7.0\AssemblyFolders\Primary Interop Assemblies");
				if (key == null)
					key = Registry.CurrentUser.OpenSubKey(
						@"SOFTWARE\Microsoft\VisualStudio\7.0\AssemblyFolders\Primary Interop Assemblies");

				if (key == null)
				{
					System.Diagnostics.Trace.WriteLineIf(Environment.OSVersion.Platform != PlatformID.Unix,
						"Can't find path to legacy Primary Interop Assemblies - build might not work correctly");
				}
				else
					s_piaPath = (string)key.GetValue("");
			}

			if(s_wcfPath30 == null)
			{
				RegistryKey key = Registry.LocalMachine.OpenSubKey(
						@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\NET Framework Setup\NDP\v3.0\Setup\Windows Communication Foundation");
				if (key == null)
					key = Registry.LocalMachine.OpenSubKey(
						@"SOFTWARE\Microsoft\NET Framework Setup\NDP\v3.0\Setup\Windows Communication Foundation");
				if (key == null)
				{
					System.Diagnostics.Trace.WriteLineIf(Environment.OSVersion.Platform != PlatformID.Unix,
						"Can't find path to WCF - build might not work correctly");
				}
				else
					s_wcfPath30 = (string)key.GetValue("RuntimeInstallPath");
			}

			this.type = type;
			this.handler = handler;
			this.methodsToIgnore = methodsToIgnore;
			this.superclassIfTypeIsInterface = superclassIfTypeIsInterface;
			if (additionalReferences == null)
				m_additonalReferences = new string[0];
			else
				m_additonalReferences = additionalReferences;

			// the methods for which we don't generated stubs
			methodsToCompletelyIgnore = new ArrayList();
			methodsToCompletelyIgnore.Add("Equals");
			methodsToCompletelyIgnore.Add("ToString");
			methodsToCompletelyIgnore.Add("Finalize");
		}

		public virtual object Generate()
		{
			Assembly assembly;
			if (m_Assemblies.Contains(type.FullName))
			{
				assembly = (Assembly)m_Assemblies[type.FullName];
			}
			else
			{
				string filePrefix = "file://";
				if (Path.DirectorySeparatorChar == '\\')
					filePrefix = "file:///";

				string source = ImplementMethods();
				if (s_fCreateSourceFile)
				{
					string srcFileName =
						Path.Combine(Path.GetTempPath(), "Mock" + type.Name + ".cs");
					System.Diagnostics.Trace.WriteLine("Creating source file: " + srcFileName);
					StreamWriter writer = new StreamWriter(srcFileName);
					writer.Write(source);
					writer.Close();
				}

				CodeDomProvider compiler = new CSharpCodeProvider();
				CompilerParameters opts = new CompilerParameters();
				opts.GenerateInMemory = true;
				string referencedAssembly = Assembly.GetAssembly(type).CodeBase.Substring(filePrefix.Length);
				opts.ReferencedAssemblies.Add(referencedAssembly);
				string dir = Path.GetDirectoryName(referencedAssembly);
				referencedAssembly = Assembly.GetExecutingAssembly().CodeBase.Substring(filePrefix.Length);
				opts.ReferencedAssemblies.Add(referencedAssembly);
				opts.ReferencedAssemblies.Add("System.dll");
				foreach(AssemblyName reference in Assembly.GetAssembly(type).GetReferencedAssemblies())
				{
					// first look in the directory where the mocked assembly is
					string referencePath = Path.Combine(dir, reference.Name + ".dll");
					if (File.Exists(referencePath))
						opts.ReferencedAssemblies.Add(referencePath);
					else if (!string.IsNullOrEmpty(s_piaPath35) &&
						File.Exists(Path.Combine(s_piaPath35, reference.Name + ".dll")))
					{
						// then try in the ".Net 3.5 Primary interop assemblies" directory (for things
						// like System.Core.dll
						referencePath = Path.Combine(s_piaPath35, reference.Name + ".dll");
						opts.ReferencedAssemblies.Add(referencePath);
					}
					else if (!string.IsNullOrEmpty(s_wcfPath30) &&
						File.Exists(Path.Combine(s_wcfPath30, reference.Name + ".dll")))
					{
						// then try in the ".Net 3.5 Primary interop assemblies" directory (for things
						// like System.Core.dll
						referencePath = Path.Combine(s_wcfPath30, reference.Name + ".dll");
						opts.ReferencedAssemblies.Add(referencePath);
					}
					else
					{
						// then try in the "legacy Primary interop assemblies" directory (for things like
						// stdole.dll
						if (!string.IsNullOrEmpty(s_piaPath))
							referencePath = Path.Combine(s_piaPath, reference.Name + ".dll");
						if (File.Exists(referencePath))
							opts.ReferencedAssemblies.Add(referencePath);
						else
						{
							//Add the assembly hoping it is in the GAC
							opts.ReferencedAssemblies.Add(Path.GetFileName(referencePath));
						}
					}
				}
				foreach (string reference in m_additonalReferences)
				{
					if (File.Exists(reference))
						opts.ReferencedAssemblies.Add(reference);
					else
					{
						string referencePath = Path.Combine(dir, reference);
						if (File.Exists(referencePath))
							opts.ReferencedAssemblies.Add(referencePath);
						else
							opts.ReferencedAssemblies.Add(reference);
					}
				}

				CompilerResults results = compiler.CompileAssemblyFromSource(opts, source);

				if (results.Errors.HasErrors)
				{
					StringBuilder error = new StringBuilder();
					error.Append("Error compiling expression: ");
					foreach (CompilerError err in results.Errors)
						error.AppendFormat("{0}: {1} ({2}, {3})\n", err.ErrorNumber, err.ErrorText, err.Line, err.Column);
					System.Diagnostics.Debug.WriteLine(error.ToString());
					throw new ApplicationException(error.ToString());
				}
				else
				{
					assembly = results.CompiledAssembly;
					m_Assemblies.Add(type.FullName, assembly);
				}
			}
			object result = assembly.CreateInstance("MockModule." + ProxyClassName);

			Type proxyType = result.GetType();
			FieldInfo field = proxyType.GetField(INVOCATION_HANDLER_FIELD_NAME);
			field.SetValue(result, handler);
			field = proxyType.GetField(METHODS_TO_IGNORE_FIELD_NAME);
			field.SetValue(result, methodsToIgnore);

			return result;
		}

		private string ImplementMethods()
		{
			Assembly ass = Assembly.GetAssembly(type);
			StringBuilder source = new StringBuilder();

			if (!type.IsInterface && !type.IsAbstract
				&& type.GetConstructor(Type.EmptyTypes) == null)
				throw new NotSupportedException("Need constructor with empty parameter list");

			source.Append("using System;\n");
			source.Append("using NMock;\n");
			source.Append("namespace MockModule {\n");

			source.AppendFormat("public {0} {1}: {2}", type.IsClass || type.IsInterface ? "class" : "struct",
				ProxyClassName, ValidTypeName(ProxySuperClass));

			foreach (Type interfaceType in ProxyInterfaces)
			{
				if (interfaceType.FullName != type.FullName)
					source.AppendFormat(", {0}", ValidTypeName(interfaceType));
			}
			source.Append("{\n");
			source.AppendFormat("public IInvocationHandler " + INVOCATION_HANDLER_FIELD_NAME
				+ ";\n");
			source.AppendFormat("public System.Collections.IList " + METHODS_TO_IGNORE_FIELD_NAME
				+ ";\n");

			foreach (Type currentType in new InterfaceLister().List(type))
			{
				foreach ( MethodInfo methodInfo in GetSortedMethods(currentType))
				{
					if (ShouldImplement(methodInfo))
					{
						try
						{
							source.Append(ImplementMethod(methodInfo, currentType.IsInterface));
						}
						catch
						{
							// we simply ignore any exception and don't add the method
						}
					}
				}
			}
			if (m_fGotProperty)
			{ // we have a previous started property - better end that first
				m_fGotProperty = false;
				source.Append("}\n");
			}
			source.Append("}\n}\n");
			return source.ToString();
		}

		private class CompareMethodInfo : IComparer
		{
			#region IComparer Members

			int IComparer.Compare(object x, object y)
			{
				string xName = GetName((MethodInfo)x);
				string yName = GetName((MethodInfo)y);
				return xName.CompareTo(yName);
			}

			private string GetName(MethodInfo info)
			{
				if (info.Name.StartsWith("get_"))
					return info.Name.Substring(4) + "_get";
				else if (info.Name.StartsWith("set_"))
					return info.Name.Substring(4) + "_set";
				else
					return info.Name;
			}

			#endregion
		}

		private MethodInfo[] GetSortedMethods(Type currentType)
		{
			MethodInfo[] allMethods = currentType.GetMethods(ALL_INSTANCE_METHODS);
			Array.Sort(allMethods, new CompareMethodInfo());
			return allMethods;
		}

		private bool ShouldImplement(MethodInfo methodInfo)
		{
			if ((! methodInfo.IsVirtual) || methodInfo.IsFinal || methodInfo.IsAssembly ||
				methodInfo.ReturnType.IsGenericType	|| methodInfo.ContainsGenericParameters ||
				methodInfo.IsGenericMethod)
			{
				methodsToCompletelyIgnore.Add(methodInfo.Name);
				methodsToIgnore.Add(methodInfo.Name);
			}
			Type[] paramTypes = ExtractParameterTypes(methodInfo.GetParameters());
			foreach (Type paramType in paramTypes)
			{
				if (paramType.IsGenericType)
				{
					methodsToCompletelyIgnore.Add(methodInfo.Name);
					methodsToIgnore.Add(methodInfo.Name);
					break;
				}
			}
			bool fContainsMethod = m_Methods.Contains(new MethodSignature(ProxyClassName, methodInfo.Name,
				paramTypes));
			bool fRet = !(methodsToCompletelyIgnore.Contains(methodInfo.Name) || fContainsMethod);
			return fRet;
		}

		private string ImplementMethod(MethodInfo methodInfo, bool fInterface)
		{
			ParameterInfo[] parameters = methodInfo.GetParameters();
			m_Methods.Add(new MethodSignature(ProxyClassName, methodInfo.Name,
				ExtractParameterTypes(parameters)));

			StringBuilder source = new StringBuilder();
			if ((methodInfo.Name.StartsWith("get_") && parameters.Length == 0)
				|| (methodInfo.Name.StartsWith("set_") && parameters.Length == 1))
			{
				return ImplementProperty(methodInfo, fInterface);
			}
			if (methodInfo.Name == "get_Item" && parameters.Length == 1)
			{
				return ImplementIndexer(methodInfo, fInterface);
			}

			if (m_fGotProperty)
			{ // we have a previous started property - better end that first
				m_fGotProperty = false;
				source.Append("}\n");
			}
			string returnType = ValidTypeName(GetRealType(methodInfo.ReturnType));
			if (returnType == typeof(void).ToString())
				returnType = "void";

			CreateMethodHeader(source, methodInfo, returnType, parameters, fInterface);
			CreateCheckForIgnore(source, methodInfo, returnType, parameters, false);
			CreateObjArray(source, parameters, false);

			if (returnType != "void")
				source.Append("object ret = ");
			CreateMethodCall(source, methodInfo, parameters);

			CreateOutParams(parameters, source);

			if (returnType != "void")
				source.AppendFormat("return ({0})ret;\n", returnType);
			source.Append("}\n");

			return source.ToString();
		}

		private void CreateCheckForIgnore(StringBuilder source, MethodInfo methodInfo,
			string returnType, ParameterInfo[] parameters, bool fProperty)
		{
			source.AppendFormat("if ({0} == null || {0}.Contains(\"{1}\"))\n",
				METHODS_TO_IGNORE_FIELD_NAME, methodInfo.Name);
			source.Append("{\n");
			// initialize all out parameters
			for (int i = 0; i < parameters.Length; i++)
			{
				ParameterInfo param = parameters[i];
				if (param.IsOut)
				{
					source.AppendFormat("p{0} = ", i);
					Type elementType = param.ParameterType.GetElementType();
					if (elementType == null)
						throw new ApplicationException(string.Format(
							"Got internal type in method {0}, parameter {1} ({2})",
							methodInfo.Name, i, param.Name));

					if (elementType.IsValueType)
					{
						source.AppendFormat(" new {0}()", ValidTypeName(elementType));
					}
					else
					{
						source.AppendFormat("({0})null", ValidTypeName(elementType));
					}
					source.Append(";\n");
				}
			}

			// insert return statement
			if (methodInfo.ReflectedType.IsInterface || methodInfo.ReflectedType.IsAbstract || methodInfo.IsAbstract)
			{
				source.Append("return");
				if (methodInfo.ReturnType.FullName != "System.Void")
				{
					if (methodInfo.ReturnType.IsValueType)
						source.AppendFormat(" new {0}()", returnType);
					else
						source.AppendFormat(" ({0})null", returnType);
				}
				source.Append(";\n");
			}
			else
			{
				bool fSetter = methodInfo.Name.StartsWith("set_");
				bool fVoidReturn = (returnType == "void" || (fProperty && fSetter));
				if (!fVoidReturn)
					source.Append("return ");
				string name = methodInfo.Name;
				if (fProperty)
					name = name.Substring(4);
				source.AppendFormat("base.{0}", name);
				if (fProperty)
				{
					if (fSetter)
						source.Append(" = value;\nreturn");
				}
				else
				{
					source.Append("(");
					for (int i = 0; i < parameters.Length; i++)
					{
						ParameterInfo param = parameters[i];
						if (i > 0)
							source.Append(", ");
						if (param.IsOut)
							source.Append("out ");
						else if (param.ParameterType.Name.EndsWith("&"))
							source.Append("ref ");
						source.AppendFormat("p{0}", i);
					}
					source.Append(")");

					if (fVoidReturn)
						source.Append(";\nreturn");
				}
				source.Append(";\n");
			}
			source.Append("}\n");
		}

		private string ImplementIndexer(MethodInfo methodInfo, bool fInterface)
		{
			StringBuilder source = new StringBuilder();

			ParameterInfo[] parameters = methodInfo.GetParameters();
			string returnType;
			returnType = ValidTypeName(methodInfo.ReturnType);

			CreateIndexerHeader(source, methodInfo, returnType, parameters, fInterface);

			source.Append("get { ");

			CreateCheckForIgnore(source, methodInfo, returnType, parameters, true);
			CreateObjArray(source, parameters, false);

			source.Append("object ret = ");
			CreateMethodCall(source, methodInfo, parameters);

			CreateOutParams(parameters, source);

			source.AppendFormat("return ({0})ret;\n", returnType);
			source.Append("}\n");

			source.Append("}\n");

			return source.ToString();
		}

		private string ImplementProperty(MethodInfo methodInfo, bool fInterface)
		{
			StringBuilder source = new StringBuilder();
			bool fGetter = 	methodInfo.Name.StartsWith("get_");
			if (m_LastProperty != methodInfo.Name.Substring(4))
			{
				if (m_fGotProperty)
					source.Append("}\n");

				m_fGotProperty = false;
			}
			m_LastProperty = methodInfo.Name.Substring(4);

			ParameterInfo[] parameters = methodInfo.GetParameters();
			string returnType;
			if (fGetter)
				returnType = ValidTypeName(methodInfo.ReturnType);
			else
				returnType = ValidTypeName(parameters[0].ParameterType);

			if (returnType == typeof(void).ToString())
				returnType = "void";

			if (!m_fGotProperty)
			{
				CreatePropertyHeader(source, methodInfo, returnType, parameters, fInterface);
			}

			source.Append(fGetter ? "get { " : "set { ");

			CreateCheckForIgnore(source, methodInfo, returnType, parameters, true);
			CreateObjArray(source, parameters, true);

			if (fGetter)
				source.Append("object ret = ");
			CreateMethodCall(source, methodInfo, parameters);

			CreateOutParams(parameters, source);

			if (fGetter)
				source.AppendFormat("return ({0})ret;\n", returnType);
			source.Append("}\n");

			if (m_fGotProperty)
				source.Append("}\n");

			m_fGotProperty = !m_fGotProperty;
			return source.ToString();
		}

		private void CreateOutParams(ParameterInfo[] parameters, StringBuilder source)
		{
			for (int i = 0; i < parameters.Length; i++)
			{
				ParameterInfo param = parameters[i];
				if (param.ParameterType.IsByRef)
				{
					source.AppendFormat("p{0} = ({1})args[{2}];\n", i,
						ValidTypeName(GetRealType(param.ParameterType)), i);
				}
			}
		}

		private void CreateMethodCall(StringBuilder source, MethodInfo methodInfo, ParameterInfo[] parameters)
		{
			source.AppendFormat("_invocationHandler.Invoke(\"{0}\", args, new string[]{{", methodInfo.Name);
			bool fFirst = true;
			foreach(ParameterInfo param in parameters)
			{
				if (fFirst)
					fFirst = false;
				else
					source.Append(", ");

				TypeCode tc = Type.GetTypeCode(param.ParameterType);
				source.AppendFormat("\"{0}\"", param.ParameterType);
			}
			source.Append("});\n");
		}

		private void CreateMethodHeader(StringBuilder source, MethodInfo methodInfo,
			string returnType, ParameterInfo[] parameters, bool fInterface)
		{
			source.AppendFormat("{0} {1} {2} {3}(", methodInfo.IsPublic ? "public" : "protected",
				fInterface ? "new" : methodInfo.IsVirtual ? "override" : "new",
				returnType, methodInfo.Name);
			for (int i = 0; i < parameters.Length; i++)
			{
				if (i > 0)
					source.Append(", ");
				source.Append(GetParam(parameters[i], i));
			}
			source.Append("){\n");
		}

		private void CreatePropertyHeader(StringBuilder source, MethodInfo methodInfo,
			string returnType, ParameterInfo[] parameters, bool fInterface)
		{
			source.AppendFormat("{0} {1} {2} {3}\n{{\n", methodInfo.IsPublic ? "public" : "protected",
				fInterface ? "new" : methodInfo.IsVirtual ? "override" : "new",
				returnType, methodInfo.Name.Substring(4));
		}

		private void CreateIndexerHeader(StringBuilder source, MethodInfo methodInfo,
			string returnType, ParameterInfo[] parameters, bool fInterface)
		{
			source.AppendFormat("{0} {1} {2} this[{3}]\n{{\n", methodInfo.IsPublic ? "public" : "protected",
				fInterface ? "new" : methodInfo.IsVirtual ? "override" : "new",
				returnType, GetParam(parameters[0], 0));
		}

		private void CreateObjArray(StringBuilder source, ParameterInfo[] parameters, bool fProperty)
		{
			source.Append("object[] args = new object[] {");
			bool fFirst = true;
			for (int i = 0; i < parameters.Length; i++)
			{
				ParameterInfo param = parameters[i];
				if (fFirst)
					fFirst = false;
				else
					source.Append(", ");
				if (param.IsOut)
					source.Append("null");
				else if (fProperty)
					source.Append("value");
				else
					source.AppendFormat("p{0}", i);
			}
			source.Append("};\n");
		}

		private string GetParam(ParameterInfo param, int i)
		{
			Type type = param.ParameterType;
			Type realType = GetRealType(type);

			string outRef = string.Empty;
			if (param.IsOut)
			{
				if (param.IsIn)
					outRef = "ref";
				else
					outRef = "out";
			}
			else if (type.IsByRef && !realType.IsByRef)
				outRef = "ref";
			return string.Format("{0} {1} p{2}", outRef,
				ValidTypeName(realType), i);
		}

		private Type GetRealType(Type type)
		{
			Type realType;
			Type elementType = type.GetElementType();
			if (type.FullName.EndsWith("&"))
				realType = elementType;
			else
				realType = type;

			if (realType.IsNotPublic
				|| (realType.DeclaringType != null && realType.DeclaringType.IsNotPublic)
				|| (elementType != null && (elementType.IsNotPublic
				|| (elementType.DeclaringType != null && elementType.DeclaringType.IsNotPublic))))
				throw new ArgumentException("Parameter type is not visible", type.FullName);

			return realType;
		}

		private string ValidTypeName(Type type)
		{
			return type.FullName.Replace('+', '.');
		}

		private Type[] ExtractParameterTypes(ParameterInfo[] parameters)
		{
			Type[] paramTypes = new Type[parameters.Length];
			for (int i = 0; i < parameters.Length; ++i)
			{
				paramTypes[i] = parameters[i].ParameterType;
			}
			return paramTypes;
		}

		public string ProxyClassName { get { return "Proxy" + type.Name; } }
		public Type ProxySuperClass
		{
			get
			{
				if (type.IsInterface && superclassIfTypeIsInterface != null)
					return superclassIfTypeIsInterface;
				else
					return type;
			}
		}
		public Type[] ProxyInterfaces { get { return type.IsInterface ? new Type[] { type } : new Type[0]; } }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set this property to <c>true</c> if you want to see the source file that gets
		/// generated for a dynamic mock. It creates a file in the temp directory named
		/// Mock&lt;typname>.cs
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool CreateSourceFile
		{
			set { s_fCreateSourceFile = value; }
		}
	}
}

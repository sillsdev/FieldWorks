// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Remoting.Contexts;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace FwBuildTasks
{
	/// <summary>
	/// Does the one thing Gendarme did reliably: enforce that <c>System.IDisposable</c> types complain when they aren't properly disposed.
	/// This requires the Types to include a method <c>Dispose(bool disposing)</c> that writes a message when <c>disposing</c> is <c>false</c>
	/// and a finalizer that calls <c>Dispose(false)</c>. (<c>Dispose()</c> calls <c>Dispose(true)</c> and suppresses the finalizer.)
	/// </summary>
	/// <example>
	/// Bad example:
	/// <code>
	/// class NoFinalizer: IDisposable
	/// {
	/// public void Dispose()
	/// {
	///     Dispose(true);
	///     GC.SuppressFinalize(this);
	/// }
	/// }
	/// </code>
	/// </example>
	/// <example>
	/// Good example:
	/// <code>
	/// class HasFinalizer: IDisposable
	/// {
	/// #IF DEBUG
	///	~HasFinalizer ()
	///	{
	///     Dispose(false)
	///	}
	/// #endif
	/// public void Dispose()
	/// {
	///     Dispose(true);
	///     GC.SuppressFinalize(this);
	/// }
	/// protected virtual void Dispose(bool disposing)
	/// {
	///     System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
	/// }
	/// }
	/// </code>
	/// </example>
	/// <example>
	/// Bad example:
	/// <code>
	/// void Dispose(bool disposing)
	/// {
	/// }
	/// </code>
	/// </example>
	/// <example>
	/// Good example:
	/// <code>
	/// void Dispose(bool disposing)
	/// {
	/// 	System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
	/// }
	/// </code>
	/// </example>
	public class Clouseau : Task
	{
		private const string Dispose = "Dispose";
		private static readonly Type DisposableType = typeof(IDisposable);
		private static readonly Module ThisModule;
		private const string MissingDisposeMsgPart1 = "****** Missing Dispose() call for ";
		private const string MissingDisposeMsgPart2 = " ******";
		private const string MissingDisposeMsgCmd = @"System.Diagnostics.Debug.WriteLineIf(!disposing, """
			+ MissingDisposeMsgPart1 + @""" + GetType().Name + """ + MissingDisposeMsgPart2 + @""");";
		private static readonly bool IsUnix;
		/// <summary>
		/// ILInstructions for System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + " ******");
		/// </summary>
		private static readonly IReadOnlyList<ILInstruction> WriteLineIf;
		/// <summary>
		/// ILInstructions for System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + " ******");
		/// </summary>
		private static readonly IReadOnlyList<ILInstruction> WriteLineIf2;

		[Required]
		public string AssemblyPathname { get; set; }

		static Clouseau()
		{
			IsUnix = BuildUtils.IsUnix;
			var insinkerator = typeof(ExemplarDisposable);
			ThisModule = insinkerator.Module;
			WriteLineIf = GetMethodPattern(GetDisposeBoolMethod(insinkerator));
			WriteLineIf2 = GetMethodPattern(insinkerator.GetMethod("Dispose2"));
		}

		private static List<ILInstruction> GetMethodPattern(MethodBase method)
		{
			var instructions = new ILReader(method).ToList();
			instructions.RemoveAt(instructions.Count - 1); // remove terminal return instruction
			// All methods end with NOP, RET. On Windows, WriteLIneIf is always followed by NOP, but not on Linux, so remove it on Linux
			if (IsUnix)
				instructions.RemoveAt(instructions.Count - 1); // remove terminal no-op instruction
			return instructions;
		}

		private static MethodBase GetDisposeBoolMethod(Type type)
		{
			return type.GetMethod(Dispose, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly, Type.DefaultBinder,
				new[] { typeof(bool) }, null); // the protected Dispose(bool) method
		}

		private static MethodBase GetFinalizer(Type type)
		{
			return type.GetMethod("Finalize", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
		}

		public override bool Execute()
		{
			try
			{
				Log.LogMessage("About to load {0} for ... Clouseauvaryzing", AssemblyPathname);
				var assembly = Assembly.LoadFrom(AssemblyPathname);
				Log.LogMessage("Clouseauvaryzing {0} {1}...", AssemblyPathname, assembly.FullName);
				InspectAssembly(assembly);
				Log.LogMessage("Finished Clouseauvaryzing {0} {1}...", AssemblyPathname, assembly.FullName);
				if (Log.HasLoggedErrors)
				{
					// Writing this message (with MissingDisposeMsgCmd) will cause the build log inspector to mark this build as UNSTABLE.
					Log.LogError($"The assembly {assembly.GetName().Name} has types that do not call {MissingDisposeMsgCmd} and base.Dispose(disposing);"
								+ " every time they should. See above errors for details.");
				}
				else
				{
					Log.LogMessage("No errors Clouseauvaryzing {0} {1}...", AssemblyPathname, assembly.FullName);
				}
				return true; // let it keep looking and report all errors at once.
			}
			catch (Exception e)
			{
				Log.LogMessage("Exception thrown, while Clouseauvaryzing {0} {1}." + Environment.NewLine + "{3}", AssemblyPathname, e.Message, e.StackTrace);
				throw;
			}
		}

		public void InspectAssembly(Assembly assembly)
		{
			try
			{
				foreach (var type in assembly.DefinedTypes)
				{
					InspectType(type);
				}
			}
			catch (ReflectionTypeLoadException e)
			{
				Log.LogError(string.Join<Exception>("\n-----------------------------\n", e.LoaderExceptions));
				throw;
			}
		}

		/// <summary>
		/// If <c>type</c> is IDisposable, ensures that <c>type</c> or one of its ancestors has
		/// <c>Dispose(bool disposing)</c> that logs a warning if <c>disposing</c> is false and a Finalizer that calls <c>Dispose(false)</c>.
		/// If <c>type</c>'s base class is also IDisposable, ensures that
		/// <c>type.Dispose(bool disposing)</c>, if present, calls <c>base.Dispose(disposing)</c>
		/// </summary>
		public void InspectType(Type type)
		{
			var baseType = type.BaseType;
			var baseCategory = GetCategory(baseType);
			// Disregard any non-disposable types, interfaces, and anonymous IEnumeratorImpls (the result of `yield return`)
			if (!DisposableType.IsAssignableFrom(type) || type.IsInterface || IsAnonymousIEnumeratorImpl(type))
				return;

			var disposeBool = GetDisposeBoolMethod(type);
			if (disposeBool == null)
			{
				if (baseCategory == TypeCategory.Disposable)
					return; // the base class's Dispose(bool) and Finalizer should suffice
				if (IsIEnumeratorImpl(type))
				{
					Log.LogWarning($"{type.FullName} doesn't implement Dispose(bool), but that's probably fine,"
						+ " since most Enumerators don't have disposable members.");
					return;
				}
				Log.LogError($"The type {type.FullName} which implements IDisposable should dispose its members in Dispose(bool disposing)"
					+ ", which should begin with:\t" + MissingDisposeMsgCmd);
			}
			else
				InspectDisposeBool(disposeBool, baseType, baseCategory);

			// Disposables, including Windows Forms, implement Dispose() { Dispose(true); GC.SuppressFinalize(this); } and ~Type() { Dispose(false); }
			// So only first-generation disposables need their Finalizers checked.
			if (baseCategory != TypeCategory.Object)
				return;

			var finalizer = GetFinalizer(type);
			if (finalizer == null)
				Log.LogError($"The type {type.FullName} which implements IDisposable should have a Finalizer that calls Dispose(false) {baseCategory}");
			else if (!FinalizerCallsDisposeFalse(finalizer, disposeBool))
				Log.LogError($"~{finalizer.DeclaringType}() should call Dispose(false);");
		}

		public TypeCategory GetCategory(Type type)
		{
			if (type == null || !DisposableType.IsAssignableFrom(type))
				return TypeCategory.Object;
			if (type.Namespace != null && (type.Namespace.StartsWith("System.Windows.Forms") || type.Namespace.StartsWith("System.ComponentModel")))
				return TypeCategory.WindowsForms;
			return TypeCategory.Disposable;
		}

		public bool IsAnonymousIEnumeratorImpl(Type disposableType)
		{
			return disposableType.FullName.Contains("+<"); // SomeClass+<GetEnumerator> or similar;
			// REVIEW (Hasso) 2016.12: make more particular: FullName.IndexOf(FullName.IndexOf("+<"), ">"); substr(idx1, idx2); has method (who has?)
		}

		public bool IsIEnumeratorImpl(Type disposableType)
		{
			// For some reason, IsAssignableFrom doesn't work in this case.
			var iEnumerator = typeof(IEnumerator<>);
			return disposableType.GetInterfaces().Any(i => i.Namespace == iEnumerator.Namespace && i.Name == iEnumerator.Name);
		}

		/// <summary>
		/// Ensure Dispose(bool disposing) calls
		///  - WriteLineIf(!disposing, MESSAGE) (if a first generation IDisposable in our code) and
		///  - base.Dispose(disposing) (if a second-plus generation IDisposable of any kind)
		/// </summary>
		public void InspectDisposeBool(MethodBase disposeBool, Type baseType, TypeCategory baseCategory)
		{
			var bodyParts = new ILReader(disposeBool).ToArray();
			var module = disposeBool.Module;

			// If the base is any kind of Disposable, we'd better call base.Dispose()!
			if (baseCategory != TypeCategory.Object && !MethodCallsBaseDispose(baseType, bodyParts, module))
				Log.LogError(disposeBool.DeclaringType?.FullName + ".Dispose(bool disposing) should call base.Dispose(disposing);");

			// If the base is not one of our Disposables, it is this method's responsibility to warn on missing Dispose() calls;
			// make sure the Dispose(bool) method starts with a WriteLineIf
			if (baseCategory != TypeCategory.Disposable
				&& !MethodBeginningsMatch(WriteLineIf, bodyParts, module) && !MethodBeginningsMatch(WriteLineIf2, bodyParts, module))
				Log.LogError(disposeBool.DeclaringType?.FullName + ".Dispose(bool disposing) should call " + MissingDisposeMsgCmd);
		}

		private bool MethodCallsBaseDispose(Type baseType, IReadOnlyList<ILInstruction> testInstructions, Module testModule)
		{
			// We have to find base.Dispose(bool) on the Type that declares it, so that its ReflectedType matches that of the result of ResolveMethod
			MethodBase baseDispose = null;
			for (var dispImplBase = baseType; dispImplBase != null && baseDispose == null; dispImplBase = dispImplBase.BaseType)
				baseDispose = GetDisposeBoolMethod(dispImplBase);
			if (baseDispose == null)
			{
				Log.LogWarning($"The Type {baseType} and its ancestors do not implement Dispose(bool)");
				return false; // can't call something that does't exist
			}

			// Most types call base.Dispose at the end of their override Dispose method,
			// but some types (e.g. SimpleRootSite) need to call base.Dispose between disposing other things.
			// Search the whole method, statrting at the end (the final two instructions are always NOP, RET--except on Unix, which may not have NOP)
			for(var i = testInstructions.Count - 2; i > 1; i--)
			{
				if (testInstructions[i].opCode == OpCodes.Call && Equals(baseDispose, testModule.ResolveMethod((int) testInstructions[i].operand)))
					// Found the call; make sure we pass `disposing`
					return testInstructions[i - 2].opCode == OpCodes.Ldarg_0
						&& testInstructions[i - 1].opCode == OpCodes.Ldarg_1
						&& (IsUnix || testInstructions[i + 1].opCode == OpCodes.Nop);
			}
			return false; // Didn't find the call
		}

		private bool MethodBeginningsMatch(IReadOnlyList<ILInstruction> exemplar, IReadOnlyList<ILInstruction> testInstructions, Module testModule)
		{
			var soFarSoGood = testInstructions.Count >= exemplar.Count;
			for (var i = 0; soFarSoGood && i < exemplar.Count; i++)
			{
				if (exemplar[i].opCode != testInstructions[i].opCode)
				{
					// OpCodes don't match. If the found OpCode is Callvirt, this is probably a `GetType().Name` where we expected a `GetType()`,
					// which is fine. Still return false, to force comparison against the second exemplar, but don't clutter the build log with
					// hundreds of  messages when we think the testInstructions will match the second exemplar
					if (testInstructions[i].opCode != OpCodes.Callvirt)
						Log.LogMessage($@"Expected at {i}: {{{exemplar[i]}}}; got {{{testInstructions[i]}}}");
					return false;
				}
				if (exemplar[i].opCode == OpCodes.Ldstr)
				{

					var message = testModule.ResolveString((int)testInstructions[i].operand);
					var correctMessage = ThisModule.ResolveString((int) exemplar[i].operand);
					soFarSoGood = message.Contains(correctMessage);
					if (!soFarSoGood)
						Log.LogMessage($@"Expected at {i}: ""{correctMessage}""; got ""{message}""");
				}
				else if (exemplar[i].opCode == OpCodes.Call || exemplar[i].opCode == OpCodes.Callvirt)
				{
					var method = testModule.ResolveMethod((int) testInstructions[i].operand);
					var correctMethod = ThisModule.ResolveMethod((int) exemplar[i].operand);
					soFarSoGood = Equals(method, correctMethod);
					if (!soFarSoGood)
						Log.LogMessage($@"Expected at {i}: `{correctMethod}`; got `{method}`");
				}
				else
				{
					soFarSoGood = Equals(testInstructions[i], exemplar[i]);
					if (!soFarSoGood)
						Log.LogMessage($@"Expected at {i}: {{{exemplar[i]}}}; got {{{testInstructions[i]}}}");
				}
			}
			return soFarSoGood;
		}

		/// <summary>Ensures the Finalizer calls Dispose(false) to generate the "Missing Dispose() call" message)</summary>
		public bool FinalizerCallsDisposeFalse(MethodBase finalizer, MethodBase disposeBool)
		{
			var module = disposeBool.Module;
			var instructions = new ILReader(finalizer).ToArray();
			for (var i = IsUnix ? 2 : 3; i < instructions.Length - 2; i++)
			{
				try
				{
					if ((instructions[i].opCode == OpCodes.Callvirt || instructions[i].opCode == OpCodes.Call)
						&& IsDisposeBoolCall(module, (int) instructions[i].operand, disposeBool))
						// found the call; make sure we pass `false`
						return PassesFalse(instructions, i);
				}
				catch (ArgumentException) // Probably failed to ResolveMethod for a parameterizable type
				{
					if (PassesFalse(instructions, i))
					{
						// The call or callvirt looks like a proper Dispose(false) call (except for the failed ResolveMethod). It's good enough.
						// On Windows, since we're slightly less sure, also log a warning.
						if (!IsUnix)
						{
							Log.LogWarning($"Clouseau has found no problems with ~{finalizer.DeclaringType}() but cannot yet complete the inspection");
							PrintMethodComparison(GetMethodPattern(GetFinalizer(typeof(ExemplarDisposable))), instructions);
						}
						return true;
					}
					// else, this may be some other call; keep looking
				}
			}
			return false;
		}

		private bool IsDisposeBoolCall(Module module, int methodToken, MethodBase disposeBool)
		{
			var methodBase = module.ResolveMethod(methodToken);
			if (Equals(disposeBool, methodBase))
				return true;
			if (IsUnix)
			{
				// On Unix, rather than throwing an ArgumentException for parameterized Types, Module.ResolveMethod returns a MethodBase with the
				// same signature, but which is somehow unequivalent. Compensate for this by checking for this case and throwing an ArgumentException.
				// ReSharper disable once PossibleNullReferenceException -- Justification: disposeBool.DeclaringType cannot be null
				var typeName = disposeBool.DeclaringType.ToString(); // ToString(), unlike FullName, includes the "[P,T]" in "ConsumerThread`2[P,T]"
				if (disposeBool.Name == methodBase.Name && typeName.Contains("`") && typeName.Contains("[") && typeName.Contains("]"))
					throw new ArgumentException();
			}
			return false;
		}

		private static bool PassesFalse(IReadOnlyList<ILInstruction> instructions, int callIdx)
		{
			return (IsUnix || instructions[callIdx - 3].opCode == OpCodes.Nop)
				&& instructions[callIdx - 2].opCode == OpCodes.Ldarg_0
				&& instructions[callIdx - 1].opCode == OpCodes.Ldc_I4_0 // ldc.i4.0 is literal false
				&& (IsUnix || instructions[callIdx + 1].opCode == OpCodes.Nop);
		}

		public enum TypeCategory
		{
			/// <summary>The Type does not implement IDisposable</summary>
			Object,
			/// <summary>The Type implements IDisposable and has presumably been inspected by Clouseau</summary>
			Disposable,
			/// <summary>
			/// A built-in .NET Type that implements IDisposable. It implements the <c>protected virtual void Dispose(bool)</c> model,
			/// but does not warn on missing dispose calls.
			/// </summary>
			WindowsForms
		}

		private void PrintMethodComparison(IReadOnlyList<ILInstruction> lhs, IReadOnlyList<ILInstruction> rhs)
		{
			var max = Math.Min(lhs.Count, rhs.Count);
			for (var i = 0; i < max; i++)
				Log.LogMessage($"[{i}]\t{{{lhs[i]}}}\t\t{{{rhs[i]}}}");
		}

		// ReSharper disable once ClassWithVirtualMembersNeverInherited.Local
		// Justification: Clouseau reflectively verifies the presence of `protected virtual void Dispose(bool disposing)`
		private class ExemplarDisposable : IDisposable
		{
			~ExemplarDisposable()
			{
				Dispose(false);
			}

			// ReSharper disable MemberHidesStaticFromOuterClass -- Justification: we don't need const string Dispose = "Dispose" here.
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			protected virtual void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, MissingDisposeMsgPart1 + GetType() + MissingDisposeMsgPart2);
			}

			public virtual void Dispose2(bool disposing)
			{
				Debug.WriteLineIf(!disposing, MissingDisposeMsgPart1 + GetType().Name + MissingDisposeMsgPart2);
			}
			// ReSharper restore MemberHidesStaticFromOuterClass
		}
	}
}

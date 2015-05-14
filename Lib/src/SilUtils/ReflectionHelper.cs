// ---------------------------------------------------------------------------------------------
// Copyright (c) 2009-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ReflectionHelper.cs
// Responsibility: FW Team, especially David Olson (this is of interest to PA also)
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.IO;
using System.Configuration;
using System.Diagnostics;

namespace SIL.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ReflectionHelper
	{
		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Dynamically find an assembly and create an object of the nameed class.
		///// </summary>
		///// <param name="assemblyName">Relative to the location of the reflection helper assemptly</param>
		///// <param name="className1"> fully qualified!!</param>
		///// <param name="args"></param>
		///// <returns></returns>
		///// ------------------------------------------------------------------------------------
		//static public Object CreateObjectForTests(string assemblyName, string className1, object[] args)
		//{
		//    Assembly assembly = Assembly.LoadFrom(assemblyName);

		//    string className = className1.Trim();
		//    Object thing = null;
		//    try
		//    {
		//        //make the object
		//        //Object thing = assembly.CreateInstance(className);
		//        thing = assembly.CreateInstance(className, false,
		//            BindingFlags.Instance | BindingFlags.NonPublic, null, args, null, null);
		//    }
		//    catch (Exception err)
		//    {
		//        Debug.WriteLine(err.Message);
		//        string message = CouldNotCreateObjectMsg(assemblyName, className);

		//        Exception inner = err;

		//        while (inner != null)
		//        {
		//            message += "\r\nInner exception message = " + inner.Message;
		//            inner = inner.InnerException;
		//        }
		//        throw new ConfigurationErrorsException(message);
		//    }

		//    if (thing == null)
		//    {
		//        // Bizarrely, CreateInstance is not specified to throw an exception if it can't
		//        // find the specified class. But we want one.
		//        throw new ConfigurationErrorsException(CouldNotCreateObjectMsg(assemblyName, className));
		//    }
		//    return thing;
		//}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Dynamically find an assembly and create an object of the nameed class using a public
		/// constructor.
		/// </summary>
		/// <param name="assemblyName">Relative to the location of the reflection helper assemptly</param>
		/// <param name="className1"> fully qualified!!</param>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		static public Object CreateObject(string assemblyName, string className1, object[] args)
		{
			return CreateObject(assemblyName, className1, BindingFlags.Public, args);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Dynamically find an assembly and create an object of the nameed class.
		/// </summary>
		/// <param name="assemblyName">Relative to the location of the reflection helper assemptly</param>
		/// <param name="className1">fully qualified!!</param>
		/// <param name="addlBindingFlags">The additional binding flags which can be used to
		/// indicate whether to look for a public or non-public constructor, etc.
		/// (BindingFlags.Instance is always included).</param>
		/// <param name="args">The arguments to the constructor.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		static public Object CreateObject(string assemblyName, string className1,
			BindingFlags addlBindingFlags, object[] args)
		{
			Assembly assembly;
			string location = Assembly.GetExecutingAssembly().Location;
			string assemblyPath = Path.Combine(Path.GetDirectoryName(location), assemblyName);
			try
			{
				assembly = Assembly.LoadFrom(assemblyPath);
			}
			catch
			{
				// That didn't work, so try the bare DLL name and let the OS try to find it. This
				// is needed for tests because each DLL gets shadow-copied in its own temp folder.
				assemblyPath = assemblyName;
				assembly = Assembly.LoadFrom(assemblyPath);
			}

			string className = className1.Trim();
			Object thing = null;
			try
			{
				//make the object
				//Object thing = assembly.CreateInstance(className);
				thing = assembly.CreateInstance(className, false,
					BindingFlags.Instance | addlBindingFlags, null, args, null, null);
			}
			catch (Exception err)
			{
				Debug.WriteLine(err.Message);
				string message = CouldNotCreateObjectMsg(assemblyPath, className);

				Exception inner = err;

				while (inner != null)
				{
					message += Environment.NewLine + "Inner exception message = " + inner.Message;
					inner = inner.InnerException;
				}
				throw new ConfigurationErrorsException(message);
			}

			if (thing == null)
			{
				// Bizarrely, CreateInstance is not specified to throw an exception if it can't
				// find the specified class. But we want one.
				throw new ConfigurationErrorsException(CouldNotCreateObjectMsg(assemblyPath, className));
			}
			return thing;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static string CouldNotCreateObjectMsg(string assemblyPath, string className)
		{
			return "ReflectionHelper found the DLL " + assemblyPath	+
				" but could not create the class: "	+ className +
				". If there are no 'InnerExceptions' below, then make sure capitalization is correct and that you include the name space (e.g. XCore.Ticker).";
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Call a static method on a dynamically specified class.
		/// </summary>
		/// <param name="assemblyName">Relative to the location of the reflection helper assemptly</param>
		/// <param name="className1"> fully qualified!!</param>
		/// <param name="methodName"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		static public object CallStaticMethod(string assemblyName, string className1,
			string methodName, params object[] args)
		{
			Type targetType = GetType(assemblyName, className1);
			return GetResult(targetType, methodName, args);
		}

		/// <summary>
		/// Return the indicated type in the indicated assembly.
		/// </summary>
		/// <param name="assemblyName">Relative to the location of the reflection helper assemptly</param>
		/// <param name="className1"> fully qualified!!</param>
		public static Type GetType(string assemblyName, string className1)
		{
			Assembly assembly;
			string baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase).Substring(Environment.OSVersion.Platform == PlatformID.Unix ? 5 : 6);
			string assemblyPath = Path.Combine(baseDir, assemblyName);
			try
			{
				assembly = Assembly.LoadFrom(assemblyPath);
			}
			catch (Exception e)
			{
				throw new Exception("Could not find the assembly called " + assemblyPath, e);
			}

			string className = className1.Trim();
			Type targetType;
			try
			{
				targetType = assembly.GetType(className, false);
			}
			catch (Exception e)
			{
				throw new Exception("Could not find the type called " + className +
									" in the assembly "+ assemblyPath +
									" (Did you remember to fully qualify the name?)", e);
			}
			return targetType;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a string value returned from a call to a private method.
		/// </summary>
		/// <param name="binding">This is either the Type of the object on which the method
		/// is called or an instance of that type of object. When the method being called
		/// is static then binding should be a type.</param>
		/// <param name="methodName">Name of the method to call.</param>
		/// <param name="args">An array of arguments to pass to the method call.</param>
		/// ------------------------------------------------------------------------------------
		public static string GetStrResult(object binding, string methodName, params object[] args)
		{
			return (GetResult(binding, methodName, args) as string);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a integer value returned from a call to a private method.
		/// </summary>
		/// <param name="binding">This is either the Type of the object on which the method
		/// is called or an instance of that type of object. When the method being called
		/// is static then binding should be a type.</param>
		/// <param name="methodName">Name of the method to call.</param>
		/// <param name="args">An array of arguments to pass to the method call.</param>
		/// ------------------------------------------------------------------------------------
		public static int GetIntResult(object binding, string methodName, params object[] args)
		{
			return ((int)GetResult(binding, methodName, args));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a float value returned from a call to a private method.
		/// </summary>
		/// <param name="binding">This is either the Type of the object on which the method
		/// is called or an instance of that type of object. When the method being called
		/// is static then binding should be a type.</param>
		/// <param name="methodName">Name of the method to call.</param>
		/// <param name="args">An array of arguments to pass to the method call.</param>
		/// ------------------------------------------------------------------------------------
		public static float GetFloatResult(object binding, string methodName, params object[] args)
		{
			return ((float)GetResult(binding, methodName, args));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a boolean value returned from a call to a private method.
		/// </summary>
		/// <param name="binding">This is either the Type of the object on which the method
		/// is called or an instance of that type of object. When the method being called
		/// is static then binding should be a type.</param>
		/// <param name="methodName">Name of the method to call.</param>
		/// <param name="args">An array of arguments to pass to the method call.</param>
		/// ------------------------------------------------------------------------------------
		public static bool GetBoolResult(object binding, string methodName, params object[] args)
		{
			return ((bool)GetResult(binding, methodName, args));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a DateTime value returned from a call to a private method.
		/// </summary>
		/// <param name="binding">This is either the Type of the object on which the method
		/// is called or an instance of that type of object. When the method being called
		/// is static then binding should be a type.</param>
		/// <param name="methodName">Name of the method to call.</param>
		/// <param name="args">An array of arguments to pass to the method call.</param>
		/// ------------------------------------------------------------------------------------
		public static DateTime GetDateTimeResult(object binding, string methodName, params object[] args)
		{
			return ((DateTime)GetResult(binding, methodName, args));
		}

		/// <summary>
		/// Compares the property values of two objects.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns>true if the property values are equal, false otherwise.</returns>
		public static bool HaveSamePropertyValues(object a, object b)
		{
			if (a == null || b == null)
				return false;
			// find all public getters and compare them.
			PropertyInfo[] propList = a.GetType().GetProperties();
			foreach (PropertyInfo property in propList)
			{
				if (!property.GetValue(a, null).Equals(property.GetValue(b, null)))
					return false;
			}
			return true;
		}


		///// <summary>
		///// invoke delegates registered to the given event property.
		///// useful during initialization (or tests) where assigned events will not
		///// fire automatically until after some initialization state has been reached.
		///// </summary>
		///// <param name="obj">object that owns the event property to invoke</param>
		///// <param name="eventProperty">event property that has event delegates</param>
		///// <param name="args">arguments to pass to the event delegate</param>
		//public static void InvokeEventDelegates(object obj, string eventProperty, object[] args)
		//{
		//    Type t = obj.GetType();
		//    FieldInfo f = t.GetField(eventProperty);

		//    object originalValue = f.GetValue(obj);
		//    if (originalValue != null)
		//    {

		//        System.MulticastDelegate originalDelegate =
		//            (System.MulticastDelegate)originalValue;
		//        System.Delegate[] originalHandlers =
		//            originalDelegate.GetInvocationList();

		//        foreach (System.Delegate d in originalHandlers)
		//        {
		//            d.DynamicInvoke(args);
		//        }
		//    }
		//}


		/// <summary>
		/// Programatically fire an event handler of an object
		/// From: http://stackoverflow.com/questions/372974/winforms-how-to-trigger-a-controls-event-handler
		/// </summary>
		/// <param name="targetObject"></param>
		/// <param name="eventName"></param>
		/// <param name="e"></param>
		public static void FireEvent(Object targetObject, string eventName, EventArgs e)
		{   /*
			 * By convention event handlers are internally called by a protected
			 * method called OnEventName
			 * e.g.
			 *		public event TextChanged
			 *  is triggered by    protected void OnTextChanged
			 *  If the object didn't create an OnXxxx protected method,
			 *  then you're screwed. But your alternative was over override
			 *  the method and call it - so you'd be screwed the other way too.
			 */
			//Event thrower method name e.g. OnTextChanged
			String methodName = "On" + eventName;
			MethodInfo mi = targetObject.GetType().GetMethod(
				methodName,
				BindingFlags.Instance | BindingFlags.NonPublic);
			if (mi == null)
				throw new ArgumentException("Cannot find event thrower named " + methodName);
			mi.Invoke(targetObject, new object[] { e });
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calls a method specified on the specified binding.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void CallMethod(object binding, string methodName, params object[] args)
		{
			GetResult(binding, methodName, args);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the result of calling a method on the specified binding.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static object GetResult(object binding, string methodName, params object[] args)
		{
			return Invoke(binding, methodName, args, BindingFlags.InvokeMethod);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the specified property on the specified binding.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void SetProperty(object binding, string propertyName, object args)
		{
			Invoke(binding, propertyName, new object[] { args }, BindingFlags.SetProperty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the specified field (i.e. member variable) on the specified binding.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void SetField(object binding, string fieldName, object args)
		{
			Invoke(binding, fieldName, new object[] { args }, BindingFlags.SetField);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the specified delegate on the specified binding.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void SetAction<T>(object binding, string fieldName, Action<T> args)
		{
			Invoke(binding, fieldName, new object[] { args }, BindingFlags.SetField);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the specified property on the specified binding.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static object GetProperty(object binding, string propertyName)
		{
			return Invoke(binding, propertyName, null, BindingFlags.GetProperty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the specified field (i.e. member variable) on the specified binding.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static object GetField(object binding, string fieldName)
		{
			return Invoke(binding, fieldName, null, BindingFlags.GetField);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the specified member variable or property (specified by name) on the
		/// specified binding.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="See TODO-Linux comment")]
		private static object Invoke(object binding, string name, object[] args, BindingFlags flags)
		{
			// If binding is a Type then assume we're invoking a static method, property
			// or field. Otherwise invoke an instance method, property or field.
			flags |= (BindingFlags.NonPublic | BindingFlags.Public |
				(binding is Type ? BindingFlags.Static : BindingFlags.Instance));

			// If necessary, go up the inheritance chain until the name
			// of the method, property or field is found.
			Type type = (binding is Type ? binding as Type : binding.GetType());
			// TODO-Linux: System.Boolean System.Type::op_Inequality(System.Type,System.Type)
			// is marked with [MonoTODO] and might not work as expected in 4.0.
			while (type.GetMember(name, flags).Length == 0 && type.BaseType != null)
				type = type.BaseType;

			return type.InvokeMember(name, flags, null, binding, args);
		}
	}
}

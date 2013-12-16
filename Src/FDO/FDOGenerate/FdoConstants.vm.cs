## --------------------------------------------------------------------------------------------
## Copyright (c) 2006-2013 SIL International
## This software is licensed under the LGPL, version 2.1 or later
## (http://www.gnu.org/licenses/lgpl-2.1.html)
##
## NVelocity template file
## This file is used by the FdoGenerate task to generate the source code from the XMI
## database model.
## --------------------------------------------------------------------------------------------
namespace SIL.FieldWorks.FDO
{
#foreach($module in $fdogenerate.Modules)
#foreach($class in $module.Classes)
#set( $className = $class.Name )
	#region ${className}Tags
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Generated, partial class for ${className}Tags
	/// This class holds constants for $className.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
#if ( $class.Name == "CmObject" )
	public abstract partial class CmObjectTags
#else
	public abstract partial class ${className}Tags : ${class.BaseClass.Name}Tags
#end
	{
#if ( $className == "CmObject" )
		/// <summary>The class ID of this FieldWorks class.</summary>
		public const int kClassId = $class.Number;
		/// <summary>The class name of this FieldWorks class.</summary>
		public const string kClassName = "$class.Name";
#else
		/// <summary>The class ID of this FieldWorks class.</summary>
		public new const int kClassId = $class.Number;
		/// <summary>The class name of this FieldWorks class.</summary>
		public new const string kClassName = "$class.Name";
#end
#if ( $className == "CmObject" )
		/// <summary>Object HVO id.</summary>
		public const int kflidHvo = 100;
		/// <summary>Object Guid Id.</summary>
		public const int kflidGuid = 101;
		/// <summary>Object Class Id.</summary>
		public const int kflidClass = 102;
		/// <summary>Owning field Id</summary>
		public const int kflidOwnFlid = 104;
		/// <summary>Owning ord</summary>
		public const int kflidOwnOrd = 105;
#end
#foreach( $prop in $class.Properties)
		/// <summary>${prop.Name}</summary>
		public const int kflid${prop.Name} = ${prop.Number};
#end
	}
	#endregion ${className}Tags
#end
#end
}
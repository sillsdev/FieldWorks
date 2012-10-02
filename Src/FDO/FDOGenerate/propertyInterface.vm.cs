## --------------------------------------------------------------------------------------------
## Copyright (C) 2006 SIL International. All rights reserved.
##
## Distributable under the terms of either the Common Public License or the
## GNU Lesser General Public License, as specified in the LICENSING.txt file.
##
## NVelocity template file
## This file is used by the FdoGenerate task to generate the source code from the XMI
## database model.
##		//  Card
##			/// <summary></summary>
##			Unknown,
##			/// <summary></summary>
##			Basic,
##			/// <summary></summary>
##			Atomic,
##			/// <summary></summary>
##			Sequence,
##			/// <summary></summary>
##			Collection,
## --------------------------------------------------------------------------------------------
#set( $propComment = $prop.Comment )
#set( $propNotes = $prop.Notes )
		/// ------------------------------------------------------------------------------------
		/// <summary>
#if( $prop.Signature == "MultiString" || $prop.Signature == "MultiUnicode" || $prop.Cardinality.ToString() == "Sequence" || $prop.Cardinality.ToString() == "Collection" )
## These properties only have a 'getter'.
		/// Get the ${prop.Name}
#if ($propComment != "")
		///
$propComment
#end
		/// </summary>
#if ($propNotes != "")
		/// <remarks>
$propNotes
		/// </remarks>
#end
		/// ------------------------------------------------------------------------------------
#if( $prop.Signature == "MultiString" || $prop.Signature == "MultiUnicode" )
#if ($prop.Signature == "MultiString")
		IMultiString ${prop.NiuginianPropName}
#else
		IMultiUnicode ${prop.NiuginianPropName}
#end
#else
#set( $propTypeInterfaceBase = $fdogenerate.GetClass($prop.Signature) )
#if( $prop.IsOwning)
## Owning col/seq.
#if( $prop.Cardinality.ToString() == "Collection" )
		IFdoOwningCollection<I$propTypeInterfaceBase> $prop.NiuginianPropName
#else
		IFdoOwningSequence<I$propTypeInterfaceBase> $prop.NiuginianPropName
#end
#elseif ( $prop.Cardinality.ToString() == "Collection" )
		IFdoReferenceCollection<I$propTypeInterfaceBase> $prop.NiuginianPropName
#else
##This has to be Reference Sequence
#if ($class.Name == "Segment" && $prop.Name == "Analyses")
		IFdoReferenceSequence<IAnalysis> $prop.NiuginianPropName
#else
		IFdoReferenceSequence<I$propTypeInterfaceBase> $prop.NiuginianPropName
#end
#end
#end
		{
			get;
			// No "setter" property is needed.
			// One "gets" the accessor and uses that to work with the property.
		}
#elseif( $prop.Cardinality.ToString() != "Unknown" )
## These properties have both a 'getter' and a 'setter'.
		/// Get or set the ${prop.Name}
#if ($propComment != "")
		///
$propComment
#end
		/// </summary>
#if ($propNotes != "")
		/// <remarks>
$propNotes
		/// </remarks>
#end
		/// ------------------------------------------------------------------------------------
#if($prop.Signature == "String")
		ITsString $prop.NiuginianPropName
#elseif($prop.Cardinality.ToString() == "Atomic")
#set( $propReturnType = $fdogenerate.GetClass($prop.Signature) )
		I$propReturnType $prop.NiuginianPropName
#else
		$prop.CSharpType $prop.NiuginianPropName
#end
		{
			get;
#if( !$prop.IsSetterInternal)
			set;
#end
		}
#else
		// 'Unknown' cardinality. Force compiler to fail.
		willNotCompile;
#end
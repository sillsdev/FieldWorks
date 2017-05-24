## --------------------------------------------------------------------------------------------
## Copyright (c) 2006-2013 SIL International
## This software is licensed under the LGPL, version 2.1 or later
## (http://www.gnu.org/licenses/lgpl-2.1.html)
##
## NVelocity template file
## This file is used by the LcmGenerate task to generate the source code from the XMI
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
#set( $propTypeInterfaceBase = $lcmgenerate.GetClass($prop.Signature) )
#if( $prop.IsOwning)
## Owning col/seq.
#if( $prop.Cardinality.ToString() == "Collection" )
		ILcmOwningCollection<I$propTypeInterfaceBase> $prop.NiuginianPropName
#else
		ILcmOwningSequence<I$propTypeInterfaceBase> $prop.NiuginianPropName
#end
#elseif ( $prop.Cardinality.ToString() == "Collection" )
		ILcmReferenceCollection<I$propTypeInterfaceBase> $prop.NiuginianPropName
#else
##This has to be Reference Sequence
#if ($class.Name == "Segment" && $prop.Name == "Analyses")
		ILcmReferenceSequence<IAnalysis> $prop.NiuginianPropName
#else
		ILcmReferenceSequence<I$propTypeInterfaceBase> $prop.NiuginianPropName
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
#set( $propReturnType = $lcmgenerate.GetClass($prop.Signature) )
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
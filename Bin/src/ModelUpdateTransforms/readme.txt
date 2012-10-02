Created March 15, 2002 Larry Hayashi

The transforms in this folder are used to update FieldWorks XML Project files whenever a change is made to the conceptual model.

Each file name is for the following form:

PatchNumber.Transform.AssociatedDateOfCmChanges.TransformNumber.xsl

e.g.

001.Feb.5.2002.1.xsl

The PatchNumber is a sequential number from 1 and up.

The AssociatedDateOfCmChanges corresponds to the changes that are listed in the \fw\bin\src\xmi\conceptualmodelchanges.txt file.

For model changes that require more than one transform passthrough, a TransformNumber is included on the end. A number greater than 1 indicates that that the transform is part of a set of transforms necessary to update data to conform to model changes of a particular date.

You can apply these changes using MSXSL or any other XSL processor.

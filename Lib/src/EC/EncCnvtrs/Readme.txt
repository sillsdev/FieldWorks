Steps needed to make changes to the core EncConverters assembly:

1) change the version in the fw\lib\src\ec\EncCnvtrs\AssemblyInfo.cs (e.g. from 2.6.1.0 to 2.6.2.0)

This is needed or the installer won't overwrite the previous version of the assembly with the same version #

1b) repeat for the applications which use IEncConverter as defined in the core assembly as a base class. Currently,
this is only fw\lib\src\ec\AIGuesserEC\Properties\AssemblyInfo.cs and fw\Lib\src\EC\SilIndicEncConverters\AssemblyInfo.cs

These 'sub-classes' *must* have a new version which is different from the past version (or they won't be able to co-exist
with older versions of the interface).

1c) adjust the information in fw\lib\src\ec\EncCnvtrs\DirectableEncConverters.cs#BindToType to deal with the new version #
of the assembly.

2) create a new version of the publisher policy.

This means edit fw\Lib\src\EC\Installer\GAC MergeModule\EncCnvtrsPubPolicy.config and update it for the new version and
then execute the following command line (from the "fw\Lib\src\EC\Installer\GAC MergeModule" folder):

al /link:EncCnvtrsPubPolicy.config /out:policy.2.2.SilEncConverters22.dll /keyfile:"..\..\EncCnvtrs\FieldWorks.snk" /v:2.6.2.0

Note that you should make the version number (i.e. the last parameter) the same as the assembly version from step 1.

3) Open the "fw\lib\src\EC\Installer\GAC MergeModule\GAC MergeModule.vdproj" project in VS.Net 2005

4) Find the file SilEncConverters22.tlb in the project folder, right click on it and choose, "Find in Editor".

This will bring up the "File System on Target Machine" folder and you will see a sub-folder, Module Retargetable Folder, and
a sub-folder of that with some version # (e.g. 2.6.1.0). Change this value to whatever the new version number you gave the
assembly in step 1 above (e.g. 2.6.2.0).

This will cause the files in this folder to be put into the "C:\Program Files\Common Files\SIL\2.6.2.0" folder (so it won't
overwrite any previous version already installed by some other client application.

After you build the GAC merge module, you have to run the Orca tool to manually edit the merge module to set some parameters.
Specifically, run Orca and open the GAC MM with it. Then go to the CustomAction table listed on the left (click on it). The
table's contents appear on the right. On each line where the Source value is "InstallUtil", you need to add 2048 to the Type
value (e.g. if it originally was 1025, you copy that to Calc add 2048 and past the result (3073) back in that value and save it.
Then and only then can you check in the MM.

4b) repeat for the fw\Lib\src\EC\Installer\AIGuesserMM\AIGuesserMM.vdproj and
fw\Lib\src\EC\Installer\SILConvertersOptionsInstallerMM\ConverterPackages\IndicConverters\IndicConverters.vdproj
projects so that they will also be put in the proper sub-folder to match with the version of the core assembly.

That is, repeat the renaming of the version folder (2.6.2...) as well as the Orca thing (since they also involve managed
custom actions (see Alistair for futher details).

4c) do the Orca thing also for fw\Lib\Lib\src\EC\Installer\SCOfficeMM\SCOfficeMM.vdproj (aka.
fw\Installer\ExternalEncodingConverters\SCOfficeMM.msm (it has custom actions as well)

5) Once you rebuild the EncCnvtrs project, then you need to update the references on all client applications that use this
assembly to begin using the new version (i.e. I'm not sure about NAnt, but the VS.Net IDE will not automatically begin linking
and using the new version until you literally go into the 'References' folder in the Solution Explorer for the client project,
delete the SilEncConverters22 reference, and then do Add Reference and browse for the new version (which is essentially the
same assembly path, but with a different version #).

6) It's possible that the three Word document templates will need to have their "references refreshed". The easiest way
to do this is to install the new version of the GAC assembly on a new system (no older versions). Then, see if the Word DOTs
actually work. If they don't, then you probably need to edit them in the VBA editor (Alt+F11), click Tools, References,
uncheck the EncConverters... entry, click OK to exit, click Tools, References again (sic), check the EncConverters entry
click OK and then Save. The Save process will invalidate the signature, so it then needs to be sent to Alistair for the
dot to be signed. Once it is signed, then you have to manually rebuild the merge module for the three dots:

fw\Lib\release\Consistent Spelling Checker 152sc.dot    fw\Lib\src\EC\Installer\CscMM\CscMM.vdproj
fw\Lib\release\Data Conversion Macro 0250.dot           fw\Lib\src\EC\Installer\DcmMM\DcmMM.vdproj
fw\Lib\release\SpellFixer.dot                           fw\Lib\src\EC\Installer\SpellFixerEcMM\SpellFixerEcMM.vdproj

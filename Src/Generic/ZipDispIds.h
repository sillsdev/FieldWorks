#ifndef __ZIPDISPIDS_H__
#define __ZIPDISPIDS_H__

/*
 * Xceed Zip Dispatch IDs
 * Copyright 1998-1999, Xceed Software Inc.
 *
 * Description:
 *    This header contains the definitions of the dispatch ids for the XceedZip
 *    and XceedCompression controls.
 *
 * Notes on usage:
 * Notes on implementation:
 *    - Property ids are from    1 to 2999
 *    - Method ids are from   3000 to 5999
 *    - Event ids are from    6000 to 8999
 *
 * TODOs:
 *
 */

// ------------------------------------------------------------------
// Zip Control
// ------------------------------------------------------------------

// ------------------------------------------------------------------
// XceedZip Properties
//

// "Zip File" category ( 100 to 199 )

#define XCD_ZIP_CATID_ZIPFILE                         1

#define XCD_ZIP_DISPID_ZIPFILENAME                    100
#define XCD_ZIP_DISPID_SPLITSIZE                      101
#define XCD_ZIP_DISPID_SPANMULTIPLEDISKS              102
#define XCD_ZIP_DISPID_USETEMPFILE                    103
#define XCD_ZIP_DISPID_TEMPFOLDER                     104
#define XCD_ZIP_DISPID_FIRSTDISKFREESPACE             105
#define XCD_ZIP_DISPID_MINDISKFREESPACE               106

// "Folders" category ( 200 to 299 )

#define XCD_ZIP_CATID_FOLDERS                         2

#define XCD_ZIP_DISPID_BASEPATH                       200
#define XCD_ZIP_DISPID_PRESERVEPATHS                  201
#define XCD_ZIP_DISPID_PROCESSSUBFOLDERS              202
#define XCD_ZIP_DISPID_UNZIPTOFOLDER                  203

// "Filters" category ( 300 to 399 )

#define XCD_ZIP_CATID_FILTERS                         3

#define XCD_ZIP_DISPID_FILESTOPROCESS                 300
#define XCD_ZIP_DISPID_FILESTOEXCLUDE                 301
#define XCD_ZIP_DISPID_MINDATETOPROCESS               302
#define XCD_ZIP_DISPID_MAXDATETOPROCESS               303
#define XCD_ZIP_DISPID_MINSIZETOPROCESS               304
#define XCD_ZIP_DISPID_MAXSIZETOPROCESS               305
#define XCD_ZIP_DISPID_REQUIREDFILEATTRIBUTES         306
#define XCD_ZIP_DISPID_EXCLUDEDFILEATTRIBUTES         307
#define XCD_ZIP_DISPID_SKIPIFEXISTING                 308
#define XCD_ZIP_DISPID_SKIPIFNOTEXISTING              309
#define XCD_ZIP_DISPID_SKIPIFOLDERDATE                310
#define XCD_ZIP_DISPID_SKIPIFOLDERVERSION             311
#define XCD_ZIP_DISPID_EVENTSTOTRIGGER                312

// "Advanced" category ( 400 to 499 )

#define XCD_ZIP_CATID_ADVANCED                        4

#define XCD_ZIP_DISPID_COMPRESSIONLEVEL               400
#define XCD_ZIP_DISPID_EXTRAHEADERS                   401
#define XCD_ZIP_DISPID_ENCRYPTIONPASSWORD             402
#define XCD_ZIP_DISPID_ZIPOPENEDFILES                 403
#define XCD_ZIP_DISPID_BACKGROUNDPROCESSING           404
#define XCD_ZIP_DISPID_DELETEZIPPEDFILES              405

// "Self Extractor" category ( 500 to 599 )

#define XCD_ZIP_CATID_SELFEXTRACTOR                   5

#define XCD_ZIP_DISPID_SFXBINARYMODULE                501
#define XCD_ZIP_DISPID_SFXBUTTONS                     502
#define XCD_ZIP_DISPID_SFXMESSAGES                    503
#define XCD_ZIP_DISPID_SFXSTRINGS                     504
#define XCD_ZIP_DISPID_SFXDEFAULTPASSWORD             505
#define XCD_ZIP_DISPID_SFXDEFAULTUNZIPTOFOLDER        506
#define XCD_ZIP_DISPID_SFXEXISTINGFILEBEHAVIOR        507
#define XCD_ZIP_DISPID_SFXREADMEFILE                  508
#define XCD_ZIP_DISPID_SFXEXECUTEAFTER                509
#define XCD_ZIP_DISPID_SFXINSTALLMODE                 510
#define XCD_ZIP_DISPID_SFXPROGRAMGROUP                511
#define XCD_ZIP_DISPID_SFXPROGRAMGROUPITEMS           512
#define XCD_ZIP_DISPID_SFXEXTENSIONSTOASSOCIATE       513
#define XCD_ZIP_DISPID_SFXICONFILENAME                514
// Note: The following two are methods, but were added in the "properties" section
#define XCD_ZIP_DISPID_SFXLOADCONFIG                  515
#define XCD_ZIP_DISPID_SFXSAVECONFIG                  516
// Added in version 4.2
#define XCD_ZIP_DISPID_SFXFILESTOCOPY                 517
#define XCD_ZIP_DISPID_SFXFILESTOREGISTER             518
#define XCD_ZIP_DISPID_SFXREGISTRYKEYS                519

// Not categorized (2900 to 2999 )

#define XCD_ZIP_DISPID_ABORT                          2900
#define XCD_ZIP_DISPID_CURRENTOPERATION               2901

// ------------------------------------------------------------------
// XceedZip Methods
//

// Utility methods ( 3000 to 3099 )

#define XCD_ZIP_DISPID_ADDFILESTOPROCESS              3000
#define XCD_ZIP_DISPID_ADDFILESTOEXCLUDE              3001

// Zip operations ( 3100 to 3199 )

#define XCD_ZIP_DISPID_PREVIEWFILES                   3100
#define XCD_ZIP_DISPID_LISTZIPCONTENTS                3101
#define XCD_ZIP_DISPID_GETZIPFILEINFORMATION          3102
#define XCD_ZIP_DISPID_ZIP                            3103
#define XCD_ZIP_DISPID_UNZIP                          3104
#define XCD_ZIP_DISPID_REMOVEFILES                    3105
#define XCD_ZIP_DISPID_TESTZIPFILE                    3106
#define XCD_ZIP_DISPID_GETZIPCONTENTS                 3107

// Self extrator methods ( 3200 to 3299 )

#define XCD_ZIP_DISPID_SFXADDPROGRAMGROUPITEM         3200
#define XCD_ZIP_DISPID_SFXADDEXTENSIONTOASSOCIATE     3201
#define XCD_ZIP_DISPID_SFXRESETBUTTONS                3202
#define XCD_ZIP_DISPID_SFXRESETMESSAGES               3203
#define XCD_ZIP_DISPID_SFXRESETSTRINGS                3204
#define XCD_ZIP_DISPID_SFXCLEARBUTTONS                3205
#define XCD_ZIP_DISPID_SFXCLEARMESSAGES               3206
#define XCD_ZIP_DISPID_SFXCLEARSTRINGS                3207
#define XCD_ZIP_DISPID_CONVERT                        3208
#define XCD_ZIP_DISPID_SFXADDEXECUTEAFTER             3209
#define XCD_ZIP_DISPID_SFXADDFILETOCOPY               3210
#define XCD_ZIP_DISPID_SFXADDFILETOREGISTER           3211
#define XCD_ZIP_DISPID_SFXADDREGISTRYKEY              3212

// Other methods

#define XCD_ZIP_DISPID_GETERRORDESCRIPTION            5000
#define XCD_ZIP_DISPID_LICENSE                        5999

// ------------------------------------------------------------------
// XceedZip Events
//

// Zip operations ( 6000 to 6099 )

#define XCD_ZIP_DISPID_PREVIEWINGFILE                 6000
#define XCD_ZIP_DISPID_LISTINGFILE                    6001
#define XCD_ZIP_DISPID_QUERYMEMORYFILE                6002
#define XCD_ZIP_DISPID_ZIPPINGMEMORYFILE              6003
#define XCD_ZIP_DISPID_ZIPPREPROCESSINGFILE           6004
#define XCD_ZIP_DISPID_UNZIPPREPROCESSINGFILE         6005
#define XCD_ZIP_DISPID_UNZIPPINGMEMORYFILE            6006
#define XCD_ZIP_DISPID_REMOVINGFILE                   6007
#define XCD_ZIP_DISPID_TESTINGFILE                    6008
#define XCD_ZIP_DISPID_DELETINGFILE                   6009
#define XCD_ZIP_DISPID_CONVERTPREPROCESSINGFILE       6010

// Status ( 6100 to 6199 )

#define XCD_ZIP_DISPID_FILESTATUS                     6100
#define XCD_ZIP_DISPID_GLOBALSTATUS                   6101
#define XCD_ZIP_DISPID_SKIPPINGFILE                   6102
#define XCD_ZIP_DISPID_WARNING                        6103
#define XCD_ZIP_DISPID_ZIPCONTENTSSTATUS              6104

// User intervention ( 6200 to 6299 )

#define XCD_ZIP_DISPID_INSERTDISK                     6200
#define XCD_ZIP_DISPID_DISKNOTEMPTY                   6201
#define XCD_ZIP_DISPID_INVALIDPASSWORD                6202
#define XCD_ZIP_DISPID_REPLACINGFILE                  6203

// Other events ( 8900 to 8999 )

#define XCD_ZIP_DISPID_PROCESSCOMPLETED               8900
#define XCD_ZIP_DISPID_ZIPCOMMENT                     8901

// ------------------------------------------------------------------
// Compression Control
// ------------------------------------------------------------------

// ------------------------------------------------------------------
// XceedCompression Properties
//

#define XCD_CMP_DISPID_COMPRESSIONLEVEL               100
#define XCD_CMP_DISPID_ENCRYPTIONPASSWORD             101

// ------------------------------------------------------------------
// XceedCompression Methods
//

#define XCD_CMP_DISPID_COMPRESS                       3000
#define XCD_CMP_DISPID_UNCOMPRESS                     3001
#define XCD_CMP_DISPID_CALCULATECRC                   3002
#define XCD_CMP_DISPID_GETERRORDESCRIPTION            5000
#define XCD_CMP_DISPID_LICENSE                        5999

// ------------------------------------------------------------------
// XceedZipItem object
// ------------------------------------------------------------------

//
// Properties
//

#define XCD_ZIPITEM_DISPID_FILENAME                   100
#define XCD_ZIPITEM_DISPID_FILENAMEONDISK             101
#define XCD_ZIPITEM_DISPID_COMMENT                    102
#define XCD_ZIPITEM_DISPID_SIZE                       103
#define XCD_ZIPITEM_DISPID_COMPRESSEDSIZE             104
#define XCD_ZIPITEM_DISPID_COMPRESSIONRATIO           105
#define XCD_ZIPITEM_DISPID_ATTRIBUTES                 106
#define XCD_ZIPITEM_DISPID_CRC                        107
#define XCD_ZIPITEM_DISPID_LASTMODIFIED               108
#define XCD_ZIPITEM_DISPID_LASTACCESSED               109
#define XCD_ZIPITEM_DISPID_CREATED                    110
#define XCD_ZIPITEM_DISPID_COMPRESSIONMETHOD          111
#define XCD_ZIPITEM_DISPID_ENCRYPTED                  112
#define XCD_ZIPITEM_DISPID_DISKNUMBER                 113
#define XCD_ZIPITEM_DISPID_EXCLUDED                   114
#define XCD_ZIPITEM_DISPID_EXCLUDEDREASON             115

// ------------------------------------------------------------------
// XceedZipItems collection object
// ------------------------------------------------------------------

//
// Properties
//

#define XCD_ZIPITEMS_DISPID_ITEM                      100
#define XCD_ZIPITEMS_DISPID_COUNT                     101


#endif // __ZIPDISPIDS_H__

//
// END_OF_FILE
//

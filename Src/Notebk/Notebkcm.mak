# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#

BUILD_PRODUCT=NotebkCM
# BUILD_EXTENSION is empty indicating that there is no main executable target (yet).
BUILD_EXTENSION=
BUILD_REGSVR=1

RN_SRC=$(BUILD_ROOT)\Src\NoteBk
NB_XML=$(BUILD_ROOT)\src\Notebk\XML
CELLAR_SRC=$(BUILD_ROOT)\src\Cellar\Lib

# Set the USER_INCLUDE environment variable.
UI=$(RN_SRC);$(CELLAR_SRC)
!IF "$(USER_INCLUDE)"!=""
USER_INCLUDE=$(UI);$(USER_INCLUDE)
!ELSE
USER_INCLUDE=$(UI)
!ENDIF

XML_INC=$(NB_XML)

!INCLUDE "$(BUILD_ROOT)\bld\_init.mak"

PATH=$(COM_OUT_DIR);$(PATH)

# === Object Lists ===

SQL_MAIN=Notebk.sql
SQO_MAIN=Notebk.sqo

# === Targets ===
!INCLUDE "$(BUILD_ROOT)\bld\_targ.mak"

# === Rules ===
PCHNAME=NotebkCM

ARG_SRCDIR=$(RN_SRC)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

ARG_SRCDIR=$(NB_XML)
!INCLUDE "$(BUILD_ROOT)\bld\_rule.mak"

# === Custom Targets ===

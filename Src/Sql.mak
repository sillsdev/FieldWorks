# Input
# =====
# BUILD_ROOT: d:\FieldWorks
# BUILD_TYPE: d, r, p
# BUILD_CONFIG: Debug, Release, Profile
#
# REVIEW SteveMc: should the output SQL script go into $(BUILD_ROOT)\Bin instead of
# $(BUILD_ROOT)\Output\Common?

CAT=$(BUILD_ROOT)\Bin\cat --squeeze-blank

!IF "$(COM_OUT_DIR)"==""
COM_OUT_DIR=$(BUILD_ROOT)\Output\Common
!ENDIF

SQL_MAIN=NewLangProj.sql

SQL_DST_MAIN=$(COM_OUT_DIR)\$(SQL_MAIN)

SQL_ALL=$(COM_OUT_DIR)\FwCellar.sql \
 $(COM_OUT_DIR)\FeatSys.sql \
 $(COM_OUT_DIR)\Notebk.sql \
 $(COM_OUT_DIR)\Ling.sql \
 $(COM_OUT_DIR)\Scripture.sql \
 $(COM_OUT_DIR)\LangProj.sql \


# default target
build: sql

main: sql

delmain: delsql

dirs: $(COM_OUT_DIR)

sql: $(SQL_DST_MAIN)

delsql:
	if exist "$(SQL_DST_MAIN)" del $(SQL_DST_MAIN)

delobjs:

clean: delmain

cleancom:

register: build

unregister:

proxystub:

delps:

regps:

unregps:

$(COM_OUT_DIR):; if not exist "$@/$(NUL)" $(BUILD_ROOT)\bin\mkdir.exe -p "$@"

$(SQL_DST_MAIN): dirs $(SQL_ALL)
	$(CAT) $(SQL_ALL) >$(SQL_DST_MAIN)

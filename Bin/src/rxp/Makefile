VERSION=	1.4.7

CHAR_SIZE=16

DEBUG=		-g
CC=		gcc
GCCFLAGS=	-Wall -ansi -pedantic
# HP-UX users may need to add -D_HPUX_SOURCE
CFLAGS=		$(GCCFLAGS) $(DEBUG) -O -DCHAR_SIZE=$(CHAR_SIZE)
LDFLAGS=	$(DEBUG)

# You may well have to change this line.  Just "LIBS=" will probably work.
LIBS:sh=	[ -f /usr/lib/libsocket.a ] && echo "-lnsl -lsocket -ldl" || echo ""

CATALOG=	catalog.o catutil.o resolve.o entityopener.o
CATSRC=		catalog.c catutil.c resolve.c entityopener.c catalog_dtd.c
CATINC=		catalog.h catutil.h
NORMSRC=	nf16check.c nf16data.c
NORMINC=	nf16check.h nf16data.h

COMMON=		xmlparser.o url.o charset.o string16.o ctype16.o dtd.o \
		input.o stdio16.o system.o hash.o version.o namespaces.o \
		http.o nf16check.o nf16data.o
SOURCES=	rxp.c xmlparser.c url.c charset.c string16.c ctype16.c dtd.c \
		input.c stdio16.c system.c hash.c version.c \
		namespaces.c http.c infoset-print.c $(CATSRC) $(NORMSRC)
INCLUDES=	xmlparser.h url.h charset.h string16.h ctype16.h dtd.h \
		input.h stdio16.h system.h hash.h version.h rxputil.h \
		namespaces.h http.h infoset-print.h $(CATINC) $(NORMINC)

#DOCS=		RELNOTES COPYRIGHT COPYING rxp.1 Manual Threads
# RELNOTES is out of date
DOCS=		COPYRIGHT COPYING rxp.1 Manual Threads

DISTDIR=	/home/ftp/pub/richard

all:	rxp
everything:	rxp catalog

rxp:	rxp.o infoset-print.o librxp.a libcatalog.a
	$(CC) $(LDFLAGS) -o rxp rxp.o infoset-print.o -L. -lrxp -lcatalog $(LIBS)

rxp-web:	rxp-web.o librxp.a
	$(CC) $(LDFLAGS) -o rxp-web rxp-web.o -L. -lrxp $(LIBS)

catalog:	catalog-test.o librxp.a libcatalog.a
	$(CC) $(LDFLAGS) -o catalog catalog-test.o -L. -lrxp -lcatalog $(LIBS)

librxp.a:	$(COMMON)
	ar rv librxp.a $(COMMON)
	ranlib librxp.a

libcatalog.a:	$(CATALOG)
	ar rv libcatalog.a $(CATALOG)
	ranlib libcatalog.a

rxp.o: rxp.c xmlparser.h input.h dtd.h charset.h stdio16.h string16.h namespaces.h
rxp-web.o: rxp-web.c xmlparser.h input.h dtd.h charset.h stdio16.h string16.h namespaces.h

xmlparser.o:	xmlparser.c xmlparser.h input.h dtd.h charset.h stdio16.h string16.h ctype16.h namespaces.h nf16check.h

input.o:	input.c input.h dtd.h charset.h url.h stdio16.h ctype16.h

dtd.o:		dtd.c dtd.h charset.h

charset.o:	charset.c charset.h

stdio16.o:	stdio16.c stdio16.h charset.h

string16.o:	string16.c string16.h charset.h

system.o:	system.c system.h stdio16.h charset.h

url.o:		url.c url.h stdio16.h http.h

hash.o:		hash.c hash.h charset.h string16.h

ctype16.o:	ctype16.c ctype16.h charset.h system.h

namespaces.o:	namespaces.c namespaces.h charset.h rxputil.h charset.h string16.h

http.o:		http.c http.h stdio16.h string16.h url.h

version.o:	version.c version.h

infoset-print.o:	infoset-print.c infoset-print.h xmlparser.h dtd.h ctype16.h

catalog.o:	catalog.c catalog_dtd.c $(INCLUDES)

catutil.o:	catutil.c catutil.h

resolve.o:	resolve.c catalog.h catutil.h

entityopener.o:	entityopener.c catalog.h catalog_dtd.c xmlparser.h input.h dtd.h

catalog-test.o:	catalog-test.c catalog.h

nf16check.o:	nf16check.c nf16check.h nf16data.h

nf16data.o:	nf16data.c nf16data.h

backup:
	rm -f ../XML2.tar
	tar cfh ../XML2.tar *.[ch] Makefile

dist:
	rm -rf rxp-$(VERSION).tar.gz rxp-$(VERSION)
	mkdir rxp-$(VERSION)
	cp -p $(SOURCES) $(INCLUDES) Makefile $(DOCS) rxp-$(VERSION)
	mkdir rxp-$(VERSION)/iso-8859
	cp -p iso-8859/iso-8859-* rxp-$(VERSION)/iso-8859
	tar cvf rxp-$(VERSION).tar rxp-$(VERSION)
	rm -rf rxp-$(VERSION)
	gzip -v rxp-$(VERSION).tar

dist-install: dist
	mv rxp-$(VERSION).tar.gz $(DISTDIR)
#	sed s/_VERSION_/$(VERSION)/ <rxp-release.xml | sed s/_DATE_/`date +%Y%m%d`/ >$(DISTDIR)/rxp-release.xml
#	cd $(DISTDIR); rm rxp.tar.gz; ln -s rxp-$(VERSION).tar.gz rxp.tar.gz

import string
import sys

if '-icu' in sys.argv:
   print u'es'
else:
   i = sys.argv.index('-i')
   s= sys.argv[i+1]
   print string.swapcase(s)

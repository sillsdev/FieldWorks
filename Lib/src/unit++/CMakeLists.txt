PROJECT(UnitPP,C)
INCLUDE_DIRECTORIES("../")
ADD_LIBRARY("unit++" main.cc optmap.cc tester.cc "unit++.cc")

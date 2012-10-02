"""
	unitassert

	Description:
		An IronPython script using unittest doesn't crashes on asserts.
		(22 March 2007.) Even if the bug is fixed, these asserts are
		helpful for counting up pass/failure.

"""

class UnitAssert:
	"""Replacement for asserts in IronPython and unittest"""

	def __init__(self):
		self.passed = 0
		self.failures = 0
		self.failureMessages = []

	def assertEqual(self, arg1, arg2, failureMessage, testName):
		""" Replacement for assertEqual"""
		if arg1 != arg2:
			self.failures += 1
			self.failureMessages.append([testName, failureMessage])
		else:
			self.passed += 1

	def assertNotEqual(self, arg1, arg2, failureMessage, testName):
		""" Replacement for assertNotEqual"""
		if arg1 == arg2:
			self.failures += 1
			self.failureMessages.append([testName, failureMessage])
		else:
			self.passed += 1

	def showpassfail(self):
		"""Show accumulated unit test failures"""

		print " "
		print "======================================================================"
		if self.failures > 0:
			for fail in self.failureMessages:
				print "FAIL: " + fail[0] + ", assertion error: "  + fail[1]
			print "----------------------------------------------------------------------"
		print "FAILED (failures = %s)" % self.failures
		print "PASSED (passed = %s)" % self.passed
		print "======================================================================"

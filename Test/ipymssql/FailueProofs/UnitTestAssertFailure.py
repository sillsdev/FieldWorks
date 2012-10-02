import unittest

class testX(unittest.TestCase):
	def testx(self):
		x = 1
		#self.assertEqual(x, 2, "1 doesn't equal 2")
		assert x == 2, "1 doesn't equal 2"

if __name__ == '__main__':
	unittest.main()

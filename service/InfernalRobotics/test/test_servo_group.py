import unittest
import krpctest

class TestServoGroup(krpctest.TestCase):

    @classmethod
    def setUpClass(cls):
        krpctest.new_save()
        krpctest.launch_vessel_from_vab('InfernalRobotics')
        krpctest.remove_other_vessels()
        cls.conn = krpctest.connect(cls)
        cls.ir = cls.conn.infernal_robotics
        cls.vessel = cls.conn.space_center.active_vessel

    @classmethod
    def tearDownClass(cls):
        cls.conn.close()

    def test_servo_group(self):
        group1 = self.ir.servo_group_with_name(self.vessel, 'Group1')
        group2 = self.ir.servo_group_with_name(self.vessel, 'Group2')
        self.assertEqual('Group1', group1.name)
        self.assertEqual(['Hinge', 'Rail', 'Rotatron'], sorted(x.name for x in group1.servos))
        self.assertEqual(['Adjustable Rail', 'IR Rotatron', 'Powered Hinge'], sorted(x.title for x in group1.parts))
        self.assertEqual('Group2', group2.name)
        self.assertEqual(['DockingFree', 'DockingRotatron'], sorted(x.name for x in group2.servos))
        self.assertEqual(
            ['Docking Washer Standard', 'Docking Washer Standard (Free Moving)'],
            sorted(x.title for x in group2.parts))

    def test_servo_with_name(self):
        group = self.ir.servo_group_with_name(self.vessel, 'Group1')
        servo = group.servo_with_name('Rotatron')
        self.assertEqual(servo.name, 'Rotatron')
        self.assertIsNone(group.servo_with_name('Foo'))

if __name__ == '__main__':
    unittest.main()
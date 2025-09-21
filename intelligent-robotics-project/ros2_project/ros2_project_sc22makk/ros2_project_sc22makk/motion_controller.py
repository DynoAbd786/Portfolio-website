# Module for controlling robot movement - simplified for navigation and scanning only

from geometry_msgs.msg import Twist
import time

class MotionController:
    def __init__(self, node):
        self.node = node
        
        # Create publisher for robot movement
        self.publisher = self.node.create_publisher(Twist, '/cmd_vel', 10)
        
        # Define standard velocities
        self.linear_speed = 0.4
        self.angular_speed = 0.3
        
        # Update rate management for low-spec systems
        self.last_cmd_time = 0
        self.cmd_interval = 0.3  # Send commands at most every 0.3 seconds
    
    def stop(self):
        velocity = Twist()
        self.publisher.publish(velocity)
        self.node.get_logger().info("Stopping")
    
    def move_forward(self, speed=None):
        velocity = Twist()
        velocity.linear.x = speed if speed is not None else self.linear_speed
        self.publisher.publish(velocity)
        self.node.get_logger().info(f"Moving forward at {velocity.linear.x} m/s")
    
    def move_backward(self, speed=None):
        velocity = Twist()
        velocity.linear.x = -1 * (speed if speed is not None else self.linear_speed)
        self.publisher.publish(velocity)
        self.node.get_logger().info(f"Moving backward at {abs(velocity.linear.x)} m/s")
    
    def rotate(self, direction=1.0):
        """Rotate with standard speed"""
        velocity = Twist()
        velocity.angular.z = self.angular_speed * direction
        self.publisher.publish(velocity)
        self.node.get_logger().info(f"Rotating {'left' if direction > 0 else 'right'}")

    def turn(self, direction, speed=None):
        """Combined forward motion and turning"""
        velocity = Twist()
        velocity.linear.x = speed if speed is not None else self.linear_speed
        velocity.angular.z = direction * self.angular_speed
        self.publisher.publish(velocity)
        self.node.get_logger().info(f"Turning {'right' if direction < 0 else 'left'} while moving forward")
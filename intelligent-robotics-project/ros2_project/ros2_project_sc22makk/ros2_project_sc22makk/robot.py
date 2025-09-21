# Main robot node for exploring and scanning
import threading
import sys, time
import rclpy
from rclpy.node import Node
from geometry_msgs.msg import Twist
from sensor_msgs.msg import Image, LaserScan
from rclpy.exceptions import ROSInterruptException
import signal

# Import our custom modules
from .color_detector import ColorDetector
from .motion_controller import MotionController
from .exploration_strategy import ExplorationStrategy

class Robot(Node):
    def __init__(self):
        super().__init__('robot')
        
        # Create modular components
        self.color_detector = ColorDetector(self)
        self.motion_controller = MotionController(self)
        self.exploration_strategy = ExplorationStrategy(self)
        
        # State flags
        self.exploring = True
        self.colors_detected = set()
        self.scan_completed = False
        
        # Store latest laser scan data for distance estimation
        self.latest_laser_scan = None
        
        # Subscribe to sensor topics
        self.laser_subscription = self.create_subscription(
            LaserScan, '/scan', self.laser_callback, 10)
        
        self.get_logger().info('Robot initialized and ready to explore')

    def laser_callback(self, msg):
        """Store the latest laser scan data"""
        self.latest_laser_scan = msg

    def update(self):
        # Main update function called in the control loop
        
        # Check if colors are detected
        if hasattr(self.color_detector, 'red_found') and self.color_detector.red_found:
            self.colors_detected.add("Red")
        if hasattr(self.color_detector, 'green_found') and self.color_detector.green_found:
            self.colors_detected.add("Green")
        if hasattr(self.color_detector, 'blue_found') and self.color_detector.blue_found:
            self.colors_detected.add("Blue")
        
        # Check if all RGB colors have been found
        all_colors_found = len(self.colors_detected) >= 3
        
        # Add a debug log to show which colors we've found so far
        if int(time.time()) % 5 == 0:
            self.get_logger().info(f"Colors detected so far: {', '.join(self.colors_detected)}")
            self.get_logger().info(f"All colors found: {all_colors_found}")
        
        # Process movement based on current state - now we only have exploration
        if self.exploring:
            # Continue exploring until the exploration strategy is complete
            self.exploration_strategy.update()
            
            # Check if exploration is complete
            if self.exploration_strategy.is_exploration_completed():
                self.get_logger().info('Exploration and scanning completed successfully!')
                if all_colors_found:
                    self.get_logger().info('All RGB colors were found during exploration!')
                else:
                    self.get_logger().info(f'Found {len(self.colors_detected)} colors: {", ".join(self.colors_detected)}')
                
                self.exploring = False  # Stop exploration mode
                self.motion_controller.stop()  # Stop the robot when done


def main():
    def signal_handler(sig, frame):
        robot.motion_controller.stop()
        rclpy.shutdown()

    # Initialize the node
    rclpy.init(args=None)
    robot = Robot()

    signal.signal(signal.SIGINT, signal_handler)
    thread = threading.Thread(target=rclpy.spin, args=(robot,), daemon=True)
    thread.start()

    try:
        while rclpy.ok():
            # Update robot behavior
            robot.update()
            
            # Sleep to prevent CPU hogging
            time.sleep(0.1)

    except ROSInterruptException:
        pass

    # Clean up
    robot.motion_controller.stop()
    robot.color_detector.cleanup()

if __name__ == '__main__':
    main()
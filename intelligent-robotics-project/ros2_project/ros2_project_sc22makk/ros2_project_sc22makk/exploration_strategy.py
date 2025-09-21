import time
import math
from geometry_msgs.msg import Twist, PoseStamped
from nav2_msgs.action import NavigateToPose
from rclpy.action import ActionClient

class ExplorationStrategy:
    def __init__(self, node):
        self.node = node
        self.motion_controller = node.motion_controller
        
        # Set up Nav2 action client for navigation
        self.nav_client = ActionClient(node, NavigateToPose, 'navigate_to_pose')
        self.navigation_in_progress = False
        
        # Exploration states - add approach blue state
        self.STATE_SCANNING = "scanning"
        self.STATE_TRAVELING = "traveling"
        self.STATE_ALIGN_BLUE = "align_blue"  # Align with blue block
        self.STATE_APPROACH_BLUE = "approach_blue"  # NEW state for approaching blue
        
        # Current state
        self.current_state = self.STATE_TRAVELING
        self.state_change_time = time.time()
        
        # Waypoints to visit (coordinates in the map frame) - keeping only one
        self.waypoints = [
            (0.928, -8.34),  # Target waypoint for scanning
        ]
        
        # Waypoint tracking
        self.current_waypoint_index = 0
        self.waypoint_reached = False  # Flag to track if the waypoint was reached
        
        # Scanning parameters - improved for proper 360° rotation
        self.scanning_start_time = 0
        self.rotation_angle = 0.0
        self.rotation_speed = 0.4
        self.last_scan_time = 0.0
        
        # Need full 2π radians (slightly more for safety)
        self.full_rotation = 2.0 * math.pi + 0.2
        
        self.scan_completed = False
        
        # Movement parameters
        self.forward_speed = 0.2   # m/s
        self.approach_speed = 0.1  # Slower speed for approaching
        
        # Blue alignment parameters
        self.blue_alignment_started = False
        self.blue_centered = False
        self.centering_tolerance = 0.1
        
        # Blue approach parameters
        self.blue_approach_complete = False
        self.target_distance = 0.5  # Target distance in meters
        self.distance_tolerance = 0.05  # Stop within ±5cm of target
        
        self.node.get_logger().info('Exploration strategy initialized with travel-then-scan pattern')
        self.node.get_logger().info(f'Target waypoint: {self.waypoints[0]}')
    
    def update(self):
        """Main update function for the exploration strategy"""
        current_time = time.time()
        
        # Handle each state
        if self.current_state == self.STATE_SCANNING:
            self.handle_scanning_mode(current_time)
        elif self.current_state == self.STATE_TRAVELING:
            self.handle_traveling_mode(current_time)
        elif self.current_state == self.STATE_ALIGN_BLUE:
            self.handle_blue_alignment_mode(current_time)
        elif self.current_state == self.STATE_APPROACH_BLUE:
            self.handle_blue_approach_mode(current_time)
    
    def is_exploration_completed(self):
        """Return True when exploration is fully completed, including blue approach if all colors found"""
        all_colors_found = len(self.node.colors_detected) >= 3
        
        if all_colors_found:
            # If all colors found, we need to complete the full sequence including approach
            return (self.waypoint_reached and self.scan_completed and 
                    self.blue_centered and self.blue_approach_complete)
        else:
            # If not all colors found, just complete the basic scan
            return self.waypoint_reached and self.scan_completed
    
    def handle_scanning_mode(self, current_time):
        """Improved scanning mode with proper angle tracking for 360° in-place rotation"""
        # If just entered scanning mode, initialize scan parameters
        if self.scanning_start_time == 0:
            self.scanning_start_time = current_time
            self.last_scan_time = current_time
            self.rotation_angle = 0.0
            self.scan_completed = False
            self.node.get_logger().info('Starting precise 360° scan at waypoint')
        
        # Calculate elapsed time since last update to determine rotation amount
        dt = current_time - self.last_scan_time
        self.last_scan_time = current_time
        
        # Calculate newly accumulated rotation angle
        new_rotation = self.rotation_speed * dt
        self.rotation_angle += new_rotation
        
        # Calculate scan progress percentage based on angle
        scan_progress = min(100, int((self.rotation_angle / self.full_rotation) * 100))
        
        # Log progress at 25% intervals
        progress_milestone = scan_progress // 25 * 25  # Round down to nearest 25%
        if hasattr(self, 'last_reported_progress') and self.last_reported_progress != progress_milestone and progress_milestone > 0:
            angle_degrees = int(self.rotation_angle * 180.0 / math.pi)
            self.node.get_logger().info(f'Scan progress: {progress_milestone}% ({angle_degrees}°)')
            self.last_reported_progress = progress_milestone
        elif not hasattr(self, 'last_reported_progress'):
            self.last_reported_progress = 0
        
        # If completed full rotation, finish scan
        if self.rotation_angle >= self.full_rotation:
            if not self.scan_completed:
                self.scan_completed = True
                self.node.get_logger().info('360-degree scan completed')
                self.node.get_logger().info(f'Colors found during scan: {", ".join(self.node.colors_detected)}')
                self.motion_controller.stop()
                
                # Mark waypoint as scanned
                self.waypoint_reached = True
                
                # Check if all colors were found
                all_colors_found = len(self.node.colors_detected) >= 3
                
                # If all colors found, move to blue alignment mode
                if all_colors_found:
                    self.node.get_logger().info('All RGB blocks found! Starting alignment with blue block...')
                    self.current_state = self.STATE_ALIGN_BLUE
                    self.state_change_time = current_time
                    self.blue_alignment_started = False
                    self.blue_centered = False
                
                return  # Important - return here to avoid continuing rotation
        
        # Continue rotating with pure angular velocity (no linear component)
        if not self.scan_completed:
            rotation_cmd = Twist()
            rotation_cmd.angular.z = self.rotation_speed  # Constant rotation speed
            rotation_cmd.linear.x = 0.0  # Ensure no forward motion
            self.motion_controller.publisher.publish(rotation_cmd)
            
            # Periodically log that we're still rotating (every 3 seconds)
            if int(current_time) % 3 == 0:
                angle_degrees = int(self.rotation_angle * 180.0 / math.pi)
                self.node.get_logger().info(f'Rotating: {angle_degrees}° of 360° completed')
    
    def handle_traveling_mode(self, current_time):
        """Handle the traveling mode behavior to reach the waypoint"""
        # Get target waypoint
        if len(self.waypoints) > 0:
            target_x, target_y = self.waypoints[0]
            
            # If navigation is not in progress, send a navigation goal
            if not self.navigation_in_progress:
                self.node.get_logger().info(f'Navigating to waypoint: ({target_x}, {target_y})')
                self.send_nav_goal(target_x, target_y)
        else:
            # No waypoints defined, skip to scanning
            self.current_state = self.STATE_SCANNING
            self.state_change_time = current_time
            self.node.get_logger().info('No waypoints defined, starting scan at current position')
    
    def handle_blue_alignment_mode(self, current_time):
        """Method to handle aligning with the center of the blue block"""
        # If just entered blue alignment mode, initialize
        if not self.blue_alignment_started:
            self.blue_alignment_started = True
            self.node.get_logger().info('Starting alignment with blue block...')
        
        # Check if blue is currently visible
        blue_visible = hasattr(self.node.color_detector, 'blue_found') and self.node.color_detector.blue_found
        
        if blue_visible:
            # Blue is visible, check its position
            blue_offset = self.node.color_detector.blue_center_offset
            
            # Log the offset periodically
            if int(current_time) % 2 == 0:
                self.node.get_logger().info(f'Blue block offset from center: {blue_offset:.2f}')
            
            # Check if blue is centered
            if abs(blue_offset) < self.centering_tolerance:
                # Blue is centered - stop and complete alignment
                if not self.blue_centered:
                    self.blue_centered = True
                    self.motion_controller.stop()
                    
                    # Calculate and log blue block information
                    blue_area = getattr(self.node.color_detector, 'blue_area', 0)
                    blue_x = getattr(self.node.color_detector, 'blue_center_x', 0)
                    blue_y = getattr(self.node.color_detector, 'blue_center_y', 0)
                    
                    self.node.get_logger().info('==================================')
                    self.node.get_logger().info('BLUE BLOCK CENTERED SUCCESSFULLY!')
                    self.node.get_logger().info(f'Blue block center: ({blue_x}, {blue_y})')
                    self.node.get_logger().info(f'Blue block area: {blue_area:.1f} pixels')
                    self.node.get_logger().info(f'Offset from center: {blue_offset:.3f}')
                    self.node.get_logger().info('==================================')
                    
                    # Move to approach mode
                    self.node.get_logger().info('Alignment complete. Starting approach to blue block...')
                    self.current_state = self.STATE_APPROACH_BLUE
                    self.state_change_time = current_time
                    self.blue_approach_complete = False
            else:
                # Not centered - adjust rotation direction based on offset
                rotation_cmd = Twist()
                
                # Use slower rotation speed for precision alignment
                alignment_speed = self.rotation_speed * 0.3
                
                # Determine rotation direction
                if blue_offset < 0:  # Blue is to the left
                    rotation_cmd.angular.z = alignment_speed  # Turn left
                else:  # Blue is to the right
                    rotation_cmd.angular.z = -alignment_speed  # Turn right
                    
                # Don't move forward
                rotation_cmd.linear.x = 0.0
                
                # Apply the rotation
                self.motion_controller.publisher.publish(rotation_cmd)
        else:
            # Blue not visible - rotate slowly to find it
            if not self.blue_centered:
                rotation_cmd = Twist()
                rotation_cmd.angular.z = self.rotation_speed * 0.4
                rotation_cmd.linear.x = 0.0
                self.motion_controller.publisher.publish(rotation_cmd)
                
                if int(current_time) % 3 == 0:
                    self.node.get_logger().info('Searching for blue block...')
    
    def handle_blue_approach_mode(self, current_time):
        """Approach the blue block using CV2 contour area as distance metric"""
        # Check if blue is still visible
        blue_visible = hasattr(self.node.color_detector, 'blue_found') and self.node.color_detector.blue_found
        
        if not blue_visible:
            # Lost sight of blue - stop and log
            self.motion_controller.stop()
            if int(current_time) % 3 == 0:
                self.node.get_logger().warn('Lost sight of blue block during approach. Stopping.')
            return
        
        # Get current contour area 
        if hasattr(self.node.color_detector, 'blue_area'):
            blue_area = self.node.color_detector.blue_area
            
            # Log raw area
            self.node.get_logger().info(f'Blue contour area: {blue_area:.1f} pixels')
            
            # Target area for 0.5m distance
            TARGET_AREA = 680000  # Raw contour area value from testing
            AREA_TOLERANCE = 1000  # Reasonable tolerance in raw pixel count
            
            # Check if we're at target area/distance
            if abs(blue_area - TARGET_AREA) <= AREA_TOLERANCE:
                # Reached target distance - stop and complete
                if not self.blue_approach_complete:
                    self.blue_approach_complete = True
                    self.motion_controller.stop()
                    self.node.get_logger().info('==================================')
                    self.node.get_logger().info('TARGET DISTANCE REACHED!')
                    self.node.get_logger().info(f'Final blue area: {blue_area:.1f} pixels')
                    self.node.get_logger().info('Exploration complete!')
                    self.node.get_logger().info('==================================')
            else:
                approach_cmd = Twist()
                
                # Blue is visible, center on it first if needed
                blue_offset = getattr(self.node.color_detector, 'blue_center_offset', 0.0)
                
                if abs(blue_offset) > self.centering_tolerance:
                    # Need to recenter first
                    correction_speed = min(0.15, abs(blue_offset) * 0.3)
                    approach_cmd.angular.z = correction_speed * (-1 if blue_offset > 0 else 1)
                    self.node.get_logger().info(f'Recentering during approach, offset: {blue_offset:.2f}')
                    
                    # Don't move forward while centering
                    approach_cmd.linear.x = 0.0
                else:
                    # Blue is centered - adjust distance based solely on raw area
                    if blue_area < TARGET_AREA - AREA_TOLERANCE:
                        # Area too small - move forward at constant speed
                        approach_cmd.linear.x = 0.10  # Fixed speed regardless of distance
                        self.node.get_logger().info('Area too small - moving toward blue block')
                    elif blue_area > TARGET_AREA + AREA_TOLERANCE:
                        # Area too large - move backward at constant speed
                        approach_cmd.linear.x = -0.10  # Fixed reverse speed
                        self.node.get_logger().info('Area too large - moving away from blue block')
                    else:
                        # At target distance
                        approach_cmd.linear.x = 0.0
                        self.node.get_logger().info('At target distance')
                
                # Apply the command
                self.motion_controller.publisher.publish(approach_cmd)
                self.node.get_logger().info(f'Command: linear.x={approach_cmd.linear.x}, angular.z={approach_cmd.angular.z}')
    
    def send_nav_goal(self, x, y, yaw=0.0):
        """Send a navigation goal to Nav2 with improved error handling"""
        # Wait for the action server
        self.node.get_logger().info('Waiting for navigation action server...')
        if not self.nav_client.wait_for_server(timeout_sec=5.0):
            self.node.get_logger().error('Navigation server not available after 5 seconds timeout')
            # Try a simple movement instead
            self.use_simple_movement()
            return False
        
        # Create the goal message
        goal_msg = NavigateToPose.Goal()
        goal_msg.pose.header.frame_id = 'map'
        goal_msg.pose.header.stamp = self.node.get_clock().now().to_msg()

        # Position
        goal_msg.pose.pose.position.x = x
        goal_msg.pose.pose.position.y = y
        goal_msg.pose.pose.position.z = 0.0

        # Orientation - ensure proper quaternion normalization
        goal_msg.pose.pose.orientation.z = math.sin(yaw / 2)
        goal_msg.pose.pose.orientation.w = math.cos(yaw / 2)
        goal_msg.pose.pose.orientation.x = 0.0
        goal_msg.pose.pose.orientation.y = 0.0

        # Send the goal with error handling
        try:
            self.navigation_in_progress = True
            self.send_goal_future = self.nav_client.send_goal_async(
                goal_msg, 
                feedback_callback=self.nav_feedback_callback
            )
            self.send_goal_future.add_done_callback(self.nav_goal_response_callback)
            self.node.get_logger().info(f'Sent navigation goal: ({x}, {y})')
            return True
        except Exception as e:
            self.node.get_logger().error(f'Error sending navigation goal: {e}')
            self.navigation_in_progress = False
            # On error, switch to scanning anyway at current position
            self.current_state = self.STATE_SCANNING
            self.state_change_time = time.time()
            return False
        
    def use_simple_movement(self):
        """Fallback to simple movement when navigation fails"""
        self.node.get_logger().info('Navigation failed. Scanning at current position instead.')
        # Switch to scanning mode at current position
        self.current_state = self.STATE_SCANNING
        self.state_change_time = time.time()

    def nav_goal_response_callback(self, future):
        """Callback when navigation goal is accepted/rejected"""
        try:
            goal_handle = future.result()
            if not goal_handle.accepted:
                self.node.get_logger().warn('Navigation goal REJECTED')
                self.navigation_in_progress = False
                
                # Even if rejected, try to scan at current location
                self.current_state = self.STATE_SCANNING
                self.state_change_time = time.time()
                return

            self.node.get_logger().info('Navigation goal ACCEPTED')
            self.get_result_future = goal_handle.get_result_async()
            self.get_result_future.add_done_callback(self.nav_result_callback)
        except Exception as e:
            self.node.get_logger().error(f'Error in navigation goal response: {e}')
            self.navigation_in_progress = False
            # On error, switch to scanning at current position
            self.current_state = self.STATE_SCANNING
            self.state_change_time = time.time()

    def nav_result_callback(self, future):
        """Callback when navigation is complete - switch to scanning"""
        try:
            status = future.result().status
            self.navigation_in_progress = False
            
            # Navigation completed (regardless of success/failure)
            self.node.get_logger().info(f'Navigation completed with status: {status}')
            
            # Start scanning
            self.current_state = self.STATE_SCANNING
            self.state_change_time = time.time()
            
        except Exception as e:
            self.node.get_logger().error(f'Error processing navigation result: {e}')
            self.navigation_in_progress = False
            
            # Even on error, try to scan anyway
            self.current_state = self.STATE_SCANNING
            self.state_change_time = time.time()

    def nav_feedback_callback(self, feedback_msg):
        """Process navigation feedback"""
        feedback = feedback_msg.feedback
        
        # Calculate remaining distance
        distance_remaining = feedback.distance_remaining
        
        # Log occasionally (not on every feedback to avoid spamming)
        if int(time.time()) % 5 == 0:  # Log every 5 seconds
            self.node.get_logger().info(f'Distance to waypoint: {distance_remaining:.2f}m')
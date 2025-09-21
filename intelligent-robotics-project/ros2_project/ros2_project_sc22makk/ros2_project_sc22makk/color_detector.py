# Module for detecting colors in camera feed - simplified to remove distance estimation

import cv2
import numpy as np
from sensor_msgs.msg import Image
from cv_bridge import CvBridge, CvBridgeError

class ColorDetector:
    def __init__(self, node):
        self.node = node
        
        # Color detection flags
        self.red_found = False
        self.green_found = False
        self.blue_found = False
        
        # Color detection parameters
        self.detection_threshold = 500
        
        # Initialize CV bridge and create display window
        self.bridge = CvBridge()
        cv2.namedWindow('RGB Detection', cv2.WINDOW_NORMAL)
        cv2.resizeWindow('RGB Detection', 640, 480)
        
        # Subscribe to camera feed
        self.subscription = self.node.create_subscription(
            Image, '/camera/image_raw', self.image_callback, 10)
    
    def image_callback(self, data):
        try:
            # Convert ROS image to OpenCV format
            image = self.bridge.imgmsg_to_cv2(data, 'bgr8')
            display_image = image.copy()
            
            # Reset color detection flags
            self.red_found = False
            self.green_found = False
            self.blue_found = False
            
            # Process image to detect colors
            hsv_image = cv2.cvtColor(image, cv2.COLOR_BGR2HSV)
            
            # Detect colors and draw visualization
            self._detect_red(hsv_image, display_image)
            self._detect_green(hsv_image, display_image)
            self._detect_blue(hsv_image, display_image)
            
            # Add status information to display
            self._add_status_info(display_image)
            
            # Show the processed image
            cv2.imshow('RGB Detection', display_image)
            cv2.waitKey(3)
            
        except CvBridgeError as e:
            self.node.get_logger().error(f"Error converting image: {e}")
    
    def _detect_red(self, hsv_image, display_image):
        # RED detection (need two ranges because hue wraps around in HSV)
        hsv_red_lower1 = np.array([0, 100, 100])
        hsv_red_upper1 = np.array([10, 255, 255])
        hsv_red_lower2 = np.array([160, 100, 100])
        hsv_red_upper2 = np.array([180, 255, 255])
        
        mask_red1 = cv2.inRange(hsv_image, hsv_red_lower1, hsv_red_upper1)
        mask_red2 = cv2.inRange(hsv_image, hsv_red_lower2, hsv_red_upper2)
        mask_red = cv2.bitwise_or(mask_red1, mask_red2)
        
        contours, _ = cv2.findContours(mask_red, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
        if contours:
            # Find largest contour
            c_red = max(contours, key=cv2.contourArea)
            red_area = cv2.contourArea(c_red)
            
            if red_area > self.detection_threshold:
                self.red_found = True
                self.node.colors_detected.add("Red")
                
                # Draw bounding box and center
                x, y, w, h = cv2.boundingRect(c_red)
                cv2.rectangle(display_image, (x, y), (x + w, y + h), (0, 0, 255), 2)
                cv2.putText(display_image, "RED", (x, y - 10),
                           cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0, 0, 255), 2)
    
    def _detect_green(self, hsv_image, display_image):
        # GREEN detection
        hsv_green_lower = np.array([40, 100, 100])
        hsv_green_upper = np.array([80, 255, 255])
        
        mask_green = cv2.inRange(hsv_image, hsv_green_lower, hsv_green_upper)
        
        contours, _ = cv2.findContours(mask_green, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
        if contours:
            # Find largest contour
            c_green = max(contours, key=cv2.contourArea)
            green_area = cv2.contourArea(c_green)
            
            if green_area > self.detection_threshold:
                self.green_found = True
                self.node.colors_detected.add("Green")
                
                # Draw bounding box and center
                x, y, w, h = cv2.boundingRect(c_green)
                cv2.rectangle(display_image, (x, y), (x + w, y + h), (0, 255, 0), 2)
                cv2.putText(display_image, "GREEN", (x, y - 10),
                           cv2.FONT_HERSHEY_SIMPLEX, 0.6, (0, 255, 0), 2)
    
    def _detect_blue(self, hsv_image, display_image):
        """Detect blue with extra information for centering and distance estimation"""
        # BLUE detection
        hsv_blue_lower = np.array([100, 100, 100])
        hsv_blue_upper = np.array([140, 255, 255])
        
        mask_blue = cv2.inRange(hsv_image, hsv_blue_lower, hsv_blue_upper)
        
        contours, _ = cv2.findContours(mask_blue, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
        if contours:
            # Find largest contour
            c_blue = max(contours, key=cv2.contourArea)
            blue_area = cv2.contourArea(c_blue)
            
            if blue_area > self.detection_threshold:
                self.blue_found = True
                self.node.colors_detected.add("Blue")
                
                # Calculate centroid using moments
                M = cv2.moments(c_blue)
                if M["m00"] != 0:
                    self.blue_center_x = int(M["m10"] / M["m00"])
                    self.blue_center_y = int(M["m01"] / M["m00"])
                    self.blue_area = blue_area
                    
                    # Store image dimensions for centering calculations 
                    height, width = hsv_image.shape[:2]
                    self.image_width = width
                    self.image_center_x = width // 2
                    
                    # Calculate centering offset (-1.0 to 1.0)
                    self.blue_center_offset = (self.blue_center_x - self.image_center_x) / (self.image_center_x)
                    
                    # Draw visualization 
                    x, y, w, h = cv2.boundingRect(c_blue)
                    cv2.rectangle(display_image, (x, y), (x + w, y + h), (255, 0, 0), 2)
                    cv2.circle(display_image, (self.blue_center_x, self.blue_center_y), 5, (0, 255, 255), -1)
                    cv2.putText(display_image, f"BLUE (area: {int(blue_area)})", (x, y - 10),
                               cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255, 0, 0), 2)
                    cv2.putText(display_image, f"Offset: {self.blue_center_offset:.2f}", (x, y + h + 20),
                               cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255, 255, 0), 2)
            else:
                self.blue_found = False
                if hasattr(self, 'blue_center_offset'):
                    delattr(self, 'blue_center_offset')
                    delattr(self, 'blue_area')
        else:
            self.blue_found = False
            if hasattr(self, 'blue_center_offset'):
                delattr(self, 'blue_center_offset')
                delattr(self, 'blue_area')
    
    def _add_status_info(self, display_image):
        # Show which colors have been detected so far
        cv2.putText(display_image, f"Colors found: {', '.join(self.node.colors_detected)}", 
                   (10, 30), cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255, 255, 255), 2)
        
        # Show navigation status
        if self.node.exploring:
            cv2.putText(display_image, "Mode: Exploring", 
                       (10, 60), cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255, 255, 255), 2)
        else:
            cv2.putText(display_image, "Mode: Exploration Complete", 
                       (10, 60), cv2.FONT_HERSHEY_SIMPLEX, 0.6, (255, 255, 255), 2)
    
    def cleanup(self):
        cv2.destroyAllWindows()
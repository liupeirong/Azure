import os
import cv2
import json
import time

from detector import Detector
from sender import Sender

# Can be used to change used camera in case multiple available
CAMERA_INDEX = int(os.getenv('OPENCV_CAMERA_INDEX', 0))

# Can be used for testing purposes to limit the amount of detections
# performed (0 means loop forever)
DETECTION_COUNT = int(os.getenv('DETECTION_COUNT', 0))

# These variables are set by the IoT Edge Agent
CONNECTION_STRING = os.getenv('EdgeHubConnectionString', False)
CA_CERTIFICATE = os.getenv('EdgeModuleCACertificateFile', False)
IS_EDGE = CONNECTION_STRING and CA_CERTIFICATE or False

if IS_EDGE:
    sender = Sender(CONNECTION_STRING, CA_CERTIFICATE)
else:
    sender = False

video_capture = cv2.VideoCapture(CAMERA_INDEX)
detector = Detector()
detection_index = 0

while True:
    capture = video_capture.read()
    if capture[0]:
        array = capture[1]
        type = 'captured'
    else:
        type = 'static'
        array = cv2.imread('data/dog.jpg')

    print('Using %s %ix%i image, threshold %s' % (type, array.shape[0], array.shape[1], DETECTION_THRESHOLD))
    time_before = time.time()
    result = detector.detect(array, DETECTION_THRESHOLD)
    detection_index += 1
    time_after = time.time()
    print('Detection took %s seconds' % (time_after - time_before))

    print(json.dumps(result, indent=4))

    if sender:
        msg_properties = {
            'detection_index': str(detection_index)
        }
        json_formatted = json.dumps(result)
        sender.send_event_to_output('detectionOutput', json_formatted, msg_properties, detection_index)

    if DETECTION_COUNT > 0 and detection_index >= DETECTION_COUNT:
        break

    # To avoid sending too frequently on hardware where detection is fast
    time.sleep(1)

# This stdout print is currently checked in the Travis CI script exactly
# as it is so pay attention if changing it
print('Program exiting normally')

# The content of this file is largely based on:
# https://github.com/Azure/azure-iot-sdk-python/blob/d3619f8d5ec0beca87b0d3b98833ae8053c39419/device/samples/iothub_client_sample_module_sender.py

import iothub_client
from iothub_client import IoTHubClient, IoTHubClientError, IoTHubTransportProvider
from iothub_client import IoTHubMessage, IoTHubMessageDispositionResult, IoTHubError, DeviceMethodReturnValue
import json
import __builtin__

# messageTimeout - the maximum time in milliseconds until a message times out.
# The timeout period starts at IoTHubClient.send_event_to_output.
# By default, messages do not expire.
MESSAGE_TIMEOUT = 1000

TWIN_CALLBACKS = 0
__builtin__.DETECTION_THRESHOLD = .5

# Default to use MQTT to communicate to IoT Edge
PROTOCOL = IoTHubTransportProvider.MQTT

def send_confirmation_callback(message, result, send_context):
    print('Confirmation for message [%d] received with result %s' % (send_context, result))

def device_twin_callback(update_state, payload, user_context):
    global TWIN_CALLBACKS
    print ( "\nTwin callback called with:\nupdateStatus = %s\npayload = %s\ncontext = %s" % (update_state, payload, user_context) )
    data = json.loads(payload)
    if "desired" in data and "DetectionThreshold" in data["desired"]:
        __builtin__.DETECTION_THRESHOLD = data["desired"]["DetectionThreshold"]
    if "DetectionThreshold" in data:
        __builtin__.DETECTION_THRESHOLD = data["DetectionThreshold"]
    TWIN_CALLBACKS += 1
    print ( "DetectionThreshold from twin: %s\n" % __builtin__.DETECTION_THRESHOLD )
    print ( "Total twin calls confirmed: %d\n" % TWIN_CALLBACKS )

class Sender(object):

    def __init__(self, connection_string, certificate_path=False,
                 protocol=PROTOCOL):
        self.client_protocol = protocol
        self.client = IoTHubClient(connection_string, protocol)
        # set the time until a message times out
        self.client.set_option('messageTimeout', MESSAGE_TIMEOUT)
        # sets the callback when a twin's desired properties are updated.
        self.client.set_device_twin_callback(device_twin_callback, self)
        # some embedded platforms need certificate information
        if certificate_path:
            self.set_certificates(certificate_path)

    def set_certificates(self, certificate_path):
        file = open(certificate_path, 'r')
        try:
            self.client.set_option('TrustedCerts', file.read())
            print('IoT Edge TrustedCerts set successfully')
        except IoTHubClientError as iothub_client_error:
            print('Setting IoT Edge TrustedCerts failed (%s)' % iothub_client_error)
        file.close()

    def send_event_to_output(self, outputQueueName, event, properties, send_context):
        if not isinstance(event, IoTHubMessage):
            event = IoTHubMessage(bytearray(event, 'utf8'))

        if len(properties) > 0:
            prop_map = event.properties()
            for key in properties:
                prop_map.add_or_update(key, properties[key])

        self.client.send_event_async(
            outputQueueName, event, send_confirmation_callback, send_context)

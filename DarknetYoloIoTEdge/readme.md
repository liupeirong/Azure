This sample converts [Darknet Yolo](https://github.com/pjreddie/darknet) to an Azure IoT edge module. The code in the iot-edge-darknet-module folder is largely similar to [this original GitHub repo](https://github.com/vjrantal/iot-edge-darknet-module). The differences are:
- It's a simplified version which supports CPU only. 
- It copies the darknet binary to the final Docker image in order to run demo mode.
- It includes code that takes detection threshold from IoT Hub in Azure.
- It connects to the IP/RTSP camera. 

### Start local Docker registry so that the docker image doesn't have to be pushed to Cloud during development
```sh
docker run -d -p 5000:5000 --restart=always --name registry registry:2
```

### Build the IoT module:
```sh 
docker build -f base/Dockerfile -t localhost:5000/iot-edge-darknet-base .
docker push localhost:5000/iot-edge-darknet-base
docker build -f darknet/Dockerfile -t localhost:5000/darknet-iot .
docker push localhost:5000/darknet-iot
docker build -t localhost:5000/iot-edge-darknet-module .
docker push localhost:5000/iot-edge-darknet-module
```

### Deploy the IoT module:
To deploy from command line:
```sh
az iot hub apply-configuration --device-id {iot edge device id} --hub-name {iot hub name} --content ./deployment.json
```

To deploy from Azure portal:
In the module's Docker container configuration, specify:
```json
{
 "HostConfig": {
   "Privileged": true
 },
 "Env": [
   "CAMERA_URL=rtsp://<user>:<pwd>@<ip>/axis-media/media.amp"
 ]
}
```

### Note on IP camera:
To do live stream image detection from an IP camera, here's the syntax: 
if the Camera IP is: ```http://<user>:<pwd>@<ip>/axis-cgi/mjpg/video.cgi?resolution=1280x720&camera=1&compression=0```
then specify its RTSP to VideoCapture in Python or cvCaptureFromFile in C: ```rtsp://<user>:<pwd>@<ip>/axis-media/media.amp```

### To change detection threshold from Azure IoT Hub
Go to the IoT edge device, then *Set Modules*, *Enable* module twin's desired properties, type in the desired property, for example:
```json
{
  "properties.desired": {
      "DetectionThreshold": 0.7
   }
}
```

### To run in demo mode
Download yolov3.weights, then run
```sh
./darknet detector demo cfg/coco.data cfg/yolov3.cfg /path/to/yolov3.weights rtsp://<user>:<pwd>@<ip>/axis-media/media.amp
```

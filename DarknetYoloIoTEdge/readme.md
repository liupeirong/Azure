This sample converts [Darknet Yolo](https://github.com/pjreddie/darknet) to an Azure IoT edge module. The code in the iot-edge-darknet-module folder is largely similar to [this original GitHub repo](https://github.com/vjrantal/iot-edge-darknet-module). The differences are:
- It's a simplified version which supports CPU only. 
- It [copies the darknet binary](/DarknetYoloIoTEdge/iot-edge-darknet-module/Dockerfile#L14) to the final Docker image in order to run demo mode.
- It [takes detection threshold](/DarknetYoloIoTEdge/iot-edge-darknet-module/sender.py#L28) from IoT Hub in Azure.
- It [connects](/DarknetYoloIoTEdge/iot-edge-darknet-module/module.py#L28) to the IP/RTSP camera. 

### Start local Docker registry so that the docker image doesn't have to be pushed to Cloud during development
```sh
docker run -d -p 5000:5000 --restart=always --name registry registry:2
```

### Build the IoT module:
```sh 
docker build -f base/Dockerfile -t <docker registry>/iot-edge-darknet-base .
docker push <docker registry>/iot-edge-darknet-base
docker build -f darknet/Dockerfile -t <docker registry>/darknet-iot .
docker push <docker registry>/darknet-iot
docker build -t <docker registry>/iot-edge-darknet-module .
docker push <docker registry>/iot-edge-darknet-module
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
To do live stream image detection from an IP camera, use the following syntax:
if the Camera IP is ```http://<user>:<pwd>@<ip>/axis-cgi/mjpg/video.cgi?resolution=1280x720&camera=1&compression=0```,
then pass its RTSP address ```rtsp://<user>:<pwd>@<ip>/axis-media/media.amp``` to ```VideoCapture``` in Python or ```cvCaptureFromFile``` in C. 

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
In the host machine, you may need to run ```xhost +local:docker``` or ```xhost local:``` to allow x11 forwarding from Docker.

from command line:
```sh
docker run --privileged -e DISPLAY=$DISPLAY -v /tmp/.X11-unix:/tmp/.X11-unix -e CAMERA_URL=$CAMERA_URL <docker registry>/iot-edge-darknet-module ./darknet detector demo cfg/coco.data cfg/yolo.cfg yolo.weights rtsp://<user>:<pwd>@<ip>/axis-media/media.amp 
```

from Azure portal, go to *Set Modules*, select the target module, type the following in the Docker container configuration:
```json
{
 "Env": [
   "CAMERA_URL=rtsp://<user>:<pwd>@<ip>/axis-media/media.amp",
   "DISPLAY=:0"
 ],
 "HostConfig": {
   "Privileged": true,
   "Binds": [
     "/tmp/.X11-unix:/tmp/.X11-unix"
   ]
 },
 "WorkingDir": "/",
 "Cmd": [
   "./darknet", "detector", "demo", "cfg/coco.data", "cfg/yolo.cfg", "yolo.weights", "rtsp://<user>:<pwd>@<ip>/axis-media/media.amp", "-thresh", "0.2"
 ]
}
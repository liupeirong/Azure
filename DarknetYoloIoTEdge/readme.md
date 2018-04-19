This sample converts [Darknet Yolo](https://github.com/pjreddie/darknet) to an Azure IoT edge module. The code in the iot-edge-darknet-module folder is largely similar to [this original GitHub repo](https://github.com/vjrantal/iot-edge-darknet-module). It's a simplified version which supports CPU only. It also includes code that takes detection threshold set from IoT Hub in Azure. 

### Start local Docker registry so that the docker image doesn't have to be pushed to Cloud during development
```sh
docker run -d -p 5000:5000 --restart=always --name registry registry:2
```

### To build the IoT module:
```sh 
docker build -f base/Dockerfile -t localhost:5000/iot-edge-darknet-base .
docker push localhost:5000/iot-edge-darknet-base
docker build -f darknet/Dockerfile -t localhost:5000/darknet-iot .
docker push localhost:5000/darknet-iot
docker build -t localhost:5000/iot-edge-darknet-module .
docker push localhost:5000/iot-edge-darknet-module
```

### To deploy the IoT module:
```sh
az iot hub apply-configuration --device-id {iot edge device id} --hub-name {iot hub name} --content ./deployment.json
```

### To connect to the camera:
Camera IP: http://<user>:<pwd>@<ip>/axis-cgi/mjpg/video.cgi?resolution=1280x720&camera=1&compression=0
Camera RTSP: rtsp://<user>:<pwd>@<ip>/axis-media/media.amp

### To run in IoT edge module mode:
{
 "HostConfig": {
   "Privileged": true
 }
}


Set Modules -> Module twin's desired properties
{
  "properties.desired": {
      "DetectionThreshold": 0.7
   }
}

### To run in demo mode
```sh
./darknet detector demo cfg/coco.data cfg/yolov3.cfg /data/yolov3.weights rtsp://<user>:<pwd>@<ip>/axis-media/media.amp
```

# Predict Remaining Useful Life using trained model

In this step, we use the model trained in the previous step to predict Remaining Useful Life on the data points in the stream.

### Limitations with Structured Streaming

* Joining two streams (or self-join) is not supported, so we find the max cycle and max counter for a device reading in a time window, join it back to the stream to get all the sensor data to do prediction.  Prediction is done on raw data. 

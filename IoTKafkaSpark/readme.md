# IoT pipeline with Kafka, Spark Structured Streaming, Azure Data Lake Store, Spark ML and Power BI

This example demonstrates how to build an end-to-end IoT pipeline with Kafka for [data ingestion](/IoTKafkaSpark/1.Ingest), Spark Structured Streaming for real time [data processing](/IoTKafkaSpark/2.Streaming), Azure Data Lake Store for storage independent of compute and query engines, Hive or Impala for [query](/IoTKafkaSpark/4.Query), Spark ML for training and prediction, and finally Power BI for realtime dashboards and visualization.  

In this sample scenario, a set of devices send their sensor data to Kafka, Spark processes the data and produces aggregates which can be queried by Hive or Impala, and visualized in Power BI.  Additionally, we also use Spark to train the sample data and use the trained model to predict remaining useful life of these devices. 

![Alt text](/IoTKafkaSpark/diagram.png?raw=true "Data Pipeline")

We use the "Turbofan Engine Degradation Simulation Data Set" in the [NASA Ames Prognostics Data Repository](http://ti.arc.nasa.gov/tech/dash/pcoe/prognostic-data-repository/) as sample data, and the [Azure Predictive Maintenance Prediction sample](https://gallery.cortanaintelligence.com/Collection/Predictive-Maintenance-Template-3) as basis for ML model.   

// 
// this app uses Gradient Boosted Tree to predict the remaining useful life of turbine engines
// download data to hdfs in datadir from NASA: https://ti.arc.nasa.gov/c/6/
//

val datadir = "/user/pliu/CMAPSS/"

// import training data, define structure for efficient loading
import org.apache.spark.sql.types._

val engineStruct = StructType(
  StructField("id", IntegerType) :: 
  StructField("cycle", IntegerType) :: 
  StructField("setting1", FloatType) ::
  StructField("setting2", FloatType) :: 
  StructField("setting3", FloatType) :: 
  StructField("s1", FloatType) :: 
  StructField("s2", FloatType) :: 
  StructField("s3", FloatType) :: 
  StructField("s4", FloatType) :: 
  StructField("s5", FloatType) :: 
  StructField("s6", FloatType) :: 
  StructField("s7", FloatType) :: 
  StructField("s8", FloatType) :: 
  StructField("s9", FloatType) :: 
  StructField("s10", FloatType) :: 
  StructField("s11", FloatType) :: 
  StructField("s12", FloatType) :: 
  StructField("s13", FloatType) :: 
  StructField("s14", FloatType) :: 
  StructField("s15", FloatType) :: 
  StructField("s16", FloatType) :: 
  StructField("s17", FloatType) :: 
  StructField("s18", FloatType) :: 
  StructField("s19", FloatType) :: 
  StructField("s20", FloatType) :: 
  StructField("s21", FloatType) :: 
  Nil
  )
  
val train_raw = spark.read.schema(engineStruct).option("delimiter", " ").csv(datadir + "train_FD001.txt")
// find the max cycle value per engine id
val train_maxcycle = train_raw.groupBy("id").max("cycle").select($"id".alias("id2"), $"max(cycle)".alias("maxc"))
// remaining useful life (RUL) = max cycle - current cycle
val train_labeled = train_raw.join(train_maxcycle, $"id" === $"id2").withColumn("RUL", $"maxc" - $"cycle")
// the features we will build the model on, and the label
val train_df = train_labeled.select("cycle", "s9", "s11", "s14", "s15", "RUL")

// convert features in dataframe into vectors for machine learning algorithms (transformer)
import org.apache.spark.ml.Pipeline
import org.apache.spark.ml.feature.VectorAssembler
import org.apache.spark.ml.evaluation.RegressionEvaluator
import org.apache.spark.ml.regression.{GBTRegressionModel, GBTRegressor}
val assembler = new VectorAssembler().
  setInputCols(Array("cycle", "s9", "s11", "s14", "s15")).
  setOutputCol("features")
// configure the model (estimator)
val gbt = new GBTRegressor().
  setLabelCol("RUL").
  setFeaturesCol("features").
  setPredictionCol("RUL_Pred").
  setMinInstancesPerNode(10).
  setMinInfoGain(10).
  setStepSize(0.2).
  setSeed(5).
  setMaxIter(100)
// build the pipeline with the transformers and estimators
val pipeline = new Pipeline().setStages(Array(assembler, gbt))
// train the model
val model = pipeline.fit(train_df)

// import test data, same structure as the training data
val test_raw = spark.read.schema(engineStruct).option("delimiter", " ").csv(datadir + "test_FD001.txt")
val test_maxcycle = test_raw.groupBy("id").max("cycle").select($"id".alias("id2"), $"max(cycle)".alias("maxc"))
// only need to predict the RUL from the latest cycle
val test_maxconly = test_raw.join(test_maxcycle, $"id" === $"id2" && $"cycle" === $"maxc")
// order the data by engine id to be merged with the ground truth for test data in the RUL data file
val test_ordered = test_maxconly.orderBy("id").select("id", "cycle", "s9", "s11", "s14", "s15")

// import ground truth for test data, assuming engine 1 to N, that can be concat with the ordered testing data
val rulStruct = StructType(
  StructField("RUL", IntegerType) :: 
  Nil)
val rul_raw = spark.read.schema(rulStruct).option("delimiter", " ").csv("/user/pliu/CMAPSS/RUL_FD001.txt")
val rul_ordered = rul_raw.withColumn("id3", monotonically_increasing_id()+1)
val test_df = test_ordered.join(rul_ordered, $"id" === $"id3")

// predict on the test data using the model
val predictions = model.transform(test_df)

// evaluate how well the prediction did compared to ground truth using root mean squared error
val evaluator = new RegressionEvaluator().
  setLabelCol("RUL").
  setPredictionCol("RUL_Pred").
  setMetricName("rmse")
val rmse = evaluator.evaluate(predictions)
// rmse should be 28.952102049453856

// print out what the model looks like, it's a decision tree
val gbtModel = model.stages(1).asInstanceOf[GBTRegressionModel]
gbtModel.toDebugString

// save the model
model.write.overwrite().save(datadir + "gbtmodel")

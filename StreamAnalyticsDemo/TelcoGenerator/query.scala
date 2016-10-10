import sqlContext.implicits._
import org.apache.spark.sql.SaveMode

// Create the schema
case class CallRecord(RecordType: String, SystemIdentity: String, SwitchNum: String, CallingNum: String,
                      CallingIMSI: String, CalledNum: String, CalledIMSI: String, TimeType: Int, 
                      CallPeriod: Int, UnitPrice: Double, ServiceType: String, Transfer: Int, 
                      EndType: String, IncomingTrunk: String, OutgoingTrunk: String, MSRN: String,
                      CallTime: Long);

//load the data
val callsRDD = sc.textFile("adl://pliuadls.azuredatalakestore.net/Hackfest/test0.txt");

val isoformat = new java.text.SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss'Z'")
def date2long: (String => Long) = (s: String) => try { isoformat.parse(s).getTime() } catch { case _: Throwable => 0 }

//convert RDD to DataFrame
val callsDF = callsRDD.map(_.split(",")).
    map(c => CallRecord(c(0).trim, c(1).trim, c(2).trim, c(3).trim, 
                        c(4).trim, c(5).trim, c(6).trim, c(7).trim.toInt, 
                        c(8).trim.toInt, c(9).trim.toDouble, c(10).trim, c(11).trim.toInt,
                        c(12).trim, c(13).trim, c(14).trim, c(15).trim,
                        date2long(c(16).trim))).toDF();

//callsDF.count();
//Register the data fram as a table to run queries against
callsDF.write.mode(SaveMode.Overwrite).saveAsTable("telco")

//callsDF.registerTempTable("calls");
//val results = sqlContext.sql("select * from calls");
//results.show()
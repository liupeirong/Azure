import org.apache.spark._
import org.apache.spark.streaming._
import org.apache.spark.streaming.StreamingContext._
import com.granturing.spark.powerbi._
import scala.concurrent.Await
import scala.concurrent.duration._

object SimpleApp {
  def main(args: Array[String]) {
    val conf = new SparkConf().setAppName("Simple Application")
    conf.set("spark.powerbi.username", "powerbi@liupeironggmail.onmicrosoft.com")
    conf.set("spark.powerbi.password", "P0werbi!")
    conf.set("spark.powerbi.clientid", "4d77f082-8bc3-4be2-ad84-df4a8e70c5ad")

    case class ClickThruItem(adid: Int, clicked: Boolean, orgurl: String, userid: String, location: String, happened: String, keyword: String)
    case class LocationMap(location: String, clicks: Int)

    val dataset = "Strata Demo"
    val table = "Locations"
    val tableSchema = Table(
      table, Seq(
        Column("location", "string"),
        Column("clicks", "Int64")
      ))

    val clientConf = ClientConf.fromSparkConf(conf)
    val client = new Client(clientConf)

    val ds = Await.result(client.getDatasets, scala.concurrent.duration.Duration.Inf)
    val datasetId = ds.filter(_.name == dataset).headOption match {
      case Some(d) => {
        Await.result(client.updateTableSchema(d.id, table, tableSchema), scala.concurrent.duration.Duration.Inf)
        d.id
      }
      case None => {
        val result = Await.result(client.createDataset(Schema(dataset, Seq(tableSchema))), scala.concurrent.duration.Duration.Inf)
        result.id
      }
    }

    val sc = new SparkContext(conf)
    val csvstrm = sc.textFile("/demo/input/ct_0000.txt")
    //val ssc = new StreamingContext(conf, Seconds(60))
    //val csvstrm = ssc.socketTextStream("ftd5d10-nn0", 9999)
    //csvstrm.saveAsTextFile("/demo/output/ct")

    val lines = csvstrm.map(line => line.split(",").map(_.trim))
    val rows = lines.map(line => ClickThruItem(line(0).toInt,line(1).toBoolean,line(2),line(3),line(4),line(5), line(6)))
    val impressions = rows.map(row => (row.adid, 1))
    val clicks = rows.filter(row => row.clicked).map(ct => (ct.adid, 1))
    val keywords = rows.map(ct => (ct.keyword, 1))
    val locations = rows.map(ct => (ct.location, 1))

    val ctrCount = clicks.reduceByKey(_ + _)
    val impCount = impressions.reduceByKey(_ + _)
    val kwCount = keywords.reduceByKey(_ + _)
    val locCount = locations.reduceByKey(_ + _)

    locCount.map{case (k, v) => LocationMap(k, v)}.saveToPowerBI(dataset, table)

    sc.stop()

    //ssc.start()
    //ssc.awaitTermination()

}

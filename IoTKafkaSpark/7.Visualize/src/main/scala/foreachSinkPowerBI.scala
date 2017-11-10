package org.pliu.iot.bi

import org.apache.spark.sql.ForeachWriter
import org.apache.http._
import org.apache.http.client._
import org.apache.http.client.methods.HttpPost
import org.apache.http.impl.client.DefaultHttpClient
import org.apache.http.entity.StringEntity

class PowerBISinkForeach(pbiUrl: String) extends ForeachWriter[String] {
  def open(partitionId: Long, version: Long): Boolean = {
    // open connection
    true
  }
  
  def process(record: String) = {
      val body = new StringEntity(record)
      val post = new HttpPost(pbiUrl)
      post.addHeader("Content-Type", "application/json")
      post.setEntity(body)
      val httpClient = new DefaultHttpClient
      val resp = httpClient.execute(post)
      if (resp.getStatusLine.getStatusCode != 200) 
      {
        println("Failed to send data to PowerBI: " + resp.getStatusLine.getReasonPhrase);
      }
  }
  
  def close(errorOrNull: Throwable): Unit = {
    // close the connection
  }
}
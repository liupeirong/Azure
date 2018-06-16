package com.pliu.powerbiembed;

/**
 * Created by pliu on 11/14/2016.
 */
public class Report {
    public String datasetId;
    public String id;
    public String name;
    public String webUrl;
    public String embedUrl;

    public void setDatasetId (String datasetId) {this.datasetId = datasetId;}
    public void setId (String id) {this.id = id;}
    public void setName (String name) {this.name = name;}
    public void setWebUrl (String webUrl) {this.webUrl = webUrl;}
    public void setEmbedUrl (String embedUrl) {this.embedUrl = embedUrl;}

    public String getDatasetId () {return this.datasetId;}
    public String getId() {return this.id;}
    public String getName () {return this.name;}
    public String getWebUrl() {return this.webUrl;}
    public String getEmbedUrl() {return this.embedUrl;}
}

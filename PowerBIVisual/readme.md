# Power BI Custom Visual for a D3 Force-Directed Graph

This custom visual implements a [D3 force-directed graph](https://github.com/mbostock/d3/wiki/Force-Layout) with curved path.  You can optionally add arrows to represent direction of the relationship and use the thickness or the color of the path to represent the weight of the relationship between the nodes. The nodes can be circles or images. 

![Alt text](/PowerBIVisual/screenshots/powerbiForce.PNG?raw=true "Force diagram visual in Power BI") 

### Try it in Power BI dev tool

* Go to powerbi.com, click on "Settings" and then "Dev Tools"
* Copy and paste forceGraph/forceGraph.ts to the upper left pane
* Copy and paste forceGraph/forceGraph.css to the lower left pane
* Chose a dataset that has 2 string columns and a numeric column, or, comment out the following line in forceGraph.ts and uncomment the lines above it where we assign sample row data
```javascript
var rows = dataView.table.rows;
```
* Click "Compile+Run"
* You can debug by starting the browser dev tool, and find the source code under "(no domain)" ForceGraphnn...nn.js

![Alt text](/PowerBIVisual/screenshots/devtoolDebug.PNG?raw=true "Debug the visual in Dev Tool") 

### Import to Power BI
* Go to powerbi.com, "Get Data" and import a csv in the sampleData folder
* Import forceGraph/forceGraph.pbiviz
* Click on the dataset you imported and choose 2 string columns and a numeric column to see it in action

![Alt text](/PowerBIVisual/screenshots/import2Powerbi.PNG?raw=true "Import the visual to Power BI") 

### Dataset 
Map data to the following fields:

![Alt text](/PowerBIVisual/screenshots/mapData.PNG?raw=true "Map data to fields") 

* The minimum requirements for this visual include:
  * a column that represents the source entity
  * a column that represents the target entity
* Optionally, add the following columns:
  * a numeric column for the weight of the relationship
  * a column that represents the type of relationship, the links can be colored by this type
  * a column that represents the type of the source entity, images can be displayed for this value, the full image url is "base url + source type + image extension"
  * a column that represents the type of the target entity, images can be displayed for this value, the full image url is "base url + target type + image extension"

For examples, see [sample data](/PowerBIVisual/sampleData). 

### Formatting options
The following options can be customized for this visual:

![Alt text](/PowerBIVisual/screenshots/formatOptions.PNG?raw=true "Formatting options") 

* links
  * Arrow: whether or not to display arrows on the link to represent directional relationship
  * Label: whether or not to display the weight of the relationship in the middle of the curved link
  * Color: highlight the links upon mouse hover, or color the links based on weight, or color the links based on link type
  * Thickness: whether or not to vary the thickness of the link based on the weight of the relationship
* nodes
  * Image: whether or not to display image instead of circles for the nodes, image is specified in the SourceType or TargetType columns, the first time a node shows up in the dataset, its image is set, it's not changed if it's set later in the dataset
  * Default image: for the nodes that didn't specify image, this default image will be displayed, if this is not specified, a circle is displayed
  * Image url: the base url for images. Full url is composed by "base url + sourceType or targetType + image extension"
  * Image extension: the file extension of the images
  * Max name length: the maximum length of node names to display, if names are too long, the visual could be hard to read
  * Highlight all reachable links: whether or not to highlight the immediate neighboring links of a node or all links that a node can reach to 
* size
  * Charge: the charge of the force directed graph. The larger the negative large, the more spread the visual. This must be a negative value


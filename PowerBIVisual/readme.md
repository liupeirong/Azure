# Power BI Custom Visual for D3 Force Layout Diagram

This custom visual implements a D3 force layout diagram with curved path.  The thickness of the path represents the weight of the relationship between the nodes.

![Alt text](/screenshots/powerbiForce.png?raw=true "Force diagram visual in Power BI") 

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

![Alt text](/screenshots/devtoolDebug.png?raw=true "Debug the visual in Dev Tool") 

### Import to Power BI
* Go to powerbi.com, "Get Data" and import a csv in the sampleData folder
* Click on the dataset you imported and check 2 string columns and a numeric column, Power BI will place the data in a table control
* Import forceGraph/forceGraph.pbiviz

![Alt text](/screenshots/import2PowerBI.png?raw=true "Import the visual to Power BI") 

* Click on the imported visual to see it in action

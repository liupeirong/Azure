// Escape key closes popup
document.onkeydown = function (evt) {
    evt = evt || window.event;
    if (evt.keyCode === 27) {
        d3.select("#popup").classed("hidden", true);
    }
};


// get the data
d3.json("links1.json", function (error, links) {
    var nodes = {};
    var minFiles = d3.min(links.map(function (d) {
        return Number(d.filecount);
    }));
    var maxFiles = d3.max(links.map(function (d) {
        return Number(d.filecount);
    }));

    // Compute the distinct nodes from the links.
    var linkedByName = {};

    links.forEach(function (link) {
        link.source = nodes[link.source] ||
            (nodes[link.source] = { name: link.source });
        link.target = nodes[link.target] ||
            (nodes[link.target] = { name: link.target });
        link.value = +link.filecount;
        linkedByName[link.source.name + "," + link.target.name] = 1;
    });
    function isConnected(a, b) {
        return linkedByName[a.name + "," + b.name] || linkedByName[b.name + "," + a.name] || a.name == b.name;
    }


    var margin = {
        top: 50, right: 0, bottom: 60, left: 0
    };
    var width = window.innerWidth * 0.9 - margin.left - margin.right;
    var height = window.innerHeight - margin.top - margin.bottom;
    var circleRadius = 15;
    var circleRadiusBig = 25;
    var normalPathColor = "#999";
    var fadedPathColor = "#eee";


    var force = d3.layout.force()
        .nodes(d3.values(nodes))
        .links(links)
        .size([width, height])
        .linkDistance(200)
        .charge(-300)
        .on("tick", tick)
        .start();

    var svg = d3.select("body").append("svg")
        .attr("width", width)
        .attr("height", height);

    // add the links and the arrows

    var scale0to100 = d3.scale.linear().domain([minFiles, maxFiles]).range([20, 100]).clamp(true);

    var path = svg.append("svg:g").selectAll("path")
        .data(force.links())
      .enter().append("svg:path")
    //    .attr("class", function(d) { return "link " + d.type; })
        .attr("class", "link")
        .attr("marker-end", "url(#end)")
        .attr("stroke-width", function (d) {
            return scale0to100(d.filecount) / 10;
        })
        .on("click", function (d) {
            //tabulate(d.files, ["files"]);
            d3.select("#fileCount").text(d.filecount);
            d3.select("#popupLinks").text("Files between " + d.source.name + " and " + d.target.name);
            d3.select("#popup").classed("hidden", false);
        }
        )
        .on("mouseover", function (d) {
            console.log(d);
            fadePath(.3);
        })
        .on("mouseout", fadePath(1))
    ;

    // define the nodes
    var node = svg.selectAll(".node")
    .data(force.nodes())
    .enter().append("g")
    .attr("class", "node")
    .on("click", click)
    .on("dblclick", dblclick)
        .call(force.drag)
        .on("mouseover", fadeNode(.3))
        .on("mouseout", fadeNode(1));

    // add the nodes
    node.append("circle")
        .attr("r", function (d) {
            return d.weight < 10 ? 10 : d.weight;
        });

    // add the text 
    node.append("text")
        .attr("x", 12)
        .attr("dy", ".35em")
        .text(function (d) {
            return d.name;
        });

    // add the curvy lines
    function tick() {
        path.attr("d", function (d) {
            var dx = d.target.x - d.source.x,
                dy = d.target.y - d.source.y,
                dr = Math.sqrt(dx * dx + dy * dy);
            return "M" +
                d.source.x + "," +
                d.source.y + "A" +
                dr + "," + dr + " 0 0,1 " +
                d.target.x + "," +
                d.target.y;
        });

        node
            .attr("transform", function (d) {
                return "translate(" + d.x + "," + d.y + ")";
            });

        //force.stop();
    }

    function fadeNode(opacity) {
        return function (d) {
            node.style("stroke-opacity", function (o) {
                thisOpacity = isConnected(d, o) ? 1 : opacity;
                this.setAttribute('fill-opacity', thisOpacity);
                return thisOpacity;
            });

            path.style("stroke-opacity", function (o) {
                return o.source === d || o.target === d ? 1 : opacity;
            });
            path.style("stroke", function (o) {
                return o.source === d || o.target === d ? "#f00" : "#bbb";
            });
        };
    }

    function fadePath(opacity) {
        return function (d) {
            path.style("stroke-opacity", function (o) {
                return o.source === d || o.target === d ? 1 : opacity;
            });
            path.style("stroke", function (o) {
                return o.source === d || o.target === d ? "#f00" : "#bbb";
            });
        };
    }
    // action to take on mouse click of node
    function click() {
        d3.select(this);
        //d3.select(this).select("text").transition()
        //    .duration(750)
        //    .attr("x", 22)
        //    .style("fill", "steelblue")
        //    .style("stroke", "lightsteelblue")
        //    .style("stroke-width", ".5px")
        //    .style("font", "20px sans-serif");
        //d3.select(this).select("circle").transition()
        //    .duration(750)
        //    .attr("r", circleRadiusBig)
        //    .style("fill", "lightsteelblue");
    }

    // action to take on mouse double click
    function dblclick() {
        //    d3.select(this).select("circle").transition()
        //        .duration(750)
        //        .attr("r", circleRadius)
        //    .style("fill", "#ccc");
        //d3.select(this).select("text").transition()
        //    .duration(750)
        //    .attr("x", 12)
        //    .style("stroke", "none")
        //    .style("fill", "#00a")
        //    .style("stroke", "none")
        //        .style("font", "12px sans-serif");
    }

    var k = 0;
    while ((force.alpha() > 1e-2) && (k < 500)) {
        force.tick(),
        k = k + 1;
    }

    function tabulate(data, columns) {
        d3.select("#popupTable").html("");
        var table = d3.select("#popupTable").append("table"),
            thead = table.append("thead"),
            tbody = table.append("tbody");

        // append the header row
        thead.append("tr")
            .selectAll("th")
            .data(columns)
            .enter()
            .append("th")
                .text(function (column) {
                    return column;
                });

        // create a row for each object in the data
        var rows = tbody.selectAll("tr")
            .data(data)
            .enter()
            .append("tr");

        // create a cell in each row for each column
        var cells = rows.selectAll("td")
            .data(function (row) {
                return columns.map(function (column) {
                    return {
                        column: column, value: row
                    };
                });
            })
            .enter()
            .append("td")
            .attr("style", "font-family: Courier") // sets the font style
                .html(function (d) {
                    return d.value;
                });

        return table;
    }


});
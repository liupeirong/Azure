module powerbi.visuals {
    export class ForceGraph implements IVisual {
        public static capabilities: VisualCapabilities = {
            dataRoles: [
                {
                    name: 'Values',
                    kind: VisualDataRoleKind.GroupingOrMeasure,
                },
            ],
            objects: {
                general: {
                    properties: {
                        formatString: {
                            type: { formatting: { formatString: true } },
                        },
                    },
                }
            },
            dataViewMappings: [{
                table: {
                    rows: {
                        for: { in: 'Values' },
                        dataReductionAlgorithm: { window: {} }
                    },
                    rowCount: { preferred: { min: 1 } }
                },
            }],
            suppressDefaultTitle: true,
        };

        private root: D3.Selection;
        private dataView: DataView;

        // converts data from Values to two dimensional array
        // expected order: MemberFrom MemberTo Value Valu2 (optional - for coloring)
        public static converter(dataView: DataView): any {
            var nodes = {};
            var minFiles = Number.MAX_VALUE;
            var maxFiles = 0;
            var linkedByName = {};

            //var links = [
            //    { "source": "john", "target": "joe", "filecount": 50 },
            //    { "source": "john", "target": "bob", "filecount": 150 },
            //    { "source": "mary", "target": "joe", "filecount": 80 },
            //    { "source": "bob", "target": "mary", "filecount": 70 },
            //    { "source": "joe", "target": "bob", "filecount": 20 },
            //];

            //links.forEach(function (link) {
            //    link.source = nodes[link.source] ||
            //    (nodes[link.source] = { name: link.source });
            //    link.target = nodes[link.target] ||
            //    (nodes[link.target] = { name: link.target });
            //    //link.value = +link.filecount;
            //    if (link.filecount < minFiles) { minFiles = link.filecount };
            //    if (link.filecount > maxFiles) { maxFiles = link.filecount };
            //    linkedByName[link.source.name + "," + link.target.name] = 1;
            //});

            var links = [];
            var rows2 = [
                ["Harry", "Sally", 4631],
                ["Harry", "Mario", 4018]
            ];
            var rows3 = [
                ["Hutt Valley", "Whanganul", 359],
                ["Hutt Valley", "Wairarapa", 483],
                ["Hutt Valley", "Capital & Coast", 857],
                ["Hutt Valley", "Hawkes Bay", 1304],
                ["Hutt Valley", "MidCentral", 1526],
                ["Capital & Coast", "Whanganul", 1183],
                ["Capital & Coast", "Hutt Valley", 735],
                ["Capital & Coast", "Wairarapa", 1172],
                ["Capital & Coast", "MidCentral", 1390],
                ["Capital & Coast", "Hawkes Bay", 955],
                ["Hawkes Bay", "Whanganul", 465],
                ["Hawkes Bay", "Wairarapa", 1057],
                ["Hawkes Bay", "MidCentral", 1401],
                ["Hawkes Bay", "Capital & Coast", 1052],
                ["Hawkes Bay", "Hutt Valley", 213]
            ];
            if (dataView && dataView.table) {
                var rows = dataView.table.rows;
                rows.forEach(function (item) {
                    linkedByName[item[0] + "," + item[1]] = 1;
                    var link = {
                        "source": nodes[item[0]] || (nodes[item[0]] = { name: item[0] }),
                        "target": nodes[item[1]] || (nodes[item[1]] = { name: item[1] }),
                        "filecount": item[2]
                    };
                    if (link.filecount < minFiles) { minFiles = link.filecount };
                    if (link.filecount > maxFiles) { maxFiles = link.filecount };
                    links.push(link);
                });
            };
            var data = {
                "nodes": nodes, "links": links, "minFiles": minFiles, "maxFiles": maxFiles, "linkedByName": linkedByName
            };

            return data;
        }

        public init(options: VisualInitOptions): void {
            this.root = d3.select(options.element.get(0));
        }

        public update(options: VisualUpdateOptions) {
            if (!options.dataViews || (options.dataViews.length < 1)) return;
            var data = ForceGraph.converter(this.dataView = options.dataViews[0]);

            var viewport = options.viewport;
            var w = viewport.width,
                h = viewport.height;
            var k = Math.sqrt(Object.keys(data.nodes).length / (w * h));
            var color = d3.scale.category10();

            this.root.selectAll("svg").remove();

            var svg = this.root
                .append("svg")
                .attr("width", w)
                .attr("height", h);

            var force = d3.layout.force()
                .gravity(100 * k)
                .nodes(d3.values(data.nodes))
                .links(data.links)
                .size([w, h])
                .linkDistance(100)
                .charge(-100 / k)
                .on("tick", tick)
                .start();

            var scale0to100 = d3.scale.linear().domain([data.minFiles, data.maxFiles]).rangeRound([1, 10]).clamp(true);
            
            // build the arrow.
            function marker(d, i) {
                var val = "mid_" + i;
                svg.append("defs").selectAll("marker")
                    .data([val])      // Different link/path types can be defined here
                    .enter().append("marker")    // This section adds in the arrows
                    .attr("id", String)
                    .attr("viewBox", "0 -5 10 10")
                    .attr("refX", 10)
                    .attr("refY", 0)
                    .attr("markerWidth", 6)
                    .attr("markerHeight", 6)
                    .attr("orient", "auto")
                    .attr("markerUnits", "userSpaceOnUse")
                    .append("path")
                    .attr("d", "M0,-5L10,0L0,5")
                    .style("fill", color(scale0to100(d.filecount)))
                ;
                return "url(#" + val + ")";
            }

            var path = svg.selectAll(".link")
                .data(force.links())
                .enter().append("path")
                .attr("class", "link")
                .attr("id", function (d, i) { return "linkid_" + i; })
                // uncomment if we don't need the marker-end workaround
                //.attr("marker-end", function (d, i) { return marker(d, i); })
                .attr("stroke-width", function (d) {
                    return scale0to100(d.filecount);
                })
                .style("stroke", function (d) {
                    return color(scale0to100(d.filecount));
                })
                // no need for "fill" if we don't need the marker-end workaround
                .style("fill", function (d) {
                    return color(scale0to100(d.filecount));
                })
             //   .on("mouseover", fadePath(.3))
             //   .on("mouseout", fadePath(1))
                ;

            //path.append("title")
            //    .text(function (d) { return d.source.name + "-" + d.target.name + ":" + d.filecount });

            var linktext = svg.selectAll(".linklabelholder")
                .data(force.links())
                .enter().append("g")
                .attr("class", "linklabelholder")
                .append("text")
                .attr("class", "linklabel")
                .attr("y", "-12")
                .attr("text-anchor", "middle")
                .style("fill", "#000")
                .append("textPath")
                .attr("xlink:href", function (d, i) { return "#linkid_" + i; })
                .attr("startOffset", "25%") //use "50%" if we don't need the marker-end workaround
                .text(function (d) { return d.filecount; });
            
            // define the nodes
            var node = svg.selectAll(".node")
                .data(force.nodes())
                .enter().append("g")
                .attr("class", "node")
                .call(force.drag)
            //    .on("mouseover", fadeNode(.3))
            //    .on("mouseout", fadeNode(1))
                .on("mousedown", function () { d3.event.stopPropagation(); })
                ;

            // add the nodes
            node.append("circle")
                .attr("r", function (d) {
                    if (d.weight < 5) {
                        d.radius = 5;
                    } else {
                        d.radius = d.weight;
                    }
                    return d.radius;
                });

            // add the text 
            node.append("text")
                .attr("x", 12)
                .attr("dy", ".35em")
                .text(function (d) {
                    return d.name;
                });

            function isConnected(a, b) {
                return data.linkedByName[a.name + "," + b.name] || data.linkedByName[b.name + "," + a.name] || a.name == b.name;
            }

            // add the curvy lines
            function tick() {
                path.each(function () { this.parentNode.insertBefore(this, this); });
                //use this if we don't need the marker-end workaround
                //path.attr("d", function (d) {
                //    var dx = d.target.x - d.source.x,
                //        dy = d.target.y - d.source.y,
                //        dr = Math.sqrt(dx * dx + dy * dy);
                //    // x and y distances from center to outside edge of target node
                //    var offsetX = (dx * d.target.radius) / dr;
                //    var offsetY = (dy * d.target.radius) / dr;
                //    return "M" +
                //        d.source.x + "," +
                //        d.source.y + "A" +
                //        dr + "," + dr + " 0 0,1 " +
                //        (d.target.x - offsetX) + "," +
                //        (d.target.y - offsetY);
                //});

                //this is for marker-end workaround, build the marker with the path
                path.attr("d", function (d) {
                    var dx = d.target.x - d.source.x,
                        dy = d.target.y - d.source.y,
                        dr = Math.sqrt(dx * dx + dy * dy),
                        theta = Math.atan2(dy, dx) + Math.PI / 7.85,
                        d90 = Math.PI / 2,
                        dtxs = d.target.x - 6 * Math.cos(theta),
                        dtys = d.target.y - 6 * Math.sin(theta);
                    return "M" +
                        d.source.x + "," +
                        d.source.y + "A" +
                        dr + "," + dr + " 0 0 1," +
                        d.target.x + "," +
                        d.target.y +
                        "A" + dr + "," + dr + " 0 0 0," + d.source.x + "," + d.source.y + "M" + dtxs + "," + dtys + "l" + (3.5 * Math.cos(d90 - theta) - 10 * Math.cos(theta)) + "," + (-3.5 * Math.sin(d90 - theta) - 10 * Math.sin(theta)) + "L" + (dtxs - 3.5 * Math.cos(d90 - theta) - 10 * Math.cos(theta)) + "," + (dtys + 3.5 * Math.sin(d90 - theta) - 10 * Math.sin(theta)) + "z";
                });

                node
                    .attr("transform", function (d) {
                        return "translate(" + d.x + "," + d.y + ")";
                    });
            };

            function fadeNode(opacity) {
                return function (d) {
                    node.style("stroke-opacity", function (o) {
                        var thisOpacity = isConnected(d, o) ? 1 : opacity;
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
                        return o.source === d.source && o.target === d.target ? 1 : opacity;
                    });
                    path.style("stroke", function (o) {
                        return o.source === d.source && o.target === d.target ? "#f00" : "#bbb";
                    });
                };
            }
        }

        public destroy(): void {
            this.root = null;
        }
    }

}
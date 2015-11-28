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
            //var rows = [
            //    ["Harry", "Sally", 4631],
            //    ["Harry", "Mario", 4018]
            //];
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
                .gravity(80 * k)
                .nodes(d3.values(data.nodes))
                .links(data.links)
                .size([w, h])
                .linkDistance(100)
                .charge(-15 / k)
                .on("tick", tick)
                .start();

            var scale0to100 = d3.scale.linear().domain([data.minFiles, data.maxFiles]).rangeRound([1, 10]).clamp(true);

            var path = svg.selectAll(".link")
                .data(force.links())
                .enter().append("path")
                .attr("class", "link")
                .attr("id", function (d, i) { return "linkid_" + i; })
                .attr("stroke-width", function (d) {
                    return scale0to100(d.filecount);
                })
                .style("stroke", function (d) {
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
                .attr("startOffset", "50%")
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
                    return d.weight < 5 ? 5 : d.weight;
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
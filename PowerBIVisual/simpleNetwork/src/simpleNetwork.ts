module powerbi.visuals.samples {

    export class SimpleNetwork implements IVisual {
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
            var nodes = [];
            var links = [];
            var dict = {};
            var n = 0;

            //var raw = [
            //    { "device1": "john", "device2": "joe", "weight": 50 },
            //    { "device1": "john", "device2": "bob", "weight": 150 },
            //    { "device1": "mary", "device2": "joe", "weight": 80 },
            //    { "device1": "bob", "device2": "mary", "weight": 70 },
            //    { "device1": "joe", "device2": "bob", "weight": 20 },
            //];

            //raw.forEach(function (item) {
            //    if (!(item.device1 in dict)) {
            //        dict[item.device1] = n++;
            //        var node = { "name": item.device1, "group": 1 }
            //        nodes.push(node);
            //    }
            //    if (!(item.device2 in dict)) {
            //        dict[item.device2] = n++;
            //        var node = { "name": item.device2, "group": 1 }
            //        nodes.push(node);
            //    }
            //    var link = {
            //        "source": dict[item.device1],
            //        "target": dict[item.device2],
            //        "weight": item.weight
            //    };
            //    links.push(link);
            //});

            var rows = dataView.table.rows;
            rows.forEach(function (item) {
                if (!(item[0] in dict)) {
                    dict[item[0]] = n++;
                    var node = { "name": item[0], "group": 1 }
                    nodes.push(node);
                }
                if (!(item[1] in dict)) {
                    dict[item[1]] = n++;
                    var node = { "name": item[1], "group": 1 }
                    nodes.push(node);
                }
                var link = {
                    "source": dict[item[0]],
                    "target": dict[item[1]],
                    "weight": item[2]
                };
                links.push(link);
            })
            var data = { "nodes": nodes, "links": links };
            //var data = {
            //    "nodes":
            //    [{ "name": "node1", "group": 1 },
            //        { "name": "node2", "group": 2 },
            //        { "name": "node3", "group": 2 },
            //        { "name": "node4", "group": 3 }],
            //    "links":
            //    [{ "source": 2, "target": 1, "weight": 1 },
            //        { "source": 0, "target": 2, "weight": 3 }]
            //};

            return data;
        }

        public init(options: VisualInitOptions): void {
            this.root = d3.select(options.element.get(0))
                .append('svg');
        }

        public update(options: VisualUpdateOptions) {
            // convert dataview into the format for drawing   
            var data = SimpleNetwork.converter(this.dataView = options.dataViews[0]);
            
            // visual initialization
            var viewport = options.viewport;
            var w = viewport.width,
                h = viewport.height;

            var svg = this.root
                .attr("width", w)
                .attr("height", h);

            var force = d3.layout.force()
                .gravity(.05)
                .linkDistance(100)
                .charge(-100)
                .size([w, h]);

            var nodes = data.nodes;
            var links = data.links;

            // draw network
            force
                .nodes(data.nodes)
                .links(data.links)
                .start();

            var link = svg.selectAll(".link")
                .data(data.links)
                .enter().append("line")
                .attr("class", "link")
                .style("stroke-width", function (d) { return Math.sqrt(d.weight); });

            var node = svg.selectAll(".node")
                .data(data.nodes)
                .enter().append("g")
                .attr("class", "node")
                .call(force.drag);

            node.append("circle")
                .attr("r", "5");

            node.append("text")
                .attr("dx", 12)
                .attr("dy", ".35em")
                .text(function (d) { return d.name; });

            force.on("tick", function () {
                link.attr("x1", function (d) { return d.source.x; })
                    .attr("y1", function (d) { return d.source.y; })
                    .attr("x2", function (d) { return d.target.x; })
                    .attr("y2", function (d) { return d.target.y; });

                node.attr("transform", function (d) { return "translate(" + d.x + "," + d.y + ")"; });
            });
        }

        public destroy(): void {
            this.root = null;
        }
    }

}
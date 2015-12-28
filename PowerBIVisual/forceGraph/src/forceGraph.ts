module powerbi.visuals {
    export var test = false;
    export module linkColorType {
        export var byWeight: string = 'ByWeight';
        export var byLinkType: string = 'ByLinkType';
        export var interactive: string = 'Interactive';

        export var type: IEnumType = createEnumType([
            { value: byWeight, displayName: 'ByWeight' },
            { value: byLinkType, displayName: 'ByLinkType' },
            { value: interactive, displayName: 'Interactive' },
        ]);
    }

    export interface ForceGraphOptions {
        showArrow: boolean;
        showLabel: boolean;
        colorLink: linkColorType;
        thickenLink: boolean;
        displayImage: boolean;
        defaultImage: string;
        imageUrl: string;
        imageExt: string;
        nameMaxLength: number;
        highlightReachableLinks: boolean;
        charge: number;
        defaultLinkColor: string;
        defaultLinkHighlightColor: string;
        defaultLinkThickness: string;
    }

    export var forceProps = {
        links: {
            showArrow: <DataViewObjectPropertyIdentifier>{ objectName: 'links', propertyName: 'showArrow' },
            showLabel: <DataViewObjectPropertyIdentifier>{ objectName: 'links', propertyName: 'showLabel' },
            colorLink: <DataViewObjectPropertyIdentifier>{ objectName: 'links', propertyName: 'colorLink' },
            thickenLink: <DataViewObjectPropertyIdentifier>{ objectName: 'links', propertyName: 'thickenLink' },
        },
        nodes: {
            displayImage: <DataViewObjectPropertyIdentifier>{ objectName: 'nodes', propertyName: 'displayImage' },
            defaultImage: <DataViewObjectPropertyIdentifier>{ objectName: 'nodes', propertyName: 'defaultImage' },
            imageUrl: <DataViewObjectPropertyIdentifier>{ objectName: 'nodes', propertyName: 'imageUrl' },
            imageExt: <DataViewObjectPropertyIdentifier>{ objectName: 'nodes', propertyName: 'imageExt' },
            nameMaxLength: <DataViewObjectPropertyIdentifier>{ objectName: 'nodes', propertyName: 'nameMaxLength' },
            highlightReachableLinks: <DataViewObjectPropertyIdentifier>{ objectName: 'nodes', propertyName: 'highlightReachableLinks' },
        },
        size: {
            charge: <DataViewObjectPropertyIdentifier>{ objectName: 'size', propertyName: 'charge' },
        },
    }

    export interface ForceGraphData {
        nodes: {};
        links: any[];
        minFiles: number;
        maxFiles: number;
        linkedByName: {};
        dataPointsToEnumerate: {};
    }

    export class ForceGraph implements IVisual {
        private root: D3.Selection;
        private dataView: DataView;
        private colors: IDataColorPalette;
        private options: ForceGraphOptions;
        private data: ForceGraphData;

        private getDefaultOptions(): ForceGraphOptions {
            return {
                showArrow: false,
                showLabel: false,
                colorLink: linkColorType.interactive,
                thickenLink: true,
                displayImage: false,
                defaultImage: "Home",
                imageUrl: "http://pliupublic.blob.core.windows.net/files/",
                imageExt: ".png",
                nameMaxLength: 10,
                highlightReachableLinks: false,
                charge: -15,
                defaultLinkColor: "#bbb",
                defaultLinkHighlightColor: "#f00",
                defaultLinkThickness: "1.5px",
            };
        }

        private updateOptions(objects: DataViewObjects) {
            this.options.showArrow = DataViewObjects.getValue(objects, forceProps.links.showArrow, this.options.showArrow);
            this.options.showLabel = DataViewObjects.getValue(objects, forceProps.links.showLabel, this.options.showLabel);
            this.options.colorLink = DataViewObjects.getValue(objects, forceProps.links.colorLink, this.options.colorLink);
            this.options.thickenLink = DataViewObjects.getValue(objects, forceProps.links.thickenLink, this.options.thickenLink);
            this.options.displayImage = DataViewObjects.getValue(objects, forceProps.nodes.displayImage, this.options.displayImage);
            this.options.defaultImage = DataViewObjects.getValue(objects, forceProps.nodes.defaultImage, this.options.defaultImage);
            this.options.imageUrl = DataViewObjects.getValue(objects, forceProps.nodes.imageUrl, this.options.imageUrl);
            this.options.imageExt = DataViewObjects.getValue(objects, forceProps.nodes.imageExt, this.options.imageExt);
            this.options.nameMaxLength = DataViewObjects.getValue(objects, forceProps.nodes.nameMaxLength, this.options.nameMaxLength);
            this.options.highlightReachableLinks = DataViewObjects.getValue(objects, forceProps.nodes.highlightReachableLinks, this.options.highlightReachableLinks);
            this.options.charge = DataViewObjects.getValue(objects, forceProps.size.charge, this.options.charge);
            if (this.options.charge >= 0) this.options.charge = this.getDefaultOptions().charge;
        }

        public static capabilities: VisualCapabilities = {
            dataRoles: [
                {
                    name: 'Source',
                    kind: VisualDataRoleKind.Grouping,
                    displayName: 'Source',
                },
                {
                    name: 'Target',
                    kind: VisualDataRoleKind.Grouping,
                    displayName: 'Target',
                },
                {
                    name: 'Weight',
                    kind: VisualDataRoleKind.Measure,
                    displayName: 'Weight',
                },
                {
                    name: 'LinkType',
                    kind: VisualDataRoleKind.Grouping,
                    displayName: 'LinkType',
                    description: 'Links can be colored by link types',
                },
                {
                    name: 'SourceType',
                    kind: VisualDataRoleKind.Grouping,
                    displayName: 'SourceType',
                    description: 'Source type represents the image name for source entities',
                },
                {
                    name: 'TargetType',
                    kind: VisualDataRoleKind.Grouping,
                    displayName: 'TargetType',
                    description: 'Target type represents the image name for target entities',
                },
            ],
            objects: {
                general: {
                    properties: {
                        formatString: {
                            type: { formatting: { formatString: true } },
                        }
                    }
                },
                links: {
                    displayName: data.createDisplayNameGetter('Visual_Links'),
                    properties: {
                        showArrow: {
                            type: { bool: true },
                            displayName: 'Arrow'
                        },
                        showLabel: {
                            type: { bool: true },
                            displayName: 'Label',
                            description: 'Displays weight on links',
                        },
                        colorLink: {
                            type: { enumeration: linkColorType.type },
                            displayName: 'Color',
                        },
                        thickenLink: {
                            type: { bool: true },
                            displayName: 'Thickness',
                            description: 'Thickenss of links represents weight',
                        },
                    }
                },
                nodes: {
                    displayName: data.createDisplayNameGetter('Visual_Nodes'),
                    properties: {
                        displayImage: {
                            type: { bool: true },
                            displayName: 'Image',
                            description: 'Images are loaded from image url + source or target type + image extension',
                        },
                        defaultImage: {
                            type: { text: true },
                            displayName: 'Default image'
                        },
                        imageUrl: {
                            type: { text: true },
                            displayName: 'Image url'
                        },
                        imageExt: {
                            type: { text: true },
                            displayName: 'Image extension'
                        },
                        nameMaxLength: {
                            type: { numeric: true },
                            displayName: 'Max name length',
                            description: 'Max length of the name of entities displayed',
                        },
                        highlightReachableLinks: {
                            type: { bool: true },
                            displayName: 'Highlight all reachable links',
                            description: "In interactive mode, whether a node's all reachable links will be highlighted",
                        },
                    }
                },
                size: {
                    displayName: data.createDisplayNameGetter('Visual_Size'),
                    properties: {
                        charge: {
                            type: { numeric: true },
                            displayName: 'Charge',
                            description: 'The larger the negative charge the more apart the entities, must be negative',
                        },
                    }
                },
            },
            dataViewMappings: [{
                conditions: [
                    { 'Source': { max: 1 }, 'Target': { max: 1 }, 'Weight': { max: 1 }, 'LinkType': { max: 1 }, 'SourceType': { max: 1 }, 'TargetType': { max: 1 } },
                ],
                categorical: {
                    categories: {
                        for: { in: 'Source' },
                        dataReductionAlgorithm: { top: {} }
                    },
                    values: {
                        select: [
                            { bind: { to: 'Target' } },
                            { bind: { to: 'Weight' } },
                            { bind: { to: 'LinkType' } },
                            { bind: { to: 'SourceType' } },
                            { bind: { to: 'TargetType' } },
                        ],
                    },
                    rowCount: { preferred: { min: 1 } }
                },
            }],
            suppressDefaultTitle: true,
        };

        public enumerateObjectInstances(options: EnumerateVisualObjectInstancesOptions): VisualObjectInstance[] {
            var instances: VisualObjectInstance[] = [];
            var dataView = this.dataView;
            switch (options.objectName) {
                case 'links':
                    var links: VisualObjectInstance = {
                        objectName: 'links',
                        displayName: 'Links',
                        selector: null,
                        properties: {
                            showArrow: this.options.showArrow,
                            colorLink: this.options.colorLink,
                            showLabel: this.options.showLabel,
                            thickenLink: this.options.thickenLink,
                        }
                    };
                    instances.push(links);
                    break;
                case 'nodes':
                    var nodes: VisualObjectInstance = {
                        objectName: 'nodes',
                        displayName: 'Nodes',
                        selector: null,
                        properties: {
                            displayImage: this.options.displayImage,
                            defaultImage: this.options.defaultImage,
                            imageUrl: this.options.imageUrl,
                            imageExt: this.options.imageExt,
                            nameMaxLength: this.options.nameMaxLength,
                            highlightReachableLinks: this.options.highlightReachableLinks,
                        }
                    };
                    instances.push(nodes);
                    break;
                case 'size':
                    var size: VisualObjectInstance = {
                        objectName: 'size',
                        displayName: 'Size',
                        selector: null,
                        properties: {
                            charge: this.options.charge,
                        }
                    };
                    instances.push(size);
                    break;
            }

            return instances;
        }

        public static converter(dataView: DataView, colors: IDataColorPalette): ForceGraphData {
            var nodes = {};
            var minFiles = Number.MAX_VALUE;
            var maxFiles = 0;
            var linkedByName = {};
            var links = [];
            var linkDataPoints = {};
            var linkTypeCount = 0;
            var sourceCol = -1, targetCol = -1, weightCol = -1, linkTypeCol = -1, sourceTypeCol = -1, targetTypeCol = -1;
            var rows;
            if (dataView && dataView.categorical && dataView.categorical.categories && dataView.metadata && dataView.metadata.columns) {
                var metadataColumns = dataView.metadata.columns;
                for (var i = 0; i < metadataColumns.length; i++) {
                    var col = metadataColumns[i];
                    if (col.roles) {
                        if (col.roles['Source'])
                            sourceCol = i;
                        else if (col.roles['Target'])
                            targetCol = i;
                        else if (col.roles['Weight'])
                            weightCol = i;
                        else if (col.roles['LinkType'])
                            linkTypeCol = i;
                        else if (col.roles['SourceType'])
                            sourceTypeCol = i;
                        else if (col.roles['TargetType'])
                            targetTypeCol = i;
                    }
                }
            }
            if (test) {
                sourceCol = 0;
                targetCol = 1;
                weightCol = -1;//2;
                linkTypeCol = -1;//3;
                sourceTypeCol = -1;//5;
                targetTypeCol = -1;//4;
                var rowsSimple = [
                    ["a", "b"],
                    ["b", "c"],
                    ["c", "d"],
                    ["e", "d"],
                ]
                rows = [
                    ["Hutt Valley", "Whanganul", 359, "direct", "Album", "Lock"],
                    ["Hutt Valley", "Wairarapa", 483, "", "Mail"],
                    ["Hutt Valley", "Capital & Coast", 857, "", "Home"],
                    ["Capital & Coast", "Whanganul", 1183, "", ""],
                    ["Capital & Coast", "Hutt Valley", 735, "", "Heart"],
                    ["Capital & Coast", "Wairarapa", 1172, "", ""],
                    ["Capital & Coast", "MidCentral", 1390, "", ""],
                    ["Whanganul", "Hawkes Bay", 465, "", ""],
                    ["Hawkes Bay", "Wairarapa", 1057, "", ""],
                    ["Hawkes Bay", "MidCentral", 1401, "", ""],
                    ["Hawkes Bay", "Capital & Coast", 1052, "", ""],
                    ["Wairarapa", "Hutt Valley", 213, "", ""]
                ];
            }
            else if (dataView && dataView.table) {
                rows = dataView.table.rows;
            }
            if (sourceCol < 0 || targetCol < 0)
                return <ForceGraphData>{
                    "nodes": {}, "links": [], "minFiles": 0, "maxFiles": 0, "linkedByName": {}, "dataPointsToEnumerate": {}
                };

            rows.forEach(function (item) {
                linkedByName[item[sourceCol] + "," + item[targetCol]] = 1;
                var source = nodes[item[sourceCol]] ||
                    (nodes[item[sourceCol]] = { name: item[sourceCol], image: sourceTypeCol > 0 ? item[sourceTypeCol] : '', adj: [] });
                var target = nodes[item[targetCol]] ||
                    (nodes[item[targetCol]] = { name: item[targetCol], image: targetTypeCol > 0 ? item[targetTypeCol] : '', adj: [] });
                source.adj.push(target);

                var link = {
                    "source": source,
                    "target": target,
                    "filecount": weightCol > 0 ? item[weightCol] : 0,
                    "type": linkTypeCol > 0 ? item[linkTypeCol] : '',
                };
                if (linkTypeCol > 0) {
                    if (!linkDataPoints[item[linkTypeCol]]) {
                        linkDataPoints[item[linkTypeCol]] = {
                            label: item[linkTypeCol],
                            color: colors.getColorByIndex(linkTypeCount++).value,
                        };
                    }
                };
                if (link.filecount < minFiles) { minFiles = link.filecount };
                if (link.filecount > maxFiles) { maxFiles = link.filecount };
                links.push(link);
            });

            var data = {
                "nodes": nodes, "links": links, "minFiles": minFiles, "maxFiles": maxFiles, "linkedByName": linkedByName, "dataPointsToEnumerate": linkDataPoints
            };
            return data;
        }

        public init(options: VisualInitOptions): void {
            this.root = d3.select(options.element.get(0));
            this.colors = options.style.colorPalette.dataColors;
            this.options = this.getDefaultOptions();
        }

        public update(options: VisualUpdateOptions) {
            if (!options.dataViews || (options.dataViews.length < 1)) return;
            var colors = this.colors;
            var data = this.data = ForceGraph.converter(this.dataView = options.dataViews[0], colors);
            if (!data) return;
            this.updateOptions(options.dataViews[0].metadata.objects);
            var voptions = this.options;
            var scale1to10 = d3.scale.linear().domain([data.minFiles, data.maxFiles]).rangeRound([1, 10]).clamp(true);

            var viewport = options.viewport;
            var w = viewport.width,
                h = viewport.height;
            var k = Math.sqrt(Object.keys(data.nodes).length / (w * h));

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
                .charge(voptions.charge / k)
                .on("tick", tick)
                .start();

            if (voptions.showArrow) {
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
                    //below works if no marker-end workaround needed
                        .style("fill", function (d) { return getLinkColor(d); })
                    ;
                    return "url(#" + val + ")";
                }
            }
            var path = svg.selectAll(".link")
                .data(force.links())
                .enter().append("path")
                .attr("class", "link")
                .attr("id", function (d, i) { return "linkid_" + i; })
            // uncomment if we don't need the marker-end workaround
            //.attr("marker-end", function (d, i) { return marker(d, i); })
                .attr("stroke-width", function (d) {
                    return voptions.thickenLink ? scale1to10(d.filecount) : voptions.defaultLinkThickness;
                })
                .style("stroke", function (d) {
                    return getLinkColor(d);
                })
            // no need for "fill" if we don't need the marker-end workaround
                .style("fill", function (d) {
                    if (!voptions.showArrow) return;
                    return getLinkColor(d);
                })
                .on("mouseover", fadePath(.3))
                .on("mouseout", fadePath(1))
                ;

            if (voptions.showLabel) {
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
            } else {
                path.append("title")
                    .text(function (d) { return d.source.name + "-" + d.target.name + ":" + d.filecount });
            }
            // define the nodes
            var node = svg.selectAll(".node")
                .data(force.nodes())
                .enter().append("g")
                .attr("class", "node")
                .call(force.drag)
                .on("mouseover", fadeNode(.3))
                .on("mouseout", fadeNode(1))
                .on("mousedown", function () { d3.event.stopPropagation(); })
                ;

            // add the nodes
            if (voptions.displayImage) {
                node.append("image")
                    .attr("xlink:href", function (d) {
                        return d.image && d.image != '' ?
                            voptions.imageUrl + d.image + voptions.imageExt :
                            (
                                voptions.defaultImage && voptions.defaultImage != '' ?
                                    voptions.imageUrl + voptions.defaultImage + voptions.imageExt :
                                    'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABsAAAAbCAMAAAHNDTTxAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAACuUExURQAAAMbGxvLy8sfHx/Hx8fLy8vHx8cnJycrKyvHx8fHx8cvLy/Ly8szMzM3NzfHx8dDQ0PHx8fLy8vHx8e/v79LS0tPT0/Ly8tTU1NXV1dbW1vHx8fHx8fDw8NjY2PT09PLy8vLy8vHx8fLy8vHx8fHx8enp6fDw8PLy8uPj4+Tk5OXl5fHx8b+/v/Pz8+bm5vHx8ejo6PLy8vHx8fLy8sTExPLy8vLy8sXFxfHx8YCtMbUAAAA6dFJOUwD/k/+b7/f///+r/////0z/w1RcEP//ZP///4fj/v8Yj3yXn/unDEhQ////YP9Y/8//aIMU/9+L/+fzC4s1AAAACXBIWXMAABcRAAAXEQHKJvM/AAABQElEQVQoU5WS61LCMBCFFymlwSPKVdACIgWkuNyL+P4v5ibZ0jKjP/xm0uw5ySa7mRItAhnMoIC5TwQZdCZiZjcoC8WU6EVsmZgzoqGdxafgvJAvjUXCb2M+0cXNsd/GDarZqSf7av3M2P1E3xhfLkPUvLD5joEYwVVJQXM6+9McWUwLf4nDTCQZAy96UoDjNI/jhl3xPLbQamu8xD7iaIsPKw7GJ7KZEnWLY3Gi8EFj5nqibXnwD5VEGjJXk5sbpLppfvvo1RazQVrhSopPK4TODrtnjS3dY4ic8KurruWQYF+UG60BacexTMyT2jlNg41dOmKvTpkUd/Jevy7ZxQ61ULRUpoododx8GeDPvIrktbFVdUsK6f8Na5VlVpjZJtowTXVy7kfXF5wCaV1tqXAFuIdWJu+JviaQzNzfQvQDGKRXXEmy83cAAAAASUVORK5CYII='
                            );
                    })
                    .attr("x", "-12px")
                    .attr("y", "-12px")
                    .attr("width", "24px")
                    .attr("height", "24px");
            } else {
                node.append("circle")
                    .attr("r", function (d) {
                        if (d.weight < 5) {
                            d.radius = 5;
                        } else {
                            d.radius = d.weight;
                        }
                        return d.radius;
                    });
            }

            // add the text 
            node.append("text")
                .attr("x", 12)
                .attr("dy", ".35em")
                .text(function (d) {
                    var name: string = d.name;
                    return name ? (name.length > voptions.nameMaxLength ? name.substr(0, voptions.nameMaxLength) : name) : '';
                });

            function getLinkColor(d): string {
                if (voptions.colorLink == linkColorType.interactive)
                    return voptions.defaultLinkColor;
                if (voptions.colorLink == linkColorType.byWeight)
                    return colors.getColorByIndex(scale1to10(d.filecount)).value;
                if (voptions.colorLink == linkColorType.byLinkType)
                    return data.dataPointsToEnumerate[d.type].color;
            }

            function isConnected(a, b) {
                return data.linkedByName[a.name + "," + b.name] || data.linkedByName[b.name + "," + a.name] || a.name == b.name;
            }

            function isReachable(a, b) {
                if (a.name == b.name) return true;
                if (data.linkedByName[a.name + "," + b.name]) return true;
                var visited = {};
                for (var name in data.nodes) {
                    visited[name] = false;
                };
                visited[a.name] = true;

                var stack = [];
                stack.push(a.name);
                while (stack.length > 0) {
                    var cur = stack.pop();
                    var node = data.nodes[cur];
                    for (var i = 0; i < node.adj.length; i++) {
                        var nb = node.adj[i];
                        if (nb.name == b.name) return true;

                        if (!visited[nb.name]) {
                            visited[nb.name] = true;
                            stack.push(nb.name);
                        }
                    }
                };
                return false;
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

                if (voptions.showArrow) {
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
                } else {
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
                }

                node
                    .attr("transform", function (d) {
                        return "translate(" + d.x + "," + d.y + ")";
                    });
            };

            function fadeNode(opacity) {
                if (voptions.colorLink != linkColorType.interactive) return;
                return function (d) {
                    node.style("stroke-opacity", function (o) {
                        var thisOpacity = (voptions.highlightReachableLinks ? isReachable(d, o) : isConnected(d, o)) ? 1 : opacity;
                        this.setAttribute('fill-opacity', thisOpacity);
                        return thisOpacity;
                    });

                    path.style("stroke-opacity", function (o) {
                        return (voptions.highlightReachableLinks ? isReachable(d, o.source) :
                            (o.source === d || o.target === d)) ? 1 : opacity;
                    });
                    path.style("stroke", function (o) {
                        return (voptions.highlightReachableLinks ? isReachable(d, o.source) :
                            (o.source === d || o.target === d)) ? voptions.defaultLinkHighlightColor : voptions.defaultLinkColor;
                    });
                };
            }

            function fadePath(opacity) {
                if (voptions.colorLink != linkColorType.interactive) return;
                return function (d) {
                    path.style("stroke-opacity", function (o) {
                        return o.source === d.source && o.target === d.target ? 1 : opacity;
                    });
                    path.style("stroke", function (o) {
                        return o.source === d.source && o.target === d.target ? voptions.defaultLinkHighlightColor : voptions.defaultLinkColor;
                    });
                };
            }
        }

        public destroy(): void {
            this.root = null;
        }
    }

}
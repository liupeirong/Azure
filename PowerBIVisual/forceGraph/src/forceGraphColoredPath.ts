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
        },
        size: {
            charge: <DataViewObjectPropertyIdentifier>{ objectName: 'size', propertyName: 'charge' },
        }
    }

    export interface ForceGraphData {
        nodes: any[];
        links: any[];
        minFiles: number;
        maxFiles: number;
        linkedByName: {};
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
            this.options.charge = DataViewObjects.getValue(objects, forceProps.size.charge, this.options.charge);
        }

        public static capabilities: VisualCapabilities = {
            dataRoles: [
                {
                    name: 'Values',
                    kind: VisualDataRoleKind.GroupingOrMeasure,
                },
                {
                    name: 'LinkType',
                    kind: VisualDataRoleKind.Grouping,
                    displayName: 'LinkType',
                },
                {
                    name: 'SourceType',
                    kind: VisualDataRoleKind.Grouping,
                    displayName: 'SourceType',
                },
                {
                    name: 'TargetType',
                    kind: VisualDataRoleKind.Grouping,
                    displayName: 'TargetType',
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
                            displayName: 'Label'
                        },
                        colorLink: {
                            type: { enumeration: linkColorType.type },
                            displayName: 'Color'
                        },
                        thickenLink: {
                            type: { bool: true },
                            displayName: 'Thickness'
                        },
                    }
                },
                nodes: {
                    displayName: data.createDisplayNameGetter('Visual_Nodes'),
                    properties: {
                        displayImage: {
                            type: { bool: true },
                            displayName: 'Image'
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
                            displayName: 'Max name length'
                        },
                    }
                },
                size: {
                    displayName: data.createDisplayNameGetter('Visual_Size'),
                    properties: {
                        charge: {
                            type: { numeric: true },
                            displayName: 'Charge'
                        },
                    }
                },
                dataPoint: {
                    displayName: data.createDisplayNameGetter('Visual_DataPoint'),
                    properties: {
                        defaultColor: {
                            displayName: data.createDisplayNameGetter('Visual_DefaultColor'),
                            type: { fill: { solid: { color: true } } }
                        },
                        showAllDataPoints: {
                            displayName: data.createDisplayNameGetter('Visual_DataPoint_Show_All'),
                            type: { bool: true }
                        },
                        fill: {
                            displayName: data.createDisplayNameGetter('Visual_Fill'),
                            type: { fill: { solid: { color: true } } }
                        },
                    }
                },
            },
            dataViewMappings: [{
                //conditions: [
                //    {'Values': {min: 2}, 'LinkType': {max: 1}, 'SourceType': {max: 1}, 'TargetType': {max: 1}},
                //],
                categorical: {
                    values: {
                        select: [
                            { bind: { to: 'Values' } },
                            { bind: { to: 'LinkType' } },
                            { bind: { to: 'SourceType' } },
                            { bind: { to: 'TargetType' } },
                        ],
                        dataReductionAlgorithm: { window: {} }
                    },
                    rowCount: { preferred: { min: 1 } }
                },
            }],
            suppressDefaultTitle: true,
        };

        public enumerateObjectInstances(options: EnumerateVisualObjectInstancesOptions): VisualObjectInstance[] {
            var enumeration = new ObjectEnumerationBuilder();
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
                case 'dataPoint':
                    break;
            }

            return instances;
        }

        public static converter(dataView: DataView): any {
            var nodes = {};
            var minFiles = Number.MAX_VALUE;
            var maxFiles = 0;
            var linkedByName = {};
            var links = [];
            var weightCol = -1, linkTypeCol = -1, sourceTypeCol = -1, targetTypeCol = -1;
            var valueCol = 0;
            var rows;

            if (dataView && dataView.categorical && dataView.categorical.categories && dataView.metadata && dataView.metadata.columns) {
                var metadataColumns = dataView.metadata.columns;
                for (var i = 0; i < metadataColumns.length; i++) {
                    var col = metadataColumns[i];
                    if (col.roles) {
                        if (col.roles['Values'])
                            ++valueCol;
                        if (col.roles['LinkType'])
                            linkTypeCol = i;
                        if (col.roles['SourceType'])
                            sourceTypeCol = i;
                        if (col.roles['TargetType'])
                            targetTypeCol = i;
                    }
                }
                if (valueCol > 2) { weightCol = valueCol - 1; }
            }
            if (valueCol < 2) return null;

            if (test) {
                weightCol = 2;
                linkTypeCol = 3;
                sourceTypeCol = 5;
                targetTypeCol = 4;
                rows = [
                    ["Hutt Valley", "Whanganul", 359, "direct", "Album", "Lock"],
                    ["Hutt Valley", "Wairarapa", 483, "", "Mail"],
                    ["Hutt Valley", "Capital & Coast", 857, "", "Home"],
                    ["Hutt Valley", "Hawkes Bay", 1304, "", ""],
                    ["Hutt Valley", "MidCentral", 1526, "co", "Egg"],
                    ["Capital & Coast", "Whanganul", 1183, "", ""],
                    ["Capital & Coast", "Hutt Valley", 735, "", "Heart"],
                    ["Capital & Coast", "Wairarapa", 1172, "", ""],
                    ["Capital & Coast", "MidCentral", 1390, "", ""],
                    ["Capital & Coast", "Hawkes Bay", 955, "", ""],
                    ["Hawkes Bay", "Whanganul", 465, "", ""],
                    ["Hawkes Bay", "Wairarapa", 1057, "", ""],
                    ["Hawkes Bay", "MidCentral", 1401, "", ""],
                    ["Hawkes Bay", "Capital & Coast", 1052, "", ""],
                    ["Hawkes Bay", "Hutt Valley", 213, "", ""]
                ];

            }
            else if (dataView && dataView.table) {
                rows = dataView.table.rows;
            }
            rows.forEach(function (item) {
                linkedByName[item[0] + "," + item[1]] = 1;
                var link;
                link = {
                    "source": nodes[item[0]] || (nodes[item[0]] = { name: item[0], image: sourceTypeCol > 0 ? item[sourceTypeCol] : '' }),
                    "target": nodes[item[1]] || (nodes[item[1]] = { name: item[1], image: targetTypeCol > 0 ? item[targetTypeCol] : '' }),
                    "filecount": weightCol > 0 ? item[weightCol] : 0,
                    "type": linkTypeCol > 0 ? item[linkTypeCol] : '',
                };
                if (link.filecount < minFiles) { minFiles = link.filecount };
                if (link.filecount > maxFiles) { maxFiles = link.filecount };
                links.push(link);
            });
            var data = {
                "nodes": nodes, "links": links, "minFiles": minFiles, "maxFiles": maxFiles, "linkedByName": linkedByName
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
            var data = this.data = ForceGraph.converter(this.dataView = options.dataViews[0]);
            if (!data) return;
            this.updateOptions(options.dataViews[0].metadata.objects);
            var voptions = this.options;
            var colors = this.colors;
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
                    return colors.getColorByIndex(type2Index(d.type)).value;
            }

            function type2Index(d: string): number {
                return d ? d.length % 10 : 0;
            }

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
                        var thisOpacity = isConnected(d, o) ? 1 : opacity;
                        this.setAttribute('fill-opacity', thisOpacity);
                        return thisOpacity;
                    });

                    path.style("stroke-opacity", function (o) {
                        return o.source === d || o.target === d ? 1 : opacity;
                    });
                    path.style("stroke", function (o) {
                        return o.source === d || o.target === d ? voptions.defaultLinkHighlightColor : voptions.defaultLinkColor;
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
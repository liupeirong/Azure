/*
 *  Power BI Visualizations
 *
 *  Copyright (c) Microsoft Corporation
 *  All rights reserved. 
 *  MIT License
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a copy
 *  of this software and associated documentation files (the ""Software""), to deal
 *  in the Software without restriction, including without limitation the rights
 *  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 *  copies of the Software, and to permit persons to whom the Software is
 *  furnished to do so, subject to the following conditions:
 *   
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *   
 *  THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 *  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 *  THE SOFTWARE.
 */

/*
 *  This file is based on or incorporates material from the projects listed below (Third Party IP). 
 *  The original copyright notice and the license under which Microsoft received such Third Party IP, 
 *  are set forth below. Such licenses and notices are provided for informational purposes only. 
 *  Microsoft licenses the Third Party IP to you under the licensing terms for the Microsoft product. 
 *  Microsoft reserves all other rights not expressly granted under this agreement, whether by 
 *  implication, estoppel or otherwise.
 *  
 *  d3 Force Layout
 *  Copyright (c) 2010-2015, Michael Bostock
 *  All rights reserved.
 *  
 *  Redistribution and use in source and binary forms, with or without
 *  modification, are permitted provided that the following conditions are met:
 *  
 *  * Redistributions of source code must retain the above copyright notice, this
 *    list of conditions and the following disclaimer.
 *  
 *  * Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 *  
 *  * The name Michael Bostock may not be used to endorse or promote products
 *    derived from this software without specific prior written permission.
 *  
 *  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 *  AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 *  IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 *  DISCLAIMED. IN NO EVENT SHALL MICHAEL BOSTOCK BE LIABLE FOR ANY DIRECT,
 *  INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
 *  BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 *  DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY
 *  OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 *  NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
 *  EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

module powerbi.visuals {
    export var test = false;
    module linkColorType {
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
        colorLink: string;
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
        }
    };

    export interface ForceGraphData {
        nodes: {};
        links: any[];
        minFiles: number;
        maxFiles: number;
        linkedByName: {};
        linkTypes: {};
    }

    export class ForceGraph implements IVisual {
        public static VisualClassName = 'forceGraph';
        private root: D3.Selection;
        private paths: D3.Selection;
        private nodes: D3.Selection;
        private forceLayout: D3.Layout.ForceLayout;
        private dataView: DataView;
        private colors: IDataColorPalette;
        private options: ForceGraphOptions;
        private data: ForceGraphData;

        private marginValue: IMargin;
        private get margin(): IMargin {
            return this.marginValue || { left: 0, right: 0, top: 0, bottom: 0 };
        }
        private set margin(value: IMargin) {
            this.marginValue = $.extend({}, value);
            this.viewportInValue = ForceGraph.substractMargin(this.viewport, this.margin);
        }

        private viewportValue: IViewport;
        private get viewport(): IViewport {
            return this.viewportValue || { width: 0, height: 0 };
        }
        private set viewport(value: IViewport) {
            this.viewportValue = $.extend({}, value);
            this.viewportInValue = ForceGraph.substractMargin(this.viewport, this.margin);
        }

        private viewportInValue: IViewport;
        private get viewportIn(): IViewport {
            return this.viewportInValue || this.viewport;
        }

        private static substractMargin(viewport: IViewport, margin: IMargin): IViewport {
            return {
                width: Math.max(viewport.width - (margin.left + margin.right), 0),
                height: Math.max(viewport.height - (margin.top + margin.bottom), 0)
            };
        }

        private scale1to10(d) {
            var scale = d3.scale.linear().domain([this.data.minFiles, this.data.maxFiles]).rangeRound([1, 10]).clamp(true);
            return scale(d);
        }

        private getLinkColor(d): string {
            switch (this.options.colorLink) {
                case linkColorType.byWeight:
                    return this.colors.getColorByIndex(this.scale1to10(d.filecount)).value;
                case linkColorType.byLinkType:
                    return d.type && this.data.linkTypes[d.type] ? this.data.linkTypes[d.type].color : this.options.defaultLinkColor;
            };
            return this.options.defaultLinkColor;
        }

        private getDefaultOptions(): ForceGraphOptions {
            return {
                showArrow: false,
                showLabel: false,
                colorLink: linkColorType.interactive,
                thickenLink: true,
                displayImage: false,
                defaultImage: "Home",
                imageUrl: "https://pliupublic.blob.core.windows.net/files/",
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
            if (this.options.charge >= 0 || this.options.charge < -100) this.options.charge = this.getDefaultOptions().charge;
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
                            description: 'The larger the negative charge the more apart the entities, must be negative but greater than -100',
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
                if (false) // for testing highlightallreachable 
                {
                    weightCol = -1;
                    linkTypeCol = -1;
                    sourceTypeCol = -1;
                    targetTypeCol = -1;
                    rows = [
                        ["a", "b"],
                        ["b", "c"],
                        ["c", "d"],
                        ["e", "f"],
                    ]
                }
                else {
                    weightCol = 2;
                    linkTypeCol = 3;
                    sourceTypeCol = 5;
                    targetTypeCol = 4;

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
            }
            else if (dataView && dataView.table) {
                rows = dataView.table.rows;
            }
            if (sourceCol < 0 || targetCol < 0)
                return <ForceGraphData>{
                    "nodes": {}, "links": [], "minFiles": 0, "maxFiles": 0, "linkedByName": {}, "linkTypes": {}
                };

            rows.forEach(function (item) {
                linkedByName[item[sourceCol] + "," + item[targetCol]] = 1;
                var source = nodes[item[sourceCol]] ||
                    (nodes[item[sourceCol]] = { name: item[sourceCol], image: sourceTypeCol > 0 ? item[sourceTypeCol] : '', adj: {} });
                var target = nodes[item[targetCol]] ||
                    (nodes[item[targetCol]] = { name: item[targetCol], image: targetTypeCol > 0 ? item[targetTypeCol] : '', adj: {} });
                source.adj[target.name] = 1;
                target.adj[source.name] = 1;

                var link = {
                    source: source,
                    target: target,
                    filecount: weightCol > 0 ? item[weightCol] : 0,
                    type: linkTypeCol > 0 ? item[linkTypeCol] : '',
                };
                if (linkTypeCol > 0) {
                    if (!linkDataPoints[item[linkTypeCol]]) {
                        linkDataPoints[item[linkTypeCol]] = {
                            label: item[linkTypeCol],
                            color: colors.getColorByIndex(linkTypeCount++).value,
                        };
                    };
                };
                if (link.filecount < minFiles) { minFiles = link.filecount; };
                if (link.filecount > maxFiles) { maxFiles = link.filecount; };
                links.push(link);
            });

            var data = {
                "nodes": nodes, "links": links, "minFiles": minFiles, "maxFiles": maxFiles, "linkedByName": linkedByName, "linkTypes": linkDataPoints
            };
            return data;
        }

        public init(options: VisualInitOptions): void {
            this.root = d3.select(options.element.get(0));
            this.forceLayout = d3.layout.force();
            this.colors = options.style.colorPalette.dataColors;
            this.options = this.getDefaultOptions();
        }

        public update(options: VisualUpdateOptions) {
            if (!options.dataViews || (options.dataViews.length < 1)) return;
            this.data = ForceGraph.converter(this.dataView = options.dataViews[0], this.colors);
            if (!this.data) return;
            if (options.dataViews[0].metadata && options.dataViews[0].metadata.objects)
                this.updateOptions(options.dataViews[0].metadata.objects);
            this.viewport = options.viewport;
            var k = Math.sqrt(Object.keys(this.data.nodes).length / (this.viewport.width * this.viewport.height));

            this.root.selectAll("svg").remove();

            var svg = this.root
                .append("svg")
                .attr("width", this.viewport.width)
                .attr("height", this.viewport.height)
                .classed(ForceGraph.VisualClassName, true);

            this.forceLayout
                .gravity(100 * k)
                .links(this.data.links)
                .size([this.viewport.width, this.viewport.height])
                .linkDistance(100)
                .charge(this.options.charge / k)
                .on("tick", this.tick());
            this.updateNodes();
            this.forceLayout.start();

            // uncomment if we don't need the marker-end workaround
            //if (this.options.showArrow) {
                    // build the arrow.
                    //function marker(d, i) {
                    //    var val = "mid_" + i;
                    //    svg.append("defs").selectAll("marker")
                    //        .data([val])      // Different link/path types can be defined here
                    //        .enter().append("marker")    // This section adds in the arrows
                    //        .attr("id", String)
                    //        .attr("viewBox", "0 -5 10 10")
                    //        .attr("refX", 10)
                    //        .attr("refY", 0)
                    //        .attr("markerWidth", 6)
                    //        .attr("markerHeight", 6)
                    //        .attr("orient", "auto")
                    //        .attr("markerUnits", "userSpaceOnUse")
                    //        .append("path")
                    //        .attr("d", "M0,-5L10,0L0,5")
                    //    //below works if no marker-end workaround needed
                    //        .style("fill", d => this.getLinkColor(d))
                    //    ;
                    //    return "url(#" + val + ")";
                    //}
            //}
            this.paths = svg.selectAll(".link")
                .data(this.forceLayout.links())
                .enter().append("path")
                .attr("class", "link")
                .attr("id", (d, i) => "linkid_" + i)
            // uncomment if we don't need the marker-end workaround
            //.attr("marker-end", function (d, i) { return marker(d, i); })
                .attr("stroke-width", d => this.options.thickenLink ? this.scale1to10(d.filecount) : this.options.defaultLinkThickness)
                .style("stroke", d => this.getLinkColor(d))
            // no need for "fill" if we don't need the marker-end workaround
                .style("fill", d => { if (this.options.showArrow) return this.getLinkColor(d); })
                .on("mouseover", this.fadePath(.3, this.options.defaultLinkHighlightColor))
                .on("mouseout", this.fadePath(1, this.options.defaultLinkColor))
            ;

            if (this.options.showLabel) {
                svg.selectAll(".linklabelholder")
                    .data(this.forceLayout.links())
                    .enter().append("g")
                    .attr("class", "linklabelholder")
                    .append("text")
                    .attr("class", "linklabel")
                    .attr("y", "-12")
                    .attr("text-anchor", "middle")
                    .style("fill", "#000")
                    .append("textPath")
                    .attr("xlink:href", (d, i) => "#linkid_" + i)
                    .attr("startOffset", "25%") //use "50%" if we don't need the marker-end workaround
                    .text(d => this.options.colorLink === linkColorType.byLinkType ? d.type : d.filecount);
            } else {
                this.paths.append("title")
                    .text(d => this.options.colorLink === linkColorType.byLinkType ? d.type : d.source.name + "-" + d.target.name + ":" + d.filecount);
            }
            // define the nodes
            this.nodes = svg.selectAll(".node")
                .data(this.forceLayout.nodes())
                .enter().append("g")
                .attr("class", "node")
                .call(this.forceLayout.drag)
                .on("mouseover", this.fadeNode(.3, this.options.defaultLinkHighlightColor))
                .on("mouseout", this.fadeNode(1, this.options.defaultLinkColor))
                .on("mousedown", () => d3.event.stopPropagation())
            ;

            // add the nodes
            if (this.options.displayImage) {
                this.nodes.append("image")
                    .attr("xlink:href", d =>
                        d.image && d.image !== '' ?
                            this.options.imageUrl + d.image + this.options.imageExt :
                            (
                                this.options.defaultImage && this.options.defaultImage !== '' ?
                                    this.options.imageUrl + this.options.defaultImage + this.options.imageExt :
                                    'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAABsAAAAbCAMAAAHNDTTxAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAACuUExURQAAAMbGxvLy8sfHx/Hx8fLy8vHx8cnJycrKyvHx8fHx8cvLy/Ly8szMzM3NzfHx8dDQ0PHx8fLy8vHx8e/v79LS0tPT0/Ly8tTU1NXV1dbW1vHx8fHx8fDw8NjY2PT09PLy8vLy8vHx8fLy8vHx8fHx8enp6fDw8PLy8uPj4+Tk5OXl5fHx8b+/v/Pz8+bm5vHx8ejo6PLy8vHx8fLy8sTExPLy8vLy8sXFxfHx8YCtMbUAAAA6dFJOUwD/k/+b7/f///+r/////0z/w1RcEP//ZP///4fj/v8Yj3yXn/unDEhQ////YP9Y/8//aIMU/9+L/+fzC4s1AAAACXBIWXMAABcRAAAXEQHKJvM/AAABQElEQVQoU5WS61LCMBCFFymlwSPKVdACIgWkuNyL+P4v5ibZ0jKjP/xm0uw5ySa7mRItAhnMoIC5TwQZdCZiZjcoC8WU6EVsmZgzoqGdxafgvJAvjUXCb2M+0cXNsd/GDarZqSf7av3M2P1E3xhfLkPUvLD5joEYwVVJQXM6+9McWUwLf4nDTCQZAy96UoDjNI/jhl3xPLbQamu8xD7iaIsPKw7GJ7KZEnWLY3Gi8EFj5nqibXnwD5VEGjJXk5sbpLppfvvo1RazQVrhSopPK4TODrtnjS3dY4ic8KurruWQYF+UG60BacexTMyT2jlNg41dOmKvTpkUd/Jevy7ZxQ61ULRUpoododx8GeDPvIrktbFVdUsK6f8Na5VlVpjZJtowTXVy7kfXF5wCaV1tqXAFuIdWJu+JviaQzNzfQvQDGKRXXEmy83cAAAAASUVORK5CYII='
                            )
                    )
                    .attr("x", "-12px")
                    .attr("y", "-12px")
                    .attr("width", "24px")
                    .attr("height", "24px");
            } else {
                this.nodes.append("circle")
                    .attr("r", d => d.weight < 5 ? 5 : d.weight);
            }

            // add the text 
            this.nodes.append("text")
                .attr("x", 12)
                .attr("dy", ".35em")
                .text(d => d.name ? (d.name.length > this.options.nameMaxLength ? d.name.substr(0, this.options.nameMaxLength) : d.name) : '');
        }

        private updateNodes() {
            var oldNodes = this.forceLayout.nodes();
            this.forceLayout.nodes(d3.values(this.data.nodes));
            this.forceLayout.nodes().forEach((node, i) => {
                if (!oldNodes[i]) {
                    return;
                }
                node.x = oldNodes[i].x;
                node.y = oldNodes[i].y;
                node.px = oldNodes[i].px;
                node.py = oldNodes[i].py;
                node.weight = oldNodes[i].weight;
            });
        }

        private tick() {
            var viewport = this.viewportIn;
            // limitX and limitY is necessary when you minimize the graph and then resize it to normal.
            //"width/height * 20" seems enough to move nodes freely by force layout.
            var maxWidth = viewport.width * 20;
            var maxHeight = viewport.height * 20;
            var limitX = x => Math.max((viewport.width - maxWidth) / 2, Math.min((viewport.width + maxWidth) / 2, x));
            var limitY = y => Math.max((viewport.height - maxHeight) / 2, Math.min((viewport.height + maxHeight) / 2, y));
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

            var getPath = this.options.showArrow ?
                //this is for marker-end workaround, build the marker with the path
                d => {
                    d.source.x = limitX(d.source.x);
                    d.source.y = limitY(d.source.y);
                    d.target.x = limitX(d.target.x);
                    d.target.y = limitY(d.target.y);
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
                } :
                d => {
                    d.source.x = limitX(d.source.x);
                    d.source.y = limitY(d.source.y);
                    d.target.x = limitX(d.target.x);
                    d.target.y = limitY(d.target.y);
                    var dx = d.target.x - d.source.x,
                        dy = d.target.y - d.source.y,
                        dr = Math.sqrt(dx * dx + dy * dy);
                    return "M" +
                        d.source.x + "," +
                        d.source.y + "A" +
                        dr + "," + dr + " 0 0,1 " +
                        d.target.x + "," +
                        d.target.y;
                };

            return () => {
                this.paths.each(function () { this.parentNode.insertBefore(this, this); });
                this.paths.attr("d", getPath);
                this.nodes.attr("transform", d => "translate(" + limitX(d.x) + "," + limitY(d.y) + ")");
            };
        }

        private fadePath(opacity: number, highlight: string) {
            if (this.options.colorLink !== linkColorType.interactive) return;
            return d => {
                this.paths.style("stroke-opacity", o => o.source === d.source && o.target === d.target ? 1 : opacity);
                this.paths.style("stroke", o => o.source === d.source && o.target === d.target ? highlight : this.options.defaultLinkColor);
            };
        }

        private isReachable(a, b): boolean {
            if (a.name === b.name) return true;
            if (this.data.linkedByName[a.name + "," + b.name]) return true;
            var visited = {};
            for (var name in this.data.nodes) {
                visited[name] = false;
            };
            visited[a.name] = true;

            var stack = [];
            stack.push(a.name);
            while (stack.length > 0) {
                var cur = stack.pop();
                var node = this.data.nodes[cur];
                for (var nb in node.adj) {
                    if (nb === b.name) return true;

                    if (!visited[nb]) {
                        visited[nb] = true;
                        stack.push(nb);
                    }
                }
            };
            return false;
        }

        private fadeNode(opacity: number, highlight: string) {
            if (this.options.colorLink !== linkColorType.interactive) return;
            var isConnected = (a, b) => this.data.linkedByName[a.name + "," + b.name] || this.data.linkedByName[b.name + "," + a.name] || a.name === b.name;

            return d => {
                var that = this;
                this.nodes.style("stroke-opacity", function (o) {
                    var thisOpacity = (that.options.highlightReachableLinks ? that.isReachable(d, o) : isConnected(d, o)) ? 1 : opacity;
                    this.setAttribute('fill-opacity', thisOpacity);
                    return thisOpacity;
                });

                this.paths.style("stroke-opacity", o =>
                    (this.options.highlightReachableLinks ? this.isReachable(d, o.source) :
                        (o.source === d || o.target === d)) ? 1 : opacity);
                this.paths.style("stroke", o =>
                    (this.options.highlightReachableLinks ? this.isReachable(d, o.source) :
                        (o.source === d || o.target === d)) ? highlight : this.options.defaultLinkColor);
            };
        }

        public destroy(): void {
            this.root = null;
        }
    }

}
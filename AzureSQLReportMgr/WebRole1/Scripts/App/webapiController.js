var webapiControllers = angular.module('webapiControllers', ['webapiService', 'ui.grid', 'ui.grid.selection', 'ui.grid.resizeColumns']);

webapiControllers.controller('reportsGenerateController', ['$scope', 'ReportGenerator', 
    function ($scope, ReportGenerator) {
        var reports = [];
        $scope.reportsToGenerate = {
            // filtering
            enableFiltering: true,
            columnDefs: [
                {
                    name: 'Report Name',
                    field: 'Path',
                    filter: {
                        condition: function (searchTerm, reportPath) {
                            var reportPath2Up = reportPath.toUpperCase();
                            var searchTerm2Up = searchTerm.toUpperCase();
                            return reportPath2Up.indexOf(searchTerm2Up) >= 0;
                        }
                    }
                },
            ],
            // selection
            enableRowSelection: true,
            multiSelect: true,
            enableRowHeaderSelection: false,
            onRegisterApi: function(gridApi) {
                $scope.gridApi = gridApi;
            }
        };
        $scope.selectAll = function () {
            $scope.gridApi.selection.selectAllRows();
        };
        $scope.clearAll = function () {
            $scope.gridApi.selection.clearSelectedRows();
        };
        $scope.generateSelected = function () {
            $('#confirmModal').modal('hide');
            var succCnt = 0;
            var selectedRows = $scope.gridApi.selection.getSelectedRows();
            for (ii = 0; ii < selectedRows.length; ++ii) {
                var row = selectedRows[ii];
                var req = row.Path + "&rs:Format=PDF";
                var report = { Path: req};
                ReportGenerator.update(report, function (success) {
                    ++succCnt;
                    if (succCnt == selectedRows.length) {
                        $scope.message = "Requests queued successfully.";
                        $('#msgModal').modal('show');
                    }
                },
                function (err) {
                    $scope.message = "Failed to queue request: " + req + ". " + err.status + err.statusText + ". " + err.data.ExceptionMessage;
                    $('#msgModal').modal('show');
                });
            };
            
        };
        $scope.noSelection = function () {
            var selectedRows = $scope.gridApi.selection.getSelectedRows();
            return (selectedRows == null || selectedRows.length <= 0);
        };
        $scope.$evalAsync(function () {
            $scope.autoMessage = "Retrieving data, please wait...";
            $('#autoModal').modal('show');
            ReportGenerator.query(function (data) {
                angular.forEach(data, function (report) {
                    var reportItem = {
                        Path: report.Path,
                    };
                    reports.push(reportItem);
                })
                $('#autoModal').modal('hide');
            }, function (err) {
                $('#autoModal').modal('hide');
                $scope.message = "Failed to retrieve data. " + err.status + " " + err.statusText + ". " + err.data.ExceptionMessage;
                $('#msgModal').modal('show');
            });
        });
        $scope.reportsToGenerate.data = reports;
}]);

webapiControllers.controller('latestReportsController', ['$scope', 'ReportServer',
    function ($scope, ReportServer) {
        var reports = [];
        $scope.latestReports = {
            // filtering
            enableFiltering: true,
            columnDefs: [
                {
                    name: 'Name',
                    field: 'Name',
                    width: '60%',
                    filter: {
                        condition: function (searchTerm, reportPath) {
                            var reportPath2Up = reportPath.toUpperCase();
                            var searchTerm2Up = searchTerm.toUpperCase();
                            return reportPath2Up.indexOf(searchTerm2Up) >= 0;
                        }
                    }
                },
                { name: 'Format', field: 'Format', width: '10%', enableFiltering: false },
                { name: 'Last Updated', field: 'ModifiedDate', width: '20%', enableFiltering: false },
                {
                    name: 'Location', field: 'Path', width: '10%', enableFiltering: false,
                    cellTemplate: '<a href={{row.entity[col.field]}}>Download</a>'
                },
            ],
        };
        $scope.$evalAsync(function () {
            $scope.autoMessage = "Retrieving data, please wait...";
            $('#autoModal').modal('show');
            ReportServer.getLatest.query(function (data) {
                angular.forEach(data, function (report) {
                    var localDate = new Date(report.ModifiedDate);
                    var reportItem = {
                        Name: report.Name,
                        Format: report.Format,
                        Path: report.Path,
                        ModifiedDate: localDate.toLocaleString(),
                    };
                    reports.push(reportItem);
                })
                $('#autoModal').modal('hide');
            }, function (err) {
                $('#autoModal').modal('hide');
                $scope.messge = "Failed to retrieve data. " + err.status + " " + err.statusText + ". " + err.data.ExceptionMessage;
                $('#msgModal').modal('show');
            });
        });
        $scope.latestReports.data = reports;
    }]);

webapiControllers.controller('archivedReportsController', ['$scope', 'ReportServer',
    function ($scope, ReportServer) {
        var reports = [];
        $scope.archivedReports = {
            // filtering
            enableFiltering: true,
            columnDefs: [
                {
                    name: 'Name',
                    field: 'Name',
                    width: '60%',
                    filter: {
                        condition: function (searchTerm, reportPath) {
                            var reportPath2Up = reportPath.toUpperCase();
                            var searchTerm2Up = searchTerm.toUpperCase();
                            return reportPath2Up.indexOf(searchTerm2Up) >= 0;
                        }
                    }
                },
                { name: 'Format', field: 'Format', width: '10%', enableFiltering: false },
                { name: 'Last Updated', field: 'ModifiedDate', width: '20%', enableFiltering: false },
                {
                    name: 'Location', field: 'Path', width: '10%', enableFiltering: false,
                    cellTemplate: '<a href={{row.entity[col.field]}}>Download</a>'
                },
            ],
        };
        $scope.$evalAsync(function () {
            $scope.autoMessage = "Retrieving data, please wait...";
            $('#autoModal').modal('show');
            ReportServer.getArchive.query(function (data) {
                angular.forEach(data, function (report) {
                    var localDate = new Date(report.ModifiedDate);
                    var reportItem = {
                        Name: report.Name,
                        Format: report.Format,
                        Path: report.Path,
                        ModifiedDate: localDate.toLocaleString(),
                    };
                    reports.push(reportItem);
                })
                $('#autoModal').modal('hide');
            }, function (err) {
                $('#autoModal').modal('hide');
                $scope.message = "Failed to retrieve data. " + err.status + " " + err.statusText + ". " + err.data.ExceptionMessage;
                $('#msgModal').modal('show');
            });
        });
        $scope.archivedReports.data = reports;
    }]);

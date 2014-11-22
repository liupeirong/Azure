var mainApp = angular.module('mainApp', ['treeGrid']);

mainApp.controller('syncGridController', ['$scope', '$http', function ($scope, $http) {
    $scope.my_tree = {};
    $scope.tree_data = [];
    $scope.expanding_property = "Name";
    $scope.col_defs = [
        { field: "Value" },
        { field: "LastUpdatedAt" },
        { field: "DelayedBy" },
        { field: "Status" }
    ];
    
    $scope.$evalAsync(function () {
        $http.get('http://www.corsproxy.com/yourblob/sqldatasync/syncstatus')
            .success(function (data, status, headers, config) {
                $scope.tree_data = data;
            })
            .error(function (data, status, headers, config) {
                $scope.message = "Failed to retrieve data. " + status + " " + data + ". ";
                $('#msgModal').modal('show');
            });
    });
}]);



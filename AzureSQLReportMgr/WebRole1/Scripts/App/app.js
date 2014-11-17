var mainApp = angular.module('mainApp', ['ngRoute', 'webapiControllers', 'webapiService']);

mainApp.config([
    '$routeProvider',
    function ($routeProvider) {
        $routeProvider.
        when('/reportgen', {
            templateUrl: '/Scripts/App/Views/reportgen.html',
            controller: 'reportsGenerateController'
        }).
        when('/latestreports', {
            templateUrl: '/Scripts/App/Views/latestreports.html',
            controller: 'reportsGenerateController'
        }).
        when('/archivedreports', {
            templateUrl: '/Scripts/App/Views/archivedreports.html',
            controller: 'reportsGenerateController'
        }).
        otherwise({
            redirectTo: '/latestreports'
        });
    }
]);

mainApp.controller('navController', ['$scope', '$location', function ($scope, $location) {
    $scope.isActive = function (viewLocation) {
        if (viewLocation.indexOf('report') >= 0)
            return $location.path().indexOf('report') >= 0;
        else
            return $location.absUrl().indexOf(viewLocation) >= 0;
    };
}]);

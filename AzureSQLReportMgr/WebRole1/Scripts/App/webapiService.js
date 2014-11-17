var webapiService = angular.module('webapiService', ['ngResource']);

webapiService.factory('ReportGenerator', ['$resource', function ($resource) {
    return $resource('/api/ReportGenerate', {}, {
        query: { method: 'GET', params: {}, isArray: true },
        update: { method: 'PUT'}
});
}])

webapiService.factory('ReportServer', ['$resource', function ($resource) {
    return {
        getLatest: $resource('/api/ReportServe/GetLatest', {}, {
            query: { method: 'GET', params: {}, isArray: true }
        }),
        getLatestByName: $resource('/api/ReportServe/GetLatest/:name', {}, {
            query: { method: 'GET', params: {name:'@name'}, isArray: true }
        }),
        getArchive: $resource('/api/ReportServe/GetArchive', {}, {
            query: { method: 'GET', params: {}, isArray: true }
        }),
        getArchiveByName: $resource('/api/ReportServe/GetArchive/:name', {}, {
            query: { method: 'GET', params: { name: '@name' }, isArray: true }
        }),
    };
}])

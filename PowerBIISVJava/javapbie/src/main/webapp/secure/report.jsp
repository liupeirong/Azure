<%@ taglib prefix="spring" uri="http://www.springframework.org/tags"%>
<!DOCTYPE html PUBLIC "-//W3C//DTD HTML 4.01 Transitional//EN" "http://www.w3.org/TR/html4/loose.dtd">
<html>
<head>
    <meta http-equiv="Content-Type" content="text/html; charset=UTF-8">
    <title>Report</title>
    <spring:url value="/scripts/powerbi.js" var="powerbijs" />
    <script src="${powerbijs}" type="text/javascript"></script>
    <script type="text/javascript">
        window.onload = function () {
            var models = window['powerbi-client'].models;
            var config = {
                type: 'report',
                tokenType: models.TokenType.Embed,
                accessToken: '${accessToken}',
                embedUrl: '${embedUrl}',
                id: '${reportId}',
                settings: {
                    filterPaneEnabled: true,
                    navContentPaneEnabled: true//,
                    //useCustomSaveAsDialog: true
                }
            };

            var reportContainer = document.getElementById('reportContainer');
            var report = powerbi.embed(reportContainer, config);
        }
    </script>
</head>
<body style="height: 1600px">
    <div id="reportContainer" style="height: 100%;width: 100%"></div>
</body>
</html>
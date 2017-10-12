// This file contains your Data Connector logic
section OAuth2DataConnector;

RedirectUrl = Json.Document ( Extension.Contents( "config.json") ) [settings][RedirectUrl];
AuthorizeUrl = Json.Document ( Extension.Contents( "config.json") ) [settings][AuthorizeUrl];
TokenUrl = Json.Document ( Extension.Contents( "config.json") ) [settings][TokenUrl];
Client_ID = Json.Document ( Extension.Contents( "config.json") ) [settings][Client_ID];
Client_Secret = Json.Document ( Extension.Contents( "config.json") ) [settings][Client_Secret];
APP_ID_URI = Json.Document ( Extension.Contents( "config.json") ) [settings][APP_ID_URI];
DataUrl = Json.Document ( Extension.Contents( "config.json") ) [settings][DataUrl];

//use shared function for testing
shared OAuth2DataConnector.test = () =>
        let
            result = AuthorizeUrl & Uri.BuildQueryString([
             client_id = Client_ID,
			 response_type = "code",
             redirect_uri = RedirectUrl
            ])
        in
            result;

[DataSource.Kind="OAuth2DataConnector", Publish="OAuth2DataConnector.Publish"]
    shared OAuth2DataConnector.Contents = () =>
        let
            response = Web.Contents (DataUrl),
            list = Json.Document(response),
            table = Table.FromList(list)
        in
            table;

OAuth2DataConnector = [
    Authentication = [
        OAuth = [
        StartLogin = StartLogin,
        FinishLogin = FinishLogin
        ]
    ],
    Label = Extension.LoadString("DataSourceLabel")
];

// Data Source UI publishing description
OAuth2DataConnector.Publish = [
    Beta = true,
    Category = "Other",
    ButtonText = { Extension.LoadString("ButtonTitle"), Extension.LoadString("ButtonHelp") },
    LearnMoreUrl = "https://powerbi.microsoft.com/",
    SourceImage = OAuth2DataConnector.Icons,
    SourceTypeImage = OAuth2DataConnector.Icons
];

OAuth2DataConnector.Icons = [
    Icon16 = { Extension.Contents("OAuth2DataConnector16.png"), Extension.Contents("OAuth2DataConnector20.png"), Extension.Contents("OAuth2DataConnector24.png"), Extension.Contents("OAuth2DataConnector32.png") },
    Icon32 = { Extension.Contents("OAuth2DataConnector32.png"), Extension.Contents("OAuth2DataConnector40.png"), Extension.Contents("OAuth2DataConnector48.png"), Extension.Contents("OAuth2DataConnector64.png") }
];


StartLogin = (resourceUrl, state, display) =>
    let
        AuthorizeUrl = AuthorizeUrl & Uri.BuildQueryString([
            client_id = Client_ID,
			response_type = "code",
            redirect_uri = RedirectUrl
            ])
    in
        [
            LoginUri = AuthorizeUrl,
            CallbackUri = RedirectUrl,
            WindowHeight = 500,
            WindowWidth = 700,
            Context = null
        ];

FinishLogin = (context, callbackUri, state) =>
    let
        Parts = Uri.Parts(callbackUri)[Query]
    in
        TokenMethod(Parts[code]);

TokenMethod = (code) =>
    let
        body = [
                client_id = Client_ID,
                client_secret = Client_Secret,
                code = code,
                grant_type= "authorization_code",
                resource = APP_ID_URI,
                redirect_uri = RedirectUrl
                ],

       Response = Web.Contents(TokenUrl,[
           Content = Text.ToBinary(Uri.BuildQueryString(body)),
           Headers=[#"Content-type" = "application/x-www-form-urlencoded"]]),

       Parts = Json.Document(Response)
    in
       Parts;
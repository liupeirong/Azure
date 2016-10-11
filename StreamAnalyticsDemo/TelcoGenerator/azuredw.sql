CREATE MASTER KEY;

CREATE DATABASE SCOPED CREDENTIAL AzureStorageCredential
WITH
    IDENTITY = 'somename',
    SECRET = 'storage key'
;

CREATE EXTERNAL DATA SOURCE TelcoStorage
WITH (
    TYPE = HADOOP,
    LOCATION = 'wasbs://container@account.blob.core.windows.net',
    CREDENTIAL = AzureStorageCredential
);

CREATE EXTERNAL FILE FORMAT TextFileFormat 
WITH 
(   FORMAT_TYPE = DELIMITEDTEXT
,   FORMAT_OPTIONS  (   FIELD_TERMINATOR = ','
                    ,   STRING_DELIMITER = ''
                    ,   USE_TYPE_DEFAULT = FALSE 
                    )
);

create schema telcodb;

CREATE EXTERNAL TABLE [telcodb].factsCalls (
    [RecordType] [nvarchar] (50) NOT NULL,
    [SystemIdentity] [nvarchar](50) NULL,
    [SwitchNum] [nvarchar](10) NULL,
    [CallingNum] [nvarchar](50) NULL,
    [CallingIMSI] [nvarchar] (50) NULL,
    [CalledNum] [nvarchar](50) NULL,
    [CalledIMSI] [nvarchar](50) NULL,
    [TimeType] [int] NULL,
    [CallPeriod] [int] NULL,
    [UnitPrice] [float] NULL,
    [ServiceType] [nvarchar](50) NULL,
    [Transfer] [int] NULL,
    [EndType] [nvarchar](50) NULL,
    [IncomingTrunk] [nvarchar](50) NULL,
    [OutgoingTrunk] [nvarchar](50) NULL,
    [MSRN] [nvarchar](50) NULL,
    [CallTime] [nvarchar](50)  NULL
)
WITH
(
    LOCATION='/hackfest/' 
,   DATA_SOURCE = TelcoStorage
,   FILE_FORMAT = TextFileFormat
,   REJECT_TYPE = VALUE
,   REJECT_VALUE = 0
)
;

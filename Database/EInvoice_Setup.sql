/* =====================================================================================================
   EInvoice_Setup.sql  -  Hóa đơn điện tử cấu hình động (port từ PMSH_Web)
   DB đích: DB của OneS_PMSH_Web (DefaultConnection)
   Viết theo kiểu chạy lại được nhiều lần (idempotent): bảng/FK/chỉ mục chỉ tạo nếu chưa có,
   SP tạo lại bằng DROP + CREATE (SQL Server 2014 chưa có CREATE OR ALTER), dữ liệu seed chỉ chèn khi ID chưa tồn tại.

   Nội dung:
     1. Bảng cấu hình hóa đơn điện tử (11 bảng)
     2. Quyền chức năng (FormAndFunction) EInvoice_Config / EInvoice_Issue / EInvoice_View, gán cho nhóm Administrator (14)
     3. Stored procedure - Giai đoạn 1 (Provider / Field / Option)
     4. Dữ liệu seed Provider / Field / Option (giữ nguyên ID từ PMSH_Web)
     5. Stored procedure - Giai đoạn 2 (Operation / Mapping / Response Rule)
     6. Dữ liệu seed Operation / Mapping / Response Rule / BodyField (giữ nguyên ID)
     7. Stored procedure - Giai đoạn 3 (Property E-Invoice: kết nối DB động + giá trị cấu hình Provider)
     8. Stored procedure - Giai đoạn 4 (nạp cấu hình runtime cho engine chạy hóa đơn)
   Giai đoạn sau (màn vận hành PMS) sẽ bổ sung tiếp vào cuối file này.
   ===================================================================================================== */
SET NOCOUNT ON;
GO

/* ------------------------------------------------------------------------------------------------
   1. BẢNG
   ------------------------------------------------------------------------------------------------ */

-- 1.1 Nhà cung cấp
IF OBJECT_ID(N'dbo.EInvoiceProvider', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EInvoiceProvider
    (
        ID          int IDENTITY(1,1) NOT NULL,
        Code        nvarchar(50)  NOT NULL,
        Name        nvarchar(150) NOT NULL,
        Description nvarchar(500) NULL,
        LogoPath    nvarchar(500) NULL,
        Inactive    bit           NOT NULL CONSTRAINT DF_EInvoiceProvider_Inactive DEFAULT (0),
        CreatedBy   nvarchar(100) NULL,
        CreatedDate datetime      NOT NULL CONSTRAINT DF_EInvoiceProvider_CreatedDate DEFAULT (GETDATE()),
        UpdatedBy   nvarchar(100) NULL,
        UpdatedDate datetime      NULL,
        CONSTRAINT PK_EInvoiceProvider PRIMARY KEY CLUSTERED (ID),
        CONSTRAINT UQ_EInvoiceProvider_Code UNIQUE (Code)
    );
END
GO

-- 1.2 Field cấu hình của nhà cung cấp (người dùng nhập theo từng Property)
IF OBJECT_ID(N'dbo.EInvoiceProviderField', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EInvoiceProviderField
    (
        ID                   int IDENTITY(1,1) NOT NULL,
        ProviderID           int           NOT NULL,
        FieldCode            nvarchar(100) NOT NULL,
        DisplayName          nvarchar(200) NOT NULL,
        Description          nvarchar(500) NULL,
        DataType             nvarchar(30)  NOT NULL,
        ControlType          nvarchar(30)  NOT NULL,
        IsRequired           bit           NOT NULL CONSTRAINT DF_EInvoiceProviderField_IsRequired DEFAULT (0),
        IsEncrypted          bit           NOT NULL CONSTRAINT DF_EInvoiceProviderField_IsEncrypted DEFAULT (0),
        IsReadOnly           bit           NOT NULL CONSTRAINT DF_EInvoiceProviderField_IsReadOnly DEFAULT (0),
        DefaultValue         nvarchar(max) NULL,
        Placeholder          nvarchar(300) NULL,
        MaxLength            int           NULL,
        RegexPattern         nvarchar(500) NULL,
        GroupName            nvarchar(150) NULL,
        DisplayOrder         int           NOT NULL CONSTRAINT DF_EInvoiceProviderField_DisplayOrder DEFAULT (0),
        ColumnWidth          int           NOT NULL CONSTRAINT DF_EInvoiceProviderField_ColumnWidth DEFAULT (6),
        Inactive             bit           NOT NULL CONSTRAINT DF_EInvoiceProviderField_Inactive DEFAULT (0),
        CreatedBy            nvarchar(100) NULL,
        CreatedDate          datetime      NOT NULL CONSTRAINT DF_EInvoiceProviderField_CreatedDate DEFAULT (GETDATE()),
        UpdatedBy            nvarchar(100) NULL,
        UpdatedDate          datetime      NULL,
        VisibleWhenFieldCode nvarchar(100) NULL,
        VisibleWhenOperator  nvarchar(20)  NULL,
        VisibleWhenValue     nvarchar(200) NULL,
        CONSTRAINT PK_EInvoiceProviderField PRIMARY KEY CLUSTERED (ID),
        CONSTRAINT UQ_EInvoiceProviderField_Provider_FieldCode UNIQUE (ProviderID, FieldCode),
        CONSTRAINT FK_EInvoiceProviderField_Provider FOREIGN KEY (ProviderID) REFERENCES dbo.EInvoiceProvider (ID)
    );
END
GO

-- 1.3 Lựa chọn của field dạng Select
IF OBJECT_ID(N'dbo.EInvoiceProviderFieldOption', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EInvoiceProviderFieldOption
    (
        ID              int IDENTITY(1,1) NOT NULL,
        ProviderFieldID int           NOT NULL,
        OptionValue     nvarchar(150) NOT NULL,
        OptionText      nvarchar(250) NOT NULL,
        IsDefault       bit           NOT NULL CONSTRAINT DF_EInvoiceProviderFieldOption_IsDefault DEFAULT (0),
        DisplayOrder    int           NOT NULL CONSTRAINT DF_EInvoiceProviderFieldOption_DisplayOrder DEFAULT (0),
        Inactive        bit           NOT NULL CONSTRAINT DF_EInvoiceProviderFieldOption_Inactive DEFAULT (0),
        CreatedBy       nvarchar(100) NULL,
        CreatedDate     datetime      NOT NULL CONSTRAINT DF_EInvoiceProviderFieldOption_CreatedDate DEFAULT (GETDATE()),
        UpdatedBy       nvarchar(100) NULL,
        UpdatedDate     datetime      NULL,
        CONSTRAINT PK_EInvoiceProviderFieldOption PRIMARY KEY CLUSTERED (ID),
        CONSTRAINT UQ_EInvoiceProviderFieldOption_Value UNIQUE (ProviderFieldID, OptionValue),
        CONSTRAINT FK_EInvoiceProviderFieldOption_Field FOREIGN KEY (ProviderFieldID) REFERENCES dbo.EInvoiceProviderField (ID)
    );
END
GO

-- 1.4 API (Operation) của nhà cung cấp
IF OBJECT_ID(N'dbo.EInvoiceProviderOperation', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EInvoiceProviderOperation
    (
        ID                       int IDENTITY(1,1) NOT NULL,
        ProviderID               int           NOT NULL,
        OperationCode            nvarchar(50)  NOT NULL,
        OperationName            nvarchar(150) NULL,
        HttpMethod               nvarchar(10)  NOT NULL CONSTRAINT DF_EInvoiceProviderOperation_HttpMethod DEFAULT ('POST'),
        EndpointPath             nvarchar(500) NOT NULL,
        ContentType              nvarchar(100) NOT NULL CONSTRAINT DF_EInvoiceProviderOperation_ContentType DEFAULT ('application/json'),
        TimeoutSeconds           int           NOT NULL CONSTRAINT DF_EInvoiceProviderOperation_Timeout DEFAULT (60),
        IsAuthenticationRequired bit           NOT NULL CONSTRAINT DF_EInvoiceProviderOperation_Auth DEFAULT (1),
        SuccessStatusCodes       nvarchar(100) NULL,
        Inactive                 bit           NOT NULL CONSTRAINT DF_EInvoiceProviderOperation_Inactive DEFAULT (0),
        CreatedBy                nvarchar(100) NULL,
        CreatedDate              datetime      NOT NULL CONSTRAINT DF_EInvoiceProviderOperation_CreatedDate DEFAULT (GETDATE()),
        UpdatedBy                nvarchar(100) NULL,
        UpdatedDate              datetime      NULL,
        BodyType                 nvarchar(50)  NULL,
        ExternalMethodCode       nvarchar(50)  NULL,
        ActionCode               nvarchar(50)  NULL,
        Description              nvarchar(500) NULL,
        DisplayOrder             int           NOT NULL CONSTRAINT DF_EInvoiceProviderOperation_DisplayOrder DEFAULT (0),
        BusinessActionCode       varchar(50)   NULL,
        AuthenticationOperationID int          NULL,
        CONSTRAINT PK_EInvoiceProviderOperation PRIMARY KEY CLUSTERED (ID),
        CONSTRAINT FK_EInvoiceProviderOperation_Provider FOREIGN KEY (ProviderID) REFERENCES dbo.EInvoiceProvider (ID),
        CONSTRAINT FK_EInvoiceProviderOperation_AuthenticationOperation FOREIGN KEY (AuthenticationOperationID) REFERENCES dbo.EInvoiceProviderOperation (ID)
    );
    CREATE UNIQUE INDEX UX_EInvoiceProviderOperation_Provider_Code ON dbo.EInvoiceProviderOperation (ProviderID, OperationCode);
END
GO

-- 1.5 Mapping tham số request/response của từng Operation
IF OBJECT_ID(N'dbo.EInvoiceProviderMapping', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EInvoiceProviderMapping
    (
        ID                    int IDENTITY(1,1) NOT NULL,
        OperationID           int            NOT NULL,
        MappingLocation       nvarchar(30)   NOT NULL,
        TargetPath            nvarchar(500)  NOT NULL,
        SourceType            nvarchar(30)   NOT NULL,
        SourceValue           nvarchar(max)  NULL,
        DataType              nvarchar(30)   NOT NULL CONSTRAINT DF_EInvoiceProviderMapping_DataType DEFAULT ('STRING'),
        IsRequired            bit            NOT NULL CONSTRAINT DF_EInvoiceProviderMapping_IsRequired DEFAULT (0),
        DefaultValue          nvarchar(max)  NULL,
        FormatString          nvarchar(100)  NULL,
        OmitWhenNull          bit            NOT NULL CONSTRAINT DF_EInvoiceProviderMapping_OmitWhenNull DEFAULT (1),
        PrefixValue           nvarchar(200)  NULL,
        SuffixValue           nvarchar(200)  NULL,
        DisplayOrder          int            NOT NULL CONSTRAINT DF_EInvoiceProviderMapping_DisplayOrder DEFAULT (0),
        Inactive              bit            NOT NULL CONSTRAINT DF_EInvoiceProviderMapping_Inactive DEFAULT (0),
        CreatedBy             nvarchar(100)  NULL,
        CreatedDate           datetime       NOT NULL CONSTRAINT DF_EInvoiceProviderMapping_CreatedDate DEFAULT (GETDATE()),
        UpdatedBy             nvarchar(100)  NULL,
        UpdatedDate           datetime       NULL,
        ConditionSourceType   nvarchar(30)   NULL,
        ConditionSourceValue  nvarchar(500)  NULL,
        ConditionOperator     nvarchar(30)   NULL,
        ConditionExpectedValue nvarchar(500) NULL,
        CONSTRAINT PK_EInvoiceProviderMapping PRIMARY KEY CLUSTERED (ID),
        CONSTRAINT FK_EInvoiceProviderMapping_Operation FOREIGN KEY (OperationID) REFERENCES dbo.EInvoiceProviderOperation (ID)
    );
    CREATE UNIQUE INDEX UX_EInvoiceProviderMapping_Operation_Location_Path ON dbo.EInvoiceProviderMapping (OperationID, MappingLocation, TargetPath);
END
GO

-- 1.6 Khai báo document/collection khi dựng body
IF OBJECT_ID(N'dbo.EInvoiceProviderBodyField', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EInvoiceProviderBodyField
    (
        ID             int IDENTITY(1,1) NOT NULL,
        ProviderID     int            NOT NULL,
        OperationCode  nvarchar(50)   NOT NULL,
        JsonPath       nvarchar(500)  NOT NULL,
        SourceType     nvarchar(30)   NOT NULL,
        SourceValue    nvarchar(500)  NULL,
        DataType       nvarchar(30)   NOT NULL CONSTRAINT DF_EInvoiceProviderBodyField_DataType DEFAULT ('STRING'),
        IsRequired     bit            NOT NULL CONSTRAINT DF_EInvoiceProviderBodyField_IsRequired DEFAULT (0),
        DefaultValue   nvarchar(max)  NULL,
        FormatString   nvarchar(100)  NULL,
        OmitWhenNull   bit            NOT NULL CONSTRAINT DF_EInvoiceProviderBodyField_OmitWhenNull DEFAULT (1),
        DisplayOrder   int            NOT NULL CONSTRAINT DF_EInvoiceProviderBodyField_DisplayOrder DEFAULT (0),
        Inactive       bit            NOT NULL CONSTRAINT DF_EInvoiceProviderBodyField_Inactive DEFAULT (0),
        CreatedBy      nvarchar(100)  NULL,
        CreatedDate    datetime       NOT NULL CONSTRAINT DF_EInvoiceProviderBodyField_CreatedDate DEFAULT (GETDATE()),
        UpdatedBy      nvarchar(100)  NULL,
        UpdatedDate    datetime       NULL,
        DocumentType   nvarchar(20)   NULL,
        RootName       nvarchar(100)  NULL,
        CollectionName nvarchar(100)  NULL,
        ItemPath       nvarchar(500)  NULL,
        CONSTRAINT PK_EInvoiceProviderBodyField PRIMARY KEY CLUSTERED (ID),
        CONSTRAINT FK_EInvoiceProviderBodyField_Provider FOREIGN KEY (ProviderID) REFERENCES dbo.EInvoiceProvider (ID)
    );
    CREATE UNIQUE INDEX UX_EInvoiceProviderBodyField_Scalar ON dbo.EInvoiceProviderBodyField (ProviderID, OperationCode, JsonPath)
        WHERE CollectionName IS NULL AND ItemPath IS NULL AND Inactive = 0;
    CREATE UNIQUE INDEX UX_EInvoiceProviderBodyField_Collection ON dbo.EInvoiceProviderBodyField (ProviderID, OperationCode, CollectionName, ItemPath, JsonPath)
        WHERE CollectionName IS NOT NULL AND ItemPath IS NOT NULL AND Inactive = 0;
END
GO

-- 1.7 Quy tắc xác định response thành công / thất bại
IF OBJECT_ID(N'dbo.EInvoiceProviderResponseRule', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EInvoiceProviderResponseRule
    (
        ID            int IDENTITY(1,1) NOT NULL,
        OperationID   int            NOT NULL,
        RuleCode      nvarchar(50)   NOT NULL,
        SourcePath    nvarchar(500)  NULL,
        Operator      nvarchar(30)   NOT NULL,
        ExpectedValue nvarchar(500)  NULL,
        ResultType    nvarchar(20)   NOT NULL,
        Priority      int            NOT NULL CONSTRAINT DF_EInvoiceProviderResponseRule_Priority DEFAULT (0),
        Inactive      bit            NOT NULL CONSTRAINT DF_EInvoiceProviderResponseRule_Inactive DEFAULT (0),
        CreatedBy     nvarchar(100)  NULL,
        CreatedDate   datetime       NOT NULL CONSTRAINT DF_EInvoiceProviderResponseRule_CreatedDate DEFAULT (GETDATE()),
        UpdatedBy     nvarchar(100)  NULL,
        UpdatedDate   datetime       NULL,
        CONSTRAINT PK_EInvoiceProviderResponseRule PRIMARY KEY CLUSTERED (ID)
    );
END
GO

-- 1.8 Nhật ký thực thi (request / response)
IF OBJECT_ID(N'dbo.EInvoiceProviderExecutionLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EInvoiceProviderExecutionLog
    (
        ID                  bigint IDENTITY(1,1) NOT NULL,
        PropertyID          int            NOT NULL,
        FolioVATID          int            NULL,
        ProviderID          int            NOT NULL,
        OperationID         int            NOT NULL,
        OperationCode       nvarchar(100)  NOT NULL,
        ProviderKey         nvarchar(100)  NULL,
        Pattern             nvarchar(50)   NULL,
        Serial              nvarchar(50)   NULL,
        InvoiceNo           nvarchar(50)   NULL,
        InvoiceDate         date           NULL,
        VerificationCode    nvarchar(200)  NULL,
        KeySearch           nvarchar(200)  NULL,
        HttpSuccess         bit            NOT NULL CONSTRAINT DF_EInvoiceProviderExecutionLog_HttpSuccess DEFAULT (0),
        HttpStatusCode      int            NULL,
        ProviderSuccess     bit            NOT NULL CONSTRAINT DF_EInvoiceProviderExecutionLog_ProviderSuccess DEFAULT (0),
        ProviderCode        int            NULL,
        RequestUrl          nvarchar(1000) NULL,
        RequestBody         nvarchar(max)  NULL,
        RawResponse         nvarchar(max)  NULL,
        ElapsedMilliseconds bigint         NULL,
        ErrorMessage        nvarchar(2000) NULL,
        CreatedBy           nvarchar(100)  NULL,
        CreatedDate         datetime2      NOT NULL CONSTRAINT DF_EInvoiceProviderExecutionLog_CreatedDate DEFAULT (SYSDATETIME()),
        SourceType          varchar(10)    NULL,
        SourceRecordID      int            NULL,
        CONSTRAINT PK_EInvoiceProviderExecutionLog PRIMARY KEY CLUSTERED (ID)
    );
    CREATE INDEX IX_EInvoiceProviderExecutionLog_Folio ON dbo.EInvoiceProviderExecutionLog (PropertyID, FolioVATID, CreatedDate);
    CREATE INDEX IX_EInvoiceExecutionLog_DuplicateCheck ON dbo.EInvoiceProviderExecutionLog (PropertyID, SourceType, SourceRecordID, ProviderID, OperationID, ProviderSuccess);
END
GO

-- 1.9 Property: nguồn dữ liệu (kết nối DB động) của từng đơn vị phát hành
IF OBJECT_ID(N'dbo.Property_Einvoice', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Property_Einvoice
    (
        ID                int IDENTITY(1,1) NOT NULL,
        Code              nvarchar(50)  NULL,
        Name              nvarchar(100) NULL,
        Description       nvarchar(100) NULL,
        Database_IP       nvarchar(50)  NULL,
        Database_Name     nvarchar(50)  NULL,
        Database_User     nvarchar(50)  NULL,
        Database_Password nvarchar(200) NULL,   -- sẽ lưu dạng đã mã hóa (Encryptor), nên rộng hơn bản gốc (50)
        Api_Name          nvarchar(50)  NULL,
        Api_link          nvarchar(150) NULL,
        Api_User          nvarchar(50)  NULL,
        Api_Password      nvarchar(50)  NULL,
        Inactive          bit           NULL,
        CreatedBy         nvarchar(50)  NULL,
        CreatedDate       datetime      NULL,
        UpdatedBy         nvarchar(50)  NULL,
        UpdatedDate       datetime      NULL,
        TaxCode           nvarchar(50)  NULL,
        InvoiceSearchSP   nvarchar(200) NULL,
        ProviderType      nvarchar(20)  NULL,
        SourceType        varchar(10)   NOT NULL CONSTRAINT DF_Property_Einvoice_SourceType DEFAULT ('PMS'),
        InvoiceDetailSP   nvarchar(200) NULL,
        CONSTRAINT PK_Property_Einvoice PRIMARY KEY CLUSTERED (ID)
    );
END
GO

-- 1.10 Property gắn với Provider
IF OBJECT_ID(N'dbo.Property_EInvoiceProvider', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Property_EInvoiceProvider
    (
        ID          int IDENTITY(1,1) NOT NULL,
        PropertyID  int           NOT NULL,
        ProviderID  int           NOT NULL,
        IsDefault   bit           NOT NULL CONSTRAINT DF_Property_EInvoiceProvider_IsDefault DEFAULT (1),
        Inactive    bit           NOT NULL CONSTRAINT DF_Property_EInvoiceProvider_Inactive DEFAULT (0),
        CreatedBy   nvarchar(100) NULL,
        CreatedDate datetime      NOT NULL CONSTRAINT DF_Property_EInvoiceProvider_CreatedDate DEFAULT (GETDATE()),
        UpdatedBy   nvarchar(100) NULL,
        UpdatedDate datetime      NULL,
        CONSTRAINT PK_Property_EInvoiceProvider PRIMARY KEY CLUSTERED (ID),
        CONSTRAINT UQ_Property_EInvoiceProvider UNIQUE (PropertyID, ProviderID),
        CONSTRAINT FK_Property_EInvoiceProvider_Property FOREIGN KEY (PropertyID) REFERENCES dbo.Property_Einvoice (ID),
        CONSTRAINT FK_Property_EInvoiceProvider_Provider FOREIGN KEY (ProviderID) REFERENCES dbo.EInvoiceProvider (ID)
    );
    CREATE INDEX IX_Property_EInvoiceProvider_PropertyID ON dbo.Property_EInvoiceProvider (PropertyID, Inactive, IsDefault);
END
GO

-- 1.11 Giá trị field cấu hình của Property
IF OBJECT_ID(N'dbo.Property_EInvoiceProviderValue', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Property_EInvoiceProviderValue
    (
        ID                 int IDENTITY(1,1) NOT NULL,
        PropertyProviderID int           NOT NULL,
        ProviderFieldID    int           NOT NULL,
        FieldValue         nvarchar(max) NULL,
        CreatedBy          nvarchar(100) NULL,
        CreatedDate        datetime      NOT NULL CONSTRAINT DF_Property_EInvoiceProviderValue_CreatedDate DEFAULT (GETDATE()),
        UpdatedBy          nvarchar(100) NULL,
        UpdatedDate        datetime      NULL,
        CONSTRAINT PK_Property_EInvoiceProviderValue PRIMARY KEY CLUSTERED (ID),
        CONSTRAINT UQ_Property_EInvoiceProviderValue UNIQUE (PropertyProviderID, ProviderFieldID),
        CONSTRAINT FK_Property_EInvoiceProviderValue_Config FOREIGN KEY (PropertyProviderID) REFERENCES dbo.Property_EInvoiceProvider (ID),
        CONSTRAINT FK_Property_EInvoiceProviderValue_Field FOREIGN KEY (ProviderFieldID) REFERENCES dbo.EInvoiceProviderField (ID)
    );
END
GO

/* ------------------------------------------------------------------------------------------------
   2. QUYỀN CHỨC NĂNG (hệ phân quyền mới của OneS: FormAndFunction + UserGroupRightDistribution)
      Nhóm chức năng 49 = Invoicing (Cashiering). Nhóm người dùng 14 = Administrator.
      EInvoice_Config : Provider Management, Property E-Invoice (cấu hình)
      EInvoice_Issue  : Upload / Publish / Issue hóa đơn
      EInvoice_View   : Chỉ xem danh sách hóa đơn và log
   ------------------------------------------------------------------------------------------------ */
DECLARE @codes TABLE (Code nvarchar(100), Name nvarchar(200), Description nvarchar(200));
INSERT INTO @codes (Code, Name, Description) VALUES
    (N'EInvoice_Config', N'E-Invoice Configuration', N'Cấu hình hóa đơn điện tử (Provider, Property)'),
    (N'EInvoice_Issue',  N'E-Invoice Issue',         N'Upload / Publish / Issue hóa đơn điện tử'),
    (N'EInvoice_View',   N'E-Invoice View',          N'Xem danh sách hóa đơn điện tử và nhật ký');

INSERT INTO dbo.FormAndFunction (Code, Name, Description, FormAndFunctionGroupID, IsHide, OrderIndex, Inactive, CreatedBy, CreatedDate)
SELECT c.Code, c.Name, c.Description, 49, 0, NULL, 0, N'EInvoice_Setup', GETDATE()
FROM @codes c
WHERE NOT EXISTS (SELECT 1 FROM dbo.FormAndFunction f WHERE f.Code = c.Code);

INSERT INTO dbo.UserGroupRightDistribution (FormAndFunctionID, UserGroupID, ViewRight, CreateRight, ModifyRight, DeleteRight, SpecialRight, CreatedBy, CreatedDate, FormAndFunctionDataID)
SELECT f.ID, 14, 1, 1, 1, 1, 1, N'EInvoice_Setup', GETDATE(), 0
FROM dbo.FormAndFunction f
INNER JOIN @codes c ON c.Code = f.Code
WHERE NOT EXISTS (SELECT 1 FROM dbo.UserGroupRightDistribution r WHERE r.FormAndFunctionID = f.ID AND r.UserGroupID = 14);
GO

/* ------------------------------------------------------------------------------------------------
   3. STORED PROCEDURE - GIAI ĐOẠN 1: Provider / Field / Option
      Lỗi nghiệp vụ dùng RAISERROR (mức 16) để service trả nguyên thông báo về giao diện.
   ------------------------------------------------------------------------------------------------ */

-- Danh sách Provider
IF OBJECT_ID(N'dbo.spEInvoice_ProviderSearch', N'P') IS NOT NULL DROP PROCEDURE dbo.spEInvoice_ProviderSearch;
GO
CREATE PROCEDURE dbo.spEInvoice_ProviderSearch
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, Code, Name, Description, LogoPath, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate
    FROM dbo.EInvoiceProvider WITH (NOLOCK)
    ORDER BY Code;
END
GO

-- Thêm (@ID = 0) hoặc sửa Provider. Trả về ID.
IF OBJECT_ID(N'dbo.spEInvoice_ProviderSave', N'P') IS NOT NULL DROP PROCEDURE dbo.spEInvoice_ProviderSave;
GO
CREATE PROCEDURE dbo.spEInvoice_ProviderSave
    @ID          int,
    @Code        nvarchar(50),
    @Name        nvarchar(150),
    @Description nvarchar(500) = NULL,
    @LogoPath    nvarchar(500) = NULL,
    @Inactive    bit,
    @User        nvarchar(100)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.EInvoiceProvider WHERE UPPER(LTRIM(RTRIM(Code))) = UPPER(LTRIM(RTRIM(@Code))) AND ID <> @ID)
    BEGIN RAISERROR (N'Provider Code already exists.', 16, 1); RETURN; END

    IF @ID > 0
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProvider WHERE ID = @ID)
    BEGIN RAISERROR (N'Provider not found.', 16, 1); RETURN; END

        UPDATE dbo.EInvoiceProvider
        SET Code = @Code, Name = @Name, Description = @Description, LogoPath = @LogoPath,
            Inactive = @Inactive, UpdatedBy = @User, UpdatedDate = GETDATE()
        WHERE ID = @ID;

        SELECT @ID AS ID;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.EInvoiceProvider (Code, Name, Description, LogoPath, Inactive, CreatedBy, CreatedDate)
        VALUES (@Code, @Name, @Description, @LogoPath, @Inactive, @User, GETDATE());

        SELECT CAST(SCOPE_IDENTITY() AS int) AS ID;
    END
END
GO

-- Danh sách Field của một Provider
IF OBJECT_ID(N'dbo.spEInvoice_FieldSearch', N'P') IS NOT NULL DROP PROCEDURE dbo.spEInvoice_FieldSearch;
GO
CREATE PROCEDURE dbo.spEInvoice_FieldSearch
    @ProviderID int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly,
           DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive,
           VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate
    FROM dbo.EInvoiceProviderField WITH (NOLOCK)
    WHERE ProviderID = @ProviderID
    ORDER BY ISNULL(GroupName, N''), DisplayOrder, ID;
END
GO

-- Thêm (@ID = 0) hoặc sửa Field. Trả về ID.
IF OBJECT_ID(N'dbo.spEInvoice_FieldSave', N'P') IS NOT NULL DROP PROCEDURE dbo.spEInvoice_FieldSave;
GO
CREATE PROCEDURE dbo.spEInvoice_FieldSave
    @ID                   int,
    @ProviderID           int,
    @FieldCode            nvarchar(100),
    @DisplayName          nvarchar(200),
    @Description          nvarchar(500) = NULL,
    @DataType             nvarchar(30),
    @ControlType          nvarchar(30),
    @IsRequired           bit,
    @IsEncrypted          bit,
    @IsReadOnly           bit,
    @DefaultValue         nvarchar(max) = NULL,
    @Placeholder          nvarchar(300) = NULL,
    @MaxLength            int = NULL,
    @RegexPattern         nvarchar(500) = NULL,
    @GroupName            nvarchar(150) = NULL,
    @DisplayOrder         int,
    @ColumnWidth          int,
    @Inactive             bit,
    @VisibleWhenFieldCode nvarchar(100) = NULL,
    @VisibleWhenOperator  nvarchar(20) = NULL,
    @VisibleWhenValue     nvarchar(200) = NULL,
    @User                 nvarchar(100)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProvider WHERE ID = @ProviderID)
    BEGIN RAISERROR (N'Provider not found.', 16, 1); RETURN; END

    IF EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField
               WHERE ProviderID = @ProviderID AND UPPER(LTRIM(RTRIM(FieldCode))) = UPPER(LTRIM(RTRIM(@FieldCode))) AND ID <> @ID)
    BEGIN RAISERROR (N'Field Code already exists for this provider.', 16, 1); RETURN; END

    IF @ID > 0
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = @ID AND ProviderID = @ProviderID)
    BEGIN RAISERROR (N'Provider field not found.', 16, 1); RETURN; END

        UPDATE dbo.EInvoiceProviderField
        SET FieldCode = @FieldCode, DisplayName = @DisplayName, Description = @Description, DataType = @DataType,
            ControlType = @ControlType, IsRequired = @IsRequired, IsEncrypted = @IsEncrypted, IsReadOnly = @IsReadOnly,
            DefaultValue = @DefaultValue, Placeholder = @Placeholder, MaxLength = @MaxLength, RegexPattern = @RegexPattern,
            GroupName = @GroupName, DisplayOrder = @DisplayOrder, ColumnWidth = @ColumnWidth, Inactive = @Inactive,
            VisibleWhenFieldCode = @VisibleWhenFieldCode, VisibleWhenOperator = @VisibleWhenOperator, VisibleWhenValue = @VisibleWhenValue,
            UpdatedBy = @User, UpdatedDate = GETDATE()
        WHERE ID = @ID;

        SELECT @ID AS ID;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.EInvoiceProviderField
            (ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly,
             DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive,
             VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue, CreatedBy, CreatedDate)
        VALUES
            (@ProviderID, @FieldCode, @DisplayName, @Description, @DataType, @ControlType, @IsRequired, @IsEncrypted, @IsReadOnly,
             @DefaultValue, @Placeholder, @MaxLength, @RegexPattern, @GroupName, @DisplayOrder, @ColumnWidth, @Inactive,
             @VisibleWhenFieldCode, @VisibleWhenOperator, @VisibleWhenValue, @User, GETDATE());

        SELECT CAST(SCOPE_IDENTITY() AS int) AS ID;
    END
END
GO

-- Danh sách Option của một Field
IF OBJECT_ID(N'dbo.spEInvoice_OptionSearch', N'P') IS NOT NULL DROP PROCEDURE dbo.spEInvoice_OptionSearch;
GO
CREATE PROCEDURE dbo.spEInvoice_OptionSearch
    @FieldID int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, ProviderFieldID, OptionValue, OptionText, IsDefault, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate
    FROM dbo.EInvoiceProviderFieldOption WITH (NOLOCK)
    WHERE ProviderFieldID = @FieldID
    ORDER BY DisplayOrder, ID;
END
GO

-- Thêm (@ID = 0) hoặc sửa Option. Trả về ID.
IF OBJECT_ID(N'dbo.spEInvoice_OptionSave', N'P') IS NOT NULL DROP PROCEDURE dbo.spEInvoice_OptionSave;
GO
CREATE PROCEDURE dbo.spEInvoice_OptionSave
    @ID             int,
    @ProviderFieldID int,
    @OptionValue    nvarchar(150),
    @OptionText     nvarchar(250),
    @IsDefault      bit,
    @DisplayOrder   int,
    @Inactive       bit,
    @User           nvarchar(100)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = @ProviderFieldID)
    BEGIN RAISERROR (N'Provider field not found.', 16, 1); RETURN; END

    IF EXISTS (SELECT 1 FROM dbo.EInvoiceProviderFieldOption
               WHERE ProviderFieldID = @ProviderFieldID AND OptionValue = @OptionValue AND ID <> @ID)
    BEGIN RAISERROR (N'Option Value already exists for this field.', 16, 1); RETURN; END

    IF @ID > 0
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderFieldOption WHERE ID = @ID AND ProviderFieldID = @ProviderFieldID)
    BEGIN RAISERROR (N'Field option not found.', 16, 1); RETURN; END

        UPDATE dbo.EInvoiceProviderFieldOption
        SET OptionValue = @OptionValue, OptionText = @OptionText, IsDefault = @IsDefault,
            DisplayOrder = @DisplayOrder, Inactive = @Inactive, UpdatedBy = @User, UpdatedDate = GETDATE()
        WHERE ID = @ID;

        SELECT @ID AS ID;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.EInvoiceProviderFieldOption
            (ProviderFieldID, OptionValue, OptionText, IsDefault, DisplayOrder, Inactive, CreatedBy, CreatedDate)
        VALUES
            (@ProviderFieldID, @OptionValue, @OptionText, @IsDefault, @DisplayOrder, @Inactive, @User, GETDATE());

        SELECT CAST(SCOPE_IDENTITY() AS int) AS ID;
    END
END
GO

/* ------------------------------------------------------------------------------------------------
   4. DỮ LIỆU SEED - Provider / Field / Option (giữ nguyên ID của PMSH_Web để các giai đoạn sau khớp khóa ngoại)
   ------------------------------------------------------------------------------------------------ */
SET IDENTITY_INSERT dbo.EInvoiceProvider ON;
GO
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProvider WHERE ID = 1)
    INSERT INTO dbo.EInvoiceProvider (ID, Code, Name, Description, LogoPath, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate) VALUES (1, N'VIETTEL', N'Viettel SInvoice', N'Nhà cung cấp hóa đơn điện tử Viettel SInvoice V2.50', NULL, 0, N'admin', '2026-07-16 16:51:57.780', N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProvider WHERE ID = 2)
    INSERT INTO dbo.EInvoiceProvider (ID, Code, Name, Description, LogoPath, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate) VALUES (2, N'FAST', N'Fast e-Invoice', N'Nhà cung cấp hóa đơn điện tử Fast e-Invoice', NULL, 0, N'admin', '2026-07-16 16:51:57.780', N'ones', '2026-07-30 14:30:30.810');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProvider WHERE ID = 4)
    INSERT INTO dbo.EInvoiceProvider (ID, Code, Name, Description, LogoPath, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate) VALUES (4, N'VNPT', N'VNPT Invoice', N'Nhà cung cấp hóa đơn điện tử VNPT theo TT78', NULL, 0, N'SQL_CONFIG_VNPT', '2026-08-10 11:59:49.743', NULL, NULL);
GO
SET IDENTITY_INSERT dbo.EInvoiceProvider OFF;
GO
SET IDENTITY_INSERT dbo.EInvoiceProviderField ON;
GO
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = 1)
    INSERT INTO dbo.EInvoiceProviderField (ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly, DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue) VALUES (1, 1, N'BASE_URL', N'API Base URL', N'Địa chỉ gốc API Viettel SInvoice do Viettel cung cấp', N'String', N'Text', 1, 0, 0, NULL, N'https://api-vinvoice.viettel.vn', 250, NULL, N'CONNECTION', 10, 12, 0, N'admin', '2026-07-16 16:52:20.610', N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = 2)
    INSERT INTO dbo.EInvoiceProviderField (ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly, DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue) VALUES (2, 1, N'AUTH_TYPE', N'Authentication Type', N'Hình thức xác thực khi gọi Viettel SInvoice', N'String', N'Select', 1, 0, 0, N'BASIC', NULL, 30, NULL, N'AUTHENTICATION', 30, 6, 0, N'admin', '2026-07-16 16:52:20.610', N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = 3)
    INSERT INTO dbo.EInvoiceProviderField (ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly, DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue) VALUES (3, 1, N'API_USERNAME', N'API Username', N'Tài khoản kết nối API Viettel', N'String', N'Text', 1, 0, 0, NULL, N'Nhập tài khoản API', NULL, NULL, N'Thông tin kết nối', 3, 6, 0, N'admin', '2026-07-16 16:52:20.610', NULL, NULL, N'AUTH_TYPE', N'EQUALS', N'BASIC');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = 4)
    INSERT INTO dbo.EInvoiceProviderField (ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly, DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue) VALUES (4, 1, N'API_PASSWORD', N'API Password', N'Mật khẩu kết nối API Viettel', N'String', N'Password', 1, 1, 0, NULL, N'Nhập mật khẩu API', NULL, NULL, N'Thông tin kết nối', 4, 6, 0, N'admin', '2026-07-16 16:52:20.610', NULL, NULL, N'AUTH_TYPE', N'EQUALS', N'BASIC');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = 5)
    INSERT INTO dbo.EInvoiceProviderField (ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly, DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue) VALUES (5, 1, N'SUPPLIER_TAX_CODE', N'Supplier Tax Code', N'Mã số thuế doanh nghiệp hoặc chi nhánh phát hành hóa đơn', N'String', N'Text', 1, 0, 0, NULL, N'Ví dụ: 0312770607 hoặc 0312770607-001', 20, N'^[0-9-]+$', N'INVOICE', 100, 6, 0, N'admin', '2026-07-16 16:52:20.610', N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = 6)
    INSERT INTO dbo.EInvoiceProviderField (ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly, DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue) VALUES (6, 1, N'TEMPLATE_CODE', N'Template Code', N'Ký hiệu mẫu hóa đơn đã đăng ký trên Viettel', N'String', N'Text', 1, 0, 0, NULL, N'Ví dụ: 1/001', 20, NULL, N'INVOICE', 110, 6, 0, N'admin', '2026-07-16 16:52:20.610', N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = 7)
    INSERT INTO dbo.EInvoiceProviderField (ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly, DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue) VALUES (7, 1, N'INVOICE_SERIES', N'Invoice Series', N'Ký hiệu hóa đơn đã đăng ký trên Viettel', N'String', N'Text', 1, 0, 0, NULL, N'Ví dụ: C26MLD', 25, N'^[a-zA-Z0-9]+$', N'INVOICE', 120, 6, 0, N'admin', '2026-07-16 16:52:20.610', N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = 8)
    INSERT INTO dbo.EInvoiceProviderField (ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly, DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue) VALUES (8, 1, N'SIGN_TYPE', N'Digital Signature Type', N'Loại chữ ký số sử dụng khi phát hành hóa đơn', N'String', N'Select', 1, 0, 0, N'HSM', NULL, 30, NULL, N'INVOICE', 140, 6, 0, N'admin', '2026-07-16 16:52:20.610', N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = 9)
    INSERT INTO dbo.EInvoiceProviderField (ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly, DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue) VALUES (9, 1, N'VALIDATION', N'Viettel Validation', N'Cho phép Viettel kiểm tra và tính toán dữ liệu hóa đơn', N'String', N'Select', 1, 0, 0, N'1', NULL, 1, N'^[01]$', N'INVOICE', 150, 6, 0, N'admin', '2026-07-16 16:52:20.610', N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = 10)
    INSERT INTO dbo.EInvoiceProviderField (ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly, DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue) VALUES (10, 2, N'BASE_URL', N'API URL', NULL, N'String', N'Text', 1, 0, 0, NULL, NULL, NULL, NULL, N'Thông tin kết nối', 1, 12, 0, N'ones', '2026-07-17 11:05:45.377', N'ones', '2026-07-30 10:55:17.623', NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = 11)
    INSERT INTO dbo.EInvoiceProviderField (ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly, DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue) VALUES (11, 2, N'SIGN_TYPE', N'Sign Type', NULL, N'String', N'Select', 1, 0, 0, NULL, NULL, NULL, NULL, N'Thông tin kết nối', 0, 6, 0, N'ones', '2026-07-17 11:52:38.730', N'ones', '2026-07-30 10:56:42.063', NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = 12)
    INSERT INTO dbo.EInvoiceProviderField (ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly, DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue) VALUES (12, 1, N'API_KEY', N'API KEY', NULL, N'String', N'Text', 0, 0, 0, NULL, NULL, NULL, NULL, N'Thông tin kết nối', 0, 6, 0, N'ones', '2026-07-20 09:54:34.423', NULL, NULL, N'AUTH_TYPE', N'EQUALS', N'API_KEY');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = 1012)
    INSERT INTO dbo.EInvoiceProviderField (ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly, DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue) VALUES (1012, 2, N'PORTAL_URL', N'Portal URL', NULL, N'String', N'Text', 0, 0, 0, NULL, NULL, NULL, NULL, N'Thông tin kết nối', 0, 6, 0, N'ones', '2026-07-30 10:56:11.410', NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = 1013)
    INSERT INTO dbo.EInvoiceProviderField (ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly, DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue) VALUES (1013, 2, N'CLIENT_CODE', N'Client Code', NULL, N'String', N'Text', 1, 0, 0, NULL, NULL, NULL, NULL, N'Thông tin kết nối', 0, 6, 0, N'ones', '2026-07-30 10:57:30.263', NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = 1014)
    INSERT INTO dbo.EInvoiceProviderField (ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly, DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue) VALUES (1014, 2, N'PROXY_CODE', N'Proxy Code', NULL, N'String', N'Text', 1, 0, 0, NULL, NULL, NULL, NULL, N'Thông tin kết nối', 0, 6, 0, N'ones', '2026-07-30 10:58:08.183', NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = 1015)
    INSERT INTO dbo.EInvoiceProviderField (ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly, DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue) VALUES (1015, 2, N'UNIT_CODE', N'Unit Code', NULL, N'String', N'Text', 1, 0, 0, NULL, NULL, NULL, NULL, N'Thông tin kết nối', 0, 6, 0, N'ones', '2026-07-30 10:58:30.573', NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = 1016)
    INSERT INTO dbo.EInvoiceProviderField (ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly, DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue) VALUES (1016, 2, N'API_USER', N'Portal User', NULL, N'String', N'Text', 1, 0, 0, NULL, NULL, NULL, NULL, N'Thông tin kết nối', 0, 6, 0, N'ones', '2026-07-30 10:58:58.507', NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = 1017)
    INSERT INTO dbo.EInvoiceProviderField (ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly, DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue) VALUES (1017, 2, N'API_PASSWORD', N'Portal Password', NULL, N'String', N'Password', 1, 1, 0, NULL, NULL, NULL, NULL, N'Thông tin kết nối', 0, 6, 0, N'ones', '2026-07-30 10:59:22.657', NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = 1018)
    INSERT INTO dbo.EInvoiceProviderField (ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly, DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue) VALUES (1018, 2, N'VOUCHER_BOOK', N'Voucher Book', NULL, N'String', N'Text', 1, 0, 0, NULL, NULL, NULL, NULL, NULL, 0, 6, 0, N'ones', '2026-07-30 10:59:54.277', NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = 1019)
    INSERT INTO dbo.EInvoiceProviderField (ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly, DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue) VALUES (1019, 2, N'TIMEOUT_SECONDS', N'Timeout Seconds', NULL, N'Integer', N'Number', 0, 0, 0, NULL, NULL, NULL, NULL, N'Thông tin kết nối', 0, 6, 0, N'ones', '2026-07-30 11:00:20.823', NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = 1020)
    INSERT INTO dbo.EInvoiceProviderField (ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly, DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue) VALUES (1020, 2, N'MAX_BATCH_SIZE', N'Max Batch Size', NULL, N'Integer', N'Number', 0, 0, 0, NULL, NULL, NULL, NULL, N'Thông tin kết nối', 0, 6, 0, N'ones', '2026-07-30 11:00:52.247', NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = 1021)
    INSERT INTO dbo.EInvoiceProviderField (ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly, DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue) VALUES (1021, 2, N'ENVIRONMENT', N'Environment', NULL, N'String', N'Select', 1, 0, 0, NULL, NULL, NULL, NULL, N'Thông tin kết nối', 0, 6, 0, N'ones', '2026-07-30 11:01:21.733', NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = 2016)
    INSERT INTO dbo.EInvoiceProviderField (ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly, DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue) VALUES (2016, 1, N'TIMEOUT_SECONDS', N'Request Timeout (Seconds)', N'Thời gian chờ khi gọi API Viettel', N'Integer', N'Number', 1, 0, 0, N'90', N'90', NULL, N'^[0-9]+$', N'CONNECTION', 20, 6, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = 2017)
    INSERT INTO dbo.EInvoiceProviderField (ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly, DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue) VALUES (2017, 1, N'USERNAME', N'Viettel Username', N'Tài khoản đăng nhập Viettel SInvoice', N'String', N'Text', 0, 0, 0, NULL, N'Tài khoản Viettel SInvoice', 150, NULL, N'AUTHENTICATION', 40, 6, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, N'AUTH_TYPE', N'IN', N'BASIC,TOKEN');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = 2018)
    INSERT INTO dbo.EInvoiceProviderField (ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly, DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue) VALUES (2018, 1, N'PASSWORD', N'Viettel Password', N'Mật khẩu đăng nhập Viettel SInvoice', N'String', N'Password', 0, 1, 0, NULL, N'Mật khẩu Viettel SInvoice', 300, NULL, N'AUTHENTICATION', 50, 6, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, N'AUTH_TYPE', N'IN', N'BASIC,TOKEN');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = 2019)
    INSERT INTO dbo.EInvoiceProviderField (ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly, DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue) VALUES (2019, 1, N'TOKEN_LOGIN_PATH', N'Token Login Path', N'Endpoint dùng để lấy access_token', N'String', N'Text', 0, 0, 1, N'/auth/login', N'/auth/login', 250, NULL, N'AUTHENTICATION', 60, 6, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, N'AUTH_TYPE', N'EQUALS', N'TOKEN');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = 2020)
    INSERT INTO dbo.EInvoiceProviderField (ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly, DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue) VALUES (2020, 1, N'APP_KID', N'APP-KID', N'Mã ứng dụng tích hợp do Viettel cấp', N'String', N'Text', 0, 0, 0, NULL, N'Mã ứng dụng tích hợp', 150, NULL, N'AUTHENTICATION', 70, 6, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, N'AUTH_TYPE', N'EQUALS', N'API_KEY');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = 2021)
    INSERT INTO dbo.EInvoiceProviderField (ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly, DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue) VALUES (2021, 1, N'X_KID', N'X-KID', N'Tài khoản người dùng trên hệ thống Viettel', N'String', N'Text', 0, 0, 0, NULL, N'Tài khoản Viettel', 150, NULL, N'AUTHENTICATION', 80, 6, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, N'AUTH_TYPE', N'EQUALS', N'API_KEY');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = 2022)
    INSERT INTO dbo.EInvoiceProviderField (ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly, DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue) VALUES (2022, 1, N'X_API_KEY', N'X-API-KEY', N'API Key do Viettel cấp cho APP-KID và X-KID', N'String', N'Password', 0, 1, 0, NULL, N'API Key Viettel', 500, NULL, N'AUTHENTICATION', 90, 12, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, N'AUTH_TYPE', N'EQUALS', N'API_KEY');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = 2023)
    INSERT INTO dbo.EInvoiceProviderField (ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly, DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue) VALUES (2023, 1, N'INVOICE_TYPE', N'Invoice Type', N'Loại hóa đơn theo quy định Viettel SInvoice', N'String', N'Select', 1, 0, 0, N'1', NULL, 10, NULL, N'INVOICE', 130, 6, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = 2024)
    INSERT INTO dbo.EInvoiceProviderField (ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly, DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue) VALUES (2024, 4, N'BASE_URL', N'API Base URL', N'Địa chỉ gốc Web Service VNPT do VNPT cung cấp', N'String', N'Text', 1, 0, 0, NULL, N'Nhập API Base URL VNPT', 500, NULL, N'CONNECTION', 10, 12, 0, N'SQL_CONFIG_VNPT', '2026-08-10 12:02:13.850', NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = 2025)
    INSERT INTO dbo.EInvoiceProviderField (ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly, DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue) VALUES (2025, 4, N'REQUEST_TIMEOUT', N'Request Timeout (Seconds)', N'Thời gian chờ khi gọi Web Service VNPT', N'Int', N'Text', 1, 0, 0, N'90', N'90', NULL, NULL, N'CONNECTION', 20, 6, 0, N'SQL_CONFIG_VNPT', '2026-08-10 12:02:13.850', NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = 2026)
    INSERT INTO dbo.EInvoiceProviderField (ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly, DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue) VALUES (2026, 4, N'USERNAME', N'VNPT Username', N'Tài khoản Web Service VNPT', N'String', N'Text', 1, 0, 0, NULL, N'Nhập tài khoản VNPT', 200, NULL, N'AUTHENTICATION', 30, 6, 0, N'SQL_CONFIG_VNPT', '2026-08-10 12:02:13.850', NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = 2027)
    INSERT INTO dbo.EInvoiceProviderField (ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly, DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue) VALUES (2027, 4, N'PASSWORD', N'VNPT Password', N'Mật khẩu Web Service VNPT', N'String', N'Password', 1, 1, 0, NULL, N'Nhập mật khẩu VNPT', 500, NULL, N'AUTHENTICATION', 40, 6, 0, N'SQL_CONFIG_VNPT', '2026-08-10 12:02:13.850', NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = 2028)
    INSERT INTO dbo.EInvoiceProviderField (ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly, DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue) VALUES (2028, 4, N'PATTERN', N'Pattern (M?u s?)', N'Mẫu số hóa đơn sử dụng khi gọi nghiệp vụ VNPT yêu cầu pattern', N'String', N'Text', 1, 0, 0, NULL, N'Nhập mẫu số hóa đơn', 100, NULL, N'INVOICE', 50, 6, 0, N'SQL_CONFIG_VNPT', '2026-08-10 12:02:13.850', NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = 2029)
    INSERT INTO dbo.EInvoiceProviderField (ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly, DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue) VALUES (2029, 4, N'SERIAL', N'Serial (Ký hi?u)', N'Ký hiệu hóa đơn sử dụng khi gọi nghiệp vụ VNPT yêu cầu serial', N'String', N'Text', 1, 0, 0, NULL, N'Nhập ký hiệu hóa đơn', 100, NULL, N'INVOICE', 60, 6, 0, N'SQL_CONFIG_VNPT', '2026-08-10 12:02:13.850', NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = 2030)
    INSERT INTO dbo.EInvoiceProviderField (ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly, DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue) VALUES (2030, 4, N'ACCOUNT', N'VNPT Account', N'Account dùng trong các Web Service VNPT', N'String', N'Text', 1, 0, 0, NULL, N'Nhập Account VNPT', 200, NULL, N'AUTHENTICATION', 35, 6, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:40:22.700', NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderField WHERE ID = 2031)
    INSERT INTO dbo.EInvoiceProviderField (ID, ProviderID, FieldCode, DisplayName, Description, DataType, ControlType, IsRequired, IsEncrypted, IsReadOnly, DefaultValue, Placeholder, MaxLength, RegexPattern, GroupName, DisplayOrder, ColumnWidth, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, VisibleWhenFieldCode, VisibleWhenOperator, VisibleWhenValue) VALUES (2031, 4, N'AC_PASSWORD', N'VNPT AC Password', N'ACpass dùng trong các Web Service VNPT', N'String', N'Password', 1, 1, 0, NULL, N'Nhập AC Password VNPT', 500, NULL, N'AUTHENTICATION', 36, 6, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:40:22.700', NULL, NULL, NULL, NULL, NULL);
GO
SET IDENTITY_INSERT dbo.EInvoiceProviderField OFF;
GO
SET IDENTITY_INSERT dbo.EInvoiceProviderFieldOption ON;
GO
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderFieldOption WHERE ID = 1)
    INSERT INTO dbo.EInvoiceProviderFieldOption (ID, ProviderFieldID, OptionValue, OptionText, IsDefault, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate) VALUES (1, 2, N'BASIC_AUTH', N'Basic Auth', 1, 1, 0, N'admin', '2026-07-16 16:52:33.290', NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderFieldOption WHERE ID = 2)
    INSERT INTO dbo.EInvoiceProviderFieldOption (ID, ProviderFieldID, OptionValue, OptionText, IsDefault, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate) VALUES (2, 2, N'TOKEN', N'Access Token', 0, 20, 0, N'admin', '2026-07-16 16:52:33.290', N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderFieldOption WHERE ID = 3)
    INSERT INTO dbo.EInvoiceProviderFieldOption (ID, ProviderFieldID, OptionValue, OptionText, IsDefault, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate) VALUES (3, 2, N'API_KEY', N'API Key', 0, 30, 0, N'admin', '2026-07-16 16:52:33.290', N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderFieldOption WHERE ID = 4)
    INSERT INTO dbo.EInvoiceProviderFieldOption (ID, ProviderFieldID, OptionValue, OptionText, IsDefault, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate) VALUES (4, 8, N'HSM', N'HSM / Server Signing', 1, 10, 0, N'admin', '2026-07-16 16:52:39.737', N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderFieldOption WHERE ID = 5)
    INSERT INTO dbo.EInvoiceProviderFieldOption (ID, ProviderFieldID, OptionValue, OptionText, IsDefault, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate) VALUES (5, 8, N'USB_TOKEN', N'USB Token', 0, 20, 0, N'admin', '2026-07-16 16:52:39.737', N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderFieldOption WHERE ID = 6)
    INSERT INTO dbo.EInvoiceProviderFieldOption (ID, ProviderFieldID, OptionValue, OptionText, IsDefault, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate) VALUES (6, 8, N'CLOUD_CA', N'Cloud CA', 0, 30, 0, N'admin', '2026-07-16 16:52:39.737', N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderFieldOption WHERE ID = 7)
    INSERT INTO dbo.EInvoiceProviderFieldOption (ID, ProviderFieldID, OptionValue, OptionText, IsDefault, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate) VALUES (7, 9, N'1', N'Viettel kiểm tra và tính toán', 1, 10, 0, N'admin', '2026-07-16 16:52:47.687', N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderFieldOption WHERE ID = 8)
    INSERT INTO dbo.EInvoiceProviderFieldOption (ID, ProviderFieldID, OptionValue, OptionText, IsDefault, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate) VALUES (8, 9, N'0', N'Giữ nguyên dữ liệu gửi lên', 0, 20, 0, N'admin', '2026-07-16 16:52:47.687', N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderFieldOption WHERE ID = 9)
    INSERT INTO dbo.EInvoiceProviderFieldOption (ID, ProviderFieldID, OptionValue, OptionText, IsDefault, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate) VALUES (9, 11, N'HSM', N'HSM - Kí só tự động', 1, 0, 0, N'ones', '2026-07-17 11:53:15.870', N'ones', '2026-07-30 11:02:06.777');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderFieldOption WHERE ID = 10)
    INSERT INTO dbo.EInvoiceProviderFieldOption (ID, ProviderFieldID, OptionValue, OptionText, IsDefault, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate) VALUES (10, 11, N'USB_Token', N'USB Token - Kí số thủ công trên Portal', 0, 0, 0, N'ones', '2026-07-17 11:58:03.227', N'ones', '2026-07-30 11:02:44.867');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderFieldOption WHERE ID = 11)
    INSERT INTO dbo.EInvoiceProviderFieldOption (ID, ProviderFieldID, OptionValue, OptionText, IsDefault, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate) VALUES (11, 2, N'BASIC', N'Basic Authentication', 1, 10, 0, NULL, '2026-07-20 09:57:51.273', N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderFieldOption WHERE ID = 1002)
    INSERT INTO dbo.EInvoiceProviderFieldOption (ID, ProviderFieldID, OptionValue, OptionText, IsDefault, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate) VALUES (1002, 1021, N'TEST', N'Test', 1, 0, 0, N'ones', '2026-07-30 11:03:22.857', NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderFieldOption WHERE ID = 1003)
    INSERT INTO dbo.EInvoiceProviderFieldOption (ID, ProviderFieldID, OptionValue, OptionText, IsDefault, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate) VALUES (1003, 1021, N'PRODUCTION', N'Production', 0, 0, 0, N'ones', '2026-07-30 11:03:39.633', NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderFieldOption WHERE ID = 2008)
    INSERT INTO dbo.EInvoiceProviderFieldOption (ID, ProviderFieldID, OptionValue, OptionText, IsDefault, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate) VALUES (2008, 2023, N'1', N'Hóa đơn giá trị gia tăng', 1, 10, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderFieldOption WHERE ID = 2009)
    INSERT INTO dbo.EInvoiceProviderFieldOption (ID, ProviderFieldID, OptionValue, OptionText, IsDefault, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate) VALUES (2009, 2023, N'2', N'Hóa đơn bán hàng', 0, 20, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderFieldOption WHERE ID = 2010)
    INSERT INTO dbo.EInvoiceProviderFieldOption (ID, ProviderFieldID, OptionValue, OptionText, IsDefault, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate) VALUES (2010, 2023, N'3', N'Hóa đơn bán tài sản công', 0, 30, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderFieldOption WHERE ID = 2011)
    INSERT INTO dbo.EInvoiceProviderFieldOption (ID, ProviderFieldID, OptionValue, OptionText, IsDefault, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate) VALUES (2011, 2023, N'4', N'Hóa đơn bán hàng dự trữ quốc gia', 0, 40, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderFieldOption WHERE ID = 2012)
    INSERT INTO dbo.EInvoiceProviderFieldOption (ID, ProviderFieldID, OptionValue, OptionText, IsDefault, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate) VALUES (2012, 2023, N'5', N'Hóa đơn điện tử khác', 0, 50, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderFieldOption WHERE ID = 2013)
    INSERT INTO dbo.EInvoiceProviderFieldOption (ID, ProviderFieldID, OptionValue, OptionText, IsDefault, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate) VALUES (2013, 2023, N'6', N'Phiếu xuất kho kiêm vận chuyển nội bộ', 0, 60, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL);
GO
SET IDENTITY_INSERT dbo.EInvoiceProviderFieldOption OFF;
GO

/* ------------------------------------------------------------------------------------------------
   5. STORED PROCEDURE - GIAI ĐOẠN 2: Operation (API) / Mapping / Response Rule
   ------------------------------------------------------------------------------------------------ */

-- Danh sách Operation của một Provider (kèm số mapping, số rule đang dùng và mã Operation xác thực)
IF OBJECT_ID(N'dbo.spEInvoice_OperationSearch', N'P') IS NOT NULL DROP PROCEDURE dbo.spEInvoice_OperationSearch;
GO
CREATE PROCEDURE dbo.spEInvoice_OperationSearch
    @ProviderID int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT o.ID, o.ProviderID, o.OperationCode, o.OperationName, o.BusinessActionCode, o.HttpMethod, o.EndpointPath,
           o.ContentType, o.BodyType, o.TimeoutSeconds, o.IsAuthenticationRequired, o.AuthenticationOperationID,
           a.OperationCode AS AuthOperationCode, o.ExternalMethodCode, o.ActionCode, o.SuccessStatusCodes,
           o.Description, o.DisplayOrder, o.Inactive, o.CreatedBy, o.CreatedDate, o.UpdatedBy, o.UpdatedDate,
           (SELECT COUNT(*) FROM dbo.EInvoiceProviderMapping m WITH (NOLOCK) WHERE m.OperationID = o.ID AND m.Inactive = 0) AS MappingCount,
           (SELECT COUNT(*) FROM dbo.EInvoiceProviderResponseRule r WITH (NOLOCK) WHERE r.OperationID = o.ID AND r.Inactive = 0) AS RuleCount
    FROM dbo.EInvoiceProviderOperation o WITH (NOLOCK)
    LEFT JOIN dbo.EInvoiceProviderOperation a WITH (NOLOCK) ON a.ID = o.AuthenticationOperationID
    WHERE o.ProviderID = @ProviderID
    ORDER BY o.DisplayOrder, o.ID;
END
GO

-- Thêm (@ID = 0) hoặc sửa Operation. Trả về ID.
IF OBJECT_ID(N'dbo.spEInvoice_OperationSave', N'P') IS NOT NULL DROP PROCEDURE dbo.spEInvoice_OperationSave;
GO
CREATE PROCEDURE dbo.spEInvoice_OperationSave
    @ID                        int,
    @ProviderID                int,
    @OperationCode             nvarchar(50),
    @OperationName             nvarchar(150),
    @BusinessActionCode        varchar(50) = NULL,
    @HttpMethod                nvarchar(10),
    @EndpointPath              nvarchar(500),
    @ContentType               nvarchar(100),
    @BodyType                  nvarchar(50) = NULL,
    @TimeoutSeconds            int,
    @IsAuthenticationRequired  bit,
    @AuthenticationOperationID int = NULL,
    @ExternalMethodCode        nvarchar(50) = NULL,
    @ActionCode                nvarchar(50) = NULL,
    @SuccessStatusCodes        nvarchar(100) = NULL,
    @Description               nvarchar(500) = NULL,
    @DisplayOrder              int,
    @Inactive                  bit,
    @User                      nvarchar(100)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProvider WHERE ID = @ProviderID)
    BEGIN RAISERROR (N'Provider not found.', 16, 1); RETURN; END

    IF EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation
               WHERE ProviderID = @ProviderID AND UPPER(LTRIM(RTRIM(OperationCode))) = UPPER(LTRIM(RTRIM(@OperationCode))) AND ID <> @ID)
    BEGIN RAISERROR (N'Operation Code already exists for this provider.', 16, 1); RETURN; END

    -- Operation xác thực phải cùng Provider và không được là chính nó
    IF @AuthenticationOperationID IS NOT NULL
    BEGIN
        IF @AuthenticationOperationID = @ID
        BEGIN RAISERROR (N'Authentication Operation must not be the operation itself.', 16, 1); RETURN; END

        IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = @AuthenticationOperationID AND ProviderID = @ProviderID)
        BEGIN RAISERROR (N'Authentication Operation must belong to the same provider.', 16, 1); RETURN; END
    END

    IF @ID > 0
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = @ID AND ProviderID = @ProviderID)
        BEGIN RAISERROR (N'Operation not found.', 16, 1); RETURN; END

        UPDATE dbo.EInvoiceProviderOperation
        SET OperationCode = @OperationCode, OperationName = @OperationName, BusinessActionCode = @BusinessActionCode,
            HttpMethod = @HttpMethod, EndpointPath = @EndpointPath, ContentType = @ContentType, BodyType = @BodyType,
            TimeoutSeconds = @TimeoutSeconds, IsAuthenticationRequired = @IsAuthenticationRequired,
            AuthenticationOperationID = @AuthenticationOperationID, ExternalMethodCode = @ExternalMethodCode,
            ActionCode = @ActionCode, SuccessStatusCodes = @SuccessStatusCodes, Description = @Description,
            DisplayOrder = @DisplayOrder, Inactive = @Inactive, UpdatedBy = @User, UpdatedDate = GETDATE()
        WHERE ID = @ID;

        SELECT @ID AS ID;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.EInvoiceProviderOperation
            (ProviderID, OperationCode, OperationName, BusinessActionCode, HttpMethod, EndpointPath, ContentType, BodyType,
             TimeoutSeconds, IsAuthenticationRequired, AuthenticationOperationID, ExternalMethodCode, ActionCode,
             SuccessStatusCodes, Description, DisplayOrder, Inactive, CreatedBy, CreatedDate)
        VALUES
            (@ProviderID, @OperationCode, @OperationName, @BusinessActionCode, @HttpMethod, @EndpointPath, @ContentType, @BodyType,
             @TimeoutSeconds, @IsAuthenticationRequired, @AuthenticationOperationID, @ExternalMethodCode, @ActionCode,
             @SuccessStatusCodes, @Description, @DisplayOrder, @Inactive, @User, GETDATE());

        SELECT CAST(SCOPE_IDENTITY() AS int) AS ID;
    END
END
GO

-- Danh sách Mapping của một Operation
IF OBJECT_ID(N'dbo.spEInvoice_MappingSearch', N'P') IS NOT NULL DROP PROCEDURE dbo.spEInvoice_MappingSearch;
GO
CREATE PROCEDURE dbo.spEInvoice_MappingSearch
    @OperationID int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue,
           FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive,
           ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue,
           CreatedBy, CreatedDate, UpdatedBy, UpdatedDate
    FROM dbo.EInvoiceProviderMapping WITH (NOLOCK)
    WHERE OperationID = @OperationID
    ORDER BY MappingLocation, DisplayOrder, ID;
END
GO

-- Thêm (@ID = 0) hoặc sửa Mapping. Trả về ID.
IF OBJECT_ID(N'dbo.spEInvoice_MappingSave', N'P') IS NOT NULL DROP PROCEDURE dbo.spEInvoice_MappingSave;
GO
CREATE PROCEDURE dbo.spEInvoice_MappingSave
    @ID                     int,
    @OperationID            int,
    @MappingLocation        nvarchar(30),
    @TargetPath             nvarchar(500),
    @SourceType             nvarchar(30),
    @SourceValue            nvarchar(max) = NULL,
    @DataType               nvarchar(30),
    @IsRequired             bit,
    @DefaultValue           nvarchar(max) = NULL,
    @FormatString           nvarchar(100) = NULL,
    @OmitWhenNull           bit,
    @PrefixValue            nvarchar(200) = NULL,
    @SuffixValue            nvarchar(200) = NULL,
    @DisplayOrder           int,
    @Inactive               bit,
    @ConditionSourceType    nvarchar(30) = NULL,
    @ConditionSourceValue   nvarchar(500) = NULL,
    @ConditionOperator      nvarchar(30) = NULL,
    @ConditionExpectedValue nvarchar(500) = NULL,
    @User                   nvarchar(100)
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = @OperationID)
    BEGIN RAISERROR (N'Provider operation does not exist.', 16, 1); RETURN; END

    -- Khóa duy nhất: Operation + Location + Target Path (không phân biệt hoa thường)
    IF EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping
               WHERE OperationID = @OperationID
                 AND UPPER(LTRIM(RTRIM(MappingLocation))) = UPPER(LTRIM(RTRIM(@MappingLocation)))
                 AND UPPER(LTRIM(RTRIM(TargetPath))) = UPPER(LTRIM(RTRIM(@TargetPath)))
                 AND ID <> @ID)
    BEGIN RAISERROR (N'Target Path already exists in this operation and location.', 16, 1); RETURN; END

    IF @ID > 0
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = @ID AND OperationID = @OperationID)
        BEGIN RAISERROR (N'Mapping not found.', 16, 1); RETURN; END

        UPDATE dbo.EInvoiceProviderMapping
        SET MappingLocation = @MappingLocation, TargetPath = @TargetPath, SourceType = @SourceType, SourceValue = @SourceValue,
            DataType = @DataType, IsRequired = @IsRequired, DefaultValue = @DefaultValue, FormatString = @FormatString,
            OmitWhenNull = @OmitWhenNull, PrefixValue = @PrefixValue, SuffixValue = @SuffixValue, DisplayOrder = @DisplayOrder,
            Inactive = @Inactive, ConditionSourceType = @ConditionSourceType, ConditionSourceValue = @ConditionSourceValue,
            ConditionOperator = @ConditionOperator, ConditionExpectedValue = @ConditionExpectedValue,
            UpdatedBy = @User, UpdatedDate = GETDATE()
        WHERE ID = @ID;

        SELECT @ID AS ID;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.EInvoiceProviderMapping
            (OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString,
             OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive,
             ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue, CreatedBy, CreatedDate)
        VALUES
            (@OperationID, @MappingLocation, @TargetPath, @SourceType, @SourceValue, @DataType, @IsRequired, @DefaultValue, @FormatString,
             @OmitWhenNull, @PrefixValue, @SuffixValue, @DisplayOrder, @Inactive,
             @ConditionSourceType, @ConditionSourceValue, @ConditionOperator, @ConditionExpectedValue, @User, GETDATE());

        SELECT CAST(SCOPE_IDENTITY() AS int) AS ID;
    END
END
GO

-- Danh sách Response Rule của một Operation (chỉ xem; PMSH_Web cũng không có màn sửa)
IF OBJECT_ID(N'dbo.spEInvoice_RuleSearch', N'P') IS NOT NULL DROP PROCEDURE dbo.spEInvoice_RuleSearch;
GO
CREATE PROCEDURE dbo.spEInvoice_RuleSearch
    @OperationID int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, OperationID, RuleCode, SourcePath, Operator, ExpectedValue, ResultType, Priority, Inactive
    FROM dbo.EInvoiceProviderResponseRule WITH (NOLOCK)
    WHERE OperationID = @OperationID
    ORDER BY Priority, ID;
END
GO

/* ------------------------------------------------------------------------------------------------
   6. DỮ LIỆU SEED - GIAI ĐOẠN 2: Operation / Mapping / Response Rule / BodyField (giữ nguyên ID)
      Operation chèn với AuthenticationOperationID = NULL rồi cập nhật sau (khóa ngoại tự tham chiếu).
   ------------------------------------------------------------------------------------------------ */
SET IDENTITY_INSERT dbo.EInvoiceProviderOperation ON;
GO
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1, 1, N'CREATE_INVOICE', N'Lập và phát hành hóa đơn Viettel', N'POST', N'/services/einvoiceapplication/api/InvoiceAPI/InvoiceWS/createInvoice/{supplierTaxCode}', N'application/json', 90, 1, N'200,201', 0, N'ones', '2026-07-20 16:03:27.917', N'FIX_VIETTEL_ENDPOINT', '2026-08-06 15:51:01.583', N'JSON', NULL, NULL, N'Lập và phát hành hóa đơn Viettel trong một request', 20, N'ISSUE_INVOICE', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 2)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (2, 1, N'TEST_CANCEL_INVOICE', N'Test hủy hóa đơn đã sửa', N'POST', N'/invoice/test-cancel-v2', N'application/json', 90, 1, N'200,201,202', 1, N'ones', '2026-07-27 14:38:31.217', N'ones', '2026-07-27 15:55:32.647', N'JSON', N'7330', N'0', N'Dữ liệu đã được cập nhật', 98, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1002)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1002, 2, N'FAST_RESERVE_INVOICE', N'Cấp số và mã CQT', N'POST', N'/api/Command/ExecuteCommand', N'text/plain', 120, 1, N'200', 0, N'ones', '2026-07-30 11:09:26.387', NULL, NULL, N'JSON', N'7100', NULL, N'Cấp trước số hóa đơn, mẫu số, ký hiệu, mã cơ quan thuế và mã tra cứu cho hóa đơn máy tính tiền trước khi phát hành. FAST method 7100, action 0.', 1, N'UPLOAD_INVOICE', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1003)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1003, 2, N'FAST_CHECK_RESERVE_STATUS', N'Kiểm tra trạng thái cấp số', N'POST', N'/api/Command/ExecuteCommand', N'text/plain', 120, 1, N'200', 0, N'ones', '2026-07-30 11:10:46.380', NULL, NULL, N'JSON', N'7360', NULL, N'Kiểm tra hóa đơn đã được FAST cấp số, mẫu số, ký hiệu, mã CQT và mã tra cứu hay chưa. FAST method 7360, action 0.', 2, N'GET_UPLOAD_STATUS', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1004)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1004, 2, N'FAST_PREVIEW_INVOICE', N'Xem trước hóa đơn', N'POST', N'/api/Command/ExecuteCommand', N'text/plain', 120, 1, N'200', 0, N'ones', '2026-07-30 11:13:26.430', NULL, NULL, N'JSON', N'7310', N'600', N'Xem trước bản thể PDF của hóa đơn gốc trước khi phát hành. FAST action 600, method 7310.', 3, N'PREVIEW_INVOICE', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1005)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1005, 2, N'FAST_PUBLISH_INVOICE', N'Phát hành hóa đơn', N'POST', N'/api/Command/ExecuteCommand', N'text/plain', 120, 1, N'200', 0, N'ones', '2026-07-30 11:16:11.957', NULL, NULL, N'JSON', N'7310', NULL, N'Phát hành hóa đơn gốc đã được FAST cấp trước số hóa đơn và mã cơ quan thuế. FAST action 0, method 7310.', 4, N'PUBLISH_INVOICE', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1006)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1006, 2, N'FAST_CHECK_PUBLISH_STATUS', N'Kiểm tra trạng thái phát hành', N'POST', N'/api/Command/ExecuteCommand', N'text/plain', 120, 1, N'200', 0, N'ones', '2026-07-30 11:18:25.000', NULL, NULL, N'JSON', N'7370', NULL, N'Kiểm tra trạng thái phát hành hóa đơn trên hệ thống FAST e-Invoice. FAST method 7370, action 0.', 5, N'GET_PUBLISH_STATUS', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1007)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1007, 2, N'FAST_DOWNLOAD_PDF', N'Tải PDF hóa đơn', N'POST', N'/api/Command/ExecuteCommand', N'text/plain', 120, 1, N'200', 0, N'ones', '2026-07-30 11:20:31.723', N'ones', '2026-07-30 11:39:02.230', N'JSON', N'7380', NULL, N'Tải bản thể hiện PDF của hóa đơn đã phát hành và được cơ quan thuế chấp nhận. FAST method 7380, action 0.', 6, N'DOWNLOAD_INVOICE_PDF', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1008)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1008, 2, N'FAST_ADJUST_INVOICE', N'Phát hành hóa đơn điều chỉnh', N'POST', N'/api/Command/ExecuteCommand', N'text/plain', 60, 1, N'200,201', 0, N'ones', '2026-07-30 11:22:11.277', NULL, NULL, N'JSON', N'7320', NULL, N'Phát hành hóa đơn điều chỉnh cho hóa đơn gốc. Hóa đơn điều chỉnh phải được cấp số trước. FAST method 7320, action 0.', 7, N'ADJUST_INVOICE', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1009)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1009, 2, N'FAST_PREVIEW_ADJUST_INVOICE', N'Xem trước hóa đơn điều chỉnh', N'POST', N'/api/Command/ExecuteCommand', N'text/plain', 120, 1, N'200', 0, N'ones', '2026-07-30 11:24:12.853', NULL, NULL, N'JSON', N'7320', N'600', N'Xem trước PDF hóa đơn điều chỉnh trước khi phát hành. FAST action 600, method 7320.', 8, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1010)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1010, 2, N'FAST_REPLACE_INVOICE', N'Phát hành hóa đơn thay thế', N'POST', N'/api/Command/ExecuteCommand', N'text/plain', 120, 1, N'200', 0, N'ones', '2026-07-30 11:25:56.990', NULL, NULL, N'JSON', N'7350', NULL, N'Phát hành hóa đơn thay thế cho hóa đơn gốc. Hóa đơn thay thế phải được cấp số trước. FAST method 7350, action 0.', 9, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1011)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1011, 2, N'FAST_PREVIEW_REPLACE_INVOICE', N'Xem trước hóa đơn thay thế', N'POST', N'/api/Command/ExecuteCommand', N'text/plain', 120, 1, N'200', 0, N'ones', '2026-07-30 11:27:43.360', NULL, NULL, N'JSON', N'7350', N'600', N'Xem trước PDF hóa đơn thay thế trước khi phát hành. FAST action 600, method 7350.', 10, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1012)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1012, 2, N'FAST_CANCEL_INVOICE', N'Hủy nội bộ hóa đơn', N'POST', N'/api/Command/ExecuteCommand', N'text/plain', 120, 1, N'200', 0, N'ones', '2026-07-30 11:30:11.657', NULL, NULL, N'JSON', N'7330', NULL, N'Hủy nội bộ dữ liệu hóa đơn trên FAST trong trường hợp hóa đơn gửi cơ quan thuế bị từ chối hoặc sai dữ liệu. FAST method 7330, action 0.', 11, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1013)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1013, 2, N'FAST_PUSH_RAW_INVOICE', N'Đẩy dữ liệu hóa đơn thô', N'POST', N'/api/Command/ExecuteCommand', N'text/plain', 120, 1, N'200', 0, N'ones', '2026-07-30 11:33:29.163', NULL, NULL, N'JSON', N'7311', NULL, N'Đẩy dữ liệu hóa đơn thô lên portal FAST ở trạng thái chờ xác thực. FAST method 7311, action 0.', 12, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1014)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1014, 2, N'FAST_DOWNLOAD_DRAFT_PDF', N'Xem PDF hóa đơn nháp', N'POST', N'/api/Command/ExecuteCommand', N'text/plain', 120, 1, N'200', 0, N'ones', '2026-07-30 11:35:19.960', NULL, NULL, N'JSON', N'7381', NULL, N'Xem PDF hóa đơn nháp được đẩy lên bằng API dữ liệu thô. FAST method 7381, action 0.', 13, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1015)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1015, 2, N'FAST_CHECK_DRAFT_STATUS', N'Truy vấn trạng thái hóa đơn nháp', N'POST', N'/api/Command/ExecuteCommand', N'text/plain', 120, 1, N'200', 0, N'ones', '2026-07-30 11:37:26.103', NULL, NULL, N'JSON', N'7371', NULL, N'Truy vấn thông tin phát hành của hóa đơn nháp được đẩy lên bằng API dữ liệu thô. FAST method 7371, action 0.', 14, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1016)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1016, 1, N'AUTH_LOGIN', N'Đăng nhập lấy Access Token', N'POST', N'/auth/login', N'application/json', 90, 0, N'200', 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, N'JSON', NULL, NULL, N'Đăng nhập Viettel SInvoice và lấy access_token', 10, N'AUTH_LOGIN', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1017)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1017, 1, N'SEARCH_BY_TRANSACTION_UUID', N'Tra cứu hóa đơn theo transactionUuid', N'POST', N'/InvoiceAPI/InvoiceWS/searchInvoiceByTransactionUuid', N'application/x-www-form-urlencoded', 90, 1, N'200', 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, N'FORM_URLENCODED', NULL, NULL, N'Tra cứu và đối soát hóa đơn bằng transactionUuid', 30, N'SEARCH_INVOICE', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1018)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1018, 1, N'GET_INVOICE_FILE', N'Lấy file hóa đơn', N'POST', N'/InvoiceAPI/InvoiceUtilsWS/getInvoiceRepresentationFile', N'application/json', 90, 1, N'200', 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, N'JSON', NULL, NULL, N'Lấy file PDF hoặc ZIP của hóa đơn đã phát hành', 40, N'GET_INVOICE_FILE', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1019)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1019, 1, N'SEND_INVOICE_EMAIL', N'Gửi email hóa đơn cho khách hàng', N'POST', N'/InvoiceAPI/InvoiceUtilsWS/sendEmailToCustomer', N'application/json', 90, 1, N'200', 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, N'JSON', NULL, NULL, N'Gửi hóa đơn đến email khách hàng', 50, N'SEND_INVOICE_EMAIL', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1020)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1020, 1, N'CANCEL_INVOICE_LEGACY', N'Hủy hóa đơn - API cũ', N'POST', N'/InvoiceAPI/InvoiceWS/cancelTransactionInvoice', N'application/x-www-form-urlencoded', 90, 1, N'200', 1, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, N'FORM_URLENCODED', NULL, NULL, N'API hủy hóa đơn cũ, không dùng cho luồng nghiệp vụ mới', 90, N'CANCEL_INVOICE', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1021)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1021, 1, N'CREATE_INVOICE_POS', N'Lập và phát hành hóa đơn Viettel - POS', N'POST', N'/services/einvoiceapplication/api/InvoiceAPI/InvoiceWS/createInvoice/{supplierTaxCode}', N'application/json', 90, 1, NULL, 0, NULL, '2026-08-08 09:20:58.847', NULL, NULL, NULL, NULL, NULL, NULL, 0, N'ISSUE_INVOICE', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1022)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1022, 4, N'VNPT_IMPORT_INV', N'Tạo mới hóa đơn - ImportInv', N'POST', N'/PublishService.asmx', N'text/xml', 90, 1, NULL, 0, N'SQL_CONFIG_VNPT', '2026-08-10 12:17:20.927', N'CONFIG_VNPT_SOAP', '2026-08-10 15:44:14.770', N'SOAP11', NULL, N'ImportInv', N'VNPT PublishService - Tạo mới hóa đơn từ XML', 10, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1023)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1023, 4, N'VNPT_IMPORT_INV_BY_PATTERN', N'Tạo mới hóa đơn theo mẫu số, ký hiệu - ImportInvByPattern', N'POST', N'/PublishService.asmx', N'text/xml', 180, 1, NULL, 1, N'SQL_CONFIG_VNPT', '2026-08-10 12:17:20.927', N'CONFIG_VNPT_ACTION', '2026-08-10 16:23:12.407', N'SOAP11', NULL, N'ImportInvByPattern', N'VNPT PublishService - Tạo hóa đơn theo pattern/serial', 20, N'UPLOAD_INVOICE', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1024)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1024, 4, N'VNPT_IMPORT_AND_PUBLISH_INV', N'Tạo và phát hành hóa đơn - ImportAndPublishInv', N'POST', N'/PublishService.asmx', N'text/xml', 90, 1, NULL, 0, N'SQL_CONFIG_VNPT', '2026-08-10 12:17:20.927', N'CONFIG', '2026-08-11 17:08:55.290', N'SOAP11', N'http://tempuri.org/', N'ImportAndPublishInvMTT', N'VNPT PublishService - Tạo mới và phát hành hóa đơn', 30, N'ISSUE_INVOICE', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1025)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1025, 4, N'VNPT_IMPORT_AND_PUBLISH_ASSIGNED_NO', N'Phát hành hóa đơn theo số truyền vào - ImportAndPublishAssignedNo', N'POST', N'/PublishService.asmx', N'text/xml', 90, 1, NULL, 0, N'SQL_CONFIG_VNPT', '2026-08-10 12:17:20.927', N'CONFIG_VNPT_SOAP', '2026-08-10 15:44:14.770', N'SOAP11', NULL, N'ImportAndPublishAssignedNo', N'VNPT PublishService - Phát hành hóa đơn với số hóa đơn truyền vào', 40, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1026)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1026, 4, N'VNPT_PUBLISH_INV_FKEY', N'Phát hành hóa đơn theo Fkey - PublishInvFkey', N'POST', N'/PublishService.asmx', N'text/xml', 90, 1, NULL, 1, N'SQL_CONFIG_VNPT', '2026-08-10 12:17:20.927', N'CONFIG_VNPT_ACTION', '2026-08-10 16:23:12.410', N'SOAP11', NULL, N'PublishInvFkey', N'VNPT PublishService - Phát hành hóa đơn theo danh sách fkey', 50, N'PUBLISH_INVOICE', NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1027)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1027, 4, N'VNPT_GET_INV_DATA_BY_FKEY', N'Lấy dữ liệu hóa đơn theo Fkey - GetInvDataByFkey', N'POST', N'/PublishService.asmx', N'text/xml', 90, 1, NULL, 0, N'SQL_CONFIG_VNPT', '2026-08-10 12:17:20.927', N'CONFIG_VNPT_SOAP', '2026-08-10 15:44:14.770', N'SOAP11', NULL, N'GetInvDataByFkey', N'VNPT PublishService - Lấy XMLData/trạng thái hóa đơn theo fkey', 60, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1028)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1028, 4, N'VNPT_ADJUST_ACTION_ASSIGNED_NO', N'Điều chỉnh hóa đơn theo số - AdjustActionAssignedNo', N'POST', N'/BusinessService.asmx', N'text/xml', 90, 1, NULL, 0, N'SQL_CONFIG_VNPT', '2026-08-10 12:17:20.927', N'CONFIG_VNPT_SOAP', '2026-08-10 15:44:14.770', N'SOAP11', NULL, N'AdjustActionAssignedNo', N'VNPT BusinessService - Điều chỉnh hóa đơn cho phép truyền số hóa đơn', 100, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1029)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1029, 4, N'VNPT_ADJUST_INVOICE_NO_PUBLISH', N'Điều chỉnh không phát sinh hóa đơn mới - AdjustInvoiceNoPublish', N'POST', N'/BusinessService.asmx', N'text/xml', 90, 1, NULL, 0, N'SQL_CONFIG_VNPT', '2026-08-10 12:17:20.927', N'CONFIG_VNPT_SOAP', '2026-08-10 15:44:14.770', N'SOAP11', NULL, N'AdjustInvoiceNoPublish', N'VNPT BusinessService - Điều chỉnh không phát sinh hóa đơn mới', 110, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1030)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1030, 4, N'VNPT_ADJUST_INVOICE_ACTION', N'Điều chỉnh hóa đơn - AdjustInvoiceAction', N'POST', N'/BusinessService.asmx', N'text/xml', 90, 1, NULL, 0, N'SQL_CONFIG_VNPT', '2026-08-10 12:17:20.927', N'CONFIG_VNPT_SOAP', '2026-08-10 15:44:14.770', N'SOAP11', NULL, N'AdjustInvoiceAction', N'VNPT BusinessService - Điều chỉnh theo fkey, pattern, serial', 120, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1031)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1031, 4, N'VNPT_ADJUST_WITHOUT_INV', N'Điều chỉnh hóa đơn cũ - AdjustWithoutInv', N'POST', N'/BusinessService.asmx', N'text/xml', 90, 1, NULL, 0, N'SQL_CONFIG_VNPT', '2026-08-10 12:17:20.927', N'CONFIG_VNPT_SOAP', '2026-08-10 15:44:14.770', N'SOAP11', NULL, N'AdjustWithoutInv', N'VNPT BusinessService - Điều chỉnh hóa đơn gốc không tồn tại trên hệ thống', 130, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1032)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1032, 4, N'VNPT_REPLACE_ACTION_ASSIGNED_NO', N'Thay thế hóa đơn theo số - ReplaceActionAssignedNo', N'POST', N'/BusinessService.asmx', N'text/xml', 90, 1, NULL, 0, N'SQL_CONFIG_VNPT', '2026-08-10 12:17:20.927', N'CONFIG_VNPT_SOAP', '2026-08-10 15:44:14.770', N'SOAP11', NULL, N'ReplaceActionAssignedNo', N'VNPT BusinessService - Thay thế hóa đơn cho phép truyền số hóa đơn', 140, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1033)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1033, 4, N'VNPT_REPLACE_INVOICE_NO_PUBLISH', N'Thay thế không phát sinh hóa đơn mới - ReplaceInvoiceNoPublish', N'POST', N'/BusinessService.asmx', N'text/xml', 90, 1, NULL, 0, N'SQL_CONFIG_VNPT', '2026-08-10 12:17:20.930', N'CONFIG_VNPT_SOAP', '2026-08-10 15:44:14.770', N'SOAP11', NULL, N'ReplaceInvoiceNoPublish', N'VNPT BusinessService - Thay thế không phát sinh hóa đơn mới', 150, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1034)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1034, 4, N'VNPT_REPLACE_ASSIGNED_NO_NEW_PATTERN', N'Thay thế giữ số khác mẫu số - ReplaceAssignedNoNewPattern', N'POST', N'/BusinessService.asmx', N'text/xml', 90, 1, NULL, 0, N'SQL_CONFIG_VNPT', '2026-08-10 12:17:20.930', N'CONFIG_VNPT_SOAP', '2026-08-10 15:44:14.770', N'SOAP11', NULL, N'ReplaceAssignedNoNewPattern', N'VNPT BusinessService - Thay thế giữ số khác mẫu số', 160, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1035)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1035, 4, N'VNPT_REPLACE_INVOICE_ACTION', N'Thay thế hóa đơn - ReplaceInvoiceAction', N'POST', N'/BusinessService.asmx', N'text/xml', 90, 1, NULL, 0, N'SQL_CONFIG_VNPT', '2026-08-10 12:17:20.930', N'CONFIG_VNPT_SOAP', '2026-08-10 15:44:14.770', N'SOAP11', NULL, N'ReplaceInvoiceAction', N'VNPT BusinessService - Thay thế theo fkey, pattern, serial', 170, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1036)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1036, 4, N'VNPT_REPLACE_WITHOUT_INV', N'Thay thế hóa đơn cũ - ReplaceWithoutInv', N'POST', N'/BusinessService.asmx', N'text/xml', 90, 1, NULL, 0, N'SQL_CONFIG_VNPT', '2026-08-10 12:17:20.930', N'CONFIG_VNPT_SOAP', '2026-08-10 15:44:14.770', N'SOAP11', NULL, N'ReplaceWithoutInv', N'VNPT BusinessService - Thay thế hóa đơn gốc không tồn tại trên hệ thống', 180, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1037)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1037, 4, N'VNPT_GET_INV_VIEW', N'Xem hóa đơn theo Token - getInvView', N'POST', N'/PortalService.asmx', N'text/xml', 90, 1, NULL, 0, N'SQL_CONFIG_VNPT', '2026-08-10 12:17:20.930', N'CONFIG_VNPT_SOAP', '2026-08-10 15:44:14.770', N'SOAP11', NULL, N'getInvView', N'VNPT PortalService - Trả về HTML hóa đơn theo token', 200, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1038)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1038, 4, N'VNPT_GET_INV_VIEW_NO_PAY', N'Xem hóa đơn không kiểm tra thanh toán - getInvViewNoPay', N'POST', N'/PortalService.asmx', N'text/xml', 90, 1, NULL, 0, N'SQL_CONFIG_VNPT', '2026-08-10 12:17:20.930', N'CONFIG_VNPT_SOAP', '2026-08-10 15:44:14.770', N'SOAP11', NULL, N'getInvViewNoPay', N'VNPT PortalService - Xem hóa đơn theo token không kiểm tra trạng thái thanh toán', 210, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1039)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1039, 4, N'VNPT_GET_INV_VIEW_FKEY', N'Xem hóa đơn theo Fkey - getInvViewFkey', N'POST', N'/PortalService.asmx', N'text/xml', 90, 1, NULL, 0, N'SQL_CONFIG_VNPT', '2026-08-10 12:17:20.930', N'CONFIG_VNPT_SOAP', '2026-08-10 15:44:14.770', N'SOAP11', NULL, N'getInvViewFkey', N'VNPT PortalService - Trả về HTML hóa đơn theo fkey', 220, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1040)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1040, 4, N'VNPT_GET_NEW_INV_VIEW_FKEY', N'Xem hóa đơn mới theo Fkey - getNewInvViewFkey', N'POST', N'/PortalService.asmx', N'text/xml', 90, 1, NULL, 0, N'SQL_CONFIG_VNPT', '2026-08-10 12:17:20.930', N'CONFIG_VNPT_SOAP', '2026-08-10 15:44:14.770', N'SOAP11', NULL, N'getNewInvViewFkey', N'VNPT PortalService - Xem hóa đơn mới tạo theo fkey', 230, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderOperation WHERE ID = 1041)
    INSERT INTO dbo.EInvoiceProviderOperation (ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath, ContentType, TimeoutSeconds, IsAuthenticationRequired, SuccessStatusCodes, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, BodyType, ExternalMethodCode, ActionCode, Description, DisplayOrder, BusinessActionCode, AuthenticationOperationID) VALUES (1041, 4, N'VNPT_GET_INV_VIEW_FKEY_NO_PAY', N'Xem hóa đơn theo Fkey không kiểm tra thanh toán - getInvViewFkeyNoPay', N'POST', N'/PortalService.asmx', N'text/xml', 90, 1, NULL, 0, N'SQL_CONFIG_VNPT', '2026-08-10 12:17:20.930', N'CONFIG_VNPT_SOAP', '2026-08-10 15:44:14.770', N'SOAP11', NULL, N'getInvViewFkeyNoPay', N'VNPT PortalService - Xem hóa đơn theo fkey không kiểm tra trạng thái thanh toán', 240, NULL, NULL);
GO
SET IDENTITY_INSERT dbo.EInvoiceProviderOperation OFF;
GO
UPDATE dbo.EInvoiceProviderOperation SET AuthenticationOperationID = 1016 WHERE ID = 1 AND AuthenticationOperationID IS NULL;
UPDATE dbo.EInvoiceProviderOperation SET AuthenticationOperationID = 1016 WHERE ID = 1021 AND AuthenticationOperationID IS NULL;
GO
SET IDENTITY_INSERT dbo.EInvoiceProviderMapping ON;
GO
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 1)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (1, 1, N'BODY', N'supplierTaxCode', N'CONFIG', N'SUPPLIER_TAX_CODE', N'STRING', 1, NULL, NULL, 1, NULL, NULL, 10, 1, NULL, '2026-07-20 14:48:55.300', N'CLEAN_VIETTEL_MAPPING', '2026-08-06 14:15:28.997', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2, 1, N'BODY', N'templateCode', N'CONFIG', N'TEMPLATE_CODE', N'STRING', 1, NULL, NULL, 1, NULL, NULL, 20, 1, NULL, '2026-07-20 14:48:55.300', N'CLEAN_VIETTEL_MAPPING', '2026-08-06 14:15:28.997', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3, 1, N'BODY', N'invoiceSeries', N'CONFIG', N'INVOICE_SERIES', N'STRING', 1, NULL, NULL, 1, NULL, NULL, 30, 1, NULL, '2026-07-20 14:48:55.300', N'CLEAN_VIETTEL_MAPPING', '2026-08-06 14:15:28.997', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 4)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (4, 1, N'BODY', N'invoiceDate', N'INVOICE', N'InvoiceDate', N'DATE', 1, NULL, N'yyyy-MM-dd', 1, NULL, NULL, 40, 1, NULL, '2026-07-20 14:48:55.300', N'CLEAN_VIETTEL_MAPPING', '2026-08-06 14:15:28.997', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 5)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (5, 1, N'BODY', N'currencyCode', N'INVOICE', N'CurrencyCode', N'STRING', 0, N'VND', NULL, 1, NULL, NULL, 50, 1, NULL, '2026-07-20 14:48:55.300', N'CLEAN_VIETTEL_MAPPING', '2026-08-06 14:15:28.997', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 6)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (6, 1, N'BODY', N'paymentMethod', N'INVOICE', N'PaymentMethod', N'STRING', 0, N'TM/CK', NULL, 1, NULL, NULL, 60, 1, NULL, '2026-07-20 14:48:55.300', N'CLEAN_VIETTEL_MAPPING', '2026-08-06 14:15:28.997', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 7)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (7, 1, N'BODY', N'buyer.name', N'INVOICE', N'CustomerName', N'STRING', 1, NULL, NULL, 1, NULL, NULL, 70, 1, NULL, '2026-07-20 14:48:55.300', N'CLEAN_VIETTEL_MAPPING', '2026-08-06 14:15:28.997', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 8)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (8, 1, N'BODY', N'buyer.companyName', N'INVOICE', N'CustomerCompany', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 80, 1, NULL, '2026-07-20 14:48:55.300', N'CLEAN_VIETTEL_MAPPING', '2026-08-06 14:15:28.997', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 9)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (9, 1, N'BODY', N'buyer.taxCode', N'INVOICE', N'CustomerTaxCode', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 90, 1, NULL, '2026-07-20 14:48:55.300', N'CLEAN_VIETTEL_MAPPING', '2026-08-06 14:15:28.997', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 10)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (10, 1, N'BODY', N'buyer.address', N'INVOICE', N'CustomerAddress', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 100, 1, NULL, '2026-07-20 14:48:55.300', N'CLEAN_VIETTEL_MAPPING', '2026-08-06 14:15:28.997', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 11)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (11, 1, N'BODY', N'buyer.email', N'INVOICE', N'CustomerEmail', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 110, 1, NULL, '2026-07-20 14:48:55.300', N'CLEAN_VIETTEL_MAPPING', '2026-08-06 14:15:28.997', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 12)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (12, 1, N'BODY', N'total.amountBeforeTax', N'INVOICE', N'AmountBeforeTax', N'DECIMAL', 1, NULL, NULL, 1, NULL, NULL, 120, 1, NULL, '2026-07-20 14:48:55.300', N'CLEAN_VIETTEL_MAPPING', '2026-08-06 14:15:28.997', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 13)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (13, 1, N'BODY', N'total.taxAmount', N'INVOICE', N'TaxAmount', N'DECIMAL', 1, NULL, NULL, 1, NULL, NULL, 130, 1, NULL, '2026-07-20 14:48:55.300', N'CLEAN_VIETTEL_MAPPING', '2026-08-06 14:15:28.997', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 14)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (14, 1, N'BODY', N'total.totalAmount', N'INVOICE', N'TotalAmount', N'DECIMAL', 1, NULL, NULL, 1, NULL, NULL, 140, 1, NULL, '2026-07-20 14:48:55.300', N'CLEAN_VIETTEL_MAPPING', '2026-08-06 14:15:28.997', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 15)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (15, 1, N'BODY', N'requestId', N'SYSTEM', N'GUID', N'STRING', 1, NULL, NULL, 1, NULL, NULL, 150, 1, NULL, '2026-07-20 14:48:55.300', N'CLEAN_VIETTEL_MAPPING', '2026-08-06 14:15:28.997', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 16)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (16, 1, N'HEADER', N'Authorization', N'CONFIG', N'API_KEY', N'STRING', 1, NULL, NULL, 1, N'Bearer ', NULL, 10, 1, NULL, '2026-07-20 16:39:33.430', N'CLEAN_VIETTEL_MAPPING', '2026-08-06 14:15:28.993', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 17)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (17, 1, N'HEADER', N'Accept', N'STATIC', N'application/json', N'STRING', 1, NULL, NULL, 1, NULL, NULL, 20, 0, NULL, '2026-07-20 16:39:33.430', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 18)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (18, 1, N'QUERY', N'taxCode', N'CONFIG', N'SUPPLIER_TAX_CODE', N'STRING', 1, NULL, NULL, 1, NULL, NULL, 10, 1, NULL, '2026-07-20 16:57:00.537', N'CLEAN_VIETTEL_MAPPING', '2026-08-06 14:15:29.000', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 1016)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (1016, 1, N'RESPONSE', N'InvoiceNo', N'RESPONSE', N'result.invoiceNo', N'STRING', 1, NULL, NULL, 1, NULL, NULL, 10, 0, NULL, '2026-07-27 10:51:50.343', N'ENABLE_DYNAMIC_RESPONSE', '2026-08-07 14:13:31.287', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 1017)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (1017, 1, N'RESPONSE', N'TransactionID', N'RESPONSE', N'result.transactionID', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 20, 0, NULL, '2026-07-27 10:51:50.343', N'ENABLE_DYNAMIC_RESPONSE', '2026-08-07 14:13:31.287', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 1018)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (1018, 1, N'RESPONSE', N'ReservationCode', N'RESPONSE', N'result.reservationCode', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 30, 0, NULL, '2026-07-27 10:51:50.343', N'ENABLE_DYNAMIC_RESPONSE', '2026-08-07 14:13:31.287', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 1019)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (1019, 1, N'RESPONSE', N'VerificationCode', N'RESPONSE', N'result.codeOfTax', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 40, 0, NULL, '2026-07-27 10:51:50.343', N'ENABLE_DYNAMIC_RESPONSE', '2026-08-07 14:13:31.287', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 1020)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (1020, 1, N'RESPONSE', N'ErrorCode', N'RESPONSE', N'errorCode', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 50, 0, NULL, '2026-07-27 10:51:50.343', N'ENABLE_DYNAMIC_RESPONSE', '2026-08-07 14:13:31.287', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 1021)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (1021, 1, N'RESPONSE', N'ErrorMessage', N'RESPONSE', N'description', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 60, 0, NULL, '2026-07-27 10:51:50.343', N'ENABLE_DYNAMIC_RESPONSE', '2026-08-07 14:13:31.287', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 1022)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (1022, 1, N'BODY', N'items[].lineNumber', N'COLLECTION', N'Items.Sequence', N'INTEGER', 1, NULL, NULL, 1, NULL, NULL, 200, 1, NULL, '2026-07-27 11:29:09.340', N'ones', '2026-07-28 15:40:23.223', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 1023)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (1023, 1, N'BODY', N'items[].itemCode', N'COLLECTION', N'Items.ItemCode', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 210, 1, NULL, '2026-07-27 11:29:09.340', N'ones', '2026-07-28 15:40:23.223', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 1024)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (1024, 1, N'BODY', N'items[].itemName', N'COLLECTION', N'Items.ItemName', N'STRING', 1, NULL, NULL, 1, NULL, NULL, 220, 1, NULL, '2026-07-27 11:29:09.340', N'ones', '2026-07-28 15:40:23.223', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 1025)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (1025, 1, N'BODY', N'items[].unitName', N'COLLECTION', N'Items.UnitName', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 230, 1, NULL, '2026-07-27 11:29:09.340', N'ones', '2026-07-28 15:40:23.223', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 1026)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (1026, 1, N'BODY', N'items[].quantity', N'COLLECTION', N'Items.Quantity', N'DECIMAL', 1, NULL, NULL, 1, NULL, NULL, 240, 1, NULL, '2026-07-27 11:29:09.340', N'ones', '2026-07-28 15:40:23.223', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 1027)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (1027, 1, N'BODY', N'items[].unitPrice', N'COLLECTION', N'Items.UnitPrice', N'DECIMAL', 1, NULL, NULL, 1, NULL, NULL, 250, 1, NULL, '2026-07-27 11:29:09.340', N'ones', '2026-07-28 15:40:23.223', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 1028)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (1028, 1, N'BODY', N'items[].amountBeforeTax', N'COLLECTION', N'Items.AmountBeforeTax', N'DECIMAL', 1, NULL, NULL, 1, NULL, NULL, 260, 1, NULL, '2026-07-27 11:29:09.340', N'ones', '2026-07-28 15:40:23.223', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 1029)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (1029, 1, N'BODY', N'items[].taxRate', N'COLLECTION', N'Items.TaxRate', N'DECIMAL', 1, NULL, NULL, 1, NULL, NULL, 270, 1, NULL, '2026-07-27 11:29:09.340', N'ones', '2026-07-28 15:40:23.223', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 1030)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (1030, 1, N'BODY', N'items[].taxAmount', N'COLLECTION', N'Items.TaxAmount', N'DECIMAL', 1, NULL, NULL, 1, NULL, NULL, 280, 1, NULL, '2026-07-27 11:29:09.340', N'ones', '2026-07-28 15:40:23.223', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 1031)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (1031, 1, N'BODY', N'items[].totalAmount', N'COLLECTION', N'Items.TotalAmount', N'DECIMAL', 1, NULL, NULL, 1, NULL, NULL, 290, 1, NULL, '2026-07-27 11:29:09.340', N'ones', '2026-07-28 15:40:23.223', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 1032)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (1032, 1, N'BODY', N'testClientField', N'CONSTANT', N'TEST_CLIENT', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 999, 1, N'ones', '2026-07-28 09:22:04.457', N'CLEAN_VIETTEL_MAPPING', '2026-08-06 14:14:00.103', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 1033)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (1033, 1, N'BODY', N'testUiField', N'CONSTANT', N'TEST_UI', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 998, 1, N'ones', '2026-07-28 10:34:24.743', N'CLEAN_VIETTEL_MAPPING', '2026-08-06 14:14:00.103', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 1034)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (1034, 1, N'BODY', N'items', N'INVOICEDETAIL', NULL, N'ARRAY', 1, NULL, NULL, 0, NULL, NULL, 123, 1, N'ones', '2026-07-28 15:33:09.110', N'CONFIG_VIETTEL_BODY_V2', '2026-08-06 17:23:56.810', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2016)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2016, 1002, N'HEADER', N'user', N'CONFIG', N'API_USER', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 1, 0, N'ones', '2026-07-30 11:59:21.547', N'ones', '2026-07-30 12:00:49.440', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2017)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2017, 1003, N'HEADER', N'user', N'CONFIG', N'API_USER', N'STRING', 0, NULL, NULL, 0, NULL, NULL, 1, 0, N'ones', '2026-07-30 11:59:52.143', N'ones', '2026-07-30 12:00:54.523', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2018)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2018, 1004, N'HEADER', N'user', N'CONFIG', N'API_USER', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 1, 0, N'ones', '2026-07-30 12:00:24.847', N'ones', '2026-07-30 12:00:59.220', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2019)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2019, 1005, N'HEADER', N'user', N'CONFIG', N'API_USER', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 1, 0, N'ones', '2026-07-30 12:01:19.397', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2020)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2020, 1006, N'HEADER', N'user', N'CONFIG', N'API_USER', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 1, 0, N'ones', '2026-07-30 12:01:41.063', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2021)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2021, 1007, N'HEADER', N'user', N'CONFIG', N'API_USER', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 1, 0, N'ones', '2026-07-30 12:01:59.340', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2022)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2022, 1008, N'HEADER', N'user', N'CONFIG', N'API_USER', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 1, 0, N'ones', '2026-07-30 12:02:26.013', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2023)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2023, 1009, N'HEADER', N'user', N'CONFIG', N'API_USER', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 1, 0, N'ones', '2026-07-30 12:02:49.547', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2024)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2024, 1010, N'HEADER', N'user', N'CONFIG', N'API_USER', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 1, 0, N'ones', '2026-07-30 12:03:05.587', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2025)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2025, 1011, N'HEADER', N'user', N'CONFIG', N'API_USER', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 1, 0, N'ones', '2026-07-30 12:03:28.140', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2026)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2026, 1012, N'HEADER', N'user', N'CONFIG', N'API_USER', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 1, 0, N'ones', '2026-07-30 12:03:48.420', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2027)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2027, 1013, N'HEADER', N'user', N'CONFIG', N'API_USER', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 1, 0, N'ones', '2026-07-30 12:04:15.640', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2028)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2028, 1014, N'HEADER', N'user', N'CONFIG', N'API_USER', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 1, 0, N'ones', '2026-07-30 12:04:35.453', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2029)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2029, 1015, N'HEADER', N'user', N'CONFIG', N'API_USER', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 1, 0, N'ones', '2026-07-30 12:04:48.620', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2030)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2030, 1002, N'HEADER', N'unitcode', N'CONFIG', N'UNIT_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 2, 0, N'ones', '2026-07-30 12:06:58.110', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2031)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2031, 1003, N'HEADER', N'unitcode', N'CONFIG', N'UNIT_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 2, 0, N'ones', '2026-07-30 12:07:14.867', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2032)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2032, 1004, N'HEADER', N'unitcode', N'CONFIG', N'UNIT_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 2, 0, N'ones', '2026-07-30 12:07:30.070', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2033)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2033, 1005, N'HEADER', N'unitcode', N'CONFIG', N'UNIT_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 2, 0, N'ones', '2026-07-30 12:07:44.517', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2034)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2034, 1006, N'HEADER', N'unitcode', N'CONFIG', N'UNIT_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 2, 0, N'ones', '2026-07-30 12:08:02.847', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2035)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2035, 1007, N'HEADER', N'unitcode', N'CONFIG', N'UNIT_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 2, 0, N'ones', '2026-07-30 12:08:18.610', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2036)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2036, 1008, N'HEADER', N'unitcode', N'CONFIG', N'UNIT_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 2, 0, N'ones', '2026-07-30 12:08:35.790', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2037)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2037, 1009, N'HEADER', N'unitcode', N'CONFIG', N'UNIT_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 2, 0, N'ones', '2026-07-30 12:08:52.007', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2038)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2038, 1010, N'HEADER', N'unitcode', N'CONFIG', N'UNIT_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 2, 0, N'ones', '2026-07-30 12:09:06.483', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2039)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2039, 1011, N'HEADER', N'unitcode', N'CONFIG', N'UNIT_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 2, 0, N'ones', '2026-07-30 12:09:42.717', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2040)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2040, 1012, N'HEADER', N'unitcode', N'CONFIG', N'UNIT_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 2, 0, N'ones', '2026-07-30 12:10:03.757', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2041)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2041, 1013, N'HEADER', N'unitcode', N'CONFIG', N'UNIT_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 2, 0, N'ones', '2026-07-30 12:10:23.540', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2042)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2042, 1014, N'HEADER', N'unitcode', N'CONFIG', N'UNIT_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 2, 0, N'ones', '2026-07-30 12:10:39.860', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2043)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2043, 1015, N'HEADER', N'unitcode', N'CONFIG', N'UNIT_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 2, 0, N'ones', '2026-07-30 12:11:05.683', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2044)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2044, 1002, N'HEADER', N'checkSum', N'CUSTOM', N'${CONFIG.API_PASSWORD|md5|appendpath:CONFIG.CLIENT_CODE|md5}', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 3, 0, N'ones', '2026-07-30 12:12:35.547', N'ones', '2026-07-31 10:16:32.033', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2045)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2045, 1003, N'HEADER', N'checkSum', N'CUSTOM', N'FAST_CHECKSUM', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 3, 0, N'ones', '2026-07-30 12:12:48.067', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2046)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2046, 1004, N'HEADER', N'checkSum', N'CUSTOM', N'FAST_CHECKSUM', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 3, 0, N'ones', '2026-07-30 12:13:06.740', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2047)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2047, 1005, N'HEADER', N'checkSum', N'CUSTOM', N'${CONFIG.API_PASSWORD|md5|appendpath:CONFIG.CLIENT_CODE|md5}', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 3, 0, N'ones', '2026-07-30 12:13:25.027', N'ones', '2026-08-01 11:19:43.720', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2048)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2048, 1006, N'HEADER', N'checkSum', N'CUSTOM', N'FAST_CHECKSUM', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 3, 0, N'ones', '2026-07-30 12:13:41.447', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2049)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2049, 1007, N'HEADER', N'checkSum', N'CUSTOM', N'FAST_CHECKSUM', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 3, 0, N'ones', '2026-07-30 12:14:01.360', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2050)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2050, 1008, N'HEADER', N'checkSum', N'CUSTOM', N'FAST_CHECKSUM', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 3, 0, N'ones', '2026-07-30 12:14:17.347', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2051)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2051, 1009, N'HEADER', N'checkSum', N'CUSTOM', N'FAST_CHECKSUM', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 3, 0, N'ones', '2026-07-30 12:14:38.157', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2052)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2052, 1010, N'HEADER', N'checkSum', N'CUSTOM', N'FAST_CHECKSUM', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 3, 0, N'ones', '2026-07-30 12:15:03.900', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2053)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2053, 1011, N'HEADER', N'checkSum', N'CUSTOM', N'FAST_CHECKSUM', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 3, 0, N'ones', '2026-07-30 12:15:44.597', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2054)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2054, 1012, N'HEADER', N'checkSum', N'CUSTOM', N'FAST_CHECKSUM', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 3, 0, N'ones', '2026-07-30 12:15:59.350', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2055)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2055, 1013, N'HEADER', N'checkSum', N'CUSTOM', N'FAST_CHECKSUM', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 3, 0, N'ones', '2026-07-30 12:16:19.637', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2056)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2056, 1014, N'HEADER', N'checkSum', N'CUSTOM', N'FAST_CHECKSUM', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 3, 0, N'ones', '2026-07-30 12:16:40.190', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2057)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2057, 1015, N'HEADER', N'checkSum', N'CUSTOM', N'FAST_CHECKSUM', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 3, 0, N'ones', '2026-07-30 12:16:57.320', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2058)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2058, 1002, N'QUERY', N'action', N'CONSTANT', N'0', N'INT', 1, NULL, NULL, 0, NULL, NULL, 4, 0, N'ones', '2026-07-30 12:25:48.530', N'ones', '2026-07-30 12:26:42.180', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2059)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2059, 1003, N'QUERY', N'action', N'CONSTANT', N'0', N'INT', 1, NULL, NULL, 0, NULL, NULL, 4, 0, N'ones', '2026-07-30 12:26:33.827', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2060)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2060, 1004, N'QUERY', N'action', N'CONSTANT', N'600', N'INT', 1, NULL, NULL, 0, NULL, NULL, 4, 0, N'ones', '2026-07-30 12:27:02.263', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2061)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2061, 1005, N'QUERY', N'action', N'CONSTANT', N'0', N'INT', 1, NULL, NULL, 0, NULL, NULL, 4, 0, N'ones', '2026-07-30 12:27:22.830', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2062)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2062, 1006, N'QUERY', N'action', N'CONSTANT', N'0', N'INT', 1, NULL, NULL, 0, NULL, NULL, 4, 0, N'ones', '2026-07-30 12:27:44.877', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2063)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2063, 1007, N'QUERY', N'action', N'CONSTANT', N'0', N'INT', 1, NULL, NULL, 0, NULL, NULL, 4, 0, N'ones', '2026-07-30 12:28:04.940', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2064)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2064, 1008, N'QUERY', N'action', N'CONSTANT', N'0', N'INT', 1, NULL, NULL, 0, NULL, NULL, 4, 0, N'ones', '2026-07-30 12:28:30.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2065)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2065, 1009, N'QUERY', N'action', N'CONSTANT', N'600', N'INT', 1, NULL, NULL, 0, NULL, NULL, 4, 0, N'ones', '2026-07-30 12:28:50.530', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2066)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2066, 1010, N'QUERY', N'action', N'CONSTANT', N'0', N'INT', 1, NULL, NULL, 0, NULL, NULL, 4, 0, N'ones', '2026-07-30 12:29:20.243', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2067)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2067, 1011, N'QUERY', N'action', N'CONSTANT', N'600', N'INT', 1, NULL, NULL, 0, NULL, NULL, 4, 0, N'ones', '2026-07-30 12:29:41.390', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2068)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2068, 1012, N'QUERY', N'action', N'CONSTANT', N'0', N'INT', 1, NULL, NULL, 0, NULL, NULL, 4, 0, N'ones', '2026-07-30 12:29:59.297', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2069)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2069, 1013, N'QUERY', N'action', N'CONSTANT', N'0', N'INT', 1, NULL, NULL, 0, NULL, NULL, 4, 0, N'ones', '2026-07-30 12:30:18.157', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2070)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2070, 1014, N'QUERY', N'action', N'CONSTANT', N'0', N'INT', 1, NULL, NULL, 0, NULL, NULL, 4, 0, N'ones', '2026-07-30 12:30:43.923', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2071)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2071, 1015, N'QUERY', N'action', N'CONSTANT', N'0', N'INT', 1, NULL, NULL, 0, NULL, NULL, 4, 0, N'ones', '2026-07-30 12:31:04.530', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2072)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2072, 1002, N'QUERY', N'method', N'CONSTANT', N'7100', N'INT', 1, NULL, NULL, 0, NULL, NULL, 5, 0, N'ones', '2026-07-30 12:32:06.627', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2073)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2073, 1003, N'QUERY', N'method', N'CONSTANT', N'7360', N'INT', 1, NULL, NULL, 0, NULL, NULL, 5, 0, N'ones', '2026-07-30 12:32:41.587', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2074)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2074, 1004, N'QUERY', N'method', N'CONSTANT', N'7310', N'INT', 1, NULL, NULL, 0, NULL, NULL, 5, 0, N'ones', '2026-07-30 12:33:07.493', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2075)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2075, 1005, N'QUERY', N'method', N'CONSTANT', N'7310', N'INT', 1, NULL, NULL, 0, NULL, NULL, 5, 0, N'ones', '2026-07-30 12:33:31.930', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2076)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2076, 1006, N'QUERY', N'method', N'CONSTANT', N'7370', N'INT', 1, NULL, NULL, 0, NULL, NULL, 5, 0, N'ones', '2026-07-30 12:34:00.347', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2077)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2077, 1007, N'QUERY', N'method', N'CONSTANT', N'7380', N'INT', 1, NULL, NULL, 0, NULL, NULL, 5, 0, N'ones', '2026-07-30 13:48:32.613', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2078)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2078, 1008, N'QUERY', N'method', N'CONSTANT', N'7320', N'INT', 0, NULL, NULL, 1, NULL, NULL, 5, 0, N'ones', '2026-07-30 13:48:54.650', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2079)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2079, 1009, N'QUERY', N'method', N'CONSTANT', N'7320', N'INT', 1, NULL, NULL, 0, NULL, NULL, 5, 0, N'ones', '2026-07-30 13:49:16.130', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2080)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2080, 1010, N'QUERY', N'method', N'CONSTANT', N'7350', N'INT', 1, NULL, NULL, 0, NULL, NULL, 5, 0, N'ones', '2026-07-30 13:49:41.417', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2081)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2081, 1011, N'QUERY', N'method', N'CONSTANT', N'7350', N'INT', 1, NULL, NULL, 0, NULL, NULL, 5, 0, N'ones', '2026-07-30 13:50:13.500', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2082)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2082, 1012, N'QUERY', N'method', N'CONSTANT', N'7330', N'INT', 1, NULL, NULL, 0, NULL, NULL, 5, 0, N'ones', '2026-07-30 13:50:38.407', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2083)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2083, 1013, N'QUERY', N'method', N'CONSTANT', N'7311', N'INT', 1, NULL, NULL, 0, NULL, NULL, 5, 0, N'ones', '2026-07-30 13:51:13.280', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2084)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2084, 1014, N'QUERY', N'method', N'CONSTANT', N'7381', N'INT', 1, NULL, NULL, 0, NULL, NULL, 5, 0, N'ones', '2026-07-30 13:51:35.637', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2085)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2085, 1015, N'QUERY', N'method', N'CONSTANT', N'7371', N'INT', 1, NULL, NULL, 0, NULL, NULL, 5, 0, N'ones', '2026-07-30 13:51:57.560', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2086)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2086, 1002, N'QUERY', N'clientCode', N'CONFIG', N'CLIENT_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 6, 0, N'ones', '2026-07-30 13:53:17.560', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2087)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2087, 1003, N'QUERY', N'clientCode', N'CONFIG', N'CLIENT_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 6, 0, N'ones', '2026-07-30 13:53:34.973', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2088)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2088, 1004, N'QUERY', N'clientCode', N'CONFIG', N'CLIENT_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 6, 0, N'ones', '2026-07-30 13:53:51.307', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2089)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2089, 1005, N'QUERY', N'clientCode', N'CONFIG', N'CLIENT_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 6, 0, N'ones', '2026-07-30 13:54:10.017', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2090)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2090, 1006, N'QUERY', N'clientCode', N'CONFIG', N'CLIENT_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 6, 0, N'ones', '2026-07-30 13:54:28.353', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2091)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2091, 1008, N'QUERY', N'clientCode', N'CONFIG', N'CLIENT_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 6, 0, N'ones', '2026-07-30 13:54:48.120', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2092)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2092, 1009, N'QUERY', N'clientCode', N'CONFIG', N'CLIENT_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 6, 0, N'ones', '2026-07-30 13:55:03.090', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2093)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2093, 1010, N'QUERY', N'clientCode', N'CONFIG', N'CLIENT_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 6, 0, N'ones', '2026-07-30 13:55:19.400', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2094)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2094, 1011, N'QUERY', N'clientCode', N'CONFIG', N'CLIENT_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 6, 0, N'ones', '2026-07-30 13:55:42.210', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2095)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2095, 1012, N'QUERY', N'clientCode', N'CONFIG', N'CLIENT_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 6, 0, N'ones', '2026-07-30 13:56:01.333', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2096)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2096, 1013, N'QUERY', N'clientCode', N'CONFIG', N'CLIENT_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 6, 0, N'ones', '2026-07-30 13:56:19.860', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2097)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2097, 1014, N'QUERY', N'clientCode', N'CONFIG', N'CLIENT_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 6, 0, N'ones', '2026-07-30 13:56:42.573', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2098)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2098, 1015, N'QUERY', N'clientCode', N'CONFIG', N'CLIENT_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 6, 0, N'ones', '2026-07-30 13:56:58.370', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2099)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2099, 1007, N'QUERY', N'clientCode', N'CONFIG', N'CLIENT_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 6, 0, N'ones', '2026-07-30 13:57:26.927', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2100)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2100, 1002, N'QUERY', N'proxyCode', N'CONFIG', N'PROXY_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 7, 0, N'ones', '2026-07-30 13:58:14.890', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2101)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2101, 1003, N'QUERY', N'proxyCode', N'CONFIG', N'PROXY_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 7, 0, N'ones', '2026-07-30 13:58:29.020', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2102)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2102, 1004, N'QUERY', N'proxyCode', N'CONFIG', N'PROXY_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 7, 0, N'ones', '2026-07-30 13:58:44.403', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2103)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2103, 1005, N'QUERY', N'proxyCode', N'CONFIG', N'PROXY_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 7, 0, N'ones', '2026-07-30 13:59:02.320', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2104)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2104, 1006, N'QUERY', N'proxyCode', N'CONFIG', N'PROXY_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 7, 0, N'ones', '2026-07-30 13:59:18.767', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2105)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2105, 1007, N'QUERY', N'proxyCode', N'CONFIG', N'p', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 7, 0, N'ones', '2026-07-30 13:59:42.463', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2106)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2106, 1008, N'QUERY', N'proxyCode', N'CONFIG', N'PROXY_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 7, 0, N'ones', '2026-07-30 14:00:00.250', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2107)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2107, 1009, N'QUERY', N'proxyCode', N'CONFIG', N'PROXY_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 7, 0, N'ones', '2026-07-30 14:00:15.513', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2108)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2108, 1010, N'QUERY', N'proxyCode', N'CONFIG', N'PROXY_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 7, 0, N'ones', '2026-07-30 14:00:37.460', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2109)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2109, 1011, N'QUERY', N'proxyCode', N'CONFIG', N'PROXY_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 7, 0, N'ones', '2026-07-30 14:00:57.060', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2110)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2110, 1012, N'QUERY', N'proxyCode', N'CONFIG', N'PROXY_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 7, 0, N'ones', '2026-07-30 14:01:22.037', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2111)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2111, 1013, N'QUERY', N'proxyCode', N'CONFIG', N'PROXY_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 7, 0, N'ones', '2026-07-30 14:01:39.983', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2112)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2112, 1014, N'QUERY', N'proxyCode', N'CONFIG', N'PROXY_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 7, 0, N'ones', '2026-07-30 14:01:55.647', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2113)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2113, 1015, N'QUERY', N'proxyCode', N'CONFIG', N'PROXY_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 7, 0, N'ones', '2026-07-30 14:02:13.340', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2114)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2114, 1002, N'BODY', N'voucherBook', N'CONFIG', N'VOUCHER_BOOK', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 8, 0, N'ones', '2026-07-30 14:03:58.173', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2115)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2115, 1002, N'BODY', N'data', N'CUSTOM', N'[   {     "key": "P${PROPERTY.PropertyID}-VAT${INVOICE.ID}",     "invoiceDate": "${SYSTEM.CurrentDate|date:dd/MM/yyyy}"   } ]', N'ARRAY', 1, NULL, NULL, 0, NULL, NULL, 9, 0, N'ones', '2026-07-30 14:04:22.883', N'ones', '2026-07-31 15:08:36.477', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2116)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2116, 1003, N'BODY', N'data', N'CUSTOM', N'FAST_STATUS_DATA', N'ARRAY', 1, NULL, NULL, 0, NULL, NULL, 8, 0, N'ones', '2026-07-30 14:05:03.420', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2117)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2117, 1004, N'BODY', N'voucherBook', N'CONFIG', N'VOUCHER_BOOK', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 8, 0, N'ones', '2026-07-30 14:05:47.797', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2118)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2118, 1004, N'BODY', N'data', N'CUSTOM', N'FAST_PUBLISH_DATA', N'OBJECT', 1, NULL, NULL, 0, NULL, NULL, 9, 0, N'ones', '2026-07-30 14:06:15.650', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2119)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2119, 1005, N'BODY', N'voucherBook', N'CONFIG', N'VOUCHER_BOOK', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 8, 0, N'ones', '2026-07-30 14:06:49.307', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2120)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2120, 1005, N'BODY', N'data', N'CUSTOM', N'{' + CHAR(13) + N'' + CHAR(10) + N'      "structure": {' + CHAR(13) + N'' + CHAR(10) + N'        "master": [' + CHAR(13) + N'' + CHAR(10) + N'          "Key",' + CHAR(13) + N'' + CHAR(10) + N'          "InvoiceNumber",' + CHAR(13) + N'' + CHAR(10) + N'          "InvoiceForm",' + CHAR(13) + N'' + CHAR(10) + N'          "InvoiceSerial",' + CHAR(13) + N'' + CHAR(10) + N'          "VerificationCode",' + CHAR(13) + N'' + CHAR(10) + N'          "KeySearch",' + CHAR(13) + N'' + CHAR(10) + N'          "InvoiceDate",' + CHAR(13) + N'' + CHAR(10) + N'          "Amount",' + CHAR(13) + N'' + CHAR(10) + N'          "TotalAmount",' + CHAR(13) + N'' + CHAR(10) + N'          "AmountInWords",' + CHAR(13) + N'' + CHAR(10) + N'          "ReleaseType"' + CHAR(13) + N'' + CHAR(10) + N'        ],' + CHAR(13) + N'' + CHAR(10) + N'        "detail": [' + CHAR(13) + N'' + CHAR(10) + N'          "ProcessType",' + CHAR(13) + N'' + CHAR(10) + N'          "ItemCode",' + CHAR(13) + N'' + CHAR(10) + N'          "ItemName",' + CHAR(13) + N'' + CHAR(10) + N'          "UOM",' + CHAR(13) + N'' + CHAR(10) + N'          "Quantity",' + CHAR(13) + N'' + CHAR(10) + N'          "Price",' + CHAR(13) + N'' + CHAR(10) + N'          "Amount",' + CHAR(13) + N'' + CHAR(10) + N'          "TaxRate",' + CHAR(13) + N'' + CHAR(10) + N'          "TaxAmount",' + CHAR(13) + N'' + CHAR(10) + N'          "DiscountRate",' + CHAR(13) + N'' + CHAR(10) + N'          "DiscountAmount"' + CHAR(13) + N'' + CHAR(10) + N'        ]' + CHAR(13) + N'' + CHAR(10) + N'      },' + CHAR(13) + N'' + CHAR(10) + N'      "invoices": [' + CHAR(13) + N'' + CHAR(10) + N'        {' + CHAR(13) + N'' + CHAR(10) + N'          "master": [' + CHAR(13) + N'' + CHAR(10) + N'            "${RESPONSE.Key}",' + CHAR(13) + N'' + CHAR(10) + N'            "${RESPONSE.InvoiceNumber}",' + CHAR(13) + N'' + CHAR(10) + N'            "${RESPONSE.InvoiceForm}",' + CHAR(13) + N'' + CHAR(10) + N'            "${RESPONSE.InvoiceSerial}",' + CHAR(13) + N'' + CHAR(10) + N'            "${RESPONSE.VerificationCode}",' + CHAR(13) + N'' + CHAR(10) + N'            "${RESPONSE.KeySearch}",' + CHAR(13) + N'' + CHAR(10) + N'            "${RESPONSE.InvoiceDate}",' + CHAR(13) + N'' + CHAR(10) + N'            "${INVOICE.AmountBeforeTax}",' + CHAR(13) + N'' + CHAR(10) + N'            "${INVOICE.TotalAmount}",' + CHAR(13) + N'' + CHAR(10) + N'            "${INVOICE.AmountInWords}",' + CHAR(13) + N'' + CHAR(10) + N'            "1"' + CHAR(13) + N'' + CHAR(10) + N'          ],' + CHAR(13) + N'' + CHAR(10) + N'          "detail":' + CHAR(13) + N'' + CHAR(10) + N'            "${SYSTEM.InvoiceDetailRows}"' + CHAR(13) + N'' + CHAR(10) + N'        }' + CHAR(13) + N'' + CHAR(10) + N'      ]' + CHAR(13) + N'' + CHAR(10) + N'    }', N'OBJECT', 1, NULL, NULL, 0, NULL, NULL, 9, 0, N'ones', '2026-07-30 14:07:14.870', N'ones', '2026-08-03 10:36:15.700', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2121)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2121, 1006, N'BODY', N'data', N'CUSTOM', N'FAST_STATUS_DATA', N'ARRAY', 1, NULL, NULL, 0, NULL, NULL, 8, 0, N'ones', '2026-07-30 14:07:59.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2122)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2122, 1007, N'BODY', N'keySearch', N'INVOICE', N'keySearch', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 8, 0, N'ones', '2026-07-30 14:08:36.230', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2123)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2123, 1012, N'BODY', N'key', N'CUSTOM', N'FAST_CANCEL_KEYS', N'ARRAY', 1, NULL, NULL, 0, NULL, NULL, 8, 0, N'ones', '2026-07-30 14:09:31.957', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2124)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2124, 1012, N'BODY', N'key with', N'CUSTOM', N'FAST_CANCEL_WITH_REASON', N'ARRAY', 1, NULL, NULL, 0, NULL, NULL, 9, 0, N'ones', '2026-07-30 14:10:56.600', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2125)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2125, 1008, N'BODY', N'voucherBook', N'CONFIG', N'VOUCHER_BOOK', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 8, 0, N'ones', '2026-07-30 14:12:05.687', N'ones', '2026-07-30 14:12:55.753', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2126)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2126, 1008, N'BODY', N'adjustmentType', N'INVOICE', N'AdjustmentType', N'INT', 1, NULL, NULL, 0, NULL, NULL, 9, 0, N'ones', '2026-07-30 14:12:36.520', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2127)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2127, 1008, N'BODY', N'originalInvoice', N'INVOICE', N'OriginalKeySearch', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 10, 0, N'ones', '2026-07-30 14:13:24.870', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2128)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2128, 1008, N'BODY', N'data', N'CUSTOM', N'FAST_ADJUST_DATA', N'OBJECT', 1, NULL, NULL, 0, NULL, NULL, 0, 0, N'ones', '2026-07-30 14:13:42.810', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2129)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2129, 1009, N'BODY', N'voucherBook', N'CONFIG', N'VOUCHER_BOOK', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 8, 0, N'ones', '2026-07-30 14:15:36.230', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2130)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2130, 1009, N'BODY', N'adjustmentType', N'INVOICE', N'AdjustmentType', N'INT', 1, NULL, NULL, 0, NULL, NULL, 0, 0, N'ones', '2026-07-30 14:16:03.943', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2131)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2131, 1009, N'BODY', N'originalInvoice', N'INVOICE', N'OriginalKeySearch', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 10, 0, N'ones', '2026-07-30 14:16:27.710', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2132)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2132, 1009, N'BODY', N'data', N'CUSTOM', N'FAST_ADJUST_DATA', N'OBJECT', 1, NULL, NULL, 0, NULL, NULL, 11, 0, N'ones', '2026-07-30 14:17:20.690', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2133)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2133, 1010, N'BODY', N'voucherBook', N'CONFIG', N'VOUCHER_BOOK', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 8, 0, N'ones', '2026-07-30 14:18:12.880', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2134)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2134, 1010, N'BODY', N'adjustmentType', N'CONSTANT', N'4', N'INT', 1, NULL, NULL, 0, NULL, NULL, 9, 0, N'ones', '2026-07-30 14:18:44.207', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2135)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2135, 1010, N'BODY', N'originalInvoice', N'INVOICE', N'OriginalKeySearch', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 10, 0, N'ones', '2026-07-30 14:19:08.873', N'ones', '2026-07-30 14:19:56.607', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2136)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2136, 1010, N'BODY', N'data', N'CUSTOM', N'FAST_REPLACE_DATA', N'OBJECT', 1, NULL, NULL, 0, NULL, NULL, 11, 0, N'ones', '2026-07-30 14:19:45.460', N'ones', '2026-07-30 14:20:01.190', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2137)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2137, 1011, N'BODY', N'voucherBook', N'CONFIG', N'VOUCHER_BOOK', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 8, 0, N'ones', '2026-07-30 14:21:22.570', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2138)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2138, 1011, N'BODY', N'adjustmentType', N'CONSTANT', N'4', N'INT', 1, NULL, NULL, 0, NULL, NULL, 9, 0, N'ones', '2026-07-30 14:21:54.237', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2139)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2139, 1011, N'BODY', N'originalInvoice', N'INVOICE', N'OriginalKeySearch', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 10, 0, N'ones', '2026-07-30 14:22:19.437', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2140)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2140, 1011, N'BODY', N'data', N'INVOICE', N'FAST_REPLACE_DATA', N'OBJECT', 1, NULL, NULL, 0, NULL, NULL, 0, 0, N'ones', '2026-07-30 14:23:01.490', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2141)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2141, 1013, N'BODY', N'data', N'CUSTOM', N'FAST_RAW_DATA', N'OBJECT', 1, NULL, NULL, 0, NULL, NULL, 8, 0, N'ones', '2026-07-30 14:24:23.870', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2142)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2142, 1014, N'BODY', N'key', N'INVOICE', N'InvoiceKey', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 8, 0, N'ones', '2026-07-30 14:25:33.840', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2143)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2143, 1014, N'BODY', N'invoiceDate', N'INVOICE', N'InvoiceDate', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 9, 0, N'ones', '2026-07-30 14:26:05.060', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 2144)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (2144, 1015, N'BODY', N'data', N'CUSTOM', N'FAST_STATUS_DATA', N'ARRAY', 1, NULL, NULL, 0, NULL, NULL, 8, 0, N'ones', '2026-07-30 14:27:23.377', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3016)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3016, 1016, N'BODY', N'username', N'CONFIG', N'USERNAME', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 10, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3017)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3017, 1016, N'BODY', N'password', N'CONFIG', N'PASSWORD', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 20, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3018)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3018, 1, N'PATH', N'supplierTaxCode', N'CONFIG', N'SUPPLIER_TAX_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 10, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3019)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3019, 1, N'BODY', N'generalInvoiceInfo.invoiceType', N'CONFIG', N'INVOICE_TYPE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 20, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3020)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3020, 1, N'BODY', N'generalInvoiceInfo.templateCode', N'CONFIG', N'TEMPLATE_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 30, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3021)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3021, 1, N'BODY', N'generalInvoiceInfo.invoiceSeries', N'CONFIG', N'INVOICE_SERIES', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 40, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3022)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3022, 1, N'BODY', N'generalInvoiceInfo.currencyCode', N'INVOICE', N'CurrencyCode', N'STRING', 1, N'VND', NULL, 0, NULL, NULL, 50, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3023)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3023, 1, N'BODY', N'generalInvoiceInfo.adjustmentType', N'CONSTANT', N'1', N'STRING', 1, N'1', NULL, 0, NULL, NULL, 60, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3024)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3024, 1, N'BODY', N'generalInvoiceInfo.paymentStatus', N'INVOICE', N'PaymentStatus', N'BOOLEAN', 1, N'false', NULL, 0, NULL, NULL, 70, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3025)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3025, 1, N'BODY', N'generalInvoiceInfo.transactionUuid', N'INVOICE', N'TransactionUuid', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 80, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3026)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3026, 1, N'BODY', N'generalInvoiceInfo.invoiceIssuedDate', N'INVOICE', N'InvoiceDate', N'UNIX_MILLISECONDS', 0, NULL, NULL, 1, NULL, NULL, 90, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', N'FIX_VIETTEL_DATATYPE', '2026-08-07 09:14:41.793', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3027)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3027, 1, N'BODY', N'generalInvoiceInfo.validation', N'CONFIG', N'VALIDATION', N'INTEGER', 1, N'1', NULL, 0, NULL, NULL, 100, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', N'FIX_VIETTEL_DATATYPE', '2026-08-07 09:15:28.620', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3028)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3028, 1, N'BODY', N'generalInvoiceInfo.invoiceNote', N'INVOICE', N'Comment', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 110, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3029)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3029, 1, N'BODY', N'buyerInfo.buyerName', N'INVOICE', N'CustomerName', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 120, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3030)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3030, 1, N'BODY', N'buyerInfo.buyerLegalName', N'INVOICE', N'CompanyName', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 130, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3031)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3031, 1, N'BODY', N'buyerInfo.buyerTaxCode', N'INVOICE', N'TaxCode', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 140, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3032)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3032, 1, N'BODY', N'buyerInfo.buyerAddressLine', N'INVOICE', N'CustomerAddress', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 150, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3033)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3033, 1, N'BODY', N'buyerInfo.buyerEmail', N'INVOICE', N'CustomerEmail', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 160, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3034)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3034, 1017, N'BODY', N'supplierTaxCode', N'CONFIG', N'SUPPLIER_TAX_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 10, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3035)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3035, 1017, N'BODY', N'transactionUuid', N'INVOICE', N'TransactionUuid', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 20, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3036)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3036, 1018, N'BODY', N'supplierTaxCode', N'CONFIG', N'SUPPLIER_TAX_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 10, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3037)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3037, 1018, N'BODY', N'invoiceNo', N'INVOICE', N'InvoiceNo', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 20, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3038)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3038, 1018, N'BODY', N'templateCode', N'CONFIG', N'TEMPLATE_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 30, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3039)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3039, 1018, N'BODY', N'transactionUuid', N'INVOICE', N'TransactionUuid', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 40, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3040)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3040, 1018, N'BODY', N'fileType', N'CONSTANT', N'PDF', N'STRING', 1, N'PDF', NULL, 0, NULL, NULL, 50, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3041)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3041, 1019, N'BODY', N'supplierTaxCode', N'CONFIG', N'SUPPLIER_TAX_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 10, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3042)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3042, 1019, N'BODY', N'transactionUuid', N'INVOICE', N'TransactionUuid', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 20, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3043)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3043, 1019, N'BODY', N'buyerEmail', N'INVOICE', N'CustomerEmail', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 30, 0, N'SQL_CONFIG_VIETTEL', '2026-08-06 10:01:38.513', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3044)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3044, 1, N'HEADER', N'Cookie', N'RESPONSE', N'access_token', N'STRING', 1, NULL, NULL, 0, N'access_token=', NULL, 5, 0, N'CONFIG_AUTH_RESPONSE', '2026-08-06 16:17:27.270', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3045)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3045, 1, N'BODY', N'payments[].paymentMethodName', N'INVOICE', N'PaymentMethod', N'STRING', 0, N'TM/CK', NULL, 1, NULL, NULL, 170, 0, N'CONFIG_VIETTEL_BODY_V2', '2026-08-06 17:23:56.813', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3046)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3046, 1, N'BODY', N'itemInfo[].lineNumber', N'COLLECTION', N'Items.LineNumber', N'INTEGER', 1, NULL, NULL, 0, NULL, NULL, 200, 0, N'CONFIG_VIETTEL_BODY_V2', '2026-08-06 17:23:56.813', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3047)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3047, 1, N'BODY', N'itemInfo[].itemCode', N'COLLECTION', N'Items.ItemCode', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 210, 0, N'CONFIG_VIETTEL_BODY_V2', '2026-08-06 17:23:56.813', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3048)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3048, 1, N'BODY', N'itemInfo[].itemName', N'COLLECTION', N'Items.ItemName', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 220, 0, N'CONFIG_VIETTEL_BODY_V2', '2026-08-06 17:23:56.813', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3049)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3049, 1, N'BODY', N'itemInfo[].unitName', N'COLLECTION', N'Items.UnitName', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 230, 0, N'CONFIG_VIETTEL_BODY_V2', '2026-08-06 17:23:56.813', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3050)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3050, 1, N'BODY', N'itemInfo[].quantity', N'COLLECTION', N'Items.Quantity', N'DECIMAL', 1, NULL, NULL, 0, NULL, NULL, 240, 0, N'CONFIG_VIETTEL_BODY_V2', '2026-08-06 17:23:56.813', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3051)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3051, 1, N'BODY', N'itemInfo[].unitPrice', N'COLLECTION', N'Items.UnitPrice', N'DECIMAL', 1, NULL, N'ROUND:0', 0, NULL, NULL, 250, 0, N'CONFIG_VIETTEL_BODY_V2', '2026-08-06 17:23:56.813', N'FIX_VND_PRECISION', '2026-08-07 11:28:45.477', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3052)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3052, 1, N'BODY', N'itemInfo[].itemTotalAmountWithoutTax', N'COLLECTION', N'Items.AmountBeforeTax', N'DECIMAL', 1, NULL, N'ROUND:0', 0, NULL, NULL, 260, 0, N'CONFIG_VIETTEL_BODY_V2', '2026-08-06 17:23:56.813', N'FIX_VND_PRECISION', '2026-08-07 11:28:45.477', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3053)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3053, 1, N'BODY', N'itemInfo[].taxPercentage', N'COLLECTION', N'Items.VATRate', N'DECIMAL', 1, NULL, NULL, 0, NULL, NULL, 270, 0, N'CONFIG_VIETTEL_BODY_V2', '2026-08-06 17:23:56.813', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3054)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3054, 1, N'BODY', N'itemInfo[].taxAmount', N'COLLECTION', N'Items.TaxAmount', N'DECIMAL', 1, NULL, N'ROUND:0', 0, NULL, NULL, 280, 0, N'CONFIG_VIETTEL_BODY_V2', '2026-08-06 17:23:56.813', N'FIX_VND_PRECISION', '2026-08-07 11:28:45.477', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3055)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3055, 1, N'BODY', N'itemInfo[].discount', N'COLLECTION', N'Items.DiscountAmount', N'DECIMAL', 0, N'0', NULL, 1, NULL, NULL, 290, 0, N'CONFIG_VIETTEL_BODY_V2', '2026-08-06 17:23:56.813', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3056)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3056, 1, N'BODY', N'itemInfo[].itemTotalAmountWithTax', N'COLLECTION', N'Items.TotalAmount', N'DECIMAL', 1, NULL, N'ROUND:0', 0, NULL, NULL, 300, 0, N'CONFIG_VIETTEL_BODY_V2', '2026-08-06 17:23:56.813', N'FIX_VND_PRECISION', '2026-08-07 11:28:45.477', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3057)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3057, 1, N'BODY', N'summarizeInfo.sumOfTotalLineAmountWithoutTax', N'INVOICE', N'AmountBeforeTax', N'DECIMAL', 1, NULL, N'ROUND:0', 0, NULL, NULL, 400, 0, N'CONFIG_VIETTEL_BODY_V2', '2026-08-06 17:23:56.813', N'FIX_VND_PRECISION', '2026-08-07 11:28:45.477', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3058)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3058, 1, N'BODY', N'summarizeInfo.totalAmountWithoutTax', N'INVOICE', N'AmountBeforeTax', N'DECIMAL', 1, NULL, N'ROUND:0', 0, NULL, NULL, 410, 0, N'CONFIG_VIETTEL_BODY_V2', '2026-08-06 17:23:56.813', N'FIX_VND_PRECISION', '2026-08-07 11:28:45.477', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3059)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3059, 1, N'BODY', N'summarizeInfo.totalTaxAmount', N'INVOICE', N'TaxAmount', N'DECIMAL', 1, NULL, N'ROUND:0', 0, NULL, NULL, 420, 0, N'CONFIG_VIETTEL_BODY_V2', '2026-08-06 17:23:56.813', N'FIX_VND_PRECISION', '2026-08-07 11:28:45.477', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3060)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3060, 1, N'BODY', N'summarizeInfo.totalAmountWithTax', N'INVOICE', N'TotalAmount', N'DECIMAL', 1, NULL, N'ROUND:0', 0, NULL, NULL, 430, 0, N'CONFIG_VIETTEL_BODY_V2', '2026-08-06 17:23:56.813', N'FIX_VND_PRECISION', '2026-08-07 11:28:45.477', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3061)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3061, 1, N'BODY', N'taxBreakdowns[].taxPercentage', N'COLLECTION', N'TaxBreakdowns.TaxPercentage', N'DECIMAL', 1, NULL, NULL, 0, NULL, NULL, 310, 0, N'CONFIG_TAX_BREAKDOWN', '2026-08-07 12:10:15.727', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3062)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3062, 1, N'BODY', N'taxBreakdowns[].taxableAmount', N'COLLECTION', N'TaxBreakdowns.TaxableAmount', N'DECIMAL', 1, NULL, N'ROUND:0', 0, NULL, NULL, 320, 0, N'CONFIG_TAX_BREAKDOWN', '2026-08-07 12:10:15.727', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3063)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3063, 1, N'BODY', N'taxBreakdowns[].taxAmount', N'COLLECTION', N'TaxBreakdowns.TaxAmount', N'DECIMAL', 1, NULL, N'ROUND:0', 0, NULL, NULL, 330, 0, N'CONFIG_TAX_BREAKDOWN', '2026-08-07 12:10:15.727', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3064)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3064, 1002, N'RESPONSE', N'Key', N'RESPONSE', N'Message[0].key', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 10, 0, NULL, '2026-08-07 17:09:55.557', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3065)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3065, 1002, N'RESPONSE', N'Pattern', N'RESPONSE', N'Message[0].pattern', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 20, 0, NULL, '2026-08-07 17:09:55.557', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3066)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3066, 1002, N'RESPONSE', N'Serial', N'RESPONSE', N'Message[0].serial', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 30, 0, NULL, '2026-08-07 17:09:55.557', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3067)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3067, 1002, N'RESPONSE', N'InvoiceNo', N'RESPONSE', N'Message[0].invoiceNo', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 40, 0, NULL, '2026-08-07 17:09:55.557', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3068)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3068, 1002, N'RESPONSE', N'InvoiceDate', N'RESPONSE', N'Message[0].invoiceDate', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 50, 0, NULL, '2026-08-07 17:09:55.557', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3069)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3069, 1002, N'RESPONSE', N'VerificationCode', N'RESPONSE', N'Message[0].verificationCode', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 60, 0, NULL, '2026-08-07 17:09:55.557', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3070)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3070, 1002, N'RESPONSE', N'KeySearch', N'RESPONSE', N'Message[0].keySearch', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 70, 0, NULL, '2026-08-07 17:09:55.557', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3071)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3071, 1002, N'RESPONSE', N'ErrorCode', N'RESPONSE', N'Code', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 80, 0, NULL, '2026-08-07 17:09:55.557', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3072)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3072, 1005, N'RESPONSE', N'ErrorCode', N'RESPONSE', N'Code', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 80, 0, NULL, '2026-08-07 17:21:11.563', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3073)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3073, 1005, N'RESPONSE', N'InvoiceDate', N'RESPONSE', N'Message[0].invoiceDate', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 50, 0, NULL, '2026-08-07 17:21:11.563', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3074)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3074, 1005, N'RESPONSE', N'InvoiceNo', N'RESPONSE', N'Message[0].invoiceNo', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 40, 0, NULL, '2026-08-07 17:21:11.563', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3075)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3075, 1005, N'RESPONSE', N'Key', N'RESPONSE', N'Message[0].key', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 10, 0, NULL, '2026-08-07 17:21:11.563', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3076)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3076, 1005, N'RESPONSE', N'KeySearch', N'RESPONSE', N'Message[0].keySearch', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 70, 0, NULL, '2026-08-07 17:21:11.563', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3077)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3077, 1005, N'RESPONSE', N'Pattern', N'RESPONSE', N'Message[0].pattern', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 20, 0, NULL, '2026-08-07 17:21:11.563', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3078)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3078, 1005, N'RESPONSE', N'Serial', N'RESPONSE', N'Message[0].serial', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 30, 0, NULL, '2026-08-07 17:21:11.563', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3079)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3079, 1005, N'RESPONSE', N'VerificationCode', N'RESPONSE', N'Message[0].verificationCode', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 60, 0, NULL, '2026-08-07 17:21:11.563', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3080)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3080, 1021, N'BODY', N'supplierTaxCode', N'CONFIG', N'SUPPLIER_TAX_CODE', N'STRING', 1, NULL, NULL, 1, NULL, NULL, 10, 1, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3081)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3081, 1021, N'BODY', N'templateCode', N'CONFIG', N'TEMPLATE_CODE', N'STRING', 1, NULL, NULL, 1, NULL, NULL, 20, 1, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3082)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3082, 1021, N'BODY', N'invoiceSeries', N'CONFIG', N'INVOICE_SERIES', N'STRING', 1, NULL, NULL, 1, NULL, NULL, 30, 1, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3083)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3083, 1021, N'BODY', N'invoiceDate', N'INVOICE', N'InvoiceDate', N'DATE', 1, NULL, N'yyyy-MM-dd', 1, NULL, NULL, 40, 1, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3084)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3084, 1021, N'BODY', N'currencyCode', N'INVOICE', N'CurrencyCode', N'STRING', 0, N'VND', NULL, 1, NULL, NULL, 50, 1, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3085)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3085, 1021, N'BODY', N'paymentMethod', N'INVOICE', N'PaymentMethod', N'STRING', 0, N'TM/CK', NULL, 1, NULL, NULL, 60, 1, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3086)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3086, 1021, N'BODY', N'buyer.name', N'INVOICE', N'CustomerName', N'STRING', 1, NULL, NULL, 1, NULL, NULL, 70, 1, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3087)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3087, 1021, N'BODY', N'buyer.companyName', N'INVOICE', N'CustomerCompany', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 80, 1, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3088)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3088, 1021, N'BODY', N'buyer.taxCode', N'INVOICE', N'CustomerTaxCode', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 90, 1, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3089)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3089, 1021, N'BODY', N'buyer.address', N'INVOICE', N'CustomerAddress', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 100, 1, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3090)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3090, 1021, N'BODY', N'buyer.email', N'INVOICE', N'CustomerEmail', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 110, 1, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3091)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3091, 1021, N'BODY', N'total.amountBeforeTax', N'INVOICE', N'AmountBeforeTax', N'DECIMAL', 1, NULL, NULL, 1, NULL, NULL, 120, 1, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3092)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3092, 1021, N'BODY', N'total.taxAmount', N'INVOICE', N'TaxAmount', N'DECIMAL', 1, NULL, NULL, 1, NULL, NULL, 130, 1, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3093)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3093, 1021, N'BODY', N'total.totalAmount', N'INVOICE', N'TotalAmount', N'DECIMAL', 1, NULL, NULL, 1, NULL, NULL, 140, 1, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3094)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3094, 1021, N'BODY', N'requestId', N'SYSTEM', N'GUID', N'STRING', 1, NULL, NULL, 1, NULL, NULL, 150, 1, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3095)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3095, 1021, N'HEADER', N'Authorization', N'CONFIG', N'API_KEY', N'STRING', 1, NULL, NULL, 1, N'Bearer ', NULL, 10, 1, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3096)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3096, 1021, N'HEADER', N'Accept', N'STATIC', N'application/json', N'STRING', 1, NULL, NULL, 1, NULL, NULL, 20, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3097)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3097, 1021, N'QUERY', N'taxCode', N'CONFIG', N'SUPPLIER_TAX_CODE', N'STRING', 1, NULL, NULL, 1, NULL, NULL, 10, 1, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3098)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3098, 1021, N'RESPONSE', N'InvoiceNo', N'RESPONSE', N'result.invoiceNo', N'STRING', 1, NULL, NULL, 1, NULL, NULL, 10, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3099)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3099, 1021, N'RESPONSE', N'TransactionID', N'RESPONSE', N'result.transactionID', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 20, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3100)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3100, 1021, N'RESPONSE', N'ReservationCode', N'RESPONSE', N'result.reservationCode', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 30, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3101)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3101, 1021, N'RESPONSE', N'VerificationCode', N'RESPONSE', N'result.codeOfTax', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 40, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3102)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3102, 1021, N'RESPONSE', N'ErrorCode', N'RESPONSE', N'errorCode', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 50, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3103)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3103, 1021, N'RESPONSE', N'ErrorMessage', N'RESPONSE', N'description', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 60, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3104)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3104, 1021, N'BODY', N'items[].lineNumber', N'COLLECTION', N'Items.Sequence', N'INTEGER', 1, NULL, NULL, 1, NULL, NULL, 200, 1, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3105)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3105, 1021, N'BODY', N'items[].itemCode', N'COLLECTION', N'Items.ItemCode', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 210, 1, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3106)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3106, 1021, N'BODY', N'items[].itemName', N'COLLECTION', N'Items.ItemName', N'STRING', 1, NULL, NULL, 1, NULL, NULL, 220, 1, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3107)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3107, 1021, N'BODY', N'items[].unitName', N'COLLECTION', N'Items.UnitName', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 230, 1, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3108)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3108, 1021, N'BODY', N'items[].quantity', N'COLLECTION', N'Items.Quantity', N'DECIMAL', 1, NULL, NULL, 1, NULL, NULL, 240, 1, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3109)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3109, 1021, N'BODY', N'items[].unitPrice', N'COLLECTION', N'Items.UnitPrice', N'DECIMAL', 1, NULL, NULL, 1, NULL, NULL, 250, 1, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3110)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3110, 1021, N'BODY', N'items[].amountBeforeTax', N'COLLECTION', N'Items.AmountBeforeTax', N'DECIMAL', 1, NULL, NULL, 1, NULL, NULL, 260, 1, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3111)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3111, 1021, N'BODY', N'items[].taxRate', N'COLLECTION', N'Items.TaxRate', N'DECIMAL', 1, NULL, NULL, 1, NULL, NULL, 270, 1, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3112)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3112, 1021, N'BODY', N'items[].taxAmount', N'COLLECTION', N'Items.TaxAmount', N'DECIMAL', 1, NULL, NULL, 1, NULL, NULL, 280, 1, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3113)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3113, 1021, N'BODY', N'items[].totalAmount', N'COLLECTION', N'Items.TotalAmount', N'DECIMAL', 1, NULL, NULL, 1, NULL, NULL, 290, 1, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3114)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3114, 1021, N'BODY', N'testClientField', N'CONSTANT', N'TEST_CLIENT', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 999, 1, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3115)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3115, 1021, N'BODY', N'testUiField', N'CONSTANT', N'TEST_UI', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 998, 1, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3116)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3116, 1021, N'BODY', N'items', N'INVOICEDETAIL', NULL, N'ARRAY', 1, NULL, NULL, 0, NULL, NULL, 123, 1, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3117)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3117, 1021, N'PATH', N'supplierTaxCode', N'CONFIG', N'SUPPLIER_TAX_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 10, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3118)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3118, 1021, N'BODY', N'generalInvoiceInfo.invoiceType', N'CONFIG', N'INVOICE_TYPE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 20, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3119)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3119, 1021, N'BODY', N'generalInvoiceInfo.templateCode', N'CONFIG', N'TEMPLATE_CODE', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 30, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3120)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3120, 1021, N'BODY', N'generalInvoiceInfo.invoiceSeries', N'CONFIG', N'INVOICE_SERIES', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 40, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3121)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3121, 1021, N'BODY', N'generalInvoiceInfo.currencyCode', N'INVOICE', N'CurrencyCode', N'STRING', 1, N'VND', NULL, 0, NULL, NULL, 50, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3122)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3122, 1021, N'BODY', N'generalInvoiceInfo.adjustmentType', N'CONSTANT', N'1', N'STRING', 1, N'1', NULL, 0, NULL, NULL, 60, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3123)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3123, 1021, N'BODY', N'generalInvoiceInfo.paymentStatus', N'INVOICE', N'PaymentStatus', N'BOOLEAN', 1, N'false', NULL, 0, NULL, NULL, 70, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3124)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3124, 1021, N'BODY', N'generalInvoiceInfo.transactionUuid', N'INVOICE', N'TransactionUuid', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 80, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3125)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3125, 1021, N'BODY', N'generalInvoiceInfo.invoiceIssuedDate', N'INVOICE', N'InvoiceDate', N'UNIX_MILLISECONDS', 0, NULL, NULL, 1, NULL, NULL, 90, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3126)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3126, 1021, N'BODY', N'generalInvoiceInfo.validation', N'CONFIG', N'VALIDATION', N'INTEGER', 1, N'1', NULL, 0, NULL, NULL, 100, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3127)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3127, 1021, N'BODY', N'generalInvoiceInfo.invoiceNote', N'INVOICE', N'Comment', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 110, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3128)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3128, 1021, N'BODY', N'buyerInfo.buyerName', N'INVOICE', N'CustomerName', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 120, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3129)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3129, 1021, N'BODY', N'buyerInfo.buyerLegalName', N'INVOICE', N'CompanyName', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 130, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3130)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3130, 1021, N'BODY', N'buyerInfo.buyerTaxCode', N'INVOICE', N'TaxCode', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 140, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3131)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3131, 1021, N'BODY', N'buyerInfo.buyerAddressLine', N'INVOICE', N'CustomerAddress', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 150, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3132)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3132, 1021, N'BODY', N'buyerInfo.buyerEmail', N'INVOICE', N'CustomerEmail', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 160, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3133)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3133, 1021, N'HEADER', N'Cookie', N'RESPONSE', N'access_token', N'STRING', 1, NULL, NULL, 0, N'access_token=', NULL, 5, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3134)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3134, 1021, N'BODY', N'payments[].paymentMethodName', N'INVOICE', N'PaymentMethod', N'STRING', 0, N'TM/CK', NULL, 1, NULL, NULL, 170, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3135)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3135, 1021, N'BODY', N'itemInfo[].lineNumber', N'COLLECTION', N'Items.LineNumber', N'INTEGER', 1, NULL, NULL, 0, NULL, NULL, 200, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3136)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3136, 1021, N'BODY', N'itemInfo[].itemCode', N'COLLECTION', N'Items.ItemCode', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 210, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3137)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3137, 1021, N'BODY', N'itemInfo[].itemName', N'COLLECTION', N'Items.ItemName', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 220, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3138)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3138, 1021, N'BODY', N'itemInfo[].unitName', N'COLLECTION', N'Items.UnitName', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 230, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3139)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3139, 1021, N'BODY', N'itemInfo[].quantity', N'COLLECTION', N'Items.Quantity', N'DECIMAL', 1, NULL, NULL, 0, NULL, NULL, 240, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3140)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3140, 1021, N'BODY', N'itemInfo[].unitPrice', N'COLLECTION', N'Items.UnitPrice', N'DECIMAL', 1, NULL, N'ROUND:0', 0, NULL, NULL, 250, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3141)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3141, 1021, N'BODY', N'itemInfo[].itemTotalAmountWithoutTax', N'COLLECTION', N'Items.AmountBeforeTax', N'DECIMAL', 1, NULL, N'ROUND:0', 0, NULL, NULL, 260, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3142)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3142, 1021, N'BODY', N'itemInfo[].taxPercentage', N'COLLECTION', N'Items.VATRate', N'DECIMAL', 1, NULL, NULL, 0, NULL, NULL, 270, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3143)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3143, 1021, N'BODY', N'itemInfo[].taxAmount', N'COLLECTION', N'Items.TaxAmount', N'DECIMAL', 1, NULL, N'ROUND:0', 0, NULL, NULL, 280, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3144)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3144, 1021, N'BODY', N'itemInfo[].discount', N'COLLECTION', N'Items.DiscountAmount', N'DECIMAL', 0, N'0', NULL, 1, NULL, NULL, 290, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3145)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3145, 1021, N'BODY', N'itemInfo[].itemTotalAmountWithTax', N'COLLECTION', N'Items.TotalAmount', N'DECIMAL', 1, NULL, N'ROUND:0', 0, NULL, NULL, 300, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3146)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3146, 1021, N'BODY', N'summarizeInfo.sumOfTotalLineAmountWithoutTax', N'INVOICE', N'AmountBeforeTax', N'DECIMAL', 1, NULL, N'ROUND:0', 0, NULL, NULL, 400, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3147)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3147, 1021, N'BODY', N'summarizeInfo.totalAmountWithoutTax', N'INVOICE', N'AmountBeforeTax', N'DECIMAL', 1, NULL, N'ROUND:0', 0, NULL, NULL, 410, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3148)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3148, 1021, N'BODY', N'summarizeInfo.totalTaxAmount', N'INVOICE', N'TaxAmount', N'DECIMAL', 1, NULL, N'ROUND:0', 0, NULL, NULL, 420, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3149)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3149, 1021, N'BODY', N'summarizeInfo.totalAmountWithTax', N'INVOICE', N'TotalAmount', N'DECIMAL', 1, NULL, N'ROUND:0', 0, NULL, NULL, 430, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3150)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3150, 1021, N'BODY', N'taxBreakdowns[].taxPercentage', N'COLLECTION', N'TaxBreakdowns.TaxPercentage', N'DECIMAL', 1, NULL, NULL, 0, NULL, NULL, 310, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3151)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3151, 1021, N'BODY', N'taxBreakdowns[].taxableAmount', N'COLLECTION', N'TaxBreakdowns.TaxableAmount', N'DECIMAL', 1, NULL, N'ROUND:0', 0, NULL, NULL, 320, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3152)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3152, 1021, N'BODY', N'taxBreakdowns[].taxAmount', N'COLLECTION', N'TaxBreakdowns.TaxAmount', N'DECIMAL', 1, NULL, N'ROUND:0', 0, NULL, NULL, 330, 0, NULL, '2026-08-08 09:21:22.527', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3153)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3153, 1022, N'BODY', N'xmlInvData', N'DOCUMENT', N'VNPT_INVOICE_XML', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 10, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', N'CONFIG_VNPT_DOCUMENT', '2026-08-10 16:38:12.387', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3154)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3154, 1022, N'BODY', N'username', N'CONFIG', N'USERNAME', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 20, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3155)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3155, 1022, N'BODY', N'password', N'CONFIG', N'PASSWORD', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 30, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3156)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3156, 1022, N'BODY', N'convert', N'STATIC', NULL, N'INTEGER', 1, N'0', NULL, 0, NULL, NULL, 40, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', N'FIX_VNPT_DATATYPE', '2026-08-10 15:23:58.460', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3157)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3157, 1023, N'BODY', N'Account', N'CONFIG', N'ACCOUNT', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 10, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3158)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3158, 1023, N'BODY', N'ACpass', N'CONFIG', N'AC_PASSWORD', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 20, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3159)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3159, 1023, N'BODY', N'xmlInvData', N'DOCUMENT', N'VNPT_POS_INVOICE_XML', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 30, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', N'CONFIG_VNPT_DOCUMENT', '2026-08-10 16:38:12.387', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3160)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3160, 1023, N'BODY', N'username', N'CONFIG', N'USERNAME', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 40, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3161)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3161, 1023, N'BODY', N'password', N'CONFIG', N'PASSWORD', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 50, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3162)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3162, 1023, N'BODY', N'pattern', N'CONFIG', N'PATTERN', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 60, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3163)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3163, 1023, N'BODY', N'serial', N'CONFIG', N'SERIAL', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 70, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3164)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3164, 1023, N'BODY', N'convert', N'STATIC', NULL, N'INTEGER', 1, N'0', NULL, 0, NULL, NULL, 80, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', N'FIX_VNPT_DATATYPE', '2026-08-10 15:23:58.460', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3165)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3165, 1024, N'BODY', N'Account', N'CONFIG', N'ACCOUNT', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 10, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3166)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3166, 1024, N'BODY', N'ACpass', N'CONFIG', N'AC_PASSWORD', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 20, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3167)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3167, 1024, N'BODY', N'xmlInvData', N'DOCUMENT', N'VNPT_INVOICE_XML', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 30, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', N'CONFIG_VNPT_DOCUMENT', '2026-08-10 16:38:12.387', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3168)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3168, 1024, N'BODY', N'username', N'CONFIG', N'USERNAME', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 40, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3169)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3169, 1024, N'BODY', N'password', N'CONFIG', N'PASSWORD', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 50, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3170)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3170, 1024, N'BODY', N'pattern', N'CONFIG', N'PATTERN', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 60, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3171)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3171, 1024, N'BODY', N'serial', N'CONFIG', N'SERIAL', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 70, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3172)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3172, 1024, N'BODY', N'convert', N'STATIC', NULL, N'INTEGER', 1, N'0', NULL, 0, NULL, NULL, 80, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', N'FIX_VNPT_DATATYPE', '2026-08-10 15:23:58.460', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3173)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3173, 1025, N'BODY', N'Account', N'CONFIG', N'ACCOUNT', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 10, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3174)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3174, 1025, N'BODY', N'ACpass', N'CONFIG', N'AC_PASSWORD', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 20, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3175)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3175, 1025, N'BODY', N'xmlInvData', N'DOCUMENT', N'VNPT_INVOICE_XML', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 30, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', N'CONFIG_VNPT_DOCUMENT', '2026-08-10 16:38:12.387', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3176)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3176, 1025, N'BODY', N'username', N'CONFIG', N'USERNAME', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 40, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3177)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3177, 1025, N'BODY', N'password', N'CONFIG', N'PASSWORD', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 50, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3178)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3178, 1025, N'BODY', N'pattern', N'CONFIG', N'PATTERN', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 60, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3179)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3179, 1025, N'BODY', N'serial', N'CONFIG', N'SERIAL', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 70, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3180)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3180, 1025, N'BODY', N'convert', N'STATIC', NULL, N'INTEGER', 1, N'0', NULL, 0, NULL, NULL, 80, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', N'FIX_VNPT_DATATYPE', '2026-08-10 15:23:58.460', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3181)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3181, 1026, N'BODY', N'Account', N'CONFIG', N'ACCOUNT', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 10, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3182)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3182, 1026, N'BODY', N'ACpass', N'CONFIG', N'AC_PASSWORD', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 20, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3183)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3183, 1026, N'BODY', N'lsFkey', N'RESPONSE', N'Key', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 30, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3184)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3184, 1026, N'BODY', N'username', N'CONFIG', N'USERNAME', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 40, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3185)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3185, 1026, N'BODY', N'password', N'CONFIG', N'PASSWORD', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 50, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3186)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3186, 1026, N'BODY', N'pattern', N'CONFIG', N'PATTERN', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 60, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3187)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3187, 1026, N'BODY', N'serial', N'CONFIG', N'SERIAL', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 70, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3188)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3188, 1027, N'BODY', N'fkey', N'INVOICE', N'Fkey', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 10, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3189)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3189, 1027, N'BODY', N'userName', N'CONFIG', N'USERNAME', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 20, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3190)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3190, 1027, N'BODY', N'userPass', N'CONFIG', N'PASSWORD', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 30, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3191)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3191, 1027, N'BODY', N'Account', N'CONFIG', N'ACCOUNT', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 40, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3192)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3192, 1027, N'BODY', N'ACpass', N'CONFIG', N'AC_PASSWORD', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 50, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3193)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3193, 1027, N'BODY', N'pattern', N'CONFIG', N'PATTERN', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 60, 0, N'SQL_CONFIG_VNPT', '2026-08-10 13:41:06.880', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3194)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3194, 1028, N'BODY', N'Account', N'CONFIG', N'ACCOUNT', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 10, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3195)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3195, 1028, N'BODY', N'ACpass', N'CONFIG', N'AC_PASSWORD', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 20, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3196)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3196, 1028, N'BODY', N'xmlInvData', N'DOCUMENT', N'VNPT_INVOICE_XML', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 30, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', N'CONFIG_VNPT_DOCUMENT', '2026-08-10 16:38:12.387', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3197)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3197, 1028, N'BODY', N'username', N'CONFIG', N'USERNAME', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 40, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3198)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3198, 1028, N'BODY', N'pass', N'CONFIG', N'PASSWORD', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 50, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3199)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3199, 1028, N'BODY', N'fkey', N'INVOICE', N'Fkey', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 60, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3200)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3200, 1028, N'BODY', N'Attachfile', N'INVOICE', N'AttachFile', N'STRING', 0, NULL, NULL, 0, NULL, NULL, 70, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3201)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3201, 1028, N'BODY', N'convert', N'STATIC', NULL, N'INTEGER', 1, N'0', NULL, 0, NULL, NULL, 80, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', N'FIX_VNPT_DATATYPE', '2026-08-10 15:23:58.460', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3202)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3202, 1028, N'BODY', N'pattern', N'CONFIG', N'PATTERN', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 90, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3203)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3203, 1028, N'BODY', N'serial', N'CONFIG', N'SERIAL', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 100, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3204)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3204, 1029, N'BODY', N'Account', N'CONFIG', N'ACCOUNT', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 10, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3205)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3205, 1029, N'BODY', N'ACpass', N'CONFIG', N'AC_PASSWORD', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 20, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3206)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3206, 1029, N'BODY', N'xmlInvData', N'DOCUMENT', N'VNPT_INVOICE_XML', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 30, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', N'CONFIG_VNPT_DOCUMENT', '2026-08-10 16:38:12.387', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3207)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3207, 1029, N'BODY', N'username', N'CONFIG', N'USERNAME', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 40, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3208)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3208, 1029, N'BODY', N'pass', N'CONFIG', N'PASSWORD', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 50, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3209)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3209, 1029, N'BODY', N'fkey', N'INVOICE', N'Fkey', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 60, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3210)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3210, 1029, N'BODY', N'convert', N'STATIC', NULL, N'INTEGER', 1, N'0', NULL, 0, NULL, NULL, 70, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', N'FIX_VNPT_DATATYPE', '2026-08-10 15:23:58.460', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3211)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3211, 1029, N'BODY', N'pattern', N'CONFIG', N'PATTERN', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 80, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3212)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3212, 1029, N'BODY', N'serial', N'CONFIG', N'SERIAL', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 90, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3213)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3213, 1029, N'BODY', N'OldPattern', N'INVOICE', N'OldPattern', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 100, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3214)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3214, 1030, N'BODY', N'Account', N'CONFIG', N'ACCOUNT', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 10, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3215)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3215, 1030, N'BODY', N'ACpass', N'CONFIG', N'AC_PASSWORD', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 20, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3216)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3216, 1030, N'BODY', N'xmlInvData', N'DOCUMENT', N'VNPT_INVOICE_XML', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 30, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', N'CONFIG_VNPT_DOCUMENT', '2026-08-10 16:38:12.387', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3217)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3217, 1030, N'BODY', N'username', N'CONFIG', N'USERNAME', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 40, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3218)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3218, 1030, N'BODY', N'pass', N'CONFIG', N'PASSWORD', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 50, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3219)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3219, 1030, N'BODY', N'fkey', N'INVOICE', N'Fkey', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 60, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3220)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3220, 1030, N'BODY', N'Attachfile', N'INVOICE', N'AttachFile', N'STRING', 0, NULL, NULL, 0, NULL, NULL, 70, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3221)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3221, 1030, N'BODY', N'convert', N'STATIC', NULL, N'INTEGER', 1, N'0', NULL, 0, NULL, NULL, 80, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', N'FIX_VNPT_DATATYPE', '2026-08-10 15:23:58.460', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3222)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3222, 1030, N'BODY', N'pattern', N'CONFIG', N'PATTERN', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 90, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3223)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3223, 1030, N'BODY', N'serial', N'CONFIG', N'SERIAL', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 100, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3224)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3224, 1031, N'BODY', N'Account', N'CONFIG', N'ACCOUNT', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 10, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3225)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3225, 1031, N'BODY', N'ACpass', N'CONFIG', N'AC_PASSWORD', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 20, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3226)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3226, 1031, N'BODY', N'invXml', N'INVOICE', N'XmlInvData', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 30, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3227)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3227, 1031, N'BODY', N'userName', N'CONFIG', N'USERNAME', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 40, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3228)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3228, 1031, N'BODY', N'userPass', N'CONFIG', N'PASSWORD', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 50, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3229)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3229, 1031, N'BODY', N'oldPattern', N'INVOICE', N'OldPattern', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 60, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3230)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3230, 1031, N'BODY', N'oldSerial', N'INVOICE', N'OldSerial', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 70, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3231)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3231, 1031, N'BODY', N'oldNo', N'INVOICE', N'OldInvoiceNo', N'DECIMAL', 1, NULL, NULL, 0, NULL, NULL, 80, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3232)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3232, 1031, N'BODY', N'strOldArisingDate', N'INVOICE', N'OldArisingDate', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 90, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3233)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3233, 1031, N'BODY', N'convert', N'STATIC', NULL, N'INTEGER', 1, N'0', NULL, 0, NULL, NULL, 100, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', N'FIX_VNPT_DATATYPE', '2026-08-10 15:23:58.460', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3234)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3234, 1031, N'BODY', N'pattern', N'CONFIG', N'PATTERN', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 110, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3235)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3235, 1031, N'BODY', N'serial', N'CONFIG', N'SERIAL', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 120, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3236)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3236, 1031, N'BODY', N'relatedInvType', N'INVOICE', N'RelatedInvType', N'INTEGER', 1, NULL, NULL, 0, NULL, NULL, 130, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', N'FIX_VNPT_DATATYPE', '2026-08-10 15:23:58.460', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3237)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3237, 1031, N'BODY', N'feature', N'INVOICE', N'Feature', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 140, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3238)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3238, 1032, N'BODY', N'Account', N'CONFIG', N'ACCOUNT', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 10, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3239)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3239, 1032, N'BODY', N'ACpass', N'CONFIG', N'AC_PASSWORD', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 20, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3240)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3240, 1032, N'BODY', N'xmlInvData', N'DOCUMENT', N'VNPT_INVOICE_XML', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 30, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', N'CONFIG_VNPT_DOCUMENT', '2026-08-10 16:38:12.387', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3241)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3241, 1032, N'BODY', N'username', N'CONFIG', N'USERNAME', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 40, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3242)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3242, 1032, N'BODY', N'pass', N'CONFIG', N'PASSWORD', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 50, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3243)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3243, 1032, N'BODY', N'fkey', N'INVOICE', N'Fkey', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 60, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3244)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3244, 1032, N'BODY', N'Attachfile', N'INVOICE', N'AttachFile', N'STRING', 0, NULL, NULL, 0, NULL, NULL, 70, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3245)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3245, 1032, N'BODY', N'convert', N'STATIC', NULL, N'INTEGER', 1, N'0', NULL, 0, NULL, NULL, 80, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', N'FIX_VNPT_DATATYPE', '2026-08-10 15:23:58.460', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3246)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3246, 1032, N'BODY', N'pattern', N'CONFIG', N'PATTERN', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 90, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3247)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3247, 1032, N'BODY', N'serial', N'CONFIG', N'SERIAL', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 100, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3248)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3248, 1033, N'BODY', N'Account', N'CONFIG', N'ACCOUNT', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 10, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3249)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3249, 1033, N'BODY', N'ACpass', N'CONFIG', N'AC_PASSWORD', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 20, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3250)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3250, 1033, N'BODY', N'xmlInvData', N'DOCUMENT', N'VNPT_INVOICE_XML', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 30, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', N'CONFIG_VNPT_DOCUMENT', '2026-08-10 16:38:12.387', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3251)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3251, 1033, N'BODY', N'username', N'CONFIG', N'USERNAME', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 40, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3252)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3252, 1033, N'BODY', N'pass', N'CONFIG', N'PASSWORD', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 50, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3253)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3253, 1033, N'BODY', N'fkey', N'INVOICE', N'Fkey', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 60, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3254)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3254, 1033, N'BODY', N'convert', N'STATIC', NULL, N'INTEGER', 1, N'0', NULL, 0, NULL, NULL, 70, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', N'FIX_VNPT_DATATYPE', '2026-08-10 15:23:58.460', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3255)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3255, 1033, N'BODY', N'pattern', N'CONFIG', N'PATTERN', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 80, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3256)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3256, 1033, N'BODY', N'serial', N'CONFIG', N'SERIAL', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 90, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3257)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3257, 1033, N'BODY', N'OldPattern', N'INVOICE', N'OldPattern', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 100, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3258)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3258, 1034, N'BODY', N'Account', N'CONFIG', N'ACCOUNT', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 10, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3259)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3259, 1034, N'BODY', N'ACpass', N'CONFIG', N'AC_PASSWORD', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 20, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3260)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3260, 1034, N'BODY', N'xmlInvData', N'DOCUMENT', N'VNPT_INVOICE_XML', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 30, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', N'CONFIG_VNPT_DOCUMENT', '2026-08-10 16:38:12.387', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3261)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3261, 1034, N'BODY', N'username', N'CONFIG', N'USERNAME', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 40, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3262)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3262, 1034, N'BODY', N'pass', N'CONFIG', N'PASSWORD', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 50, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3263)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3263, 1034, N'BODY', N'fkey', N'INVOICE', N'Fkey', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 60, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3264)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3264, 1034, N'BODY', N'Attachfile', N'INVOICE', N'AttachFile', N'STRING', 0, NULL, NULL, 0, NULL, NULL, 70, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3265)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3265, 1034, N'BODY', N'convert', N'STATIC', NULL, N'INTEGER', 1, N'0', NULL, 0, NULL, NULL, 80, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', N'FIX_VNPT_DATATYPE', '2026-08-10 15:23:58.460', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3266)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3266, 1034, N'BODY', N'pattern', N'CONFIG', N'PATTERN', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 90, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3267)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3267, 1034, N'BODY', N'serial', N'CONFIG', N'SERIAL', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 100, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3268)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3268, 1034, N'BODY', N'OldPattern', N'INVOICE', N'OldPattern', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 110, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3269)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3269, 1035, N'BODY', N'Account', N'CONFIG', N'ACCOUNT', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 10, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3270)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3270, 1035, N'BODY', N'ACpass', N'CONFIG', N'AC_PASSWORD', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 20, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3271)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3271, 1035, N'BODY', N'xmlInvData', N'DOCUMENT', N'VNPT_INVOICE_XML', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 30, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', N'CONFIG_VNPT_DOCUMENT', '2026-08-10 16:38:12.387', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3272)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3272, 1035, N'BODY', N'username', N'CONFIG', N'USERNAME', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 40, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3273)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3273, 1035, N'BODY', N'pass', N'CONFIG', N'PASSWORD', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 50, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3274)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3274, 1035, N'BODY', N'fkey', N'INVOICE', N'Fkey', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 60, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3275)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3275, 1035, N'BODY', N'Attachfile', N'INVOICE', N'AttachFile', N'STRING', 0, NULL, NULL, 0, NULL, NULL, 70, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3276)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3276, 1035, N'BODY', N'convert', N'STATIC', NULL, N'INTEGER', 1, N'0', NULL, 0, NULL, NULL, 80, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', N'FIX_VNPT_DATATYPE', '2026-08-10 15:23:58.460', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3277)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3277, 1035, N'BODY', N'pattern', N'CONFIG', N'PATTERN', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 90, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3278)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3278, 1035, N'BODY', N'serial', N'CONFIG', N'SERIAL', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 100, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3279)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3279, 1036, N'BODY', N'Account', N'CONFIG', N'ACCOUNT', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 10, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3280)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3280, 1036, N'BODY', N'ACpass', N'CONFIG', N'AC_PASSWORD', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 20, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3281)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3281, 1036, N'BODY', N'invXml', N'INVOICE', N'XmlInvData', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 30, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3282)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3282, 1036, N'BODY', N'userName', N'CONFIG', N'USERNAME', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 40, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3283)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3283, 1036, N'BODY', N'userPass', N'CONFIG', N'PASSWORD', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 50, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3284)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3284, 1036, N'BODY', N'oldPattern', N'INVOICE', N'OldPattern', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 60, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3285)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3285, 1036, N'BODY', N'oldSerial', N'INVOICE', N'OldSerial', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 70, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3286)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3286, 1036, N'BODY', N'oldNo', N'INVOICE', N'OldInvoiceNo', N'DECIMAL', 1, NULL, NULL, 0, NULL, NULL, 80, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3287)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3287, 1036, N'BODY', N'strOldArisingDate', N'INVOICE', N'OldArisingDate', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 90, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3288)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3288, 1036, N'BODY', N'convert', N'STATIC', NULL, N'INTEGER', 1, N'0', NULL, 0, NULL, NULL, 100, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', N'FIX_VNPT_DATATYPE', '2026-08-10 15:23:58.460', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3289)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3289, 1036, N'BODY', N'pattern', N'CONFIG', N'PATTERN', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 110, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3290)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3290, 1036, N'BODY', N'serial', N'CONFIG', N'SERIAL', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 120, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3291)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3291, 1036, N'BODY', N'relatedInvType', N'INVOICE', N'RelatedInvType', N'INTEGER', 1, NULL, NULL, 0, NULL, NULL, 130, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', N'FIX_VNPT_DATATYPE', '2026-08-10 15:23:58.460', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3292)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3292, 1036, N'BODY', N'feature', N'INVOICE', N'Feature', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 140, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3293)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3293, 1037, N'BODY', N'invToken', N'INVOICE', N'InvToken', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 10, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3294)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3294, 1037, N'BODY', N'userName', N'CONFIG', N'USERNAME', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 20, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3295)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3295, 1037, N'BODY', N'userPass', N'CONFIG', N'PASSWORD', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 30, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3296)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3296, 1038, N'BODY', N'invToken', N'INVOICE', N'InvToken', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 10, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3297)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3297, 1038, N'BODY', N'userName', N'CONFIG', N'USERNAME', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 20, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3298)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3298, 1038, N'BODY', N'userPass', N'CONFIG', N'PASSWORD', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 30, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3299)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3299, 1039, N'BODY', N'fkey', N'INVOICE', N'Fkey', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 10, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3300)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3300, 1039, N'BODY', N'userName', N'CONFIG', N'USERNAME', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 20, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3301)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3301, 1039, N'BODY', N'userPass', N'CONFIG', N'PASSWORD', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 30, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3302)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3302, 1040, N'BODY', N'fkey', N'INVOICE', N'Fkey', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 10, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3303)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3303, 1040, N'BODY', N'userName', N'CONFIG', N'USERNAME', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 20, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3304)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3304, 1040, N'BODY', N'userPass', N'CONFIG', N'PASSWORD', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 30, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3305)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3305, 1041, N'BODY', N'fkey', N'INVOICE', N'Fkey', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 10, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3306)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3306, 1041, N'BODY', N'userName', N'CONFIG', N'USERNAME', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 20, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3307)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3307, 1041, N'BODY', N'userPass', N'CONFIG', N'PASSWORD', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 30, 0, N'SQL_CONFIG_VNPT', '2026-08-10 14:27:53.933', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3308)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3308, 1024, N'RESPONSE', N'RawResult', N'RESPONSE', N'Envelope/Body/ImportAndPublishInvMTTResponse/ImportAndPublishInvMTTResult', N'STRING', 1, NULL, NULL, 0, NULL, NULL, 10, 0, N'CONFIG_VNPT_RESPONSE', '2026-08-11 11:31:22.813', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3309)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3309, 1024, N'RESPONSE', N'ErrorMessage', N'RESPONSE', N'Envelope/Body/ImportAndPublishInvMTTResponse/ImportAndPublishInvMTTResult', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 20, 0, N'CONFIG_VNPT_RESPONSE', '2026-08-11 11:34:33.313', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3310)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3310, 1024, N'RESPONSE', N'Pattern', N'RESPONSE', N'Envelope/Body/ImportAndPublishInvMTTResponse/ImportAndPublishInvMTTResult', N'STRING', 0, NULL, N'REGEX:^OK:(?<Pattern>[^;]+);[^-]+-.+-[^-]+$', 0, NULL, NULL, 90, 0, N'user_dev', '2026-08-13 09:51:35.263', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3311)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3311, 1024, N'RESPONSE', N'Serial', N'RESPONSE', N'Envelope/Body/ImportAndPublishInvMTTResponse/ImportAndPublishInvMTTResult', N'STRING', 0, NULL, N'REGEX:^OK:[^;]+;(?<Serial>[^-]+)-.+-[^-]+$', 0, NULL, NULL, 100, 0, N'user_dev', '2026-08-13 09:51:35.263', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3312)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3312, 1024, N'RESPONSE', N'Key', N'RESPONSE', N'Envelope/Body/ImportAndPublishInvMTTResponse/ImportAndPublishInvMTTResult', N'STRING', 0, NULL, N'REGEX:^OK:[^;]+;[^-]+-(?<Key>.+)-[^-]+$', 0, NULL, NULL, 110, 0, N'user_dev', '2026-08-13 09:51:35.263', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3313)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3313, 1024, N'RESPONSE', N'InvoiceNo', N'RESPONSE', N'Envelope/Body/ImportAndPublishInvMTTResponse/ImportAndPublishInvMTTResult', N'STRING', 0, NULL, N'REGEX:^OK:[^;]+;[^-]+-.+-(?<InvoiceNo>[^-]+)$', 0, NULL, NULL, 120, 0, N'user_dev', '2026-08-13 09:51:35.263', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3314)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3314, 1024, N'RESPONSE', N'KeySearch', N'RESPONSE', N'Envelope/Body/ImportAndPublishInvMTTResponse/ImportAndPublishInvMTTResult', N'STRING', 0, NULL, N'REGEX:^OK:[^;]+;[^-]+-(?<KeySearch>.+)-[^-]+$', 0, NULL, NULL, 130, 0, N'user_dev', '2026-08-13 09:51:35.263', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3315)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3315, 1023, N'RESPONSE', N'RawResult', N'RESPONSE', N'Envelope/Body/ImportInvByPatternResponse/ImportInvByPatternResult', N'STRING', 1, NULL, NULL, 1, NULL, NULL, 10, 0, NULL, '2026-08-13 14:56:35.783', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3316)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3316, 1023, N'RESPONSE', N'ErrorMessage', N'RESPONSE', N'Envelope/Body/ImportInvByPatternResponse/ImportInvByPatternResult', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 20, 0, NULL, '2026-08-13 14:56:35.783', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3317)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3317, 1023, N'RESPONSE', N'Pattern', N'RESPONSE', N'Envelope/Body/ImportInvByPatternResponse/ImportInvByPatternResult', N'STRING', 0, NULL, N'REGEX:^OK:(?<Pattern>[^;]+);', 1, NULL, NULL, 90, 0, NULL, '2026-08-13 14:56:35.783', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3318)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3318, 1023, N'RESPONSE', N'Serial', N'RESPONSE', N'Envelope/Body/ImportInvByPatternResponse/ImportInvByPatternResult', N'STRING', 0, NULL, N'REGEX:^OK:[^;]+;(?<Serial>[^-]+)-', 1, NULL, NULL, 100, 0, NULL, '2026-08-13 14:56:35.783', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3319)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3319, 1023, N'RESPONSE', N'InvoiceNo', N'RESPONSE', N'Envelope/Body/ImportInvByPatternResponse/ImportInvByPatternResult', N'STRING', 0, NULL, N'REGEX:^OK:[^;]+;[^-]+-(?<InvoiceNo>.+)$', 1, NULL, NULL, 110, 0, NULL, '2026-08-13 14:56:35.783', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3320)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3320, 1023, N'RESPONSE', N'Key', N'RESPONSE', N'Envelope/Body/ImportInvByPatternResponse/ImportInvByPatternResult', N'STRING', 0, NULL, N'REGEX:^OK:[^;]+;[^-]+-(?<Key>.+)$', 1, NULL, NULL, 120, 0, NULL, '2026-08-13 14:56:35.783', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3321)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3321, 1023, N'RESPONSE', N'KeySearch', N'RESPONSE', N'Envelope/Body/ImportInvByPatternResponse/ImportInvByPatternResult', N'STRING', 0, NULL, N'REGEX:^OK:[^;]+;[^-]+-(?<KeySearch>.+)$', 1, NULL, NULL, 130, 0, NULL, '2026-08-13 14:56:35.783', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3322)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3322, 1023, N'RESPONSE', N'ErrorCode', N'RESPONSE', N'Envelope/Body/ImportInvByPatternResponse/ImportInvByPatternResult', N'STRING', 0, NULL, N'REGEX:^ERR:(?<ErrorCode>.+)$', 1, NULL, NULL, 30, 0, NULL, '2026-08-13 15:06:41.687', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3323)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3323, 1026, N'RESPONSE', N'ErrorMessage', N'RESPONSE', N'Envelope/Body/PublishInvFkeyResponse/PublishInvFkeyResult', N'STRING', 0, NULL, NULL, 1, NULL, NULL, 20, 0, NULL, '2026-08-13 15:17:36.030', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3324)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3324, 1026, N'RESPONSE', N'InvoiceNo', N'RESPONSE', N'Envelope/Body/PublishInvFkeyResponse/PublishInvFkeyResult', N'STRING', 0, NULL, N'REGEX:OK:#(?<Key>[^_#|]+)_(?<InvoiceNo>[^#|]+)', 1, NULL, NULL, 120, 0, NULL, '2026-08-13 15:17:36.030', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3325)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3325, 1026, N'RESPONSE', N'Key', N'RESPONSE', N'Envelope/Body/PublishInvFkeyResponse/PublishInvFkeyResult', N'STRING', 0, NULL, N'REGEX:OK:#(?<Key>[^_#|]+)_(?<InvoiceNo>[^#|]+)', 1, NULL, NULL, 110, 0, NULL, '2026-08-13 15:17:36.030', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3326)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3326, 1026, N'RESPONSE', N'KeySearch', N'RESPONSE', N'Envelope/Body/PublishInvFkeyResponse/PublishInvFkeyResult', N'STRING', 0, NULL, N'REGEX:^OK:[^;]+;[^-]+-(?<KeySearch>.+)-[^-]+$', 1, NULL, NULL, 130, 0, NULL, '2026-08-13 15:17:36.030', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3327)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3327, 1026, N'RESPONSE', N'Pattern', N'RESPONSE', N'Envelope/Body/PublishInvFkeyResponse/PublishInvFkeyResult', N'STRING', 0, NULL, N'REGEX:^OK:(?<Pattern>[^;]+);[^-]+-.+-[^-]+$', 1, NULL, NULL, 90, 0, NULL, '2026-08-13 15:17:36.030', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3328)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3328, 1026, N'RESPONSE', N'RawResult', N'RESPONSE', N'Envelope/Body/PublishInvFkeyResponse/PublishInvFkeyResult', N'STRING', 1, NULL, NULL, 1, NULL, NULL, 10, 0, NULL, '2026-08-13 15:17:36.030', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3329)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3329, 1026, N'RESPONSE', N'Serial', N'RESPONSE', N'Envelope/Body/PublishInvFkeyResponse/PublishInvFkeyResult', N'STRING', 0, NULL, N'REGEX:^OK:[^;]+;(?<Serial>[^-]+)-.+-[^-]+$', 1, NULL, NULL, 100, 0, NULL, '2026-08-13 15:17:36.030', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderMapping WHERE ID = 3330)
    INSERT INTO dbo.EInvoiceProviderMapping (ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue) VALUES (3330, 1026, N'RESPONSE', N'ErrorCode', N'RESPONSE', N'Envelope/Body/PublishInvFkeyResponse/PublishInvFkeyResult', N'STRING', 0, NULL, N'REGEX:^ERR:(?<ErrorCode>.+)$', 1, NULL, NULL, 30, 0, NULL, '2026-08-13 15:17:55.423', NULL, NULL, NULL, NULL, NULL, NULL);
GO
SET IDENTITY_INSERT dbo.EInvoiceProviderMapping OFF;
GO
SET IDENTITY_INSERT dbo.EInvoiceProviderResponseRule ON;
GO
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderResponseRule WHERE ID = 1)
    INSERT INTO dbo.EInvoiceProviderResponseRule (ID, OperationID, RuleCode, SourcePath, Operator, ExpectedValue, ResultType, Priority, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate) VALUES (1, 1, N'VIETTEL_SUCCESS', N'errorCode', N'IS_NULL', NULL, N'SUCCESS', 10, 0, N'SYSTEM', '2026-08-07 14:18:26.640', NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderResponseRule WHERE ID = 3)
    INSERT INTO dbo.EInvoiceProviderResponseRule (ID, OperationID, RuleCode, SourcePath, Operator, ExpectedValue, ResultType, Priority, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate) VALUES (3, 1002, N'FAST_1002_SUCCESS', N'Success', N'EQUALS', N'1', N'SUCCESS', 10, 0, NULL, '2026-08-07 17:07:33.400', NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderResponseRule WHERE ID = 4)
    INSERT INTO dbo.EInvoiceProviderResponseRule (ID, OperationID, RuleCode, SourcePath, Operator, ExpectedValue, ResultType, Priority, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate) VALUES (4, 1005, N'FAST_1005_SUCCESS', N'Success', N'EQUALS', N'1', N'SUCCESS', 10, 0, NULL, '2026-08-07 17:20:41.373', NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderResponseRule WHERE ID = 5)
    INSERT INTO dbo.EInvoiceProviderResponseRule (ID, OperationID, RuleCode, SourcePath, Operator, ExpectedValue, ResultType, Priority, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate) VALUES (5, 1021, N'VIETTEL_POS_UPLOAD_SUCCESS', N'errorCode', N'IS_NULL', NULL, N'SUCCESS', 10, 0, NULL, '2026-08-08 09:21:30.663', NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderResponseRule WHERE ID = 6)
    INSERT INTO dbo.EInvoiceProviderResponseRule (ID, OperationID, RuleCode, SourcePath, Operator, ExpectedValue, ResultType, Priority, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate) VALUES (6, 1024, N'RESULT_ERROR', N'Envelope/Body/ImportAndPublishInvMTTResponse/ImportAndPublishInvMTTResult', N'STARTS_WITH', N'ERR:', N'FAILURE', 10, 0, N'CONFIG_VNPT_RESPONSE', '2026-08-11 11:34:47.890', NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderResponseRule WHERE ID = 7)
    INSERT INTO dbo.EInvoiceProviderResponseRule (ID, OperationID, RuleCode, SourcePath, Operator, ExpectedValue, ResultType, Priority, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate) VALUES (7, 1024, N'RESULT_SUCCESS', N'Envelope/Body/ImportAndPublishInvMTTResponse/ImportAndPublishInvMTTResult', N'STARTS_WITH', N'OK:', N'SUCCESS', 20, 0, N'CONFIG_VNPT_RESPONSE', '2026-08-11 11:34:57.733', NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderResponseRule WHERE ID = 8)
    INSERT INTO dbo.EInvoiceProviderResponseRule (ID, OperationID, RuleCode, SourcePath, Operator, ExpectedValue, ResultType, Priority, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate) VALUES (8, 1023, N'RESULT_ERROR', N'Envelope/Body/ImportInvByPatternResponse/ImportInvByPatternResult', N'STARTS_WITH', N'ERR:', N'FAILURE', 10, 0, NULL, '2026-08-13 14:56:58.013', NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderResponseRule WHERE ID = 9)
    INSERT INTO dbo.EInvoiceProviderResponseRule (ID, OperationID, RuleCode, SourcePath, Operator, ExpectedValue, ResultType, Priority, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate) VALUES (9, 1023, N'RESULT_SUCCESS', N'Envelope/Body/ImportInvByPatternResponse/ImportInvByPatternResult', N'STARTS_WITH', N'OK:', N'SUCCESS', 20, 0, NULL, '2026-08-13 14:56:58.013', NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderResponseRule WHERE ID = 10)
    INSERT INTO dbo.EInvoiceProviderResponseRule (ID, OperationID, RuleCode, SourcePath, Operator, ExpectedValue, ResultType, Priority, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate) VALUES (10, 1026, N'RESULT_ERROR', N'Envelope/Body/PublishInvFkeyResponse/PublishInvFkeyResult', N'NOT_CONTAINS', N'OK:', N'FAILURE', 10, 0, NULL, '2026-08-13 15:18:13.550', NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderResponseRule WHERE ID = 11)
    INSERT INTO dbo.EInvoiceProviderResponseRule (ID, OperationID, RuleCode, SourcePath, Operator, ExpectedValue, ResultType, Priority, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate) VALUES (11, 1026, N'RESULT_SUCCESS', N'Envelope/Body/PublishInvFkeyResponse/PublishInvFkeyResult', N'CONTAINS', N'OK:', N'SUCCESS', 20, 0, NULL, '2026-08-13 15:18:13.550', NULL, NULL);
GO
SET IDENTITY_INSERT dbo.EInvoiceProviderResponseRule OFF;
GO
SET IDENTITY_INSERT dbo.EInvoiceProviderBodyField ON;
GO
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1, 1, N'CREATE_INVOICE', N'supplierTaxCode', N'CONFIG', N'SUPPLIER_TAX_CODE', N'STRING', 1, NULL, NULL, 1, 10, 0, NULL, '2026-07-20 14:48:55.300', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 2)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (2, 1, N'CREATE_INVOICE', N'templateCode', N'CONFIG', N'TEMPLATE_CODE', N'STRING', 1, NULL, NULL, 1, 20, 0, NULL, '2026-07-20 14:48:55.300', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 3)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (3, 1, N'CREATE_INVOICE', N'invoiceSeries', N'CONFIG', N'INVOICE_SERIES', N'STRING', 1, NULL, NULL, 1, 30, 0, NULL, '2026-07-20 14:48:55.300', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 4)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (4, 1, N'CREATE_INVOICE', N'invoiceDate', N'INVOICE', N'InvoiceDate', N'DATE', 1, NULL, N'yyyy-MM-dd', 1, 40, 0, NULL, '2026-07-20 14:48:55.300', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 5)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (5, 1, N'CREATE_INVOICE', N'currencyCode', N'INVOICE', N'CurrencyCode', N'STRING', 0, N'VND', NULL, 1, 50, 0, NULL, '2026-07-20 14:48:55.300', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 6)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (6, 1, N'CREATE_INVOICE', N'paymentMethod', N'INVOICE', N'PaymentMethod', N'STRING', 0, N'TM/CK', NULL, 1, 60, 0, NULL, '2026-07-20 14:48:55.300', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 7)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (7, 1, N'CREATE_INVOICE', N'buyer.name', N'INVOICE', N'CustomerName', N'STRING', 1, NULL, NULL, 1, 70, 0, NULL, '2026-07-20 14:48:55.300', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 8)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (8, 1, N'CREATE_INVOICE', N'buyer.companyName', N'INVOICE', N'CustomerCompany', N'STRING', 0, NULL, NULL, 1, 80, 0, NULL, '2026-07-20 14:48:55.300', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 9)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (9, 1, N'CREATE_INVOICE', N'buyer.taxCode', N'INVOICE', N'CustomerTaxCode', N'STRING', 0, NULL, NULL, 1, 90, 0, NULL, '2026-07-20 14:48:55.300', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 10)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (10, 1, N'CREATE_INVOICE', N'buyer.address', N'INVOICE', N'CustomerAddress', N'STRING', 0, NULL, NULL, 1, 100, 0, NULL, '2026-07-20 14:48:55.300', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 11)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (11, 1, N'CREATE_INVOICE', N'buyer.email', N'INVOICE', N'CustomerEmail', N'STRING', 0, NULL, NULL, 1, 110, 0, NULL, '2026-07-20 14:48:55.300', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 12)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (12, 1, N'CREATE_INVOICE', N'total.amountBeforeTax', N'INVOICE', N'AmountBeforeTax', N'DECIMAL', 1, NULL, NULL, 1, 120, 0, NULL, '2026-07-20 14:48:55.300', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 13)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (13, 1, N'CREATE_INVOICE', N'total.taxAmount', N'INVOICE', N'TaxAmount', N'DECIMAL', 1, NULL, NULL, 1, 130, 0, NULL, '2026-07-20 14:48:55.300', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 14)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (14, 1, N'CREATE_INVOICE', N'total.totalAmount', N'INVOICE', N'TotalAmount', N'DECIMAL', 1, NULL, NULL, 1, 140, 0, NULL, '2026-07-20 14:48:55.300', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 15)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (15, 1, N'CREATE_INVOICE', N'requestId', N'SYSTEM', N'GUID', N'STRING', 1, NULL, NULL, 1, 150, 0, NULL, '2026-07-20 14:48:55.300', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1002)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1002, 4, N'VNPT_INVOICE_XML', N'HDon.key', N'INVOICE', N'SourceRecordID', N'STRING', 1, NULL, NULL, 0, 10, 0, N'CONFIG_VNPT_XML', '2026-08-10 16:37:32.487', N'user_dev', '2026-08-13 11:37:26.980', N'XML', N'DSHDon', NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1003)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1003, 4, N'VNPT_INVOICE_XML', N'HDon.DLHDon.TTChung.NLap', N'INVOICE', N'InvoiceDate', N'DATE', 1, NULL, N'yyyy-MM-dd', 0, 20, 0, N'CONFIG_VNPT_XML', '2026-08-10 16:37:32.487', NULL, NULL, N'XML', N'DSHDon', NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1004)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1004, 4, N'VNPT_INVOICE_XML', N'HDon.DLHDon.TTChung.DVTTe', N'INVOICE', N'CurrencyCode', N'STRING', 1, N'VND', NULL, 0, 30, 0, N'CONFIG_VNPT_XML', '2026-08-10 16:37:32.487', NULL, NULL, N'XML', N'DSHDon', NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1005)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1005, 4, N'VNPT_INVOICE_XML', N'HDon.DLHDon.TTChung.HTTToan', N'INVOICE', N'PaymentMethod', N'STRING', 0, N'TM/CK', NULL, 0, 40, 0, N'CONFIG_VNPT_XML', '2026-08-10 16:37:32.487', NULL, NULL, N'XML', N'DSHDon', NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1006)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1006, 4, N'VNPT_INVOICE_XML', N'HDon.DLHDon.NDHDon.NMua.Ten', N'INVOICE', N'CustomerName', N'STRING', 0, NULL, NULL, 0, 50, 0, N'CONFIG_VNPT_XML', '2026-08-10 16:37:32.487', NULL, NULL, N'XML', N'DSHDon', NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1007)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1007, 4, N'VNPT_INVOICE_XML', N'HDon.DLHDon.NDHDon.NMua.MST', N'INVOICE', N'CustomerTaxCode', N'STRING', 0, NULL, NULL, 0, 60, 0, N'CONFIG_VNPT_XML', '2026-08-10 16:37:32.487', NULL, NULL, N'XML', N'DSHDon', NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1008)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1008, 4, N'VNPT_INVOICE_XML', N'HDon.DLHDon.NDHDon.NMua.DChi', N'INVOICE', N'CustomerAddress', N'STRING', 0, NULL, NULL, 0, 70, 0, N'CONFIG_VNPT_XML', '2026-08-10 16:37:32.487', NULL, NULL, N'XML', N'DSHDon', NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1009)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1009, 4, N'VNPT_INVOICE_XML', N'STT', N'COLLECTION_ITEM', N'LineNumber', N'INTEGER', 1, NULL, NULL, 0, 100, 0, N'CONFIG_VNPT_XML', '2026-08-11 09:50:08.853', NULL, NULL, N'XML', N'DSHDon', N'Items', N'HDon.DLHDon.NDHDon.DSHHDVu.HHDVu');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1010)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1010, 4, N'VNPT_INVOICE_XML', N'MHHDVu', N'COLLECTION_ITEM', N'ItemCode', N'STRING', 0, NULL, NULL, 0, 110, 0, N'CONFIG_VNPT_XML', '2026-08-11 09:50:08.853', NULL, NULL, N'XML', N'DSHDon', N'Items', N'HDon.DLHDon.NDHDon.DSHHDVu.HHDVu');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1011)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1011, 4, N'VNPT_INVOICE_XML', N'THHDVu', N'COLLECTION_ITEM', N'ItemName', N'STRING', 1, NULL, NULL, 0, 120, 0, N'CONFIG_VNPT_XML', '2026-08-11 09:50:08.853', NULL, NULL, N'XML', N'DSHDon', N'Items', N'HDon.DLHDon.NDHDon.DSHHDVu.HHDVu');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1012)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1012, 4, N'VNPT_INVOICE_XML', N'DVTinh', N'COLLECTION_ITEM', N'UnitName', N'STRING', 0, NULL, NULL, 0, 130, 0, N'CONFIG_VNPT_XML', '2026-08-11 09:50:08.853', NULL, NULL, N'XML', N'DSHDon', N'Items', N'HDon.DLHDon.NDHDon.DSHHDVu.HHDVu');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1013)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1013, 4, N'VNPT_INVOICE_XML', N'SLuong', N'COLLECTION_ITEM', N'Quantity', N'DECIMAL', 1, NULL, NULL, 0, 140, 0, N'CONFIG_VNPT_XML', '2026-08-11 09:50:08.853', NULL, NULL, N'XML', N'DSHDon', N'Items', N'HDon.DLHDon.NDHDon.DSHHDVu.HHDVu');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1014)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1014, 4, N'VNPT_INVOICE_XML', N'DGia', N'COLLECTION_ITEM', N'UnitPrice', N'DECIMAL', 1, NULL, NULL, 0, 150, 0, N'CONFIG_VNPT_XML', '2026-08-11 09:50:08.853', N'CONFIG', '2026-08-12 11:09:46.143', N'XML', N'DSHDon', N'Items', N'HDon.DLHDon.NDHDon.DSHHDVu.HHDVu');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1015)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1015, 4, N'VNPT_INVOICE_XML', N'ThTien', N'COLLECTION_ITEM', N'TaxableAmount', N'DECIMAL', 1, NULL, NULL, 0, 160, 0, N'CONFIG_VNPT_XML', '2026-08-11 09:50:08.853', NULL, NULL, N'XML', N'DSHDon', N'Items', N'HDon.DLHDon.NDHDon.DSHHDVu.HHDVu');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1016)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1016, 4, N'VNPT_INVOICE_XML', N'TSuat', N'COLLECTION_ITEM', N'VATRate', N'DECIMAL', 1, NULL, NULL, 0, 170, 0, N'CONFIG_VNPT_XML', '2026-08-11 09:50:08.853', NULL, NULL, N'XML', N'DSHDon', N'Items', N'HDon.DLHDon.NDHDon.DSHHDVu.HHDVu');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1017)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1017, 4, N'VNPT_INVOICE_XML', N'TThue', N'COLLECTION_ITEM', N'TaxAmount', N'DECIMAL', 0, NULL, NULL, 1, 180, 0, N'CONFIG', '2026-08-11 15:22:47.810', N'CONFIG', '2026-08-11 17:20:25.887', NULL, NULL, N'Items', N'HDon.DLHDon.NDHDon.DSHHDVu.HHDVu');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1018)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1018, 4, N'VNPT_INVOICE_XML', N'TSThue', N'COLLECTION_ITEM', N'TotalAmount', N'DECIMAL', 0, NULL, NULL, 1, 190, 0, N'CONFIG', '2026-08-11 15:22:47.810', N'CONFIG', '2026-08-11 17:20:25.887', NULL, NULL, N'Items', N'HDon.DLHDon.NDHDon.DSHHDVu.HHDVu');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1020)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1020, 4, N'VNPT_INVOICE_XML', N'TSuat', N'COLLECTION_ITEM', N'TaxPercentage', N'DECIMAL', 0, NULL, NULL, 1, 300, 0, N'CONFIG', '2026-08-11 15:36:53.953', N'CONFIG', '2026-08-11 17:20:25.887', NULL, NULL, N'TaxBreakdowns', N'HDon.DLHDon.NDHDon.TToan.THTTLTSuat.LTSuat');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1021)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1021, 4, N'VNPT_INVOICE_XML', N'TThue', N'COLLECTION_ITEM', N'TaxAmount', N'DECIMAL', 0, NULL, NULL, 1, 310, 0, N'CONFIG', '2026-08-11 15:36:53.953', N'CONFIG', '2026-08-11 17:20:25.887', NULL, NULL, N'TaxBreakdowns', N'HDon.DLHDon.NDHDon.TToan.THTTLTSuat.LTSuat');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1022)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1022, 4, N'VNPT_INVOICE_XML', N'ThTien', N'COLLECTION_ITEM', N'TaxableAmount', N'DECIMAL', 0, NULL, NULL, 1, 320, 0, N'CONFIG', '2026-08-11 15:36:53.953', N'CONFIG', '2026-08-11 17:20:25.887', NULL, NULL, N'TaxBreakdowns', N'HDon.DLHDon.NDHDon.TToan.THTTLTSuat.LTSuat');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1023)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1023, 4, N'VNPT_INVOICE_XML', N'HDon.DLHDon.NDHDon.TToan.TgTCThue', N'INVOICE', N'TaxableAmount', N'DECIMAL', 0, NULL, NULL, 1, 400, 0, N'CONFIG', '2026-08-11 15:37:55.367', N'CONFIG', '2026-08-11 17:20:25.887', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1024)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1024, 4, N'VNPT_INVOICE_XML', N'HDon.DLHDon.NDHDon.TToan.TgTThue', N'INVOICE', N'TaxAmount', N'DECIMAL', 0, NULL, NULL, 1, 410, 0, N'CONFIG', '2026-08-11 15:38:02.980', N'CONFIG', '2026-08-11 17:20:25.887', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1025)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1025, 4, N'VNPT_INVOICE_XML', N'HDon.DLHDon.NDHDon.TToan.TgTTTBSo', N'INVOICE', N'TotalAmount', N'DECIMAL', 0, NULL, NULL, 1, 430, 0, N'CONFIG', '2026-08-11 15:38:10.943', N'CONFIG', '2026-08-11 17:20:25.887', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1026)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1026, 4, N'VNPT_INVOICE_XML', N'HDon.DLHDon.NDHDon.TToan.TgTTTBChu', N'INVOICE', N'AmountInWords', N'STRING', 0, NULL, NULL, 1, 440, 0, N'CONFIG', '2026-08-11 15:38:19.303', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1027)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1027, 4, N'VNPT_INVOICE_XML', N'HDon.DLHDon.NDHDon.TToan.TTCKTMai', N'INVOICE', N'DiscountAmount', N'DECIMAL', 0, NULL, NULL, 1, 420, 0, N'CONFIG', '2026-08-11 16:15:27.677', N'CONFIG', '2026-08-11 17:20:25.887', NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1028)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1028, 4, N'VNPT_INVOICE_XML', N'HDon.DLHDon.NDHDon.NMua.SDThoai', N'INVOICE', N'CustomerPhone', N'STRING', 0, NULL, NULL, 1, 80, 0, N'CONFIG', '2026-08-11 16:19:04.967', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1029)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1029, 4, N'VNPT_INVOICE_XML', N'HDon.DLHDon.NDHDon.NMua.DCTDTu', N'INVOICE', N'CustomerEmail', N'STRING', 0, NULL, NULL, 1, 90, 0, N'CONFIG', '2026-08-11 16:19:04.967', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1030)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1030, 4, N'VNPT_INVOICE_XML', N'HDon.DLHDon.NDHDon.NMua.FolioNo', N'INVOICE', N'FolioNo', N'STRING', 0, NULL, NULL, 1, 91, 0, N'CONFIG', '2026-08-11 16:42:21.347', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1031)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1031, 4, N'VNPT_INVOICE_XML', N'HDon.DLHDon.NDHDon.NMua.RoomNo', N'INVOICE', N'RoomNo', N'STRING', 0, NULL, NULL, 1, 92, 0, N'CONFIG', '2026-08-11 16:42:21.347', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1032)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1032, 4, N'VNPT_INVOICE_XML', N'TChat', N'COLLECTION_ITEM', N'ItemType', N'INTEGER', 1, N'1', NULL, 0, 95, 0, NULL, '2026-08-12 14:41:31.617', NULL, NULL, N'XML', N'DSHDon', N'Items', N'HDon.DLHDon.NDHDon.DSHHDVu.HHDVu');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1062)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1062, 4, N'VNPT_POS_INVOICE_XML', N'HDon.key', N'INVOICE', N'SourceRecordID', N'STRING', 1, NULL, NULL, 1, 10, 0, NULL, '2026-08-13 14:44:48.833', NULL, NULL, N'XML', N'DSHDon', NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1063)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1063, 4, N'VNPT_POS_INVOICE_XML', N'HDon.DLHDon.TTChung.NLap', N'INVOICE', N'InvoiceDate', N'DATE', 1, NULL, N'yyyy-MM-dd', 1, 20, 0, NULL, '2026-08-13 14:44:48.833', NULL, NULL, N'XML', N'DSHDon', NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1064)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1064, 4, N'VNPT_POS_INVOICE_XML', N'HDon.DLHDon.TTChung.DVTTe', N'INVOICE', N'CurrencyCode', N'STRING', 1, N'VND', NULL, 1, 30, 0, NULL, '2026-08-13 14:44:48.833', NULL, NULL, N'XML', N'DSHDon', NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1065)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1065, 4, N'VNPT_POS_INVOICE_XML', N'HDon.DLHDon.TTChung.HTTToan', N'INVOICE', N'PaymentMethod', N'STRING', 0, N'TM/CK', NULL, 1, 40, 0, NULL, '2026-08-13 14:44:48.833', NULL, NULL, N'XML', N'DSHDon', NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1066)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1066, 4, N'VNPT_POS_INVOICE_XML', N'HDon.DLHDon.NDHDon.NMua.Ten', N'INVOICE', N'CustomerName', N'STRING', 0, NULL, NULL, 1, 50, 0, NULL, '2026-08-13 14:44:48.833', NULL, NULL, N'XML', N'DSHDon', NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1067)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1067, 4, N'VNPT_POS_INVOICE_XML', N'HDon.DLHDon.NDHDon.NMua.MST', N'INVOICE', N'CustomerTaxCode', N'STRING', 0, NULL, NULL, 1, 60, 0, NULL, '2026-08-13 14:44:48.833', NULL, NULL, N'XML', N'DSHDon', NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1068)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1068, 4, N'VNPT_POS_INVOICE_XML', N'HDon.DLHDon.NDHDon.NMua.DChi', N'INVOICE', N'CustomerAddress', N'STRING', 1, N'Không cung cấp', NULL, 1, 70, 0, NULL, '2026-08-13 14:44:48.833', NULL, NULL, N'XML', N'DSHDon', NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1069)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1069, 4, N'VNPT_POS_INVOICE_XML', N'STT', N'COLLECTION_ITEM', N'LineNumber', N'INTEGER', 1, NULL, NULL, 1, 100, 0, NULL, '2026-08-13 14:44:48.833', NULL, NULL, N'XML', N'DSHDon', N'Items', N'HDon.DLHDon.NDHDon.DSHHDVu.HHDVu');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1070)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1070, 4, N'VNPT_POS_INVOICE_XML', N'MHHDVu', N'COLLECTION_ITEM', N'ItemCode', N'STRING', 0, NULL, NULL, 1, 110, 0, NULL, '2026-08-13 14:44:48.833', NULL, NULL, N'XML', N'DSHDon', N'Items', N'HDon.DLHDon.NDHDon.DSHHDVu.HHDVu');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1071)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1071, 4, N'VNPT_POS_INVOICE_XML', N'THHDVu', N'COLLECTION_ITEM', N'ItemName', N'STRING', 1, NULL, NULL, 1, 120, 0, NULL, '2026-08-13 14:44:48.833', NULL, NULL, N'XML', N'DSHDon', N'Items', N'HDon.DLHDon.NDHDon.DSHHDVu.HHDVu');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1072)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1072, 4, N'VNPT_POS_INVOICE_XML', N'DVTinh', N'COLLECTION_ITEM', N'UnitName', N'STRING', 0, NULL, NULL, 1, 130, 0, NULL, '2026-08-13 14:44:48.833', NULL, NULL, N'XML', N'DSHDon', N'Items', N'HDon.DLHDon.NDHDon.DSHHDVu.HHDVu');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1073)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1073, 4, N'VNPT_POS_INVOICE_XML', N'SLuong', N'COLLECTION_ITEM', N'Quantity', N'DECIMAL', 1, NULL, NULL, 1, 140, 0, NULL, '2026-08-13 14:44:48.833', NULL, NULL, N'XML', N'DSHDon', N'Items', N'HDon.DLHDon.NDHDon.DSHHDVu.HHDVu');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1074)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1074, 4, N'VNPT_POS_INVOICE_XML', N'DGia', N'COLLECTION_ITEM', N'UnitPrice', N'DECIMAL', 1, NULL, NULL, 1, 150, 0, NULL, '2026-08-13 14:44:48.833', NULL, NULL, N'XML', N'DSHDon', N'Items', N'HDon.DLHDon.NDHDon.DSHHDVu.HHDVu');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1075)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1075, 4, N'VNPT_POS_INVOICE_XML', N'ThTien', N'COLLECTION_ITEM', N'TaxableAmount', N'DECIMAL', 1, NULL, NULL, 1, 160, 0, NULL, '2026-08-13 14:44:48.833', NULL, NULL, N'XML', N'DSHDon', N'Items', N'HDon.DLHDon.NDHDon.DSHHDVu.HHDVu');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1076)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1076, 4, N'VNPT_POS_INVOICE_XML', N'TSuat', N'COLLECTION_ITEM', N'VATRate', N'DECIMAL', 1, NULL, NULL, 1, 170, 0, NULL, '2026-08-13 14:44:48.833', NULL, NULL, N'XML', N'DSHDon', N'Items', N'HDon.DLHDon.NDHDon.DSHHDVu.HHDVu');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1077)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1077, 4, N'VNPT_POS_INVOICE_XML', N'TThue', N'COLLECTION_ITEM', N'TaxAmount', N'DECIMAL', 0, NULL, NULL, 1, 180, 0, NULL, '2026-08-13 14:44:48.833', NULL, NULL, NULL, NULL, N'Items', N'HDon.DLHDon.NDHDon.DSHHDVu.HHDVu');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1078)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1078, 4, N'VNPT_POS_INVOICE_XML', N'TSThue', N'COLLECTION_ITEM', N'TotalAmount', N'DECIMAL', 0, NULL, NULL, 1, 190, 0, NULL, '2026-08-13 14:44:48.833', NULL, NULL, NULL, NULL, N'Items', N'HDon.DLHDon.NDHDon.DSHHDVu.HHDVu');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1079)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1079, 4, N'VNPT_POS_INVOICE_XML', N'TSuat', N'COLLECTION_ITEM', N'TaxPercentage', N'DECIMAL', 0, NULL, NULL, 1, 300, 0, NULL, '2026-08-13 14:44:48.833', NULL, NULL, NULL, NULL, N'TaxBreakdowns', N'HDon.DLHDon.NDHDon.TToan.THTTLTSuat.LTSuat');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1080)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1080, 4, N'VNPT_POS_INVOICE_XML', N'TThue', N'COLLECTION_ITEM', N'TaxAmount', N'DECIMAL', 0, NULL, NULL, 1, 310, 0, NULL, '2026-08-13 14:44:48.833', NULL, NULL, NULL, NULL, N'TaxBreakdowns', N'HDon.DLHDon.NDHDon.TToan.THTTLTSuat.LTSuat');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1081)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1081, 4, N'VNPT_POS_INVOICE_XML', N'ThTien', N'COLLECTION_ITEM', N'TaxableAmount', N'DECIMAL', 0, NULL, NULL, 1, 320, 0, NULL, '2026-08-13 14:44:48.833', NULL, NULL, NULL, NULL, N'TaxBreakdowns', N'HDon.DLHDon.NDHDon.TToan.THTTLTSuat.LTSuat');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1082)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1082, 4, N'VNPT_POS_INVOICE_XML', N'HDon.DLHDon.NDHDon.TToan.TgTCThue', N'INVOICE', N'TaxableAmount', N'DECIMAL', 0, NULL, NULL, 1, 400, 0, NULL, '2026-08-13 14:44:48.833', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1083)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1083, 4, N'VNPT_POS_INVOICE_XML', N'HDon.DLHDon.NDHDon.TToan.TgTThue', N'INVOICE', N'TaxAmount', N'DECIMAL', 0, NULL, NULL, 1, 410, 0, NULL, '2026-08-13 14:44:48.833', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1084)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1084, 4, N'VNPT_POS_INVOICE_XML', N'HDon.DLHDon.NDHDon.TToan.TgTTTBSo', N'INVOICE', N'TotalAmount', N'DECIMAL', 0, NULL, NULL, 1, 430, 0, NULL, '2026-08-13 14:44:48.833', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1085)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1085, 4, N'VNPT_POS_INVOICE_XML', N'HDon.DLHDon.NDHDon.TToan.TgTTTBChu', N'INVOICE', N'AmountInWords', N'STRING', 0, NULL, NULL, 1, 440, 0, NULL, '2026-08-13 14:44:48.833', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1086)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1086, 4, N'VNPT_POS_INVOICE_XML', N'HDon.DLHDon.NDHDon.TToan.TTCKTMai', N'INVOICE', N'DiscountAmount', N'DECIMAL', 0, NULL, NULL, 1, 420, 0, NULL, '2026-08-13 14:44:48.833', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1087)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1087, 4, N'VNPT_POS_INVOICE_XML', N'HDon.DLHDon.NDHDon.NMua.SDThoai', N'INVOICE', N'CustomerPhone', N'STRING', 0, NULL, NULL, 1, 80, 0, NULL, '2026-08-13 14:44:48.833', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1088)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1088, 4, N'VNPT_POS_INVOICE_XML', N'HDon.DLHDon.NDHDon.NMua.DCTDTu', N'INVOICE', N'CustomerEmail', N'STRING', 0, NULL, NULL, 1, 90, 0, NULL, '2026-08-13 14:44:48.833', NULL, NULL, NULL, NULL, NULL, NULL);
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1089)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1089, 4, N'VNPT_POS_INVOICE_XML', N'TChat', N'COLLECTION_ITEM', N'ItemType', N'INTEGER', 1, N'1', NULL, 1, 95, 0, NULL, '2026-08-13 14:44:48.833', NULL, NULL, N'XML', N'DSHDon', N'Items', N'HDon.DLHDon.NDHDon.DSHHDVu.HHDVu');
IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProviderBodyField WHERE ID = 1090)
    INSERT INTO dbo.EInvoiceProviderBodyField (ID, ProviderID, OperationCode, JsonPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue, FormatString, OmitWhenNull, DisplayOrder, Inactive, CreatedBy, CreatedDate, UpdatedBy, UpdatedDate, DocumentType, RootName, CollectionName, ItemPath) VALUES (1090, 4, N'VNPT_POS_INVOICE_XML', N'HDon.DLHDon.TTChung.HDCTTChinh', N'STATIC', NULL, N'INTEGER', 1, N'0', NULL, 1, 35, 0, NULL, '2026-08-13 14:44:48.833', NULL, NULL, N'XML', N'DSHDon', NULL, NULL);
GO
SET IDENTITY_INSERT dbo.EInvoiceProviderBodyField OFF;
GO

/* ------------------------------------------------------------------------------------------------
   7. STORED PROCEDURE - GIAI ĐOẠN 3: Property E-Invoice (kết nối DB động + giá trị cấu hình Provider)
      Mật khẩu DB và giá trị field Encrypted được ứng dụng mã hóa (AES, prefix "enc:") trước khi lưu;
      SP chỉ lưu chuỗi đã mã hóa và không bao giờ trả mật khẩu ra ngoài (chỉ có cờ HasDatabasePassword).
   ------------------------------------------------------------------------------------------------ */

-- Danh sách Property (@ID = 0: tất cả; @ID > 0: một Property). Kèm Provider mặc định. KHÔNG trả mật khẩu.
IF OBJECT_ID(N'dbo.spEInvoice_PropertySearch', N'P') IS NOT NULL DROP PROCEDURE dbo.spEInvoice_PropertySearch;
GO
CREATE PROCEDURE dbo.spEInvoice_PropertySearch
    @ID int = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.ID, p.Code, p.Name, p.Description, p.SourceType, p.TaxCode,
           p.Database_IP, p.Database_Name, p.Database_User,
           CAST(CASE WHEN ISNULL(p.Database_Password, N'') <> N'' THEN 1 ELSE 0 END AS bit) AS HasDatabasePassword,
           p.InvoiceSearchSP, p.InvoiceDetailSP, ISNULL(p.Inactive, 0) AS Inactive,
           dp.ProviderID, dp.ProviderCode, dp.ProviderName,
           p.CreatedBy, p.CreatedDate, p.UpdatedBy, p.UpdatedDate
    FROM dbo.Property_Einvoice p WITH (NOLOCK)
    OUTER APPLY (
        SELECT TOP 1 pp.ProviderID, ep.Code AS ProviderCode, ep.Name AS ProviderName
        FROM dbo.Property_EInvoiceProvider pp WITH (NOLOCK)
        INNER JOIN dbo.EInvoiceProvider ep WITH (NOLOCK) ON ep.ID = pp.ProviderID
        WHERE pp.PropertyID = p.ID AND pp.Inactive = 0 AND pp.IsDefault = 1
        ORDER BY pp.ID DESC
    ) dp
    WHERE @ID = 0 OR p.ID = @ID
    ORDER BY p.ID DESC;
END
GO

-- Thông tin kết nối thật của Property (cả mật khẩu đã mã hóa) - CHỈ dùng nội bộ ở server (test kết nối, engine chạy hóa đơn)
IF OBJECT_ID(N'dbo.spEInvoice_PropertyConnection', N'P') IS NOT NULL DROP PROCEDURE dbo.spEInvoice_PropertyConnection;
GO
CREATE PROCEDURE dbo.spEInvoice_PropertyConnection
    @ID int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, Code, Name, SourceType, Database_IP, Database_Name, Database_User, Database_Password,
           InvoiceSearchSP, InvoiceDetailSP, ISNULL(Inactive, 0) AS Inactive
    FROM dbo.Property_Einvoice WITH (NOLOCK)
    WHERE ID = @ID;
END
GO

-- Field của một Provider kèm giá trị đã lưu của Property (nếu Property đang dùng đúng Provider đó)
IF OBJECT_ID(N'dbo.spEInvoice_PropertyFieldValues', N'P') IS NOT NULL DROP PROCEDURE dbo.spEInvoice_PropertyFieldValues;
GO
CREATE PROCEDURE dbo.spEInvoice_PropertyFieldValues
    @PropertyID int,
    @ProviderID int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT f.ID AS ProviderFieldID, f.ProviderID, f.FieldCode, f.DisplayName, f.Description, f.DataType, f.ControlType,
           f.IsRequired, f.IsEncrypted, f.IsReadOnly, f.DefaultValue, f.Placeholder, f.MaxLength, f.RegexPattern,
           f.GroupName, f.DisplayOrder, f.ColumnWidth, f.VisibleWhenFieldCode, f.VisibleWhenOperator, f.VisibleWhenValue,
           v.FieldValue AS SavedValue,
           CAST(CASE WHEN ISNULL(v.FieldValue, N'') <> N'' THEN 1 ELSE 0 END AS bit) AS HasSavedValue
    FROM dbo.EInvoiceProviderField f WITH (NOLOCK)
    LEFT JOIN dbo.Property_EInvoiceProvider pp WITH (NOLOCK)
           ON pp.PropertyID = @PropertyID AND pp.ProviderID = f.ProviderID AND pp.Inactive = 0
    LEFT JOIN dbo.Property_EInvoiceProviderValue v WITH (NOLOCK)
           ON v.PropertyProviderID = pp.ID AND v.ProviderFieldID = f.ID
    WHERE f.ProviderID = @ProviderID AND f.Inactive = 0
    ORDER BY ISNULL(f.GroupName, N''), f.DisplayOrder, f.ID;
END
GO

-- Option (đang dùng) của tất cả field thuộc một Provider
IF OBJECT_ID(N'dbo.spEInvoice_OptionsByProvider', N'P') IS NOT NULL DROP PROCEDURE dbo.spEInvoice_OptionsByProvider;
GO
CREATE PROCEDURE dbo.spEInvoice_OptionsByProvider
    @ProviderID int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT o.ID, o.ProviderFieldID, o.OptionValue, o.OptionText, o.IsDefault, o.DisplayOrder
    FROM dbo.EInvoiceProviderFieldOption o WITH (NOLOCK)
    INNER JOIN dbo.EInvoiceProviderField f WITH (NOLOCK) ON f.ID = o.ProviderFieldID
    WHERE f.ProviderID = @ProviderID AND f.Inactive = 0 AND o.Inactive = 0
    ORDER BY o.ProviderFieldID, o.DisplayOrder, o.ID;
END
GO

-- Thêm (@ID = 0) hoặc sửa Property cùng Provider và giá trị field, trong MỘT transaction. Trả về ID.
--   @DatabasePassword : đã mã hóa; NULL = giữ nguyên mật khẩu cũ.
--   @Values : <values><v id="ProviderFieldID" keep="0|1">giá trị đã mã hóa nếu field Encrypted</v>...</values>
--             keep = 1: giữ nguyên giá trị đã lưu (không ghi).
IF OBJECT_ID(N'dbo.spEInvoice_PropertySave', N'P') IS NOT NULL DROP PROCEDURE dbo.spEInvoice_PropertySave;
GO
CREATE PROCEDURE dbo.spEInvoice_PropertySave
    @ID               int,
    @Code             nvarchar(50),
    @Name             nvarchar(100),
    @Description      nvarchar(100) = NULL,
    @SourceType       varchar(10),
    @TaxCode          nvarchar(50) = NULL,
    @DatabaseIP       nvarchar(50),
    @DatabaseName     nvarchar(50),
    @DatabaseUser     nvarchar(50),
    @DatabasePassword nvarchar(200) = NULL,
    @InvoiceSearchSP  nvarchar(200) = NULL,
    @InvoiceDetailSP  nvarchar(200) = NULL,
    @Inactive         bit,
    @ProviderID       int,
    @Values           xml = NULL,
    @User             nvarchar(50)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @PropertyID int = @ID, @LinkID int, @msg nvarchar(2048);

    BEGIN TRY
        BEGIN TRAN;

        IF EXISTS (SELECT 1 FROM dbo.Property_Einvoice WHERE UPPER(LTRIM(RTRIM(Code))) = UPPER(LTRIM(RTRIM(@Code))) AND ID <> @ID)
            RAISERROR (N'Code already exists.', 16, 1);

        IF NOT EXISTS (SELECT 1 FROM dbo.EInvoiceProvider WHERE ID = @ProviderID AND Inactive = 0)
            RAISERROR (N'E-Invoice Provider not found or inactive.', 16, 1);

        -- 1. Property
        IF @ID > 0
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM dbo.Property_Einvoice WHERE ID = @ID)
                RAISERROR (N'Property not found.', 16, 1);

            UPDATE dbo.Property_Einvoice
            SET Code = @Code, Name = @Name, Description = @Description, SourceType = @SourceType, TaxCode = @TaxCode,
                Database_IP = @DatabaseIP, Database_Name = @DatabaseName, Database_User = @DatabaseUser,
                Database_Password = CASE WHEN @DatabasePassword IS NULL THEN Database_Password ELSE @DatabasePassword END,
                InvoiceSearchSP = @InvoiceSearchSP, InvoiceDetailSP = @InvoiceDetailSP, Inactive = @Inactive,
                UpdatedBy = @User, UpdatedDate = GETDATE()
            WHERE ID = @ID;
        END
        ELSE
        BEGIN
            INSERT INTO dbo.Property_Einvoice
                (Code, Name, Description, SourceType, TaxCode, Database_IP, Database_Name, Database_User, Database_Password,
                 InvoiceSearchSP, InvoiceDetailSP, Inactive, CreatedBy, CreatedDate)
            VALUES
                (@Code, @Name, @Description, @SourceType, @TaxCode, @DatabaseIP, @DatabaseName, @DatabaseUser, @DatabasePassword,
                 @InvoiceSearchSP, @InvoiceDetailSP, @Inactive, @User, GETDATE());
            SET @PropertyID = CAST(SCOPE_IDENTITY() AS int);
        END

        -- 2. Liên kết Property - Provider: Provider được chọn là mặc định, các liên kết khác của Property bỏ mặc định
        --    (giữ lại giá trị đã nhập của Provider cũ để quay lại không phải nhập lại)
        UPDATE dbo.Property_EInvoiceProvider SET IsDefault = 0, UpdatedBy = @User, UpdatedDate = GETDATE()
        WHERE PropertyID = @PropertyID AND ProviderID <> @ProviderID AND IsDefault = 1;

        SELECT @LinkID = ID FROM dbo.Property_EInvoiceProvider WHERE PropertyID = @PropertyID AND ProviderID = @ProviderID;
        IF @LinkID IS NULL
        BEGIN
            INSERT INTO dbo.Property_EInvoiceProvider (PropertyID, ProviderID, IsDefault, Inactive, CreatedBy, CreatedDate)
            VALUES (@PropertyID, @ProviderID, 1, 0, @User, GETDATE());
            SET @LinkID = CAST(SCOPE_IDENTITY() AS int);
        END
        ELSE
            UPDATE dbo.Property_EInvoiceProvider SET IsDefault = 1, Inactive = 0, UpdatedBy = @User, UpdatedDate = GETDATE() WHERE ID = @LinkID;

        -- 3. Giá trị field
        DECLARE @tv TABLE (ProviderFieldID int NOT NULL, FieldValue nvarchar(max) NULL, KeepExisting int NOT NULL);
        IF @Values IS NOT NULL
            INSERT INTO @tv (ProviderFieldID, FieldValue, KeepExisting)
            SELECT x.n.value('@id', 'int'), x.n.value('(./text())[1]', 'nvarchar(max)'), ISNULL(x.n.value('@keep', 'int'), 0)
            FROM @Values.nodes('/values/v') x(n);

        IF EXISTS (SELECT 1 FROM @tv t WHERE NOT EXISTS
                   (SELECT 1 FROM dbo.EInvoiceProviderField f WHERE f.ID = t.ProviderFieldID AND f.ProviderID = @ProviderID AND f.Inactive = 0))
            RAISERROR (N'A configuration value does not belong to the selected provider.', 16, 1);

        UPDATE v SET v.FieldValue = t.FieldValue, v.UpdatedBy = @User, v.UpdatedDate = GETDATE()
        FROM dbo.Property_EInvoiceProviderValue v
        INNER JOIN @tv t ON t.ProviderFieldID = v.ProviderFieldID
        WHERE v.PropertyProviderID = @LinkID AND t.KeepExisting = 0;

        INSERT INTO dbo.Property_EInvoiceProviderValue (PropertyProviderID, ProviderFieldID, FieldValue, CreatedBy, CreatedDate)
        SELECT @LinkID, t.ProviderFieldID, t.FieldValue, @User, GETDATE()
        FROM @tv t
        WHERE t.KeepExisting = 0
          AND NOT EXISTS (SELECT 1 FROM dbo.Property_EInvoiceProviderValue v WHERE v.PropertyProviderID = @LinkID AND v.ProviderFieldID = t.ProviderFieldID);

        COMMIT TRAN;
        SELECT @PropertyID AS ID;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        SET @msg = ERROR_MESSAGE();
        RAISERROR (@msg, 16, 1);
    END CATCH
END
GO

-- Xóa Property (chỉ khi chưa từng chạy hóa đơn: chưa có nhật ký thực thi). Đã có lịch sử thì chuyển sang Inactive.
IF OBJECT_ID(N'dbo.spEInvoice_PropertyDelete', N'P') IS NOT NULL DROP PROCEDURE dbo.spEInvoice_PropertyDelete;
GO
CREATE PROCEDURE dbo.spEInvoice_PropertyDelete
    @ID int
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @msg nvarchar(2048);

    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM dbo.Property_Einvoice WHERE ID = @ID)
            RAISERROR (N'Property not found.', 16, 1);

        IF EXISTS (SELECT 1 FROM dbo.EInvoiceProviderExecutionLog WHERE PropertyID = @ID)
            RAISERROR (N'This property already has e-invoice history. Set it to Inactive instead of deleting.', 16, 1);

        BEGIN TRAN;
        DELETE v FROM dbo.Property_EInvoiceProviderValue v
        INNER JOIN dbo.Property_EInvoiceProvider pp ON pp.ID = v.PropertyProviderID
        WHERE pp.PropertyID = @ID;
        DELETE FROM dbo.Property_EInvoiceProvider WHERE PropertyID = @ID;
        DELETE FROM dbo.Property_Einvoice WHERE ID = @ID;
        COMMIT TRAN;

        SELECT @ID AS ID;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRAN;
        SET @msg = ERROR_MESSAGE();
        RAISERROR (@msg, 16, 1);
    END CATCH
END
GO

/* ------------------------------------------------------------------------------------------------
   8. STORED PROCEDURE - GIAI ĐOẠN 4: Engine chạy hóa đơn (nạp cấu hình runtime)
      Chỉ đọc dữ liệu cấu hình; việc dựng request, gọi nhà cung cấp và ghi log do ứng dụng thực hiện.
   ------------------------------------------------------------------------------------------------ */

-- Provider mặc định đang hoạt động của một Property
IF OBJECT_ID(N'dbo.spEInvoice_RuntimeProvider', N'P') IS NOT NULL DROP PROCEDURE dbo.spEInvoice_RuntimeProvider;
GO
CREATE PROCEDURE dbo.spEInvoice_RuntimeProvider
    @PropertyID int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 ID AS PropertyProviderID, PropertyID, ProviderID, ISNULL(IsDefault, 0) AS IsDefault
    FROM dbo.Property_EInvoiceProvider WITH (NOLOCK)
    WHERE PropertyID = @PropertyID AND ISNULL(Inactive, 0) = 0
    ORDER BY ISNULL(IsDefault, 0) DESC, ID DESC;
END
GO

-- Operation theo ID (@OperationID > 0) hoặc theo Provider + Business Action (Operation đang hoạt động đầu tiên)
IF OBJECT_ID(N'dbo.spEInvoice_RuntimeOperation', N'P') IS NOT NULL DROP PROCEDURE dbo.spEInvoice_RuntimeOperation;
GO
CREATE PROCEDURE dbo.spEInvoice_RuntimeOperation
    @ProviderID  int = 0,
    @ActionCode  varchar(50) = NULL,
    @OperationID int = 0
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 ID, ProviderID, OperationCode, OperationName, HttpMethod, EndpointPath AS Endpoint, ContentType, BodyType,
           ExternalMethodCode, ActionCode, IsAuthenticationRequired, AuthenticationOperationID,
           ISNULL(TimeoutSeconds, 120) AS TimeoutSeconds, Inactive
    FROM dbo.EInvoiceProviderOperation WITH (NOLOCK)
    WHERE (@OperationID > 0 AND ID = @OperationID)
       OR (@OperationID <= 0 AND ProviderID = @ProviderID AND BusinessActionCode = @ActionCode AND ISNULL(Inactive, 0) = 0)
    ORDER BY ID;
END
GO

-- Mapping đang hoạt động của một Operation
IF OBJECT_ID(N'dbo.spEInvoice_RuntimeMappings', N'P') IS NOT NULL DROP PROCEDURE dbo.spEInvoice_RuntimeMappings;
GO
CREATE PROCEDURE dbo.spEInvoice_RuntimeMappings
    @OperationID int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, OperationID, MappingLocation, TargetPath, SourceType, SourceValue, DataType, IsRequired, DefaultValue,
           FormatString, OmitWhenNull, PrefixValue, SuffixValue, DisplayOrder, Inactive,
           ConditionSourceType, ConditionSourceValue, ConditionOperator, ConditionExpectedValue
    FROM dbo.EInvoiceProviderMapping WITH (NOLOCK)
    WHERE OperationID = @OperationID AND ISNULL(Inactive, 0) = 0
    ORDER BY DisplayOrder, ID;
END
GO

-- Response Rule đang hoạt động của một Operation
IF OBJECT_ID(N'dbo.spEInvoice_RuntimeRules', N'P') IS NOT NULL DROP PROCEDURE dbo.spEInvoice_RuntimeRules;
GO
CREATE PROCEDURE dbo.spEInvoice_RuntimeRules
    @OperationID int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, OperationID, RuleCode, SourcePath, Operator, ExpectedValue, ResultType, Priority, Inactive
    FROM dbo.EInvoiceProviderResponseRule WITH (NOLOCK)
    WHERE OperationID = @OperationID AND Inactive = 0
    ORDER BY Priority, ID;
END
GO

-- Giá trị cấu hình (FieldCode -> FieldValue) của Property cho Provider đang dùng. Giá trị field Encrypted còn ở dạng đã mã hóa.
IF OBJECT_ID(N'dbo.spEInvoice_RuntimeConfig', N'P') IS NOT NULL DROP PROCEDURE dbo.spEInvoice_RuntimeConfig;
GO
CREATE PROCEDURE dbo.spEInvoice_RuntimeConfig
    @PropertyID int,
    @ProviderID int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT f.FieldCode, v.FieldValue, f.IsEncrypted
    FROM dbo.Property_EInvoiceProvider pp WITH (NOLOCK)
    INNER JOIN dbo.EInvoiceProviderField f WITH (NOLOCK) ON f.ProviderID = pp.ProviderID AND ISNULL(f.Inactive, 0) = 0
    LEFT JOIN dbo.Property_EInvoiceProviderValue v WITH (NOLOCK) ON v.PropertyProviderID = pp.ID AND v.ProviderFieldID = f.ID
    WHERE pp.PropertyID = @PropertyID AND pp.ProviderID = @ProviderID
      AND ISNULL(pp.Inactive, 0) = 0 AND ISNULL(pp.IsDefault, 0) = 1
    ORDER BY f.DisplayOrder, f.ID;
END
GO

-- Phản hồi (RawResponse) của lần Upload thành công gần nhất của một hóa đơn PMS (dùng cho bước Publish)
IF OBJECT_ID(N'dbo.spEInvoice_RuntimePrevious', N'P') IS NOT NULL DROP PROCEDURE dbo.spEInvoice_RuntimePrevious;
GO
CREATE PROCEDURE dbo.spEInvoice_RuntimePrevious
    @PropertyID int,
    @FolioVATID int
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1 l.RawResponse
    FROM dbo.EInvoiceProviderExecutionLog l WITH (NOLOCK)
    INNER JOIN dbo.EInvoiceProviderOperation o WITH (NOLOCK) ON o.ID = l.OperationID
    WHERE l.PropertyID = @PropertyID AND l.FolioVATID = @FolioVATID
      AND ISNULL(l.ProviderSuccess, 0) = 1 AND o.BusinessActionCode = 'UPLOAD_INVOICE'
    ORDER BY l.ID DESC;
END
GO

/* ------------------------------------------------------------------------------------------------
   9. STORED PROCEDURE - GIAI ĐOẠN 5: Màn thao tác hóa đơn PMS (trạng thái, nhật ký)
   ------------------------------------------------------------------------------------------------ */

-- Trạng thái Upload/Publish thành công mới nhất của danh sách hóa đơn PMS (theo nhật ký thực thi).
-- @Ids: <ids><i v="123"/><i v="456"/></ids>
IF OBJECT_ID(N'dbo.spEInvoice_InvoiceStates', N'P') IS NOT NULL DROP PROCEDURE dbo.spEInvoice_InvoiceStates;
GO
CREATE PROCEDURE dbo.spEInvoice_InvoiceStates
    @PropertyID int,
    @Ids        xml
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ProviderID int;
    SELECT TOP 1 @ProviderID = ProviderID
    FROM dbo.Property_EInvoiceProvider WITH (NOLOCK)
    WHERE PropertyID = @PropertyID AND ISNULL(Inactive, 0) = 0
    ORDER BY ISNULL(IsDefault, 0) DESC, ID DESC;

    IF @ProviderID IS NULL RETURN;

    DECLARE @T TABLE (ID int PRIMARY KEY);
    INSERT INTO @T (ID)
    SELECT DISTINCT x.i.value('@v', 'int') FROM @Ids.nodes('/ids/i') x(i);

    ;WITH Ranked AS
    (
        SELECT l.SourceRecordID, o.BusinessActionCode, l.Pattern, l.Serial, l.InvoiceNo, l.InvoiceDate,
               ROW_NUMBER() OVER (PARTITION BY l.SourceRecordID, o.BusinessActionCode ORDER BY l.ID DESC) AS RowNumber
        FROM dbo.EInvoiceProviderExecutionLog l WITH (NOLOCK)
        INNER JOIN dbo.EInvoiceProviderOperation o WITH (NOLOCK) ON o.ID = l.OperationID
        INNER JOIN @T t ON t.ID = l.SourceRecordID
        WHERE l.PropertyID = @PropertyID AND l.SourceType = 'PMS' AND l.ProviderID = @ProviderID
          AND ISNULL(l.ProviderSuccess, 0) = 1
          AND o.BusinessActionCode IN ('UPLOAD_INVOICE', 'PUBLISH_INVOICE', 'ISSUE_INVOICE')
    )
    SELECT SourceRecordID, BusinessActionCode, Pattern, Serial, InvoiceNo, InvoiceDate
    FROM Ranked WHERE RowNumber = 1;
END
GO

-- Chi tiết request/response của một dòng nhật ký (chỉ dành cho người có quyền cấu hình)
IF OBJECT_ID(N'dbo.spEInvoice_ExecutionLogDetail', N'P') IS NOT NULL DROP PROCEDURE dbo.spEInvoice_ExecutionLogDetail;
GO
CREATE PROCEDURE dbo.spEInvoice_ExecutionLogDetail
    @ID bigint
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ID, OperationCode, RequestUrl, RequestBody, RawResponse, ErrorMessage
    FROM dbo.EInvoiceProviderExecutionLog WITH (NOLOCK)
    WHERE ID = @ID;
END
GO

-- Property đơn vị phát hành mà người dùng được phép thao tác (nhóm 14 thấy tất cả; nhóm khác theo bảng PropertyPermission)
IF OBJECT_ID(N'dbo.spEInvoice_AllowedProperties', N'P') IS NOT NULL DROP PROCEDURE dbo.spEInvoice_AllowedProperties;
GO
CREATE PROCEDURE dbo.spEInvoice_AllowedProperties
    @UserID      int,
    @UserGroupID int,
    @SourceType  varchar(10) = 'PMS'
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.ID, p.Code, p.Name, p.SourceType
    FROM dbo.Property_Einvoice p WITH (NOLOCK)
    WHERE ISNULL(p.Inactive, 0) = 0
      AND p.SourceType = @SourceType
      AND (@UserGroupID = 14
           OR EXISTS (SELECT 1 FROM dbo.PropertyPermission pp WITH (NOLOCK) WHERE pp.PropertyID = p.ID AND pp.UserID = @UserID))
    ORDER BY p.Code;
END
GO

-- Nghiệp vụ mà Provider mặc định của Property hỗ trợ (để màn hình chỉ hiện các nút phù hợp)
IF OBJECT_ID(N'dbo.spEInvoice_ProviderActions', N'P') IS NOT NULL DROP PROCEDURE dbo.spEInvoice_ProviderActions;
GO
CREATE PROCEDURE dbo.spEInvoice_ProviderActions
    @PropertyID int
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @ProviderID int;
    SELECT TOP 1 @ProviderID = ProviderID
    FROM dbo.Property_EInvoiceProvider WITH (NOLOCK)
    WHERE PropertyID = @PropertyID AND ISNULL(Inactive, 0) = 0
    ORDER BY ISNULL(IsDefault, 0) DESC, ID DESC;

    SELECT o.ID, o.OperationCode, o.OperationName, UPPER(LTRIM(RTRIM(o.BusinessActionCode))) AS ActionCode
    FROM dbo.EInvoiceProviderOperation o WITH (NOLOCK)
    WHERE o.ProviderID = @ProviderID AND ISNULL(o.Inactive, 0) = 0 AND NULLIF(LTRIM(RTRIM(o.BusinessActionCode)), '') IS NOT NULL
    ORDER BY o.DisplayOrder, o.ID;
END
GO

-- Nhật ký thực thi của các hóa đơn PMS thuộc một Property (không kèm request/response để danh sách nhẹ)
IF OBJECT_ID(N'dbo.spEInvoice_LogSearch', N'P') IS NOT NULL DROP PROCEDURE dbo.spEInvoice_LogSearch;
GO
CREATE PROCEDURE dbo.spEInvoice_LogSearch
    @PropertyID int,
    @FolioVATID int         = NULL,
    @ActionCode varchar(50) = NULL,
    @Status     int         = NULL,   -- 1 = thành công, 0 = thất bại, NULL = tất cả
    @FromDate   date        = NULL,
    @ToDate     date        = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 1000
           l.ID, l.SourceRecordID AS FolioVatId, l.CreatedDate AS LogDate,
           CASE o.BusinessActionCode WHEN 'UPLOAD_INVOICE' THEN 'Upload' WHEN 'PUBLISH_INVOICE' THEN 'Publish' WHEN 'ISSUE_INVOICE' THEN 'Issue'
                WHEN 'ADJUST_INVOICE' THEN 'Adjust' WHEN 'REPLACE_INVOICE' THEN 'Replace' WHEN 'CANCEL_INVOICE' THEN 'Cancel'
                ELSE ISNULL(o.BusinessActionCode, '') END AS ActionName,
           l.OperationCode, ep.Code AS ProviderCodeText,
           CASE WHEN ISNULL(l.ProviderSuccess, 0) = 1 THEN 'Success' ELSE 'Failed' END AS Status,
           l.HttpStatusCode, l.ProviderCode, l.ErrorMessage AS Message, l.ElapsedMilliseconds, l.CreatedBy
    FROM dbo.EInvoiceProviderExecutionLog l WITH (NOLOCK)
    LEFT JOIN dbo.EInvoiceProviderOperation o WITH (NOLOCK) ON o.ID = l.OperationID
    LEFT JOIN dbo.EInvoiceProvider ep WITH (NOLOCK) ON ep.ID = l.ProviderID
    WHERE l.PropertyID = @PropertyID AND l.SourceType = 'PMS'
      AND (@FolioVATID IS NULL OR l.SourceRecordID = @FolioVATID)
      AND (@ActionCode IS NULL OR o.BusinessActionCode = @ActionCode)
      AND (@Status IS NULL OR ISNULL(l.ProviderSuccess, 0) = @Status)
      AND (@FromDate IS NULL OR l.CreatedDate >= @FromDate)
      AND (@ToDate IS NULL OR l.CreatedDate < DATEADD(DAY, 1, @ToDate))
    ORDER BY l.ID DESC;
END
GO

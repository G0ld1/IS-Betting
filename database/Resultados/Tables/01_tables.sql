IF DB_ID('ResultadosFutebol') IS NULL
    CREATE DATABASE ResultadosFutebol;
GO

USE ResultadosFutebol;
GO

IF OBJECT_ID('dbo.Jogo', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Jogo
    (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CodigoJogo VARCHAR(20) NOT NULL UNIQUE,
        DataJogo DATE NOT NULL,
        HoraInicio TIME(0) NOT NULL,
        EquipaCasa NVARCHAR(100) NOT NULL,
        EquipaFora NVARCHAR(100) NOT NULL,
        GolosCasa INT NOT NULL CONSTRAINT DF_Resultados_GolosCasa DEFAULT(0),
        GolosFora INT NOT NULL CONSTRAINT DF_Resultados_GolosFora DEFAULT(0),
        Estado INT NOT NULL,
        CONSTRAINT CK_Resultados_Estado CHECK (Estado BETWEEN 1 AND 5)
    );
END
GO

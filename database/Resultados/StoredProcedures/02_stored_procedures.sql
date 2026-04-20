USE ResultadosFutebol;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Resultados_InserirJogo
    @CodigoJogo VARCHAR(20),
    @DataJogo DATE,
    @HoraInicio TIME(0),
    @EquipaCasa NVARCHAR(100),
    @EquipaFora NVARCHAR(100),
    @Estado INT
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM dbo.Jogo WHERE CodigoJogo = @CodigoJogo)
        THROW 50001, 'Codigo_Jogo já existe.', 1;

    INSERT INTO dbo.Jogo (CodigoJogo, DataJogo, HoraInicio, EquipaCasa, EquipaFora, Estado)
    VALUES (@CodigoJogo, @DataJogo, @HoraInicio, @EquipaCasa, @EquipaFora, @Estado);

    SELECT CAST(SCOPE_IDENTITY() AS INT);
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Resultados_AtualizarJogo
    @CodigoJogo VARCHAR(20),
    @Estado INT,
    @GolosCasa INT,
    @GolosFora INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @EstadoAtual INT;
    SELECT @EstadoAtual = Estado FROM dbo.Jogo WHERE CodigoJogo = @CodigoJogo;

    IF @EstadoAtual IS NULL
        THROW 50002, 'Jogo não existe.', 1;

    IF (@EstadoAtual = 3 AND @Estado <> 3) OR (@EstadoAtual IN (4,5) AND @Estado <> @EstadoAtual)
        THROW 50003, 'Transição de estado inválida.', 1;

    UPDATE dbo.Jogo
    SET Estado = @Estado,
        GolosCasa = @GolosCasa,
        GolosFora = @GolosFora
    WHERE CodigoJogo = @CodigoJogo;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Resultados_ListarJogos
    @DataJogo DATE = NULL,
    @Estado INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Id, CodigoJogo, DataJogo, HoraInicio, EquipaCasa, EquipaFora, GolosCasa, GolosFora, Estado
    FROM dbo.Jogo
    WHERE (@DataJogo IS NULL OR DataJogo = @DataJogo)
      AND (@Estado IS NULL OR Estado = @Estado)
    ORDER BY DataJogo, HoraInicio;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Resultados_ObterJogoPorCodigo
    @CodigoJogo VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1 Id, CodigoJogo, DataJogo, HoraInicio, EquipaCasa, EquipaFora, GolosCasa, GolosFora, Estado
    FROM dbo.Jogo
    WHERE CodigoJogo = @CodigoJogo;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Resultados_RemoverJogo
    @CodigoJogo VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.Jogo
    WHERE CodigoJogo = @CodigoJogo
      AND Estado = 1;
END
GO

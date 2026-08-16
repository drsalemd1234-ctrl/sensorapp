CREATE TABLE t_dev (
    id  INT IDENTITY(1,1) PRIMARY KEY,
    nm  VARCHAR(100),
    loc VARCHAR(200),
    tp  INT,
    st  INT,
    cfg VARCHAR(500),   -- thr=|unit=|int= pipe delimited
    dt  DATETIME
);

CREATE TABLE t_dat (
    id  INT IDENTITY(1,1) PRIMARY KEY,
    did INT,            -- device ref
    ts  DATETIME,
    v   FLOAT,          -- primary value
    v2  FLOAT,
    v3  FLOAT,
    typ INT,            -- 1=?, 3=?
    st  INT,
    flg INT,
    n   VARCHAR(500),
    dt1 DATETIME,
    dt2 DATETIME        -- reserved
    -- FOREIGN KEY (did) REFERENCES t_dev(id)
);

CREATE TABLE t_log (
    id  INT IDENTITY(1,1) PRIMARY KEY,
    ref INT,
    msg VARCHAR(1000),
    dt  DATETIME,
    flg INT
);

CREATE PROCEDURE sp_calc @did INT AS
BEGIN
    DECLARE @avg FLOAT, @mx FLOAT, @thr FLOAT, @cfg VARCHAR(500), @p INT, @p2 INT
    SELECT @avg = AVG(v), @mx = MAX(v) FROM t_dat WHERE did = @did AND typ = 1 AND ts >= DATEADD(hour,-1,GETDATE())
    SELECT @cfg = cfg FROM t_dev WHERE id = @did
    SET @thr = 75
    IF @cfg IS NOT NULL AND CHARINDEX('thr=',@cfg) > 0
    BEGIN
        SET @p = CHARINDEX('thr=',@cfg) + 4
        SET @p2 = CHARINDEX('|',@cfg,@p)
        IF @p2 = 0 SET @p2 = LEN(@cfg)+1
        SET @thr = CAST(SUBSTRING(@cfg,@p,@p2-@p) AS FLOAT)
    END
    IF @mx > @thr
        INSERT INTO t_dat(did,ts,v,v2,typ,st,flg,n,dt1)
        VALUES(@did,GETDATE(),@mx,@avg,3,1,1,'ALERT: threshold exceeded',GETDATE())
    INSERT INTO t_log(ref,msg,dt,flg) VALUES(@did,'calc did='+CAST(@did AS VARCHAR),GETDATE(),0)
    SELECT @avg avg, @mx mx, @thr thr
END;

CREATE TABLE IF NOT EXISTS t_dev (
    id  INTEGER PRIMARY KEY AUTOINCREMENT,
    nm  TEXT,
    loc TEXT,
    tp  INTEGER,
    st  INTEGER,
    cfg TEXT,   -- thr=|unit=|int= pipe delimited
    dt  TEXT
);

CREATE TABLE IF NOT EXISTS t_dat (
    id  INTEGER PRIMARY KEY AUTOINCREMENT,
    did INTEGER,            -- device ref
    ts  TEXT,
    v   REAL,               -- primary value
    v2  REAL,
    v3  REAL,
    typ INTEGER,            -- 1=?, 3=?
    st  INTEGER,
    flg INTEGER,
    n   TEXT,
    dt1 TEXT,
    dt2 TEXT                -- reserved
    -- FOREIGN KEY (did) REFERENCES t_dev(id)
);

CREATE TABLE IF NOT EXISTS t_log (
    id  INTEGER PRIMARY KEY AUTOINCREMENT,
    ref INTEGER,
    msg TEXT,
    dt  TEXT,
    flg INTEGER
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

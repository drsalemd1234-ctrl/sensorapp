using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading;

namespace SensorApp.Mgr
{
    public class SM
    {
        static string cs = "Server=db,1433;Database=sdb;User Id=sa;Password=Pass123!;";
        static string mcs = "Server=db,1433;Database=master;User Id=sa;Password=Pass123!;";

        public static void Init()
        {
            int x = 0;
            while (x < 10)
            {
                try
                {
                    var mcon = new SqlConnection(mcs);
                    mcon.Open();
                    new SqlCommand("IF NOT EXISTS (SELECT name FROM sys.databases WHERE name='sdb') CREATE DATABASE sdb", mcon).ExecuteNonQuery();
                    mcon.Close();

                    var con = new SqlConnection(cs);
                    con.Open();

                    new SqlCommand(@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='t_dev' AND xtype='U')
CREATE TABLE t_dev (id INT IDENTITY(1,1) PRIMARY KEY, nm VARCHAR(100), loc VARCHAR(200), tp INT, st INT, cfg VARCHAR(500), dt DATETIME)", con).ExecuteNonQuery();

                    new SqlCommand(@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='t_dat' AND xtype='U')
CREATE TABLE t_dat (id INT IDENTITY(1,1) PRIMARY KEY, did INT, ts DATETIME, v FLOAT, v2 FLOAT, v3 FLOAT, typ INT, st INT, flg INT, n VARCHAR(500), dt1 DATETIME, dt2 DATETIME)", con).ExecuteNonQuery();

                    new SqlCommand(@"IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='t_log' AND xtype='U')
CREATE TABLE t_log (id INT IDENTITY(1,1) PRIMARY KEY, ref INT, msg VARCHAR(1000), dt DATETIME, flg INT)", con).ExecuteNonQuery();

                    new SqlCommand("IF OBJECT_ID('sp_calc') IS NOT NULL DROP PROCEDURE sp_calc", con).ExecuteNonQuery();

                    new SqlCommand(@"CREATE PROCEDURE sp_calc @did INT AS
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
END", con).ExecuteNonQuery();

                    new SqlCommand(@"IF NOT EXISTS (SELECT TOP 1 1 FROM t_dev)
BEGIN
    INSERT INTO t_dev(nm,loc,tp,st,cfg,dt) VALUES('snsr-01','Building A|Room 1',1,1,'thr=75|unit=C|int=30',GETDATE())
    INSERT INTO t_dev(nm,loc,tp,st,cfg,dt) VALUES('snsr-02','Building A|Room 2',1,1,'thr=80|unit=C|int=30',GETDATE())
    INSERT INTO t_dev(nm,loc,tp,st,cfg,dt) VALUES('snsr-03','Building B|Floor 1',2,1,'thr=70|unit=F|int=60',GETDATE())
END", con).ExecuteNonQuery();

                    new SqlCommand(@"IF NOT EXISTS (SELECT TOP 1 1 FROM t_dat)
BEGIN
    DECLARE @i INT = 0
    WHILE @i < 100
    BEGIN
        INSERT INTO t_dat(did,ts,v,v2,v3,typ,st,flg,n,dt1) VALUES(1,DATEADD(minute,-@i*5,GETDATE()),65+(@i%15),55+(@i%20),1013+(@i%5),1,1,0,'',GETDATE())
        INSERT INTO t_dat(did,ts,v,v2,v3,typ,st,flg,n,dt1) VALUES(2,DATEADD(minute,-@i*5,GETDATE()),70+(@i%10),60+(@i%15),1010+(@i%8),1,1,0,'',GETDATE())
        SET @i = @i + 1
    END
END", con).ExecuteNonQuery();

                    con.Close();
                    return;
                }
                catch (Exception)
                {
                    x++;
                    Thread.Sleep(3000);
                }
            }
        }

        public static List<Mdl.D> GetAll(string tp, string did, string df, string dt)
        {
            var r = new List<Mdl.D>();
            SqlConnection con = null;
            try
            {
                con = new SqlConnection(cs);
                con.Open();
                string s = "SELECT TOP 1000 * FROM t_dat WHERE 1=1";
                if (!string.IsNullOrEmpty(tp) && tp != "0") s += " AND typ=" + tp;
                if (!string.IsNullOrEmpty(did) && did != "0") s += " AND did=" + did;
                if (!string.IsNullOrEmpty(df)) s += " AND ts>='" + df + "'";
                if (!string.IsNullOrEmpty(dt)) s += " AND ts<='" + dt + "'";
                s += " ORDER BY ts DESC";
                var rd = new SqlCommand(s, con).ExecuteReader();
                while (rd.Read())
                {
                    var d = new Mdl.D();
                    d.Id = Convert.ToInt32(rd["id"]);
                    d.Did = rd["did"] == DBNull.Value ? 0 : Convert.ToInt32(rd["did"]);
                    d.Ts = rd["ts"] == DBNull.Value ? "" : Convert.ToDateTime(rd["ts"]).ToString("yyyy-MM-dd HH:mm:ss");
                    d.V = rd["v"] == DBNull.Value ? 0 : Convert.ToDouble(rd["v"]);
                    d.V2 = rd["v2"] == DBNull.Value ? 0 : Convert.ToDouble(rd["v2"]);
                    d.V3 = rd["v3"] == DBNull.Value ? 0 : Convert.ToDouble(rd["v3"]);
                    d.Typ = rd["typ"] == DBNull.Value ? 0 : Convert.ToInt32(rd["typ"]);
                    d.St = rd["st"] == DBNull.Value ? 0 : Convert.ToInt32(rd["st"]);
                    d.Flg = rd["flg"] == DBNull.Value ? 0 : Convert.ToInt32(rd["flg"]);
                    d.N = rd["n"] == DBNull.Value ? "" : rd["n"].ToString();
                    r.Add(d);
                }
            }
            catch { }
            finally { if (con != null) try { con.Close(); } catch { } }
            return r;
        }

        public static bool Save(Mdl.D d)
        {
            try
            {
                var con = new SqlConnection(cs);
                con.Open();
                string ts = string.IsNullOrEmpty(d.Ts) ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : d.Ts;
                string s = "INSERT INTO t_dat(did,ts,v,v2,v3,typ,st,flg,n,dt1) VALUES(" +
                    d.Did + ",'" + ts + "'," + d.V + "," + d.V2 + "," + d.V3 + "," +
                    d.Typ + "," + d.St + "," + d.Flg + ",'" + d.N + "',GETDATE())";
                new SqlCommand(s, con).ExecuteNonQuery();
                new SqlCommand("UPDATE t_dev SET dt=GETDATE() WHERE id=" + d.Did, con).ExecuteNonQuery();
                new SqlCommand("INSERT INTO t_log(ref,msg,dt,flg) VALUES(" + d.Did + ",'data saved',GETDATE(),0)", con).ExecuteNonQuery();

                var rd2 = new SqlCommand("SELECT cfg FROM t_dev WHERE id=" + d.Did, con).ExecuteReader();
                string cfg = "";
                if (rd2.Read()) cfg = rd2["cfg"] == DBNull.Value ? "" : rd2["cfg"].ToString();
                rd2.Close();

                double thr = 75;
                if (cfg.Contains("thr="))
                {
                    int p = cfg.IndexOf("thr=") + 4;
                    int p2 = cfg.IndexOf("|", p);
                    if (p2 < 0) p2 = cfg.Length;
                    try { thr = double.Parse(cfg.Substring(p, p2 - p)); } catch { }
                }
                if (d.V > thr)
                {
                    new SqlCommand("INSERT INTO t_dat(did,ts,v,v2,typ,st,flg,n,dt1) VALUES(" + d.Did + ",GETDATE()," + d.V + "," + thr + ",3,1,1,'AUTO ALERT',GETDATE())", con).ExecuteNonQuery();
                    new SqlCommand("INSERT INTO t_log(ref,msg,dt,flg) VALUES(" + d.Did + ",'alert val=" + d.V + "',GETDATE(),1)", con).ExecuteNonQuery();
                }
                con.Close();
                return true;
            }
            catch { return false; }
        }

        public static List<Mdl.D> GetDevs(string st)
        {
            var r = new List<Mdl.D>();
            SqlConnection con = null;
            try
            {
                con = new SqlConnection(cs);
                con.Open();
                string s = "SELECT * FROM t_dev";
                if (!string.IsNullOrEmpty(st) && st != "0") s += " WHERE st=" + st;
                var rd = new SqlCommand(s, con).ExecuteReader();
                while (rd.Read())
                {
                    var d = new Mdl.D();
                    d.Id = Convert.ToInt32(rd["id"]);
                    d.Nm = rd["nm"] == DBNull.Value ? "" : rd["nm"].ToString();
                    d.Loc = rd["loc"] == DBNull.Value ? "" : rd["loc"].ToString();
                    d.Tp = rd["tp"] == DBNull.Value ? 0 : Convert.ToInt32(rd["tp"]);
                    d.St = rd["st"] == DBNull.Value ? 0 : Convert.ToInt32(rd["st"]);
                    d.Cfg = rd["cfg"] == DBNull.Value ? "" : rd["cfg"].ToString();
                    d.Ts = rd["dt"] == DBNull.Value ? "" : Convert.ToDateTime(rd["dt"]).ToString("yyyy-MM-dd HH:mm:ss");
                    if (!string.IsNullOrEmpty(d.Cfg))
                    {
                        try
                        {
                            foreach (var kv in d.Cfg.Split('|'))
                            {
                                var tmp = kv.Split('=');
                                if (tmp.Length == 2)
                                {
                                    if (tmp[0] == "thr") d.V = double.Parse(tmp[1]);
                                    if (tmp[0] == "int") d.V2 = double.Parse(tmp[1]);
                                }
                            }
                        }
                        catch { }
                    }
                    r.Add(d);
                }
            }
            catch { }
            finally { if (con != null) try { con.Close(); } catch { } }
            return r;
        }

        public static bool SaveDev(Mdl.D d)
        {
            try
            {
                var con = new SqlConnection(cs);
                con.Open();
                if (d.Id > 0)
                {
                    new SqlCommand("UPDATE t_dev SET nm='" + d.Nm + "',loc='" + d.Loc + "',tp=" + d.Tp + ",st=" + d.St + ",cfg='" + d.Cfg + "',dt=GETDATE() WHERE id=" + d.Id, con).ExecuteNonQuery();
                    new SqlCommand("INSERT INTO t_log(ref,msg,dt,flg) VALUES(" + d.Id + ",'dev updated',GETDATE(),0)", con).ExecuteNonQuery();
                }
                else
                {
                    new SqlCommand("INSERT INTO t_dev(nm,loc,tp,st,cfg,dt) VALUES('" + d.Nm + "','" + d.Loc + "'," + d.Tp + "," + d.St + ",'" + d.Cfg + "',GETDATE())", con).ExecuteNonQuery();
                    new SqlCommand("INSERT INTO t_log(ref,msg,dt,flg) VALUES(0,'dev added " + d.Nm + "',GETDATE(),0)", con).ExecuteNonQuery();
                }
                con.Close();
                return true;
            }
            catch { return false; }
        }

        public static object Calc(int did)
        {
            object res = null;
            SqlConnection con = null;
            try
            {
                con = new SqlConnection(cs);
                con.Open();
                var cmd = new SqlCommand("sp_calc", con);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@did", did);
                var rd = cmd.ExecuteReader();
                if (rd.Read())
                    res = new
                    {
                        avg = rd["avg"] == DBNull.Value ? 0 : Math.Round(Convert.ToDouble(rd["avg"]), 2),
                        mx = rd["mx"] == DBNull.Value ? 0 : Math.Round(Convert.ToDouble(rd["mx"]), 2),
                        thr = rd["thr"]
                    };
            }
            catch { }
            finally { if (con != null) try { con.Close(); } catch { } }
            return res;
        }

        public static List<Mdl.D> GetLog(string did, string flg)
        {
            var r = new List<Mdl.D>();
            SqlConnection con = null;
            try
            {
                con = new SqlConnection(cs);
                con.Open();
                string s = "SELECT * FROM t_log WHERE 1=1";
                if (!string.IsNullOrEmpty(did) && did != "0") s += " AND ref=" + did;
                if (!string.IsNullOrEmpty(flg) && flg != "-1") s += " AND flg=" + flg;
                s += " ORDER BY dt DESC";
                var rd = new SqlCommand(s, con).ExecuteReader();
                while (rd.Read())
                {
                    var d = new Mdl.D();
                    d.Id = Convert.ToInt32(rd["id"]);
                    d.Did = rd["ref"] == DBNull.Value ? 0 : Convert.ToInt32(rd["ref"]);
                    d.N = rd["msg"] == DBNull.Value ? "" : rd["msg"].ToString();
                    d.Ts = rd["dt"] == DBNull.Value ? "" : Convert.ToDateTime(rd["dt"]).ToString("yyyy-MM-dd HH:mm:ss");
                    d.Flg = rd["flg"] == DBNull.Value ? 0 : Convert.ToInt32(rd["flg"]);
                    r.Add(d);
                }
            }
            catch { }
            finally { if (con != null) try { con.Close(); } catch { } }
            return r;
        }

        public static object Stats(int did)
        {
            SqlConnection con = null;
            try
            {
                con = new SqlConnection(cs);
                con.Open();
                string s = "SELECT COUNT(*) total, AVG(v) avg_v, MAX(v) max_v, MIN(v) min_v, " +
                    "SUM(CASE WHEN typ=3 THEN 1 ELSE 0 END) alerts, SUM(CASE WHEN typ=1 THEN 1 ELSE 0 END) readings, MAX(ts) last_ts " +
                    "FROM t_dat WHERE did=" + did + " AND typ IN (1,3)";
                var rd = new SqlCommand(s, con).ExecuteReader();
                if (rd.Read())
                {
                    return new
                    {
                        total = rd["total"],
                        avg = rd["avg_v"] == DBNull.Value ? 0 : Math.Round(Convert.ToDouble(rd["avg_v"]), 2),
                        max = rd["max_v"],
                        min = rd["min_v"],
                        alerts = rd["alerts"],
                        readings = rd["readings"],
                        last = rd["last_ts"] == DBNull.Value ? "" : Convert.ToDateTime(rd["last_ts"]).ToString("yyyy-MM-dd HH:mm:ss")
                    };
                }
            }
            catch { }
            finally { if (con != null) try { con.Close(); } catch { } }
            return null;
        }
    }
}

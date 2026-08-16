using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.Sqlite;
using System.Threading;

namespace SensorApp.Mgr
{
    public class SM
    {
        static string cs = "Data Source=sdb.db";
        static string mcs = "Data Source=sdb.db";

        public static void Init()
        {
            int x = 0;
            while (x < 10)
            {
                try
                {
                    var con = new SqliteConnection(cs);
                    con.Open();

                    new SqliteCommand(@"CREATE TABLE IF NOT EXISTS t_dev (id INTEGER PRIMARY KEY AUTOINCREMENT, nm TEXT, loc TEXT, tp INTEGER, st INTEGER, cfg TEXT, dt TEXT)", con).ExecuteNonQuery();

                    new SqliteCommand(@"CREATE TABLE IF NOT EXISTS t_dat (id INTEGER PRIMARY KEY AUTOINCREMENT, did INTEGER, ts TEXT, v REAL, v2 REAL, v3 REAL, typ INTEGER, st INTEGER, flg INTEGER, n TEXT, dt1 TEXT, dt2 TEXT)", con).ExecuteNonQuery();

                    new SqliteCommand(@"CREATE TABLE IF NOT EXISTS t_log (id INTEGER PRIMARY KEY AUTOINCREMENT, ref INTEGER, msg TEXT, dt TEXT, flg INTEGER)", con).ExecuteNonQuery();

                    var chk = new SqliteCommand("SELECT COUNT(*) FROM t_dev", con).ExecuteScalar();
                    if (Convert.ToInt32(chk) == 0)
                    {
                        new SqliteCommand("INSERT INTO t_dev(nm,loc,tp,st,cfg,dt) VALUES('snsr-01','Building A|Room 1',1,1,'thr=75|unit=C|int=30',datetime('now'))", con).ExecuteNonQuery();
                        new SqliteCommand("INSERT INTO t_dev(nm,loc,tp,st,cfg,dt) VALUES('snsr-02','Building A|Room 2',1,1,'thr=80|unit=C|int=30',datetime('now'))", con).ExecuteNonQuery();
                        new SqliteCommand("INSERT INTO t_dev(nm,loc,tp,st,cfg,dt) VALUES('snsr-03','Building B|Floor 1',2,1,'thr=70|unit=F|int=60',datetime('now'))", con).ExecuteNonQuery();
                    }

                    var chk2 = new SqliteCommand("SELECT COUNT(*) FROM t_dat", con).ExecuteScalar();
                    if (Convert.ToInt32(chk2) == 0)
                    {
                        for (int i = 0; i < 100; i++)
                        {
                            string ts1 = DateTime.Now.AddMinutes(-i * 5).ToString("yyyy-MM-dd HH:mm:ss");
                            new SqliteCommand($"INSERT INTO t_dat(did,ts,v,v2,v3,typ,st,flg,n,dt1) VALUES(1,'{ts1}',{65 + (i % 15)},{55 + (i % 20)},{1013 + (i % 5)},1,1,0,'',datetime('now'))", con).ExecuteNonQuery();
                            new SqliteCommand($"INSERT INTO t_dat(did,ts,v,v2,v3,typ,st,flg,n,dt1) VALUES(2,'{ts1}',{70 + (i % 10)},{60 + (i % 15)},{1010 + (i % 8)},1,1,0,'',datetime('now'))", con).ExecuteNonQuery();
                        }
                    }

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
            SqliteConnection con = null;
            try
            {
                con = new SqliteConnection(cs);
                con.Open();
                string s = "SELECT * FROM t_dat WHERE 1=1";
                if (!string.IsNullOrEmpty(tp) && tp != "0") s += " AND typ=" + tp;
                if (!string.IsNullOrEmpty(did) && did != "0") s += " AND did=" + did;
                if (!string.IsNullOrEmpty(df)) s += " AND ts>='" + df + "'";
                if (!string.IsNullOrEmpty(dt)) s += " AND ts<='" + dt + "'";
                s += " ORDER BY ts DESC LIMIT 1000";
                var rd = new SqliteCommand(s, con).ExecuteReader();
                while (rd.Read())
                {
                    var d = new Mdl.D();
                    d.Id = Convert.ToInt32(rd["id"]);
                    d.Did = rd["did"] == DBNull.Value ? 0 : Convert.ToInt32(rd["did"]);
                    d.Ts = rd["ts"] == DBNull.Value ? "" : rd["ts"].ToString();
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
                var con = new SqliteConnection(cs);
                con.Open();
                string ts = string.IsNullOrEmpty(d.Ts) ? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") : d.Ts;
                string s = "INSERT INTO t_dat(did,ts,v,v2,v3,typ,st,flg,n,dt1) VALUES(" +
                    d.Did + ",'" + ts + "'," + d.V + "," + d.V2 + "," + d.V3 + "," +
                    d.Typ + "," + d.St + "," + d.Flg + ",'" + d.N + "',datetime('now'))";
                new SqliteCommand(s, con).ExecuteNonQuery();
                new SqliteCommand("UPDATE t_dev SET dt=datetime('now') WHERE id=" + d.Did, con).ExecuteNonQuery();
                new SqliteCommand("INSERT INTO t_log(ref,msg,dt,flg) VALUES(" + d.Did + ",'data saved',datetime('now'),0)", con).ExecuteNonQuery();

                var rd2 = new SqliteCommand("SELECT cfg FROM t_dev WHERE id=" + d.Did, con).ExecuteReader();
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
                    new SqliteCommand("INSERT INTO t_dat(did,ts,v,v2,typ,st,flg,n,dt1) VALUES(" + d.Did + ",datetime('now')," + d.V + "," + thr + ",3,1,1,'AUTO ALERT',datetime('now'))", con).ExecuteNonQuery();
                    new SqliteCommand("INSERT INTO t_log(ref,msg,dt,flg) VALUES(" + d.Did + ",'alert val=" + d.V + "',datetime('now'),1)", con).ExecuteNonQuery();
                }
                con.Close();
                return true;
            }
            catch { return false; }
        }

        public static List<Mdl.D> GetDevs(string st)
        {
            var r = new List<Mdl.D>();
            SqliteConnection con = null;
            try
            {
                con = new SqliteConnection(cs);
                con.Open();
                string s = "SELECT * FROM t_dev";
                if (!string.IsNullOrEmpty(st) && st != "0") s += " WHERE st=" + st;
                var rd = new SqliteCommand(s, con).ExecuteReader();
                while (rd.Read())
                {
                    var d = new Mdl.D();
                    d.Id = Convert.ToInt32(rd["id"]);
                    d.Nm = rd["nm"] == DBNull.Value ? "" : rd["nm"].ToString();
                    d.Loc = rd["loc"] == DBNull.Value ? "" : rd["loc"].ToString();
                    d.Tp = rd["tp"] == DBNull.Value ? 0 : Convert.ToInt32(rd["tp"]);
                    d.St = rd["st"] == DBNull.Value ? 0 : Convert.ToInt32(rd["st"]);
                    d.Cfg = rd["cfg"] == DBNull.Value ? "" : rd["cfg"].ToString();
                    d.Ts = rd["dt"] == DBNull.Value ? "" : rd["dt"].ToString();
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
                var con = new SqliteConnection(cs);
                con.Open();
                if (d.Id > 0)
                {
                    new SqliteCommand("UPDATE t_dev SET nm='" + d.Nm + "',loc='" + d.Loc + "',tp=" + d.Tp + ",st=" + d.St + ",cfg='" + d.Cfg + "',dt=datetime('now') WHERE id=" + d.Id, con).ExecuteNonQuery();
                    new SqliteCommand("INSERT INTO t_log(ref,msg,dt,flg) VALUES(" + d.Id + ",'dev updated',datetime('now'),0)", con).ExecuteNonQuery();
                }
                else
                {
                    new SqliteCommand("INSERT INTO t_dev(nm,loc,tp,st,cfg,dt) VALUES('" + d.Nm + "','" + d.Loc + "'," + d.Tp + "," + d.St + ",'" + d.Cfg + "',datetime('now'))", con).ExecuteNonQuery();
                    new SqliteCommand("INSERT INTO t_log(ref,msg,dt,flg) VALUES(0,'dev added " + d.Nm + "',datetime('now'),0)", con).ExecuteNonQuery();
                }
                con.Close();
                return true;
            }
            catch { return false; }
        }

        public static object Calc(int did)
        {
            SqliteConnection con = null;
            try
            {
                con = new SqliteConnection(cs);
                con.Open();

                var rd = new SqliteCommand("SELECT cfg FROM t_dev WHERE id=" + did, con).ExecuteReader();
                string cfg = "";
                if (rd.Read()) cfg = rd["cfg"] == DBNull.Value ? "" : rd["cfg"].ToString();
                rd.Close();

                double thr = 75;
                if (cfg.Contains("thr="))
                {
                    int p = cfg.IndexOf("thr=") + 4;
                    int p2 = cfg.IndexOf("|", p);
                    if (p2 < 0) p2 = cfg.Length;
                    try { thr = double.Parse(cfg.Substring(p, p2 - p)); } catch { }
                }

                string cutoff = DateTime.Now.AddHours(-1).ToString("yyyy-MM-dd HH:mm:ss");
                var rd2 = new SqliteCommand("SELECT AVG(v) avg, MAX(v) mx FROM t_dat WHERE did=" + did + " AND typ=1 AND ts>='" + cutoff + "'", con).ExecuteReader();
                double avg = 0, mx = 0;
                if (rd2.Read())
                {
                    avg = rd2["avg"] == DBNull.Value ? 0 : Math.Round(Convert.ToDouble(rd2["avg"]), 2);
                    mx = rd2["mx"] == DBNull.Value ? 0 : Math.Round(Convert.ToDouble(rd2["mx"]), 2);
                }
                rd2.Close();

                if (mx > thr)
                {
                    new SqliteCommand("INSERT INTO t_dat(did,ts,v,v2,typ,st,flg,n,dt1) VALUES(" + did + ",datetime('now')," + mx + "," + avg + ",3,1,1,'ALERT: threshold exceeded',datetime('now'))", con).ExecuteNonQuery();
                    new SqliteCommand("INSERT INTO t_log(ref,msg,dt,flg) VALUES(" + did + ",'calc did=" + did + "',datetime('now'),0)", con).ExecuteNonQuery();
                }

                return new { avg, mx, thr };
            }
            catch { }
            finally { if (con != null) try { con.Close(); } catch { } }
            return null;
        }

        public static List<Mdl.D> GetLog(string did, string flg)
        {
            var r = new List<Mdl.D>();
            SqliteConnection con = null;
            try
            {
                con = new SqliteConnection(cs);
                con.Open();
                string s = "SELECT * FROM t_log WHERE 1=1";
                if (!string.IsNullOrEmpty(did) && did != "0") s += " AND ref=" + did;
                if (!string.IsNullOrEmpty(flg) && flg != "-1") s += " AND flg=" + flg;
                s += " ORDER BY dt DESC";
                var rd = new SqliteCommand(s, con).ExecuteReader();
                while (rd.Read())
                {
                    var d = new Mdl.D();
                    d.Id = Convert.ToInt32(rd["id"]);
                    d.Did = rd["ref"] == DBNull.Value ? 0 : Convert.ToInt32(rd["ref"]);
                    d.N = rd["msg"] == DBNull.Value ? "" : rd["msg"].ToString();
                    d.Ts = rd["dt"] == DBNull.Value ? "" : rd["dt"].ToString();
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
            SqliteConnection con = null;
            try
            {
                con = new SqliteConnection(cs);
                con.Open();
                string s = "SELECT COUNT(*) total, AVG(v) avg_v, MAX(v) max_v, MIN(v) min_v, " +
                    "SUM(CASE WHEN typ=3 THEN 1 ELSE 0 END) alerts, SUM(CASE WHEN typ=1 THEN 1 ELSE 0 END) readings, MAX(ts) last_ts " +
                    "FROM t_dat WHERE did=" + did + " AND typ IN (1,3)";
                var rd = new SqliteCommand(s, con).ExecuteReader();
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
                        last = rd["last_ts"] == DBNull.Value ? "" : rd["last_ts"].ToString()
                    };
                }
            }
            catch { }
            finally { if (con != null) try { con.Close(); } catch { } }
            return null;
        }
    }
}

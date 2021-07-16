using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;

namespace WebApplication3
{
    public class Global : System.Web.HttpApplication
    {
        //http://localhost:54580/zoom/996_664/2021/07/16/3fba1cad5816ac48f5071626361954299-1626400835161.jpg?_=icdn.dantri.com.vn
        static string CacheServerWrite = ConfigurationManager.AppSettings["CacheServerWrite"];
        static string CacheServerRead = ConfigurationManager.AppSettings["CacheServerRead"];
        static IDatabase dbWrite, dbRead;
        protected void Application_Start(object sender, EventArgs e)
        {
            ConnectionMultiplexer redisRead = ConnectionMultiplexer.Connect(CacheServerRead);
            dbRead = redisRead.GetDatabase(0);
            ConnectionMultiplexer redisWrite = ConnectionMultiplexer.Connect(CacheServerWrite);
            dbWrite = redisWrite.GetDatabase(0);
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            string path = HttpUtility.UrlDecode(Request.Url.AbsolutePath).ToLower();
            if (path.Length == 1) { respons404(); return; }

            byte[] buf;
            if (dbRead.KeyExists(path))
            {
                Response.Cache.SetCacheability(HttpCacheability.Public);
                Response.Cache.SetExpires(DateTime.UtcNow.AddYears(1));

                buf = dbRead.StringGet(path);

                Response.ContentType = "image/jpg";
                Response.OutputStream.Write(buf, 0, buf.Length);
                this.CompleteRequest();
            }
            else
            {
                string ext = Path.GetExtension(path);
                switch (ext)
                {
                    case ".jpg":
                    case ".jpeg":
                    case ".png":
                    case ".ashx":
                        string host = Request.QueryString["_"];
                        if (!string.IsNullOrWhiteSpace(host))
                        {
                            try
                            {
                                string url = string.Format("https://{0}{1}", host, path);
                                using (WebClient w = new WebClient())
                                {
                                    buf = w.DownloadData(url);
                                    updateSuccess(path, buf);
                                    return;
                                }
                            }
                            catch { }
                        }
                        break;
                }
                respons404();
            }
        }
        private void respons404()
        {
            Response.StatusCode = 404;
            this.CompleteRequest();
        }

        private void updateSuccess(string path, byte[] buf)
        {
            dbWrite.StringSet(path, buf);

            Response.ContentType = "image/jpg";
            Response.OutputStream.Write(buf, 0, buf.Length);
            this.CompleteRequest();
        }
    }
}
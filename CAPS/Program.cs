using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Net.Http;
using System.Collections.Generic;

public static class Program
{
    public static void Main(string[] args)
    {
        IHttpServer server = new HttpServer(9876);
        server.Start();
    }
}

public interface IHttpServer
{
    void Start();
}

public class HttpServer : IHttpServer
{
    static Dictionary<string, string> ParseQuery(string uri)
    {
        var matches = Regex.Matches(uri, @"[\?&](([^&=]+)=([^&=#]*))", RegexOptions.Compiled);
        return matches.Cast<Match>().ToDictionary(
            m => Uri.UnescapeDataString(m.Groups[2].Value),
            m => Uri.UnescapeDataString(m.Groups[3].Value)
        );
    }

    private readonly TcpListener listener;

    public HttpServer(int port)
    {
        this.listener = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
    }

    public void Start()
    {
        this.listener.Start();
        while (true)
        {
            var client = this.listener.AcceptTcpClient();
            var buffer = new byte[10240];
            var stream = client.GetStream();
            var length = stream.Read(buffer, 0, buffer.Length);
            var incomingMessage = Encoding.UTF8.GetString(buffer, 0, length);

            Regex r = new Regex(@"GET (.+?) HTTP");
            MatchCollection mc = r.Matches(incomingMessage);

            string page = mc[0].Groups[1].Value;

            var queryString = ParseQuery(page);

            string result = "";

            if (page.Contains("clfstat"))
            {
                if (queryString.TryGetValue("map", out string map))
                {
                    if (map.StartsWith("https://cubey.hubza.co.uk/levels/uploads/level/"))
                    {
                        string level;
                        using (var wc = new System.Net.WebClient())
                            level = wc.DownloadString(map);

                        result += "<h1>Hello, world!</h1> Your URL should be: " + page + " and map query should be " + map;
                        result += "<br><br>SELECTED CLF CONTENTS:<br>" + level;
                    }
                    else
                    {
                        result = "Unverified Location";
                    }
                }
                else
                {
                    result = "couldn't parse map";
                }
            }
            else
            {
                result = "unknown page";
            }
            stream.Write(
                Encoding.UTF8.GetBytes(
                    "HTTP/1.0 200 OK" + Environment.NewLine
                    + "Content-Length: " + result.Length + Environment.NewLine
                    + "Content-Type: " + "text/html" + Environment.NewLine
                    + Environment.NewLine
                    + result
                    + Environment.NewLine + Environment.NewLine));
            Console.WriteLine("Incoming message: {0}", incomingMessage);
        }
    }
}
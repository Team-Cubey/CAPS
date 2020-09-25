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

public class Tile
{
    public Tile(string name, int id, int amount = 0)
    {
        this.name = name;
        this.id = id;
        this.amount = amount;
    }

    public string name { get; set; }
    public int id { get; set; }
    public int amount { get; set; }
    //public int LocationY { get; set; }
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
        Tile[] tiles = new Tile[]
            {
                new Tile("Land", 1),
                new Tile("Cubey", 2),
                new Tile("Key", 3),
                new Tile("Portal", 4),
                new Tile("Vertical Evilcube", 5),
                new Tile("Horizontal Evilcube", 6),
                new Tile("Evilcube Reverser", 7),
                new Tile("Evilflower", 8),
                new Tile("Reserved", 8),
                new Tile("Reserved", 9),
                new Tile("Reserved", 10),
                new Tile("Reserved", 11),
                new Tile("Reserved", 12),
                new Tile("Reserved", 13),
                new Tile("Reserved", 14),
                new Tile("Reserved", 15),
                new Tile("Jumppad", 16),
                new Tile("Evilkey", 17),
                new Tile("Evilflower Shooter", 18),
                new Tile("4D Shooter", 19),
                new Tile("Land Nocol", 20),
                new Tile("Barrier", 21),
                new Tile("Flag/Checkpoint", 22),
                new Tile("Red Gate", 23),
                new Tile("Red Gate Key", 24),
                new Tile("Green Gate", 25),
                new Tile("Green Gate Key", 26),
                new Tile("Blue Gate", 27),
                new Tile("Blue Gate Key", 28),
                new Tile("Moving Land", 29),
                new Tile("Meta Display", 30),
                new Tile("Teleportal", 31),
                new Tile("Reserved", 32),
                new Tile("Land Reverser", 33),
                new Tile("Heart", 34),
                new Tile("Evilheart", 35)
            };

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
                    if (map.StartsWith("https://cubey.hubza.co.uk/"))
                    {
                        string levelcontents;
                        using (var wc = new System.Net.WebClient())
                            levelcontents = wc.DownloadString(map);

                        // code stripped from cubey's adventures

                        string level = levelcontents.Substring(levelcontents.LastIndexOf(']') + 1);
                        int pFrom = levelcontents.IndexOf("[META]") + "[META]".Length;
                        int pTo = levelcontents.LastIndexOf("[LEVEL]");
                        string meta = levelcontents.Substring(pFrom, pTo - pFrom);
                        meta = Regex.Replace(meta, @"^\s+$[\r\n]*", string.Empty, RegexOptions.Multiline);
                        level = Regex.Replace(level, @"^\s+$[\r\n]*", string.Empty, RegexOptions.Multiline);
                        char[] delims = new[] { '\r', '\n' };
                        string[] levels = level.Split(delims, StringSplitOptions.RemoveEmptyEntries);

                        // is no longer

                        foreach (string line in levels)
                        {
                            string[] dataChunks = line.Split(',');
                            float x = float.Parse(dataChunks[0]);
                            float y = float.Parse(dataChunks[1]);
                            float rotation = float.Parse(dataChunks[2]);
                            int id = int.Parse(dataChunks[3]);
                            tiles[id].amount += 1;
                        }

                        //result += "<h1>Hello, world!</h1> Your URL should be: " + page + " and map query should be " + map;

                        string tiles_json = "";

                        int count = 0;

                        foreach (Tile ea in tiles)
                        {
                            //result += ea.amount + " " + ea.name + "s | ";
                            tiles_json += "\"" + ea.name + "\": { \"amount\": \"" + ea.amount + "\" },";
                            ea.amount = 0;
                            count += 1;
                        }

                        var index = tiles_json.LastIndexOf(',');
                        if (index >= 0)
                        {
                            tiles_json = tiles_json.Substring(0, index);
                            Console.WriteLine(result);
                        }

                        result = "{ \"tiles\": { " + tiles_json + " } }";
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
                    + "Content-Type: " + "application/json" + Environment.NewLine
                    + Environment.NewLine
                    + result
                    + Environment.NewLine + Environment.NewLine));
            Console.WriteLine("Incoming message: {0}", incomingMessage);
        }
    }
}
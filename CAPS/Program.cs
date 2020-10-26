using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Net.Http;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.IO;

public static class Program
{
    public static void Main(string[] args)
    {
        IHttpServer server = new HttpServer(50959);
        server.Start();
    }
}

public class Tile
{
    public Tile(string name, int id, int amount = 0, string image = "unknown")
    {
        this.name = name;
        this.id = id;
        this.amount = amount;
        this.image = image;
    }

    public string name { get; set; }
    public int id { get; set; }
    public int amount { get; set; }
    public string image { get; set; }
    //public int LocationY { get; set; }
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
        this.listener = new TcpListener(IPAddress.Parse("0.0.0.0"), port);
    }

    public bool dont = false;
    public Tile[] tiles;

    public void generatetiles()
    {
        tiles = new Tile[]
            {
                new Tile("Land", 1, 0, "https://cubey.hubza.co.uk/img/tiles/land.png"),
                new Tile("Cubey", 2, 0, "https://cubey.hubza.co.uk/img/tiles/cubey.png"),
                new Tile("Key", 3, 0, "https://cubey.hubza.co.uk/img/tiles/key.png"),
                new Tile("Portal", 4, 0, "https://cubey.hubza.co.uk/img/tiles/portal.png"),
                new Tile("Vertical Evilcube", 5, 0, "https://cubey.hubza.co.uk/img/tiles/killcube.png"),
                new Tile("Horizontal Evilcube", 6, 0, "https://cubey.hubza.co.uk/img/tiles/killcube.png"),
                new Tile("Evilcube Reverser", 7, 0, "https://cubey.hubza.co.uk/img/tiles/debugcube.png"),
                new Tile("Evilflower", 8, 0, "https://cubey.hubza.co.uk/img/tiles/flower.png"),
                new Tile("Reserved", 8, 0, "unknown"),
                new Tile("Reserved", 9, 0, "unknown"),
                new Tile("Reserved", 10, 0, "unknown"),
                new Tile("Reserved", 11, 0, "unknown"),
                new Tile("Reserved", 12, 0, "unknown"),
                new Tile("Reserved", 13, 0, "unknown"),
                new Tile("Reserved", 14, 0, "unknown"),
                new Tile("Reserved", 15, 0, "unknown"),
                new Tile("Jumppad", 16, 0, "https://cubey.hubza.co.uk/img/tiles/jumppad.png"),
                new Tile("Evilkey", 17, 0, "https://cubey.hubza.co.uk/img/tiles/evilkey.png"),
                new Tile("Evilflower Shooter", 18, 0, "https://cubey.hubza.co.uk/img/tiles/flower.png"),
                new Tile("4D Shooter", 19, 0, "https://cubey.hubza.co.uk/img/tiles/fireballshooter-4d.png"),
                new Tile("Land Nocol", 20, 0, "https://cubey.hubza.co.uk/img/tiles/land-nocol.png"),
                new Tile("Barrier", 21, 0, "unknown"),
                new Tile("Flag/Checkpoint", 22, 0, "https://cubey.hubza.co.uk/img/tiles/flag.png"),
                new Tile("Red Gate", 23, 0, "https://cubey.hubza.co.uk/img/tiles/gate-red.png"),
                new Tile("Red Gate Key", 24, 0, "https://cubey.hubza.co.uk/img/tiles/key-red.png"),
                new Tile("Green Gate", 25, 0, "https://cubey.hubza.co.uk/img/tiles/gate-green.png"),
                new Tile("Green Gate Key", 26, 0, "https://cubey.hubza.co.uk/img/tiles/key-green.png"),
                new Tile("Blue Gate", 27, 0, "https://cubey.hubza.co.uk/img/tiles/gate-blue.png"),
                new Tile("Blue Gate Key", 28, 0, "https://cubey.hubza.co.uk/img/tiles/key-blue.png"),
                new Tile("Moving Land", 29, 0, "https://cubey.hubza.co.uk/img/tiles/land.png"),
                new Tile("Meta Display", 30, 0, "unknown"),
                new Tile("Teleportal", 31, 0, "unknown"),
                new Tile("Reserved", 32, 0, "unknown"),
                new Tile("Land Reverser", 33, 0, "https://cubey.hubza.co.uk/img/tiles/debugcube.png"),
                new Tile("Heart", 34, 0, "https://cubey.hubza.co.uk/img/tiles/heart.png"),
                new Tile("Evilheart", 35, 0, "https://cubey.hubza.co.uk/img/tiles/evilheart.png")
            };
    }

    public string goclfstat(string map)
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

        string tiles_json = "";

        int count = 0;

        foreach (Tile ea in tiles)
        {
            //result += ea.amount + " " + ea.name + "s | ";
            tiles_json += "\"" + ea.name + "\": { \"name\": \"" + ea.name + "\", \"id\": \"" + ea.id + "\",  \"amount\": \"" + ea.amount + "\", \"image\": \"" + ea.image + "\" },";
            ea.amount = 0;
            count += 1;
        }

        var index = tiles_json.LastIndexOf(',');
        if (index >= 0)
        {
            tiles_json = tiles_json.Substring(0, index);
        }

        return "{ \"tiles\": { " + tiles_json + " } }";
    }

    public void writestream(string result, Stream stream, string incomingMessage, string type)
    {
        stream.Write(
                    Encoding.UTF8.GetBytes(
                        "HTTP/1.0 200 OK" + Environment.NewLine
                        + "Content-Length: " + result.Length + Environment.NewLine
                        + "Content-Type: " + type + Environment.NewLine
                        + Environment.NewLine
                        + result
                        + Environment.NewLine + Environment.NewLine));
        Console.WriteLine("Incoming message: {0}", incomingMessage);
    }

    public void Start()
    {
        generatetiles();

        this.listener.Start();
        Console.WriteLine("Started CAPS server");
        while (true)
        {
            try
            {
                var client = this.listener.AcceptTcpClient();
                var buffer = new byte[10240];
                var stream = client.GetStream();
                var length = stream.Read(buffer, 0, buffer.Length);
                var incomingMessage = Encoding.UTF8.GetString(buffer, 0, length);

                MatchCollection mc;

                string result = "";

                if (incomingMessage.Contains("GET"))
                {
                    Regex r = new Regex(@"GET (.+?) HTTP");
                    mc = r.Matches(incomingMessage);
                }
                else if (incomingMessage.Contains("POST"))
                {
                    Regex r = new Regex(@"GET (.+?) POST");
                    mc = r.Matches(incomingMessage);
                }
                else
                {
                    Regex r = new Regex(@"(.+?)");
                    mc = r.Matches(incomingMessage);
                    dont = true;
                }

                string page = mc[0].Groups[1].Value;

                var queryString = ParseQuery(page);

                if (page.Contains("clfstat"))
                {
                    if (queryString.TryGetValue("map", out string map))
                    {
                        if (map.StartsWith("https://cubey.cc/")
                        {
                            result = goclfstat(map);
                            writestream(result, stream, incomingMessage, "application/json");
                        }
                        else
                        {
                            writestream("Unverified CLF upload location", stream, incomingMessage, "text/html");
                        }
                    }
                    else
                    {
                        writestream("Map not set", stream, incomingMessage, "text/html");
                    }
                }
                else
                {
                    writestream("Unknown Page. 404", stream, incomingMessage, "text/html");
                }
            }catch (Exception e)
            {
                Console.WriteLine("Error: " + e);
            }
        }
    }
}
using System.CommandLine;
using System.Net;

Option<string> portOption = new("--port")
{
    Description = "The port where the local server will run"
};

Option<string> urlOption = new("--origin")
{
    Description = "The url where the app will send the requests"
};

RootCommand rootCommand = new("Caching proxy");
rootCommand.Options.Add(portOption);
rootCommand.Options.Add(urlOption);
ParseResult parseResult = rootCommand.Parse(args);

HttpListener listener = new();

string port = parseResult.GetValue(portOption) ?? "8080";
string serverUrl = parseResult.GetValue(urlOption) ?? "http://dummyjson.com";

string prefix = $"http://localhost:{port}/";

listener.Prefixes.Add(prefix);
listener.Start();

Dictionary<string, string> cache = [];

Console.WriteLine($"Listening to {prefix}");

while (true)
{
    HttpListenerContext context = listener.GetContext();
    HttpListenerRequest request = context.Request;
    HttpListenerResponse contextResponse = context.Response;
    Console.WriteLine("Request received...");

    string rawURl = request.RawUrl!;

    string responseBody = string.Empty;
    string httpURL = serverUrl + rawURl;

    if (cache.ContainsKey(httpURL))
    {
        responseBody = cache[httpURL];
        contextResponse.AddHeader("X-Cache", "HIT");
        SendResponse(contextResponse, responseBody);
        continue;
    }

    using HttpClient client = new();
    var httpResponse = await client.GetAsync(httpURL);
    string content = await httpResponse.Content.ReadAsStringAsync();

    cache.Add(httpURL, content);

    responseBody = content;
    contextResponse.AddHeader("X-Cache", "MISS");

    SendResponse(contextResponse, responseBody);
}


static void SendResponse(HttpListenerResponse response, string res)
{
    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(res);
    response.ContentLength64 = buffer.Length;
    Stream output = response.OutputStream;
    output.Write(buffer, 0, buffer.Length);
    output.Close();
}
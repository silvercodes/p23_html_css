using System.Net;
using System.Text;
using HeyRed.Mime;

namespace FileServer;

internal class Server
{
    private HttpListener listener = null!;
    private string host = "127.0.0.1";
    private int port;
    private string[] indexFiles =
        [
            "index.html",
        ];
    public required string RootDirectory { get; set; }

    public Server(int port = 80)
    {
        this.port = port;

        InitServer();
    }
    private void InitServer()
    {
        listener = new HttpListener();
        listener.Prefixes.Add($@"http://{host}:{port}/");           // <-- LAST SLASH!
    }

    public async Task StartAsync()
    {
        listener.Start();
        Console.WriteLine($"Server started at http://{host}:{port}");

        while (true)
        {
            try
            {
                HttpListenerContext ctx = await listener.GetContextAsync();

                _ = Task.Run(() => HandleRequest(ctx));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection error: {ex.Message}");
            }
        }
    }

    private async Task HandleRequest(HttpListenerContext ctx)
    {
        string? path = ctx.Request.Url?.AbsolutePath;

        await Console.Out.WriteLineAsync($"Path requsted: {path}");

        path = path.Trim('/');

        if (string.IsNullOrEmpty(path))
        {
            foreach (string indexFile in indexFiles)
            {
                if (File.Exists(Path.Combine(RootDirectory, indexFile)))
                {
                    path = indexFile;
                    break;
                }
            }
        }

        string filePath = Path.Combine(RootDirectory, path);

        if (File.Exists(filePath))
        {
            try
            {
                using FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);

                ctx.Response.ContentLength64 = fs.Length;
                ctx.Response.ContentType = MimeTypesMap.GetMimeType(filePath);

                DateTime lm = File.GetLastWriteTime(filePath);
                ctx.Response.AddHeader("Last-Modified", $"{lm.ToShortDateString()} {lm.ToLongTimeString()}");

                ctx.Response.AddHeader("X-Custom", "Hello");

                await fs.CopyToAsync(ctx.Response.OutputStream);

                ctx.Response.StatusCode = (int)HttpStatusCode.OK;

                await fs.FlushAsync();
            }
            catch (Exception ex)
            {
                ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
        }
        else
        {
            ctx.Response.StatusCode = (int)HttpStatusCode.NotFound;
        }

        ctx.Response.Close();
    }
}

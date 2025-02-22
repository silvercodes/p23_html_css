
using FileServer;

Server server = new Server(8080)
{
    RootDirectory = @"C:\itstep\p23\p23_html_css",
};

await server.StartAsync();

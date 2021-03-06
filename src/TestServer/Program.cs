﻿using System;
using System.IO;
using System.Net;
using JwtSessions;
using Red;
using Red.CommonMarkRenderer;
using Red.CookieSessions;
using Red.EcsRenderer;

namespace TestServer
{
    public class Program
    {
        class MySess
        {
            public string Username;
        }

        public static void Main(string[] args)
        {
            // We serve static files, such as index.html from the 'public' directory
            var server = new RedHttpServer(5000, "public");
            server.Use(new EcsRenderer());
          
            server.Use(new CookieSessions<MySess>(new CookieSessionSettings(TimeSpan.FromDays(1))
            {
                Secure = false,
                AutoRenew = true,
                ShouldAuthenticate = path => !path.Contains("login") // We allow people to send requests to /login, where we can authenticate them
            }));
            var startTime = DateTime.UtcNow;

            // URL param demo
            server.Get("/:param1/:paramtwo/:somethingthird",
                async (req, res) =>
                {
                    await res.SendString(
                        $"URL: {req.Parameters["param1"]} / {req.Parameters["paramtwo"]} / {req.Parameters["somethingthird"]}");
                });

            server.Get("/login", async (req, res) =>
            {
                // To make it easy to test the session system only using the browser and no credentials
                req.OpenSession(new MySess {Username = "benny"});
                await res.SendStatus(HttpStatusCode.OK);
            });
            
            server.Get("/jwtlogin", async (req, res) =>
            {
                // To make it easy to test the session system only using the browser and no credentials
                await res.SendJwtToken(new MySess {Username = "benny"});
            });
            server.Get("/logout", async (req, res) =>
            {
                // To make it easy to test the session system only using the browser and no credentials

                req.GetSession<MySess>().Close(req);
                await res.SendStatus(HttpStatusCode.OK);
            });
            server.Post("/login", async (req, res) =>
            {
                // Here we could authenticate the user properly, with credentials sent in a form, or similar
                req.OpenSession(new MySess {Username = "benny"});
                await res.SendStatus(HttpStatusCode.OK);
            });

            // Redirect to page on same host
            server.Get("/redirect", async (req, res) => { await res.Redirect("/redirect/test/here"); });

            // Save uploaded file from request body 
            Directory.CreateDirectory("uploads");
            server.Post("/upload", async (req, res) =>
            {
                if (await req.SaveFiles("uploads"))
                    await res.SendString("OK");
                else
                    await res.SendString("Error", status: HttpStatusCode.NotAcceptable);
            });

            server.Get("/file", async (req, res) => { await res.SendFile("testimg.jpeg"); });

            // Handling formdata from client
            server.Post("/formdata", async (req, res) =>
            {
                var form = await req.GetFormDataAsync();
                await res.SendString("Hello " + form["firstname"]);
            });

            // Using url queries to generate an answer
            server.Get("/hello", async (req, res) =>
            {
                var session = req.GetData<MySess>();
                var queries = req.Queries;
                await res.SendString(
                    $"Hello {queries["firstname"]} {queries["lastname"]}, you are logged in as {session.Username} - have a nice day");
            });

            // Render a markdown file to test 
            server.Get("/markdown", async (req, res) => { await res.RenderFile("markdown.md"); });

            // Rendering a page for dynamic content
            server.Get("/serverstatus", async (req, res) =>
            {
                await res.RenderPage("pages/statuspage.ecs", new RenderParams
                {
                    {"uptime", (int) DateTime.UtcNow.Subtract(startTime).TotalSeconds},
                    {"version", RedHttpServer.Version}
                });
            });

            // WebSocket echo server
            server.WebSocket("/echo", async (req, wsd) =>
            {
                await wsd.SendText("Welcome to the echo test server");
                wsd.OnTextReceived += (sender, eventArgs) => { wsd.SendText("you sent: " + eventArgs.Text); };
            });
            server.Start();
            Console.ReadKey();
        }
    }
}
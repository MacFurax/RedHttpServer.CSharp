﻿using System.Threading.Tasks;

namespace Red.Interfaces
{
    /// <summary>
    ///     Interface for middleware that handle websocket middleware 
    /// </summary>
    public interface IRedWebSocketMiddleware : IRedExtension
    {
        /// <summary>
        ///     Method called for every websocket request to the server
        /// </summary>
        /// <param name="path">The path of the request, relative to the server.</param>
        /// <param name="req">The incoming request</param>
        /// <param name="wsd">The websocket dialog</param>
        /// <returns>Whether to continue through the middleware stack</returns>
        Task<bool> Process(string path, Request req, WebSocketDialog wsd);
    }
}
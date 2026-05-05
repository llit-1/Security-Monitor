using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Security_Monitor.Services
{
    public class HttpServer
    {
        private readonly string _url;
        private HttpListener _listener;

        public event Action<int, string, string, int, string> OnIdReceived;

        public HttpServer()
        {
            _url = "http://" + GetLocalIPAddress() + ":6000/";
        }

        public void Start()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(_url);
            _listener.Start();
            _ = Task.Run(ListenLoop);
        }

        private async Task ListenLoop()
        {
            while (_listener.IsListening)
            {
                try
                {
                    var ctx = await _listener.GetContextAsync();
                    _ = Task.Run(() => HandleRequest(ctx));
                }
                catch { }
            }
        }

        private async Task HandleRequest(HttpListenerContext context)
        {
            try
            {
                var idStr = context.Request.QueryString["id"];
                var time = context.Request.QueryString["time"];
                var reader = context.Request.QueryString["reader"];
                var statusStr = context.Request.QueryString["status"];
                var code = context.Request.QueryString["code"];
                if (int.TryParse(idStr, out int id) && int.TryParse(statusStr, out int status))
                {
                    OnIdReceived?.Invoke(id, reader, time, status, code);
                }
            }
            catch

            {

            }
            finally
            {
                context.Response.Close();
            }
        }
        private static string GetLocalIPAddress()
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530); // подключаемся к внешнему адресу
            var endPoint = socket.LocalEndPoint as IPEndPoint;
            return endPoint?.Address.ToString() ?? "127.0.0.1";
        }
    }
}

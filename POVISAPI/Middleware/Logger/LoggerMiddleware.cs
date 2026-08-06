namespace POVISAPI.Middleware.Logger
{
    public class LoggerMiddleware
    {
        private readonly RequestDelegate _next;
        public LoggerMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        public async Task InvokeAsync(HttpContext context)
        {
            var method = context.Request.Method;
            var path = context.Request.Path;
            var time = DateTime.Now.ToString("MMM dd, yyyy hh:mm:ss tt");
            var ip = context.Connection.RemoteIpAddress;

            Console.WriteLine($"Incoming request: {method} | {path} | {time} | {ip}");

            await _next(context);

            Console.WriteLine($"Outgoing: {method} | {path} | {time} | {ip} | {context.Response.StatusCode}");
        }
    }
}

using System.Net.WebSockets;
using System.Text;

namespace WebSocket;

public class Socket
{
    public static void openWebSocket(IApplicationBuilder App, IWebHostEnvironment env)
    {

        App.UseWebSockets();
        App.UseRouting();

        App.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();

            endpoints.Map("/ws", async context =>
            {

                if (context.WebSockets.IsWebSocketRequest)
                {
                    var WebSocket = await context.WebSockets.AcceptWebSocketAsync();
                    Console.WriteLine("WebSocket conectado com sucesso!");
                    await HandleWebSocketAsync(WebSocket);
                }
                else
                {
                    context.Response.StatusCode = 400;
                    Console.WriteLine("Error no WebSocket!");
                }
            });
        });
    }

    public static async Task HandleWebSocketAsync(System.Net.WebSockets.WebSocket WebSocket){
        Timer timer = new Timer(async _ =>{
            if(WebSocket.State == WebSocketState.Open){
                var statusMessage = Encoding.UTF8.GetBytes("Server is running");
                await WebSocket.SendAsync(new ArraySegment<byte>(statusMessage), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }, null, 0 , 5000);
        
        async Task ReceiveMessageAsync(){
            try{
                var buffer = new byte[1024 * 4];
                var result = await WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if(result.MessageType == WebSocketMessageType.Close){
                    await WebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    timer.Dispose();
                    return;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Console.WriteLine($"Mensagem recebida {message}");

                await ReceiveMessageAsync();
            }catch (Exception ex)
        {
            Console.WriteLine($"Erro no WebSocket: {ex.Message}");
            timer.Dispose(); 
        }
        }
            await ReceiveMessageAsync();
    }

}
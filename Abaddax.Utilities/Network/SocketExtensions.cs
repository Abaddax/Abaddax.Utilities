using System.Net.Sockets;

namespace Abaddax.Utilities.Network
{
    public static class SocketExtensions
    {
        public static bool IsConnected(this Socket socket, bool closeDisconnectedSocket = true)
        {
            ArgumentNullException.ThrowIfNull(socket);

            if (socket.Connected == false)
                return false;

            //Poll client
            try
            {
                //Check if client is still connected
                if (socket.Poll(0, SelectMode.SelectRead))
                {
                    Span<byte> buff = stackalloc byte[1];
                    if (socket.Receive(buff, SocketFlags.Peek) == 0)
                    {
                        //Client is diconnected
                        if (closeDisconnectedSocket)
                            socket.Close();
                        return false;
                    }
                }
                return true;
            }
            catch (Exception)
            {
                //Client is diconnected
                if (closeDisconnectedSocket)
                    socket.Close();
                return false;
            }
        }
    }
}

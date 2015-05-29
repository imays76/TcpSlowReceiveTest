using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TcpSlowReceiveTest
{
    class Program
    {
        static void Main(string[] args)
        {
            // Intentionally uses non-block socket instead of XXXAsync functions for freeing from awkard implementation in Mono.
            try
            {
                bool isSender;
                if (args.Length >= 2 && args[0] == "send")
                    isSender = true;
                else
                    if (args.Length == 1 && args[0] == "receive")
                        isSender = false;
                    else
                    {
                        Console.WriteLine("<Arguments>");
                        Console.WriteLine("send <hostname> => connects to server and tries sending tons of data endlessly.");
                        Console.WriteLine("receive => accepts connections and receives very slowly.");
                        return;
                    }


                long prevTick = DateTime.Now.Ticks;

                if (isSender)
                {
                    var receiverAddr = args[1];
                    Console.WriteLine("Connecting to {0}...", receiverAddr);
                    Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    socket.Connect(receiverAddr, 34521);
                    socket.Blocking = false;
                    Console.WriteLine("Connected to {0}. non-blocking endlessly. of course, a lot of would block will occur. Let's see if connection is lost in minutes.", receiverAddr);
                    Console.WriteLine("It is {0} now.", DateTime.Now);
                    Console.WriteLine("Local addr is {0}.", socket.LocalEndPoint.ToString());
                    Console.WriteLine("Press ctrl-c to exit.");
                    int successCount = 0;
                    int wouldBlockErrorCount = 0;

                    while (true)
                    {
                        try
                        {
                            // 아~주 빠르게 죽어라 송신한다.
                            socket.Send(new byte[10000]);
                            successCount++;
                        }
                        catch (SocketException e)
                        {
                            if (e.SocketErrorCode != SocketError.WouldBlock)
                            {
                                Console.WriteLine("Disconnected! Error={0}", e.ToString());
                                break;
                            }
                            else
                            {
                                wouldBlockErrorCount++;
                            }
                        }

                        if (DateTime.Now.Ticks - prevTick > 10000 * 1000) //1sec
                        {
                            prevTick = DateTime.Now.Ticks;
                            Console.WriteLine("Send ok: {0} WouldBlock: {1}", successCount, wouldBlockErrorCount);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Accepts connection and receives stream very slowly.");

                    Socket listenSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                    listenSocket.Bind(new IPEndPoint(IPAddress.Any, 34521));
                    listenSocket.Listen(5);
                    listenSocket.Blocking = false;
                    Console.WriteLine("Waiting for connection...");
                    List<Socket> tcpConnections = new List<Socket>();

                    while (true)
                    {
                        try
                        {
                            Socket socket = listenSocket.Accept();

                            Console.WriteLine("Accepted a connection.");
                            socket.Blocking = false;
                            tcpConnections.Add(socket);
                        }
                        catch (SocketException e)
                        {
                            if (e.SocketErrorCode != SocketError.WouldBlock)
                            {
                                Console.WriteLine("Accept failed! Error={0}", e.ToString());
                                break;
                            }
                            else
                            {
                                // ignore wouldblock error by non-block accept
                            }
                        }

                        // 아~주 느리게 수신을 처리한다.
                        if (DateTime.Now.Ticks - prevTick > 1000 * 10000) // 1sec
                        {
                            prevTick = DateTime.Now.Ticks;
                            foreach (var s in tcpConnections)
                            {
                                try
                                {
                                    s.Receive(new byte[1000]);
                                }
                                catch (SocketException e)
                                {
                                    if (e.SocketErrorCode != SocketError.WouldBlock)
                                    {
                                        Console.WriteLine("Disconnected! Error={0}", e.ToString());
                                        tcpConnections.Remove(s);
                                    }
                                    else
                                    {
                                        // ignore
                                    }
                                }
                            }

                            Console.WriteLine("There are {0} TCP connections.", tcpConnections.Count);
                        }


                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.Write("Press Enter to exit.");
                Console.ReadLine();
            }
        }
    }
}

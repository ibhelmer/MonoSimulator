using System;                       // For Console, Int32, ArgumentException, Environment
using System.Text;
using System.Net;                   // For IPAddress
using System.Net.Sockets;           // For TcpListener, TcpClient
using System.Threading.Tasks;       // For Treading

namespace MonoSimulator
{
    class MonoSimulator
    {
        // Constant
        private const string VER = "V1.0/IHN/UCN";  // Title line in Console 
        private const int BUFSIZE = 32;             // Size of send buffer
        private const int INIT      = 0;
        private const int IDLE      = 1;
        private const int NEWCMD    = 2;
        private const int TEMP      = 3;
        private const int DATE      = 4;
        private const int HUMID     = 5;
        private const int ACC       = 6;
        private const int ERROR     = 7;

        // Public Methode
        public static string GetLocalIPAddress()    // Get the local IP address for the MonoSimulator
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("Local IP Address Not Found!");
        }
        public static void SetConColor(ConsoleColor bg, ConsoleColor fg)    // Methode for setting color 
        {
            Console.BackgroundColor = bg;
            Console.Clear();
            Console.ForegroundColor = fg;
        }
        public static void SetConColor(ConsoleColor fg)                     // Overloaded SetColor only setting foreground color  
        {
            Console.ForegroundColor = fg;
        }
        public static void ResetConColor()                                  // Back to default color
        {
            Console.ResetColor();
        }
        public static void GotoXPos(int x)                                  // Place cursor x posision for current pos
        {
            for (int i=0; i<x; i++)
            {
                Console.Write(" ");     // Add one white space
            }
        }
        public static void CntrWrite(string str)                            // Print str string center in consoul window
        {
            int posX;
            if ((Console.WindowWidth-str.Length)>0)
            {
                posX = (int)(Console.WindowWidth - str.Length) / 2;
            }
            else
            {
                posX = 0;
            }
            GotoXPos(posX);
            Console.WriteLine(str);
        }
        public static void Info()                                           // Print info text
        {
            SetConColor(ConsoleColor.DarkBlue, ConsoleColor.White);
            CntrWrite("----=***=----");
            CntrWrite("-----= MonoSimulator "+VER+" =-----");
            SetConColor(ConsoleColor.Yellow);
            Console.WriteLine("Make sure that firewall is turned off or TCP port 7913 is open for ingoing trafic");
            Console.WriteLine("The following address is not correct if you are accessing MonoSimulator from ");
            Console.WriteLine("the outerside of a NAT firewall and last only one client can access at a time !!!");  
            SetConColor(ConsoleColor.Gray);
            Console.WriteLine("Local ip address of MonoSimulator is : {0}", GetLocalIPAddress());
        }
        public static double ConvertToUnixTimestamp(DateTime date)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan diff = date.ToUniversalTime() - origin;
            return Math.Floor(diff.TotalSeconds);
        }

        static void Main(string[] args)
        {
            const int servPort = 7913;            // Assign serverport 7913 for MonoSimulator
            int rcv;
            int state = IDLE;
            TcpListener listener = null;
            byte[] sendResp = Encoding.ASCII.GetBytes("");
            try
            {
                // Create a TCPListener to accept client connections
                listener = new TcpListener(IPAddress.Any, servPort);  // Listens on all my PC's IP adresses
                listener.Start();                                     // Start listening
            }
            catch (SocketException se)      // Catch error   
            {
                Console.WriteLine(se.ErrorCode + ": " + se.Message);
                Environment.Exit(se.ErrorCode);
            }
            
            Info();
            for (;;)
            {                                                       // Run forever, accepting and servicing connections

                TcpClient client = null;
                NetworkStream netStream = null;
                try
                {
                    client = listener.AcceptTcpClient();            // Get client connection
                    netStream = client.GetStream();
                    Console.WriteLine("Client connected with ip address     : {0}", ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString());

                    // Receive until client closes connection, indicated by 0 return value
                   // while ((bytesRcvd = netStream.Read(rcvBuffer, 0, rcvBuffer.Length)) > 0)
                    while((rcv = netStream.ReadByte())>0)
                    {
                        //netStream.Write(rcvBuffer, 0, bytesRcvd);
                        switch(state)
                        {
                            case IDLE   :  if (rcv == '*') { state = NEWCMD;} break;
                            case NEWCMD :  switch(rcv)
                                           {
                                              case 'T':
                                              case 't': state = TEMP;
                                                        break;
                                              case 'H':
                                              case 'h': state = HUMID;
                                                        break;
                                              case 'd':
                                              case 'D': state = DATE;
                                                        break;
                                              case 'A':
                                              case 'a': state = ACC;
                                                        break;
                                              default : state = IDLE; break; 
                                           }
                                           break;
                            case TEMP   :  sendResp = Encoding.ASCII.GetBytes("t23.20\n\r");
                                           netStream.Write(sendResp, 0, sendResp.Length);
                                           Console.WriteLine("Recieved cmd->*T");
                                           Console.WriteLine("Send response->t23.20");
                                           state = IDLE;
                                           break;
                            case HUMID  :  sendResp = Encoding.ASCII.GetBytes("h45.00\n\r");
                                           netStream.Write(sendResp, 0, sendResp.Length);
                                           Console.WriteLine("Recieved cmd->*H");
                                           Console.WriteLine("Send response->h45.00");
                                           state = IDLE;
                                           break;
                            case DATE   :  DateTime saveNow = DateTime.Now;
                                           double unixTime = ConvertToUnixTimestamp(saveNow);
                                           sendResp = Encoding.ASCII.GetBytes('d' + unixTime.ToString() + "\n\r");
                                           netStream.Write(sendResp, 0, sendResp.Length);
                                           Console.WriteLine("Recieved cmd->*D");
                                           Console.WriteLine("Send response->d{0}",unixTime.ToString());
                                           state = IDLE;
                                           break;
                            case ACC    :  sendResp = Encoding.ASCII.GetBytes("x0.23y0.54z0.21\n\r");
                                           netStream.Write(sendResp, 0, sendResp.Length);
                                           Console.WriteLine("Recieved cmd->*A");
                                           Console.WriteLine("Send response->x0.23y0.54z0.21");
                                           state = IDLE;
                                           break;
                            defaule     :  state = IDLE;
                                           SetConColor(ConsoleColor.Red);
                                           Console.WriteLine("Unknown command!!");
                                           ResetConColor();
                        }
                    }
                    // Close the stream and socket. We are done with this client!
                    netStream.Close();
                    client.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    netStream.Close();
                }
            }
        }
    }
}

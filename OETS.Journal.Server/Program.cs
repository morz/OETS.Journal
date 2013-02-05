using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime;
using System.Threading;
using OETS.Shared.Util;
using System.Collections;
using OETS.Shared.Structures;
using NLog;
using System.IO;

namespace OETS.Server
{
    public class Program
    {
        #region Members
        private static SocketServer server;

        private static readonly Logger s_log = LogManager.GetCurrentClassLogger();
        public static string Title = string.Empty;
        public static System.Reflection.Assembly asm = System.Reflection.Assembly.GetEntryAssembly();
        #endregion

        static void Main(string[] args)
        {
            try
            {
                AppDomain.CurrentDomain.UnhandledException += 
                    (sender, arg) => LogUtil.FatalException(arg.ExceptionObject as Exception,"Неизвестная ошибко!");

                string Header = "/**\n" +
                                 "* Продукт: Сервер журнала передачи смены.\n"+
                                 "* Версия: 001 $\n" +
                                 "* Автор: Егоров Данил;\n"+
                                 "* таб.№: 03776024\n" +
                                 "* */\n";
                string rev = "Версия: 001";
                rev = rev.Replace('$', ' ');

                if (asm != null)
                    Title = asm.GetName().Name + ". " +rev;

                Console.Title = Title;
                Console.SetWindowSize(120, 50);
                Console.Write(Header);
                s_log.Info("Загрузка данных из базы данных...");
                if (GCSettings.IsServerGC)
                {
                    GCSettings.LatencyMode = GCLatencyMode.Batch;
                }
                else
                {
                    GCSettings.LatencyMode = GCLatencyMode.Interactive;
                }
                
                Start();
            }
            catch (Exception exc)
            {
                LogUtil.ErrorException(exc, false, "Ошибка запуска сервера.");
                Console.ReadKey(true);
                Environment.Exit(1);
            }
        }
        #region Command Handler

        #region PrintINFO
#endregion PrintINFO

        #endregion

        public static bool Start()
        {
            s_log.Info("Загрузка данных из базы данных ЗАВЕРШЕНА.");
            server = SocketServer.Instance;
            server.Started += server_Started;
            server.LoginSuccess += server_LoginSuccess;
            server.LoginFailed += server_LoginFailed;
            server.Stopped += server_Stopped;
            server.ClientDisconnected += server_ClientDisconnected;
            server.Start();
            Thread.CurrentThread.IsBackground = true;
            ServerConsole.Run();            
            return true;
        }

        static void server_ClientDisconnected(object sender, ClientEventArgs ea)
        {
            s_log.Info("Клиент с {0} удачно отключён.", ea.ClientManager.IPEndPoint.ToString());
        }

        static void server_Stopped(object sender, OETS.Shared.TimedEventArgs ea)
        {
            s_log.Info("Сервер успешно остановлен");
        }

        static void server_LoginFailed(object sender, ClientEventArgs ea)
        {
            s_log.Info("{0}", ea.Args.ToString());
        }

        static void server_LoginSuccess(object sender, ClientEventArgs ea)
        {
            s_log.Info("Клиент с {0} удачно авторизирован.", ea.ClientManager.IPEndPoint.ToString());
        }

        static void server_Started(object sender, OETS.Shared.TimedEventArgs ea)
        {
            s_log.Info("Сервер удачно запущен");
        }

        public static void Quit()
        {
            server.Stop();
            Environment.Exit(-1);
        }
    }
}

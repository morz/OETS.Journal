using System;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using OETS.Shared.Util;
using System.Net;
using NLog;

namespace OETS.Server
{
    internal static class ServerConsole
    {
        private static readonly Logger s_log = LogManager.GetCurrentClassLogger();
        internal static void Run()
        {
            s_log.Info("Консоль загружена!!  Нажмите \"?\" для помощи.");

            string line;
            while ((line = Console.ReadLine()) != null)
            {
                line = line.ToLower();
                string[] c = line.Split(' ');                
                lock (Console.Out)
                {
                    switch (c[0])
                    {
                        case "?":
                        case "help":                            
                            if (c.Length >= 2)
                            {
                                string sc = c[1].ToLower();
                                switch (sc)
                                {
                                    case "exit":
                                        s_log.Trace("Выход, завершение работы");
                                        break;
                                }
                            }
                            else
                                s_log.Trace(
                                "Доступные команды: ?, help <command>, Exit"
                                );
                            break;
                        case "exit":
                        case "quit":
                        case "quiti":
                            Program.Quit();
                            break;
                        default:
                            s_log.Error("==> {0} - Неизвестная команда! Напишите \"?\" для помощи.", line);
                            break;
                    }
                }
            }
        }
    }
}
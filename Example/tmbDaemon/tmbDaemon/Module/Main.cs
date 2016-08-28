using Nancy;
using System;
using System.Linq;
using TheMessageBus.TcpTransport;
using System.Collections.Generic;
using mbDaemon;

namespace Daemon.Module
{
    public class Main : NancyModule
    {

        public class ViewData
        {
            public string MachineName = System.Environment.MachineName;
            public string UserName = System.Environment.UserName;
            public string UserDomainName = System.Environment.UserDomainName;
            public string Application = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            public Version Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

            public DateTime DateTime = mbDaemon.Main.DateTime;

            public List<Multicast> Transports;
            public List<mbDaemon.Main.Client> Clients;
        }

        public Main()
        {
            Get("/", args =>
            {
                return View["Main", GetViewData()];
            });
            Get("/Transports", args =>
            {
                return View["Transports", GetViewData()];
            });
            Get("/Clients", args =>
            {
                return View["Clients", GetViewData()];
            });
            Get("/ClientDetail/{Id}", args =>
            {
                return View["ClientDetail", GetClientData(args.Id)];
            });
            Get("/TransportDetail/{Port}", args =>
            {
                return View["TransportDetail", GetTransportData((args.Port))];
            });
        }

        private Multicast GetTransportData(int port)
        {
            return mbDaemon.Main.Transports[port].Multicast;
        }

        private mbDaemon.Main.Client GetClientData(string id)
        {
            return mbDaemon.Main.Clients.Values.Where(u => u.ApplicationInfo.ClientId == id).FirstOrDefault();
        }

        private ViewData GetViewData()
        {
            return new ViewData { Transports=mbDaemon.Main.Transports.Values.Select(u=>u.Multicast).ToList(), Clients=mbDaemon.Main.Clients.Values.ToList()};
        }

        //private HandleClientRequest GetClientData(string Id)
        //{
        //    return HandleClientRequest.list.Where(u => u.ApplicationInfo.ClientId== Id).FirstOrDefault();
        //}
        //private MMQ.wpf.Transport GetTransportData(int Port)
        //{
        //    MMQ.wpf.Transport t;
        //    if (HandleClientRequest.transports.TryGetValue(Port, out t )) return t;
        //    return null;
        //}

        //ViewData GetViewData()
        //{
        //    var ret = new Module.Main.ViewData {
        //        Transports = HandleClientRequest.transports.Values.ToList(),
        //        Clients = HandleClientRequest.list.ToList()
        //};
        //        return ret;
        //}
    }
}
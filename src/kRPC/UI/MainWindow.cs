﻿using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using KSP;
using KRPC.Server;
using KRPC.Server.RPC;
using KRPC.Server.Net;

namespace KRPC.UI
{
    sealed class MainWindow : Window
    {
        private GUIStyle labelStyle, activityStyle;
        public RPCServer Server { private get; set; }
        private Dictionary<IClient, long> lastClientActivity = new Dictionary<IClient, long> ();

        public event EventHandler OnStartServerPressed;
        public event EventHandler OnStopServerPressed;

        protected override void Init() {
            Style.fixedWidth = 250f;

            labelStyle = new GUIStyle(UnityEngine.GUI.skin.label);
            labelStyle.stretchWidth = true;

            activityStyle = new GUIStyle (HighLogic.Skin.toggle);
            activityStyle.active = HighLogic.Skin.toggle.normal;
            activityStyle.focused = HighLogic.Skin.toggle.normal;
            activityStyle.hover = HighLogic.Skin.toggle.normal;
            activityStyle.border = new RectOffset (0, 0, 0, 0);
            activityStyle.padding = new RectOffset (6, 0, 6, 0);
            activityStyle.overflow = new RectOffset (0, 0, 0, 0);
            activityStyle.imagePosition = ImagePosition.ImageOnly;
        }

        protected override void Draw ()
        {
            GUILayout.BeginVertical();
            GUILayout.Label ("Server status: " + (Server.Running ? "Online" : "Offline"), labelStyle);
            if (Server.Running) {
                if (GUILayout.Button ("Stop server"))
                    OnStopServerPressed (this, EventArgs.Empty);
                TCPServer tcpServer = (TCPServer)Server.Server;

                GUILayout.BeginHorizontal();
                GUILayout.Label ("Server address:", labelStyle);
                GUILayout.Label (tcpServer.Address.ToString (), labelStyle);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label ("Server port:", labelStyle);
                GUILayout.Label (tcpServer.Port.ToString (), labelStyle);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label ("Allowed client(s):", labelStyle);
                GUILayout.Label (AllowedClientsString(tcpServer.Address), labelStyle);
                GUILayout.EndHorizontal();

                if (Server.Clients.Count () == 0) {
                    GUILayout.Label ("No clients connected", labelStyle);
                } else {
                    GUILayout.Label ("Clients connected:", labelStyle);
                    foreach (var client in Server.Clients) {
                        string name = (client.Name == "") ? "<unknown>" : client.Name;
                        GUILayout.BeginHorizontal ();
                        GUILayout.Toggle (IsClientActive(client), "", activityStyle);
                        GUILayout.Label (name + " @ " + client.Address);
                        GUILayout.EndHorizontal ();
                    }
                }
            } else {
                if (GUILayout.Button ("Start server"))
                    OnStartServerPressed (this, EventArgs.Empty);
            }
            GUILayout.EndVertical ();
            GUI.DragWindow ();
        }

        public void SawClientActivity (IClient client)
        {
            lastClientActivity [client] = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }

        private bool IsClientActive (IClient client) {
            if (!lastClientActivity.ContainsKey (client))
                return false;
            long now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            long lastActivity = lastClientActivity [client];
            return now - 100L < lastActivity;
        }

        private string AllowedClientsString(IPAddress localAddress) {
            if (localAddress.ToString() == "127.0.0.1")
                return "Local only";
            var subnet = GetSubnetMask (localAddress);
            if (subnet != null)
                return "Subnet mask " + subnet;
            return "?";
        }

        private static IPAddress GetSubnetMask(IPAddress address)
        {
            //TODO: fails due to native code not being available
//            foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces()) {
//                foreach (UnicastIPAddressInformation unicastIPAddressInformation in adapter.GetIPProperties().UnicastAddresses)    {
//                    if (unicastIPAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork) {
//                        if (address.Equals(unicastIPAddressInformation.Address)) {
//                            return unicastIPAddressInformation.IPv4Mask;
//                        }
//                    }
//                }
//            }
            return null;
        }
    }
}

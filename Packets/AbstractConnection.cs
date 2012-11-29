﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WvsBeta.Common.Sessions
{
    public abstract class AbstractConnection : Session
    {
        public AbstractConnection(System.Net.Sockets.Socket pSocket)
            : base(pSocket, "")
        {

        }

        public AbstractConnection(string pIP, ushort pPort)
            : base(pIP, pPort, "")
        {
        }

        public override void OnPacketInbound(Packet pPacket)
        {
            if (pPacket.Length == 0)
                return;
            pPacket.Reset(0);

            AC_OnPacketInbound(pPacket);
        }

        public abstract void AC_OnPacketInbound(Packet pPacket);
    }
}
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WvsBeta.Common.Sessions;

namespace MWLR_Logging.MapleStories
{

    class MapleGlobal : AbstractMaple
    {
        private short version, subversion;
        private Session _session;
        public MapleGlobal(Session pSession, short _v, short _sv)
            : base("GMS", 8)
        {
            _session = pSession;
            this.RequiresVersion = -1;
            version = _v;
            subversion = _sv;
            Program.mTotalOnline = 0;
            Program.Connection.AmountOnline = 0;
        }

        public override void handlePackets(Packet packet)
        {
            //Logger.WriteLine("Got packet: {0}", packet.ToString());
            switch (packet.ReadShort())
            {
                case 0x09: handleWorld(packet); break; // 0x0A
                case 0x11: sendPong(); break;
                //default: Logger.WriteLine("Unknown packet: {0}", packet.ToString()); break;
            }
        }

        bool gotData = Program.CRASHMODE;//false;
        int iworlds = 0;
        int ichannels = 0;
        public void handleWorld(Packet packet)
        {
            byte ID = packet.ReadByte();
            if (ID == 0xFF)
            {
                HandleData(null);
                // Logger.WriteLine("Worlds: {0}; Channels: {1}; Channel load avg: {2}", iworlds, ichannels, (double)Program.mTotalOnline / (double)ichannels);
                gotData = true;
                iworlds = ichannels = 0;
                return;
            }
            iworlds++;

            string name = packet.ReadString();
            byte ribbon = packet.ReadByte(); // Ribbon
            string eventMsg = packet.ReadString(); // Event message
            packet.ReadShort(); // EXP rate
            packet.ReadShort(); // DROP rate
            packet.ReadByte(); // Unknown
            int channels = packet.ReadByte();
            ichannels += channels;
            if (!gotData)
                DataBase.UpdateWorldData(ID, name, channels, ribbon, eventMsg);

            if (Config.Instance.SkipWorlds.Contains(ID)) return;

            List<Channel> channelList = new List<Channel>();
            Channel chan;
            for (int i = 0; i < channels; i++)
            {
                chan = new Channel();
                chan.ChannelName = packet.ReadString();
                chan.Population = packet.ReadInt();
                chan.World = packet.ReadByte();
                chan.ID = packet.ReadByte();
                chan.ID += 1;
                Program.Connection.AmountOnline += chan.Population;
                chan.SpecialValue = packet.ReadByte();
                channelList.Add(chan);

                if (OldLoads.ContainsKey(chan.ChannelName) && OldLoads[chan.ChannelName] >= 10 && (chan.Population * 100 / OldLoads[chan.ChannelName]) <= 20)
                {
                    Logger.WriteLine("{0} crashed, as it went from {1:N0} to {2:N0} people online.", chan.ChannelName, OldLoads[chan.ChannelName], chan.Population);
                    DataBase.AddCrash(chan.World, chan.ID, OldLoads[chan.ChannelName], chan.Population);
                    TweetCrash(chan);
                }

                if (chan.Population < 0)
                {
                    Logger.WriteLine("Seems like Nexon didn't fix the negative bug: Channel {0} has {1} players online.", chan.ChannelName, chan.Population);
                    chan.Population = Math.Abs(chan.Population); // Make it positive!
                }

                if (OldLoads.ContainsKey(chan.ChannelName))
                {
                    OldLoads[chan.ChannelName] = chan.Population;
                }
                else
                {
                    OldLoads.Add(chan.ChannelName, chan.Population);
                }
            }

            HandleData(channelList);
        }

        public override void sendClientReady()
        {
            Packet packet = new Packet();
            packet.WriteShort(0x14);
            packet.WriteByte(_locale);
            packet.WriteShort(version);
            packet.WriteShort(subversion);
            _session.SendPacket(packet);
        }

        public override void sendWorldListReRequest()
        {
            Packet packet = new Packet();
            // packet.WriteShort(0x19);
            packet.WriteShort(0x1A);
            _session.SendPacket(packet);
        }


        public override void sendPong()
        {
            Packet packet = new Packet();
            if (HandlingVersion >= 153)
                packet.WriteShort(0x46);
            else if (HandlingVersion >= 145)
                packet.WriteShort(0x45);
            else if (HandlingVersion >= 143)
                packet.WriteShort(0x46);
            else if (HandlingVersion >= 118)
                packet.WriteShort(0x2D);
            else if (HandlingVersion >= 115)
                packet.WriteShort(0x2C);
            else if (HandlingVersion >= 101)
                packet.WriteShort(0x2E);
            else if (HandlingVersion >= 99)
                packet.WriteShort(0x1B);
            else if (HandlingVersion >= 86)
                packet.WriteShort(0x1A);
            else if (HandlingVersion >= 83)
                packet.WriteShort(0x19);
            else
                packet.WriteShort(0x18);


            if (HandlingVersion >= 143)
                packet.WriteInt(0);

            _session.SendPacket(packet);
        }
    }
}

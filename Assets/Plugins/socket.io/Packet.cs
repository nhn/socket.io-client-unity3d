using UnityEngine;
using System.Collections;
using System;

namespace socket.io {

    /// <summary>
    /// Engine.io protocol packet types
    /// </summary>
    public enum EnginePacketTypes {
        UNKNOWN = -1,
        OPEN = 0,
        CLOSE = 1,
        PING = 2,
        PONG = 3,
        MESSAGE = 4,
        UPGRADE = 5,
        NOOP = 6
    }

    /// <summary>
    /// Socket.io protocol packet types
    /// </summary>
    public enum SocketPacketTypes {
        UNKNOWN = -1,
        CONNECT = 0,
        DISCONNECT = 1,
        EVENT = 2,
        ACK = 3,
        ERROR = 4,
        BINARY_EVENT = 5,
        BINARY_ACK = 6,
        CONTROL = 7
    }

    /// <summary>
    /// Socket.io pakcet object
    /// |메시지 길이| : |engine.io 메시지 타입| |socket.io 메시지 타입| |네임스페이스| , |패킷Id| |[(Json)String])|
    /// </summary>
    public class Packet {

        #region Pre-defined packet values
        public static string Ping {
            get { return "2"; }
        }

        public static string Pong {
            get { return "3"; }
        }

        public static string Probe {
            get { return "2probe"; }
        }

        public static string ProbeAnswer {
            get { return "3probe"; }
        }
        #endregion
        
        #region Helper properties
        public bool IsMessage {
            get { return enginePktType == EnginePacketTypes.MESSAGE; }
        }

        public bool IsBinary {
            get { return socketPktType == SocketPacketTypes.BINARY_ACK || socketPktType == SocketPacketTypes.BINARY_EVENT; }
        }

        public bool HasNamespace {
            get { return !(string.IsNullOrEmpty(nsp) || nsp == "/"); }
        }

        public bool HasId {
            get { return id > -1; }
        }

        public bool HasBody {
            get { return !string.IsNullOrEmpty(body); }
        }
        #endregion

        #region Packet Data
        public EnginePacketTypes enginePktType;
        public SocketPacketTypes socketPktType;
        public int id;
        public string nsp;
        public string body;
        #endregion

        public override string ToString() {
            return string.Format("[socket.io Packet => ({0} | {1} | {2}:id | {3}:nsp | {4}:json)]", enginePktType, socketPktType, id, nsp, body);
        }

        public Packet(EnginePacketTypes enginePktType, SocketPacketTypes socketPktType, int id, string nsp, string json) {
            this.enginePktType = enginePktType;
            this.socketPktType = socketPktType;
            this.id = id;
            this.nsp = nsp;
            this.body = json;
        }

        public Packet(EnginePacketTypes enginePktType, SocketPacketTypes socketPktType, string nsp, string json) : this(enginePktType, socketPktType, -1, nsp, json) { }
        public Packet(EnginePacketTypes enginePktType, SocketPacketTypes socketPktType, string json) : this(enginePktType, socketPktType, -1, "/", json) { }
        public Packet(EnginePacketTypes enginePktType, SocketPacketTypes socketPktType) : this(enginePktType, socketPktType, -1, "/", null) { }
        public Packet(EnginePacketTypes enginePktType) : this(enginePktType, SocketPacketTypes.UNKNOWN, -1, "/", null) { }
        public Packet() : this(EnginePacketTypes.UNKNOWN, SocketPacketTypes.UNKNOWN, -1, "/", null) { }

    }

}
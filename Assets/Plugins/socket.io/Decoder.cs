using System.Text;
using System;


namespace socket.io {

    /// <summary>
    /// The converter from a string encoded value to Packet object.
    /// </summary>
    public static class Decoder {

        public static Packet Decode(this string data) {
            try {
                var pkt = new Packet();

                pkt.enginePktType = (EnginePacketTypes)(data[0] - '0');
                if (data.Length == 1)
                    return pkt;

                if (pkt.enginePktType != EnginePacketTypes.MESSAGE) {
                    pkt.body = data.Substring(1);
                    return pkt;
                }

                pkt.socketPktType = (SocketPacketTypes)(data[1] - '0');
                if (data.Length == 2)
                    return pkt;

                var readPos = 2;

                if (data[readPos] == '/')
                    pkt.nsp = ReadChunk(ref data, ref readPos);

                if (readPos == data.Length)
                    return pkt;

                if (data[readPos] == ',')
                    ++readPos;

                if (readPos == data.Length)
                    return pkt;

                if (data[readPos] != '[')
                    int.TryParse(ReadChunk(ref data, ref readPos), out pkt.id);

                pkt.body = data.Substring(readPos);
                return pkt;
            }
            catch (Exception e) {
                throw e;
            }
        }

        static string ReadChunk(ref string data, ref int readPos) {
            var i = readPos;
            for (; i < data.Length; ++i) {
                if (data[i] == ',' || data[i] == '[')
                    break;
            }

            var startIndex = readPos;
            var len = i - readPos;

            readPos = i;
            return data.Substring(startIndex, len);
        }

    }

}
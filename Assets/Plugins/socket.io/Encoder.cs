using System.Text;
using System;

namespace socket.io {

    /// <summary>
    /// The converter from Packet object to a string encoded value.
    /// </summary>
    public static class Encoder {

        public static string Encode(this Packet pkt) {
            try {
                var builder = new StringBuilder();

                builder.Append((int)pkt.enginePktType);
                if (!pkt.IsMessage)
                    return builder.ToString();

                builder.Append((int)pkt.socketPktType);

                if (pkt.HasNamespace) {
                    builder.Append(pkt.nsp);
                    if (pkt.HasId || pkt.HasBody)
                        builder.Append(',');
                }

                if (pkt.HasId)
                    builder.Append(pkt.id);

                if (pkt.HasBody)
                    builder.Append(pkt.body);

                return builder.ToString();
            }
            catch (Exception e) {
                throw e;
            }
        }

    }
}

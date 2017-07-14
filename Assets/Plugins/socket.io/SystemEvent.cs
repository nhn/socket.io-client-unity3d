using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace socket.io {

    public enum SystemEvents {
        connect,
        connectTimeOut,
        reconnectAttempt,
        reconnectFailed,
        disconnect,
        reconnect,
        reconnecting,
        connectError,
        reconnectError,
        Max
    }

    public static class SystemEventHelper {

        public static string ToString(SystemEvents @event) {
            var ret = @event.ToString();
            if (ret == "Max")
                return string.Empty;

            return ret;
        }

        public static SystemEvents FromString(string @event) {
            try {
                return (SystemEvents)Enum.Parse(typeof(SystemEvents), @event);
            }
            catch (Exception) {
                return SystemEvents.Max;
            }
        }

        public static bool IsSystemEvent(string @event) {
            return FromString(@event) != SystemEvents.Max;
        }

    }

}

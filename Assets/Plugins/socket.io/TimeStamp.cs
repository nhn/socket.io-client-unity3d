using System;


namespace socket.io {

    public static class TimeStamp {

        /// <summary>
        /// return a string encoded value of current time.
        /// </summary>
        public static string Now {
            get {
                return EncodeTimeStamp(DateTime.Now.Ticks / 1000);
            }
        }

        public static string EncodeTimeStamp(long num) {
            if (_encodeAlphabets == null)
                _encodeAlphabets = "0,1,2,3,4,5,6,7,8,9,A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P,Q,R,S,T,U,V,W,X,Y,Z,a,b,c,d,e,f,g,h,i,j,k,l,m,n,o,p,q,r,s,t,u,v,w,x,y,z,-,_".Split(',');

            var encoded = string.Empty;
            do {
                encoded = _encodeAlphabets[num % _encodeAlphabets.Length] + encoded;
                num = num / _encodeAlphabets.Length;
            } while (num > 0);

            return encoded;
        }

        static string[] _encodeAlphabets;

    }

}
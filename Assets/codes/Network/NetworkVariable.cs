using System;
using System.Collections.Generic;
using System.Text;

namespace Assets.codes.Network
{
    internal class NetworkVariable<T>
    {
        private T data;
        public NetworkVariable(T initialValue)
        {
            data = initialValue;
        }
        public void Set(T value)
        {
            data = value;
            // Here you would add code to synchronize this change over the network
        }
        public T GetValue()
        {
            return data;
        }
    }
}

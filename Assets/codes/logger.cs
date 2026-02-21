#define ENABLE_PACKET_SEND_LOG
#define ENABLE_PACKET_RECEIVE_LOG

using UnityEngine;
using System.Collections;
using System.Diagnostics;

namespace Assets.codes
{
	public static class logger
	{
		[Conditional("ENABLE_PACKET_SEND_LOG")]
		public static void LogPacketSend(string message)
		{
			UnityEngine.Debug.Log($"[Packet Send] {message}");
        }
		[Conditional("ENABLE_PACKET_RECEIVE_LOG")]
		public static void LogPacketReceive(string message)
		{
			UnityEngine.Debug.Log($"[Packet Receive] {message}");
		}
    }
}
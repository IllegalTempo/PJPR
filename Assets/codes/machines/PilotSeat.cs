using Assets.codes.Network.Messages;
using System.Collections;
using UnityEngine;

namespace Assets.codes.machines
{
    public class PilotSeat : Seat
    {
        public void ControlMainSpaceship(Vector2 input)
        {
            (new NMS_Both_ControlSpaceshipWASD(input)).SendMessageAsServerOrClient();
        }
    }
}
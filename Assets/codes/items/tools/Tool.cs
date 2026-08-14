using System;
using System.Collections.Generic;
using System.Text;

    /// <summary>
    /// Tools are item that can interact with a Selectable
    /// </summary>
    public abstract class Tool : Item
    {
        /// <summary>
        /// When holding this tool, the player can interact with a Selectable
        /// </summary>
        /// <param name="target"></param>
        public abstract void OnUsingInteract(Selectable target);
    }


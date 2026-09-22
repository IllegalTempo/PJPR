using System;
using System.Text;

public static class ModuleControllerSlotLink
{
    private const string Prefix = "ModuleController|";
    private const string LegacyPrefix = "ModuleSlot_";

    public static string CreateNetworkId(ModuleSlot slot)
    {
        string slotId = slot != null && slot.Identity != null ? slot.Identity.Identifier : string.Empty;
        string encodedSlotId = Convert.ToBase64String(Encoding.UTF8.GetBytes(slotId));
        return $"{Prefix}{encodedSlotId}|{Guid.NewGuid():N}";
    }

    public static bool TryResolve(string networkID, out ModuleSlot slot)
    {
        slot = null;
        if (string.IsNullOrEmpty(networkID))
        {
            return false;
        }

        if (TryResolveBySlotIdentity(networkID, out slot))
        {
            return true;
        }

        return TryResolveLegacyIndex(networkID, out slot);
    }

    private static bool TryResolveBySlotIdentity(string networkID, out ModuleSlot slot)
    {
        slot = null;
        if (!networkID.StartsWith(Prefix))
        {
            return false;
        }

        int startIndex = Prefix.Length;
        int endIndex = networkID.IndexOf('|', startIndex);
        if (endIndex < 0)
        {
            return false;
        }

        string encodedSlotId = networkID.Substring(startIndex, endIndex - startIndex);
        string slotId;
        try
        {
            slotId = Encoding.UTF8.GetString(Convert.FromBase64String(encodedSlotId));
        }
        catch (FormatException)
        {
            return false;
        }

        slot = NetworkSystem.Instance != null
            ? NetworkSystem.Instance.GetComponentOfIdentity<ModuleSlot>(slotId)
            : null;
        return slot != null;
    }

    private static bool TryResolveLegacyIndex(string networkID, out ModuleSlot slot)
    {
        slot = null;
        if (!networkID.StartsWith(LegacyPrefix) || MainSpaceship.Instance == null)
        {
            return false;
        }

        int startIndex = LegacyPrefix.Length;
        int endIndex = networkID.IndexOf('_', startIndex);
        string slotIndexText = endIndex >= 0
            ? networkID.Substring(startIndex, endIndex - startIndex)
            : networkID.Substring(startIndex);

        if (!int.TryParse(slotIndexText, out int slotIndex))
        {
            return false;
        }

        slot = MainSpaceship.Instance.GetModuleSlot(slotIndex);
        return slot != null;
    }
}

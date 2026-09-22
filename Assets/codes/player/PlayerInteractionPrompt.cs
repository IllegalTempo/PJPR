public readonly struct InteractionPrompt
{
    public readonly string Name;
    public readonly string Key;

    public InteractionPrompt(string name, string key)
    {
        Name = name;
        Key = key;
    }

    public bool IsVisible => !string.IsNullOrEmpty(Name);
}

public readonly struct InteractionPromptSet
{
    public readonly InteractionPrompt Primary;
    public readonly InteractionPrompt Secondary;

    public InteractionPromptSet(InteractionPrompt primary, InteractionPrompt secondary)
    {
        Primary = primary;
        Secondary = secondary;
    }
}

public static class PlayerInteractionPromptBuilder
{
    public static InteractionPromptSet Build(
        SelectionContext context,
        Item holdingItem,
        string pickupKey,
        string interactKey,
        string rotateKey)
    {
        InteractionPrompt secondary = context.Usable != null
            ? new InteractionPrompt("Use", interactKey)
            : default;

        if (holdingItem != null && context.Slot != null)
        {
            InteractionPromptSet slotPrompts = BuildSlotPrompt(context.Slot, holdingItem, pickupKey, rotateKey);
            return new InteractionPromptSet(
                slotPrompts.Primary,
                slotPrompts.Secondary.IsVisible ? slotPrompts.Secondary : secondary);
        }

        InteractionPrompt primary = BuildPrimary(context, holdingItem, pickupKey);
        return new InteractionPromptSet(primary, secondary);
    }

    private static InteractionPrompt BuildPrimary(
        SelectionContext context,
        Item holdingItem,
        string pickupKey)
    {
        if (holdingItem == null)
        {
            return context.Item != null ? new InteractionPrompt("Pick Up", pickupKey) : default;
        }

        if (context.Item != null &&
            holdingItem.HasItemType(ItemType.Processable) &&
            context.Item.HasItemType(ItemType.Processable))
        {
            return new InteractionPrompt("Combine", pickupKey);
        }

        return new InteractionPrompt("Drop", pickupKey);
    }

    private static InteractionPromptSet BuildSlotPrompt(Slot slot, Item holdingItem, string pickupKey, string rotateKey)
    {
        if (!holdingItem.FitIn(slot))
        {
            return new InteractionPromptSet(new InteractionPrompt("Not Available", ""), default);
        }

        if (slot is Port)
        {
            return new InteractionPromptSet(new InteractionPrompt("Put", pickupKey), default);
        }

        return new InteractionPromptSet(
            new InteractionPrompt("Install", pickupKey),
            new InteractionPrompt("Rotate", rotateKey));
    }
}

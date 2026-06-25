namespace UI.NyangQuarium
{
    public enum NyangquariumHubEntryMode
    {
        Story,
        Board,
        Collection,
        Layout,
        WaterGaze
    }

    public static class NyangquariumHubEntryContext
    {
        public static NyangquariumHubEntryMode Current { get; private set; } = NyangquariumHubEntryMode.Layout;

        public static void Set(NyangquariumHubEntryMode entryMode)
        {
            Current = entryMode;
        }
    }

    public interface INyangquariumHubEntryReceiver
    {
        void SetNyangquariumEntryMode(NyangquariumHubEntryMode entryMode);
    }
}

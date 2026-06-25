namespace UI.NyangQuarium
{
    public enum NyangquariumEntryMode
    {
        Story,
        Board,
        Collection,
        Layout,
        WaterGaze
    }

    public static class NyangquariumEntryContext
    {
        public static NyangquariumEntryMode Current { get; private set; } = NyangquariumEntryMode.Layout;

        public static void Set(NyangquariumEntryMode entryMode)
        {
            Current = entryMode;
        }
    }

    public interface INyangquariumEntryReceiver
    {
        void SetNyangquariumEntryMode(NyangquariumEntryMode entryMode);
    }
}

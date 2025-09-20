namespace Action.Editor.Channel
{
    public interface IActionChannelGroup: IActionChannel
    {
        void AddChildChannel(IActionChannel channel);
        void UpdateChildChannel(IActionChannel channel, ActionChannelEditorData data);
        void RemoveChildChannel(IActionChannel channel);
    }
}
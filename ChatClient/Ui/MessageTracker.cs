using ChatClient.Dtos;

namespace ChatClient.Ui;

public sealed class MessageTracker
{
    private readonly object _sync = new();
    private readonly HashSet<int> _seenIds = [];
    private int _lastId;

    public int LastId
    {
        get { lock (_sync) return _lastId; }
    }

    public void Seed(IEnumerable<MessageResponse> messages)
    {
        foreach (var message in messages)
            Accept(message);
    }

    public bool Accept(MessageResponse message)
    {
        lock (_sync)
        {
            if (!_seenIds.Add(message.Id))
                return false;
            _lastId = Math.Max(_lastId, message.Id);
            return true;
        }
    }
}

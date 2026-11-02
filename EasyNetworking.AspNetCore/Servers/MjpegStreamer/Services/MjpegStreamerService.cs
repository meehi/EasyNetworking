using EasyNetworking.AspNetCore.Servers.MjpegStreamer.Models;
using System.Collections.Concurrent;

namespace EasyNetworking.AspNetCore.Servers.MjpegStreamer.Services
{
    internal sealed class MjpegStreamerService
    {
        private readonly ConcurrentDictionary<string, MjpegGroup> _groups = new();
        private readonly ConcurrentDictionary<string, string> _streamers = new();

        public bool RegisterStreamer(string groupName, string connectionId)
        {
            return _streamers.TryAdd(groupName, connectionId);
        }

        public void RemoveStreamer(string groupName, string connectionId)
        {
            if (!_streamers.TryGetValue(groupName, out string? current) || current != connectionId)
                return;

            if (!_streamers.TryRemove(new KeyValuePair<string, string>(groupName, connectionId)))
                return;

            if (_groups.TryRemove(groupName, out MjpegGroup? group))
                foreach (Client client in group.Clients.Values)
                    client.FrameChannel.Writer.TryComplete();
        }

        public bool RegisterClient(string groupName, Client client)
        {
            if (!_streamers.ContainsKey(groupName))
                return false;

            MjpegGroup group = _groups.GetOrAdd(groupName, _ => new MjpegGroup());
            return group.Clients.TryAdd(client.Id, client);
        }

        public void RemoveClient(string groupName, Client client)
        {
            if (!_groups.TryGetValue(groupName, out MjpegGroup? group))
                return;

            if (group.Clients.TryGetValue(client.Id, out Client? current) &&
                ReferenceEquals(current, client))
            {
                group.Clients.TryRemove(client.Id, out _);
                client.FrameChannel.Writer.TryComplete();
            }

            if (group.Clients.IsEmpty)
            {
                ((ICollection<KeyValuePair<string, MjpegGroup>>)_groups)
                    .Remove(new(groupName, group));
            }
        }

        public void BroadcastFrame(string groupName, byte[] frame)
        {
            if (!_groups.TryGetValue(groupName, out MjpegGroup? group))
                return;

            foreach (Client client in group.Clients.Values)
                client.FrameChannel.Writer.TryWrite(frame);
        }
    }
}
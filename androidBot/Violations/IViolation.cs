using Discord.WebSocket;
using System.Threading.Tasks;

namespace AndroidBot.Listeners
{
    public interface IViolation
    {
        bool Violates(SocketMessage message, Android android);
        Task Consequence(SocketMessage message, Android android);
    }
}

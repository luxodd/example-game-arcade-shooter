using System;

namespace Luxodd.Game.Scripts.Network
{
    public class CommandRequestHandler
    {
        public CommandRequestType CommandRequestType { get; set; }
        public int Id { get; set; }
        public CommandResponse CommandResponse { get; set; }
        public string RawResponse { get; set; }
        public Action<CommandRequestHandler> OnCommandResponseSuccessHandler { get; set; }

        internal int TransportGeneration { get; set; }
        internal float ResponseDeadline { get; set; }
        internal bool IsCompleted { get; set; }
        internal string ErrorMessage { get; set; }
    }
}

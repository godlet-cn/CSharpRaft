using System;

namespace CSharpRaft
{
    /// <summary>
    /// IStateMachine is the interface for allowing the host application to save and recovery 
    /// the state machine. This makes it possible to make snapshots and compact the log.
    /// </summary>
    public interface IStateMachine
    {
        byte[] Save();

        void Recovery(byte[] bytes);
    }
}
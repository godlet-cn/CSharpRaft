using System;
using System.Collections.Generic;
using System.IO;

namespace CSharpRaft
{
    public class Log
    {
        public Func<LogEntry, Command, object> ApplyFunc;

        private FileStream file;

        private string path;

        internal List<LogEntry> entries;

        private int commitIndex;

        // The index before the first entry in the Log entries
        internal int startIndex;

        internal int startTerm;

        internal bool initialized = false;

        private readonly object mutex = new object();

        public Log()
        {
            entries = new List<LogEntry>();
        }

        #region Properties

        //--------------------------------------
        // Log Indices
        //--------------------------------------

        // The last committed index in the log.
        public int CommitIndex
        {
            get {
                lock (mutex)
                {
                    return this.commitIndex;
                }
            }
        }

        // The current index in the log.
        internal int currentIndex
        {
            get {
                lock (mutex)
                {
                    return this.internalCurrentIndex;
                }
            }
        }

        // The current index in the log without locking
        internal int internalCurrentIndex
        {
            get {
                if (this.entries.Count == 0)
                {
                    return this.startIndex;
                }
                return this.entries[this.entries.Count - 1].Index;
            }
        }

        // The next index in the log.
        internal int nextIndex
        {
            get {
                return this.currentIndex + 1;
            }
        }

        // Determines if the log contains zero entries.
        internal bool isEmpty
        {
            get
            {
                lock (mutex)
                {
                    return (this.entries.Count == 0) && (this.startIndex == 0);
                }
            }
        }

        // The name of the last command in the log.
        internal string lastCommandName
        {
            get
            {
                lock (mutex)
                {
                    if (this.entries.Count > 0)
                    {
                        var entry = this.entries[this.entries.Count - 1];
                        if (entry != null)
                        {
                            return entry.CommandName;
                        }
                    }
                }
                return "";
            }
        }

        //--------------------------------------
        // Log Terms
        //--------------------------------------
        // The current term in the log.
        internal int currentTerm
        {
            get {
                lock (mutex)
                {

                    if (this.entries.Count == 0)
                    {
                        return this.startTerm;
                    }
                    return this.entries[this.entries.Count - 1].Term;
                }
            }
        }

        #endregion

        #region Methods
        
        //--------------------------------------
        // State
        //--------------------------------------

        // Opens the log file and reads existing entries. The log can remain open and
        // continue to append entries to the end of the log.
        internal void open(string path)
        {
            // Read all the entries from the log if one exists.
            this.file = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            this.path = path;
            this.initialized = true;

            DebugTrace.DebugLine("log.open.create ", path);

            // Read the file and decode entries.
            while (file.Position < file.Length)
            {
                // Instantiate log entry and decode into it.
                LogEntry entry = new LogEntry(this, 0, 0, null);

                entry.Position = file.Seek(0, SeekOrigin.Current);

                entry.Decode(this.file);
                if (entry.Index > this.startIndex)
                {
                    // Append entry.
                    this.entries.Add(entry);

                    if (entry.Index <= this.commitIndex)
                    {
                        Command command = Commands.NewCommand(entry.CommandName, entry.Command);
                        this.ApplyFunc(entry, command);
                    }
                    DebugTrace.DebugLine("open.log.append log index ", entry.Index);
                }
            }

            DebugTrace.DebugLine("open.log.recovery number of log ", this.entries.Count);

            this.initialized = true;
        }

        // Closes the log file.
        internal void close()
        {
            lock (mutex)
            {
                if (this.file != null)
                {
                    this.file.Close();

                    this.file = null;
                }
                this.entries = new List<LogEntry>();
            }
        }

        // sync to disk
        internal void sync()
        {
            this.file.Flush();
        }

        //--------------------------------------
        // Entries
        //--------------------------------------

        // Creates a log entry associated with this log.
        internal LogEntry createEntry(int term, Command command)
        {
            return new LogEntry(this,this.nextIndex, term, command);
        }

        // Retrieves an entry from the log. If the entry has been eliminated because
        // of a snapshot then null is returned.
        internal LogEntry getEntry(int index)
        {
            lock (mutex)
            {
                if (index <= this.startIndex || index > (this.startIndex + this.entries.Count))
                {
                    return null;
                }
                return this.entries[index - this.startIndex - 1];
            }
        }

        // Checks if the log contains a given index/term combination.
        internal bool containsEntry(int index, int term)
        {
            LogEntry entry = this.getEntry(index);
            return (entry != null && entry.Term == term);
        }

        // Retrieves a list of entries after a given index as well as the term of the
        // index provided. A null list of entries is returned if the index no longer
        // exists because a snapshot was made.
        internal void getEntriesAfter(int index, int maxLogEntriesPerRequest, out List<LogEntry> entries, out int term)
        {
            lock (mutex)
            {
                entries = null;
                term = 0;

                // Return null if index is before the start of the log.
                if (index < this.startIndex)
                {
                    DebugTrace.TraceLine("log.entriesAfter.before: ", index, " ", this.startIndex);
                    return;
                }

                // Return an error if the index doesn't exist.
                if (index > (this.entries.Count) + this.startIndex)
                {
                    throw new Exception(string.Format("raft: Index is beyond end of log: {0} {1}", this.entries.Count, index));
                }

                // If we're going from the beginning of the log then return the whole log.
                if (index == this.startIndex)
                {
                    DebugTrace.TraceLine("log.entriesAfter.beginning: ", index, " ", this.startIndex);

                    entries = this.entries;
                    term = this.startTerm;
                }

                DebugTrace.TraceLine("log.entriesAfter.partial: ", index, " ", this.entries[this.entries.Count - 1].Index);

                entries = new List<LogEntry>();
                for (int i = index - this.startIndex; i < this.entries.Count; i++)
                {
                    entries.Add(this.entries[i]);
                }
                int length = entries.Count;

                DebugTrace.TraceLine("log.entriesAfter: startIndex:", this.startIndex, " length", this.entries.Count);

                if (length < maxLogEntriesPerRequest)
                {
                    // Determine the term at the given entry and return a subslice.
                    term = this.entries[index - 1 - this.startIndex].Term;
                }
                else
                {
                    List<LogEntry> newEntries = new List<LogEntry>();
                    for (int i = 0; i < maxLogEntriesPerRequest; i++)
                    {
                        newEntries.Add(entries[i]);
                    }
                    entries = newEntries;
                    term = this.entries[index - 1 - this.startIndex].Term;
                }
            }
        }

        //--------------------------------------
        // Commit
        //--------------------------------------

        // Retrieves the last index and term that has been committed to the log.
        internal void getCommitInfo(out int index, out int term)
        {
            lock (mutex)
            {
                // If we don't have any committed entries then just return zeros.
                if (this.commitIndex == 0)
                {
                    index = 0;

                    term = 0;
                }

                // No new commit log after snapshot
                if (this.commitIndex == this.startIndex)
                {
                    index = this.startIndex;
                    term = this.startTerm;
                }

                // Return the last index & term from the last committed entry.
                DebugTrace.DebugLine("commitInfo.get.[", this.commitIndex, "/", this.startIndex, "]");

                LogEntry entry = this.entries[this.commitIndex - 1 - this.startIndex];
                index = entry.Index;
                term = entry.Term;
            }
        }

        // Retrieves the last index and term that has been appended to the log.
        internal void getLastInfo(out int index, out int term)
        {
            lock (mutex)
            {
                // If we don't have any entries then just return zeros.
                if (this.entries.Count == 0)
                {
                    index = this.startIndex;
                    term = this.startTerm;
                    return;
                }

                // Return the last index & term
                LogEntry entry = this.entries[this.entries.Count - 1];
                index = entry.Index;
                term = entry.Term;
            }
        }

        // Updates the commit index
        internal void updateCommitIndex(int index)
        {
            lock (mutex)
            {
                if (index > this.commitIndex)
                {
                    this.commitIndex = index;
                }

                DebugTrace.DebugLine("update.commit.index ", index);
            }
        }

        // Updates the commit index and writes entries after that index to the stable storage.
        internal void setCommitIndex(int index)
        {
            lock (mutex)
            {
                // this is not error any more after limited the number of sending entries
                // commit up to what we already have
                if (index > this.startIndex + this.entries.Count)
                {
                    DebugTrace.DebugLine("raft.Log: Commit index", index, "set back to ", this.entries.Count);

                    index = this.startIndex + this.entries.Count;
                }

                // Do not allow previous indices to be committed again.

                // This could happens, since the guarantee is that the new leader has up-to-dated
                // log entries rather than has most up-to-dated committed index

                // For example, Leader 1 send log 80 to follower 2 and follower 3
                // follower 2 and follow 3 all got the new entries and reply
                // leader 1 committed entry 80 and send reply to follower 2 and follower3
                // follower 2 receive the new committed index and update committed index to 80
                // leader 1 fail to send the committed index to follower 3
                // follower 3 promote to leader (server 1 and server 2 will vote, since leader 3
                // has up-to-dated the entries)
                // when new leader 3 send heartbeat with committed index = 0 to follower 2,
                // follower 2 should reply success and let leader 3 update the committed index to 80

                if (index < this.commitIndex)
                {
                    return;
                }

                // Find all entries whose index is between the previous index and the current index.
                for (int i = this.commitIndex + 1; i <= index; i++)
                {

                    int entryIndex = i - 1 - this.startIndex;
                    LogEntry entry = this.entries[entryIndex];

                    // Update commit index.
                    this.commitIndex = entry.Index;

                    // Decode the command.
                    Command command = Commands.NewCommand(entry.CommandName, entry.Command);

                    // Apply the changes to the state machine and store the error code.
                    object returnValue = this.ApplyFunc(entry, command);

                    DebugTrace.Debug("setCommitIndex.set.result index: {0}, entries index: {1}", i, entryIndex);

                    // we can only commit up to the most recent join command
                    // if there is a join in this batch of commands.
                    // after this commit, we need to recalculate the majority.
                    if (command is DefaultJoinCommand)
                    {
                        return;
                    }
                }
            }
        }

        // Set the commitIndex at the head of the log file to the current
        // commit Index. This should be called after obtained a log lock
        internal void flushCommitIndex()
        {
            this.file.Seek(0, SeekOrigin.Begin);

            byte[] indexData = BitConverter.GetBytes(this.commitIndex);
            this.file.Write(indexData, 0, indexData.Length);

            this.file.Seek(0, SeekOrigin.End);
        }

        //--------------------------------------
        // Truncation
        //--------------------------------------

        // Truncates the log to the given index and term. This only works if the log
        // at the index has not been committed.
        internal void truncate(int index, int term)
        {
            lock (mutex)
            {
                DebugTrace.DebugLine("log.truncate: ", index);

                // Do not allow committed entries to be truncated.
                if (index < this.commitIndex)
                {
                    DebugTrace.DebugLine("log.truncate.before");
                    throw new Exception(string.Format("raft.Log: Index is already committed ({0}): (IDX={1}, TERM={2})", this.commitIndex, index, term));
                }

                // Do not truncate past end of entries.
                if (index > this.startIndex + this.entries.Count)
                {
                    DebugTrace.DebugLine("log.truncate.after");

                    throw new Exception(string.Format("raft.Log: Entry index does not exist (MAX={0}): (IDX={1}, TERM={2})", this.entries.Count, index, term));
                }

                // If we're truncating everything then just clear the entries.
                if (index == this.startIndex)
                {
                    DebugTrace.DebugLine("log.truncate.clear");

                    this.file.SetLength(0);

                    this.file.Seek(0, SeekOrigin.Begin);

                    this.entries = new List<LogEntry>();
                }
                else
                {
                    // Do not truncate if the entry at index does not have the matching term.
                    LogEntry entry = this.entries[index - this.startIndex - 1];

                    if (this.entries.Count > 0 && entry.Term != term)
                    {
                        DebugTrace.DebugLine("log.truncate.termMismatch");

                        throw new Exception(string.Format("raft.Log: Entry at index does not have matching term ({0}): (IDX={1}, TERM={2})", entry.Term, index, term));
                    }

                    // Otherwise truncate up to the desired entry.
                    if (index < this.startIndex + this.entries.Count)
                    {
                        DebugTrace.DebugLine("log.truncate.finish");

                        long position = this.entries[index - this.startIndex].Position;
                        this.file.SetLength(position);

                        this.file.Seek(position, SeekOrigin.Begin);

                        // notify clients if this node is the previous leader
                        for (int i = index - this.startIndex; i < this.entries.Count; i++)
                        {
                            entry = this.entries[i];
                        }

                        List<LogEntry> newEntries = new List<LogEntry>();
                        for (int i = 0; i < index - this.startIndex; i++)
                        {
                            newEntries.Add(this.entries[i]);
                        }
                        this.entries = newEntries;
                    }
                }
            }
        }

        //--------------------------------------
        // Append
        //--------------------------------------
        // Appends a series of entries to the log.
        internal void appendEntries(List<protobuf.LogEntry> entries)
        {
            lock (mutex)
            {
                long startPosition = this.file.Seek(0, SeekOrigin.Current);

                foreach (var e in entries)
                {
                    LogEntry logEntry = new LogEntry()
                    {
                        log = this,
                        Position = startPosition,
                        pb = e
                    };

                    long size = this.writeEntry(logEntry, this.file);

                    startPosition += size;
                }

                this.file.Flush();
            }
        }

        // Writes a single log entry to the end of the log.
        internal void appendEntry(LogEntry entry)
        {
            lock (mutex)
            {
                if (this.file == null)
                {
                    throw new Exception("raft.Log: Log is not open");
                }

                // Make sure the term and index are greater than the previous.
                if (this.entries.Count > 0)
                {
                    LogEntry lastEntry = this.entries[this.entries.Count - 1];

                    if (entry.Term < lastEntry.Term)
                    {
                        throw new Exception(string.Format("raft.Log: Cannot append entry with earlier term ({0}:{1} <= {2}:{3})", entry.Term, entry.Index, lastEntry.Term, lastEntry.Index));
                    }
                    else if (entry.Term == lastEntry.Term && entry.Index <= lastEntry.Index)
                    {
                        throw new Exception(string.Format("raft.Log: Cannot append entry with earlier index in the same term ({0}:{1} <= {2}:{3})", entry.Term, entry.Index, lastEntry.Term, lastEntry.Index));
                    }
                }

                long position = this.file.Seek(0, SeekOrigin.Current);

                entry.Position = position;

                // Write to storage.
                entry.Encode(this.file);

                // Append to entries list if stored on disk.
                this.entries.Add(entry);

                this.file.Flush();
            }
        }

        // appendEntry with Buffered io
        internal long writeEntry(LogEntry entry, Stream w)
        {
            if (this.file == null)
            {
                throw new Exception("raft.Log: Log is not open");
            }

            // Make sure the term and index are greater than the previous.
            if (this.entries.Count > 0)
            {

                LogEntry lastEntry = this.entries[this.entries.Count - 1];
                if (entry.Term < lastEntry.Term)
                {
                    throw new Exception(string.Format("raft.Log: Cannot append entry with earlier term ({0}:{1} <= {2}:{3})", entry.Term, entry.Index, lastEntry.Term, lastEntry.Index));
                }
                else if (entry.Term == lastEntry.Term && entry.Index <= lastEntry.Index)
                {
                    throw new Exception(string.Format("raft.Log: Cannot append entry with earlier index in the same term ({0}:{1} <= {2}:{3})", entry.Term, entry.Index, lastEntry.Term, lastEntry.Index));
                }
            }

            // Write to storage.
            int size = entry.Encode(w);
            // Append to entries list if stored on disk.
            this.entries.Add(entry);

            return (long)size;
        }

        //--------------------------------------
        // Log compaction
        //--------------------------------------

        // compact the log before index (including index)
        internal void compact(int index, int term)
        {
            List<LogEntry> entries = new List<LogEntry>();
            lock (mutex)
            {
                if (index == 0)
                {
                    return;
                }
                // nothing to compaction
                // the index may be greater than the current index if
                // we just recovery from on snapshot
                if (index >= this.internalCurrentIndex)
                {
                    entries = new List<LogEntry>();
                }
                else
                {
                    // get all log entries after index
                    for (int i = index - this.startIndex; i < this.entries.Count; i++)
                    {
                        entries.Add(this.entries[i]);
                    }
                }

                // create a new log file and add all the entries
                string new_file_path = this.path + ".new";
                using (FileStream file = new FileStream(new_file_path, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                {
                    foreach (var entry in entries)
                    {
                        long position = this.file.Seek(0, SeekOrigin.Current);
                        entry.Position = position;
                        entry.Encode(file);
                    }
                }

                this.file.Close();
                File.Replace(new_file_path, this.path, this.path + ".bak");
                File.Delete(new_file_path);

                this.file = File.Open(this.path, FileMode.OpenOrCreate, FileAccess.ReadWrite);

                // compaction the in memory log
                this.entries = entries;
                this.startIndex = index;
                this.startTerm = term;
            }
        }

        #endregion
    }
}
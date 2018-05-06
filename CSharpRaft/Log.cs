using System;
using System.Collections.Generic;
using System.IO;
using uint64 =System.UInt64;

namespace CSharpRaft
{
    // The results of the applying a log entry.
    public class logResult
    {
        object returnValue;
        Exception err;
    }

    
    public class Log
    {
        public Func<LogEntry, Command,object> ApplyFunc;

        internal FileStream file;

        internal string path;

        internal List<LogEntry> entries;

        internal int commitIndex;

        internal object mutex;

        internal int startIndex;   // the index before the first entry in the Log entries

        internal  int startTerm;

        internal bool initialized;

        public Log()
        {
            entries = new List<LogEntry>();
            mutex = new object();
        }

        //------------------------------------------------------------------------------
        //
        // Accessors
        //
        //------------------------------------------------------------------------------

        //--------------------------------------
        // Log Indices
        //--------------------------------------

        // The last committed index in the log.
        public int CommitIndex()
        {
            lock (mutex)
            {
                return this.commitIndex;
            }
        }

        // The current index in the log.
        public int currentIndex()
        {
            lock (mutex)
            {
                return this.internalCurrentIndex();
            }
        }

        // The current index in the log without locking
        public int internalCurrentIndex()
        {
            if (this.entries.Count == 0)
            {
                return this.startIndex;
            }
            return this.entries[this.entries.Count - 1].Index();
        }

        // The next index in the log.
        public int nextIndex()
        {
            return this.currentIndex() + 1;
        }

        // Determines if the log contains zero entries.
        public bool isEmpty()
        {
            lock (mutex)
            {
                return (this.entries.Count == 0) && (this.startIndex == 0);
            }
        }

        // The name of the last command in the log.
        public string lastCommandName()
        {
            lock (mutex)
            {
                if (this.entries.Count > 0)
                {
                    var entry = this.entries[this.entries.Count - 1];
                    if (entry != null)
                    {
                        return entry.CommandName();

                    }
                }
            }
            return "";
        }

        //--------------------------------------
        // Log Terms
        //--------------------------------------

        // The current term in the log.
        public int currentTerm()
        {
            lock (mutex)
            {

                if (this.entries.Count == 0)
                {
                    return this.startTerm;

                }
                return this.entries[this.entries.Count - 1].Term();
            }
        }





        //------------------------------------------------------------------------------
        //
        // Methods
        //
        //------------------------------------------------------------------------------

        //--------------------------------------
        // State
        //--------------------------------------

        // Opens the log file and reads existing entries. The log can remain open and
        // continue to append entries to the end of the log.
        public void open(string path)
        {
            // Read all the entries from the log if one exists.
            long readBytes;

            this.file = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            this.path = path;
            this.initialized = true;

            DebugTrace.DebugLine("log.open.create ", path);


            // Read the file and decode entries.
            //        for {
            //            // Instantiate log entry and decode into it.
            //            entry, _= newLogEntry(l, null, 0, 0, null)

            //    entry.Position, _ = this.file.Seek(0, os.SEEK_CUR)


            //    n, err= entry.Decode(this.file)

            //    if err != null {
            //                if err == io.EOF {

            //                    DebugTrace.DebugLine("open.log.append: finish ")

            //        }
            //                else
            //                {
            //                    if err = os.Truncate(path, readBytes); err != null {
            //                        return fmt.Errorf("raft.Log: Unable to recover: %v", err)

            //            }
            //                }
            //                break

            //    }
            //            if entry.Index() > this.startIndex {
            //                // Append entry.
            //                this.entries = append(this.entries, entry)

            //        if entry.Index() <= this.commitIndex {
            //                    command, err= newCommand(entry.CommandName(), entry.Command())

            //            if err != null {
            //                        continue

            //            }
            //                    this.Applypublic (entry, command)

            //        }

            //                DebugTrace.DebugLine("open.log.append log index ", entry.Index())

            //    }

            //            readBytes += int64(n)

            //}

            //        DebugTrace.DebugLine("open.log.recovery number of log ", this.entries.Count)

            //this.initialized = true

            //return null
        }

        // Closes the log file.
        public void close()
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
        public void sync()
        {
            this.file.Flush();
        }

        //--------------------------------------
        // Entries
        //--------------------------------------

        // Creates a log entry associated with this log.
        public LogEntry createEntry(int term, Command command,LogEvent ev)
        {
            return new LogEntry(this, ev,this.nextIndex(), term, command);
        }

        // Retrieves an entry from the log. If the entry has been eliminated because
        // of a snapshot then null is returned.
        public LogEntry getEntry(int index)
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
        public bool containsEntry(int index, int term)
        {
            LogEntry entry = this.getEntry(index);
            return (entry != null && entry.Term() == term);
        }

        // Retrieves a list of entries after a given index as well as the term of the
        // index provided. A null list of entries is returned if the index no longer
        // exists because a snapshot was made.
        public void getEntriesAfter(int index, int maxLogEntriesPerRequest, out List<LogEntry> entries, out int term)
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
                    throw new Exception(string.Format("raft: Index is beyond end of log: %d %d", this.entries.Count, index));
                }

                // If we're going from the beginning of the log then return the whole log.
                if (index == this.startIndex)
                {
                    DebugTrace.TraceLine("log.entriesAfter.beginning: ", index, " ", this.startIndex);

                    entries = this.entries;
                    term = this.startTerm;
                }

                DebugTrace.TraceLine("log.entriesAfter.partial: ", index, " ", this.entries[this.entries.Count - 1].Index());

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
                    term = this.entries[index - 1 - this.startIndex].Term();
                }
                else
                {
                    List<LogEntry> newEntries = new List<LogEntry>();
                    for (int i = 0; i < maxLogEntriesPerRequest; i++)
                    {
                        newEntries.Add(entries[i]);
                    }
                    entries = newEntries;
                    term = this.entries[index - 1 - this.startIndex].Term();
                }
            }
        }

        //--------------------------------------
        // Commit
        //--------------------------------------

        // Retrieves the last index and term that has been committed to the log.
        public void commitInfo(out int index, out int term)
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
                index = entry.Index();
                term = entry.Term();
            }
        }

        // Retrieves the last index and term that has been appended to the log.
        public void lastInfo(out int index, out int term)
        {
            lock (mutex)
            {
                // If we don't have any entries then just return zeros.
                if (this.entries.Count == 0)
                {
                    index = this.startIndex;
                    term = this.startTerm;
                }

                // Return the last index & term
                LogEntry entry = this.entries[this.entries.Count - 1];

                index = entry.Index();
                term = entry.Term();
            }
        }

        // Updates the commit index
        public void updateCommitIndex(int index)
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
        public void setCommitIndex(int index)
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
                    this.commitIndex = entry.Index();

                    // Decode the command.
                    Command command = Commands.newCommand(entry.CommandName(), entry.Command());

                    // Apply the changes to the state machine and store the error code.
                    object returnValue = this.ApplyFunc(entry, command);

                    DebugTrace.Debug("setCommitIndex.set.result index: %v, entries index: %v", i, entryIndex);
                    if (entry.ev != null)
                    {
                        entry.ev.returnValue = returnValue;
                    }

                    // we can only commit up to the most recent join command
                    // if there is a join in this batch of commands.
                    // after this commit, we need to recalculate the majority.
                    if (command is JoinCommand)
                    {
                        return;
                    }
                }
            }
        }

        // Set the commitIndex at the head of the log file to the current
        // commit Index. This should be called after obtained a log lock
        public void flushCommitIndex()
{
            this.file.Seek(0, SeekOrigin.Begin);

            //TODO:
            //fmt.Fprintf(this.file, "%8x\n", this.commitIndex);

            this.file.Seek(0, SeekOrigin.End);
        }

////--------------------------------------
//// Truncation
////--------------------------------------

//// Truncates the log to the given index and term. This only works if the log
//// at the index has not been committed.
//public void truncate(int index , int term )  {

//            lock (mutex) { 
//            DebugTrace.DebugLine("log.truncate: ", index);

//        	// Do not allow committed entries to be truncated.
//        	if (index<this.commitIndex)
//{

//                DebugTrace.DebugLine("log.truncate.before");

//                return fmt.Errorf("raft.Log: Index is already committed (%v): (IDX=%v, TERM=%v)", this.commitIndex, index, term);

//            }

//        	// Do not truncate past end of entries.
//        	if (index > this.startIndex+this.entries.Count)
//        {
//                DebugTrace.DebugLine("log.truncate.after");

//                return fmt.Errorf("raft.Log: Entry index does not exist (MAX=%v): (IDX=%v, TERM=%v)", this.entries.Count, index, term);

//            }

//        	// If we're truncating everything then just clear the entries.
//        	if (index == this.startIndex)
//{

//                DebugTrace.DebugLine("log.truncate.clear");

//                this.file.Truncate(0);

//                this.file.Seek(0, SeekOrigin.Begin);

//                    // notify clients if this node is the previous leader
//                    foreach (var entry in this.entries)
//                    {
//                        if (entry.ev != null)
//            {
//                            entry.ev.c = errors.New("command failed to be committed due to node failure");

//                        }
//                    }

//                    this.entries = new List<LogEntry>();
//} else {
//    // Do not truncate if the entry at index does not have the matching term.
//    entry= this.entries[index - this.startIndex - 1]

//                if this.entries.Count > 0 && entry.Term() != term {

//        DebugTrace.DebugLine("log.truncate.termMismatch")

//                    return fmt.Errorf("raft.Log: Entry at index does not have matching term (%v): (IDX=%v, TERM=%v)", entry.Term(), index, term)

//                }

//    // Otherwise truncate up to the desired entry.
//    if index < this.startIndex + uint64(this.entries.Count) {

//    DebugTrace.DebugLine("log.truncate.finish");

//                    position = this.entries[index - this.startIndex].Position;
//                      this.file.Truncate(position);

//                    this.file.Seek(position, os.SEEK_SET);

//                    // notify clients if this node is the previous leader
//        for i = index - this.startIndex; i < uint64(this.entries.Count); i++ {
//            entry= this.entries[i]

//                        if entry.event != null
//        {
//                entry.event.c < -errors.New("command failed to be committed due to node failure");

//                        }
//        }

//    this.entries = this.entries[0 : index - this.startIndex];

//                }
//}

//        	return null;
//}
//        }

////--------------------------------------
//// Append
////--------------------------------------

//// Appends a series of entries to the log.
//public void appendEntries(entries[]*protobuf.LogEntry) error {

//            this.mutex.Lock()
//            defer this.mutex.Unlock()

//            startPosition, _ = this.file.Seek(0, os.SEEK_CUR)

//        	w = bufio.NewWriter(this.file)

//            var size int64

//            var err error
//        	// Append each entry but exit if we hit an error.
//        	for i = range entries
//{
//    logEntry= &LogEntry
//            {
//        log: l,
//        			Position: startPosition,
//        			pb: entries[i],
//        		}

//    if size, err = this.writeEntry(logEntry, w); err != null
//            {
//        return err

//                }

//    startPosition += size
//        }
//w.Flush()
//        err = this.sync()

//        	if err != null {

//    panic(err)

//            }

//        	return null
//        }

//// Writes a single log entry to the end of the log.
//public void appendEntry(entry* LogEntry) error {

//            this.mutex.Lock()
//            defer this.mutex.Unlock()

//        	if this.file == null {
//        		return errors.New("raft.Log: Log is not open")
//        	}

//        	// Make sure the term and index are greater than the previous.
//        	if this.entries.Count > 0 {

//                lastEntry = this.entries[this.entries.Count - 1]
//        		if entry.Term() < lastEntry.Term() {
//        			return fmt.Errorf("raft.Log: Cannot append entry with earlier term (%x:%x <= %x:%x)", entry.Term(), entry.Index(), lastEntry.Term(), lastEntry.Index())
//        		} else if entry.Term() == lastEntry.Term() && entry.Index() <= lastEntry.Index() {
//        			return fmt.Errorf("raft.Log: Cannot append entry with earlier index in the same term (%x:%x <= %x:%x)", entry.Term(), entry.Index(), lastEntry.Term(), lastEntry.Index())
//        		}
//        	}

//        	position, _ = this.file.Seek(0, os.SEEK_CUR)

//        	entry.Position = position

//        	// Write to storage.
//        	if _, err = entry.Encode(this.file); err != null {
//    return err

//            }

//        	// Append to entries list if stored on disk.
//        	this.entries = append(this.entries, entry)

//        	return null
//        }

//// appendEntry with Buffered io
//public void writeEntry(entry* LogEntry, w io.Writer) (int64, error) {
//        	if this.file == null {
//        		return -1, errors.New("raft.Log: Log is not open")
//        	}

//        	// Make sure the term and index are greater than the previous.
//        	if this.entries.Count > 0 {

//                lastEntry = this.entries[this.entries.Count - 1]
//        		if entry.Term() < lastEntry.Term() {
//        			return -1, fmt.Errorf("raft.Log: Cannot append entry with earlier term (%x:%x <= %x:%x)", entry.Term(), entry.Index(), lastEntry.Term(), lastEntry.Index())
//        		} else if entry.Term() == lastEntry.Term() && entry.Index() <= lastEntry.Index() {
//        			return -1, fmt.Errorf("raft.Log: Cannot append entry with earlier index in the same term (%x:%x <= %x:%x)", entry.Term(), entry.Index(), lastEntry.Term(), lastEntry.Index())
//        		}
//        	}

//        	// Write to storage.
//        	size, err = entry.Encode(w)
//        	if err != null {
//        		return -1, err
//        	}

//            // Append to entries list if stored on disk.
//            this.entries = append(this.entries, entry)

//        	return int64(size), null
//        }

////--------------------------------------
//// Log compaction
////--------------------------------------

//// compact the log before index (including index)
//public void compact(index uint64, term uint64) error {
//        	var entries[]* LogEntry


//            this.mutex.Lock()
//            defer this.mutex.Unlock()

//        	if index == 0 {
//        		return null

//            }
//        	// nothing to compaction
//        	// the index may be greater than the current index if
//        	// we just recovery from on snapshot
//        	if index >= this.internalCurrentIndex() {

//                entries = make([] * LogEntry, 0)
//        	} else {
//        		// get all log entries after index
//        		entries = this.entries[index - this.startIndex:]

//            }

//// create a new log file and add all the entries
//new_file_path = this.path + ".new"

//file, err = os.OpenFile(new_file_path, os.O_APPEND|os.O_CREATE|os.O_WRONLY, 0600)
//        	if err != null {
//    return err

//            }
//        	for _, entry = range entries
//{
//    position, _= this.file.Seek(0, os.SEEK_CUR)

//                entry.Position = position


//                if _, err = entry.Encode(file); err != null
//            {
//        file.Close()

//                    os.Remove(new_file_path)

//                    return err

//                }
//}
//file.Sync()

//        old_file = this.file

//// rename the new log file
//err = os.Rename(new_file_path, this.path)
//        	if err != null {
//    file.Close()
//                os.Remove(new_file_path)

//                return err

//            }
//        	this.file = file

//// close the old log file
//old_file.Close()

//            // compaction the in memory log
//        this.entries = entries
//            this.startIndex = index
//            this.startTerm = term
//        	return null
//        }

    }
}
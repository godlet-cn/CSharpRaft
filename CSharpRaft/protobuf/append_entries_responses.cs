//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from: proto/append_entries_responses.proto
namespace protobuf
{
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"AppendEntriesResponse")]
  public partial class AppendEntriesResponse : global::ProtoBuf.IExtensible
  {
    public AppendEntriesResponse() {}
    
    private uint _Term;
    [global::ProtoBuf.ProtoMember(1, IsRequired = true, Name=@"Term", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    public uint Term
    {
      get { return _Term; }
      set { _Term = value; }
    }
    private uint _Index;
    [global::ProtoBuf.ProtoMember(2, IsRequired = true, Name=@"Index", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    public uint Index
    {
      get { return _Index; }
      set { _Index = value; }
    }
    private uint _CommitIndex;
    [global::ProtoBuf.ProtoMember(3, IsRequired = true, Name=@"CommitIndex", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    public uint CommitIndex
    {
      get { return _CommitIndex; }
      set { _CommitIndex = value; }
    }
    private bool _Success;
    [global::ProtoBuf.ProtoMember(4, IsRequired = true, Name=@"Success", DataFormat = global::ProtoBuf.DataFormat.Default)]
    public bool Success
    {
      get { return _Success; }
      set { _Success = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
}
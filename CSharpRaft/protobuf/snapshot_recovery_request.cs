//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from: proto/snapshot_recovery_request.proto
namespace protobuf
{
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"SnapshotRecoveryRequest")]
  public partial class SnapshotRecoveryRequest : global::ProtoBuf.IExtensible
  {
    public SnapshotRecoveryRequest() {}
    
    private string _LeaderName;
    [global::ProtoBuf.ProtoMember(1, IsRequired = true, Name=@"LeaderName", DataFormat = global::ProtoBuf.DataFormat.Default)]
    public string LeaderName
    {
      get { return _LeaderName; }
      set { _LeaderName = value; }
    }
    private uint _LastIndex;
    [global::ProtoBuf.ProtoMember(2, IsRequired = true, Name=@"LastIndex", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    public uint LastIndex
    {
      get { return _LastIndex; }
      set { _LastIndex = value; }
    }
    private uint _LastTerm;
    [global::ProtoBuf.ProtoMember(3, IsRequired = true, Name=@"LastTerm", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    public uint LastTerm
    {
      get { return _LastTerm; }
      set { _LastTerm = value; }
    }
    private global::System.Collections.Generic.List<protobuf.SnapshotRecoveryRequest.Peer> _Peers = new global::System.Collections.Generic.List<protobuf.SnapshotRecoveryRequest.Peer>();
    [global::ProtoBuf.ProtoMember(4, Name=@"Peers", DataFormat = global::ProtoBuf.DataFormat.Default)]
    public global::System.Collections.Generic.List<protobuf.SnapshotRecoveryRequest.Peer> Peers
    {
      get { return _Peers; }
      set {  _Peers=value; }
    }
  
    private byte[] _State;
    [global::ProtoBuf.ProtoMember(5, IsRequired = true, Name=@"State", DataFormat = global::ProtoBuf.DataFormat.Default)]
    public byte[] State
    {
      get { return _State; }
      set { _State = value; }
    }
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"Peer")]
  public partial class Peer : global::ProtoBuf.IExtensible
  {
    public Peer() {}
    
    private string _Name;
    [global::ProtoBuf.ProtoMember(1, IsRequired = true, Name=@"Name", DataFormat = global::ProtoBuf.DataFormat.Default)]
    public string Name
    {
      get { return _Name; }
      set { _Name = value; }
    }
    private string _ConnectionString;
    [global::ProtoBuf.ProtoMember(2, IsRequired = true, Name=@"ConnectionString", DataFormat = global::ProtoBuf.DataFormat.Default)]
    public string ConnectionString
    {
      get { return _ConnectionString; }
      set { _ConnectionString = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
}
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// Generated from: proto/request_vote_request.proto
namespace protobuf
{
  [global::System.Serializable, global::ProtoBuf.ProtoContract(Name=@"RequestVoteRequest")]
  public partial class RequestVoteRequest : global::ProtoBuf.IExtensible
  {
    public RequestVoteRequest() {}
    
    private uint _Term;
    [global::ProtoBuf.ProtoMember(1, IsRequired = true, Name=@"Term", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    public uint Term
    {
      get { return _Term; }
      set { _Term = value; }
    }
    private uint _LastLogIndex;
    [global::ProtoBuf.ProtoMember(2, IsRequired = true, Name=@"LastLogIndex", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    public uint LastLogIndex
    {
      get { return _LastLogIndex; }
      set { _LastLogIndex = value; }
    }
    private uint _LastLogTerm;
    [global::ProtoBuf.ProtoMember(3, IsRequired = true, Name=@"LastLogTerm", DataFormat = global::ProtoBuf.DataFormat.TwosComplement)]
    public uint LastLogTerm
    {
      get { return _LastLogTerm; }
      set { _LastLogTerm = value; }
    }
    private string _CandidateName;
    [global::ProtoBuf.ProtoMember(4, IsRequired = true, Name=@"CandidateName", DataFormat = global::ProtoBuf.DataFormat.Default)]
    public string CandidateName
    {
      get { return _CandidateName; }
      set { _CandidateName = value; }
    }
    private global::ProtoBuf.IExtension extensionObject;
    global::ProtoBuf.IExtension global::ProtoBuf.IExtensible.GetExtensionObject(bool createIfMissing)
      { return global::ProtoBuf.Extensible.GetExtensionObject(ref extensionObject, createIfMissing); }
  }
  
}
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json.Nodes;
using AtlasLetrero.App.Protocol;
using AtlasLetrero.App.Services;

sealed class FakeTransport : ISerialTransport
{
    readonly ConcurrentQueue<byte> bytes = new(); int reads; public bool IsOpen { get; set; } = true; public string Name => "fake";
    public AtlasLedCommand? FailCommand; public bool BadCrc; public bool BadSequence; public bool DisconnectOnRead;
    public Task WriteAsync(ReadOnlyMemory<byte> input, CancellationToken ct)
    { ct.ThrowIfCancellationRequested(); var request=PacketCodec.Decode(input.Span); AtlasLedPacket response;
      if (FailCommand==request.Command) response=new(AtlasLedCommand.Nack,request.Sequence,[]);
      else response=request.Command switch { AtlasLedCommand.Hello=>new(AtlasLedCommand.HelloAck,request.Sequence,[]), AtlasLedCommand.GetCapabilities=>new(AtlasLedCommand.Capabilities,request.Sequence,Encoding.UTF8.GetBytes("{\"protocol\":1,\"width\":64,\"height\":32}")), _=>new(AtlasLedCommand.Ack,request.Sequence,[]) };
      var encoded=PacketCodec.Encode(response); if(BadCrc && request.Command is not (AtlasLedCommand.Hello or AtlasLedCommand.GetCapabilities)) encoded[^1]^=1; if(BadSequence && request.Command is not (AtlasLedCommand.Hello or AtlasLedCommand.GetCapabilities)) encoded[6]^=1; foreach(var b in encoded)bytes.Enqueue(b); return Task.CompletedTask; }
    public Task<int> ReadAsync(Memory<byte> target, CancellationToken ct) { ct.ThrowIfCancellationRequested(); if(DisconnectOnRead && ++reads>4){IsOpen=false;return Task.FromResult(0);} var i=0; while(i<target.Length&&bytes.TryDequeue(out var b))target.Span[i++]=b; return Task.FromResult(i); }
    public void Dispose()=>IsOpen=false;
}

static class Program
{
    static JsonObject Packet(int width=8,int height=4,int fps=10,int frames=1) => new(){["version"]=1,["width"]=width,["height"]=height,["fps"]=fps,["durationMs"]=100,["frames"]=new JsonArray(Enumerable.Range(0,frames).Select(_=>(JsonNode)new JsonArray(Enumerable.Range(0,width*height*4).Select(__=>(JsonNode)JsonValue.Create(255)!).ToArray())).ToArray())};
    static void Check(bool condition,string name){if(!condition)throw new Exception("FAIL: "+name);Console.WriteLine("PASS: "+name);}
    static DeviceConnectionService Connected(FakeTransport fake){var service=new DeviceConnectionService(_=>fake); service.Connect("fake"); return service;}
    public static void Main(){
      var fake=new FakeTransport(); using var s=Connected(fake); var p=Packet(); var hash=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(p.ToJsonString()))).ToLowerInvariant();
      var checksum=s.Upload(p,hash); var candidate=s.CandidateId!; Check(candidate.Length==32,"VERIFY produce candidato nonceado"); Check(s.Activate(candidate)==checksum,"ACTIVATE exactamente una vez"); try{s.Activate(candidate);throw new Exception();}catch(InvalidOperationException){Check(true,"candidato consumido no se reactiva");}
      foreach(var phase in new[]{AtlasLedCommand.BeginUpload,AtlasLedCommand.FrameData,AtlasLedCommand.Verify}){using var f=new FakeTransport{FailCommand=phase};using var x=Connected(f);try{x.Upload(p,hash);throw new Exception();}catch(InvalidDataException){Check(x.CandidateId is null,"NACK invalida candidato en "+phase);}}
      using(var f=new FakeTransport{BadCrc=true})using(var x=Connected(f)){try{x.Upload(p,hash);throw new Exception();}catch(InvalidDataException){Check(x.CandidateId is null,"CRC invalido invalida candidato");}}
      using(var f=new FakeTransport{BadSequence=true})using(var x=Connected(f)){try{x.Upload(p,hash);throw new Exception();}catch(InvalidDataException){Check(x.CandidateId is null,"secuencia invalida invalida candidato");}}
      using(var f=new FakeTransport{DisconnectOnRead=true})using(var x=Connected(f)){try{x.Upload(p,hash);throw new Exception();}catch(Exception){Check(x.CandidateId is null,"desconexion invalida candidato");}}
      using(var x=Connected(new FakeTransport())){try{x.Upload(p,"0".PadLeft(64,'0'));throw new Exception();}catch(InvalidDataException){Check(x.CandidateId is null,"checksum incorrecto no llega al puerto");}}
      using(var x=Connected(new FakeTransport())){try{x.Upload(Packet(65),hash);throw new Exception();}catch(InvalidDataException){Check(x.CandidateId is null,"capacidad incompatible no llega al puerto");}}
      using(var x=Connected(new FakeTransport())){using var c=new CancellationTokenSource();c.Cancel();try{x.Upload(p,hash,c.Token);throw new Exception();}catch(OperationCanceledException){Check(x.CandidateId is null,"cancelacion invalida candidato y no activa");}}
      using(var x=Connected(new FakeTransport())){var h=x.Upload(p,hash);var old=x.CandidateId!;var p2=Packet(8,4,10,1);var h2=x.Upload(p2,h);Check(x.CandidateId!=old,"segundo upload sustituye candidato");try{x.Activate(old);throw new Exception();}catch(InvalidOperationException){Check(true,"candidato viejo rechazado");}Check(x.Activate(x.CandidateId!)==h2,"nuevo candidato activa");}
      Console.WriteLine("PASS: ningún fallo anterior a ACTIVATE invoca ACTIVATE");
    }
}

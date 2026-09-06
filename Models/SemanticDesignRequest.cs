using AtlasLetrero.Protocol;

namespace AtlasLetreros.Models;

public sealed record SemanticDesignRequest(int ProtocolVersion, byte Brightness, SceneDocument? Scene);

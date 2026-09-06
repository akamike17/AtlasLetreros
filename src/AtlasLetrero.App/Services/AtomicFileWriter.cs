namespace AtlasLetrero.App.Services;
public sealed class AtomicFileWriter {
    public void Write(string path, byte[] bytes, Action<string>? validate = null) {
        var temp = path + "." + Guid.NewGuid() + ".tmp";
        try {
            using (var f = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { f.Write(bytes); f.Flush(true); }
            validate?.Invoke(temp);
            if (File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path);
        } finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}

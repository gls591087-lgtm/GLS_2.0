using LocalServerLauncher;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace LocalServerLauncher
{
    public partial class Form2 : Form
    {
        private string pinPath = string.Empty;
        private PINFile? pinFile;

        public Form2()
        {
            InitializeComponent();

            LoadPin();
        }

        // ============================================================
        // PIN LOAD
        // ============================================================

        private void LoadPin()
        {
            try
            {
                string baseDir = AppContext.BaseDirectory;

                string[] files = Directory.GetFiles(
                    baseDir,
                    "KartRider.pin",
                    SearchOption.AllDirectories);

                if (files.Length == 0)
                {
                    MessageBox.Show(
                        "Could not find KartRider.pin in the launcher folder or its subfolders.",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return;
                }

                pinPath = files[0];

                pinFile = new PINFile(pinPath);

                if (pinFile.AuthMethods.Count == 0)
                {
                    MessageBox.Show(
                        "The PIN file does not contain an AuthMethod.",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return;
                }

                if (pinFile.AuthMethods[0].LoginServers.Count == 0)
                {
                    MessageBox.Show(
                        "The PIN file does not contain a LoginServer.",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);

                    return;
                }

                IPEndPoint endpoint =
                    pinFile.AuthMethods[0].LoginServers[0];

                textBox1.Text = endpoint.IP;
                textBox2.Text = endpoint.Port.ToString();

                System.Diagnostics.Debug.WriteLine(
                    $"[PIN] Loaded: {pinPath}");

                System.Diagnostics.Debug.WriteLine(
                    $"[PIN] LoginServer: {endpoint}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.ToString(),
                    "PIN Read Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // ============================================================
        // SAVE
        // ============================================================

        private void button1_Click(object sender, EventArgs e)
        {
            if (pinFile == null)
            {
                MessageBox.Show(
                    "The PIN file has not been loaded.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            if (string.IsNullOrWhiteSpace(pinPath))
            {
                MessageBox.Show(
                    "The PIN file path is missing.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            if (pinFile.AuthMethods.Count == 0)
            {
                MessageBox.Show(
                    "AuthMethod is missing.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            if (pinFile.AuthMethods[0].LoginServers.Count == 0)
            {
                MessageBox.Show(
                    "LoginServer is missing.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            string ip =
                textBox1.Text.Trim();

            string portText =
                textBox2.Text.Trim();

            if (!IPAddress.TryParse(
                    ip,
                    out IPAddress? address))
            {
                MessageBox.Show(
                    "Enter a valid IP address.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            if (address.AddressFamily !=
                System.Net.Sockets.AddressFamily.InterNetwork)
            {
                MessageBox.Show(
                    "Only IPv4 addresses are supported.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            if (!ushort.TryParse(
                    portText,
                    out ushort port))
            {
                MessageBox.Show(
                    "Enter a valid port.",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return;
            }

            try
            {
                IPEndPoint endpoint =
                    pinFile.AuthMethods[0].LoginServers[0];

                endpoint.IP = ip;
                endpoint.Port = port;

                byte[] output =
                    pinFile.GetEncryptedData();

                string backupPath =
                    pinPath + ".bak";

                // Back up the existing file
                File.Copy(
                    pinPath,
                    backupPath,
                    true);

                // Save the updated PIN file
                File.WriteAllBytes(
                    pinPath,
                    output);

                MessageBox.Show(
                    "KartRider.pin was saved.\r\n\r\n" +
                    $"IP: {ip}\r\n" +
                    $"Port: {port}\r\n\r\n" +
                    $"Backup: {backupPath}",
                    "Done",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.ToString(),
                    "PIN Save Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // ============================================================
        // FORM EVENTS
        // ============================================================

        private void Form2_Load(
            object sender,
            EventArgs e)
        {
        }

        // ============================================================
        // PIN FILE
        // ============================================================

        private sealed class PINFile
        {
            private const uint RTTI_PINOBJECT =
                284099454u;

            private const uint ENCODE_KEY =
                862557747u;

            public PINHeader Header { get; private set; }

            public List<AuthMethod> AuthMethods { get; private set; }

            public List<BmlObject> BmlObjects { get; private set; }

            public int NullsBeforeBmlObjects { get; private set; }

            public PINFile()
            {
                Header = new PINHeader();

                AuthMethods =
                    new List<AuthMethod>();

                BmlObjects =
                    new List<BmlObject>();

                NullsBeforeBmlObjects = 0;
            }

            public PINFile(string path)
                : this()
            {
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException(
                        "Unable to locate PIN file.",
                        path);
                }

                byte[] input =
                    File.ReadAllBytes(path);

                byte[] decoded =
                    Decrypt(input);

                using InPacket packet =
                    new InPacket(decoded);

                Read(packet);
            }

            public byte[] GetEncryptedData()
            {
                return Encrypt(this);
            }

            // ========================================================
            // DECRYPT
            // ========================================================

            private static byte[] Decrypt(byte[] input)
            {
                if (input == null)
                    throw new ArgumentNullException(
                        nameof(input));

                using InPacket packet =
                    new InPacket(input);

                if (packet.Available < 4)
                {
                    throw new InvalidDataException(
                        "PIN file is too small.");
                }

                int length =
                    packet.ReadInt();

                if (length < 0)
                {
                    throw new InvalidDataException(
                        $"Invalid encoded length: {length}");
                }

                if (length > packet.Available)
                {
                    throw new InvalidDataException(
                        $"Invalid encoded block length. " +
                        $"Length={length}, " +
                        $"Available={packet.Available}");
                }

                byte[] encoded =
                    packet.ReadBytes(length);

                System.Diagnostics.Debug.WriteLine(
                    $"[PIN] Total length     = {input.Length}");

                System.Diagnostics.Debug.WriteLine(
                    $"[PIN] Encoded length   = {length}");

                if (encoded.Length >= 2)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[PIN] Magic            = 0x{encoded[0]:X2}");

                    System.Diagnostics.Debug.WriteLine(
                        $"[PIN] Flags            = 0x{encoded[1]:X2}");
                }

                return KREncodedBlock.Decode(
                    encoded);
            }

            // ========================================================
            // ENCRYPT
            // ========================================================

            private static byte[] Encrypt(
                PINFile pinFile)
            {
                using OutPacket raw =
                    new OutPacket();

                pinFile.Write(raw);

                byte[] rawData =
                    raw.ToArray();

                System.Diagnostics.Debug.WriteLine(
                    $"[PIN] Raw size before encode = {rawData.Length}");

                byte[] encoded =
                    KREncodedBlock.Encode(
                        rawData,
                        KREncodedBlock.EncodeFlag.ZLib |
                        KREncodedBlock.EncodeFlag.KartCrypto,
                        ENCODE_KEY);

                using OutPacket result =
                    new OutPacket();

                result.WriteInt(
                    encoded.Length);

                result.WriteBytes(
                    encoded);

                byte[] finalData =
                    result.ToArray();

                System.Diagnostics.Debug.WriteLine(
                    $"[PIN] Final PIN size = {finalData.Length}");

                return finalData;
            }

            // ========================================================
            // READ
            // ========================================================

            private void Read(
                InPacket p)
            {
                uint magic =
                    p.ReadUInt();

                if (magic != RTTI_PINOBJECT)
                {
                    throw new InvalidDataException(
                        $"This is not a valid PIN file. " +
                        $"RTTI=0x{magic:X8}");
                }

                Header =
                    new PINHeader();

                Header.Unk1 =
                    p.ReadByte();

                Header.LocaleID =
                    p.ReadUShort();

                Header.Unk2 =
                    p.ReadUShort();

                Header.LocaleType =
                    p.ReadByte();

                Header.Unk3 =
                    p.ReadByte();

                Header.MinorVersion =
                    p.ReadUShort();

                Header.Unk4 =
                    p.ReadByte();

                Header.Unk5 =
                    p.ReadByte();

                Header.LoginType =
                    p.ReadByte();

                Header.AESKey =
                    p.ReadString();

                Header.URL =
                    p.ReadString();

                Header.PatchURL =
                    p.ReadString();

                int authCount =
                    p.ReadInt();

                if (authCount < 0 ||
                    authCount > 10000)
                {
                    throw new InvalidDataException(
                        $"Invalid AuthMethod count: {authCount}");
                }

                AuthMethods =
                    new List<AuthMethod>(
                        authCount);

                for (int i = 0;
                     i < authCount;
                     i++)
                {
                    AuthMethod auth =
                        new AuthMethod();

                    auth.Index =
                        p.ReadByte();

                    auth.Name =
                        p.ReadString();

                    auth.AccountConfig =
                        ReadBML(p);

                    int serverCount =
                        p.ReadInt();

                    if (serverCount < 0 ||
                        serverCount > 10000)
                    {
                        throw new InvalidDataException(
                            $"Invalid LoginServer count: {serverCount}");
                    }

                    auth.LoginServers =
                        new List<IPEndPoint>(
                            serverCount);

                    for (int j = 0;
                         j < serverCount;
                         j++)
                    {
                        IPEndPoint endpoint =
                            new IPEndPoint();

                        endpoint.Read(p);

                        auth.LoginServers.Add(
                            endpoint);
                    }

                    auth.ExtraConfig =
                        ReadBML(p);

                    AuthMethods.Add(auth);
                }

                // ====================================================
                // NULL PADDING
                // ====================================================

                NullsBeforeBmlObjects = 0;

                while (p.Available > 0)
                {
                    int position =
                        p.Position;

                    byte value =
                        p.ReadByte();

                    if (value == 0)
                    {
                        NullsBeforeBmlObjects++;
                        continue;
                    }

                    p.Position =
                        position;

                    break;
                }

                // ====================================================
                // BML OBJECTS
                // ====================================================

                BmlObjects =
                    new List<BmlObject>();

                while (p.Available > 0)
                {
                    BmlObject? obj =
                        ReadBML(p);

                    if (obj != null)
                    {
                        BmlObjects.Add(obj);
                    }
                }
            }

            // ========================================================
            // WRITE
            // ========================================================

            private void Write(
                OutPacket p)
            {
                p.WriteUInt(
                    RTTI_PINOBJECT);

                p.WriteByte(
                    Header.Unk1);

                p.WriteUShort(
                    Header.LocaleID);

                p.WriteUShort(
                    Header.Unk2);

                p.WriteByte(
                    Header.LocaleType);

                p.WriteByte(
                    Header.Unk3);

                p.WriteUShort(
                    Header.MinorVersion);

                p.WriteByte(
                    Header.Unk4);

                p.WriteByte(
                    Header.Unk5);

                p.WriteByte(
                    Header.LoginType);

                p.WriteString(
                    Header.AESKey);

                p.WriteString(
                    Header.URL);

                p.WriteString(
                    Header.PatchURL);

                p.WriteInt(
                    AuthMethods.Count);

                foreach (AuthMethod auth
                         in AuthMethods)
                {
                    p.WriteByte(
                        auth.Index);

                    p.WriteString(
                        auth.Name);

                    WriteBML(
                        p,
                        auth.AccountConfig);

                    p.WriteInt(
                        auth.LoginServers.Count);

                    foreach (IPEndPoint endpoint
                             in auth.LoginServers)
                    {
                        endpoint.Write(p);
                    }

                    WriteBML(
                        p,
                        auth.ExtraConfig);
                }

                if (NullsBeforeBmlObjects > 0)
                {
                    p.WriteBytes(
                        new byte[
                            NullsBeforeBmlObjects]);
                }

                foreach (BmlObject obj
                         in BmlObjects)
                {
                    WriteBML(
                        p,
                        obj);
                }
            }

            // ========================================================
            // BML
            // ========================================================

            private static BmlObject? ReadBML(
                InPacket p)
            {
                bool exists =
                    p.ReadBool();

                if (!exists)
                    return null;

                return new BmlObject(p);
            }

            private static void WriteBML(
                OutPacket p,
                BmlObject? bml)
            {
                p.WriteBool(
                    bml != null);

                if (bml != null)
                {
                    bml.Save(p);
                }
            }
        }

        // ============================================================
        // PIN HEADER
        // ============================================================

        private sealed class PINHeader
        {
            public byte Unk1 { get; set; }

            public ushort LocaleID { get; set; }

            public ushort Unk2 { get; set; }

            public byte LocaleType { get; set; }

            public byte Unk3 { get; set; }

            public ushort MinorVersion { get; set; }

            public byte Unk4 { get; set; }

            public byte Unk5 { get; set; }

            public byte LoginType { get; set; }

            public string AESKey { get; set; }

            public string URL { get; set; }

            public string PatchURL { get; set; }

            public PINHeader()
            {
                AESKey =
                    string.Empty;

                URL =
                    string.Empty;

                PatchURL =
                    string.Empty;
            }
        }

        // ============================================================
        // IP ENDPOINT
        // ============================================================

        private sealed class IPEndPoint
        {
            public string IP { get; set; }

            public ushort Port { get; set; }

            public IPEndPoint()
            {
                IP =
                    string.Empty;
            }

            public void Read(
                InPacket p)
            {
                IP =
                    $"{p.ReadByte()}." +
                    $"{p.ReadByte()}." +
                    $"{p.ReadByte()}." +
                    $"{p.ReadByte()}";

                Port =
                    p.ReadUShort();
            }

            public void Write(
                OutPacket p)
            {
                if (!IPAddress.TryParse(
                        IP,
                        out IPAddress? address))
                {
                    throw new InvalidDataException(
                        $"Invalid IP address: {IP}");
                }

                if (address.AddressFamily !=
                    System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    throw new InvalidDataException(
                        $"Only IPv4 addresses are supported: {IP}");
                }

                p.WriteEndPoint(
                    address,
                    Port);
            }

            public override string ToString()
            {
                return
                    IP +
                    ":" +
                    Port;
            }
        }

        // ============================================================
        // AUTH METHOD
        // ============================================================

        private sealed class AuthMethod
        {
            public byte Index { get; set; }

            public string Name { get; set; }

            public List<IPEndPoint> LoginServers { get; set; }

            public BmlObject? AccountConfig { get; set; }

            public BmlObject? ExtraConfig { get; set; }

            public AuthMethod()
            {
                Name =
                    string.Empty;

                LoginServers =
                    new List<IPEndPoint>();
            }
        }

        // ============================================================
        // BML OBJECT
        // ============================================================

        private sealed class BmlObject
        {
            public string Name { get; set; }

            public string Value { get; set; }

            public Dictionary<string, string> Values { get; }

            public List<Tuple<string, BmlObject>> SubObjects { get; }

            public BmlObject()
            {
                Name =
                    string.Empty;

                Value =
                    string.Empty;

                Values =
                    new Dictionary<string, string>();

                SubObjects =
                    new List<Tuple<string, BmlObject>>();
            }

            public BmlObject(
                InPacket p)
                : this()
            {
                Name =
                    p.ReadString();

                Value =
                    p.ReadString();

                int valueCount =
                    p.ReadInt();

                if (valueCount < 0 ||
                    valueCount > 100000)
                {
                    throw new InvalidDataException(
                        $"Invalid BML value count: {valueCount}");
                }

                for (int i = 0;
                     i < valueCount;
                     i++)
                {
                    string key =
                        p.ReadString();

                    string value =
                        p.ReadString();

                    Values[key] =
                        value;
                }

                int objectCount =
                    p.ReadInt();

                if (objectCount < 0 ||
                    objectCount > 100000)
                {
                    throw new InvalidDataException(
                        $"Invalid BML object count: {objectCount}");
                }

                for (int i = 0;
                     i < objectCount;
                     i++)
                {
                    BmlObject obj =
                        new BmlObject(p);

                    SubObjects.Add(
                        new Tuple<string, BmlObject>(
                            obj.Name,
                            obj));
                }
            }

            public void Save(
                OutPacket p)
            {
                p.WriteString(
                    Name);

                p.WriteString(
                    Value);

                p.WriteInt(
                    Values.Count);

                foreach (
                    KeyValuePair<string, string> pair
                    in Values)
                {
                    p.WriteString(
                        pair.Key);

                    p.WriteString(
                        pair.Value);
                }

                p.WriteInt(
                    SubObjects.Count);

                foreach (
                    Tuple<string, BmlObject> sub
                    in SubObjects)
                {
                    sub.Item2.Save(p);
                }
            }

            public BmlObject? GetObject(
                string name)
            {
                foreach (
                    Tuple<string, BmlObject> obj
                    in SubObjects)
                {
                    if (obj.Item1 ==
                        name)
                    {
                        return obj.Item2;
                    }
                }

                return null;
            }

            public string GetString(
                string key,
                string def = "")
            {
                if (Values.TryGetValue(
                        key,
                        out string? value))
                {
                    return value;
                }

                return def;
            }

            public bool GetBool(
                string key,
                bool def = false)
            {
                string value =
                    GetString(
                        key,
                        def ? "1" : "0");

                value =
                    value.ToLowerInvariant();

                if (value == "1" ||
                    value == "true")
                {
                    return true;
                }

                if (value == "0" ||
                    value == "false")
                {
                    return false;
                }

                return def;
            }

            public byte GetByte(
                string key,
                byte def = 0)
            {
                return byte.Parse(
                    GetString(
                        key,
                        def.ToString()));
            }

            public short GetShort(
                string key,
                short def = 0)
            {
                return short.Parse(
                    GetString(
                        key,
                        def.ToString()));
            }

            public ushort GetUShort(
                string key,
                ushort def = 0)
            {
                return ushort.Parse(
                    GetString(
                        key,
                        def.ToString()));
            }

            public int GetInt(
                string key,
                int def = 0)
            {
                return int.Parse(
                    GetString(
                        key,
                        def.ToString()));
            }

            public uint GetUInt(
                string key,
                uint def = 0)
            {
                return uint.Parse(
                    GetString(
                        key,
                        def.ToString()));
            }

            public float GetFloat(
                string key,
                float def = 0f)
            {
                return float.Parse(
                    GetString(
                        key,
                        def.ToString()));
            }

            public void SetKeyValuePair(
                string key,
                string value)
            {
                Values[key] =
                    value;
            }

            public void SetBool(
                string key,
                bool value)
            {
                SetKeyValuePair(
                    key,
                    value ? "1" : "0");
            }
        }

        // ============================================================
        // PACKET BASE
        // ============================================================

        private abstract class PacketBase :
            IDisposable
        {
            public abstract int Length { get; }

            public abstract int Position { get; set; }

            public abstract byte[] ToArray();

            public virtual void Dispose()
            {
            }

            public override string ToString()
            {
                return Convert.ToHexString(
                    ToArray());
            }
        }

        // ============================================================
        // IN PACKET
        // ============================================================

        private sealed class InPacket :
            PacketBase
        {
            private readonly byte[] buffer;

            private int index;

            public int Available =>
                buffer.Length -
                index;

            public override int Length =>
                buffer.Length;

            public override int Position
            {
                get
                {
                    return index;
                }

                set
                {
                    if (value < 0 ||
                        value > buffer.Length)
                    {
                        throw new ArgumentOutOfRangeException(
                            nameof(value));
                    }

                    index =
                        value;
                }
            }

            public InPacket(
                byte[] data)
            {
                buffer =
                    data ??
                    throw new ArgumentNullException(
                        nameof(data));

                index = 0;
            }

            private void CheckLength(
                int length)
            {
                if (length < 0 ||
                    index > buffer.Length - length)
                {
                    throw new InvalidDataException(
                        "Not enough data in packet.");
                }
            }

            public bool ReadBool()
            {
                return ReadByte() == 1;
            }

            public byte ReadByte()
            {
                CheckLength(1);

                return buffer[index++];
            }

            public sbyte ReadSByte()
            {
                return unchecked(
                    (sbyte)ReadByte());
            }

            public byte[] ReadBytes(
                int count)
            {
                CheckLength(count);

                byte[] result =
                    new byte[count];

                Buffer.BlockCopy(
                    buffer,
                    index,
                    result,
                    0,
                    count);

                index +=
                    count;

                return result;
            }

            public short ReadShort()
            {
                CheckLength(2);

                short value =
                    BitConverter.ToInt16(
                        buffer,
                        index);

                index += 2;

                return value;
            }

            public ushort ReadUShort()
            {
                return unchecked(
                    (ushort)ReadShort());
            }

            public int ReadInt()
            {
                CheckLength(4);

                int value =
                    BitConverter.ToInt32(
                        buffer,
                        index);

                index += 4;

                return value;
            }

            public uint ReadUInt()
            {
                return unchecked(
                    (uint)ReadInt());
            }

            public long ReadLong()
            {
                CheckLength(8);

                long value =
                    BitConverter.ToInt64(
                        buffer,
                        index);

                index += 8;

                return value;
            }

            public ulong ReadULong()
            {
                return unchecked(
                    (ulong)ReadLong());
            }

            public float ReadFloat()
            {
                CheckLength(4);

                float value =
                    BitConverter.ToSingle(
                        buffer,
                        index);

                index += 4;

                return value;
            }

            public string ReadString(
                bool ascii = false)
            {
                int length =
                    ReadInt();

                if (length < 0)
                {
                    throw new InvalidDataException(
                        "Negative string length.");
                }

                int byteLength =
                    ascii
                        ? length
                        : checked(length * 2);

                CheckLength(
                    byteLength);

                byte[] data =
                    ReadBytes(
                        byteLength);

                return ascii
                    ? Encoding.ASCII.GetString(data)
                    : Encoding.Unicode.GetString(data);
            }

            public byte[] GetRemainingBytes()
            {
                if (Available <= 0)
                    return Array.Empty<byte>();

                return ReadBytes(
                    Available);
            }

            public string GetRemainingHex()
            {
                return Convert.ToHexString(
                    buffer,
                    index,
                    Available);
            }

            public void Skip(
                int count)
            {
                CheckLength(count);

                index +=
                    count;
            }

            public override byte[] ToArray()
            {
                byte[] result =
                    new byte[buffer.Length];

                Buffer.BlockCopy(
                    buffer,
                    0,
                    result,
                    0,
                    buffer.Length);

                return result;
            }
        }

        // ============================================================
        // OUT PACKET
        // ============================================================

        private sealed class OutPacket :
            PacketBase
        {
            private MemoryStream stream;

            private bool disposed;

            public override int Length =>
                stream == null
                    ? 0
                    : checked(
                        (int)stream.Position);

            public override int Position
            {
                get
                {
                    return stream == null
                        ? 0
                        : checked(
                            (int)stream.Position);
                }

                set
                {
                    ThrowIfDisposed();

                    if (value < 0)
                    {
                        throw new ArgumentOutOfRangeException(
                            nameof(value));
                    }

                    stream.Position =
                        value;
                }
            }

            public OutPacket(
                int size = 64)
            {
                stream =
                    new MemoryStream(size);
            }

            private void ThrowIfDisposed()
            {
                if (disposed ||
                    stream == null)
                {
                    throw new ObjectDisposedException(
                        nameof(OutPacket));
                }
            }

            private void Append(
                long value,
                int count)
            {
                ThrowIfDisposed();

                for (int i = 0;
                     i < count;
                     i++)
                {
                    stream.WriteByte(
                        unchecked(
                            (byte)value));

                    value >>= 8;
                }
            }

            public void WriteBool(
                bool value)
            {
                WriteByte(
                    value
                        ? (byte)1
                        : (byte)0);
            }

            public void WriteByte(
                byte value = 0)
            {
                ThrowIfDisposed();

                stream.WriteByte(
                    value);
            }

            public void WriteBytes(
                params byte[] value)
            {
                ThrowIfDisposed();

                if (value == null)
                {
                    throw new ArgumentNullException(
                        nameof(value));
                }

                stream.Write(
                    value,
                    0,
                    value.Length);
            }

            public void WriteShort(
                short value = 0)
            {
                Append(
                    value,
                    2);
            }

            public void WriteUShort(
                ushort value = 0)
            {
                Append(
                    value,
                    2);
            }

            public void WriteInt(
                int value = 0)
            {
                Append(
                    value,
                    4);
            }

            public void WriteUInt(
                uint value = 0)
            {
                Append(
                    value,
                    4);
            }

            public void WriteLong(
                long value = 0)
            {
                Append(
                    value,
                    8);
            }

            public void WriteULong(
                ulong value = 0)
            {
                Append(
                    unchecked(
                        (long)value),
                    8);
            }

            public void WriteString(
                string value)
            {
                if (value == null)
                {
                    throw new ArgumentNullException(
                        nameof(value));
                }

                WriteInt(
                    value.Length);

                byte[] data =
                    Encoding.Unicode.GetBytes(
                        value);

                WriteBytes(
                    data);
            }

            public void WriteEndPoint(
                IPAddress ip,
                ushort port)
            {
                if (ip == null)
                {
                    throw new ArgumentNullException(
                        nameof(ip));
                }

                byte[] address =
                    ip.GetAddressBytes();

                if (address.Length != 4)
                {
                    throw new NotSupportedException(
                        "Only IPv4 endpoints are supported.");
                }

                WriteBytes(
                    address);

                WriteUShort(
                    port);
            }

            public override byte[] ToArray()
            {
                ThrowIfDisposed();

                return stream.ToArray();
            }

            public override void Dispose()
            {
                if (disposed)
                    return;

                disposed =
                    true;

                stream.Dispose();

                stream = null!;
            }
        }

        // ============================================================
        // KR CRYPTO
        // ============================================================

        private static class KRCrypto
        {
            public static byte[] ApplyCrypto(
                byte[] input,
                uint key)
            {
                if (input == null)
                {
                    throw new ArgumentNullException(
                        nameof(input));
                }

                byte[] result =
                    new byte[input.Length];

                Buffer.BlockCopy(
                    input,
                    0,
                    result,
                    0,
                    input.Length);

                uint[] keys =
                    new uint[17];

                byte[] tailKey =
                    new byte[68];

                keys[0] =
                    key ^
                    2222193601u;

                for (int i = 1;
                     i < 16;
                     i++)
                {
                    keys[i] =
                        keys[i - 1] -
                        2072773695u;
                }

                // Keep the original implementation unchanged.
                for (int i = 0;
                     i <= 16;
                     i++)
                {
                    Buffer.BlockCopy(
                        BitConverter.GetBytes(
                            keys[i]),
                        0,
                        tailKey,
                        i * 4,
                        4);
                }

                int offset = 0;

                // 64-byte block
                for (;
                     offset + 64 <= result.Length;
                     offset += 64)
                {
                    for (int j = 0;
                         j < 16;
                         j++)
                    {
                        int position =
                            offset +
                            (4 * j);

                        uint value =
                            BitConverter.ToUInt32(
                                result,
                                position);

                        value ^=
                            keys[j];

                        byte[] bytes =
                            BitConverter.GetBytes(
                                value);

                        Buffer.BlockCopy(
                            bytes,
                            0,
                            result,
                            position,
                            4);
                    }
                }

                // Tail
                for (int i = offset;
                     i < result.Length;
                     i++)
                {
                    result[i] =
                        (byte)(
                            result[i] ^
                            tailKey[i - offset]);
                }

                return result;
            }
        }

        // ============================================================
        // ADLER32
        // ============================================================

        private static class Adler32Helper
        {
            private const uint MOD_ADLER =
                65521u;

            // ========================================================
            // KartRider Adler32
            //
            // IMPORTANT:
            // If initial is 0, A also starts at 0.
            // This differs from standard Adler-32, which initializes A to 1.
            // ========================================================

            public static uint GenerateAdler32(
                byte[] data,
                uint initial = 0)
            {
                if (data == null)
                    return 1;

                uint b =
                    initial >> 16;

                uint a =
                    (ushort)initial;

                if (data.Length == 0)
                {
                    return
                        a |
                        (b << 16);
                }

                for (int i = 0;
                     i < data.Length;
                     i++)
                {
                    a +=
                        data[i];

                    b +=
                        a;

                    // Same meaning as the original implementation.
                    // Because the values are calculated in sufficiently small chunks,
                    // apply the modulo here.
                    if (a >= MOD_ADLER)
                    {
                        a -=
                            MOD_ADLER;
                    }

                    if (b >= MOD_ADLER)
                    {
                        b %=
                            MOD_ADLER;
                    }
                }

                return
                    a |
                    (b << 16);
            }

            // ========================================================
            // ZLib Adler32
            //
            // Zlib standard:
            // A = 1
            // B = 0
            // ========================================================

            public static uint GenerateSimpleAdler32(
                byte[] data)
            {
                if (data == null ||
                    data.Length == 0)
                {
                    return 1;
                }

                uint a = 1;
                uint b = 0;

                for (int i = 0;
                     i < data.Length;
                     i++)
                {
                    a =
                        (a + data[i]) %
                        MOD_ADLER;

                    b =
                        (b + a) %
                        MOD_ADLER;
                }

                return
                    (b << 16) |
                    a;
            }
        }

        // ============================================================
        // KR ENCODED BLOCK
        // ============================================================

        private static class KREncodedBlock
        {
            public enum EncodeFlag : byte
            {
                ZLib = 1,
                KartCrypto = 2
            }

            // ========================================================
            // DECODE
            // ========================================================

            public static byte[] Decode(
                byte[] inputBytes)
            {
                if (inputBytes == null)
                {
                    throw new ArgumentNullException(
                        nameof(inputBytes));
                }

                using InPacket packet =
                    new InPacket(
                        inputBytes);

                if (packet.Available == 0)
                    return inputBytes;

                byte marker =
                    packet.ReadByte();

                // Original:
                // if (ReadByte() == 83) ...
                // Otherwise, return the original data unchanged.
                if (marker != 83)
                {
                    return inputBytes;
                }

                EncodeFlag flag =
                    (EncodeFlag)
                    packet.ReadByte();

                // Final KartRider checksum
                uint checksum =
                    packet.ReadUInt();

                uint cryptoKey;

                if ((flag &
                     EncodeFlag.KartCrypto) != 0)
                {
                    cryptoKey =
                        packet.ReadUInt();
                }
                else
                {
                    cryptoKey =
                        0;
                }

                uint originalLength;

                if ((flag &
                     EncodeFlag.ZLib) != 0)
                {
                    originalLength =
                        packet.ReadUInt();
                }
                else
                {
                    originalLength =
                        0;
                }

                byte[] data =
                    packet.ReadBytes(
                        packet.Available);

                System.Diagnostics.Debug.WriteLine(
                    $"[PIN] KREncodedBlock marker=0x{marker:X2}");

                System.Diagnostics.Debug.WriteLine(
                    $"[PIN] KREncodedBlock flag=0x{(byte)flag:X2}");

                System.Diagnostics.Debug.WriteLine(
                    $"[PIN] KREncodedBlock checksum=0x{checksum:X8}");

                System.Diagnostics.Debug.WriteLine(
                    $"[PIN] KREncodedBlock cryptoKey=0x{cryptoKey:X8}");

                System.Diagnostics.Debug.WriteLine(
                    $"[PIN] KREncodedBlock originalLength={originalLength}");

                // ====================================================
                // KartCrypto
                // ====================================================

                if ((flag &
                     EncodeFlag.KartCrypto) != 0)
                {
                    data =
                        KRCrypto.ApplyCrypto(
                            data,
                            cryptoKey);
                }

                // ====================================================
                // ZLib
                // ====================================================

                if ((flag &
                     EncodeFlag.ZLib) != 0)
                {
                    if (data.Length < 6)
                    {
                        throw new InvalidDataException(
                            "Invalid ZLib data length.");
                    }

                    if (data[0] != 120 ||
                        data[1] != 218)
                    {
                        throw new InvalidDataException(
                            "Invalid magic header! (zlib)");
                    }

                    // Last 4 bytes:
                    // ZLib Adler32
                    int storedAdler =
                        BitConverter.ToInt32(
                            data,
                            data.Length - 4);

                    // Original PIN_Editor method:
                    // data[2..end]
                    // pass it to DeflateStream.
                    byte[] compressed =
                        new byte[
                            data.Length - 2];

                    Buffer.BlockCopy(
                        data,
                        2,
                        compressed,
                        0,
                        compressed.Length);

                    using MemoryStream compressedStream =
                        new MemoryStream(
                            compressed);

                    using DeflateStream deflate =
                        new DeflateStream(
                            compressedStream,
                            CompressionMode.Decompress);

                    using MemoryStream output =
                        new MemoryStream();

                    deflate.CopyTo(
                        output);

                    data =
                        output.ToArray();

                    // =================================================
                    // Length
                    // =================================================

                    if ((uint)data.Length !=
                        originalLength)
                    {
                        throw new InvalidDataException(
                            $"Length was not equal. " +
                            $"Expected={originalLength}, " +
                            $"Actual={data.Length}");
                    }

                    // =================================================
                    // ZLib checksum
                    // =================================================

                    uint calculatedZlib =
                        Adler32Helper
                            .GenerateSimpleAdler32(
                                data);

                    uint expectedZlib =
                        unchecked(
                            (uint)
                            IPAddress
                                .HostToNetworkOrder(
                                    storedAdler));

                    System.Diagnostics.Debug.WriteLine(
                        $"[PIN] ZLib checksum stored=0x{expectedZlib:X8}");

                    System.Diagnostics.Debug.WriteLine(
                        $"[PIN] ZLib checksum calculated=0x{calculatedZlib:X8}");

                    if (calculatedZlib !=
                        expectedZlib)
                    {
                        throw new InvalidDataException(
                            $"Invalid checksum! " +
                            $"Expected=0x{expectedZlib:X8}, " +
                            $"Calculated=0x{calculatedZlib:X8}");
                    }
                }

                // ====================================================
                // Final KartRider checksum
                // ====================================================

                uint calculatedChecksum =
                    Adler32Helper.GenerateAdler32(
                        data,
                        0);

                System.Diagnostics.Debug.WriteLine(
                    $"[PIN] Final data length={data.Length}");

                System.Diagnostics.Debug.WriteLine(
                    $"[PIN] Stored checksum=0x{checksum:X8}");

                System.Diagnostics.Debug.WriteLine(
                    $"[PIN] Calculated checksum=0x{calculatedChecksum:X8}");

                if (calculatedChecksum !=
                    checksum)
                {
                    throw new InvalidDataException(
                        $"Checksums didn't match. " +
                        $"Expected=0x{checksum:X8}, " +
                        $"Calculated=0x{calculatedChecksum:X8}, " +
                        $"Length={data.Length}");
                }

                // Original final length check
                if ((flag &
                     EncodeFlag.ZLib) != 0 &&
                    (uint)data.Length !=
                    originalLength)
                {
                    throw new InvalidDataException(
                        "Lengths did not match.");
                }

                return data;
            }

            // ========================================================
            // ENCODE
            // ========================================================

            public static byte[] Encode(
                byte[] inputBytes,
                EncodeFlag flag,
                uint? kartCryptoKey)
            {
                if (inputBytes == null)
                {
                    throw new ArgumentNullException(
                        nameof(inputBytes));
                }

                using OutPacket packet =
                    new OutPacket(64);

                packet.WriteByte(
                    83);

                packet.WriteByte(
                    (byte)flag);

                // KartRider checksum
                packet.WriteUInt(
                    Adler32Helper.GenerateAdler32(
                        inputBytes,
                        0));

                // ====================================================
                // Crypto key
                // ====================================================

                if ((flag &
                     EncodeFlag.KartCrypto) != 0)
                {
                    if (!kartCryptoKey.HasValue)
                    {
                        kartCryptoKey =
                            Adler32Helper.GenerateAdler32(
                                inputBytes,
                                0);
                    }

                    packet.WriteUInt(
                        kartCryptoKey.Value);
                }

                // ====================================================
                // ZLib
                // ====================================================

                if ((flag &
                     EncodeFlag.ZLib) != 0)
                {
                    using MemoryStream zlib =
                        new MemoryStream();

                    using BinaryWriter writer =
                        new BinaryWriter(
                            zlib);

                    using MemoryStream compressed =
                        new MemoryStream();

                    using (
                        DeflateStream deflate =
                            new DeflateStream(
                                compressed,
                                CompressionMode.Compress))
                    {
                        using MemoryStream source =
                            new MemoryStream(
                                inputBytes);

                        source.CopyTo(
                            deflate);
                    }

                    // Original length
                    packet.WriteInt(
                        inputBytes.Length);

                    // 78 DA
                    writer.Write(
                        new byte[]
                        {
                            120,
                            218
                        });

                    byte[] compressedBytes =
                        compressed.ToArray();

                    writer.Write(
                        compressedBytes);

                    // ZLib Adler32
                    int adlerNetwork =
                        IPAddress.HostToNetworkOrder(
                            unchecked(
                                (int)
                                Adler32Helper
                                    .GenerateSimpleAdler32(
                                        inputBytes)));

                    writer.Write(
                        adlerNetwork);

                    writer.Flush();

                    inputBytes =
                        zlib.ToArray();
                }

                // ====================================================
                // KartCrypto
                // ====================================================

                if ((flag &
                     EncodeFlag.KartCrypto) != 0)
                {
                    inputBytes =
                        KRCrypto.ApplyCrypto(
                            inputBytes,
                            kartCryptoKey!.Value);
                }

                packet.WriteBytes(
                    inputBytes);

                return packet.ToArray();
            }
        }
    }
}

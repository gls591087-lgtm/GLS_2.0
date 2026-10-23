using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KartRider.IO.Packet;
using KartRider.IO;
namespace KartRider.Net;

public static class HandlerStore
{
    private sealed class HandlerEntry
    {
        public uint Rtti { get; }

        public MethodInfo Method { get; }

        public HandlerEntry(
            uint rtti,
            MethodInfo method)
        {
            Rtti = rtti;
            Method = method;
        }

        public void Invoke(
            KartClient client,
            InPacket packet)
        {
            Method.Invoke(
                null,
                new object[]
                {
                    client,
                    packet
                });
        }
    }

    private static readonly Dictionary<uint, HandlerEntry> _handlers =
        new();

    private static readonly object _sync =
        new();

    private static bool _initialized;

    // ============================================================
    // COUNT
    // ============================================================

    public static int Count
    {
        get
        {
            lock (_sync)
            {
                return _handlers.Count;
            }
        }
    }

    // ============================================================
    // INITIALIZE
    // ============================================================

    public static void Initialize()
    {
        lock (_sync)
        {
            if (_initialized)
                return;

            _handlers.Clear();

            Assembly[] assemblies =
                AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Where(a => !a.IsDynamic)
                    .ToArray();

            Console.WriteLine(
                $"[HandlerStore] Scanning {assemblies.Length} loaded assemblies...");

            foreach (Assembly assembly in assemblies)
            {
                try
                {
                    Console.WriteLine(
                        $"[HandlerStore] Assembly: {assembly.GetName().Name}");

                    ScanAssembly(assembly);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"[HandlerStore] Assembly scan failed: " +
                        $"{assembly.FullName}");

                    Console.WriteLine(ex);
                }
            }

            _initialized = true;

            Console.WriteLine(
                $"[HandlerStore] Cache complete. " +
                $"Registered handlers: {_handlers.Count}");
        }
    }

    // ============================================================
    // SCAN ASSEMBLY
    // ============================================================

    private static void ScanAssembly(
        Assembly assembly)
    {
        Type[] types;

        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types =
                ex.Types
                    .Where(t => t != null)
                    .Cast<Type>()
                    .ToArray();

            Console.WriteLine(
                $"[HandlerStore] Partial type load: " +
                $"{assembly.GetName().Name}");

            foreach (Exception? loaderException in
                     ex.LoaderExceptions)
            {
                if (loaderException != null)
                {
                    Console.WriteLine(
                        $"[HandlerStore] Loader: " +
                        $"{loaderException.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[HandlerStore] GetTypes failed: " +
                $"{assembly.FullName}");

            Console.WriteLine(ex);

            return;
        }

        foreach (Type type in types)
        {
            ScanType(type);
        }
    }

    // ============================================================
    // SCAN TYPE
    // ============================================================

    private static void ScanType(
        Type type)
    {
        MethodInfo[] methods;

        try
        {
            methods =
                type.GetMethods(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Static |
                    BindingFlags.DeclaredOnly);
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[HandlerStore] Type scan failed: " +
                $"{type.FullName}");

            Console.WriteLine(ex);

            return;
        }

        foreach (MethodInfo method in methods)
        {
            PacketHandlerAttribute[] attributes;

            try
            {
                attributes =
                    method.GetCustomAttributes<PacketHandlerAttribute>()
                        .ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[HandlerStore] Attribute read failed: " +
                    $"{type.FullName}.{method.Name}");

                Console.WriteLine(ex);

                continue;
            }

            foreach (PacketHandlerAttribute attribute in attributes)
            {
                Register(
                    attribute.Rtti,
                    method);
            }
        }
    }

    // ============================================================
    // REGISTER
    // ============================================================

    private static void Register(
        uint rtti,
        MethodInfo method)
    {
        lock (_sync)
        {
            ParameterInfo[] parameters;

            try
            {
                parameters =
                    method.GetParameters();
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[HandlerStore] Parameter inspection failed: " +
                    $"{method.DeclaringType?.FullName}." +
                    $"{method.Name}");

                Console.WriteLine(ex);

                return;
            }

            // ========================================================
            // METHOD SIGNATURE
            // ========================================================

            if (parameters.Length != 2)
            {
                Console.WriteLine(
                    $"[HandlerStore] INVALID PARAM COUNT: " +
                    $"{method.DeclaringType?.FullName}." +
                    $"{method.Name}");

                return;
            }

            if (!typeof(KartClient).IsAssignableFrom(
                    parameters[0].ParameterType))
            {
                Console.WriteLine(
                    $"[HandlerStore] INVALID CLIENT PARAM: " +
                    $"{method.DeclaringType?.FullName}." +
                    $"{method.Name} " +
                    $"Type={parameters[0].ParameterType.FullName}");

                return;
            }

            if (!typeof(InPacket).IsAssignableFrom(
                    parameters[1].ParameterType))
            {
                Console.WriteLine(
                    $"[HandlerStore] INVALID PACKET PARAM: " +
                    $"{method.DeclaringType?.FullName}." +
                    $"{method.Name} " +
                    $"Type={parameters[1].ParameterType.FullName}");

                return;
            }

            if (method.ReturnType != typeof(void))
            {
                Console.WriteLine(
                    $"[HandlerStore] INVALID RETURN TYPE: " +
                    $"{method.DeclaringType?.FullName}." +
                    $"{method.Name}");

                return;
            }

            // ========================================================
            // DUPLICATE
            // ========================================================

            if (_handlers.TryGetValue(
                    rtti,
                    out HandlerEntry? old))
            {
                string oldName =
                    $"{old.Method.DeclaringType?.FullName}." +
                    $"{old.Method.Name}";

                string newName =
                    $"{method.DeclaringType?.FullName}." +
                    $"{method.Name}";

                Console.WriteLine(
                    $"[HandlerStore] DUPLICATE RTTI DETECTED");

                Console.WriteLine(
                    $"[HandlerStore] RTTI=0x{rtti:X8}");

                Console.WriteLine(
                    $"[HandlerStore] EXISTING={oldName}");

                Console.WriteLine(
                    $"[HandlerStore] NEW={newName}");

                // Keep the existing handler.
                return;
            }

            // ========================================================
            // REGISTER
            // ========================================================

            HandlerEntry entry =
                new HandlerEntry(
                    rtti,
                    method);

            _handlers.Add(
                rtti,
                entry);

            Console.WriteLine(
                $"[HandlerStore] REGISTERED: " +
                $"{method.DeclaringType?.FullName}." +
                $"{method.Name} " +
                $"RTTI=0x{rtti:X8}");
        }
    }
    // ============================================================
    // TRY GET
    // ============================================================

    public static bool TryGet(
        uint rtti,
        out MethodInfo? method)
    {
        lock (_sync)
        {
            if (_handlers.TryGetValue(
                    rtti,
                    out HandlerEntry? entry))
            {
                method = entry.Method;
                return true;
            }
        }

        method = null;
        return false;
    }

    // ============================================================
    // DISPATCH
    // ============================================================

    public static void Dispatch(
      KartClient client,
      InPacket packet)
    {
        if (client == null)
            return;

        if (packet == null)
            return;

        try
        {
            // ========================================================
            // RTTI CHECK
            // ========================================================

            if (packet.Available < 4)
            {
                Console.WriteLine(
                    "[HandlerStore] PACKET TOO SMALL: " +
                    $"Length={packet.Available}");

                return;
            }

            uint rtti =
                packet.PeekUInt();

            string rttiName;

            if (Enum.IsDefined(
                    typeof(RTTIValues),
                    rtti))
            {
                rttiName =
                    ((RTTIValues)rtti).ToString();
            }
            else
            {
                rttiName =
                    "UNKNOWN";
            }

            Console.WriteLine(
                $"[HandlerStore] PACKET " +
                $"RTTI=0x{rtti:X8} " +
                $"Name={rttiName} " +
                $"Length={packet.Available}");

            // ========================================================
            // HEX
            // ========================================================

            try
            {
                Console.WriteLine(
                    "[HandlerStore] HEX=" +
                    packet.GetRemainingHex());
            }
            catch
            {
            }

            // ========================================================
            // FIND HANDLER
            // ========================================================

            HandlerEntry? handler;

            lock (_sync)
            {
                _handlers.TryGetValue(
                    rtti,
                    out handler);
            }

            if (handler == null)
            {
                // File-based packet diagnostics are disabled for release builds.
                Console.WriteLine(
                    $"[HandlerStore] NO HANDLER " +
                    $"RTTI=0x{rtti:X8} " +
                    $"Name={rttiName}");

                return;
            }

            Console.WriteLine(
                $"[HandlerStore] HANDLER=" +
                $"{handler.Method.DeclaringType?.FullName}." +
                $"{handler.Method.Name}");

            // ========================================================
            // CONSUME RTTI
            // ========================================================

            packet.ReadUInt();

            Console.WriteLine(
                $"[HandlerStore] RTTI consumed: " +
                $"0x{rtti:X8}");

            // ========================================================
            // INVOKE
            // ========================================================

            handler.Invoke(
                client,
                packet);

            Console.WriteLine(
                $"[HandlerStore] Dispatch OK " +
                $"RTTI=0x{rtti:X8}");
        }
        catch (TargetInvocationException ex)
        {
            Exception actual =
                ex.InnerException ?? ex;

            Console.WriteLine(
                $"[HandlerStore] Handler exception: " +
                $"{actual}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[HandlerStore] Dispatch ERROR: {ex}");
        }
    }
    public static void DispatchUdp(
        KartClient client,
        InPacket packet)
    {
        if (client == null || packet == null)
            return;

        try
        {
            if (packet.Available < 12)
            {
                Log.Warn(
                    "[HandlerStore] UDP packet too small. Available={0}",
                    packet.Available);

                return;
            }

            // ========================================================
            // Original UDP structure
            //
            // 00~03 : AccountId
            // 04~07 : Value / Sequence
            // 08~0B : RTTI
            // 0C~   : Handler Payload
            // ========================================================

            uint rtti = packet.PeekUIntAt(8);

            string rttiName =
                Enum.IsDefined(
                    typeof(RTTIValues),
                    rtti)
                    ? ((RTTIValues)rtti).ToString()
                    : "UNKNOWN";

            Log.Inform(
                "[HandlerStore] PACKET RTTI=0x{0:X8} Name={1} Length={2}",
                rtti,
                rttiName,
                packet.Available);

            Log.Inform(
                "[HandlerStore] HEX={0}",
                packet.GetRemainingHex());

            HandlerEntry? handler;

            lock (_sync)
            {
                _handlers.TryGetValue(
                    rtti,
                    out handler);
            }

            if (handler == null)
            {
                // File-based packet diagnostics are disabled for release builds.
                Log.Warn(
                    "[HandlerStore] NO HANDLER RTTI=0x{0:X8} Name={1}",
                    rtti,
                    rttiName);

                return;
            }

            Log.Inform(
                "[HandlerStore] HANDLER={0}",
                $"{handler.Method.DeclaringType?.FullName}.{handler.Method.Name}");

            // --------------------------------------------------------
            // Match the original server packet format.
            // Consume accountId + val2 + RTTI.
            // --------------------------------------------------------

            int accountId = packet.ReadInt();
            int val2 = packet.ReadInt();
            packet.ReadUInt();

            Log.Inform(
                "[HandlerStore] UDP HEADER: AccountId={0}, Value={1}",
                accountId,
                val2);

            Log.Inform(
                "[HandlerStore] RTTI consumed: 0x{0:X8}",
                rtti);

            handler.Invoke(
                client,
                packet);

            Log.Inform(
                "[HandlerStore] Dispatch OK RTTI=0x{0:X8}",
                rtti);
        }
        catch (TargetInvocationException ex)
        {
            Exception actual =
                ex.InnerException ?? ex;

            Log.Error(
                "[HandlerStore] Handler exception: {0}",
                actual);
        }
        catch (Exception ex)
        {
            Log.Error(
                "[HandlerStore] Dispatch failed:\n{0}",
                ex);
        }
    }

    // DUMP
    // ============================================================

    public static void Dump()
    {
        lock (_sync)
        {
            Console.WriteLine(
                $"[HandlerStore] Registered handlers: " +
                $"{_handlers.Count}");

            foreach (HandlerEntry handler in
                     _handlers.Values.OrderBy(
                         x => x.Rtti))
            {
                Console.WriteLine(
                    $"0x{handler.Rtti:X8} -> " +
                    $"{handler.Method.DeclaringType?.FullName}." +
                    $"{handler.Method.Name}");
            }
        }
    }

    // ============================================================
    // CLEAR
    // ============================================================

    public static void Clear()
    {
        lock (_sync)
        {
            _handlers.Clear();

            _initialized = false;
        }
    }
}
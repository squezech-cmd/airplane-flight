// LobbyCodes.cs — 6-символьные коды лобби.
// Код кодирует приватный IPv4-адрес хоста (LAN) в 6 символов однозначного алфавита.
// Для публичных/проброшенных адресов (порт 7777) на экране дополнительно показывается «IP:порт» —
// строка подключения также принимается полем ввода.

using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

/// <summary>Кодирование кода лобби.</summary>
public static class LobbyCodes
{
    /// <summary>Порт лобби/игры (UDP+TCP, Unity Transport).</summary>
    public const ushort DefaultPort = 7777;

    /// <summary>Однозначный алфавит без I, O, 0, 1 (32 символа = 5 бит на символ).</summary>
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    /// <summary>IPv4 хоста в этой сети (для отображения кода), либо null.</summary>
    public static string GetLocalIpv4()
    {
        try
        {
            // Предпочитаем адрес, которым машина ходит наружу.
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Connect("8.8.8.8", 65530);
                if (socket.LocalEndPoint is IPEndPoint ep) return ep.Address.ToString();
            }
        }
        catch { /* нет интернета — не страшно */ }

        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        var ip = addr.Address.ToString();
                        if (ip.StartsWith("127.")) continue;
                        return ip;
                    }
                }
            }
        }
        catch { /* fallback ниже */ }
        return "127.0.0.1";
    }

    /// <summary>Закодировать IP в 6-символьный код (только приватные диапазоны), иначе null.</summary>
    public static string Encode(string ipv4)
    {
        if (!IPAddress.TryParse(ipv4, out var ip) || ip.AddressFamily != AddressFamily.InterNetwork)
            return null;
        var bytes = ip.GetAddressBytes();
        uint b0 = bytes[0], b1 = bytes[1], b2 = bytes[2], b3 = bytes[3];
        uint value;
        if (b0 == 192 && b1 == 168) value = (0u << 28) | (b2 << 12) | b3;                              // 16 бит
        else if (b0 == 10) value = (1u << 28) | (b1 << 16) | (b2 << 8) | b3;                          // 24 бита
        else if (b0 == 172 && b1 >= 16 && b1 <= 31) value = (2u << 28) | ((b1 - 16u) << 16) | (b2 << 8) | b3; // 20 бит
        else return null; // публичный адрес — код не строим, используем IP:порт
        return EncodeBase32(value, 6);
    }

    /// <summary>Декодировать 6-символьный код в IP.</summary>
    public static bool TryDecode(string code, out string ipv4)
    {
        ipv4 = null;
        if (string.IsNullOrEmpty(code)) return false;
        code = code.Trim().ToUpperInvariant();
        if (code.Length != 6) return false;
        if (!TryDecodeBase32(code, out uint value)) return false;

        uint cls = value >> 28;
        uint payload = value & 0x0FFFFFFFu;
        switch (cls)
        {
            case 0:
                ipv4 = $"192.168.{(payload >> 12) & 0xFFu}.{payload & 0xFFu}";
                return true;
            case 1:
                ipv4 = $"10.{(payload >> 16) & 0xFFu}.{(payload >> 8) & 0xFFu}.{payload & 0xFFu}";
                return true;
            case 2:
                ipv4 = $"172.{16 + ((payload >> 16) & 0xFu)}.{(payload >> 8) & 0xFFu}.{payload & 0xFFu}";
                return true;
            default:
                return false;
        }
    }

    /// <summary>Разобрать строку подключения: 6-символьный код ИЛИ «ip:port» / «ip».</summary>
    public static bool TryParse(string input, out string address, out ushort port)
    {
        address = null;
        port = DefaultPort;
        input = (input ?? "").Trim();
        if (input.Length == 0) return false;

        if (input.Contains('.'))
        {
            // Формат ip[:port]
            var parts = input.Split(':');
            if (!IPAddress.TryParse(parts[0], out var ip)) return false;
            address = ip.ToString();
            if (parts.Length > 1 && ushort.TryParse(parts[1], out var p)) port = p;
            return true;
        }
        return TryDecode(input, out address);
    }

    private static string EncodeBase32(uint value, int chars)
    {
        char[] result = new char[chars];
        for (int i = chars - 1; i >= 0; i--)
        {
            result[i] = Alphabet[(int)(value & 31u)];
            value >>= 5;
        }
        return new string(result);
    }

    private static bool TryDecodeBase32(string code, out uint value)
    {
        value = 0;
        foreach (char c in code)
        {
            int idx = Alphabet.IndexOf(c);
            if (idx < 0) return false;
            value = (value << 5) | (uint)idx;
        }
        return true;
    }
}

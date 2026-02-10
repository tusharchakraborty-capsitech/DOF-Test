using System.Net.NetworkInformation;
using System.Net.Sockets;
using UnityEngine;

/// <summary>
/// MULTIPLAYER - Local IP Address Finder
/// Finds device's local IPv4 address for hosting
/// Path: Assets/Scripts/Multiplayer/Network/LocalIPFinder.cs
/// 
/// HOW IT WORKS:
/// - Scans all network interfaces
/// - Finds active WiFi/Ethernet adapter
/// - Returns private IP address (192.x, 172.x, or 10.x)
/// - Fallback to 127.0.0.1 if no network found
/// 
/// USAGE:
/// string ip = LocalIPFinder.GetLocalIPv4();
/// 
/// NO SETUP NEEDED - Just call the static method
/// </summary>
public static class LocalIPFinder
{
    /// <summary>
    /// Get the local IPv4 address of this device
    /// </summary>
    /// <returns>Local IP address or 127.0.0.1 if not found</returns>
    public static string GetLocalIPv4()
    {
        try
        {
            // Iterate through all network interfaces
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                // Skip if interface is down or loopback
                if (ni.OperationalStatus != OperationalStatus.Up)
                    continue;

                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                // Get IP properties
                foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                {
                    // We want IPv4 addresses only
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        string ipAddr = ip.Address.ToString();

                        // Check for private IP ranges
                        // 192.168.x.x - Most common WiFi
                        // 10.x.x.x - Some corporate/home networks
                        // 172.16-31.x.x - Some networks
                        if (ipAddr.StartsWith("192.168.") ||
                            ipAddr.StartsWith("10.") ||
                            ipAddr.StartsWith("172.16.") ||
                            ipAddr.StartsWith("172.17.") ||
                            ipAddr.StartsWith("172.18.") ||
                            ipAddr.StartsWith("172.19.") ||
                            ipAddr.StartsWith("172.20.") ||
                            ipAddr.StartsWith("172.21.") ||
                            ipAddr.StartsWith("172.22.") ||
                            ipAddr.StartsWith("172.23.") ||
                            ipAddr.StartsWith("172.24.") ||
                            ipAddr.StartsWith("172.25.") ||
                            ipAddr.StartsWith("172.26.") ||
                            ipAddr.StartsWith("172.27.") ||
                            ipAddr.StartsWith("172.28.") ||
                            ipAddr.StartsWith("172.29.") ||
                            ipAddr.StartsWith("172.30.") ||
                            ipAddr.StartsWith("172.31."))
                        {
                            Debug.Log($"✅ Found local IP: {ipAddr} on {ni.Name}");
                            return ipAddr;
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error finding local IP: {e.Message}");
        }

        Debug.LogWarning("⚠️ No local IP found, using localhost");
        return "127.0.0.1";  // Fallback to localhost
    }

    /// <summary>
    /// Check if device has an active network connection
    /// </summary>
    public static bool HasNetworkConnection()
    {
        string ip = GetLocalIPv4();
        return ip != "127.0.0.1";
    }

    /// <summary>
    /// Get all available IPs (for debugging)
    /// </summary>
    public static void DebugPrintAllIPs()
    {
        Debug.Log("=== All Network Interfaces ===");

        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            Debug.Log($"Interface: {ni.Name}");
            Debug.Log($"  Type: {ni.NetworkInterfaceType}");
            Debug.Log($"  Status: {ni.OperationalStatus}");

            foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
            {
                if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    Debug.Log($"  IPv4: {ip.Address}");
                }
            }
        }

        Debug.Log("==============================");
    }
}
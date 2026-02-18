using System.Net.NetworkInformation;
using System.Net.Sockets;
using UnityEngine;


public static class LocalIPFinder
{
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
            Debug.LogError($"Error finding local IP: {e.Message}");
        }

        Debug.LogWarning("No local IP found, using localhost");
        return "127.0.0.1";  // Fallback to localhost
    }

    public static bool HasNetworkConnection()
    {
        string ip = GetLocalIPv4();
        return ip != "127.0.0.1";
    }

    public static void DebugPrintAllIPs()
    {
        Debug.Log("=== All Network Interfaces ===");

        foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            Debug.Log($"  Interface: {ni.Name}");
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
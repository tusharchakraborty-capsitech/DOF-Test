using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using System.Collections;

public class DiscoveryHandler : MonoBehaviour
{
    private UdpClient broadcastClient;
    private Coroutine listenRoutine;
    private const int discoveryPort = 47777;
    public string hostIP = "";

    public void StartBroadcasting(string ip)
    {
        StopBroadcasting();
        hostIP = ip;
        StartCoroutine(BroadcastLoop());
        Debug.Log($"📡 Broadcasting on port {discoveryPort}");
    }

    private IEnumerator BroadcastLoop()
    {
        // Use port 0 for the sender. This tells the OS to pick ANY random available 
        // port for the OUTGOING traffic, so it doesn't collide with the Listener port.
        broadcastClient = new UdpClient(0);
        broadcastClient.EnableBroadcast = true;
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, discoveryPort);

        while (true)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes("GAME_HOST_AT:" + hostIP);
                broadcastClient.Send(data, data.Length, endPoint);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Broadcast notice: {e.Message}");
            }
            yield return new WaitForSeconds(2f);
        }
    }

    public void StartListening(Action<string> onGameFound)
    {
        if (listenRoutine != null) StopCoroutine(listenRoutine);
        listenRoutine = StartCoroutine(ListenLoop(onGameFound));
    }

    private IEnumerator ListenLoop(Action<string> onGameFound)
    {
        UdpClient listener = null;
        try
        {
            // ADVANCED BINDING: This allows multiple instances on the same PC to share the port
            listener = new UdpClient();
            listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            listener.Client.Bind(new IPEndPoint(IPAddress.Any, discoveryPort));
            listener.Client.Blocking = false;

            Debug.Log($"Listening on port {discoveryPort}");

            while (true)
            {
                try
                {
                    if (listener.Available > 0)
                    {
                        IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                        byte[] data = listener.Receive(ref endPoint);
                        string message = Encoding.UTF8.GetString(data);

                        if (message.StartsWith("GAME_HOST_AT:"))
                        {
                            string foundIP = message.Split(':')[1];
                            onGameFound?.Invoke(foundIP);
                            // If you want to keep looking for more hosts, don't break
                            yield break;
                        }
                    }
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode != SocketError.WouldBlock)
                        Debug.LogWarning($"Socket error: {e.Message}");
                }
                yield return null;
            }
        }
        finally
        {
            if (listener != null)
            {
                listener.Close();
                Debug.Log("Listener Closed");
            }
        }
    }

    public void StopBroadcasting()
    {
        if (broadcastClient != null)
        {
            broadcastClient.Close();
            broadcastClient = null;
        }
        StopAllCoroutines();
    }

    private void OnDestroy() => StopBroadcasting();
    private void OnApplicationQuit() => StopBroadcasting();
}
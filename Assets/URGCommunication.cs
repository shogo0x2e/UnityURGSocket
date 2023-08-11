using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Threading;
using System;
using System.Text;

public class URGCommunication : MonoBehaviour
{

    private const string ipAddr = "192.168.0.10";
    private const    int port = 10940;

    private const string urgMdCmd = "MD0000108001000\n";

    private    Thread clientThread;
    private TcpClient tcpClient = new TcpClient();

    public List<long> Distances { get; private set; } = new List<long>();

    private void Start()
    {
        try
        {
            tcpClient.Connect(ipAddr, port);

            Debug.Log("TCP connection has Started");
               
            // ListenForClients()
            clientThread = new Thread(new ThreadStart(ClientCommHandler));
            clientThread.Start();

            helperWriteBytes(tcpClient.GetStream(), urgMdCmd);


            Debug.Log("Threading has Started");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Application.Quit();
        }
    }

    private void OnApplicationQuit()
    {
        if (tcpClient != null)
        {
            if (tcpClient.Connected)
            {
                NetworkStream stream = tcpClient.GetStream();
                if (stream != null)
                {
                    stream.Close();
                }
            }
            tcpClient.Close();
        }

        if (clientThread != null)
        {
            clientThread.Abort();
        }

        Debug.Log("Done");
    }

    public void ClientCommHandler()
    {
        try
        {
            using (tcpClient)
            {
                using (NetworkStream stream = tcpClient.GetStream())
                {

                    int md = 0, other = 0;

                    while (true)
                    {

                        long timeStamp = 0;
                        string receiveData = helperReadLine(stream);

                        // URG の現在のモードを取得
                        string[] splitRecvData = receiveData.Split(
                            new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries
                        );

                        if (splitRecvData[0].Substring(0, 2) == "MD")
                        {
                            md++;

                            Distances.Clear();

                            if (splitRecvData[1].StartsWith("00"))
                            {
                                // 特になし (true)
                            }
                            else if (splitRecvData[1].StartsWith("99"))
                            {
                                // タイムスタンプを取得する
                                // ::: CHECK ::: 4 って何の数？？
                                timeStamp = helperDecode(splitRecvData[2], 4);

                                //
                                // Action つかって CallBack してた、元のコード
                                //

                                // distance_data(split_command, 3, ref distances);
                                StringBuilder sb = new StringBuilder();
                                for (int i = 3; i < splitRecvData.Length; i++)
                                {
                                    sb.Append(splitRecvData[i].Substring(0, splitRecvData[i].Length - 1));
                                }

                                // return SCIP_Reader.decode_array(sb.ToString(), 3, ref distances);
                                for (int pos = 0; pos <= sb.ToString().Length - 3; pos += 3)
                                {
                                    Distances.Add(helperDecode(sb.ToString(), 3, pos));
                                }
                            }
                            else
                            {
                                // 特になし (false)
                            }
                        }
                        else
                        {
                            other++;

                            Debug.LogWarningFormat(splitRecvData[0].Substring(0, 2));
                        }

                        Debug.Log("MD: " + md + " / OTHER: " + other + " / LEN: " + Distances.Count);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private static long helperDecode(string data, int size, int offset = 0)
    {
        long value = 0;

        for (int i = 0; i < size; ++i)
        {
            value <<= 6;
            value |= (long)data[offset + i] - 0x30;
        }

        return value;
    }

    private static string helperReadLine(NetworkStream stream)
    {
        if (stream.CanRead)
        {
            StringBuilder sb = new StringBuilder();
            bool isNL = false, isNL2 = false;

            do
            {
                char buf = (char)stream.ReadByte();
                if (buf == '\n')
                {
                    if (isNL)
                    {
                        isNL2 = true;
                    }
                    else
                    {
                        isNL = true;
                    }
                }
                else
                {
                    isNL = false;
                }
                sb.Append(buf);

            } while (!isNL2);

            return sb.ToString();
        }
        else
        {
            return null;
        }
    }

    private static bool helperWriteBytes(NetworkStream stream, string data)
    {
        if (stream.CanWrite)
        {
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            stream.Write(buffer, 0, buffer.Length);

            return true;
        }
        else
        {
            return false;
        }
    }
}

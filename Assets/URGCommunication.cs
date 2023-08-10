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

    private const string urgMdCmd = "MD0000108001000";

    private    Thread clientThread;
    private TcpClient tcpClient = new TcpClient();

    private List<long> distances = new List<long>();
    private Action<List<long>> onReadMD;

    private void Start()
    {
        try
        {
            tcpClient.Connect(ipAddr, port);

            clientThread = new Thread(new ThreadStart(ClientCommHandler));
            clientThread.Start();

            helperWriteBytes(tcpClient.GetStream(), urgMdCmd);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Application.Quit();
        }
    }

    public void ClientCommHandler()
    {
        try
        {
            using (tcpClient)
            {
                using (NetworkStream stream = tcpClient.GetStream())
                {
                    while (true)
                    {
                        long timeStamp = 0;
                        string receiveData = helperReadLine(stream);

                        // URG の現在のモードを取得
                        string[] splitRecvData = receiveData.Split(
                            new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries
                        );

                        int md = 0, other = 0;

                        if (splitRecvData[0].Substring(0, 2) == "MD")
                        {
                            md++;

                            distances.Clear();

                            if (splitRecvData[1].StartsWith("00"))
                            {
                                // 特になし (true)
                            }
                            else if (splitRecvData[1].StartsWith("99"))
                            {
                                // タイムスタンプを取得する
                                // ::: CHECK ::: 4 って何の数？？
                                timeStamp = 0;
                                for (int i = 0; i < 4; i++)
                                {
                                    timeStamp <<= 6;
                                    timeStamp |= (long)splitRecvData[2][i] - 0x30;
                                }

                                if (onReadMD != null)
                                {
                                    onReadMD.Invoke(distances);
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

                        Debug.Log("MD: " + md + " / OTHER: " + other);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
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

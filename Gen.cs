using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CellularAutamata;
using UnityEngine.UI;
using Telepathy;
using System.Net.NetworkInformation;

public class Gen : MonoBehaviour
{
    // Start is called before the first frame update
    GameObject[,,] objects;
    public short lenght = 40;
    public short height = 40;
    public short width = 10;
    public string rule = "26 [0]/13/36";
    public GameObject cube;

    //public CellularAutamata3D ca;
    public CellularAutamata3D ca;
 
    public Toggle inputToggleShowHideCA, inputToggleAutoRotate, inputToggleSpea;
    public ScrollRect scroll;
    public Text txt;
    public Text volumeText;

    private int generation = 0;
    private NeuroNet n;

    private PianoOut po;

    //private Client client;
    private Server server;

    private Dictionary<int, ClientInfo> clients;
    public const byte CONST_OKAGAMGA_ID = 1;
    void Start()
    {
        clients = new Dictionary<int, ClientInfo>();
        ConnectNetwork();
        //*
        n = DNA.Wake();
        ca = n.ca;

        lenght = ca.lenght; height = ca.height; width = ca.width;
        rule = ca.rule.rule; 

        objects = new GameObject[lenght, height, width];
        for (int i = 0; i < lenght; i++)
        {
            for (int j = 0; j < height; j++)
            {
                for (int k = 0; k < width; k++)
                {
                    objects[i, j, k] = Instantiate(cube,
                                 new Vector3((i-lenght/2) * 1.1f, (j - height / 2) * 1.1f, (k - width / 2) * 1.1f),
                                 Quaternion.identity);
                    objects[i, j, k].GetComponent<MeshRenderer>().material.color = new Color(1.0f, 1.0f, 1.0f, 0.5f);
                    objects[i, j, k].SetActive(false);
                }
            }
        }
        //*/
        
    }
    void Awake()
    {
        // update even if window isn't focused, otherwise we don't receive.
        Application.runInBackground = true;

        // use Debug.Log functions for Telepathy so we can see it in the console
        Telepathy.Logger.Log = Debug.Log;
        Telepathy.Logger.LogWarning = Debug.LogWarning;
        Telepathy.Logger.LogError = Debug.LogError;
    }

    private void ConnectNetwork()
    {
        //сначала TCP
        txt.text += "\r\nLooking for free TCP port...";
        int port = FindAvailableServerPort();
        if (port != 0)
        {
            txt.text += "\r\nOK, I'M on "+port+" TCP port...";
            scroll.verticalNormalizedPosition = 0f;
        }
        else
        {
            txt.text += "\r\nFAIL to open TCP ports... A lot of my copy started here?";
            scroll.verticalNormalizedPosition = 0f;
            return;
        }

        server = new Server();
        server.Start(port);


    }

    public int FindAvailableServerPort()
    {
        List<int> ports=new List<int>(){ 54320, 54321, 54322, 54323, 54324, 54325, 54326, 54327, 54328, 54329, 54330, 54331, 54332, 54333, 54334, 54335 };

        IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
        TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

        foreach (int port in ports)
        {
            bool isAvailable = true;
            foreach (TcpConnectionInformation tcpi in tcpConnInfoArray)
            {
                if (port==tcpi.LocalEndPoint.Port || port == tcpi.RemoteEndPoint.Port)
                {
                    isAvailable = false;
                    break;
                }
            }
            if(isAvailable) return port;
        }
        return 0;
    }

    
    public void ListeningClients()
    {
        if (server.Active)
        {

            // show all new messages
            Telepathy.Message msg;
            while (server.GetNextMessage(out msg))
            {
                switch (msg.eventType)
                {
                    case Telepathy.EventType.Connected:                        
                        txt.text += "\r\nNew client connected: "+msg.connectionId;
                        break;
                    case Telepathy.EventType.Data:
                        switch (msg.data[0]<<8 | msg.data[1])
                        {
                            case 0xffff: //hello from client
                                txt.text += "\r\nClient: " + msg.connectionId+"<< Hello";
                                server.Send(msg.connectionId, new byte[3] { 0xff, 0xfe, CONST_OKAGAMGA_ID });
                                txt.text += "\r\nOkag: >> Hello";
                                string uid;
                                if (msg.data.Length > 2)
                                {
                                    byte[] uidbytes = new byte[msg.data.Length - 2];
                                    System.Array.Copy(msg.data, 2, uidbytes, 0, msg.data.Length - 2);
                                    uid = System.Text.Encoding.UTF8.GetString(uidbytes);
                                }
                                else
                                {
                                    uid = "";
                                }
                                if (!clients.ContainsKey(msg.connectionId))
                                {
                                    clients.Add(msg.connectionId, new ClientInfo(uid, server.GetClientAddress(msg.connectionId)));
                                }
                                else
                                {
                                    //уже здоровался со мной этот клиент и присылал свой uID. Что делать? Ничего
                                }
                                break;
                            case 0x0001: //goodbye from client
                                txt.text += "\r\nClient: " + msg.connectionId + "<< Goodbye";
                                server.Send(msg.connectionId, new byte[2] { 0x00, 0x00});
                                txt.text += "\r\nOkag: >> Goodbye";
                                break;
                            case 0xffef: //data to spine
                                
                                txt.text += "\r\nClient: " + msg.connectionId + "<< data";
                                //в пакете должно быть минимум заголовок (2 байта), номер стороны куда плевать (1 байт) 
                                //    и собственно что плевать (минимум 1 байт), итого минимум 4 байта
                                if (run && msg.data.Length>=4)
                                {
                                    byte[] backbytes = new byte[msg.data.Length - 3];
                                    //копируем полученные данные в буфер
                                    System.Array.Copy(msg.data, 3, backbytes, 0, msg.data.Length - 3);
                                    //функция кладет данные на спину. Первый аргумент - номер стороны спины
                                    SendToBack(msg.data[2], ref backbytes);

                                    server.Send(msg.connectionId, new byte[2] { 0xff, 0xee });
                                    txt.text += "\r\nOkag: >> Yummy!";
                                }
                                else
                                {
                                    server.Send(msg.connectionId, new byte[2] { 0xff, 0xec });
                                    txt.text += "\r\nOkag: >> Can't eat now!";
                                }
                                break;

                            case 0xffdf: //учим эти слова или символы (ща пришлю с чем ассоциировать, ну или уже прислал)

                                txt.text += "\r\nClient: " + msg.connectionId + "<< learn this";
                                //в пакете должно быть минимум заголовок (2 байта), 
                                //    и символ в UTF-8 (минимум 1 байт), итого минимум 3 байта
                                if (run && msg.data.Length >=3)
                                {
                                    byte[] backbytes = new byte[msg.data.Length - 2];
                                    //копируем полученные данные в буфер
                                    System.Array.Copy(msg.data, 2, backbytes, 0, msg.data.Length - 2);
                                    //функция кладет данные на спину. Первый аргумент - номер стороны спины
                                    SendToBack(0xff, ref backbytes);

                                    server.Send(msg.connectionId, new byte[2] { 0xff, 0xde });
                                    txt.text += "\r\nOkag: >> Study!";
                                }
                                else
                                {
                                    server.Send(msg.connectionId, new byte[2] { 0xff, 0xdc });
                                    txt.text += "\r\nOkag: >> Can't study now! Not running?";
                                }
                                break;
                        }
                        break;
                    case Telepathy.EventType.Disconnected:
                        txt.text += "\r\nClient disconnected " + msg.connectionId;
                        break;
                }
            }
        }
    }

    private void SendToBack(byte nback,ref byte[] bb)
    {
        //TODO как посылать данные на спину? Хорошо бы разделить видео и уши - пусть об этом заботится клиент? Да! 
        //Занята только FF-сторона k=width, это то, что мы говорим и то, чему нас учат, если учат
        int n = 0, nn = 0;
        switch (nback)
        {
            case 0: //сторона k=0
                for (int i = 0; i < lenght; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        if (n >= bb.Length) return; //все выложили

                        ca.ChangeAge((short)(i), (short)j, 0, (short)((bb[n] >> nn) & 1));
                        nn++; if (nn > 7) { nn = 0;n++; }
                    }
                }
                break;
            case 1: //сторона j=0
                for (int i = 0; i < lenght; i++)
                {
                    for (int k = 0; k < width; k++)
                    {
                        if (n >= bb.Length) return; //все выложили

                        ca.ChangeAge((short)(i), 0, (short)k, (short)((bb[n] >> nn) & 1));
                        nn++; if (nn > 7) { nn = 0; n++; }
                    }
                }
                break;
            case 2: //сторона i=0
                for (int j = 0; j < height; j++)
                {
                    for (int k = 0; k < width; k++)
                    {
                        if (n >= bb.Length) return; //все выложили

                        ca.ChangeAge(0, (short)(j), (short)k, (short)((bb[n] >> nn) & 1));
                        nn++; if (nn > 7) { nn = 0; n++; }
                    }
                }
                break;
            case 3: //сторона j=height
                for (int i = 0; i < lenght; i++)
                {
                    for (int k = 0; k < width; k++)
                    {
                        if (n >= bb.Length) return; //все выложили

                        ca.ChangeAge((short)(i), (short)(height-1), (short)k, (short)((bb[n] >> nn) & 1));
                        nn++; if (nn > 7) { nn = 0; n++; }
                    }
                }
                break;
            case 4: //сторона i=lenght
                for (int j = 0; j < height; j++)
                {
                    for (int k = 0; k < width; k++)
                    {
                        if (n >= bb.Length) return; //все выложили

                        ca.ChangeAge((short)(lenght-1), (short)(j), (short)k, (short)((bb[n] >> nn) & 1));
                        nn++; if (nn > 7) { nn = 0; n++; }
                    }
                }
                break;

            case 0xff: //сторона k=width - то, чему мы учим (слово или несколько слов)
                for (int i = 0; i < bb.Length && i < lenght; i++)
                {
                    for (int j = 0; j < 8; j++)//8бит
                    {
                        ca.ChangeAge((short)(i), (short)j, (short)(width - 1), (short)((bb[i] >> j) & 1));
                    }
                }
                break;
        }

    }

    // Update is called once per frame
    private bool run = false;

    void Update()
    {        

        volumeText.text = "Generations: " + generation.ToString()+ "\r\nv:" + 
                    ((int)Mathf.Round(volume)).ToString() + "." + ((int)((volume - Mathf.Round(volume)) * 100)).ToString();
   
        if (run)
        {
            ca.queChangeAgeFunc();
            n.Do();

            n.DoServices();

            int hyyy = (n.service.hyppocamp_value * 1 / n.service.vconst_hyppo_ave + n.service.hyppocamp_prev * (n.service.vconst_hyppo_ave - 1) / n.service.vconst_hyppo_ave);
            if (hyyy > 0)
            {
                //textDebug.text = hyyy.ToString();
            }
        }

        ListeningClients();
    }

    uint ceur_bit = 0;
    uint breathe_bit = 0;

    private void FixedUpdate()
    {
        if (run)
        {

            //сердцебиение и дыхание
            for (int j = 0; j < height; j++)
            {
                ca.ChangeAgeFast((short)(lenght/2+ 3), (short)j, 10, 0);
                ca.ChangeAgeFast((short)(lenght / 2 + 3), (short)j, 12, 0);
            }
            for (int j=0;j< breathe_bit%(height-1);j++)
            {
                ca.ChangeAge((short)(lenght / 2 + 3), (short)j, 10, 1);
            }
            for (int j = 0; j < ceur_bit%(height >> 1); j++)
            {
                ca.ChangeAge((short)(lenght / 2 + 3), (short)(height-j-1), 12, 1);
            }
            breathe_bit++; ceur_bit++;         
            
            Step();

            //генерим ответ
            GenAnswer();
        }
    }

    public void textToBox(string text)
    {
        byte[] b = System.Text.Encoding.UTF8.GetBytes(text);
        //рисуем в КА на стороне k=width 
        for (int i= 0; i<b.Length && i<lenght; i++)
        {
            for (int j=0;j<8;j++)//8бит
            {               
                ca.ChangeAge((short)(i), (short)j, (short)(width-1), (short)((b[i] >> j) & 1));
               
            }
        }
    }

    float volume=0.0f;
    private void GenAnswer()
    {
        
        
        
        for (int i =30; i<= 40; i++)
        {
            for (int j=height/2-10;j<height/2+10;j++)
                volume = volume + ca.cell[i, j, width / 2] * 3+ ca.cell[i, j, width / 2-1]+ ca.cell[i, j, width / 2+1];   
        }
        if (volume < 1) volume = 0;        
        else volume = (Mathf.Log10(Mathf.Log(volume)) ) * 5f;

        string blabla="";
        if (volume>4f)
        {//говорит и показыват КА

            byte[] b = new byte[height-2];
            for (int j =1; j < height-1; j++)                
            {
                int v = 0;

                for (int i = 30; i <= 40; i++)
                {
                    
                    for (int k = 1; k < width - 1; k++)
                    {
                        v += ca.cell[i, j, k];

                    }
                    
                }
                v = v % 256;
                b[j]=  (byte)v;
                
            }
            blabla = System.Text.Encoding.UTF8.GetString(b);
            textToBox(blabla); //рисуем то, что говорим
            Debug.Log(blabla);
           
            txt.text += blabla+"\r\n";
            scroll.verticalNormalizedPosition = 0f;

            n.service.queQueryDebugWrite.Enqueue(new structDNADebugQueue(blabla, 0, volume, ""));
            
        }
       
    }

    private void Step()
    {
        generation++;
        ca.NextStep();
        if (inputToggleShowHideCA.isOn)
        for (short i = 0; i < lenght; i++)
        {
            for (short j = 0; j < height; j++)
            {
                for (short k = 0; k < width; k++)
                {
                    if (ca.cell[i, j, k] == 0)
                    {
                        objects[i, j, k].SetActive(false);                       
                    }
                    else
                    {
                        float al = 1f / ca.rule.max_age * ((ca.rule.max_age + 1) - ca.cell[i, j, k]);
                        objects[i, j, k].GetComponent<MeshRenderer>().material.color = new Color(1.0f, 1f - al, 1f - al, al);
                        objects[i, j, k].SetActive(true);
                    }
                }
            }
        }
    }

    public void ButtonRunStop_Click()
    {
        if (po is null) po = GetComponent<PianoOut>();


        //TODO считать с базы, табл Settings
        int t = 20;
        Time.fixedDeltaTime = t / 100.0f;
        run = !run;

        if (run)
        {
            po.PlayNote(60); po.PlayNote(64); po.PlayNote(70);
            n.service.queQueryDebugWrite.Enqueue(new structDNADebugQueue("start"));
        }
        else
        {
            po.PlayNote(55); po.PlayNote(75); po.PlayNote(90);
        }
    }

    public void ButtonStep_Click()
    {
        if (run)run = false;
        else Step();
    }

    public void inputToggle_toggle()
    {
        if (!inputToggleShowHideCA.isOn)
        {
            for(int i=0;i<lenght;i++)
                for (int j = 0; j < height; j++)
                    for (int k = 0; k < width; k++)
                    {
                        objects[i, j, k].SetActive(false);
                    }
        }
    }

    public void ButtonSleep_Click()
    {
        txt.text += "sleep now...\r\n";
        if (run) ButtonRunStop_Click();

        n.Sleep();
        txt.text += "quit now...\r\n";
        if (Application.isEditor) UnityEditor.EditorApplication.isPlaying = false;
        else Application.Quit();
    }

    void OnApplicationQuit()
    {
        // the client/server threads won't receive the OnQuit info if we are
        // running them in the Editor. they would only quit when we press Play
        // again later. this is fine, but let's shut them down here for consistency
        
        //if(client!=null) client.Disconnect();
        server.Stop();
    }


    ~Gen()
    {
        
        
        n.ca = null;
        n.service = null;
        n.dna = null;       
        n = null;

    }
}

public struct ClientInfo
{
    string uID;
    string IP;

    public ClientInfo(string uid, string ip)
    {
        uID = uid;
        IP = ip;
    }
}

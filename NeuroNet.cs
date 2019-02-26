using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CellularAutamata;
using System.Threading;

public class NeuroNet//рабочий класс - обертка, он создает не любую сеть, а только конкретную
{

    List<Neuron> nn;
    
    public Service service;
    public DNA dna;
    MAPQueue mq;

    public CellularAutamata3D ca;

    Thread myThread1, myThread2, myThread3,myThread4, myThread5, dnaMEMThread,dnaDISCThread;

    //сеть из готовых данных (из ДНК)
    public NeuroNet(ref CellularAutamata3D cla, ref Service s, ref MAPQueue mpq, ref List<Neuron> ln, ref DNA d)
    {
        service = s;
        mq = mpq;
        ca = cla;
        nn = ln;
        dna = d;
        myThread1 = new Thread(new ThreadStart(DoNeuronsThread));
        myThread2 = new Thread(new ThreadStart(DoNeuronsThread));
        myThread3 = new Thread(new ThreadStart(DoNeuronsThread));
        myThread4 = new Thread(new ThreadStart(DoNeuronsThread));
        myThread5 = new Thread(new ThreadStart(DoNeuronsThread));

        dnaMEMThread = new Thread(new ThreadStart(DnaMemoryThread));
        dnaDISCThread = new Thread(new ThreadStart(DnaDiscThread));
    }

    public NeuroNet(ref CellularAutamata3D cla)//рожаем произвольную сеть
    {
        service = new Service();
        mq = new MAPQueue();
        ca = cla;
        
        nn = new List<Neuron>();

        //создаем нейроны,слушающие аудио-входы слева
        for(int i =0;i<100;i++)
        {
            Coord[] syn = new Coord[16];       //у нерйона максимум 16 синапсов 
            for (int ii = 0; ii < 6; ii++)            //сделаем 6 синапсов в случайных местах коробки
            {
                syn[ii].i = (short)(Random.Range(0, ca.lenght));
                syn[ii].j = (short)(Random.Range(0, ca.height));
                syn[ii].k = 0;

                //синапс 0,0,0 - служебный, у него нет нейронов
                if (syn[ii].i == 0 && syn[ii].j == 0 && syn[ii].k == 0) syn[ii].i = 1;
            }

            Coord[] ax = new Coord[16];           //максимум нейрон может повлиять на 16 ячеек
            //у нового нейрона пока только один выход, сгенерируем его местоположение в коробке
            ax[0].i = (short)(Random.Range(0, ca.lenght));
            ax[0].j = (short)(Random.Range(0, ca.height));
            ax[0].k = (short)(Random.Range(0, ca.width));

            nn.Add(new Neuron(i,ref ca, syn, ax,ref mq,ref service));
        }

        //создаем нейроны,слушающие аудио-входы справа
        for (int i = 100; i < 200; i++)
        {
            Coord[] syn = new Coord[16];       //у нерйона максимум 16 синапсов 
            for (int ii = 0; ii < 6; ii++)            //сделаем 6 синапсов в случайных местах коробки
            {
                syn[ii].i = (short)(Random.Range(0, ca.lenght));
                syn[ii].j = (short)(Random.Range(0, ca.height));
                syn[ii].k = (short)(ca.width-1);              
            }

            Coord[] ax = new Coord[16];           //максимум нейрон может повлиять на 16 ячеек
            //у нового нейрона пока только один выход, сгенерируем его местоположение в коробке
            ax[0].i = (short)(Random.Range(0, ca.lenght));
            ax[0].j = (short)(Random.Range(0, ca.height));
            ax[0].k = (short)(Random.Range(0, ca.width));

            nn.Add(new Neuron(i, ref ca, syn, ax, ref mq, ref service));
        }

        //создаем случайные нейроны
        for (int i = 200; i < 300; i++)
        {
            nn.Add(new Neuron(i, ref ca, ref mq, ref service));
        }

        //создаем случайные суммирующие нейроны
        for (int i = 300; i < 400; i++)
        {
            nn.Add(new NeuronSum(i, ref ca, ref mq, ref service));
        }

        //создаем суммирующие нейроны,слушающие аудио-входы слева
        for (int i = 400; i < 500; i++)
        {
            Coord[] syn = new Coord[16];       //у нерйона максимум 16 синапсов 
            for (int ii = 0; ii < 6; ii++)            //сделаем 6 синапсов в случайных местах коробки
            {
                syn[ii].i = (short)(Random.Range(0, ca.lenght));
                syn[ii].j = (short)(Random.Range(0, ca.height));
                syn[ii].k = 0;

                //синапс 0,0,0 - служебный, у него нет нейронов
                if (syn[ii].i == 0 && syn[ii].j == 0 && syn[ii].k == 0) syn[ii].i = 1;
            }

            Coord[] ax = new Coord[16];           //максимум нейрон может повлиять на 16 ячеек
            //у нового нейрона пока только один выход, сгенерируем его местоположение в коробке
            ax[0].i = (short)(Random.Range(0, ca.lenght));
            ax[0].j = (short)(Random.Range(0, ca.height));
            ax[0].k = (short)(Random.Range(0, ca.width));

            nn.Add(new NeuronSum(i, ref ca, syn, ax, ref mq, ref service));
        }

        //создаем суммирующие нейроны, слушающие аудио-входы справа
        for (int i = 500; i < 600; i++)
        {
            Coord[] syn = new Coord[16];       //у нерйона максимум 16 синапсов 
            for (int ii = 0; ii < 6; ii++)            //сделаем 6 синапсов в случайных местах коробки
            {
                syn[ii].i = (short)(Random.Range(0, ca.lenght));
                syn[ii].j = (short)(Random.Range(0, ca.height));
                syn[ii].k = (short)(ca.width - 1);
            }

            Coord[] ax = new Coord[16];           //максимум нейрон может повлиять на 16 ячеек
            //у нового нейрона пока только один выход, сгенерируем его местоположение в коробке
            ax[0].i = (short)(Random.Range(0, ca.lenght));
            ax[0].j = (short)(Random.Range(0, ca.height));
            ax[0].k = (short)(Random.Range(0, ca.width));

            nn.Add(new NeuronSum(i, ref ca, syn, ax, ref mq, ref service));
        }

        //создаем суммирующие нейроны, создающие ГРОМКОСТЬ звука
        for (int i = 600; i < 650; i++)
        {
            Coord[] syn = new Coord[16];   
            for (int ii = 0; ii < 6; ii++)           
            {
                syn[ii].i = (short)(Random.Range(1, ca.lenght - 1)); 
                syn[ii].j = (short)(Random.Range(1, ca.height - 1)); 
                syn[ii].k = (short)(Random.Range(1, ca.width - 1));
            }

            Coord[] ax = new Coord[16];           
            ax[0].i = (short)(ca.lenght/2-(i%(ca.lenght/2-1)));
            ax[0].j = (short)(ca.height/2);
            ax[0].k = (short)(ca.width/2);

            nn.Add(new NeuronSum(i, ref ca, syn, ax, ref mq, ref service));
        }

        //создаем структурные нейроны, создающие высоту звука (ноту)
        for (int i = 650; i < 700; i++)
        {
            Coord[] syn = new Coord[16];
            for (int ii = 0; ii < 6; ii++)
            {
                syn[ii].i = (short)(Random.Range(1, ca.lenght - 1));
                syn[ii].j = (short)(Random.Range(1, ca.height - 1));
                syn[ii].k = (short)(Random.Range(1, ca.width - 1));
            }

            Coord[] ax = new Coord[16];
            ax[0].i = (short)(ca.lenght / 2 + (i % (ca.lenght / 2 - 1)));
            ax[0].j = (short)(ca.height / 2);
            ax[0].k = (short)(ca.width / 2);

            nn.Add(new Neuron(i, ref ca, syn, ax, ref mq, ref service));
        }

        dna = new DNA(ref nn,ref service);

        myThread1 = new Thread(new ThreadStart(DoNeuronsThread));
        myThread2 = new Thread(new ThreadStart(DoNeuronsThread));
        myThread3 = new Thread(new ThreadStart(DoNeuronsThread));
        myThread4 = new Thread(new ThreadStart(DoNeuronsThread));
        myThread5 = new Thread(new ThreadStart(DoNeuronsThread));
    }


    public void AddSomeNeuronRandom(int count, char typen)
    {
        for(int i=0;i<count;i++)
        {
            if(typen=='c')
            {
                nn.Add(new Neuron(nn.Count, ref ca, ref mq, ref service));
            }
            else if (typen == 's')
            {
                nn.Add(new NeuronSum(nn.Count, ref ca, ref mq, ref service));
            }
        }
        Debug.Log("Добавлено " + count + " нейронов");
    }

    public void Do()
    {
        if (!myThread1.IsAlive)
        {
            myThread1 = new Thread(new ThreadStart(DoNeuronsThread));
            myThread1.Start();
        }
        if (!myThread2.IsAlive)
        {
            myThread2 = new Thread(new ThreadStart(DoNeuronsThread));
            myThread2.Start();
        }
        if (!myThread3.IsAlive)
        {
            myThread3 = new Thread(new ThreadStart(DoNeuronsThread));
            myThread3.Start();
        }
        if (!myThread4.IsAlive)
        {
            myThread4 = new Thread(new ThreadStart(DoNeuronsThread));
            myThread4.Start();
        }
        if (!myThread5.IsAlive)
        {
            myThread5 = new Thread(new ThreadStart(DoNeuronsThread));
            myThread5.Start();
        }
    }

    private void DoNeuronsThread()
    {
        //foreach (Neuron n in nn)
        for (int i = 0; i < nn.Count; i++)
        {
            if (nn[i].ineed_do)
            {
                nn[i].Do();
                nn[i].ineed_do = true;

                if (Service.RandomRange(0, 10) > 5) nn[i].ForgotRAM();//приблизительно x случайных нейронов забывает потихоньку 
                else if (Service.RandomRange(0, 10) > 6)
                {
                    if (!service.decdecdec && !service.dnadnadna)
                        nn[i].Realign();//и еще меньше x случайных нейронов перестраивают входы и выходы, если не обчулись до сих пор
                }
            }
        }
        //Debug.Log("87000");
    }

    public void DoServices()
    {
        service.Time_realtimeSinceStartup = Time.realtimeSinceStartup;

        if (!dnaMEMThread.IsAlive)
        {
            dnaMEMThread = new Thread(new ThreadStart(DnaMemoryThread));
            dnaMEMThread.Start();
        }

        if (!dnaDISCThread.IsAlive)
        {
            dnaDISCThread = new Thread(new ThreadStart(DnaDiscThread));
            dnaDISCThread.Start();
        }


        if (service.decdecdec)//если включено торможение, проверяем, не пора ли выключить
        {
            if (service.hypophise_NONvaluable * 1.0f / (service.hypophise_valuable + service.hypophise_NONvaluable) 
                < (Service.const_hypohpise_flow_change - Service.const_hypohpise_flow_change_hesteresis))
            {
                service.decdecdec = false;
                service.hypophise_NONvaluable = 0; service.hypophise_valuable = 0;
                Debug.Log("Выключено торможение!");
            }
        }
        else if (service.hypophise_valuable + service.hypophise_NONvaluable > 0 && service.Time_realtimeSinceStartup>Service.CONST_HYPPO_TIMESTART)//только через 2 минуты после просыпания возможно торможение
        {
            if (service.hypophise_NONvaluable * 1.0f / (service.hypophise_valuable + service.hypophise_NONvaluable) > Service.const_hypohpise_flow_change)
            {
                service.decdecdec = true;
                Debug.Log("ТОРМОЖЕНИЕ ТОРМОЖЕНИЯ ВКЛЮЧЕНО!");
            }
        }
    }

    //поток чтения-записи в памяти
    private void DnaMemoryThread()
    {
        if (service.dnadnadna) //если обучение достигло пика и все пишем в ДНК, то проверим, не пора ли выключить выделение этого гормона
        {
            float val = service.hyppocamp_value * 1 / Service.const_hyppo_ave + service.hyppocamp_prev * (Service.const_hyppo_ave - 1) / Service.const_hyppo_ave;
            if (val < Service.const_hyppocamp_stop)//процент обученных нейронов меньше порога
            {//выключаем экспрессию генов
                service.dnadnadna = false;
                Debug.Log("ВЫКЛЮЧЕНА экспрессия генов!");
            }
            dna.WriteAll();//торопимся успеть все записать, пока включена экспрессия
        }
        else
        {
            float val = service.hyppocamp_value * 1 / Service.const_hyppo_ave + service.hyppocamp_prev * (Service.const_hyppo_ave - 1) / Service.const_hyppo_ave;
            //Debug.Log(val);
            if (val > Service.const_hyppocamp_start)
            {
                service.dnadnadna = true;
                Debug.Log("Экспрессия генов ВКЛЮЧЕНА!");
            }

        }
        service.hyppocamp_prev = service.hyppocamp_value;
        service.hyppocamp_value = 0;

        for (int i = 0; i <100;i++)
        {
            dna.ReadOne();
            dna.WriteOne();
        }
    }

    //поток чтения-записи на диске
    private void DnaDiscThread()
    {
        for (int i = 0; i < 100; i++)
        {
            dna.IsAxonToSynapseOne();
            dna.SynapseUpdateOrCreateOne();
            dna.AxonUpdateOrCreateOne();
            dna.WriteDebugOne();
        }

        int new_neu;
        if (service.queQueryNewNeuron.TryDequeue(out new_neu))//добавление в систему нового нейрона
        {
            //у нового нейрона такие же синапсы и аксоны как у родителя
            //и ответы пока не придумал как ставить, пусть типа нет ответов пока
            char typen = nn[new_neu].GetTypeNeuron();
            Neuron neu;

            if (typen == 'c')// структурный
                neu = new Neuron(nn.Count, ref ca, nn[new_neu].synapses, nn[new_neu].axon, ref mq, ref service);
            else if (typen == 's')// суммирующий
                neu = new NeuronSum(nn.Count, ref ca, nn[new_neu].synapses, nn[new_neu].axon, ref mq, ref service);
            else //дендритный
                neu = new NeuronDendSpike(nn.Count, ref ca, nn[new_neu].synapses, nn[new_neu].axon, ref mq, ref service);

            nn.Add(neu);
            dna.AddNeuron(neu.number);
        }
    }

    public void Sleep()
    {
        //ждем, пока не закончится потоки чтения-записи
        while (dnaMEMThread.IsAlive) { }
        while (dnaDISCThread.IsAlive) { }

        dna.Sleep(ref ca);
    }

    ~NeuroNet()
    {
        nn = null;
    }
}

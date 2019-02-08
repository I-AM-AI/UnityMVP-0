using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using CellularAutamata;

public struct Coord
{
    public short i, j, k;//координата синапса
}

public struct structMAPqueue
{
    public int number;
    public float timestamp;
    public int power;

    public structMAPqueue(int num, float time, int pow)
    {
        number = num; timestamp = time; power = pow;
    }
}

public class FixedSizedQueue<T> : Queue<T>
{
    public int Size { get; private set; }
    public FixedSizedQueue(int size)
    {
        Size = size;
    }

    public new void Enqueue(T obj)
    {
        base.Enqueue(obj);
        while (base.Count > Size)  base.Dequeue();         
    }
}


public class FixedSizedQueueConcurent<T> : ConcurrentQueue<T>
{
    private readonly object syncObject = new object();

    public int Size { get; private set; }

    public FixedSizedQueueConcurent(int size)
    {
        Size = size;
    }

    public new void Enqueue(T obj)
    {
        base.Enqueue(obj);
        lock (syncObject)
        {
            while (base.Count > Size)
            {
                T outObj;
                base.TryDequeue(out outObj);
            }
        }
    }
}
public class MAPQueue
{
    public FixedSizedQueue<structMAPqueue>[] que;

    public MAPQueue()
    {
        que = new FixedSizedQueue<structMAPqueue>[256];
        //их все надо родить
        for(int i=0;i<256;i++)
        {
            que[i] = new FixedSizedQueue<structMAPqueue>(20);//количество публикаций в мап-очедерях ограниченно 20
        }
    }
}




public class Service
{
    public FixedSizedQueueConcurent<structDNAReadQueue> queQueryToDnaRead;  //очередь запросов на чтение из ДНК
    public FixedSizedQueueConcurent<structDNAWriteQueue> queQueryToDnaWrite;  //очередь запросов на запись из ДНК

    public FixedSizedQueueConcurent<int> queQueryNewNeuron;         //очередь запросос на создание нового нейрона в системе

    //эти значения нужны для поддержки балланса в системе (эмитация гипофиза)
    public int hypophise_valuable {get;set;}       //когда нейрон посылает в КА значение больше 0, он увеличивает это значение
    public int hypophise_NONvaluable { get; set; }  //когда нейрон посылает в КА значение 0 (это происходит, когда сила его ответа больше максимально принимаемого КА)
    public bool decdecdec { get; set; }             //true - пришел тормозной нейромедиатор, уменьшаем значения
    public const float const_hypohpise_flow_change = 0.9f;   //если процент торможения выше этого порога - включаем не на долго реверс системы (торможение торможения)
    public const float const_hypohpise_flow_change_hesteresis = 0.02f; //как только торможение упадет на эту величину - выключаем
    
    //эти значения нужны для сигналов памяти
    //это значение учитывает количество вновь обучившихся нейронов
    public int hyppocamp_value { get; set; }    //когда нейрон обучился (через ОЗУ или метоботропно) он увеличивает это значение                                                
    public int hyppocamp_prev { get; set; }    //предыдущее значение 
    public bool dnadnadna { get; set; }         //действие гормона гиппокампа "пишем все в ДНК"
    public const float const_hyppocamp_start = 50.0f; //когда среднее будет больше этого за единицу времени - посылаем нейромедиатор принудительного запоминания
    public const float const_hyppocamp_stop = 200.0f;  //когда процент обученных упал до этого значение, перестаем насильно писать в память
    public const int const_hyppo_ave = 15;          //за сколько циклов усредняем значение параметра обучаемости

    public Service()
    {
        queQueryToDnaRead = new FixedSizedQueueConcurent<structDNAReadQueue>(1000);        
        queQueryNewNeuron = new FixedSizedQueueConcurent<int>(1000);
        queQueryToDnaWrite = new FixedSizedQueueConcurent<structDNAWriteQueue>(1000);
    }
}

public abstract class NeuronBase
{
    public abstract ushort GetPattern(ref List<int> syn);
    public abstract char GetTypeNeuron();
}

//нейрон - передатчик структуры
//он смотрит на структуру синапсов, а не на их веса (паттерн формируется наличием или отсутствием сигнала на нейроне)
public class Neuron : NeuronBase
{
    public int number;                 //уникальный номер нейрона
    public Coord[] synapses;               //номера синапсов в КА [0-ой синапс, 1-ый синапс,...]
    public Coord[] axon;                   //на какие ячейки влияет аксон

    public short[] responses;               //ОЗУ - ответы на патерны из синапсов, 65535 ответов, ответа на патерн 0 нет, и ответ 0 означает, что ответа нет
    public byte[] responses_DNA_flags;     //флаги на каждый ответ ОЗУ:  (не запоминается в ДНК)
                                           //0b0000 - нет флагов
                                           //0b0001 - спрашивали? отвечаем: реакция записана в ДНК нейрона
                                           //0b0010 - ожидание ответа из ДНК
                                           //0b0100 - спрашивали? отвечаем: записи в ДНК нет
                                           //0b1000 - ожидание записи в ДНК
    public bool is_have_any_response;       //нейрон имеет хоть один ответ вообще? 
    protected CellularAutamata3D      ca;           //ссылка на коробку

    public short[]          MAPa;                   //номера групп, с которыми нейрон может строить ассоциации МАП (не запоминается в ДНК)
    private MAPQueue        mapQue;                 //ссылка на очереди этих групп для МАП
    private int[]           MAPtable;               //непосредственно таблица МАП, состоящая из 16 значений. Индекс - номер синапса, значение - сила ассоциации

    private Service         service;                //сервисы главного модуля, к которым нейрон может обращаться

    const short const_spikes_write_DNA = 36;           //ответы начинают запоминаться в ДНК при сигнале выше этого
    const short const_spikes_gennew_in = 423;        //количество спайков, при достижении которого добавляется новый синапс
    const short const_spikes_gennew_out = 1234;      //количество спайков, при достижении которого добавляется новый выход на аксоне
    const short const_spikes_gennew_neuron = 2345;  //количество спайков, при котором рождается новый нейрон
    const short const_min = -500;                      //минимальный ответ нейрона
    const short const_max = 32000;                  //максимальный ответ нейрона

    const short const_first_time = -10;               //нейрон впервые видит такой патерн, он не отвечает ничего, но ставит это значение себе в ответ
    const short const_dna_search_add = -3;           //При достижении этой отрицательной величины, выдается запрос в ДНК на поиск ответа на этот патерн
    const short const_dna_search_dec = -6;           //Если при забывании  ответ достигает этого значения, то ниже опускается только при отсутствии записи в ДНК
                                                     //в этой версии оно меньше чем АДД, потому что при вспоминании значение из ДНК стирается

    const short const_del_patern = -100;              //Если при забывании  ответ достигает этого значения, то патерн становится равен 0
    const short const_dec_MAP = 1;                    //уменьшить все значения в таблице МАП на эту величину
    const short const_MAP_teach_end = 600;            //при достижении этого значения, МАП обучение завершено

    const short const_dec_by_time = 1;                //с течением времени ответы уменьшаются на эту величину

    const float const_hypo_divide = 2.0f / 3;          //гипофиз включил подавление торможения - ответы уменьшаются на эту величину

    const short const_timesLive_before_realign = 1000;   //если за это количество циклов нейрон ничему не научился, мы его переделывам

    public override char GetTypeNeuron() { return 'c'; }
    //конструктор случайного нейрона
    public Neuron(int num, ref CellularAutamata3D cla, ref MAPQueue mq, ref Service serv) //конструктор принимает ссылку на ящик, ссылку на очереди МАП, ссылку на сервисы
    {
        number = num;
        ca = cla;                   //коробка
        synapses = new Coord[16];       //у нерйона максимум 16 синапсов 
        for(int i=0;i<6;i++)            //сделаем 6 синапсов в случайных местах коробки
        {
            synapses[i].i = (short)(Random.Range(0, ca.lenght));
            synapses[i].j = (short)(Random.Range(0, ca.height));
            synapses[i].k = (short)(Random.Range(0, ca.width));

            //синапс 0,0,0 - служебный, у него нет нейронов
            if (synapses[i].i == 0 && synapses[i].j == 0 && synapses[i].k == 0) synapses[i].k = 1;
        }
        responses = new short[65536];     //65536 ответов
        responses_DNA_flags = new byte[65536];//и флагов к ним

        axon = new Coord[16];           //максимум нейрон может повлиять на 16 ячеек
        //у нового нейрона пока только один выход, сгенерируем его местоположение в коробке
        axon[0].i = (short)(Random.Range(0, ca.lenght));
        axon[0].j = (short)(Random.Range(0, ca.height));
        axon[0].k = (short)(Random.Range(0, ca.width));

        //рожаем номера групп для МАП
        int countMAP=Random.Range(1, 16); //количество групп
        MAPa = new short[countMAP];
        for(int i=0;i<countMAP;i++)
        {
            MAPa[i]= (short)(Random.Range(0, 255)); //номер группы
        }

        mapQue = mq;//ссылка на очереди МАП
        MAPtable = new int[16]; //таблица МАП

        service = serv;
    }

    //конструктор нейрона с заданными синапсами и аксонами
    public Neuron(int num, ref CellularAutamata3D cla, Coord[] syns, Coord[] axs, ref MAPQueue mq, ref Service serv)
    {
     
        number = num;
        ca = cla;                   //коробка
        synapses = new Coord[16];       
        for (int i = 0; i < syns.Length && i<16; i++)            
        {
            synapses[i].i = syns[i].i;
            synapses[i].j = syns[i].j;
            synapses[i].k = syns[i].k;
        }

        responses = new short[65536];     //65535 ответов
        responses_DNA_flags = new byte[65536];//и флагов к ним

        axon = new Coord[16];           //максимум нейрон может повлиять на 16 ячеек

        for (int i = 0; i < axs.Length && i < 16; i++)
        {
            axon[i].i = axs[i].i;
            axon[i].j = axs[i].j;
            axon[i].k = axs[i].k;
        }


        //рожаем номера групп для МАП
        int countMAP = Random.Range(1, 16); //количество групп
        MAPa = new short[countMAP];
        for (int i = 0; i < countMAP; i++)
        {
            MAPa[i] = (short)(Random.Range(0, 255)); //номер группы
        }

        mapQue = mq;
        service = serv;

        MAPtable = new int[16]; //таблица МАП
    }

    public short GetActivityForMAP(float time, int power)
    {
        float delta = Time.realtimeSinceStartup - time;
        float pd = power / delta/10;
        if (pd < 1) pd = 1;
        else if (pd > 128) pd = 128;
        //const_spikes_gennew_neuron
        return (short)pd;
    }

    public override ushort GetPattern(ref List<int> syn)
    {
        ushort pat = 0; ushort synapse_val = 0;
        for (ushort i = 0; i < 16; i++)
        {
            if (synapses[i].i == 0 && synapses[i].j == 0 && synapses[i].k == 0) //синапса нет такого еще у нейрона
                return pat;
            synapse_val = (ushort)(ca.cell[synapses[i].i, synapses[i].j, synapses[i].k] > 0 ? 1 : 0);//в клеточном автомате нам не важно какое значение. если оно больше 0, значит на синапсе 1.
            pat = (ushort)(pat | (synapse_val << i));
            if (synapse_val == 1) syn.Add(i);
        }
        return pat;
    }


    public void Do()
    {
        //узнаем паттерн
        ushort pat = 0; 
        List<int> syn_on=new List<int>();//ненулевые синапсы (нужно для МАП)

        pat = GetPattern(ref syn_on);

        if (pat==0)//нулевого паттерна 0000000000000000 в системе не существует
        {
            return;//просто выйдем и все?
        }

        if(responses[pat]==0)//ответа в ОЗУ на этот паттерн нет
        {
            responses[pat] = const_first_time;//а теперь есть
            //Debug.Log(number + "Виден впервые такой паттерн");
            if (!service.decdecdec)//если нет торможения торможения
                ImPassive(syn_on, pat);
        }
        else //ответ есть
        {            
            
            //увеличиваем его и не даем возможности быть нулевым. 0 - значит ответа нет. Отрицательным он может быть, но на выход не подается
            if(!service.decdecdec)//если нет торможения торможения
                responses[pat]++;

            if (responses[pat] == 0) responses[pat] = 1;
            //если этот ответ больше 0 (выставляется в КА)
            if (responses[pat] > 0)
            {
                is_have_any_response = true;
                if (service.decdecdec && responses[pat]>ca.rule.max_age)//система перетренирована-все положительные значения больше максимального КА, уменьшаются
                {
                    responses[pat] = (short)(responses[pat] * const_hypo_divide) ;
                }

                //добавляем в очереди групп МАП метку времени и номер нейрона
                foreach (short m in MAPa)//m пробегает номера групп, в которых нейрон строит ассоциации
                {
                    mapQue.que[m].Enqueue(new structMAPqueue(number, Time.realtimeSinceStartup, responses[pat]));
                }

                //рисуем в клеточном автомате по всем выходам аксона
                for (int i = 0; i < 16; i++)
                {
                    if (axon[i].i == 0 && axon[i].j == 0 && axon[i].k == 0) break; //больше выходов аксона нет

                    //if (responses[pat] > ca.rule.max_age) ca.ChangeAge(axon[i].i, axon[i].j, axon[i].k, ca.rule.max_age);//если ответ нейрона больше максимального в КА - ответим максимальный

                    //если ответ нейрона больше максимального в КА - положим 0, это тормозное действие. Все любят бездельничать.
                    //ЭТО ОКАЖЕТ ТОРМОЗНОЕ ДЕЙСТВИЕ НА ВСЮ СИСТЕМУ В ЦЕЛОМ, хотя отдельные нейроны, наоборот, могут активироваться
                    /*
                    if (responses[pat] > ca.rule.max_age)
                    {
                        //ca.ChangeAge(axon[i].i, axon[i].j, axon[i].k, 0);
                        ca.ChangeAge(axon[i].i, axon[i].j, axon[i].k, ca.rule.max_age);
                        service.hypophise_NONvaluable++;
                    }
                    else
                    {
                        ca.ChangeAge(axon[i].i, axon[i].j, axon[i].k, (short)responses[pat]);
                        service.hypophise_valuable++;
                    }
                    */
                    if(responses[pat]%(ca.rule.max_age/2)==1)//нейрон стреляет не постоянно, а только каждый 1,max_age/2+1,...
                    {
                        short newval = (short)(GetAge(responses[pat]) + i); //до каждого следующего выхода аксона доходит меньше активности, т.к. 0-ой аксон смамый ранний, он дольше в системе и оброс большим кол-вом швановых клеток, везикул и т.п.
                        short oldval = ca.cell[axon[i].i, axon[i].j, axon[i].k];
                        if(oldval==0)//в КА пустая клетка
                            ca.ChangeAge(axon[i].i, axon[i].j, axon[i].k, newval);
                        else
                        {//в КА не пустая клетка
                            ca.ChangeAgeByNeuron(axon[i].i, axon[i].j, axon[i].k, (short)((newval + oldval) / 2));//функция выпускает немного нейромедиатора в соседнюю случайную клетку
                        }
                    }

                }
                //для гипофиза, пока его нет, его функцию заменяют сервисы, отслеживающие активность всей сети
                if (responses[pat] > ca.rule.max_age)
                {
                    service.hypophise_NONvaluable++;
                }
                else
                {
                    service.hypophise_valuable++;
                }

                ////////// ЗАПИСЬ В ДНК ЗДЕСЬ!!!!!
                if (responses[pat]==const_spikes_write_DNA & !service.decdecdec)//достигли порога записи в ДНК
                {
                    if((responses_DNA_flags[pat] & 0b1000) > 0) //если уже не ждем запись
                    {
                        service.queQueryToDnaWrite.Enqueue(new structDNAWriteQueue(number, pat, responses[pat]));
                        responses_DNA_flags[pat] |= 0b1000; //ждем запись

                        service.hyppocamp_value++;
                    }
                }

                /////А ЭТО ЗАПИСЬ В ДНК ПРИ КОММАНДЕ СВЕРХУ ОТ ГИППОКАМПА (ЛЮБЫЕ ПОЛОЖИТЕЛЬНЫЕ ОТВЕТЫ СЕЙЧАС ЗАПОМИНАЮТСЯ В ДНК)
                if(service.dnadnadna && !service.decdecdec)//у нас положительный ответ - это раз. у нас сигнал от гиппокампа запомнить это-это два, и  у нас нет подавления переобучаемости в гиппофизе
                {
                    //нам не важно - записывали мы или нет - мы пишем, потому что так сказал дирижер памяти - гиппокамп, и директор (гипофиз) не лишает всех премии за плохое поведение)))
                    service.queQueryToDnaWrite.Enqueue(new structDNAWriteQueue(number, pat, responses[pat]));
                    responses_DNA_flags[pat] |= 0b1000; //ждем запись
                }
                   
                if (responses[pat] % const_spikes_gennew_in == 0 & !service.decdecdec)//добавляем синапс в нейрон
                {
                    responses[pat] += 50; //потому что забывание и торможение может снова включить добавление синапса
                    Debug.Log("НЕЙРОН №" + number + "новый синапс!");
                    for (int i=0;i<16;i++)
                    {
                        if (synapses[i].i == 0 && synapses[i].j == 0 && synapses[i].k == 0) //синапса нет такого еще
                        {
                            synapses[i].i = (short)(Random.Range(0, ca.lenght));
                            synapses[i].j = (short)(Random.Range(0, ca.height));
                            synapses[i].k = (short)(Random.Range(0, ca.width));

                            //синапс 0,0,0 - служебный, у него нет нейронов
                            if (synapses[i].i == 0 && synapses[i].j == 0 && synapses[i].k == 0) synapses[i].k = 1;

                            break;
                        }
                    }
                }

                if (responses[pat] % const_spikes_gennew_out == 0 & !service.decdecdec)//добавляем выход к аксону
                {
                    responses[pat] += 100; //потому что забывание и торможение может снова включить добавление аксона
                    Debug.Log("НЕЙРОН №" + number + "новый аксон!");
                    for (int i = 0; i < 16; i++)
                    {
                        if (axon[i].i == 0 && axon[i].j == 0 && axon[i].k == 0) //аксона нет такого еще
                        {
                            axon[i].i = (short)(Random.Range(0, ca.lenght));
                            axon[i].j = (short)(Random.Range(0, ca.height));
                            axon[i].k = (short)(Random.Range(0, ca.width));

                            //синапс 0,0,0 в КА - служебный, у него нет нейронов
                            if (axon[i].i == 0 && axon[i].j == 0 && axon[i].k == 0) axon[i].k = 1;

                            break;
                        }
                    }

                }

                if (responses[pat] % const_spikes_gennew_neuron == 0 & !service.decdecdec)//добавляем нейрон в систему
                {
                    responses[pat] += 500; //потому что забывание и торможение может снова включить добавление нейрона
                    Debug.Log("НЕЙРОН №" + number + "новый нейрон!");
                    //сообщаем системе, что нужно добавить еще один такой же нейрон
                    service.queQueryNewNeuron.Enqueue(number);
                }
            }
            else
            {
                if(responses[pat] == const_dna_search_add)//ответ достиг порога запроса поиска ответа в ДНК
                {
                    //если бита запроса еще не стоит
                    if ((short)(responses_DNA_flags[pat] & 0b10) != 0b10)
                    {

                        //запрос в ДНК
                        service.queQueryToDnaRead.Enqueue(new structDNAReadQueue(number, pat));

                        responses_DNA_flags[pat] = (byte)(responses_DNA_flags[pat] | 0b10);//устанавливаем бит запрос на чтение в ДНК
                    }
                }

                if (!service.decdecdec)//если нет торможения торможения
                    ImPassive(syn_on, pat);
            }

        }
    }
    //преобразует значение ответа нейрона в возраст клетки КА
    private int GetAge(int resp)
    {
        return (int)(Mathf.Log(resp) * 5.5f);
    }

    //пассивность нейрона... выполняем МАП-обучение и т.п.
    private void ImPassive(List<int> syn_on, ushort p)//принимает на вход список ненулевых синапсов и паттерн
    {
        //проверим, есть ли вообще чего ассоциировать
        int c = 0;
        foreach (short m in MAPa)
        {
            c += mapQue.que[m].Count;//записей в очередях (может все молчат?)
        }
        if (c == 0)
        {//нечего ассоциировать
            
            return;
        }

        //подсчитываем силу ассоциации нейрона с другими нейронами с групп
        short force = 0;
        short pow_ave = 0;
        foreach (short m in MAPa)//m пробегает номера групп, в которых нейрон строит ассоциации
        {
            if (mapQue.que[m].Count > 0)
            {
                structMAPqueue smapque = mapQue.que[m].Dequeue();
                if (mapQue.que[m].Count > 0)
                    force += GetActivityForMAP(smapque.timestamp, smapque.power);//функция вернет значение силы реакции ассоциированного нейрона            
                pow_ave += (short)smapque.power;
            }
        }
        pow_ave /= (short)MAPa.Length;//среднее значение сигнала на аксонах ассоциированных нейронов
        if (pow_ave < 3) return;//меньше чем 3 значения в ДНК нет

        int count_syn_teached = 0;
        foreach(int syn in syn_on)
        {
            MAPtable[syn] -= const_dec_MAP;
            if (MAPtable[syn] < 0) MAPtable[syn] = 0;
            MAPtable[syn] += force;
            if (MAPtable[syn] >= const_MAP_teach_end) count_syn_teached++;
        }

        if (count_syn_teached == syn_on.Count)//нейрон обучился
        {
            //Debug.Log("НЕЙРОН №" + number + "ОБУЧИЛСЯ!"); 

            responses[p] = pow_ave;
            foreach (int syn in syn_on)
            {
                MAPtable[syn] = const_MAP_teach_end/2;
            }
            //запишем в ДНК результат обучения
            service.queQueryToDnaWrite.Enqueue(new structDNAWriteQueue(number, p, responses[p]));
            responses_DNA_flags[p] |= 0b1000; //ждем запись

            service.hyppocamp_value++;//добавим в количество запоммнивших нейронов
        }
        
    }

    public void ForgotRAM()
    {

        
        for(int i=1;i<=65535;i++)//пробегаем все ответы, кроме ответа на 0-патерн, его нет!
        {
            if (responses[i] != 0)//если ответ вообще существует
            {
                if (responses[i] < const_dna_search_dec)//если он уже меньше критического - продолжаем уменьшать
                {
                    responses[i] = (short)(responses[i] - const_dec_by_time);
                    if(responses[i] < const_del_patern)//ответ совсем низкий, удаляем его, ведь его нет и в ДНК
                    {
                        //Debug.Log(number + "Ответ удален.");
                        responses[i] = 0;
                    }
                    continue;
                }
                short rep = (short)(responses[i] - const_dec_by_time);
                if (rep == 0) responses[i] = -1; //0 ответа не бывает
                else if(rep < const_dna_search_dec)//ответ только что стал меньше константы, при которой происходит запрос на поиск
                {//уменьшение достигло критического уровня, нужно спросить, есть ли ответ в ДНК. И если есть, ниже не понижать
                    if((byte)(responses_DNA_flags[i] & 0b100)>0)
                    {//из ДНК пришел ответ, что значение не удалось найти, понижайте нафик
                        responses[i] = rep;
                    }
                    else if((byte)(responses_DNA_flags[i] & 0b11)>0)
                    {//либо есть запрос в ДНК либо уже известно, что ответ есть в ДНК
                        //ничего не делаем
                        
                    }
                    else
                    {//
                        service.queQueryToDnaRead.Enqueue(new structDNAReadQueue(number, (ushort)i));
                        responses_DNA_flags[i] = (byte)(responses_DNA_flags[i] | 0b010); //ждем ответа из ДНК
                        
                    }
                }
            }
        }
    }

    short chancetoteech = 0;
    public void Realign()//нейрон ничему не обучился, ни один его ответ не записан в ДНК, он бесполезен, ему надо поменять синапсы и выходы
    {
        if (is_have_any_response) return;
        else if (++chancetoteech < const_timesLive_before_realign) return;//все еще есть шанс обучится

        chancetoteech = 0;
        Debug.Log("Меняем нейрону "+ number+" синапсы и аксон");

        for (int i = 0; i < 6; i++)            //сделаем 6 синапсов в случайных местах коробки
        {
            synapses[i].i = (short)(Random.Range(0, ca.lenght));
            synapses[i].j = (short)(Random.Range(0, ca.height));
            synapses[i].k = (short)(Random.Range(0, ca.width));

            //синапс 0,0,0 - служебный, у него нет нейронов
            if (synapses[i].i == 0 && synapses[i].j == 0 && synapses[i].k == 0) synapses[i].k = 1;
        }
       
        //у нового нейрона пока только один выход, сгенерируем его местоположение в коробке
        axon[0].i = (short)(Random.Range(0, ca.lenght));
        axon[0].j = (short)(Random.Range(0, ca.height));
        axon[0].k = (short)(Random.Range(0, ca.width));
    }
}

//Нейрон-сумматор. Это больше похоже на классический нейрон-персептрон. Он суммирует значения своих синапсов и смотрит, есть ли в озу ответ на это. 
//т.е. от структурного нейрона он отличается тем, что его паттерн формируется суммой на синапсе, в то время как патрен структурного нейрона формируется наличием
//или отсутствием сигнала на синапсе
//эти нейроны могут также строить МАП-ассоциации по своим активным синапсам и т.п.
//они более активны, т.к. их патерн не зависит отраспределения активности по синапсам, а только от суммы этих активностей
public class NeuronSum: Neuron
{
    public override char GetTypeNeuron() { return 's'; }

    //случайный суммирующий нейроно
    public NeuronSum(int num, ref CellularAutamata3D cla, ref MAPQueue mq, ref Service serv)
        : base( num, ref cla, ref  mq, ref serv)
    {
       
    }

    //конструктор нейрона с заданными синапсами и аксонами
    public NeuronSum(int num, ref CellularAutamata3D cla, Coord[] syns, Coord[] axs, ref MAPQueue mq, ref Service serv)
        : base(num, ref cla,syns,axs, ref  mq, ref  serv)
    {
    }

    //сумматор только лишь переопределяет метод вычисления своего паттерна - это сумма значений всех синапсов
    public override ushort GetPattern(ref List<int> syn)
    {
        ushort pat = 0; ushort synapse_val = 0;
        for (ushort i = 0; i < 16; i++)
        {
            if (synapses[i].i == 0 && synapses[i].j == 0 && synapses[i].k == 0) //синапса нет такого еще у нейрона
                return pat;
            synapse_val = (ushort)(ca.cell[synapses[i].i, synapses[i].j, synapses[i].k]);//для этого типа нейронов, значения в КА важны, т.к. он сумиирует все свои синапсы
            pat += (ushort)(synapse_val);
            if (synapse_val >= 1) syn.Add(i);
        }
        return pat;
    }
}
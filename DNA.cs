using System.Collections.Generic;
using UnityEngine;
using Mono.Data.Sqlite;
using System.Data;
using CellularAutamata;

public struct structDNASynAxUpdateQueue
{
    public int n;
    public int h;

    public structDNASynAxUpdateQueue(int neuronNum, int hand)
    {
        n = neuronNum;
        h = hand;
    }
}

public struct structDNADebugQueue
{
    public string dText;
    public int dInt;
    public float dFloat;
    public string dComment;

    public structDNADebugQueue(string debugText = "", int debugInt = 0, float debugFloat = 0, string comment = "")
    {
        dText = debugText; dInt = debugInt; dFloat = debugFloat; dComment = comment;
    }
}

public struct structDNAReadQueue
{
    public int neuronNumber;
    public ushort pattern;

    public structDNAReadQueue(int neuronNum, ushort pat)
    {
        neuronNumber = neuronNum;
        pattern = pat;
    }
}

public struct structDNAWriteQueue
{
    public int neuronNumber;
    public ushort pattern;
    public int response;

    public structDNAWriteQueue(int neuronNum, ushort pat, int res)
    {
        neuronNumber = neuronNum;
        pattern = pat;
        response = res;
    }
}

public class DNA
{
    IDbConnection dbconn,dbmemory;
    IDbCommand dbcmdmem, dbcmddisc;
    IDataReader readermem,readerdisc;

    List<Neuron> nn;        //сетка нейронов (ну, или не сетка)))
    Service service;
   
    public DNA(ref List<Neuron> n, ref Service s, bool usebase=false)
    {
        string conn;
        if (Application.isEditor) conn = "URI=file:" + Application.dataPath + "/../Neurons.db";
        else conn = "URI=file:" + Application.dataPath + "/Neurons.db";
        
        dbconn = (IDbConnection)new SqliteConnection(conn);
        dbconn.Open(); //Open connection to the database.   

        //переносим базу в память
        dbmemory = new SqliteConnection("URI=file::memory:,version=3");
        dbmemory.Open();
        dbcmdmem=dbmemory.CreateCommand();
        dbcmdmem.CommandText = "CREATE TABLE DNA (id INTEGER PRIMARY KEY UNIQUE NOT NULL, neuron INTEGER NOT NULL, pattern  INTEGER NOT NULL, response INTEGER NOT NULL);";
        dbcmdmem.ExecuteNonQuery();


        using (IDbCommand ic= dbmemory.CreateCommand())
        {
            //есть ли чего-то в ДНК?
            dbcmddisc = dbconn.CreateCommand();
            dbcmddisc.CommandText = "SELECT count(id) FROM DNA";
            int count = System.Convert.ToInt32(dbcmddisc.ExecuteScalar());
            if (count > 0)
            {
                dbcmddisc = dbconn.CreateCommand();
                dbcmddisc.CommandText = "select neuron, pattern, response from dna order by response desc";//самые сильные ответы идут первыми
                readerdisc = dbcmddisc.ExecuteReader();
                string query = "INSERT INTO DNA (neuron, pattern, response) VALUES ";
                int ne;
                ushort pa;
                short re;
                while (readerdisc.Read())
                {
                    ne = readerdisc.GetInt32(0);
                    pa = (ushort)readerdisc.GetInt32(1);
                    re = readerdisc.GetInt16(2);
                    query += "(" + ne.ToString() + ", " + pa.ToString() + ", " + re.ToString() + "),";
                    //заполняем значениями ответов из ДНК только % случайных ответов нейронов (остальное пусть вспоминает, если надо)
                    if ((usebase && Random.Range(0,100)>10) || re>=s.vCONST_DNA_RESPONSE_TO_WAKE)//если же ответ большой, он сразу попадает и не удаляется, но может переобучиться
                    {
                        //*
                        n[ne].responses[pa] = re;
                        //устанавливаем флаги, что значение есть в ДНК, снимаем флаг запроса на чтение и флаг, что нет записи в ДНК
                        n[ne].responses_DNA_flags[pa] = (byte)((n[ne].responses_DNA_flags[pa] & 0b001) | 0b001);
                        n[ne].is_have_any_response = true;
                    }
                    //*/
                }
                query = query.Remove(query.Length - 1);

                ic.CommandText = query;
                ic.ExecuteNonQuery();
            }
        }

        //считываем сеттингсы для работы сети
        dbcmddisc = dbconn.CreateCommand();
        dbcmddisc.CommandText = "select name, intVal, floatVal, stringVal from SETTINGS";//
        readerdisc = dbcmddisc.ExecuteReader();
        while(readerdisc.Read())
        {
            string name = readerdisc.GetString(0);
            switch(name)
            {
                case "vconst_hypohpise_flow_change":
                    s.vconst_hypohpise_flow_change = readerdisc.GetFloat(2);
                    break;
                case "vconst_hypohpise_flow_change_hesteresis":
                    s.vconst_hypohpise_flow_change_hesteresis = readerdisc.GetFloat(2);
                    break;
                case "vconst_hyppocamp_start":
                    s.vconst_hyppocamp_start = readerdisc.GetFloat(2);
                    break;
                case "vconst_hyppo_ave":
                    s.vconst_hyppo_ave = readerdisc.GetInt32(1);
                    break;
                case "vconst_hyppocamp_stop":
                    s.vconst_hyppocamp_stop = readerdisc.GetFloat(2);
                    break;
                case "vCONST_HYPPO_TIMESTART":
                    s.vCONST_HYPPO_TIMESTART = readerdisc.GetFloat(2);
                    break;
                case "vconst_spikes_write_DNA":
                    s.vconst_spikes_write_DNA = readerdisc.GetInt16(1);
                    break;
                case "vconst_spikes_gennew_in":
                    s.vconst_spikes_gennew_in = readerdisc.GetInt16(1);
                    break;
                case "vconst_spikes_gennew_out":
                    s.vconst_spikes_gennew_out = readerdisc.GetInt16(1);
                    break;
                case "vconst_spikes_gennew_neuron":
                    s.vconst_spikes_gennew_neuron = readerdisc.GetInt16(1);
                    break;
                case "vconst_min":
                    s.vconst_min = readerdisc.GetInt16(1);
                    break;
                case "vconst_max":
                    s.vconst_max = readerdisc.GetInt16(1);
                    break;
                case "vconst_first_time":
                    s.vconst_first_time = readerdisc.GetInt16(1);
                    break;
                case "vconst_dna_search_add":
                    s.vconst_dna_search_add = readerdisc.GetInt16(1);
                    break;
                case "vconst_dna_search_dec":
                    s.vconst_dna_search_dec = readerdisc.GetInt16(1);
                    break;
                case "vconst_del_patern":
                    s.vconst_del_patern = readerdisc.GetInt16(1);
                    break;
                case "vconst_dec_MAP":
                    s.vconst_dec_MAP = readerdisc.GetInt16(1);
                    break;
                case "vconst_MAP_teach_end":
                    s.vconst_MAP_teach_end = readerdisc.GetInt16(1);
                    break;
                case "vconst_dec_by_time":
                    s.vconst_dec_by_time = readerdisc.GetInt16(1);
                    break;
                case "vconst_hypo_divide":
                    s.vconst_hypo_divide = readerdisc.GetFloat(2);
                    break;
                case "vconst_timesLive_before_realign":
                    s.vconst_timesLive_before_realign = readerdisc.GetInt16(1);
                    break;
                case "vCONST_DNA_RESPONSE_TO_WAKE":
                    s.vCONST_DNA_RESPONSE_TO_WAKE = readerdisc.GetInt16(1);
                    break;
                case "vCONST_NEUROMEDIATOR_CACHE":
                    s.vCONST_NEUROMEDIATOR_CACHE = readerdisc.GetByte(1);
                    break;
                case "vCONST_NEUROMEDIATOR_LOW":
                    s.vCONST_NEUROMEDIATOR_LOW = readerdisc.GetByte(1);
                    break;
                case "vCONST_FORGOT_RAM_PERCENT":
                    s.vCONST_FORGOT_RAM_PERCENT = readerdisc.GetInt16(1);
                    break;
                case "vCONST_REALIGN_PERCENT":
                    s.vCONST_REALIGN_PERCENT = readerdisc.GetInt16(1);
                    break;
                case "vCONST_AXBRAKE_SPIKE_CHANGE":
                    s.vCONST_AXBRAKE_SPIKE_CHANGE = readerdisc.GetInt16(1);
                    break;

            }
        }

        nn = n;
        service = s;
    }

    ~DNA()
    {
        dbconn.Close();        
        dbmemory.Close();
        dbconn = null;
        dbmemory = null;
    }

    //прочитать из базы, есть ли синапс, соответствующий аксону
    public bool IsAxonToSynapseOne()
    {
        AxonToSyn axsyn=new AxonToSyn();
        if (service.queQueryAxonToSyn.TryDequeue(out axsyn))
        {
            dbcmddisc = dbconn.CreateCommand();
            dbcmddisc.CommandText = "SELECT id FROM SYNAPSES WHERE i=" + nn[axsyn.neuron].axon[axsyn.axon].i.ToString() + " AND j=" + nn[axsyn.neuron].axon[axsyn.axon].j.ToString() + " AND k=" + nn[axsyn.neuron].axon[axsyn.axon].k.ToString();
            readerdisc = dbcmddisc.ExecuteReader();
            if (readerdisc.Read())
            {

                nn[axsyn.neuron].axon[axsyn.axon].v = 1;//это укажет нейрону, что его аксон теперь соединен по крайней мере с одним синапсом                
            }
            else
            {
                nn[axsyn.neuron].axon[axsyn.axon].v = 0;//соединения нет
            }

            return true;//это не значит, что есть, а значит, что прочитали
        }
        return false;//это не значит, что нет, а значит что очередь пуста
    }

    public bool ReadOne() //прочитать из базы первый в очереди запрос \ true - успешно прочитали
    {
        structDNAReadQueue res=new structDNAReadQueue();
        if(service.queQueryToDnaRead.TryDequeue(out res))
        {
            

            dbcmdmem = dbmemory.CreateCommand();
            string sqlQuery = "SELECT id, neuron, pattern, response FROM DNA WHERE neuron="+ res.neuronNumber + " AND pattern=" + res.pattern + " LIMIT 1";
            dbcmdmem.CommandText = sqlQuery;
            readermem= dbcmdmem.ExecuteReader();

            if(readermem.Read())
            {
                //Debug.Log("считано из ДНК значение ответа Нейрона");
                int id= readermem.GetInt32(0);
                int ne = readermem.GetInt32(1);//номер нейрона
                ushort pat = (ushort)readermem.GetInt32(2);//паттерн
                short val = readermem.GetInt16(3);//значение

                //устанавливаем ответ нейрона таким, раз он спрашивал себе свою память
                nn[ne].responses[pat] = val;

                //устанавливаем флаги, что значение есть в ДНК, снимаем флаг запроса на чтение и флаг, что нет записи в ДНК
                nn[ne].responses_DNA_flags[pat] = (byte)(nn[ne].responses_DNA_flags[pat] & 0b11111001);
                readermem.Close();

                //*/УДАЛЯЕМ ЭТО ЗНАЧЕНИЕ ИЗ ДНК. РЕАЛИЗУЕМ ПРИНЦИП "ВСПОМНИЛ? - ЗАБУДЬ и ЗАПИШИ ЗАНОВО"
                dbcmdmem = dbmemory.CreateCommand();
                sqlQuery = "DELETE FROM DNA WHERE id=" + id.ToString();
                dbcmdmem.CommandText = sqlQuery;
                dbcmdmem.ExecuteNonQuery();
                //*/                
            }
            else
            {
                //устанавливаем флаги, что значение нет в ДНК, снимаем флаг запроса на чтение
                nn[res.neuronNumber].responses_DNA_flags[res.pattern] = (byte)((nn[res.neuronNumber].responses_DNA_flags[res.pattern] & 0b11111100) | 0b100);
                //Debug.Log("в ДНК нет ответа на этот патерн Нейрона");
            }
            readermem.Close();
            return true; //читали очередь и базу читали
        }
        return false; //не могу прочитать очередь или в очереди нет ничего
    }

    public bool WriteOne() //записать в базу первый в очереди запрос \ true - успешно записали
    {
        structDNAWriteQueue res = new structDNAWriteQueue();
        if (service.queQueryToDnaWrite.TryDequeue(out res))
        {
            dbcmdmem = dbmemory.CreateCommand();
            string sqlQuery = "INSERT INTO DNA (neuron, pattern, response) VALUES (" +res.neuronNumber.ToString() + ", "+res.pattern.ToString() + ", "+ res.response.ToString() + ") ";
            dbcmdmem.CommandText = sqlQuery;
            int iswrited = dbcmdmem.ExecuteNonQuery();

            if (iswrited>0)//таки записали в ДНК
            {
                //Debug.Log("записано в ДНК значение ответа Нейрона");

                //устанавливаем флаги, что значение есть в ДНК, снимаем флаг запроса на запись и флаг, что нет записи в ДНК
                nn[res.neuronNumber].responses_DNA_flags[res.pattern] = (byte)((nn[res.neuronNumber].responses_DNA_flags[res.pattern] & 0b11110001) | 0b1);
            }
            else
            {
                //Debug.Log("не могу записать значение в ДНК");
            }            
            return true; //читали очередь и в базу писали
        }
        return false; //не могу прочитать очередь или в очереди нет ничего
    }

    public void WriteDebugOne()
    {
        structDNADebugQueue res = new structDNADebugQueue();
        if (service.queQueryDebugWrite.TryDequeue(out res))
        {
            dbcmddisc = dbconn.CreateCommand();
            string sqlQuery = "INSERT INTO DEBUGGING (debugText, debugInt, debugFloat, comment) VALUES ('" + res.dText + "', "
                                                                                + res.dInt + ", "
                                                                                + "'" + res.dFloat + "'" + ", '"
                                                                                + res.dComment + "')";
            dbcmddisc.CommandText = sqlQuery;
            dbcmddisc.ExecuteNonQuery();
        }
    }

    public void Delete(int count)
    {
        dbcmdmem = dbmemory.CreateCommand();
        dbcmdmem.CommandText = "delete from dna where id IN (select id from dna limit "+count.ToString() + " )";
        dbcmdmem.ExecuteNonQuery();
    }

    public void WriteAll() //записать в базу первый в очереди запрос \ true - успешно записали
    {
        dbcmdmem = dbmemory.CreateCommand();
        if (service.queQueryToDnaWrite.Count == 0) return;

        structDNAWriteQueue res = new structDNAWriteQueue();
        string sqlQuery = "INSERT INTO DNA (neuron, pattern, response) VALUES ";

        int cnt = service.queQueryToDnaWrite.Count;
        while (service.queQueryToDnaWrite.Count>0)
        {
            if (service.queQueryToDnaWrite.TryDequeue(out res))
            {
                sqlQuery += "(" + res.neuronNumber.ToString() + ", " + res.pattern.ToString() + ", " + res.response.ToString() + "),";//последнюю запятую удалим позже, не ставь после нее пробел!!!!
            }
            //устанавливаем флаги, что значение есть в ДНК, снимаем флаг запроса на запись и флаг, что нет записи в ДНК
            nn[res.neuronNumber].responses_DNA_flags[res.pattern] = (byte)(nn[res.neuronNumber].responses_DNA_flags[res.pattern] & 0b11110001);
            nn[res.neuronNumber].responses_DNA_flags[res.pattern] = (byte)((nn[res.neuronNumber].responses_DNA_flags[res.pattern] & 0b11110001) | 0b1);

            //старые значения удаляем? Нет! В этой версии значения удаляются только при воспоминании

            
        }

        sqlQuery = sqlQuery.Remove(sqlQuery.Length- 1);
        //Debug.Log(sqlQuery);
        dbcmdmem.CommandText = sqlQuery;
        int iswrited = dbcmdmem.ExecuteNonQuery();

        if (iswrited == 0)
        {
            Debug.Log("не могу записать значение в ДНК по какой-то причине");
        }


    }

    public static NeuroNet Wake()//подъем из базы
    {
        IDbConnection dbconn;
        IDbCommand dbcmd,dbcmd2;
        IDataReader reader,reader2;

        string conn;
        if (Application.isEditor) conn = "URI=file:" + Application.dataPath + "/../Neurons.db";
        else conn = "URI=file:" + Application.dataPath + "/Neurons.db";

        dbconn = (IDbConnection)new SqliteConnection(conn);
        dbconn.Open(); //Open connection to the database.   

        Service s = new Service();
        MAPQueue mq = new MAPQueue();        
        


        dbcmd = dbconn.CreateCommand();
        dbcmd.CommandText = "SELECT rule, lenght, height, width, typeca FROM CA LIMIT 1";
        reader = dbcmd.ExecuteReader();
        reader.Read();
        string rule = reader.GetString(0);
        short le = reader.GetInt16(1);
        short he = reader.GetInt16(2);
        short wi = reader.GetInt16(3);
        int typeca = reader.GetInt32(4);

        CellularAutamata3D cla;
        if (typeca==3)
            cla = new CellularAutamata3D(le,he,wi,rule);
        else
            cla = new CellularAutamata3D(le, he, wi, rule, true);

        reader.Close();

        List<Neuron> nn = new List<Neuron>();

        dbcmd = dbconn.CreateCommand();
        dbcmd.CommandText = "SELECT neuron, typen FROM NEURONS ORDER BY neuron ASC";
        reader = dbcmd.ExecuteReader();
        while (reader.Read())
        {
            int nnumber = reader.GetInt32(0);
            char nt = (char)reader.GetString(1).ToCharArray()[0];

            if (nt == 's')//суммирующий нейрон
            {
                nn.Add(new NeuronSum(nnumber, ref cla, ref mq, ref s));
            }
            else if (nt == 'c')//структурный
            {
                nn.Add(new Neuron(nnumber, ref cla, ref mq, ref s));
            }
            else if (nt == 'd')//суммирующий, дендритопёрдный
            {
                nn.Add(new NeuronDendSpike(nnumber, ref cla, ref mq, ref s));
            }
            else if (nt == 'r')//дифферентный
            {
                nn.Add(new NeuronDiff(nnumber, ref cla, ref mq, ref s));
            }
        }

        dbcmd2 = dbconn.CreateCommand();
        dbcmd2.CommandText = "SELECT neuron,synapse,i,j,k FROM SYNAPSES ORDER BY neuron ASC";
        reader2 = dbcmd2.ExecuteReader();
        while (reader2.Read())
        {
            Coord syn = new Coord(); syn.i = reader2.GetInt16(2); syn.j = reader2.GetInt16(3); syn.k = reader2.GetInt16(4);
            nn[reader2.GetInt32(0)].SetSyn(reader2.GetInt16(1), syn);
        }

        dbcmd2 = dbconn.CreateCommand();
        dbcmd2.CommandText = "SELECT neuron,axon,i,j,k,typea FROM AXONS ORDER BY neuron ASC";
        reader2 = dbcmd2.ExecuteReader();

        while (reader2.Read())
        {
            Coord ax = new Coord(); ax.i = reader2.GetInt16(2); ax.j = reader2.GetInt16(3); ax.k = reader2.GetInt16(4); ax.type = reader2.GetByte(5);
            nn[reader2.GetInt32(0)].SetAx(reader2.GetInt16(1), ax);
        }

        Debug.Log("Считано нейронов: " + nn.Count);

        dbconn.Close();
        dbconn = null;

        DNA dna = new DNA(ref nn, ref s, true); //здесь считывается таблица DNA

        return new NeuroNet(ref cla,ref s,ref mq,ref nn, ref dna);
    }

    public void Sleep(ref CellularAutamata3D ca)//хотим спать
    {
        string sqlQuery;        

        dbcmddisc = dbconn.CreateCommand();
        sqlQuery = "DELETE FROM CA";
        dbcmddisc.CommandText = sqlQuery;
        dbcmddisc.ExecuteNonQuery();

        //пишем в таблицу КА
        dbcmddisc = dbconn.CreateCommand();
        int typeca = ca.is_25_emul ? 25 : 3;
        sqlQuery = "INSERT INTO CA (rule, lenght, height, width, typeca) VALUES ('" + ca.rule.rule + "', " + ca.lenght.ToString() + ", " + ca.height.ToString() + ", " + ca.width.ToString() + ", " + typeca.ToString() + " )";
        dbcmddisc.CommandText = sqlQuery;
        dbcmddisc.ExecuteNonQuery();
        
        dbcmddisc = dbconn.CreateCommand();
        sqlQuery = "DELETE FROM DNA";
        dbcmddisc.CommandText = sqlQuery;
        dbcmddisc.ExecuteNonQuery();

        //консолидация памяти и запись на диск
        bool atleastone = false;
        string query = "INSERT INTO DNA (neuron, pattern, response) VALUES ";
        while (true)
        {
            dbcmdmem = dbmemory.CreateCommand();
            dbcmdmem.CommandText = "SELECT neuron, pattern FROM DNA LIMIT 1";
            readermem = dbcmdmem.ExecuteReader();
            if (readermem.Read())
            {
                int neu = readermem.GetInt32(0);
                int pat = readermem.GetInt32(1);

                dbcmdmem = dbmemory.CreateCommand();
                dbcmdmem.CommandText = "SELECT AVG(response) FROM DNA WHERE neuron=" + neu.ToString() + " AND pattern=" + pat.ToString();
                readermem = dbcmdmem.ExecuteReader();
                if (readermem.Read())
                {
                    atleastone = true;
                    query += "(" + neu.ToString() + ", " + pat.ToString() + ", " + (int)(readermem.GetFloat(0)) + "),";
                }
                dbcmdmem = dbmemory.CreateCommand();
                dbcmdmem.CommandText = "DELETE FROM DNA WHERE neuron=" + neu.ToString() + " AND pattern=" + pat.ToString();
                dbcmdmem.ExecuteNonQuery();
            }
            else
            {
                break;
            }
        }

        //переписываем все из памяти на диск
        if (atleastone)
            using (IDbCommand ic = dbconn.CreateCommand())
            {
                query = query.Remove(query.Length - 1);
                ic.CommandText = query;
                ic.ExecuteNonQuery();
            }
    }

    public void SynapseUpdateOrCreateOne()
    {
        structDNASynAxUpdateQueue res = new structDNASynAxUpdateQueue();
        if (service.queQuerySynWrite.TryDequeue(out res))
        {
            dbcmddisc = dbconn.CreateCommand();
            dbcmddisc.CommandText = "UPDATE SYNAPSES SET i=" + nn[res.n].synapses[res.h].i + ", j=" + nn[res.n].synapses[res.h].j + ", k=" + nn[res.n].synapses[res.h].k + " WHERE neuron=" + res.n + " AND synapse=" + res.h;
            if (dbcmddisc.ExecuteNonQuery() == 0)//если такого синапса нет, мы его добавим
            {
                dbcmddisc = dbconn.CreateCommand();
                dbcmddisc.CommandText = "INSERT INTO SYNAPSES (neuron, synapse,i,j,k) VALUES (" + res.n + ", " + res.h + ", " + nn[res.n].synapses[res.h].i + ", " + nn[res.n].synapses[res.h].j + ", " + nn[res.n].synapses[res.h].k + ")";
                dbcmddisc.ExecuteNonQuery();
            }
        }
    }
    public void AxonUpdateOrCreateOne()
    {
        structDNASynAxUpdateQueue res = new structDNASynAxUpdateQueue();
        if (service.queQueryAxWrite.TryDequeue(out res))
        {
            dbcmddisc = dbconn.CreateCommand();
            dbcmddisc.CommandText = "UPDATE AXONS SET i=" + nn[res.n].axon[res.h].i + ", j=" + nn[res.n].axon[res.h].j + ", k=" + nn[res.n].axon[res.h].k + " WHERE neuron=" + res.n + " AND axon=" + res.h;
            if (dbcmddisc.ExecuteNonQuery() == 0)//если такого нет, мы его добавим
            {
                dbcmddisc = dbconn.CreateCommand();
                dbcmddisc.CommandText = "INSERT INTO AXONS (neuron, axon,i,j,k, typea) VALUES (" + res.n + ", " + res.h + ", " + nn[res.n].axon[res.h].i + ", " + nn[res.n].axon[res.h].j + ", " + nn[res.n].axon[res.h].k + ", " + nn[res.n].axon[res.h].type + ")";
                dbcmddisc.ExecuteNonQuery();
            }
        }
    }

    public void AddNeuron(int neu)
    {
        dbcmddisc = dbconn.CreateCommand();
        dbcmddisc.CommandText = "INSERT INTO NEURONS (neuron, typen) VALUES (" + neu + ", '" + nn[neu].GetTypeNeuron() + "')";
        dbcmddisc.ExecuteNonQuery();

        for (int i = 0; i < 16; i++)
        {
            if (nn[neu].synapses[i].i == 0 && nn[neu].synapses[i].j == 0 && nn[neu].synapses[i].k == 0) break; //далее нет синапсов
            dbcmddisc = dbconn.CreateCommand();
            dbcmddisc.CommandText = "INSERT INTO SYNAPSES (neuron, synapse,i,j,k) VALUES (" + neu + ", " + i + ", " + nn[neu].synapses[i].i + ", " + nn[neu].synapses[i].j + ", " + nn[neu].synapses[i].k + ")";
            dbcmddisc.ExecuteNonQuery();
        }
        for (int i = 0; i < 16; i++)
        {
            if (nn[neu].axon[i].i == 0 && nn[neu].axon[i].j == 0 && nn[neu].axon[i].k == 0) break; //далее нет 
            dbcmddisc = dbconn.CreateCommand();
            dbcmddisc.CommandText = "INSERT INTO AXONS (neuron, axon,i,j,k, typea) VALUES (" + neu + ", " + i + ", " + nn[neu].axon[i].i + ", " + nn[neu].axon[i].j + ", " + nn[neu].axon[i].k + ", " + nn[neu].axon[i].type + ")";
            dbcmddisc.ExecuteNonQuery();
        }

    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mono.Data.Sqlite;
using System.Data;
using CellularAutamata;
using UnityEngine.UI;

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
    IDbCommand dbcmd;
    IDataReader reader;

    List<Neuron> nn;        //сетка нейронов (ну, или не сетка)))
    Service service;
   
    public DNA(ref List<Neuron> n, ref Service s, bool usebase=false)
    {
        string conn = "URI=file:" + Application.dataPath + "/Neurons.db";
        
        dbconn = (IDbConnection)new SqliteConnection(conn);
        dbconn.Open(); //Open connection to the database.   

        //переносим базу в память
        dbmemory = new SqliteConnection("URI=file::memory:,version=3");
        dbmemory.Open();
        dbcmd=dbmemory.CreateCommand();
        dbcmd.CommandText = "CREATE TABLE DNA (id INTEGER PRIMARY KEY UNIQUE NOT NULL, neuron INTEGER NOT NULL, pattern  INTEGER NOT NULL, response INTEGER NOT NULL);";
        dbcmd.ExecuteNonQuery();


        using (IDbCommand ic= dbmemory.CreateCommand())
        {
            //есть ли чего-то в ДНК?
            dbcmd = dbconn.CreateCommand();
            dbcmd.CommandText = "SELECT count(id) FROM DNA";
            int count = System.Convert.ToInt32(dbcmd.ExecuteScalar());
            if (count > 0)
            {
                dbcmd = dbconn.CreateCommand();
                dbcmd.CommandText = "select neuron, pattern, response from dna order by response desc";//самые сильные ответы идут первыми
                reader = dbcmd.ExecuteReader();
                string query = "INSERT INTO DNA (neuron, pattern, response) VALUES ";
                int ne;
                ushort pa;
                short re;
                while (reader.Read())
                {
                    ne = reader.GetInt32(0);
                    pa = (ushort)reader.GetInt32(1);
                    re = reader.GetInt16(2);
                    query += "(" + ne.ToString() + ", " + pa.ToString() + ", " + re.ToString() + "),";
                    //заполняем значениями ответов из ДНК только 1% случайных нейронов (остальное пусть вспоминает, если надо)
                    if ((usebase && Random.Range(0,100)>98) || re>=Service.CONST_DNA_RESPONSE_TO_WAKE)//если же ответ большой, он сразу попадает
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

    public bool ReadOne() //прочитать из базы первый в очереди запрос \ true - успешно прочитали
    {
        structDNAReadQueue res=new structDNAReadQueue();
        if(service.queQueryToDnaRead.TryDequeue(out res))
        {
            dbcmd = dbmemory.CreateCommand();
            string sqlQuery = "SELECT id, neuron, pattern, response FROM DNA WHERE neuron="+ res.neuronNumber.ToString() + " AND pattern=" + res.pattern.ToString() + " LIMIT 1";
            dbcmd.CommandText = sqlQuery;
            reader= dbcmd.ExecuteReader();

            if(reader.Read())
            {
                //Debug.Log("считано из ДНК значение ответа Нейрона");
                int id= reader.GetInt32(0);
                ushort ne = (ushort)reader.GetInt32(1);//номер нейрона
                ushort pat = (ushort)reader.GetInt32(2);//паттерн
                short val = (short)reader.GetInt32(3);//значение

                //устанавливаем ответ нейрона таким, раз он спрашивал себе свою память
                nn[ne].responses[pat] = val;

                //устанавливаем флаги, что значение есть в ДНК, снимаем флаг запроса на чтение и флаг, что нет записи в ДНК
                nn[ne].responses_DNA_flags[pat] = (byte)(nn[ne].responses_DNA_flags[pat] & 0b11111001);

                //УДАЛЯЕМ ЭТО ЗНАЧЕНИЕ ИЗ ДНК. РЕАЛИЗУЕМ ПРИНЦИП "ВСПОМНИЛ? - ЗАБУДЬ и ЗАПИШИ ЗАНОВО"
                reader.Close();

                dbcmd = dbmemory.CreateCommand();
                sqlQuery = "DELETE FROM DNA WHERE id=" + id;
                dbcmd.CommandText = sqlQuery;
                dbcmd.ExecuteNonQuery();

                Debug.Log("Считано - удалено из ДНК");
            }
            else
            {
                //устанавливаем флаги, что значение нет в ДНК, снимаем флаг запроса на чтение
                nn[res.neuronNumber].responses_DNA_flags[res.pattern] = (byte)((nn[res.neuronNumber].responses_DNA_flags[res.pattern] & 0b11111100) | 0b100);
                //Debug.Log("в ДНК нет ответа на этот патерн Нейрона");
            }
            reader.Close();
            return true; //читали очередь и базу читали
        }
        return false; //не могу прочитать очередь или в очереди нет ничего
    }

    public bool WriteOne() //записать в базу первый в очереди запрос \ true - успешно записали
    {
        structDNAWriteQueue res = new structDNAWriteQueue();
        if (service.queQueryToDnaWrite.TryDequeue(out res))
        {
            dbcmd = dbmemory.CreateCommand();
            string sqlQuery = "INSERT INTO DNA (neuron, pattern, response) VALUES (" +res.neuronNumber+", "+res.pattern+", "+ res.response+") ";
            dbcmd.CommandText = sqlQuery;
            int iswrited = dbcmd.ExecuteNonQuery();

            if (iswrited>0)//таки записали в ДНК
            {
                //Debug.Log("записано в ДНК значение ответа Нейрона");

                //устанавливаем флаги, что значение есть в ДНК, снимаем флаг запроса на запись и флаг, что нет записи в ДНК
                nn[res.neuronNumber].responses_DNA_flags[res.pattern] = (byte)((nn[res.neuronNumber].responses_DNA_flags[res.pattern] & 0b11110001) | 0b1);

                if (Random.Range(0, 30) > 27) Delete(1);//удаляем из памяти младшее молодое значение с вероятностью 6% - это часть алгоритма забывания в ДНК
            }
            else
            {
                //Debug.Log("не могу записать значение в ДНК");
            }            
            return true; //читали очередь и в базу писали
        }
        return false; //не могу прочитать очередь или в очереди нет ничего
    }

    public void WriteDebug(string debugText="", int debugInt=0, float debugFloat = 0, string comment="")
    {
        dbcmd = dbconn.CreateCommand();
        string sqlQuery = "INSERT INTO DEBUGGING (debugText, debugInt, debugFloat, comment) VALUES ('" + debugText + "', "
                                                                            + debugInt.ToString() + ", " 
                                                                            + debugFloat.ToString() + ", '"
                                                                            + comment + "')";
        dbcmd.CommandText = sqlQuery;
        dbcmd.ExecuteNonQuery();
    }

    public void Delete(int count)
    {
        dbcmd = dbmemory.CreateCommand();
        dbcmd.CommandText = "delete from dna where id IN (select id from dna limit "+count+" )";
        dbcmd.ExecuteNonQuery();
    }

    public void WriteAll() //записать в базу первый в очереди запрос \ true - успешно записали
    {
        dbcmd = dbmemory.CreateCommand();
        if (service.queQueryToDnaWrite.Count == 0) return;

        structDNAWriteQueue res = new structDNAWriteQueue();
        string sqlQuery = "INSERT INTO DNA (neuron, pattern, response) VALUES ";

        int cnt = service.queQueryToDnaWrite.Count;
        while (service.queQueryToDnaWrite.Count>0)
        {
            if (service.queQueryToDnaWrite.TryDequeue(out res))
            {
                sqlQuery += "(" + res.neuronNumber + ", " + res.pattern + ", " + res.response+"),";//последнюю запятую удалим позже, не ставь после нее пробел!!!!
            }
            //устанавливаем флаги, что значение есть в ДНК, снимаем флаг запроса на запись и флаг, что нет записи в ДНК
            nn[res.neuronNumber].responses_DNA_flags[res.pattern] = (byte)(nn[res.neuronNumber].responses_DNA_flags[res.pattern] & 0b11110001);
            nn[res.neuronNumber].responses_DNA_flags[res.pattern] = (byte)((nn[res.neuronNumber].responses_DNA_flags[res.pattern] & 0b11110001) | 0b1);

            //старые значения удаляем? Нет! В этой версии значения удаляются только при воспоминании

            
        }

        sqlQuery = sqlQuery.Remove(sqlQuery.Length- 1);
        //Debug.Log(sqlQuery);
        dbcmd.CommandText = sqlQuery;
        int iswrited = dbcmd.ExecuteNonQuery();

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

        string conn = "URI=file:" + Application.dataPath + "/Neurons.db";

        dbconn = (IDbConnection)new SqliteConnection(conn);
        dbconn.Open(); //Open connection to the database.   

        Service s = new Service();
        MAPQueue mq = new MAPQueue();        
        List<Neuron> nn = new List<Neuron>();


        dbcmd = dbconn.CreateCommand();
        dbcmd.CommandText = "SELECT rule, lenght, height, width FROM CA LIMIT 1";
        reader = dbcmd.ExecuteReader();
        reader.Read();
        string rule = reader.GetString(0);
        short le = reader.GetInt16(1);
        short he = reader.GetInt16(2);
        short wi = reader.GetInt16(3);

        CellularAutamata3D cla = new CellularAutamata3D(le,he,wi,rule);
        reader.Close();

        dbcmd = dbconn.CreateCommand();
        dbcmd.CommandText = "SELECT neuron, typen FROM NEURONS ORDER BY neuron ASC";
        reader = dbcmd.ExecuteReader();
        while (reader.Read())
        {
            short nnumber = (short)reader.GetInt32(0);
            char nt = (char)reader.GetString(1).ToCharArray()[0];

            Coord[] syns = new Coord[16];
            Coord[] axs = new Coord[16];

            dbcmd2 = dbconn.CreateCommand();
            dbcmd2.CommandText = "SELECT synapse,i,j,k FROM SYNAPSES WHERE neuron="+nnumber.ToString()+" ORDER BY synapse ASC";
            reader2 = dbcmd2.ExecuteReader();
            int i = 0;
            while (reader2.Read())
            {
                syns[i].i = reader2.GetInt16(1);
                syns[i].j = reader2.GetInt16(2);
                syns[i].k = reader2.GetInt16(3);
                i++;
            }

            dbcmd2 = dbconn.CreateCommand();
            dbcmd2.CommandText = "SELECT axon,i,j,k FROM AXONS WHERE neuron=" + nnumber.ToString() + " ORDER BY axon ASC";
            reader2 = dbcmd2.ExecuteReader();
            i = 0;
            while (reader2.Read())
            {
                axs[i].i = reader2.GetInt16(1);
                axs[i].j = reader2.GetInt16(2);
                axs[i].k = reader2.GetInt16(3);
                i++;
            }

            if (nt=='s')//суммирующий нейрон
            {
                nn.Add(new NeuronSum(nnumber, ref cla, syns, axs, ref mq, ref s));
            }
            else if(nt=='c')//структурный
            {
                nn.Add(new Neuron(nnumber, ref cla, syns, axs, ref mq, ref s));
            }
            else if (nt == 'd')//суммирующий, дендритопёрдный
            {
                nn.Add(new NeuronDendSpike(nnumber, ref cla, syns, axs, ref mq, ref s));
            }

        }

        Debug.Log("Считано нейронов: " + nn.Count);

        dbconn.Close();
        dbconn = null;

        DNA dna = new DNA(ref nn, ref s, true); //здесь считывается таблица DNA

        return new NeuroNet(ref cla,ref s,ref mq,ref nn, ref dna);
    }

    public void Sleep(ref CellularAutamata3D ca)//хотим спать
    {
        //надо сохранить все нейроны со всеми синапсами и выходами и все их положительные ответы

        //сначала очистим таблицы
        string sqlQuery;

        dbcmd = dbconn.CreateCommand();
        sqlQuery = "DELETE FROM SYNAPSES";
        dbcmd.CommandText = sqlQuery;
        dbcmd.ExecuteNonQuery();

        dbcmd = dbconn.CreateCommand();
        sqlQuery = "DELETE FROM NEURONS";
        dbcmd.CommandText = sqlQuery;
        dbcmd.ExecuteNonQuery();

        dbcmd = dbconn.CreateCommand();
        sqlQuery = "DELETE FROM AXONS";
        dbcmd.CommandText = sqlQuery;
        dbcmd.ExecuteNonQuery();

        dbcmd = dbconn.CreateCommand();
        sqlQuery = "DELETE FROM CA";
        dbcmd.CommandText = sqlQuery;
        dbcmd.ExecuteNonQuery();

        //пишем в таблицу КА
        dbcmd = dbconn.CreateCommand();
        sqlQuery = "INSERT INTO CA (rule, lenght, height, width) VALUES ('" + ca.rule.rule+"', "+ca.lenght+", "+ ca.height+", "+ca.width +" )";
        dbcmd.CommandText = sqlQuery;
        dbcmd.ExecuteNonQuery();

        //пишем в таблицу синапсы
        dbcmd = dbconn.CreateCommand();
        sqlQuery = "INSERT INTO SYNAPSES (neuron, synapse, i,j,k) VALUES ";
        foreach (Neuron n in nn)
        {
            for(int i=0;i<16;i++)
            {
                if (n.synapses[i].i == 0 && n.synapses[i].j == 0 && n.synapses[i].k == 0) break; //далее нет синапсов

                sqlQuery += "(" + n.number + ", " + i + ", " + n.synapses[i].i + ", " + n.synapses[i].j + ", " + n.synapses[i].k + "),";
            }
        }
        sqlQuery = sqlQuery.Remove(sqlQuery.Length-1);
        dbcmd.CommandText = sqlQuery;
        dbcmd.ExecuteNonQuery();

        //пишем в таблицу аксоны
        dbcmd = dbconn.CreateCommand();
        sqlQuery = "INSERT INTO AXONS (neuron, axon, i,j,k) VALUES ";
        foreach (Neuron n in nn)
        {
            for (int i = 0; i < 16; i++)
            {
                if (n.axon[i].i == 0 && n.axon[i].j == 0 && n.axon[i].k == 0) break; //далее нет аксонов

                sqlQuery += "(" + n.number + ", " + i + ", " + n.axon[i].i + ", " + n.axon[i].j + ", " + n.axon[i].k + "),";
            }
        }
        sqlQuery = sqlQuery.Remove(sqlQuery.Length - 1);
        dbcmd.CommandText = sqlQuery;
        dbcmd.ExecuteNonQuery();

        //пишем в таблицу нейроны
        dbcmd = dbconn.CreateCommand();
        sqlQuery = "INSERT INTO NEURONS (neuron, typen) VALUES ";
        foreach (Neuron n in nn)
        {
            sqlQuery += "(" + n.number + ", '"+n.GetTypeNeuron() + "'),";            
        }
        sqlQuery = sqlQuery.Remove(sqlQuery.Length - 1);
        dbcmd.CommandText = sqlQuery;
        dbcmd.ExecuteNonQuery();

        dbcmd = dbconn.CreateCommand();
        sqlQuery = "DELETE FROM DNA";
        dbcmd.CommandText = sqlQuery;
        dbcmd.ExecuteNonQuery();

        //консолидация памяти и запись на диск
        bool atleastone = false;
        string query = "INSERT INTO DNA (neuron, pattern, response) VALUES ";
        while (true)
        {
            dbcmd = dbmemory.CreateCommand();
            dbcmd.CommandText = "SELECT neuron, pattern FROM DNA LIMIT 1";            
            reader = dbcmd.ExecuteReader();
            if (reader.Read())
            {
                int neu = reader.GetInt32(0);
                int pat = reader.GetInt32(1);

                dbcmd = dbmemory.CreateCommand();
                dbcmd.CommandText = "SELECT AVG(response) FROM DNA WHERE neuron=" + neu.ToString() + " AND pattern=" + pat.ToString();
                reader = dbcmd.ExecuteReader();
                if (reader.Read())
                {
                    atleastone = true;
                    query += "(" + neu.ToString() + ", " + pat.ToString() + ", " + (int)(reader.GetFloat(0)) + "),";
                }
                dbcmd = dbmemory.CreateCommand();
                dbcmd.CommandText = "DELETE FROM DNA WHERE neuron=" + neu.ToString() + " AND pattern=" + pat.ToString();
                dbcmd.ExecuteNonQuery();
            }
            else
            {
                break;
            }
        }

        //переписываем все из памяти на диск
        if(atleastone)
        using (IDbCommand ic = dbconn.CreateCommand())
        {
            query = query.Remove(query.Length - 1);
            ic.CommandText = query;
            ic.ExecuteNonQuery();            
        }


    }
}

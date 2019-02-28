using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Linq;
using System.Threading;

namespace CellularAutamata
{ 
	public struct Rule
	{
        public short[] r_alive;
        public short[] r_born;
        public short be_yunger;
        public short max_age;

        public string rule { get; }

        internal Rule(string r)
		{
            rule = r;    
			string[] s = r.Split('/');

            //правила выживания
            MatchCollection matches = Regex.Matches(s[0], @"\d+");
            r_alive = new short[matches.Count - 1];
            for(short i =0;i<matches.Count-1;i++)
            {
                r_alive[i] = short.Parse( matches[i].Groups[0].Value);
            }
            //в квадратных скобках последнее найденное число - насколько молодеть
			be_yunger= short.Parse(matches[matches.Count - 1].Groups[0].Value);

            //правила рождения
            matches = Regex.Matches(s[1], @"\d+");
            r_born = new short[matches.Count];
            for (short i = 0; i < matches.Count; i++)
            {
                r_born[i] = short.Parse(matches[i].Groups[0].Value);
            }

            //максимальный возраст
            matches = Regex.Matches(s[2], @"\d+");
            max_age = short.Parse(matches[0].Groups[0].Value);

        }

        public bool CheckAlive(short v)
        {
            if (r_alive.Contains(v)) return true;
            return false;
        }
        public bool CheckBorn(short v)
        {
            if (r_born.Contains(v)) return true;
            return false;
        }
    }

    public abstract class CellularAutamata3DBase
    {
        public short lenght;
        public short height;
        public short width;

        public short[,,] cell;

        public const int const_max_age = 36;
        public Rule rule;

        public abstract short ChangeAge(short i, short j, short k, short val);
        public abstract void NextStep();
    }

    public class CellularAutamata3DEasy: CellularAutamata3DBase
    {

        //конструктор длина,ширина,высота коробки, правило эволюции
        public CellularAutamata3DEasy(short l, short h, short w, string r)
        {
            lenght = l;
            height = h;
            width = w;

            cell = new short[l, h, w];
            rule = new Rule(r);//заглушка
            
        }

        //меняем в заданной клетке возвраст руками
        //возвращаем 0 если удачно
        public override short ChangeAge(short i, short j, short k, short val)
        {
            cell[i, j, k] = val;
            if (val == 0) return 0;

            if (i > 0 && i < (lenght - 1) && j > 0 && j < (height - 1) && k > 0 && k < (width - 1))
                for (int ii=i-1;ii<=i+1;ii++)
                    for (int jj = j - 1; jj <= j + 1; jj++)
                        for (int kk = k - 1; kk <= k + 1; kk++)
                        {
                            if (!(ii == i && jj == j && kk == k))//не сама клетка
                            {
                                cell[ii, jj, kk] = const_max_age;
                                
                            }
                        }
            else
                for (int ii = i - 1; ii <= i + 1; ii++)
                {
                    for (int jj = j - 1; jj <= j + 1; jj++)
                    {
                        for (int kk = k - 1; kk <= k + 1; kk++)
                        {
                            int iii = ii;
                            int jjj = jj;
                            int kkk = kk;
                            if (!(ii == i && jj == j && kk == k))//не сама клетка
                            {
                                if (ii < 0) iii = lenght - 1;//если вышли за границу, берем противоположную стенку
                                else if (ii >= lenght) iii = 0;

                                if (jj < 0) jjj = height - 1;
                                else if (jj >= height) jjj = 0;

                                if (kk < 0) kkk = width - 1;
                                else if (kk >= width) kkk = 0;

                                cell[iii, jjj, kkk] = const_max_age;
                                
                            }
                        }
                    }
                }
            return 0;

        }

        public override void NextStep()
        {
            //строим новый массив значений
            short[,,] next = new short[lenght, height, width];
            for (short i = 0; i < lenght; i++)
            {
                for (short j = 0; j < height; j++)
                {
                    for (short k = 0; k < width; k++)
                    {
                        if (cell[i, j, k] > 0)
                        {//cтареет
                            next[i, j, k] = (short)(cell[i, j, k] + 1);
                            //умирает ибо состарилась
                            if (next[i, j, k] > const_max_age)
                            {
                                next[i, j, k] = 0;
                            }
                        }
                    }
                }
            }
            
            cell = next;//и теперь массив ссылается сюда 
        }

    }

    public class CellularAutamata3D : CellularAutamata3DBase
    {
        Thread myThread;
        private readonly object syncObject = new object();

        public FixedSizedQueueConcurent<Coord> queChangeAge;

        //эмулировать 2.5D КА?
        public bool is_25_emul = false;

        //конструктор длина,ширина,высота коробки, правило эволюции
        public CellularAutamata3D(short l, short h, short w, string r, bool is_25=false)
		{
			lenght = l;
			height = h;
			width = w;

            is_25_emul = is_25;

            cell = new short[l,h,w];
            //for (short i = 0; i < l; i++) for (short j = 0; j < h; j++) for (short k = 0; k < w; k++) cell[i, j, k] = 0;
			rule = new Rule(r);

            queChangeAge = new FixedSizedQueueConcurent<Coord>(10000);

            myThread = new Thread(new ThreadStart(NextStepThread));
           
        }

        //функция потока чтения очереди на запись значений в ячейки
        public void queChangeAgeFunc()
        {
            if (!stepfinished) return;
            if (queChangeAge.Count > 0)//пока общитывали коробку, нейроны напердели что-то, будем разгребать
            {
                Coord coo = new Coord();
                while (queChangeAge.Count > 0)
                {
                    if (!stepfinished) return;
                    if (queChangeAge.TryDequeue(out coo))
                        cell[coo.i, coo.j, coo.k] = (short)coo.v;
                }
            }
        }

        //меняем в заданной клетке возвраст руками
        //возвращаем 0 если удачно
        public override short ChangeAge(short i, short j, short k, short val)
        {
            //lock (syncObject)//для мультитрединга
            if (stepfinished)//если прошел персчет значений по всему автомату - можем обновлять прямо
            {
                cell[i, j, k] = val;
                
            }
            else
            {
                Coord coo = new Coord(); coo.i = i; coo.j = j; coo.k = k; coo.v = (ushort)val;
                queChangeAge.Enqueue(coo);
            }
            return 0;
        }

        public void ChangeAgeFast(short i, short j, short k, short val)
        {
           
                cell[i, j, k] = val;
           
        }

        public short ChangeAgeByNeuron(short i, short j, short k, short val)//нейрон прыскает сюда нейромедиатор
        {
            System.Random getrandom = new System.Random();
            //выбираем случайного соседа, куда он немного разольется
            int ii = getrandom.Next(-1, 2);
            int jj = getrandom.Next(-1, 2);
            int kk = getrandom.Next(-1, 2);
            if (ii == 0 && jj == 0 && kk == 0) kk = 1;//чтобы не сама клетка
            ii = i + ii; jj = j + jj; kk = k + kk;
            if (ii < 0) ii = lenght + ii;
            if (jj < 0) jj = height + jj;
            if (kk < 0) kk = width + kk;
            if (ii > lenght-1) ii = lenght - ii;
            if (jj > height - 1) jj = height - jj;
            if (kk > width - 1) kk = width - kk;

            //lock (syncObject)//для мультитрединга
            if(stepfinished)//если прошел персчет значений по всему автомату - можем обновлять прямо
            {
                cell[i, j, k] = val;
                cell[ii, jj, kk] = (short)(rule.max_age - 1);//а в случайного соседа немного нейромедиатора (хватит на один цикл КА)
                queChangeAgeFunc();
            }
            else//сейчас идет обновление значений по всему автомату - добавим в очередь на пересчет
            {
                Coord coo = new Coord(); coo.i = i;coo.j = j;coo.k = k;coo.v = (ushort)val;
                queChangeAge.Enqueue(coo);
                Coord coo2 = new Coord(); coo2.i = i; coo2.j = j; coo2.k = k; coo2.v = (ushort)(rule.max_age - 1);
                queChangeAge.Enqueue(coo2);

            }
            return 0;
        }
        public override void NextStep()
        {
            
            if (!myThread.IsAlive)
            {                
                if(is_25_emul)
                    myThread = new Thread(new ThreadStart(NextStep25Thread));
                else
                    myThread = new Thread(new ThreadStart(NextStepThread));

                myThread.Start();
            }
            
        }
        bool stepfinished=false;
        public void NextStepThread()
        {
            
            stepfinished = false;            
            //строим новый массив значений
            short[,,] next = new short[lenght, height, width];
            for(short i=0;i<lenght;i++)
            {
                for(short j=0;j<height;j++)
                {
                    for (short k = 0; k < width;k++)//1;-1 - потому что там входы микрофона
                    {
                        byte n = CountNeigh25(i, j, k);//кол-во живых соседей
                        if (rule.CheckBorn(n) && cell[i, j, k] == 0)//может родиться только если мертвая сейчас!!!!
                        {//клетка рождается
                            next[i, j, k] = 1;
                        }
                        else if (rule.CheckAlive(n) && cell[i, j, k] != 0)//может выжить только если живет сейчас!!!
                        {//остается жить, если жила до этого, и если в правиле есть молодение - молодеет
                            if (cell[i, j, k] > 0)
                            {
                                next[i, j, k] = (short)(cell[i, j, k] - rule.be_yunger);
                                if (next[i, j, k] < 1) next[i, j, k] = 1;//моложе 1 года быть не может
                            }
                        }
                        else if (cell[i, j, k] > 0)
                        {//cтареет
                            next[i, j, k] = (short)(cell[i, j, k] + 1);
                            //умирает ибо состарилась
                            if (next[i, j, k] > rule.max_age)
                            {
                                next[i, j, k] = 0;
                            }
                        }
                        else next[i, j, k] = 0; //если этого не сделать, что попадет в next[i, j, k]??
                    }
                }
            }
            //lock (syncObject)//для мультитрединга
            {
                for (short i = 0; i < lenght; i++)
                    for (short j = 0; j < height; j++)
                    { next[i, j, 0] = cell[i, j, 0]; next[i, j, width-1] = cell[i, j, width - 1]; } //пока обновляли коробку, данные с микрофона могли измениться

                cell = next;//и теперь массив ссылается сюда 
            }           

            stepfinished = true;
            queChangeAgeFunc();
        }

        private byte CountNeigh(short i,short j,short k)
        {
            byte neigh = 0;
            //если внутренняя клетка
            if(i>0 && i<(lenght-1) && j>0 && j<(height-1) && k>0 && k<(width-1))
            {
                for (int ii= (i-1); ii<=(i+1);ii++)
                {
                    for(int jj=(j-1);jj<=(j+1);jj++)
                    {
                        for(int kk=(k-1);kk<=(k+1);kk++)
                        {
                            if(!(ii==i && jj==j && kk==k))//не сама клетка
                            {
                                if (cell[ii, jj, kk] > 0) neigh++;
                            }
                        }
                    }
                }
            }
            else //граница
            {
                for (int ii = i - 1; ii <= i + 1; ii++)
                {
                    for (int jj = j - 1; jj <= j + 1; jj++)
                    {
                        for (int kk = k - 1; kk <= k + 1; kk++)
                        {
                            int iii = ii;
                            int jjj = jj;
                            int kkk = kk;
                            if (!(ii == i && jj == j && kk == k))//не сама клетка
                            {
                                if (ii < 0) iii = lenght - 1;//если вышли за границу, берем противоположную стенку
                                else if (ii >= lenght) iii = 0;

                                if (jj < 0) jjj = height - 1;
                                else if (jj >= height) jjj = 0;

                                if (kk < 0) kkk = width - 1;
                                else if (kk >= width) kkk = 0;

                                if (cell[iii, jjj, kkk] > 0) neigh++;
                            }
                        }
                    }
                }

            }
            return neigh;
        }

        public void NextStep25Thread()
        {

            stepfinished = false;
            //строим новый массив значений
            short[,,] next = new short[lenght, height, width];
            for (short i = 0; i < lenght; i++)//i -номер слоя, lenght-1 слоев
            {
                //здесь можно запустить потоки на каждый слой, потому что значения клеток в соседних слоях не учитываются
                //тогда эти нижние циклы в функцию потока засунуть. Мы пока одним потоком посчитаем
                for (short j = 0; j < height; j++)
                {
                    for (short k = 0; k < width; k++)
                    {
                        byte n = CountNeigh25(i, j, k);//кол-во живых соседей
                        if (rule.CheckBorn(n) && cell[i,j,k]==0)//может родиться только если мертвая сейчас!!!!
                        {//клетка рождается
                            next[i, j, k] = 1;
                        }
                        else if (rule.CheckAlive(n) && cell[i, j, k] != 0)//может выжить только если живет сейчас!!!
                        {//остается жить, если жила до этого, и если в правиле есть молодение - молодеет
                            if (cell[i, j, k] > 0)
                            {
                                next[i, j, k] = (short)(cell[i, j, k] - rule.be_yunger);
                                if (next[i, j, k] < 1) next[i, j, k] = 1;//моложе 1 года быть не может
                            }
                        }
                        else if (cell[i, j, k] > 0)
                        {//cтареет
                            next[i, j, k] = (short)(cell[i, j, k] + 1);
                            //умирает ибо состарилась
                            if (next[i, j, k] > rule.max_age)
                            {
                                next[i, j, k] = 0;
                            }
                        }
                        else next[i, j, k] = 0; //если этого не сделать, что попадет в next[i, j, k]??
                    }
                }
            }
            //lock (syncObject)//для мультитрединга
            {
                for (short i = 0; i < lenght; i++)
                    for (short j = 0; j < height; j++)
                    { next[i, j, 0] = cell[i, j, 0]; next[i, j, width - 1] = cell[i, j, width - 1]; } //пока обновляли коробку, данные с микрофона могли измениться

                cell = next;//и теперь массив ссылается сюда 
            }

            stepfinished = true;
            queChangeAgeFunc();
        }

        //в отличии от 3Д, здесь всего 8 соседей и i - по сути номер слоя
        private byte CountNeigh25(short i, short j, short k)
        {
            byte neigh = 0;
            //если внутренняя клетка
            if (i > 0 && i < (lenght - 1) && j > 0 && j < (height - 1) && k > 0 && k < (width - 1))
            {
                //for (int ii = (i - 1); ii <= (i + 1); ii++)  //экономия цикла на лицо!  
                {
                    for (int jj = (j - 1); jj <= (j + 1); jj++)
                    {
                        for (int kk = (k - 1); kk <= (k + 1); kk++)
                        {
                            if (!(jj == j && kk == k))//не сама клетка
                            {
                                if (cell[i, jj, kk] > 0) neigh++;
                            }
                        }
                    }
                }
            }
            else //граница
            {
                //for (int ii = i - 1; ii <= i + 1; ii++)//экономия цикла на лицо!  
                {
                    for (int jj = j - 1; jj <= j + 1; jj++)
                    {
                        for (int kk = k - 1; kk <= k + 1; kk++)
                        {

                            int jjj = jj;
                            int kkk = kk;
                            if (!(jj == j && kk == k))//не сама клетка
                            {
                                if (jj < 0) jjj = height - 1;//если вышли за границу, берем противоположную стенку
                                else if (jj >= height) jjj = 0;

                                if (kk < 0) kkk = width - 1;
                                else if (kk >= width) kkk = 0;

                                if (cell[i, jjj, kkk] > 0) neigh++;
                            }
                        }
                    }
                }

            }
            return neigh;
        }

    }

    //класс 2,5-мерный КА. Пересчет ведется по слоям. Слои не перемешиваются между собой. В системе lenght-1 слоев
    //нейроны не знают о слоях, они просто работают со своими синапсами
    public class CellularAutamata25D : CellularAutamata3DBase
    {
        Thread myThread;
        private readonly object syncObject = new object();

        public FixedSizedQueueConcurent<Coord> queChangeAge;

        //конструктор длина,ширина,высота коробки, правило эволюции
        public CellularAutamata25D(short l, short h, short w, string r)
        {
            lenght = l;
            height = h;
            width = w;

            cell = new short[l, h, w];
            
            rule = new Rule(r);

            queChangeAge = new FixedSizedQueueConcurent<Coord>(10000);

            myThread = new Thread(new ThreadStart(NextStepThread));

        }

        //функция потока чтения очереди на запись значений в ячейки
        public void queChangeAgeFunc()
        {
            if (!stepfinished) return;
            if (queChangeAge.Count > 0)//пока общитывали коробку, нейроны напердели что-то, будем разгребать
            {
                Coord coo = new Coord();
                while (queChangeAge.Count > 0)
                {
                    if (!stepfinished) return;
                    if (queChangeAge.TryDequeue(out coo))
                        cell[coo.i, coo.j, coo.k] = (short)coo.v;
                }
            }
        }

        //меняем в заданной клетке возвраст руками
        //возвращаем 0 если удачно
        public override short ChangeAge(short i, short j, short k, short val)
        {
            if (val < 0) return -1;
            else if (val > rule.max_age) return 1;
            else if (i < 0 || j < 0 || k < 0) return -2;
            else if (i >= lenght || j >= height || k >= width) return 2;

            //lock (syncObject)//для мультитрединга
            if (stepfinished)//если прошел персчет значений по всему автомату - можем обновлять прямо
            {
                cell[i, j, k] = val;
                queChangeAgeFunc();
            }
            else
            {
                Coord coo = new Coord(); coo.i = i; coo.j = j; coo.k = k; coo.v = (ushort)val;
                queChangeAge.Enqueue(coo);
            }
            return 0;
        }

        //функция не гарантирует попадания данных на следующем цыкле в автомат, но если нужно быстро, что поделать?
        public void ChangeAgeFast(short i, short j, short k, short val)
        {

            cell[i, j, k] = val;

        }

        public short ChangeAgeByNeuron(short i, short j, short k, short val)//нейрон прыскает сюда нейромедиатор
        {
            System.Random getrandom = new System.Random();
            //выбираем случайного соседа, куда он немного разольется
            int ii = getrandom.Next(-1, 2);
            int jj = getrandom.Next(-1, 2);
            int kk = getrandom.Next(-1, 2);
            if (ii == 0 && jj == 0 && kk == 0) kk = 1;//чтобы не сама клетка
            ii = i + ii; jj = j + jj; kk = k + kk;
            if (ii < 0) ii = lenght + ii;
            if (jj < 0) jj = height + jj;
            if (kk < 0) kk = width + kk;
            if (ii > lenght - 1) ii = lenght - ii;
            if (jj > height - 1) jj = height - jj;
            if (kk > width - 1) kk = width - kk;

            //lock (syncObject)//для мультитрединга
            if (stepfinished)//если прошел персчет значений по всему автомату - можем обновлять прямо
            {
                cell[i, j, k] = val;
                cell[ii, jj, kk] = (short)(rule.max_age - 1);//а в случайного соседа немного нейромедиатора (хватит на один цикл КА)
                queChangeAgeFunc();
            }
            else//сейчас идет обновление значений по всему автомату - добавим в очередь на пересчет
            {
                Coord coo = new Coord(); coo.i = i; coo.j = j; coo.k = k; coo.v = (ushort)val;
                queChangeAge.Enqueue(coo);
                Coord coo2 = new Coord(); coo2.i = i; coo2.j = j; coo2.k = k; coo2.v = (ushort)(rule.max_age - 1);
                queChangeAge.Enqueue(coo2);

            }
            return 0;
        }
        public override void NextStep()
        {

            if (!myThread.IsAlive)
            {
                myThread = new Thread(new ThreadStart(NextStepThread));
                myThread.Start();
            }

        }
        bool stepfinished = false;
        public void NextStepThread()
        {

            stepfinished = false;
            //строим новый массив значений
            short[,,] next = new short[lenght, height, width];
            for (short i = 0; i < lenght; i++)//i -номер слоя, lenght-1 слоев
            {
                //здесь можно запустить потоки на каждый слой, потому что значения клеток в соседних слоях не учитываются
                //тогда эти нижние циклы в функцию потока засунуть. Мы пока одним потоком посчитаем
                for (short j = 0; j < height; j++)
                {
                    for (short k = 1; k < width - 1; k++)//1;-1 - потому что там входы микрофона
                    {
                        byte n = CountNeigh(i, j, k);//кол-во живых соседей
                        if (rule.CheckBorn(n))
                        {//клетка рождается
                            next[i, j, k] = 1;
                        }
                        else if (rule.CheckAlive(n))
                        {//остается жить, если жила до этого, и если в правиле есть молодение - молодеет
                            if (cell[i, j, k] > 0)
                            {
                                next[i, j, k] = (short)(cell[i, j, k] - rule.be_yunger);
                                if (next[i, j, k] < 1) next[i, j, k] = 1;//моложе 1 года быть не может
                            }
                        }
                        else if (cell[i, j, k] > 0)
                        {//cтареет
                            next[i, j, k] = (short)(cell[i, j, k] + 1);
                            //умирает ибо состарилась
                            if (next[i, j, k] > rule.max_age)
                            {
                                next[i, j, k] = 0;
                            }
                        }
                    }
                }
            }
            //lock (syncObject)//для мультитрединга
            {
                for (short i = 0; i < lenght; i++)
                    for (short j = 0; j < height; j++)
                    { next[i, j, 0] = cell[i, j, 0]; next[i, j, width - 1] = cell[i, j, width - 1]; } //пока обновляли коробку, данные с микрофона могли измениться

                cell = next;//и теперь массив ссылается сюда 
            }

            stepfinished = true;
            queChangeAgeFunc();
        }

        //в отличии от 3Д, здесь всего 8 соседей и i - по сути номер слоя
        private byte CountNeigh(short i, short j, short k)
        {
            byte neigh = 0;
            //если внутренняя клетка
            if (i > 0 && i < (lenght - 1) && j > 0 && j < (height - 1) && k > 0 && k < (width - 1))
            {
                //for (int ii = (i - 1); ii <= (i + 1); ii++)  //экономия цикла на лицо!  
                {
                    for (int jj = (j - 1); jj <= (j + 1); jj++)
                    {
                        for (int kk = (k - 1); kk <= (k + 1); kk++)
                        {
                            if (!(jj == j && kk == k))//не сама клетка
                            {
                                if (cell[i, jj, kk] > 0) neigh++;
                            }
                        }
                    }
                }
            }
            else //граница
            {
                //for (int ii = i - 1; ii <= i + 1; ii++)//экономия цикла на лицо!  
                {
                    for (int jj = j - 1; jj <= j + 1; jj++)
                    {
                        for (int kk = k - 1; kk <= k + 1; kk++)
                        {
                            
                            int jjj = jj;
                            int kkk = kk;
                            if (!(jj == j && kk == k))//не сама клетка
                            {
                                if (jj < 0) jjj = height - 1;//если вышли за границу, берем противоположную стенку
                                else if (jj >= height) jjj = 0;

                                if (kk < 0) kkk = width - 1;
                                else if (kk >= width) kkk = 0;

                                if (cell[i, jjj, kkk] > 0) neigh++;
                            }
                        }
                    }
                }

            }
            return neigh;
        }
    }

}
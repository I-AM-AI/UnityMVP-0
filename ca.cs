﻿using System.Collections;
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

        //конструктор длина,ширина,высота коробки, правило эволюции
        public CellularAutamata3D(short l, short h, short w, string r)
		{
			lenght = l;
			height = h;
			width = w;
			
			cell = new short[l,h,w];
            //for (short i = 0; i < l; i++) for (short j = 0; j < h; j++) for (short k = 0; k < w; k++) cell[i, j, k] = 0;
			rule = new Rule(r);

            myThread = new Thread(new ThreadStart(NextStepThread));
        }

        //меняем в заданной клетке возвраст руками
        //возвращаем 0 если удачно
        public override short ChangeAge(short i, short j, short k, short val)
        {
            if (val < 0) return -1;
            else if (val > rule.max_age) return 1;
            else if (i < 0 || j < 0 || k < 0) return -2;
            else if (i >= lenght || j >= height || k >= width) return 2;

            cell[i, j, k] = val;
            return 0;
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

            cell[i, j, k] = val;
            cell[ii, jj, kk] = (short)(rule.max_age-1);//а в случайного соседа немного нейромедиатора (хватит на один цикл КА)

            return 0;
        }
        public override void NextStep()
        {
            if(!myThread.IsAlive)
            {               
                myThread = new Thread(new ThreadStart(NextStepThread));
                myThread.Start();
            }
            
        }
        public void NextStepThread()
        {
            //строим новый массив значений
            short[,,] next = new short[lenght, height, width];
            for(short i=0;i<lenght;i++)
            {
                for(short j=0;j<height;j++)
                {
                    for (short k = 0; k < width;k++)
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
                        else if(cell[i, j, k]>0)
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
                cell = next;//и теперь массив ссылается сюда 
            }
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
	}
}